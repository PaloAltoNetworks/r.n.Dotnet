using dnlib.DotNet;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.Extensions.Logging;
using rnDotnet.Models;
using rnDotnet.Services.Assembly;
using System;
using System.IO;

namespace rnDotnet.Services.Assembly
{
    public class DnlibAssemblyProcessor : IAssemblyProcessor
    {
        private readonly IFileHasher _fileHasher;
        private readonly ILogger<DnlibAssemblyProcessor> _logger;

        public DnlibAssemblyProcessor(IFileHasher fileHasher, ILogger<DnlibAssemblyProcessor> logger)
        {
            _fileHasher = fileHasher;
            _logger = logger;
        }

        public ProcessedAssemblyData LoadAndDecompile(string assemblyPath)
        {
            try
            {
                _logger.LogInformation("Loading assembly: {AssemblyPath}", assemblyPath);
                ModuleDef module = ModuleDefMD.Load(assemblyPath);
                string sha256Hash = _fileHasher.CalculateSHA256(assemblyPath);

                string decompiledCode = DecompileInternal(assemblyPath);

                return new ProcessedAssemblyData(module, decompiledCode, sha256Hash, assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading or decompiling assembly '{AssemblyPath}': {Message}", assemblyPath, ex.Message);
                return null;
            }
        }

        public string DecompileOnly(string assemblyPath)
        {
            _logger.LogInformation("Decompiling assembly only: {AssemblyPath}", assemblyPath);
            return DecompileInternal(assemblyPath);
        }

        private string DecompileInternal(string assemblyPath)
        {
            var settings = new DecompilerSettings(LanguageVersion.Latest)
            {
                ThrowOnAssemblyResolveErrors = false,
                RemoveDeadCode = true,
                UseDebugSymbols = true
            };

            var resolver = new UniversalAssemblyResolver(assemblyPath, true, null);
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

            string gacPath = @"C:\Windows\Microsoft.NET\assembly";
            if (Directory.Exists(gacPath))
            {
                foreach (string dependencyPath in Directory.EnumerateFiles(gacPath, "*.dll", SearchOption.AllDirectories))
                {
                    resolver.AddSearchDirectory(Path.GetDirectoryName(dependencyPath));
                }
            }
            else
            {
                _logger.LogWarning("GAC folder not found at: {GacPath}", gacPath);
            }

            var decompiler = new CSharpDecompiler(assemblyPath, resolver, settings);
            string decompiledCode = decompiler.DecompileWholeModuleAsString();
            _logger.LogInformation("Assembly has been decompiled.");
            return decompiledCode;
        }
    }
}
