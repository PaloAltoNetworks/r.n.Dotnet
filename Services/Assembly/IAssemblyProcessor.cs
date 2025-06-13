using dnlib.DotNet;
using rnDotnet.Models;

namespace rnDotnet.Services.Assembly
{
    public interface IAssemblyProcessor
    {
        ProcessedAssemblyData LoadAndDecompile(string assemblyPath);
        string DecompileOnly(string assemblyPath);
    }
}
