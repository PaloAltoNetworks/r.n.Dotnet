# r.n.Dotnet: AI-Powered .NET Assembly Renamer and Analyzer

`r.n.Dotnet` is a .NET console application designed to assist in reverse engineering and analyzing obfuscated .NET assemblies. It leverages Vertex AI to infer functionality from decompiled code and suggest descriptive names for obfuscated namespaces, classes, methods, and fields. It can then apply these suggested renames and generate a high-level summary of the assembly's likely behavior.

This tool aims to make analyzing highly obfuscated .NET malware samples faster and easier by replacing meaningless generated names (like `a.a`, `b.a.a`, `_0x12345678`) with potentially more understandable ones based on AI context analysis.

### Disclaimers: The tool should be run inside a safe Virutal Machine.

## Features

*   **Assembly Loading & Decompilation:** Uses `dnlib` and `ICSharpCode.Decompiler` to load and decompile .NET assemblies into readable C# code.
*   **Member Identification:** Scans the assembly to identify obfuscated namespaces, classes, methods, and fields that can be renamed.
*   **Iterative AI Renaming:** Communicates with a configured Vertex AI model to suggest new, descriptive names and descriptions for identified members based on the decompiled code context. Can perform multiple passes for refinement.
*   **Apply Renames:** Modified assembly is saved with the new names applied using `dnlib`.
*   **Malware Summary Generation:** Optionally generates a summary report of the assembly's potential functionality and IoCs by sending the decompiled code to a separate Vertex AI model.
*   **SHA256 Hashing:** Calculates and includes the SHA256 hash of the original assembly in the output filename for identification.
*   **Configurable:** Uses `appsettings.json` for application and AI configurations.
*   **Environment Variable Authentication:** Supports standard Google Cloud authentication mechanisms, commonly via the `GOOGLE_APPLICATION_CREDENTIALS` environment variable.

## Prerequisites

Before running `r.n.Dotnet`, you need:

1.  **.NET SDK:** .NET 6.0 or later installed on your machine.
2.  **Google Cloud Project:** Access to a Google Cloud project.
3.  **Vertex AI API:** The Vertex AI API must be enabled in your Google Cloud project.
4.  **Vertex AI Models:**
    *   Make sure you have access to the specific models you intend to use for renaming and summarization (e.g., `gemini-1.5-flash-preview-0514`, `gemini-1.5-pro-preview-0514`, or others) in the chosen Google Cloud region. These models should be specified correctly in your configuration.
5.  **Authentication:** Set up credentials to authenticate with Google Cloud (details below).
6.  **A .NET Assembly:** A .NET assembly file (`.exe` or `.dll`) that you want to analyze and rename.

## Setup

### Option One:
1. Install Visual Studio community edition
2. Open the solution file once Visual Studio is installed
3. Build the solution or run shortcut "CTRL+SHIFT+B"
4. Navigate to directory "rnDotnet\bin\Debug\net8.0" and run command terminal
5. Execute rnDotnet.exe

### Option Two:
1.  **Clone the Repository:**
    ```bash
    git clone <repository_url> # Replace <repository_url> with the actual URL
    cd rnDotnet
    ```
2.  **Restore Dependencies:**
    ```bash
    dotnet restore
    ```
3.  **Build the Project:**
    ```bash
    dotnet build --configuration Release
    ```
    Or, publish for a self-contained executable (recommended):
    ```bash
    dotnet publish --configuration Release --self-contained -p:PublishSingleFile=true -r <your_runtime_identifier> # e.g., win-x64
    ```
    Replace `<your_runtime_identifier>` with your target platform (e.g., `win-x64`, `linux-x64`, `osx-x64`). The output will be in `bin/Release/<target_framework>/<runtime_identifier>/publish/`.

## Configuration (`appsettings.json`)

The application uses an `appsettings.json` file for configuration. You **must** create this file in the same directory as the compiled executable or in the project root if running with `dotnet run`.

Here is an example `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning", // Adjust logging level as needed (Trace, Debug, Information, Warning, Error, Critical)
      "Microsoft": "Warning",   // Reduce noise from Microsoft libraries
      "Microsoft.Hosting.Lifetime": "Warning"
    }
  },
  "Application": {
    "DescriptiveNamePrefix": "llm_" // Prefix for names suggested by the AI
  },
  "AI": {
    "ProjectId": "your-gcp-project-id", // Your Google Cloud Project ID (required)
    "Location": "your-location",          // The Google Cloud region your models are deployed in (required)
    "Publisher": "google",              // The model publisher (usually 'google') (required)
    "RenamingModel": "gemini-1.5-flash-preview-0514", // Model name for renaming (required)
    "SummaryModel": "gemini-1.5-pro-preview-0514"       // Model name for summary (required)
  }
}
```
* Logging: Standard .NET logging options. Adjust Default to Debug or Trace for more verbose output during troubleshooting.
* Application:DescriptiveNamePrefix: This prefix is added to the names suggested by the AI (if they aren't already prefixed or similar). This helps distinguish AI-generated names from original or manually renamed ones.
* AI:
  * ProjectId: Your Google Cloud project ID (required).
  * Location: The GCP region where the Vertex AI models are available (e.g., us-central1, asia-east1) (required).
  * Publisher: Always google for public models (required).
  * RenamingModel: The ID of the specific model to use for analyzing code structure and suggesting names (required).
  * SummaryModel: The ID of the specific model to use for generating the overall malware summary report (required).

Ensure the AI section values are correct for your GCP setup.

# Vertex AI Authentication
The Google.Cloud.AIPlatform.V1 library automatically handles authentication using Google's standard methods. The most common way to authenticate a local application is by setting the GOOGLE_APPLICATION_CREDENTIALS environment variable.

* Create a Service Account:
* Go to the Google Cloud Console.
* Navigate to "IAM & Admin" -> "Service Accounts".
* Click "Create Service Account".
* Give it a name and description (e.g., rn-dotnet-vertex).
* Grant it the Vertex AI User or Vertex AI Predictor role.
* Continue and click "Done".
* Generate a JSON Key:
* Find your newly created service account in the list.
* Click on the service account name.
* Go to the "Keys" tab.
* Click "Add Key" -> "Create new key".
* Select "JSON" and click "Create".
* A JSON file will be downloaded to your computer. Keep this file secure.

# Set the Environment Variable:
* Set the GOOGLE_APPLICATION_CREDENTIALS environment variable to the full path of the downloaded JSON key file.

```text
Windows (Command Prompt):
set GOOGLE_APPLICATION_CREDENTIALS="C:\Path\To\Your\key.json"

Windows (PowerShell):
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Path\To\Your\key.json"
```

```text
Linux/macOS (Bash/Zsh):
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your/key.json"
```

# Running the Application
Ensure Prerequisites and Authentication: Make sure you have .NET SDK, appsettings.json configured correctly, and the GOOGLE_APPLICATION_CREDENTIALS environment variable set (or appropriate alternative authentication is in place).

Navigate to the Build/Publish Directory: Go to the directory where your executable or published files are located.

Run the Application:

If you used dotnet build:
```text
dotnet run --project <path_to_rnDotnet.csproj>
```

Make sure your appsettings.json is in the output directory (e.g., bin/Debug/net6.0 or bin/Release/net6.0). A common approach is to set the build properties in the .csproj to copy appsettings.json to the output directory.

If you used dotnet publish:
```text
./rnDotnet  # On Linux/macOS
rnDotnet.exe # On Windows
```

* Make sure your appsettings.json is in the same directory as the executable.
* Follow Console Prompts: The application will start and interactively prompt you:

1. Enter the path to the .NET assembly (.exe or .dll) you want to analyze. You can drag and drop the file onto the console in some terminals, which might include quotes - the application attempts to handle these.
2. Confirm or provide a custom prefix for the descriptive names.
3. Enter the maximum number of AI refinement passes. The tool will suggest a number based on the total members found, but you can override this. Fewer passes are faster and cheaper but may result in fewer members being renamed.
4. (After renaming is complete) Confirm if you want to generate a malware summary report.
5. The application will then decompile the assembly, communicate with Vertex AI in iterations to get renaming suggestions, apply the renames, save the modified assembly, and optionally generate and save the summary report.

# Output
* Console Output: Provides progress updates, identified members count, AI communication logs, and success/failure messages.
* Renamed Assembly: A new file will be created in the same directory as the original assembly with a name like <original_name>_renamed_<sha256_prefix>_pass<pass_count>.<ext>.
* Malware Summary Report: If you opted to generate it, a text file will be created in the same directory as the original assembly named <original_name>_summary.txt.

# Troubleshooting
* appsettings.json Not Found: Ensure the appsettings.json file is present in the directory where you are running the executable (dotnet run or the published folder). Check the build/publish output directory.
* Authentication Errors (e.g., "Permission denied", "Could not load credentials"):
  * Double-check that the GOOGLE_APPLICATION_CREDENTIALS environment variable is set correctly to the full path of your service account JSON key file in the terminal you are using.
  * Verify the service account has the correct Vertex AI User or Vertex AI Predictor role in your Google Cloud project.
  * Ensure your Google Cloud project ID and Vertex AI region (ProjectId and Location in appsettings.json) are correct.
* AI Errors (e.g., "Model not found", "Quota exceeded", RpcException):
  * Verify the model names (RenamingModel, SummaryModel) in appsettings.json are correct and exist in your specified Location under the given Publisher ('google').
  * Check your Google Cloud project's quotas for Vertex AI. You might need to request an increase.
  * Look at the log output (especially if set to DEBUG or TRACE in appsettings.json) for the specific RpcException details, which often indicate the underlying Google Cloud error.
* JSON Parsing Errors from AI Response: The JsonHelper attempts basic recovery, but large responses or highly malformed JSON from the AI can still cause failures. Setting the logging level to DEBUG or TRACE will show the problematic JSON snippet received from the AI. If this is a persistent issue, it might indicate a problem with the prompt interacting with the specific model, or a limitation of the parsing logic.
* Decompilation Errors: Some obfuscation techniques can make decompilation difficult or impossible with the chosen tools. Look at the log output for details from ICSharpCode.Decompiler or dnlib (often logged with ERROR). This might be a limitation of the tool for certain samples.
* Namespace Renaming: The DnlibAssemblyRenamer applies namespace renames by modifying the Namespace property of types. Complex namespace structures or heavy obfuscation might result in unexpected behavior or incomplete renaming.

# Contributing
Contributions are welcome! Please consult the CONTRIBUTING.md file for guidelines (if applicable, otherwise you might create one). You can also open an issue or submit a pull request directly.