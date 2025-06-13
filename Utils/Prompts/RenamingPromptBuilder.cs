namespace rnDotnet.Utils.Prompts
{
    public static class RenamingPromptBuilder
    {
        public static string BuildPrompt(string systemInstruction, string decompiledCode, string enumerationResults, int passCount)
        {
            // Prepend the chosen system instruction to the detailed task description
            return $@"{systemInstruction}
    
    Goal:
    You are an expert in .NET malware analysis and reverse engineering. Your task is to analyze obfuscated .NET malware and systematically identify specific .NET namespaces, classes, methods, and fields.
    Assign clear, descriptive names to obfuscated elements based on their functionality, as inferred from their references to .NET libraries, namespaces, classes, methods, objects, properties, and interfaces.

    Use the following traversal methods for analyzing the relationships of coding constructs:

    * Preorder Traversal (Top-Down DFS): Detecting malicious imports, Identifying code structure, Extracting API calls early.
    * Postorder Traversal (Bottom-Up DFS): Evaluating decryption routines, Tracking string building techniques, Analyzing function outputs.
    * Inorder Traversal: Extracting conditions in statements that control execution flow, Reconstructing arithmetic encoding tricks.
    * Breadth-First Traversal: Detecting nested execution, Unpacking layered obfuscation, Identifying code injection points by mapping parent-child relationships.
    * Reverse Postorder Traversal: Analyzing malware loops and recursion, Understanding execution order, Detecting anti-analysis techniques.

    Input:
    Decompiled code:
    ```csharp
    {decompiledCode}
    ```
    List of obfuscated elements that *still need renaming*:
    ```json
    {enumerationResults}
    ```
    This is pass number: {passCount + 1}. Focus on the elements that currently have their obfuscated names, as provided in the 'List of obfuscated elements' section. Do not attempt to rename elements that already have descriptive names (i.e., those whose 'DescriptiveName' value *already* starts with your prefix or has a meaningful name, rather than an obfuscated one).

    Guidelines for Renaming:
    1. Analyze Functionality:
        - Focus on the presence of .NET library references and interactions in the code.
        - Infer the intent of each obfuscated element based on:
          - Method calls
          - Namespace and class references
          - Object instantiations
          - Interface implementations

    2. Assign Descriptive Names:
        - Clearly reflect the inferred functionality or intent of the element.
        - Use .NET naming conventions (e.g., PascalCase for classes/methods, camelCase for fields; ensure consistency).
        - Provide meaningful names, even if they are the best guess based on available context.

    IMPORTANT: The keys in the JSON input (e.g., `MyNamespace.MyClass`, `MyNamespace.MyClass::MyMethod(System.String)`, `MyNamespace.MyClass::myField`) are the **unique identifiers** for these members. **You must return these exact unique identifiers as the keys in your JSON output.** The `DescriptiveName` value should be the new **short name** you propose (e.g., `NetworkClient` for a class, `AuthenticateUser` for a method, `EncryptionKey` for a field). Your proposed names should be concise and descriptive.

    Malware Behaviors to Consider:
    - **Entry Point**: Look for a function that invokes `Application.Run` (common in malware's main entry point).
    - **Dynamic Assembly Resolution**: References to `System.Reflection` namespace and `Assembly` class suggest process injection or in-memory loading of another file.
    - **File Operations**: Look for references to `System.IO` namespace and classes like `BinaryReader` or `BinaryWriter` for file reading, writing, or deletion.
    - **Network Communication**: References to `System.Net.Http` namespace and `HttpClient` class indicate HTTP requests or data transfer over a network.
    - **Registry Manipulation**: References to `Microsoft.Win32` namespace and `Registry` classes suggests registry operations.
    - **Process Management**: Functions interacting with `System.Diagnostics.Process` likely handle process creation, termination, or manipulation.
    - **Data Encoding/Decoding**: References to `System.Convert` for base64 encoding/decoding and other data transformations.
    - **Data Encryption/Decryption**: References to `System.Security.Cryptography` or classes like `Aes`, `ICryptoTransform`, and use of keys (Key/IV) indicate cryptographic operations.
    - **Data Manipulation**: Functions using `MemoryStream`, `System.Byte`, or XOR operations `^`, with arrays suggest byte-level data transformations.
    - **Reconnaissance**: References to `System.Management` indicate WMI queries or reconnaissance activities.
    - **Anti-Analysis**: Use of `System.Threading.Thread.Sleep` or obfuscation techniques suggests anti-debugging or anti-analysis behavior.
    - **Control Flow Handler**: Use of case statements indicates a handler for the program's decision making.

    Output:
    Return the results in the following valid JSON format. The output must be well-formed, without any additional commentary or invalid characters.
    IMPORTANT: Any invalid JSON will cause the program to fail. Triple-check your output. The keys in the JSON must be the *exact unique identifiers* provided in the 'List of obfuscated elements' input section.

    Example JSON Output (note the keys are full identifiers, values for DescriptiveName are short names):
    ```json
    {{
     ""MyObfuscatedNamespace.ClassA"": {{
       ""DescriptiveName"": ""NetworkClient"",
       ""Type"": ""Class"",
       ""Description"": ""Manages network communication with C2 server, including encryption and decryption.""
     }},
     ""MyObfuscatedNamespace.ClassA::MethodB(System.String)"": {{
       ""DescriptiveName"": ""AuthenticateUser"",
       ""Type"": ""Method"",
       ""Description"": ""Authenticates a user against a remote server using provided credentials.""
     }},
     ""MyObfuscatedNamespace.ClassA::FieldC"": {{
       ""DescriptiveName"": ""EncryptionKey"",
       ""Type"": ""Field"",
       ""Description"": ""Stores the AES encryption key for C2 communication.""
     }},
     ""MyObfuscatedNamespace"": {{
       ""DescriptiveName"": ""C2_Communications"",
       ""Type"": ""Namespace"",
       ""Description"": ""Contains logic for command and control communications.""
     }}
    }}
    ```";
        }
    }
}