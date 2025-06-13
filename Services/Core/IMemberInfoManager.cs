using dnlib.DotNet;
using rnDotnet.Models;
using System.Collections.Generic;
using System.Linq;

namespace rnDotnet.Services.Core
{
    public interface IMemberInfoManager
    {
        event Action<string, string, string, string> MemberRenamed;
        void PopulateFromModule(ModuleDef module);
        string GenerateAIPromptData();
        int UpdateMembersFromAIResponse(string aiResponse, string descriptiveNamePrefix);
        IEnumerable<MemberInfo> GetAllMembers();
        int GetTotalMembersCount();
        int GetNamedMembersCount();
    }
}