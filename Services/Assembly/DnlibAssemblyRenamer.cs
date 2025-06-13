using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using rnDotnet.Services.Core;
using rnDotnet.Services.Core;
using System;
using System.Linq;

namespace rnDotnet.Services.Assembly
{
    public class DnlibAssemblyRenamer : IAssemblyRenamer
    {
        private readonly ILogger<DnlibAssemblyRenamer> _logger;

        public DnlibAssemblyRenamer(ILogger<DnlibAssemblyRenamer> logger)
        {
            _logger = logger;
        }

        public void ApplyRenames(ModuleDef module, IMemberInfoManager memberManager, string descriptiveNamePrefix)
        {
            _logger.LogInformation("Applying renames to assembly elements...");

            // --- Pass 1: Rename Namespaces ---
            // Collect namespace renames from the master list
            var renamedNamespacesMap = memberManager.GetAllMembers()
                .Where(m => m.MemberType == "Namespace" && m.IsNamed && m.CurrentDescriptiveName != m.OriginalShortName)
                .ToDictionary(m => m.UniqueId, m => m.CurrentDescriptiveName);

            // Apply namespace renames to all TypeDefs that use them
            foreach (TypeDef typeDef in module.Types)
            {
                // Handle non-global namespaces
                if (!string.IsNullOrEmpty(typeDef.Namespace) && renamedNamespacesMap.TryGetValue(typeDef.Namespace, out var newNamespace))
                {
                    if (typeDef.Namespace != newNamespace) // Avoid renaming if already matches
                    {
                        _logger.LogDebug("Renaming namespace for type '{FullName}' from '{OldNamespace}' to '{NewNamespace}'",
                            typeDef.FullName, typeDef.Namespace, newNamespace);
                        typeDef.Namespace = newNamespace;
                    }
                }
                // Handle global namespace (represented by an empty string UniqueId)
                else if (string.IsNullOrEmpty(typeDef.Namespace) && renamedNamespacesMap.TryGetValue("", out newNamespace))
                {
                    if (string.IsNullOrEmpty(typeDef.Namespace) && !string.IsNullOrEmpty(newNamespace)) // Only rename if it's currently global and new name is not empty
                    {
                        _logger.LogDebug("Renaming global namespace for type '{FullName}' to '{NewNamespace}'",
                            typeDef.FullName, newNamespace);
                        typeDef.Namespace = newNamespace;
                    }
                }
            }

            // --- Pass 2: Rename Classes, Methods, Fields ---
            foreach (var memberInfo in memberManager.GetAllMembers())
            {
                // Skip if not named, or target name is empty, or it's a namespace already handled
                if (!memberInfo.IsNamed || string.IsNullOrEmpty(memberInfo.CurrentDescriptiveName) || memberInfo.MemberType == "Namespace")
                    continue;

                // Skip if current descriptive name is the same as original short name AND not prefixed
                // This prevents logging and "renaming" items that AI didn't actually change for usability
                if (memberInfo.CurrentDescriptiveName == memberInfo.OriginalShortName && !memberInfo.CurrentDescriptiveName.StartsWith(descriptiveNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Skipped rename for '{UniqueId}': AI suggested the same name ('{NewName}') as its original short name, and it's not prefixed.",
                        memberInfo.UniqueId, memberInfo.CurrentDescriptiveName);
                    continue;
                }

                switch (memberInfo.MemberType)
                {
                    case "Class": // TypeDef
                        if (memberInfo.DnlibMember is TypeDef typeDef && typeDef.Name != memberInfo.CurrentDescriptiveName)
                        {
                            _logger.LogDebug("Renaming type '{FullName}' to '{NewName}'", typeDef.FullName, memberInfo.CurrentDescriptiveName);
                            typeDef.Name = memberInfo.CurrentDescriptiveName;
                        }
                        break;
                    case "Method": // MethodDef
                        if (memberInfo.DnlibMember is MethodDef methodDef)
                        {
                            // Ensure it's not a constructor or static constructor, as these cannot be renamed
                            if (!methodDef.IsConstructor && !methodDef.IsStaticConstructor && methodDef.Name != memberInfo.CurrentDescriptiveName)
                            {
                                _logger.LogDebug("Renaming method '{FullName}' to '{NewName}'", methodDef.FullName, memberInfo.CurrentDescriptiveName);
                                methodDef.Name = memberInfo.CurrentDescriptiveName;
                            }
                        }
                        break;
                    case "Field": // FieldDef
                        if (memberInfo.DnlibMember is FieldDef fieldDef && fieldDef.Name != memberInfo.CurrentDescriptiveName)
                        {
                            _logger.LogDebug("Renaming field '{FullName}' to '{NewName}'", fieldDef.FullName, memberInfo.CurrentDescriptiveName);
                            fieldDef.Name = memberInfo.CurrentDescriptiveName;
                        }
                        break;
                }
            }
        }
    }
}