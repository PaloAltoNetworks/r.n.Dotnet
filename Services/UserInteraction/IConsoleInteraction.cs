using System;

namespace rnDotnet.Services.UserInteraction
{
    public interface IConsoleInteraction
    {
        string GetInput(string prompt);
        bool Confirm(string message, bool defaultValue);
        int GetMaxPasses(int recommendedPasses);
        string GetSummaryPersona();
        string GetRenamingPersona();
        string PromptForExistingFileSelection(List<string> foundFiles);
        void WriteLine(string message);
        void Write(string message);
    }
}