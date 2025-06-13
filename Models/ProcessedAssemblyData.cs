using dnlib.DotNet;

namespace rnDotnet.Models
{
    public class ProcessedAssemblyData
    {
        public ModuleDef Module { get; }
        public string DecompiledCode { get; }
        public string Sha256Hash { get; }
        public string OriginalPath { get; }

        public ProcessedAssemblyData(ModuleDef module, string decompiledCode, string sha256Hash, string originalPath)
        {
            Module = module;
            DecompiledCode = decompiledCode;
            Sha256Hash = sha256Hash;
            OriginalPath = originalPath;
        }
    }
}