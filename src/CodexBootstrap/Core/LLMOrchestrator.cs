using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// High-level orchestrator for LLM operations with configurable prompts and patterns
    /// </summary>
    public class LLMOrchestrator
    {
        private readonly LLMClient _llmClient;
        private readonly PromptTemplateRepository _promptRepo;
        private readonly ILogger _logger;

        public LLMOrchestrator(LLMClient llmClient, PromptTemplateRepository promptRepo, ILogger logger)
        {
            _llmClient = llmClient;
            _promptRepo = promptRepo;
            _logger = logger;
        }

        /// <summary>
        /// Execute an LLM operation using a configured prompt template
        /// </summary>
        public async Task<LLMOperationResult> ExecuteAsync(string templateId, Dictionary<string, object> parameters, CodexBootstrap.Modules.LLMConfig? overrideConfig = null)
        {
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

                // Check if LLM is available
                var isAvailable = await _llmClient.IsServiceAvailableAsync();
                if (!isAvailable)
                {
                    _logger.Warn($"LLM service not available for template '{templateId}', using fallback");
                    return LLMOperationResult.CreateFallback($"LLM service unavailable for template '{templateId}'", parameters);
                }

                var response = await _llmClient.QueryAsync(prompt, config);
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
        /// Parse structured response from LLM
        /// </summary>
        public T? ParseStructuredResponse<T>(LLMOperationResult result) where T : class
        {
            if (!result.Success || string.IsNullOrEmpty(result.Content))
                return null;

            try
            {
                // Extract JSON from response
                var content = result.Content;
                var jsonStart = content.IndexOf('[');
                var jsonEnd = content.LastIndexOf(']') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = content.Substring(jsonStart, jsonEnd - jsonStart);
                    return JsonSerializer.Deserialize<T>(json);
                }

                // Try parsing the entire content as JSON
                return JsonSerializer.Deserialize<T>(content);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing structured response: {ex.Message}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Result of an LLM operation
    /// </summary>
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
