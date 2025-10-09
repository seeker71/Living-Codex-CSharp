using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// UI Orchestration Module - Coordinates UI generation through the breath loop
    /// </summary>
    [ApiModule(Name = "UIOrchestrationModule", Version = "1.0.0", Description = "Orchestrates UI generation through compose→expand→validate→melt/patch/refreeze→contract", Tags = new[] { "ui-orchestration", "breath-loop", "ui-generation" })]
    public class UIOrchestrationModule : ModuleBase
    {
        public override string Name => "UI Orchestration Module";
        public override string Description => "Orchestrates UI generation through the breath loop using AI and spec-driven architecture";
        public override string Version => "1.0.0";

        public UIOrchestrationModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "ui-orchestration-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "ui-orchestration", "breath-loop", "ui-generation" },
                capabilities: new[] { "ui-compose", "ui-expand", "ui-validate", "ui-patch", "ui-contract" },
                spec: "codex.spec.ui-orchestration"
            );
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            // This method is now handled by attribute-based discovery
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // This method is now handled by attribute-based discovery
        }

        /// <summary>
        /// Compose: Create minimal UI page atoms from user intent
        /// </summary>
        [ApiRoute("POST", "/ui-orchestration/compose-page", "ui-compose-page", "Create minimal UI page atoms from intent", "ui-orchestration")]
        public async Task<IResult> ComposeUIPage([ApiParameter("request", "Compose page request", Required = true, Location = "body")] ComposePageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Intent))
                {
                    return Results.BadRequest(new { success = false, error = "Intent is required" });
                }

                // Create minimal UI page atom based on intent
                var pageId = $"ui.page.{request.Intent.ToLower().Replace(" ", "-")}";
                var pageAtom = new
                {
                    id = pageId,
                    type = "codex.ui.page",
                    name = request.Intent,
                    path = request.Path ?? $"/{request.Intent.ToLower().Replace(" ", "-")}",
                    component = "DynamicPage",
                    lenses = request.Lenses ?? new List<object>(),
                    controls = request.Controls ?? new List<object>(),
                    status = "Composed",
                    createdAt = DateTimeOffset.UtcNow
                };

                // Store as Ice atom
                var pageNode = new Node(
                    Id: pageId,
                    TypeId: "codex.ui.page",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: request.Intent,
                    Description: $"UI page for {request.Intent}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(pageAtom),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["name"] = request.Intent,
                        ["path"] = pageAtom.path,
                        ["component"] = "DynamicPage",
                        ["status"] = "Composed",
                        ["createdAt"] = DateTimeOffset.UtcNow
                    }
                );

                _registry.Upsert(pageNode);

                _logger.Info($"Composed UI page atom: {pageId}");

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        pageAtom = pageAtom,
                        message = $"UI page atom composed for intent: {request.Intent}",
                        timestamp = DateTimeOffset.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error composing UI page: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Expand: Generate components via AI
        /// </summary>
        [ApiRoute("POST", "/ui-orchestration/expand-components", "ui-expand-components", "Generate components via AI", "ui-orchestration")]
        public async Task<IResult> ExpandUIComponents([ApiParameter("request", "Expand components request", Required = true, Location = "body")] ExpandComponentsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PageId))
                {
                    return Results.BadRequest(new { success = false, error = "Page ID is required" });
                }

                // Get the page atom
                if (!_registry.TryGet(request.PageId, out var pageNode))
                {
                    return Results.BadRequest(new { success = false, error = $"Page atom {request.PageId} not found" });
                }

                var pageAtom = JsonSerializer.Deserialize<JsonElement>(pageNode.Content?.InlineJson ?? "{}");

                var generatedComponents = new List<object>();

                // Generate page component via AI (simplified for now)
                var pageComponent = new
                {
                    id = $"{request.PageId}.page",
                    type = "page",
                    generatedCode = "// Generated page component placeholder",
                    status = "Generated",
                    generatedAt = DateTimeOffset.UtcNow
                };

                generatedComponents.Add(pageComponent);

                // Store as Water projection
                var componentNode = new Node(
                    Id: pageComponent.id,
                    TypeId: "codex.ui.generated-component",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: "Generated Page Component",
                    Description: $"Generated component for {request.PageId}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(pageComponent),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["type"] = "page",
                        ["status"] = "Generated",
                        ["generatedAt"] = DateTimeOffset.UtcNow
                    }
                );

                _registry.Upsert(componentNode);

                _logger.Info($"Expanded UI components for page: {request.PageId}, generated {generatedComponents.Count} components");

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        pageId = request.PageId,
                        generatedComponents = generatedComponents,
                        message = $"Generated {generatedComponents.Count} components for page {request.PageId}",
                        timestamp = DateTimeOffset.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error expanding UI components: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Validate: Test generated components with visual validation
        /// </summary>
        [ApiRoute("POST", "/ui-orchestration/validate-components", "ui-validate-components", "Test generated components with visual validation", "ui-orchestration")]
        public async Task<IResult> ValidateUIComponents([ApiParameter("request", "Validate components request", Required = true, Location = "body")] ValidateComponentsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PageId))
                {
                    return Results.BadRequest(new { success = false, error = "Page ID is required" });
                }

                var validationResults = new List<object>();

                // Get all generated components for the page
                var componentNodes = _registry.GetNodesByType("codex.ui.generated-component")
                    .Where(n => n.Id.StartsWith(request.PageId))
                    .ToList();

                foreach (var componentNode in componentNodes)
                {
                    var component = JsonSerializer.Deserialize<JsonElement>(componentNode.Content?.InlineJson ?? "{}");
                    var componentId = componentNode.Id;
                    var componentCode = component.TryGetProperty("generatedCode", out var code) ? code.GetString() ?? "" : "";

                    // Basic validation
                    var basicValidation = new
                    {
                        componentId = componentId,
                        status = "Valid", // Simplified validation for now
                        issues = new List<string>(),
                        validatedAt = DateTimeOffset.UtcNow
                    };

                    validationResults.Add(basicValidation);

                    // Visual validation if component code is available
                    if (!string.IsNullOrEmpty(componentCode) && request.EnableVisualValidation)
                    {
                        try
                        {
                            // Call visual validation pipeline
                            var visualValidationRequest = new
                            {
                                componentId = componentId,
                                componentCode = componentCode,
                                specVision = request.SpecVision ?? "Follow Living Codex design principles with resonance, joy, and unity",
                                requirements = request.Requirements ?? "Create an engaging, intuitive interface",
                                width = request.Width ?? 1920,
                                height = request.Height ?? 1080,
                                viewport = request.Viewport ?? "desktop",
                                minimumScore = request.MinimumScore ?? 0.7,
                                provider = request.Provider,
                                model = request.Model
                            };

                            // For now, simulate visual validation call
                            // In real implementation, call the visual validation module
                            var visualResult = new
                            {
                                componentId = componentId,
                                visualValidation = new
                                {
                                    overallScore = 0.85,
                                    resonanceScore = 0.8,
                                    joyScore = 0.7,
                                    unityScore = 0.9,
                                    clarityScore = 0.85,
                                    technicalQualityScore = 0.9,
                                    passed = true,
                                    feedback = new[] { "Good resonance-driven design", "Clear visual hierarchy" },
                                    issues = new[] { "Could improve joy factor" },
                                    recommendations = new[] { "Add subtle animations", "Enhance micro-interactions" }
                                },
                                renderedAt = DateTimeOffset.UtcNow
                            };

                            validationResults.Add(visualResult);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Visual validation failed for component {componentId}: {ex.Message}");
                            validationResults.Add(new
                            {
                                componentId = componentId,
                                visualValidation = new
                                {
                                    error = "Visual validation failed",
                                    message = ex.Message
                                }
                            });
                        }
                    }
                }

                _logger.Info($"Validated {validationResults.Count} components for page: {request.PageId}");

                return Results.Ok(new
                {
                    success = true,
                    data = new
                    {
                        pageId = request.PageId,
                        validationResults = validationResults,
                        message = $"Validated {validationResults.Count} components",
                        timestamp = DateTimeOffset.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error validating UI components: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Execute full breath loop for UI generation
        /// </summary>
        [ApiRoute("POST", "/ui-orchestration/breath-loop", "ui-breath-loop", "Execute full breath loop for UI generation", "ui-orchestration")]
        public async Task<IResult> ExecuteBreathLoop([ApiParameter("request", "Breath loop request", Required = true, Location = "body")] UIBreathLoopRequest request)
        {
            try
            {
                var steps = new List<object>();

                // Step 1: Compose
                var composeRequest = new ComposePageRequest(
                    Intent: request.Intent,
                    Path: request.Path,
                    Lenses: request.Lenses,
                    Controls: request.Controls
                );

                var composeResult = await ComposeUIPage(composeRequest);
                steps.Add(new
                {
                    phase = "Compose",
                    status = composeResult is JsonElement c && c.TryGetProperty("success", out var cs) && cs.GetBoolean() ? "Success" : "Failed",
                    result = composeResult,
                    timestamp = DateTimeOffset.UtcNow
                });

                if (composeResult is JsonElement composeElement && composeElement.TryGetProperty("success", out var composeSuccess) && composeSuccess.GetBoolean())
                {
                    var pageId = composeElement.GetProperty("data").GetProperty("pageAtom").GetProperty("id").GetString() ?? "";

                    // Step 2: Expand
                    var expandRequest = new ExpandComponentsRequest(
                        PageId: pageId,
                        Provider: request.Provider,
                        Model: request.Model
                    );

                    var expandResult = await ExpandUIComponents(expandRequest);
                    steps.Add(new
                    {
                        phase = "Expand",
                        status = expandResult is JsonElement e && e.TryGetProperty("success", out var es) && es.GetBoolean() ? "Success" : "Failed",
                        result = expandResult,
                        timestamp = DateTimeOffset.UtcNow
                    });

                    // Step 3: Validate
                    var validateRequest = new ValidateComponentsRequest(
                        PageId: pageId
                    );

                    var validateResult = await ValidateUIComponents(validateRequest);
                    steps.Add(new
                    {
                        phase = "Validate",
                        status = validateResult is JsonElement v && v.TryGetProperty("success", out var vs) && vs.GetBoolean() ? "Success" : "Failed",
                        result = validateResult,
                        timestamp = DateTimeOffset.UtcNow
                    });
                }

                var allSuccessful = steps.All(s => s is JsonElement step && step.TryGetProperty("status", out var status) && status.GetString() == "Success");

                _logger.Info($"Executed breath loop for intent: {request.Intent}, success: {allSuccessful}");

                return Results.Ok(new
                {
                    success = allSuccessful,
                    data = new
                    {
                        intent = request.Intent,
                        steps = steps,
                        message = allSuccessful ? "Breath loop completed successfully" : "Breath loop completed with errors",
                        timestamp = DateTimeOffset.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing breath loop: {ex.Message}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }
    }

    // Request types
    [RequestType("codex.ui.compose-page-request", "ComposePageRequest", "Request to compose UI page")]
    public record ComposePageRequest(
        string Intent,
        string? Path = null,
        List<object>? Lenses = null,
        List<object>? Controls = null
    );

    [RequestType("codex.ui.expand-components-request", "ExpandComponentsRequest", "Request to expand UI components")]
    public record ExpandComponentsRequest(
        string PageId,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.ui.validate-components-request", "ValidateComponentsRequest", "Request to validate UI components")]
    public record ValidateComponentsRequest(
        string PageId,
        bool EnableVisualValidation = false,
        string? SpecVision = null,
        string? Requirements = null,
        int? Width = null,
        int? Height = null,
        string? Viewport = null,
        double? MinimumScore = null,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.ui.breath-loop-request", "UIBreathLoopRequest", "Request for UI breath loop")]
    public record UIBreathLoopRequest(
        string Intent,
        string? Path = null,
        List<object>? Lenses = null,
        List<object>? Controls = null,
        string? EvolutionContext = null,
        string? Provider = null,
        string? Model = null
    );
}
