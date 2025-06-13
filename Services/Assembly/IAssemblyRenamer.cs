using dnlib.DotNet;
using rnDotnet.Services.Core;
using rnDotnet.Services.Core;

namespace rnDotnet.Services.Assembly
{
    public interface IAssemblyRenamer
    {
        void ApplyRenames(ModuleDef module, IMemberInfoManager memberManager, string descriptiveNamePrefix);
    }
}