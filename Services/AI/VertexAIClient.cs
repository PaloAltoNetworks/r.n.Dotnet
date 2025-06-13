using Google.Cloud.AIPlatform.V1;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using rnDotnet.Configurations;
using System.Linq;
using System.Threading.Tasks;
using System;
using Google.Api.Gax.Grpc;
using System.Text;

namespace rnDotnet.Services.AI
{
    public class VertexAIClient : IAIClient
    {
        private readonly ILogger<VertexAIClient> _logger;
        private readonly AIConfig _aiConfig;

        // Assuming AIConfig is injected if it's needed elsewhere, otherwise remove.
        public VertexAIClient(ILogger<VertexAIClient> logger, AIConfig aiConfig)
        {
            _logger = logger;
            _aiConfig = aiConfig;
        }

        public async Task<string> GetCompletionAsync(string prompt, string modelId, string projectId, string location, string publisher, bool streamToConsole = false)
        {
            _logger.LogInformation("Sending data to AI Model: {ModelId}...", modelId);

            var predictionServiceClient = new PredictionServiceClientBuilder
            {
                Endpoint = $"{location}-aiplatform.googleapis.com"
            }.Build();

            var generateContentRequest = new GenerateContentRequest
            {
                Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{modelId}",
                Contents =
                {
                    new Content
                    {
                        Role = "USER",
                        Parts =
                        {
                            new Part { Text = prompt }
                        }
                    }
                }
            };

            try
            {
                StringBuilder fullCompletion = new StringBuilder();
                using PredictionServiceClient.StreamGenerateContentStream response = predictionServiceClient.StreamGenerateContent(generateContentRequest);
                AsyncResponseStream<GenerateContentResponse> responseStream = response.GetResponseStream();

                // Only print the header if streaming is enabled
                if (streamToConsole)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("--- Live LLM Response Stream ---");
                    Console.ResetColor();
                }

                while (await responseStream.MoveNextAsync())
                {
                    GenerateContentResponse responseItem = responseStream.Current;
                    if (responseItem.Candidates.Any() &&
                        responseItem.Candidates[0].Content.Parts.Any() &&
                        !string.IsNullOrEmpty(responseItem.Candidates[0].Content.Parts[0].Text))
                    {
                        string chunk = responseItem.Candidates[0].Content.Parts[0].Text;
                        fullCompletion.Append(chunk);

                        // Only write the chunk to the console if streaming is enabled
                        if (streamToConsole)
                        {
                            Console.Write(chunk);
                            Console.Out.Flush();
                        }
                    }
                }

                // Only print the footer if streaming is enabled
                if (streamToConsole)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("--- End of Stream ---");
                    Console.ResetColor();
                    Console.WriteLine();
                }

                string resultText = fullCompletion.ToString();
                _logger.LogDebug("Received AI response for model {ModelId}. Total Length: {Length} characters.", modelId, resultText.Length);
                return resultText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while communicating with AI service: {Message}", ex.Message);
                Console.ResetColor();
                throw;
            }
        }
    }
}