using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using rnDotnet.Models;
using rnDotnet.Utils;
using rnDotnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace rnDotnet.Services.Core
{
    public class MemberInfoManager : IMemberInfoManager
    {
        private List<MemberInfo> _masterMemberList;
        private readonly ILogger<MemberInfoManager> _logger;

        public event Action<string, string, string, string> MemberRenamed;

        public MemberInfoManager(ILogger<MemberInfoManager> logger)
        {
            _masterMemberList = new List<MemberInfo>();
            _logger = logger;
        }

        public void PopulateFromModule(ModuleDef module)
        {
            _logger.LogInformation("Populating master list of identifiable members...");
            _masterMemberList.Clear(); // Clear any previous state
            var constructorRegex = new Regex(@"^\.ctor$", RegexOptions.IgnoreCase);

            // Use a HashSet to track namespaces to avoid duplicate entries in _masterMemberList
            var seenNamespaces = new HashSet<string>();

            foreach (TypeDef type in module.GetTypes())
            {
                // Add Namespace (only once per unique namespace string, including global)
                string ns = type.Namespace;
                // Treat global namespace as an empty string for consistent ID handling
                if (string.IsNullOrEmpty(ns)) ns = "";

                if (seenNamespaces.Add(ns))
                {
                    _masterMemberList.Add(new MemberInfo
                    {
                        UniqueId = ns,
                        OriginalShortName = ns,
                        CurrentDescriptiveName = ns,
                        MemberType = "Namespace",
                        Description = "",
                        IsNamed = false,
                        DnlibMember = null // No direct dnlib object for namespace string itself
                    });
                }

                // Add Class/Type (TypeDef)
                _masterMemberList.Add(new MemberInfo
                {
                    UniqueId = type.FullName,    // The definitive original identifier for AI
                    OriginalShortName = type.Name, // Original short name for AI to work with
                    CurrentDescriptiveName = type.Name, // Starting value
                    MemberType = "Class",    // Covers classes, structs, enums, interfaces
                    Description = "",
                    IsNamed = false,
                    DnlibMember = type           // Store reference to TypeDef
                });

                // Add Methods
                foreach (MethodDef method in type.Methods)
                {
                    // Skip constructors as they cannot be freely renamed
                    if (constructorRegex.IsMatch(method.Name)) continue;

                    _masterMemberList.Add(new MemberInfo
                    {
                        UniqueId = method.FullName,    // The definitive original identifier for AI
                        OriginalShortName = method.Name, // Original short name for AI to work with
                        CurrentDescriptiveName = method.Name, // Starting value
                        MemberType = "Method",
                        Description = "",
                        IsNamed = false,
                        DnlibMember = method           // Store reference to MethodDef
                    });
                }

                // Add Fields
                foreach (FieldDef field in type.Fields)
                {
                    _masterMemberList.Add(new MemberInfo
                    {
                        UniqueId = field.FullName,    // The definitive original identifier for AI
                        OriginalShortName = field.Name, // Original short name for AI to work with
                        CurrentDescriptiveName = field.Name, // Starting value
                        MemberType = "Field",
                        Description = "",
                        IsNamed = false,
                        DnlibMember = field           // Store reference to FieldDef
                    });
                }
            }
        }

        public string GenerateAIPromptData()
        {
            var aiInputData = new Dictionary<string, Dictionary<string, string>>();
            // Only get members that are NOT named yet, and are not constructors
            // Constructor names can't be freely renamed anyway, so no point asking AI
            var unnamedMembers = _masterMemberList.Where(m => !m.IsNamed && !m.OriginalShortName.StartsWith(".ctor", StringComparison.OrdinalIgnoreCase)).ToList();

            if (!unnamedMembers.Any())
            {
                _logger.LogDebug("No more unnamed members to include in AI prompt data.");
                return "{}";
            }

            _logger.LogInformation("Preparing data for AI: {Count} unnamed members remaining...", unnamedMembers.Count);

            foreach (var member in unnamedMembers)
            {
                aiInputData[member.UniqueId] = new Dictionary<string, string>
                {
                    { "Type", member.MemberType },
                    // Send its original short name, not the (potentially prefixed) current descriptive name
                    { "DescriptiveName", member.OriginalShortName },
                    // Potentially pass back previous description
                    { "Description", member.Description }
                };
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(aiInputData, Newtonsoft.Json.Formatting.Indented);
        }

        public int UpdateMembersFromAIResponse(string aiResponse, string descriptiveNamePrefix)
        {
            if (string.IsNullOrEmpty(aiResponse))
            {
                _logger.LogWarning("ParseAIResponse: Empty AI response received.");
                return 0;
            }

            string jsonToProcess = JsonHelper.ExtractAndFixJsonFromResponse(aiResponse, _logger);
            int updatedCount = 0;

            if (!string.IsNullOrEmpty(jsonToProcess))
            {
                updatedCount = TryDeserializeAndUpdateMasterList(jsonToProcess, descriptiveNamePrefix);
            }
            else
            {
                _logger.LogWarning("ParseAIResponse: Unable to fix or parse JSON from the AI response using any strategy.");
            }

            return updatedCount;
        }

        private int TryDeserializeAndUpdateMasterList(string jsonString, string prefix)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                _logger.LogWarning("TryDeserializeAndUpdateMasterList: Input JSON string is null or empty.");
                return 0;
            }

            int updatedCount = 0;
            try
            {
                var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonString);
                if (deserialized != null)
                {
                    var memberLookup = _masterMemberList.ToDictionary(m => m.UniqueId);

                    foreach (var kvp in deserialized)
                    {
                        string inboundKeyFromAI = kvp.Key;
                        if (memberLookup.TryGetValue(inboundKeyFromAI, out var matchingMember))
                        {
                            if (kvp.Value != null && kvp.Value.TryGetValue("DescriptiveName", out string descriptiveName) &&
                                !string.IsNullOrEmpty(descriptiveName.Trim()))
                            {
                                string finalDescriptiveName = descriptiveName;
                                if (!finalDescriptiveName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    finalDescriptiveName = prefix + finalDescriptiveName;
                                }

                                string description = "";
                                kvp.Value.TryGetValue("Description", out description);

                                bool hasChanged = matchingMember.CurrentDescriptiveName != finalDescriptiveName;

                                if (hasChanged && !matchingMember.OriginalShortName.StartsWith(".ctor", StringComparison.OrdinalIgnoreCase))
                                {
                                    string oldName = matchingMember.OriginalShortName; // Capture old name for the event

                                    matchingMember.CurrentDescriptiveName = finalDescriptiveName;
                                    matchingMember.Description = description;
                                    matchingMember.IsNamed = true;
                                    updatedCount++;

                                    MemberRenamed?.Invoke(oldName, matchingMember.CurrentDescriptiveName, matchingMember.MemberType, matchingMember.UniqueId);
                                }
                                else if (!matchingMember.IsNamed && !matchingMember.OriginalShortName.StartsWith(".ctor", StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingMember.IsNamed = true;
                                    _logger.LogDebug("AI suggested same name for '{UniqueId}', marked as named. (Original: {Original}, Current: {Current})",
                                        matchingMember.UniqueId, matchingMember.OriginalShortName, matchingMember.CurrentDescriptiveName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during deserialization and member update.");
            }
            return updatedCount;
        }


        public IEnumerable<MemberInfo> GetAllMembers()
        {
            return _masterMemberList;
        }

        public int GetTotalMembersCount()
        {
            return _masterMemberList.Count;
        }

        public int GetNamedMembersCount()
        {
            return _masterMemberList.Count(m => m.IsNamed);
        }
    }
}