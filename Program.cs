using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using rnDotnet.Configurations;
using rnDotnet.Services.AI;
using rnDotnet.Services.Assembly;
using rnDotnet.Services.Core;
using rnDotnet.Services.UserInteraction;
using rnDotnet.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace rnDotnet
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Setup Configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Bind the configuration section to the AIConfig class for validation
            var aiConfig = configuration.GetSection("AI").Get<AIConfig>();

            // Check if the essential ProjectId and Location are present.
            // If not, print an error and exit immediately.
            if (aiConfig == null || string.IsNullOrWhiteSpace(aiConfig.ProjectId) || string.IsNullOrWhiteSpace(aiConfig.Location))
            {
                // Use Console.Error for semantic correctness
                Console.Error.WriteLine("FATAL ERROR: Critical AI configuration is missing in appsettings.json.");
                Console.Error.WriteLine("Please ensure both 'ProjectId' and 'Location' are set under the 'AI' section inside appsettings.json.");
                Console.Error.WriteLine("Example:");
                Console.Error.WriteLine("\"AI\": {");
                Console.Error.WriteLine("  \"ProjectId\": \"your-gcp-project-id\",");
                Console.Error.WriteLine("  \"Location\": \"us-central1\",");
                Console.Error.WriteLine("  ...");
                Console.Error.WriteLine("}");
                // Exit the application because it cannot function without these settings.
                return;
            }

            // 2. Setup Dependency Injection
            ServiceCollection services = new ServiceCollection();

            // Configure Logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // Register configuration objects
            services.Configure<AppConfig>(configuration.GetSection("Application"));
            services.AddSingleton(cfg => cfg.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppConfig>>().Value);

            services.Configure<AIConfig>(configuration.GetSection("AI"));
            services.AddSingleton(cfg => cfg.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIConfig>>().Value);

            // --- ADD THESE LINES TO DEBUG CONFIGURATION BINDING ---
            var aiConfigDebug = configuration.GetSection("AI").Get<AIConfig>();
            if (aiConfigDebug == null)
            {
                Console.WriteLine("DEBUG: AIConfig section not found or failed to bind!");
            }
            else
            {
                Console.WriteLine($"DEBUG: AIConfig.Location: {aiConfigDebug.Location}");
                Console.WriteLine($"DEBUG: AIConfig.Publisher: {aiConfigDebug.Publisher}");
                Console.WriteLine($"DEBUG: AIConfig.RenamingModel: {aiConfigDebug.RenamingModel}");
                Console.WriteLine($"DEBUG: AIConfig.SummaryModel: {aiConfigDebug.SummaryModel}");
            }
            // --- END DEBUG LINES ---


            // Register Services
            services.AddSingleton<IConsoleInteraction, ConsoleInteraction>();
            services.AddSingleton<IFileHasher, FileHasher>();
            services.AddSingleton<IAssemblyProcessor, DnlibAssemblyProcessor>();
            services.AddSingleton<IAIClient, VertexAIClient>();
            services.AddSingleton<IMemberInfoManager, MemberInfoManager>();
            services.AddSingleton<IRenamingService, AIRenamingService>();
            services.AddSingleton<IAssemblyRenamer, DnlibAssemblyRenamer>();
            services.AddSingleton<IMalwareSummaryGenerator, AIMalwareSummaryGenerator>();

            // Register the main application orchestrator
            services.AddTransient<ApplicationRunner>();

            // Build the service provider
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get the application runner and execute
            try
            {
                var appRunner = serviceProvider.GetRequiredService<ApplicationRunner>();
                await appRunner.RunAsync();
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogCritical(ex, "Application terminated unexpectedly.");
            }
            finally
            {
                // Ensure all resources are properly disposed
                if (serviceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
        }
    }
}