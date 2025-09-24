using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// High-level orchestrator for LLM operations with configurable prompts and patterns
    /// </summary>
    [MetaNodeAttribute("codex.core.llm-orchestrator", "codex.meta/type", "LLMOrchestrator", "Orchestrates LLM operations with configurable prompts")]
    public class LLMOrchestrator
    {
        private readonly LLMClient _llmClient;
        private readonly PromptTemplateRepository _promptRepo;
        private readonly Core.ICodexLogger _logger;
        private readonly CancellationTokenSource? _shutdownCts;

        public LLMOrchestrator(LLMClient llmClient, PromptTemplateRepository promptRepo, Core.ICodexLogger logger, CancellationTokenSource? shutdownCts = null)
        {
            _llmClient = llmClient;
            _promptRepo = promptRepo;
            _logger = logger;
            _shutdownCts = shutdownCts;
        }

        /// <summary>
        /// Execute an LLM operation using a configured prompt template
        /// </summary>
        public async Task<LLMOperationResult> ExecuteAsync(string templateId, Dictionary<string, object> parameters, CodexBootstrap.Modules.LLMConfig? overrideConfig = null)
        {
            _logger.Debug($"LLMOrchestrator.ExecuteAsync called with templateId: {templateId}");
            
            // Note: We don't check for shutdown cancellation here to allow AI operations to complete
            // The HTTP client will handle cancellation if needed
            var startTime = DateTimeOffset.UtcNow;
            try
            {
                var template = _promptRepo.GetTemplate(templateId);
                if (template == null)
                {
                    _logger.Error($"Prompt template '{templateId}' not found");
                    return LLMOperationResult.CreateError($"Template '{templateId}' not found");
                }

                var prompt = template.Render(parameters);
                var config = overrideConfig ?? template.DefaultLLMConfig;

                // Route-level AI audit logs
                var providerForLog = string.IsNullOrWhiteSpace(config.Provider) ? "auto" : config.Provider;
                var modelForLog = string.IsNullOrWhiteSpace(config.Model) ? "auto" : config.Model;
                var baseUrlForLog = string.IsNullOrWhiteSpace(config.BaseUrl) ? "(default)" : config.BaseUrl;
                _logger.Info($"AI_PRECALL template={templateId} provider={providerForLog} model={modelForLog} baseUrl={baseUrlForLog} chars={prompt?.Length ?? 0}");

                // Skip availability check during startup - let the actual call fail gracefully if service is unavailable
                // This prevents AI calls during module loading
                _logger.Debug($"Skipping LLM availability check for template '{templateId}' - will check during actual call");

                var response = await _llmClient.QueryAsync(prompt, config, _shutdownCts?.Token ?? CancellationToken.None);
                _logger.Info($"AI_CALL template={templateId} provider={providerForLog} model={modelForLog} baseUrl={baseUrlForLog} success={response.Success} chars={response.Response?.Length ?? 0}");
                var executionTime = DateTimeOffset.UtcNow - startTime;

                return new LLMOperationResult(
                    Success: true,
                    Content: response.Response,
                    Confidence: response.Confidence,
                    TemplateId: templateId,
                    UsedConfig: config,
                    Timestamp: DateTimeOffset.UtcNow,
                    IsFallback: false,
                    Error: null,
                    ExecutionTime: executionTime,
                    Provider: config.Provider,
                    Model: config.Model
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing LLM operation with template '{templateId}': {ex.Message}", ex);
                return LLMOperationResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// Execute an LLM operation and parse to type T
        /// </summary>
        public async Task<T?> ExecuteAsync<T>(string templateId, Dictionary<string, object> parameters, CodexBootstrap.Modules.LLMConfig? overrideConfig = null) where T : class
        {
            var result = await ExecuteAsync(templateId, parameters, overrideConfig);
            var parsed = ParseStructuredResponse<T>(result, templateId);
            if (parsed is T typedResult)
            {
                return typedResult;
            }
            return default;
        }

        /// <summary>
        /// Execute multiple LLM operations in parallel
        /// </summary>
        public async Task<Dictionary<string, LLMOperationResult>> ExecuteParallelAsync(Dictionary<string, (string templateId, Dictionary<string, object> parameters)> operations)
        {
            var tasks = new Dictionary<string, Task<LLMOperationResult>>();

            foreach (var operation in operations)
            {
                tasks[operation.Key] = ExecuteAsync(operation.Value.templateId, operation.Value.parameters);
            }

            var results = new Dictionary<string, LLMOperationResult>();
            foreach (var task in tasks)
            {
                results[task.Key] = await task.Value;
            }

            return results;
        }

        /// <summary>
        /// Sanitize JSON response by removing common formatting issues
        /// </summary>
        private static string SanitizeJsonResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            // Check if this is an error message instead of JSON
            if (json.StartsWith("Error calling LLM:") || 
                json.StartsWith("OpenAI error:") || 
                json.StartsWith("The operation was canceled") ||
                json.StartsWith("LLM unavailable"))
            {
                return json; // Return as-is for error handling
            }

            // Remove code fences
            json = System.Text.RegularExpressions.Regex.Replace(json, @"```(?:json)?\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            json = System.Text.RegularExpressions.Regex.Replace(json, @"```\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove double braces at start/end
            json = json.Trim();
            if (json.StartsWith("{{") && json.EndsWith("}}"))
            {
                json = json.Substring(1, json.Length - 2);
            }

            // Remove extra whitespace and newlines
            json = System.Text.RegularExpressions.Regex.Replace(json, @"\s+", " ");
            json = json.Trim();

            return json;
        }

        /// <summary>
        /// Parse structured response from LLM and return a standardized API response
        /// </summary>
        public object ParseStructuredResponse<T>(LLMOperationResult result, string? operationName = null) where T : class
        {
            _logger.Debug($"ParseStructuredResponse called for type {typeof(T).Name}");
            _logger.Debug($"ParseStructuredResponse: Success={result.Success}, Content='{result.Content}', ContentLength={result.Content?.Length ?? 0}");

            // Handle LLM operation failure
            if (!result.Success)
            {
                var errorMessage = result.Error ?? "Unknown LLM operation error";
                _logger.Error($"LLM operation failed{(operationName != null ? $" for {operationName}" : "")}: {errorMessage}");
                
                // Check if this is a timeout error
                var isTimeoutError = errorMessage.Contains("The operation was canceled") || 
                                   errorMessage.Contains("timeout") || 
                                   errorMessage.Contains("timed out");
                
                return new
                {
                    success = false,
                    error = errorMessage,
                    details = isTimeoutError ? "The LLM operation timed out" : "LLM operation failed",
                    confidence = result.Confidence,
                    isFallback = result.IsFallback,
                    isTimeout = isTimeoutError,
                    timestamp = result.Timestamp,
                    tracking = new
                    {
                        templateId = result.TemplateId,
                        provider = result.Provider,
                        model = result.Model,
                        executionTimeMs = result.ExecutionTime.TotalMilliseconds
                    }
                };
            }

            // Handle empty content
            if (string.IsNullOrEmpty(result.Content))
            {
                _logger.Debug("ParseStructuredResponse: content is empty");
                return new
                {
                    success = false,
                    error = "Empty LLM response",
                    details = "The LLM returned an empty response",
                    confidence = result.Confidence,
                    isFallback = result.IsFallback,
                    timestamp = result.Timestamp,
                    tracking = new
                    {
                        templateId = result.TemplateId,
                        provider = result.Provider,
                        model = result.Model,
                        executionTimeMs = result.ExecutionTime.TotalMilliseconds
                    }
                };
            }

            try
            {
                // Extract JSON from response
                var content = result.Content;
                _logger.Debug($"Raw LLM response: {content}");

                // Try multiple extraction strategies
                string? jsonToParse = null;

                // Determine if we're expecting an array or object based on the type
                bool expectingArray = typeof(T).IsArray || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>));
                _logger.Debug($"JSON extraction: expectingArray = {expectingArray}, type = {typeof(T).Name}");

                if (expectingArray)
                {
                    // Strategy 1: Look for JSON array in markdown code blocks
                    var arrayCodeBlockMatch = System.Text.RegularExpressions.Regex.Match(content, @"```(?:json)?\s*(\[.*?\])\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (arrayCodeBlockMatch.Success)
                    {
                        jsonToParse = arrayCodeBlockMatch.Groups[1].Value.Trim();
                        _logger.Debug($"Extracted JSON array from code block: {jsonToParse}");
                    }
                    else
                    {
                        // Strategy 2: Look for JSON array directly
                        var jsonStart = content.IndexOf('[');
                        var jsonEnd = content.LastIndexOf(']') + 1;

                        if (jsonStart >= 0 && jsonEnd > jsonStart)
                        {
                            jsonToParse = content.Substring(jsonStart, jsonEnd - jsonStart);
                            _logger.Debug($"Extracted JSON array: {jsonToParse}");
                        }
                        else
                        {
                            // Strategy 3: Try parsing the entire content as JSON
                            jsonToParse = content.Trim();
                            _logger.Debug($"Trying to parse entire content as JSON: {jsonToParse}");
                        }
                    }
                }
                else
                {
                    // Strategy 1: Look for JSON object in markdown code blocks
                    var objectCodeBlockMatch = System.Text.RegularExpressions.Regex.Match(content, @"```(?:json)?\s*(\{.*\})\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (objectCodeBlockMatch.Success)
                    {
                        jsonToParse = objectCodeBlockMatch.Groups[1].Value.Trim();
                        _logger.Debug($"Extracted JSON object from code block: {jsonToParse}");
                    }
                    else
                    {
                        // Strategy 2: Look for JSON object directly
                        var objStart = content.IndexOf('{');
                        _logger.Debug($"Object extraction: objStart = {objStart}");
                        if (objStart >= 0)
                        {
                            // Find the matching closing brace by counting braces
                            var braceCount = 0;
                            var objEnd = objStart;
                            for (int i = objStart; i < content.Length; i++)
                            {
                                if (content[i] == '{')
                                    braceCount++;
                                else if (content[i] == '}')
                                {
                                    braceCount--;
                                    if (braceCount == 0)
                                    {
                                        objEnd = i + 1;
                                        break;
                                    }
                                }
                            }

                            _logger.Debug($"Object extraction: objEnd = {objEnd}, braceCount = {braceCount}");
                            if (objEnd > objStart)
                            {
                                jsonToParse = content.Substring(objStart, objEnd - objStart);
                                _logger.Debug($"Extracted JSON object: {jsonToParse}");
                            }
                            else
                            {
                                _logger.Debug("Object extraction failed: objEnd <= objStart");
                            }
                        }
                        else
                        {
                            _logger.Debug("Object extraction failed: no opening brace found");
                        }

                        if (jsonToParse == null)
                        {
                            // Strategy 3: Try parsing the entire content as JSON
                            jsonToParse = content.Trim();
                            _logger.Debug($"Trying to parse entire content as JSON: {jsonToParse}");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(jsonToParse))
                {
                    // Sanitize JSON: remove double braces, code fences, and extra whitespace
                    jsonToParse = SanitizeJsonResponse(jsonToParse);
                    _logger.Debug($"Attempting to deserialize sanitized JSON: {jsonToParse}");

                    // Check if we're expecting an object but got an array
                    if (jsonToParse.TrimStart().StartsWith("[") && !typeof(T).IsArray && !typeof(T).IsGenericType)
                    {
                        _logger.Warn($"Expected object type {typeof(T).Name} but received JSON array. Rejecting response.");
                        return new
                        {
                            success = false,
                            error = $"Expected JSON object but received array",
                            details = $"The LLM returned a JSON array but we expected a {typeof(T).Name} object",
                            confidence = result.Confidence,
                            isFallback = result.IsFallback,
                            timestamp = result.Timestamp,
                            tracking = new
                            {
                                templateId = result.TemplateId,
                                provider = result.Provider,
                                model = result.Model,
                                executionTimeMs = result.ExecutionTime.TotalMilliseconds
                            }
                        };
                    }

                    var deserializedResult = JsonSerializer.Deserialize<T>(jsonToParse);
                    if (deserializedResult == null)
                    {
                        _logger.Warn($"Deserialization returned null for type {typeof(T).Name}");
                        return new
                        {
                            success = false,
                            error = $"Failed to deserialize {typeof(T).Name}",
                            details = "The LLM returned valid JSON but deserialization failed. Check logs for details.",
                            confidence = result.Confidence,
                            isFallback = result.IsFallback,
                            timestamp = result.Timestamp,
                            tracking = new
                            {
                                templateId = result.TemplateId,
                                provider = result.Provider,
                                model = result.Model,
                                executionTimeMs = result.ExecutionTime.TotalMilliseconds
                            }
                        };
                    }
                    else
                    {
                        _logger.Debug($"Successfully deserialized to type {typeof(T).Name}");
                        return new
                        {
                            success = true,
                            data = deserializedResult,
                            confidence = result.Confidence,
                            isFallback = result.IsFallback,
                            timestamp = result.Timestamp,
                            tracking = new
                            {
                                templateId = result.TemplateId,
                                provider = result.Provider,
                                model = result.Model,
                                executionTimeMs = result.ExecutionTime.TotalMilliseconds
                            }
                        };
                    }
                }

                _logger.Warn("No valid JSON found in LLM response");
                return new
                {
                    success = false,
                    error = "No valid JSON found",
                    details = "The LLM response did not contain valid JSON",
                    confidence = result.Confidence,
                    isFallback = result.IsFallback,
                    timestamp = result.Timestamp,
                    tracking = new
                    {
                        templateId = result.TemplateId,
                        provider = result.Provider,
                        model = result.Model,
                        executionTimeMs = result.ExecutionTime.TotalMilliseconds
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing structured response: {ex.Message}", ex);
                _logger.Error($"Raw content that failed to parse: {result.Content}");
                return new
                {
                    success = false,
                    error = "JSON parsing error",
                    details = $"Failed to parse LLM response: {ex.Message}",
                    confidence = result.Confidence,
                    isFallback = result.IsFallback,
                    timestamp = result.Timestamp,
                    tracking = new
                    {
                        templateId = result.TemplateId,
                        provider = result.Provider,
                        model = result.Model,
                        executionTimeMs = result.ExecutionTime.TotalMilliseconds
                    }
                };
            }
        }
    }

    /// <summary>
    /// Result of an LLM operation
    /// </summary>
    [ResponseType("codex.core.llm-operation-result", "LLMOperationResult", "Result of an LLM operation")]
    public record LLMOperationResult(
        bool Success,
        string Content,
        double Confidence,
        string TemplateId,
        CodexBootstrap.Modules.LLMConfig UsedConfig,
        DateTimeOffset Timestamp,
        bool IsFallback,
        string? Error,
        TimeSpan ExecutionTime,
        string Provider,
        string Model
    )
    {
        public static LLMOperationResult CreateError(string error) => new(
            Success: false,
            Content: "",
            Confidence: 0.0,
            TemplateId: "",
            UsedConfig: new("", "", "", "", "", "", 0, 0, 0, new()),
            Timestamp: DateTimeOffset.UtcNow,
            IsFallback: false,
            Error: error,
            ExecutionTime: TimeSpan.Zero,
            Provider: "",
            Model: ""
        );

        public static LLMOperationResult CreateFallback(string message, Dictionary<string, object> parameters) => new(
            Success: true,
            Content: $"Fallback response: {message}",
            Confidence: 0.5,
            TemplateId: "",
            UsedConfig: new("", "", "", "", "", "", 0, 0, 0, new()),
            Timestamp: DateTimeOffset.UtcNow,
            IsFallback: true,
            Error: null,
            ExecutionTime: TimeSpan.Zero,
            Provider: "fallback",
            Model: "none"
        );
    }
}
