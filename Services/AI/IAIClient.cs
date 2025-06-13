using System.Threading.Tasks;

namespace rnDotnet.Services.AI
{
    public interface IAIClient
    {
        Task<string> GetCompletionAsync(string prompt, string modelId, string projectId, string location, string publisher, bool streamToConsole = false);
    }
}