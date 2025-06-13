using dnlib.DotNet;

namespace rnDotnet.Models
{
    public class MemberInfo
    {
        // The original, full name/identifier of the member (e.g., namespace string, Type.FullName, Method.FullName)
        public string UniqueId { get; set; }

        // The short, original name of the member (e.g., "MethodA", "ClassB", "My")
        public string OriginalShortName { get; set; }

        // The descriptive name updated by AI (e.g., "llm_ProcessData")
        public string CurrentDescriptiveName { get; set; }

        // The type of member (e.g., "Class", "Method", "Field", "Namespace")
        public string MemberType { get; set; }

        // A detailed description provided by the AI
        public string Description { get; set; }

        // Flag to indicate if this member has received a descriptive name from the AI
        public bool IsNamed { get; set; }

        // Reference to the actual dnlib object (TypeDef, MethodDef, FieldDef).
        // For 'Namespace' type, this will be null as it's just a string.
        public IMemberDef DnlibMember { get; set; } // Changed to IMemberDef for type safety
    }
}
