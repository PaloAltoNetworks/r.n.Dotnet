using System;
using System.Linq;
using rnDotnet.Utils.Prompts;

namespace rnDotnet.Services.UserInteraction
{
    public class ConsoleInteraction : IConsoleInteraction
    {
        public string GetInput(string prompt)
        {
            // Set color for the prompt, write it, then reset
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(prompt);

            // Set color for the '>>' symbol and then reset
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(">> ");
            Console.ResetColor();

            return Console.ReadLine();
        }

        public bool Confirm(string message, bool defaultValue)
        {
            // Add a leading newline to ensure this prompt is visually
            // separated from any previous logging output.
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(message);
            Console.ResetColor();

            Console.Write(" (y/n) ");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{(defaultValue ? 'y' : 'n')}]");
            Console.ResetColor();

            Console.Write(" : ");

            string input = Console.ReadLine().Trim().ToLower();

            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }

            return input == "y" || input == "yes";
        }


        public int GetMaxPasses(int recommendedPasses)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Enter the maximum number of refinement iterations ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"(recommended: {recommendedPasses}, default is 3)");
            Console.ResetColor();
            Console.Write(": ");

            string input = Console.ReadLine().Trim();
            if (string.IsNullOrEmpty(input))
            {
                return 3;
            }

            if (int.TryParse(input, out int maxPasses) && maxPasses > 0)
            {
                return maxPasses;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid input. Using default value of 3.");
            Console.ResetColor();
            return 3;
        }

        public string GetRenamingPersona()
        {
            var personas = DotnetRenamingPrompts.RenamingPersonas.Keys.ToList();

            Console.WriteLine(); // Add a newline for spacing
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Please select a focus area for the renaming process:");
            Console.ResetColor();

            for (int i = 0; i < personas.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {personas[i]}");
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Default is 1. {personas[0]}");
            Console.ResetColor();

            int choice = -1;
            while (choice < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Enter your choice (1-{personas.Count})");
                Console.ResetColor();
                Console.Write(": ");

                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    choice = 1;
                    break;
                }
                if (!int.TryParse(input, out choice) || choice < 1 || choice > personas.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid choice. Please try again.");
                    Console.ResetColor();
                    choice = -1;
                }
            }
            string selectedPersona = personas[choice - 1];

            Console.Write("Using renaming focus: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"'{selectedPersona}'");
            Console.ResetColor();

            return selectedPersona;
        }

        public string GetSummaryPersona()
        {
            var personas = DotnetSummaryPrompts.SummaryPersonas.Keys.ToList();

            Console.WriteLine(); // Add a newline for spacing
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Please select a persona for the malware summary report:");
            Console.ResetColor();

            for (int i = 0; i < personas.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {personas[i]}");
            }

            int choice = -1;
            while (choice < 1 || choice > personas.Count)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Enter your choice (1-{personas.Count})");
                Console.ResetColor();
                Console.Write(": ");

                if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > personas.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid choice. Please try again.");
                    Console.ResetColor();
                    choice = -1;
                }
            }
            string selectedPersona = personas[choice - 1];

            Console.Write("Selected summary persona: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"'{selectedPersona}'");
            Console.ResetColor();

            return selectedPersona;
        }

        public string PromptForExistingFileSelection(List<string> foundFiles)
        {
            Console.WriteLine(); // Spacing
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Previously analyzed files found in this directory.");
            Console.ResetColor();

            if (!Confirm("Do you want to generate a summary for one of these files?", true))
            {
                return null; // User declined
            }

            if (foundFiles.Count == 1)
            {
                var file = foundFiles[0];
                Console.Write("Auto-selecting the only available file: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Path.GetFileName(file));
                Console.ResetColor();
                return file; // Auto-select if only one
            }

            // List multiple files for selection
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Please select a file to summarize:");
            Console.ResetColor();
            for (int i = 0; i < foundFiles.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {Path.GetFileName(foundFiles[i])}");
            }

            int choice = -1;
            while (choice < 1 || choice > foundFiles.Count)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Enter your choice (1-{foundFiles.Count})");
                Console.ResetColor();
                Console.Write(": ");

                if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > foundFiles.Count)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid choice. Please try again.");
                    Console.ResetColor();
                    choice = -1;
                }
            }
            return foundFiles[choice - 1];
        }


        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Write(string message)
        {
            Console.Write(message);
        }
    }
}
