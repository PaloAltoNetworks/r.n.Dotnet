using Microsoft.Extensions.Logging;
using rnDotnet.Configurations;
using rnDotnet.Models;
using rnDotnet.Services.AI;
using rnDotnet.Services.Assembly;
using rnDotnet.Services.UserInteraction;
using rnDotnet.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using dnlib.DotNet;
using rnDotnet.Utils.Prompts;

namespace rnDotnet.Services.Core
{
    public class ApplicationRunner
    {
        private readonly IConsoleInteraction _console;
        private readonly IAssemblyProcessor _assemblyProcessor;
        private readonly IMemberInfoManager _memberInfoManager;
        private readonly IRenamingService _renamingService;
        private readonly IAssemblyRenamer _assemblyRenamer;
        private readonly IMalwareSummaryGenerator _summaryGenerator;
        private readonly ILogger<ApplicationRunner> _logger;
        private readonly AppConfig _appConfig;

        public ApplicationRunner(
            IConsoleInteraction console,
            IAssemblyProcessor assemblyProcessor,
            IMemberInfoManager memberInfoManager,
            IRenamingService renamingService,
            IAssemblyRenamer assemblyRenamer,
            IMalwareSummaryGenerator summaryGenerator,
            ILogger<ApplicationRunner> logger,
            AppConfig appConfig)
        {
            _console = console;
            _assemblyProcessor = assemblyProcessor;
            _memberInfoManager = memberInfoManager;
            _renamingService = renamingService;
            _assemblyRenamer = assemblyRenamer;
            _summaryGenerator = summaryGenerator;
            _logger = logger;
            _appConfig = appConfig;
            _memberInfoManager.MemberRenamed += OnMemberRenamed;
        }

        /// This method is called whenever the MemberRenamed event is raised.
        /// It's responsible for printing the update to the console.
        private void OnMemberRenamed(string oldName, string newName, string type, string uniqueId)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Updated ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"'{oldName}'");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" (Type: {type}) to ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"'{newName}'");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" (UniqueId: {uniqueId})");

            Console.ResetColor();

            Console.Out.Flush();
        }

        public async Task RunAsync()
        {
            _console.WriteLine("\nWelcome to r.n.Dotnet: .NET Assembly Renamer and Analyzer\n");
            Console.ForegroundColor = ConsoleColor.DarkGray;

            // 1. Get the initial assembly path from the user first.
            string initialAssemblyPath = _console.GetInput("Enter the path to the .NET assembly you want to analyze: ").Trim();

            if (initialAssemblyPath.StartsWith("\"") && initialAssemblyPath.EndsWith("\""))
            {
                initialAssemblyPath = initialAssemblyPath.Substring(1, initialAssemblyPath.Length - 2);
            }

            if (string.IsNullOrWhiteSpace(initialAssemblyPath) || !File.Exists(initialAssemblyPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine("The specified assembly file does not exist.");
                Console.ResetColor();
                return;
            }

            // 2. Now, check the DIRECTORY of that file for existing analysis runs.
            string targetDirectory = Path.GetDirectoryName(initialAssemblyPath);
            var foundFiles = Directory.GetFiles(targetDirectory, "*.*")
                .Where(f => Path.GetFileName(f).Contains("_renamed") && Path.GetFileName(f).Contains("_pass"))
                .ToList();

            if (foundFiles.Any())
            {
                string selectedFile = _console.PromptForExistingFileSelection(foundFiles);
                if (!string.IsNullOrWhiteSpace(selectedFile))
                {
                    // User selected a file, so run summary-only mode and then exit.
                    await RunSummaryOnlyModeAsync(selectedFile);
                    return; // Exit application after summary mode.
                }

                // User declined, so print a separator and continue to the normal flow.
                _console.WriteLine("\n----------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                _console.WriteLine("Continuing with new analysis on the original file...\n");
                Console.ResetColor();
            }

            // 3. If no files were found or the user declined, proceed with the full analysis
            //    of the file they originally specified.
            await RunFullAnalysisAsync(initialAssemblyPath);
        }

        private async Task RunFullAnalysisAsync(string assemblyPath)
        {
            ProcessedAssemblyData processedData = null;
            try
            {
                // The assembly path is now passed in, so we don't ask for it again.
                string prefixInput = _console.GetInput($"Enter the prefix for descriptive names (default is '{_appConfig.DescriptiveNamePrefix}'): ").Trim();
                if (!string.IsNullOrEmpty(prefixInput))
                {
                    _appConfig.DescriptiveNamePrefix = prefixInput;
                }

                processedData = _assemblyProcessor.LoadAndDecompile(assemblyPath);
                if (processedData?.Module == null)
                {
                    _logger.LogError("Failed to load or decompile the assembly. Exiting.");
                    return;
                }

                _memberInfoManager.PopulateFromModule(processedData.Module);
                _console.WriteLine($"Identified {processedData.Module.Types.Count} distinct types (classes, structs, enums, interfaces) in the assembly.");
                _console.WriteLine($"Total identified members (including namespaces, methods, fields): {_memberInfoManager.GetTotalMembersCount()} elements.");

                string renamingPersonaKey = _console.GetRenamingPersona();
                string renamingSystemInstruction = DotnetRenamingPrompts.RenamingPersonas[renamingPersonaKey];

                int passCount = 0;
                int maxPasses = _console.GetMaxPasses(CalculateRecommendedPasses());

                while (passCount < maxPasses)
                {
                    _console.WriteLine($"\n--- Starting Pass {passCount + 1}/{maxPasses} ---");
                    int newMappingsCount = await _renamingService.PerformRenamingIterationAsync(
                        processedData.DecompiledCode,
                        _memberInfoManager,
                        _appConfig.DescriptiveNamePrefix,
                        passCount,
                        renamingSystemInstruction
                    );
                    _console.WriteLine($"Pass {passCount + 1}: {newMappingsCount} new or updated mappings received. Total named: {_memberInfoManager.GetNamedMembersCount()} / {_memberInfoManager.GetTotalMembersCount()}");

                    if (_memberInfoManager.GetNamedMembersCount() == _memberInfoManager.GetTotalMembersCount())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine("All identified members have been given descriptive names. Stopping iteration early.");
                        Console.ResetColor();
                        break;
                    }
                    if (newMappingsCount == 0 && passCount > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        _console.WriteLine("No new mappings received in this pass. Stopping iteration to avoid superfluous AI calls.");
                        Console.ResetColor();
                        break;
                    }
                    passCount++;
                }

                if (_memberInfoManager.GetNamedMembersCount() < _memberInfoManager.GetTotalMembersCount())
                {
                    _console.WriteLine($"\n--- Renaming Process Completed (Partial Renaming) ---");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    _console.WriteLine($"Warning: Not all members could be renamed. {_memberInfoManager.GetTotalMembersCount() - _memberInfoManager.GetNamedMembersCount()} out of {_memberInfoManager.GetTotalMembersCount()} members remain with obfuscated names.");
                    _console.WriteLine($"Consider running the tool again with more passes or refining the AI prompt for better results.");
                    Console.ResetColor();
                }
                else
                {
                    _console.WriteLine($"\n--- Renaming Process Complete (Full Renaming) ---");
                    Console.ForegroundColor = ConsoleColor.Green;
                    _console.WriteLine("All identifiable members have been renamed!");
                    Console.ResetColor();
                }

                _assemblyRenamer.ApplyRenames(processedData.Module, _memberInfoManager, _appConfig.DescriptiveNamePrefix);
                string renamedAssemblyPath = GetRenamedAssemblyPath(processedData.OriginalPath, processedData.Sha256Hash, passCount);
                processedData.Module.Write(renamedAssemblyPath);
                _console.WriteLine($"\nRenamed assembly saved as {renamedAssemblyPath}");

                if (_console.Confirm("Generate a malware summary report?", true))
                {
                    string persona = _console.GetSummaryPersona();
                    string systemInstruction = DotnetSummaryPrompts.SummaryPersonas[persona];
                    string decompiledRenamedAssembly = _assemblyProcessor.DecompileOnly(renamedAssemblyPath);
                    string summary = await _summaryGenerator.GenerateSummaryAsync(decompiledRenamedAssembly, systemInstruction);

                    _console.WriteLine("");
                    _console.Write("--- [");
                    Console.ForegroundColor = ConsoleColor.Green;
                    _console.Write(persona);
                    Console.ResetColor();
                    _console.WriteLine("] Malware Summary Report ---");
                    _console.WriteLine(summary);

                    string summaryFileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}_{persona.Replace(" / ", "_")}_summary.txt";
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(assemblyPath), summaryFileName), summary);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during application execution.");
                Console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"An error occurred: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                processedData?.Module?.Dispose();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                _console.WriteLine("Module disposed.");
                Console.ResetColor();
            }
        }

        private async Task RunSummaryOnlyModeAsync(string assemblyPath)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                _console.WriteLine("\nEntering summary-only mode...");
                Console.ResetColor();

                string persona = _console.GetSummaryPersona();
                string systemInstruction = DotnetSummaryPrompts.SummaryPersonas[persona];

                _console.WriteLine("\nDecompiling assembly for summary...");
                string decompiledCode = _assemblyProcessor.DecompileOnly(assemblyPath);

                if (string.IsNullOrWhiteSpace(decompiledCode))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine("Failed to decompile the selected assembly.");
                    Console.ResetColor();
                    return;
                }

                _console.WriteLine("Decompilation complete. Generating summary report...");
                string summary = await _summaryGenerator.GenerateSummaryAsync(decompiledCode, systemInstruction);

                // Print and save the summary
                Console.WriteLine();
                Console.Write("--- [");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(persona);
                Console.ResetColor();
                Console.WriteLine("] Malware Summary Report ---");
                Console.WriteLine(summary);

                // Save summary to file
                string summaryFileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}_summary.txt";
                string summaryFilePath = Path.Combine(Path.GetDirectoryName(assemblyPath), summaryFileName);
                File.WriteAllText(summaryFilePath, summary);
                _console.WriteLine($"\nSummary report saved to: {summaryFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during summary-only mode.");
                Console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"An error occurred: {ex.Message}");
                Console.ResetColor();
            }
        }


        private int CalculateRecommendedPasses()
        {
            int totalMembers = _memberInfoManager.GetTotalMembersCount();
            // Using Math.Ceiling to ensure that if there are any members, at least 1 pass is recommended,
            // and partial groups are rounded up. Adjust 80.0 based on typical AI batch size.
            int recommendedPasses = (int)Math.Ceiling(totalMembers / 80.0);

            // Ensure a minimum of 1 pass if there are members, and 0 if no members (though not expected here)
            if (totalMembers > 0 && recommendedPasses == 0) recommendedPasses = 1;
            return recommendedPasses;
        }

        private string GetRenamedAssemblyPath(string originalPath, string sha256Hash, int passCount)
        {
            string baseName = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            string directory = Path.GetDirectoryName(originalPath);

            // Using SHA256 hash in filename for unique output if multiple runs of same file
            // Adding passCount to easily identify the output of each iteration.
            return Path.Combine(directory, $"{baseName}_renamed_{sha256Hash.Substring(0, 8)}_pass{passCount}{extension}");
        }
    }
}
