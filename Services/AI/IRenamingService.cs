using rnDotnet.Services.Core;

namespace rnDotnet.Services.AI
{
    public interface IRenamingService
    {
        Task<int> PerformRenamingIterationAsync(
            string decompiledCode,
            IMemberInfoManager memberInfoManager,
            string prefix,
            int passCount,
            string systemInstruction);
    }
}