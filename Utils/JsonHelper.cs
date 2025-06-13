using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace rnDotnet.Utils
{
    public static class JsonHelper
    {
        public const int MAX_LOG_SNIPPET_LENGTH = 150;

        public static string ExtractAndFixJsonFromResponse(string aiResponse, ILogger logger)
        {
            if (string.IsNullOrEmpty(aiResponse))
            {
                // logger.LogWarning("ExtractAndFixJsonFromResponse: Input AI response is null or empty."); // Handled by caller
                return null;
            }

            string jsonToProcess = aiResponse.Trim();
            string markdownJsonPrefix = "```json";

            // Try to extract from a markdown block first
            string extracted = ExtractJsonStringFromMarkdown(jsonToProcess, logger);
            if (!string.IsNullOrEmpty(extracted))
            {
                jsonToProcess = extracted;
                logger.LogDebug("Successfully extracted JSON from a complete Markdown block.");
            }
            else
            {
                // If not in a full markdown block, then attempt to find the actual JSON object
                int firstBraceIndex = jsonToProcess.IndexOf('{');
                if (firstBraceIndex != -1)
                {
                    jsonToProcess = jsonToProcess.Substring(firstBraceIndex);
                    // Trim off everything after the last '}' assuming everything else is garbage
                    int lastBraceIndex = jsonToProcess.LastIndexOf('}');
                    if (lastBraceIndex != -1)
                    {
                        jsonToProcess = jsonToProcess.Substring(0, lastBraceIndex + 1);
                    }
                    logger.LogDebug("Parsed response assuming raw JSON. Snippet: \"{JsonSnippet}\"",
                        jsonToProcess.Substring(0, Math.Min(jsonToProcess.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");
                }
                else
                {
                    logger.LogWarning("AI response did not contain a recognizable JSON object starting with '{{'. Response: {ResponseSnippet}",
                        aiResponse.Substring(0, Math.Min(aiResponse.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");
                    return null; // No JSON to even attempt fixing
                }
            }


            return FixTruncatedJson(jsonToProcess, logger);
        }

        private static string FixTruncatedJson(string jsonString, ILogger logger)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                logger.LogWarning("FixTruncatedJson: Input string is null or empty.");
                return null;
            }

            string cleanedJson = jsonString.Trim();
            // logger.LogDebug("FixTruncatedJson: Attempting to fix: \"{JsonSnippet}\"", cleanedJson.Substring(0, Math.Min(cleanedJson.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");

            // Strategy 1: Attempt direct parse
            try
            {
                JObject.Parse(cleanedJson);
                logger.LogDebug("FixTruncatedJson: Direct parse successful. JSON is valid.");
                return cleanedJson;
            }
            catch (JsonReaderException)
            {
                logger.LogDebug("FixTruncatedJson: Direct parse failed. Proceeding to targeted fix.");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "FixTruncatedJson: Direct parse failed unexpectedly: {Message}. Proceeding to targeted fix.", ex.Message);
            }

            // Strategy 2: User's specific fix - Find the last "}," and replace comma (and subsequent text) with "}"
            int lastValidEntryEndMarkerIndex = cleanedJson.LastIndexOf("},");

            if (lastValidEntryEndMarkerIndex != -1)
            {
                string potentialJson = cleanedJson.Substring(0, lastValidEntryEndMarkerIndex + 1);
                potentialJson += "}"; // Append the final '}' to close the main JSON object

                logger.LogDebug("FixTruncatedJson: Applying '}},' specific fix. Candidate: \"{JsonSnippet}\"",
                    potentialJson.Substring(0, Math.Min(potentialJson.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");
                try
                {
                    // Crucially, ensure the potentialJson itself starts with an opening brace.
                    if (!potentialJson.TrimStart().StartsWith("{"))
                    {
                        logger.LogDebug("FixTruncatedJson: Candidate after '}},' fix does not start with '{{'. Attempting to find first '{{'.");
                        int firstBrace = potentialJson.IndexOf('{');
                        if (firstBrace != -1)
                        {
                            potentialJson = potentialJson.Substring(firstBrace);
                        }
                        else
                        {
                            logger.LogDebug("FixTruncatedJson: Candidate after '}},' fix has no '{{'. Fix failed.");
                            throw new JsonSerializationException("Constructed JSON for '}},' fix does not start with an opening brace.");
                        }
                    }

                    JObject.Parse(potentialJson);
                    logger.LogDebug("FixTruncatedJson: User's specific '}},' fix successful.");
                    return potentialJson;
                }
                catch (JsonSerializationException ex) // Changed from JsonReaderException as we're constructing string here
                {
                    logger.LogDebug(ex, "FixTruncatedJson: User's specific '}},' fix failed: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "FixTruncatedJson: User's specific '}},' fix failed unexpectedly: {Message}", ex.Message);
                }
            }
            else
            {
                logger.LogDebug("FixTruncatedJson: Pattern '}},' not found for the specific fix strategy.");
            }

            // Strategy 3: Minimal Fallback - If it starts with '{' but doesn't end with '}', just append '}'
            if (cleanedJson.StartsWith("{") && !cleanedJson.EndsWith("}"))
            {
                string potentialJson = cleanedJson + "}";
                logger.LogDebug("FixTruncatedJson: Applying simple fallback: adding trailing '}}'. Candidate: \"{JsonSnippet}\"",
                    potentialJson.Substring(0, Math.Min(potentialJson.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");
                try
                {
                    JObject.Parse(potentialJson);
                    logger.LogDebug("FixTruncatedJson: Simple fallback (adding trailing '}') fix successful.");
                    return potentialJson;
                }
                catch (JsonReaderException ex)
                {
                    logger.LogDebug(ex, "FixTruncatedJson: Simple fallback (adding trailing '}}') fix failed: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "FixTruncatedJson: Simple fallback (adding trailing '}}') fix failed unexpectedly: {Message}", ex.Message);
                }
            }

            logger.LogWarning("FixTruncatedJson: All targeted fix strategies failed for JSON snippet: {JsonSnippet}",
                cleanedJson.Substring(0, Math.Min(cleanedJson.Length, MAX_LOG_SNIPPET_LENGTH)) + "...");
            return null;
        }

        internal static string ExtractJsonStringFromMarkdown(string response, ILogger logger)
        {
            // This regex specifically looks for a code block optionally marked with "json"
            // and captures anything inside the braces.
            var jsonMatch = Regex.Match(response, @"```(?:json)?\s*(?<json>{.*?})\s*```", RegexOptions.Singleline | RegexOptions.ExplicitCapture);
            if (jsonMatch.Success)
            {
                string jsonText = jsonMatch.Groups["json"].Value;
                if (jsonText.TrimStart().StartsWith("{"))
                {
                    logger.LogDebug("ExtractJsonStringFromMarkdown: Successfully extracted JSON string from Markdown block.");
                    return jsonText;
                }
                logger.LogDebug("ExtractJsonStringFromMarkdown: Matched Markdown block, but extracted content doesn't start with '{{'.");
            }
            return null;
        }
    }
}
