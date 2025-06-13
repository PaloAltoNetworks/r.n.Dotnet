using Microsoft.Extensions.Logging;
using rnDotnet.Configurations;
using rnDotnet.Services.Core;
using rnDotnet.Utils.Prompts;
using rnDotnet.Services.Core;
using System.Threading.Tasks;

namespace rnDotnet.Services.AI
{
    public class AIRenamingService : IRenamingService
    {
        private readonly IAIClient _aiClient;
        private readonly AIConfig _aiConfig;
        private readonly ILogger<AIRenamingService> _logger;

        public AIRenamingService(IAIClient aiClient, AIConfig aiConfig, ILogger<AIRenamingService> logger)
        {
            _aiClient = aiClient;
            _aiConfig = aiConfig;
            _logger = logger;
        }

        public async Task<int> PerformRenamingIterationAsync(string decompiledCode, IMemberInfoManager memberManager, string descriptiveNamePrefix, int passCount, string SystemInstructions)
        {
            string enumerationResults = memberManager.GenerateAIPromptData();

            // If no more unnamed members, stop iterating early
            if (string.IsNullOrWhiteSpace(enumerationResults) || enumerationResults.Trim() == "{}")
            {
                _logger.LogInformation("No more unnamed members to process for AI renaming. Skipping AI call.");
                return 0;
            }

            string prompt = RenamingPromptBuilder.BuildPrompt(SystemInstructions, decompiledCode, enumerationResults, passCount);
            string aiResponse = await _aiClient.GetCompletionAsync(
                prompt,
                _aiConfig.RenamingModel,
                _aiConfig.ProjectId,
                _aiConfig.Location,
                _aiConfig.Publisher,
                streamToConsole : true
            );

            _logger.LogInformation("AI Response (Pass {PassCount}):\n{Response}", passCount + 1, aiResponse);

            return memberManager.UpdateMembersFromAIResponse(aiResponse, descriptiveNamePrefix);
        }
    }
}