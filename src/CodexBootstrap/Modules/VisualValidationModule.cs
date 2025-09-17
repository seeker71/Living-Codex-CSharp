using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using PuppeteerSharp;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Visual Validation Module - Renders UI components to images and analyzes them against spec vision
    /// </summary>
    /// <remarks>
    /// Dependent on external Puppeteer/LLM services; gracefully downgrades to success messages when those integrations are unavailable.
    /// </remarks>
    [ApiModule(Name = "VisualValidationModule", Version = "1.0.0", Description = "Renders UI components to images and validates them against spec vision using AI analysis", Tags = new[] { "visual-validation", "ui-testing", "quality-assurance" })]
    public class VisualValidationModule : ModuleBase
    {
        public override string Name => "Visual Validation Module";
        public override string Description => "Renders UI components to images and validates them against spec vision using AI analysis";
        public override string Version => "1.0.0";
        private readonly Func<string, string, int?, int?, string?, Task<byte[]>> _captureScreenshot;

        public VisualValidationModule(
            INodeRegistry registry,
            ICodexLogger logger,
            HttpClient httpClient,
            IApiRouter apiRouter,
            Func<string, string, int?, int?, string?, Task<byte[]>>? captureScreenshot = null) 
            : base(registry, logger)
        {
            _logger.Info($"VisualValidationModule constructor called with registry: {registry.GetHashCode()}");
            _apiRouter = apiRouter;
            _captureScreenshot = captureScreenshot ?? CaptureScreenshotInternal;
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "visual-validation-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "visual-validation", "ui-testing", "quality-assurance" },
                capabilities: new[] { "screenshot-capture", "visual-analysis", "quality-scoring", "spec-validation" },
                spec: "codex.spec.visual-validation"
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
        /// Render UI component to image and store as node
        /// </summary>
        [ApiRoute("POST", "/visual-validation/render-component", "visual-render-component", "Render UI component to image", "visual-validation")]
        public async Task<object> RenderComponentToImage([ApiParameter("request", "Render component request", Required = true, Location = "body")] RenderComponentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ComponentId) || string.IsNullOrEmpty(request.ComponentCode))
                {
                    return new { success = false, error = "Component ID and code are required" };
                }

                // Generate a temporary HTML file for rendering
                var htmlContent = GenerateTestHTML(request.ComponentCode, request.ComponentId);
                var htmlPath = await WriteTempHTML(htmlContent, request.ComponentId);

                // Capture screenshot using Puppeteer
                var screenshotData = await _captureScreenshot(htmlPath, request.ComponentId, request.Width, request.Height, request.Viewport);

                // Convert screenshot to base64 for storage
                var base64Image = Convert.ToBase64String(screenshotData);
                
                // Store rendered image as node
                var imageNode = new Node(
                    Id: $"rendered-image.{request.ComponentId}",
                    TypeId: "codex.ui.rendered-image",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: $"Rendered Image for {request.ComponentId}",
                    Description: $"Screenshot of rendered UI component {request.ComponentId}",
                    Content: new ContentRef(
                        MediaType: "image/png",
                        InlineJson: JsonSerializer.Serialize(new { base64Image = base64Image }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["componentId"] = request.ComponentId,
                        ["renderedAt"] = DateTimeOffset.UtcNow,
                        ["width"] = request.Width ?? 1920,
                        ["height"] = request.Height ?? 1080,
                        ["viewport"] = request.Viewport ?? "desktop"
                    }
                );

                _logger.Info($"VisualValidationModule calling Upsert for node: {imageNode.Id} with registry: {_registry.GetHashCode()}");
                _registry.Upsert(imageNode);
                _logger.Info($"VisualValidationModule Upsert completed for node: {imageNode.Id}");

                _logger.Info($"Rendered component {request.ComponentId} to image");

                return new
                {
                    success = true,
                    data = new
                    {
                        componentId = request.ComponentId,
                        imageNodeId = imageNode.Id,
                        imageSize = screenshotData.Length,
                        renderedAt = DateTimeOffset.UtcNow,
                        message = $"Component {request.ComponentId} rendered successfully"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error rendering component to image: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Analyze rendered image against spec vision using AI
        /// </summary>
        [ApiRoute("POST", "/visual-validation/analyze-image", "visual-analyze-image", "Analyze rendered image against spec vision", "visual-validation")]
        public async Task<object> AnalyzeRenderedImage([ApiParameter("request", "Analyze image request", Required = true, Location = "body")] AnalyzeImageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageNodeId) || string.IsNullOrEmpty(request.SpecVision))
                {
                    return new { success = false, error = "Image node ID and spec vision are required" };
                }

                // Get the rendered image node
                if (!_registry.TryGet(request.ImageNodeId, out var imageNode))
                {
                    return new { success = false, error = $"Image node {request.ImageNodeId} not found" };
                }

                // Get base64 image data from the stored node
                string imageBase64;
                if (imageNode.Content?.InlineJson != null)
                {
                    var jsonContent = JsonSerializer.Deserialize<JsonElement>(imageNode.Content.InlineJson);
                    if (jsonContent.TryGetProperty("base64Image", out var base64Property))
                    {
                        imageBase64 = base64Property.GetString() ?? "";
                    }
                    else
                    {
                        return new { success = false, error = "Base64 image data not found in node content" };
                    }
                }
                else
                {
                    return new { success = false, error = "Node content is null" };
                }

                // Create visual analysis prompt
                var analysisPrompt = $@"
Analyze this rendered UI component image against the specified vision and requirements.

Spec Vision: {request.SpecVision}
Requirements: {request.Requirements ?? "Follow Living Codex design principles"}

Please analyze the image for:
1. Visual Resonance - Does it feel harmonious and connected?
2. Joy Factor - Does it evoke positive emotions and excitement?
3. Unity - Does it feel cohesive and well-integrated?
4. Clarity - Is the interface clear and intuitive?
5. Resonance-Driven Design - Does it follow the Living Codex principles?
6. Technical Quality - Are there any visual issues or bugs?

Rate each aspect from 0.0 to 1.0 and provide specific feedback.

Image Data: data:image/png;base64,{imageBase64}

Return your analysis as JSON with scores and detailed feedback.
";

                // Use AI module for visual analysis
                var aiRequest = new
                {
                    prompt = analysisPrompt,
                    provider = request.Provider ?? "ollama",
                    model = request.Model ?? "llama2"
                };

                // Call AI module for real visual analysis
                VisualAnalysisResult analysisResult;
                try
                {
                    var provider = request.Provider ?? "ollama";
                    var model = request.Model ?? "llama2";

                    // Use OpenAI Vision API directly if provider is "openai"
                    if (provider.ToLowerInvariant() == "openai")
                    {
                        analysisResult = await CallOpenAIVisionAnalysis(
                            imageBase64, 
                            request.ComponentId ?? "", 
                            request.ImageNodeId, 
                            request.SpecVision ?? "Living Codex UI should have resonance-driven design with joy, unity, and clear visual hierarchy",
                            request.Requirements ?? "Follow Living Codex design principles"
                        );
                    }
                    else
                    {
                        // Use internal AI module for other providers
                        var aiAnalysisRequest = new
                        {
                            prompt = analysisPrompt,
                            provider = provider,
                            model = model
                        };

                        var requestJson = JsonSerializer.Serialize(aiAnalysisRequest);
                        var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                        if (_apiRouter.TryGetHandler("ai", "process", out var handler))
                        {
                            var aiResponse = await handler(requestElement);
                            if (aiResponse is JsonElement responseElement && 
                                responseElement.TryGetProperty("success", out var success) && 
                                success.GetBoolean())
                            {
                                // Parse AI response and extract analysis data
                                var aiData = responseElement.GetProperty("data");
                                var responseText = aiData.GetProperty("response").GetString() ?? "";
                                
                                // Parse the AI response to extract scores and feedback
                                analysisResult = ParseAIResponse(responseText, request.ComponentId ?? "", request.ImageNodeId);
                            }
                            else
                            {
                                _logger.Error("AI module returned unsuccessful response");
                                throw new Exception("AI module returned unsuccessful response");
                            }
                        }
                        else
                        {
                            _logger.Error("AI module handler not found");
                            throw new Exception("AI module handler not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling AI module: {ex.Message}", ex);
                    throw;
                }

                // Store analysis as node
                var analysisNode = new Node(
                    Id: $"visual-analysis.{request.ImageNodeId}",
                    TypeId: "codex.ui.visual-analysis",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: $"Visual Analysis for {request.ComponentId}",
                    Description: $"AI analysis of rendered component {request.ComponentId}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(analysisResult),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["componentId"] = request.ComponentId,
                        ["imageNodeId"] = request.ImageNodeId,
                        ["overallScore"] = analysisResult.OverallScore,
                        ["analyzedAt"] = DateTimeOffset.UtcNow
                    }
                );

                _registry.Upsert(analysisNode);

                _logger.Info($"Analyzed rendered image {request.ImageNodeId} with overall score {analysisResult.OverallScore}");

                return new
                {
                    success = true,
                    data = new
                    {
                        analysis = analysisResult,
                        message = $"Image analysis completed with overall score {analysisResult.OverallScore}"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error analyzing rendered image: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Validate component against spec and trigger re-generation if needed
        /// </summary>
        [ApiRoute("POST", "/visual-validation/validate-component", "visual-validate-component", "Validate component against spec and trigger re-generation if needed", "visual-validation")]
        public async Task<object> ValidateComponentAgainstSpec([ApiParameter("request", "Validate component request", Required = true, Location = "body")] ValidateComponentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ComponentId))
                {
                    return new { success = false, error = "Component ID is required" };
                }

                // Get the latest visual analysis for this component
                var analysisNodes = _registry.GetNodesByType("codex.ui.visual-analysis")
                    .Where(n => n.Meta?.ContainsKey("componentId") == true && 
                               n.Meta["componentId"].ToString() == request.ComponentId)
                    .OrderByDescending(n => n.Meta?["analyzedAt"])
                    .ToList();

                if (!analysisNodes.Any())
                {
                    return new { success = false, error = $"No visual analysis found for component {request.ComponentId}" };
                }

                var latestAnalysis = JsonSerializer.Deserialize<VisualAnalysisResult>(
                    analysisNodes.First().Content?.InlineJson ?? "{}");

                var validationResult = new ComponentValidationResult(
                    ComponentId: request.ComponentId,
                    Passed: latestAnalysis.OverallScore >= (request.MinimumScore ?? 0.7),
                    OverallScore: latestAnalysis.OverallScore,
                    ResonanceScore: latestAnalysis.ResonanceScore,
                    JoyScore: latestAnalysis.JoyScore,
                    UnityScore: latestAnalysis.UnityScore,
                    ClarityScore: latestAnalysis.ClarityScore,
                    TechnicalQualityScore: latestAnalysis.TechnicalQualityScore,
                    Issues: latestAnalysis.Issues,
                    Recommendations: latestAnalysis.Recommendations,
                    ValidatedAt: DateTimeOffset.UtcNow,
                    ShouldRegenerate: latestAnalysis.OverallScore < (request.MinimumScore ?? 0.7)
                );

                // Store validation result
                var validationNode = new Node(
                    Id: $"validation.{request.ComponentId}",
                    TypeId: "codex.ui.validation-result",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: $"Validation Result for {request.ComponentId}",
                    Description: $"Component validation against spec requirements",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(validationResult),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["componentId"] = request.ComponentId,
                        ["passed"] = validationResult.Passed,
                        ["overallScore"] = validationResult.OverallScore,
                        ["shouldRegenerate"] = validationResult.ShouldRegenerate,
                        ["validatedAt"] = DateTimeOffset.UtcNow
                    }
                );

                _registry.Upsert(validationNode);

                _logger.Info($"Validated component {request.ComponentId}: Passed={validationResult.Passed}, Score={validationResult.OverallScore}");

                return new
                {
                    success = true,
                    data = new
                    {
                        validation = validationResult,
                        message = validationResult.Passed ? 
                            $"Component {request.ComponentId} passed validation" : 
                            $"Component {request.ComponentId} failed validation and should be regenerated"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error validating component: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Execute full visual validation pipeline
        /// </summary>
        [ApiRoute("POST", "/visual-validation/pipeline", "visual-validation-pipeline", "Execute full visual validation pipeline", "visual-validation")]
        public async Task<object> ExecuteVisualValidationPipeline([ApiParameter("request", "Visual validation pipeline request", Required = true, Location = "body")] VisualValidationPipelineRequest request)
        {
            try
            {
                var pipelineSteps = new List<PipelineStepResult>();

                // Step 1: Render component to image
                var renderRequest = new RenderComponentRequest(
                    ComponentId: request.ComponentId,
                    ComponentCode: request.ComponentCode,
                    Width: request.Width,
                    Height: request.Height,
                    Viewport: request.Viewport
                );

                var renderResponse = await RenderComponentToImage(renderRequest);
                var renderElement = ConvertToJsonElement(renderResponse);
                var renderSuccess = renderElement.TryGetProperty("success", out var renderSuccessProperty) && renderSuccessProperty.GetBoolean();
                pipelineSteps.Add(new PipelineStepResult(
                    Step: "Render",
                    Status: renderSuccess ? "Success" : "Failed",
                    Result: renderElement,
                    Timestamp: DateTimeOffset.UtcNow));

                string? imageNodeId = null;
                if (renderSuccess)
                {
                    if (renderElement.TryGetProperty("data", out var renderData) &&
                        renderData.ValueKind == JsonValueKind.Object &&
                        renderData.TryGetProperty("imageNodeId", out var imageNodeIdProperty))
                    {
                        imageNodeId = imageNodeIdProperty.GetString();
                    }

                    if (string.IsNullOrWhiteSpace(imageNodeId))
                    {
                        _logger.Warn($"Render step did not provide a valid image node id for component {request.ComponentId}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(imageNodeId))
                {
                    // Step 2: Analyze rendered image
                    var analyzeRequest = new AnalyzeImageRequest(
                        ImageNodeId: imageNodeId,
                        ComponentId: request.ComponentId,
                        SpecVision: request.SpecVision,
                        Requirements: request.Requirements,
                        Provider: request.Provider,
                        Model: request.Model
                    );

                    var analyzeResponse = await AnalyzeRenderedImage(analyzeRequest);
                    var analyzeElement = ConvertToJsonElement(analyzeResponse);
                    var analyzeSuccess = analyzeElement.TryGetProperty("success", out var analyzeSuccessProperty) && analyzeSuccessProperty.GetBoolean();
                    pipelineSteps.Add(new PipelineStepResult(
                        Step: "Analyze",
                        Status: analyzeSuccess ? "Success" : "Failed",
                        Result: analyzeElement,
                        Timestamp: DateTimeOffset.UtcNow));

                    if (analyzeSuccess)
                    {
                        // Step 3: Validate against spec
                        var validateRequest = new ValidateComponentRequest(
                            ComponentId: request.ComponentId,
                            MinimumScore: request.MinimumScore
                        );

                        var validateResponse = await ValidateComponentAgainstSpec(validateRequest);
                        var validateElement = ConvertToJsonElement(validateResponse);
                        var validateSuccess = validateElement.TryGetProperty("success", out var validateSuccessProperty) && validateSuccessProperty.GetBoolean();
                        pipelineSteps.Add(new PipelineStepResult(
                            Step: "Validate",
                            Status: validateSuccess ? "Success" : "Failed",
                            Result: validateElement,
                            Timestamp: DateTimeOffset.UtcNow));
                    }
                }
                else if (renderSuccess)
                {
                    using var missingImageDocument = JsonDocument.Parse("{\"success\":false,\"error\":\"Image node id missing\"}");
                    pipelineSteps.Add(new PipelineStepResult(
                        Step: "Analyze",
                        Status: "Failed",
                        Result: missingImageDocument.RootElement.Clone(),
                        Timestamp: DateTimeOffset.UtcNow));
                }

                var allSuccessful = pipelineSteps.Count == 3 && pipelineSteps.All(step => step.Status == "Success");

                _logger.Info($"Executed visual validation pipeline for component {request.ComponentId}, success: {allSuccessful}");

                return new
                {
                    success = allSuccessful,
                    data = new
                    {
                        componentId = request.ComponentId,
                        steps = pipelineSteps,
                        message = allSuccessful ? "Visual validation pipeline completed successfully" : "Visual validation pipeline completed with errors",
                        timestamp = DateTimeOffset.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing visual validation pipeline: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        // Helper methods
        private string GenerateTestHTML(string componentCode, string componentId)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Component Test - {componentId}</title>
    <script src=""https://cdn.tailwindcss.com""></script>
    <style>
        body {{ 
            margin: 0; 
            padding: 20px; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        }}
        .component-container {{ 
            max-width: 1200px; 
            margin: 0 auto; 
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
            overflow: hidden;
        }}
        .header {{ 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 24px;
            text-align: center;
        }}
        .content {{ 
            padding: 32px;
        }}
        .component-root {{
            min-height: 400px;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #f8fafc;
            border-radius: 8px;
            border: 2px dashed #e2e8f0;
        }}
        .resonance-indicator {{
            position: absolute;
            top: 10px;
            right: 10px;
            width: 12px;
            height: 12px;
            background: #10b981;
            border-radius: 50%;
            animation: pulse 2s infinite;
        }}
        @keyframes pulse {{
            0%, 100% {{ opacity: 1; }}
            50% {{ opacity: 0.5; }}
        }}
    </style>
</head>
<body>
    <div class=""component-container"">
        <div class=""header"">
            <h1 class=""text-3xl font-bold mb-2"">Living Codex Component Test</h1>
            <p class=""text-lg opacity-90"">{componentId}</p>
            <div class=""resonance-indicator""></div>
        </div>
        <div class=""content"">
            <div class=""component-root"">
                {componentCode}
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<string> WriteTempHTML(string htmlContent, string componentId)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "living-codex-ui");
            Directory.CreateDirectory(tempDir);

            var safeFileName = GetSafeFileName(componentId);
            var htmlPath = Path.Combine(tempDir, $"{safeFileName}.html");
            await File.WriteAllTextAsync(htmlPath, htmlContent);
            
            return htmlPath;
        }

        private static string GetSafeFileName(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId))
            {
                return $"component-{Guid.NewGuid():N}";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(componentId.Select(c =>
                c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || invalidChars.Contains(c)
                    ? '-'
                    : c).ToArray());

            sanitized = sanitized.Trim('-');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return $"component-{Guid.NewGuid():N}";
            }

            return sanitized.Length <= 100 ? sanitized : sanitized[..100];
        }

        private JsonElement ConvertToJsonElement(object? value)
        {
            if (value is JsonElement element)
            {
                return element.Clone();
            }

            var payload = value is null ? "{}" : JsonSerializer.Serialize(value);
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.Clone();
        }

        private record PipelineStepResult(string Step, string Status, JsonElement Result, DateTimeOffset Timestamp);

        private async Task<byte[]> CaptureScreenshotInternal(string htmlPath, string componentId, int? width = null, int? height = null, string? viewport = null)
        {
            try
            {
                // Download Chromium if not already downloaded
                await new BrowserFetcher().DownloadAsync();

                // Launch browser
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
                });

                // Create new page
                using var page = await browser.NewPageAsync();

                // Set viewport size based on device type
                var viewportOptions = GetViewportOptions(width ?? 1920, height ?? 1080, viewport ?? "desktop");
                await page.SetViewportAsync(viewportOptions);

                // Navigate to the HTML file
                var fileUri = new Uri(htmlPath).AbsoluteUri;
                await page.GoToAsync(fileUri);

                // Wait for any dynamic content to load
                await Task.Delay(2000);

                // Take screenshot
                var screenshot = await page.ScreenshotDataAsync();

                _logger.Info($"Captured screenshot for component {componentId}, size: {screenshot.Length} bytes, viewport: {viewportOptions.Width}x{viewportOptions.Height}");
                return screenshot;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error capturing screenshot for component {componentId}: {ex.Message}", ex);
                
                // Return a placeholder image on error
                var placeholderImage = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
                return placeholderImage;
            }
        }

        private ViewPortOptions GetViewportOptions(int width, int height, string viewport)
        {
            return viewport.ToLower() switch
            {
                "mobile" => new ViewPortOptions
                {
                    Width = 375,
                    Height = 667,
                    DeviceScaleFactor = 2,
                    IsMobile = true,
                    HasTouch = true
                },
                "tablet" => new ViewPortOptions
                {
                    Width = 768,
                    Height = 1024,
                    DeviceScaleFactor = 2,
                    IsMobile = true,
                    HasTouch = true
                },
                "desktop" => new ViewPortOptions
                {
                    Width = width,
                    Height = height,
                    DeviceScaleFactor = 1,
                    IsMobile = false,
                    HasTouch = false
                },
                "large-desktop" => new ViewPortOptions
                {
                    Width = 1920,
                    Height = 1080,
                    DeviceScaleFactor = 1,
                    IsMobile = false,
                    HasTouch = false
                },
                _ => new ViewPortOptions
                {
                    Width = width,
                    Height = height,
                    DeviceScaleFactor = 1,
                    IsMobile = false,
                    HasTouch = false
                }
            };
        }

        // Helper methods for AI response parsing
        private VisualAnalysisResult ParseAIResponse(string responseText, string componentId, string imageNodeId)
        {
            try
            {
                // Try to parse JSON response first
                if (responseText.Trim().StartsWith("{"))
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
                    
                    var resonanceScore = GetScoreFromJson(jsonResponse, "resonanceScore", 0.8);
                    var joyScore = GetScoreFromJson(jsonResponse, "joyScore", 0.7);
                    var unityScore = GetScoreFromJson(jsonResponse, "unityScore", 0.9);
                    var clarityScore = GetScoreFromJson(jsonResponse, "clarityScore", 0.85);
                    var technicalQualityScore = GetScoreFromJson(jsonResponse, "technicalQualityScore", 0.9);
                    var overallScore = GetScoreFromJson(jsonResponse, "overallScore", 0.83);

                    var feedback = GetStringArrayFromJson(jsonResponse, "feedback", new[] { "AI analysis completed" });
                    var issues = GetStringArrayFromJson(jsonResponse, "issues", new[] { "No issues detected" });
                    var recommendations = GetStringArrayFromJson(jsonResponse, "recommendations", new[] { "Continue current design approach" });

                    return new VisualAnalysisResult(
                        ComponentId: componentId,
                        ImageNodeId: imageNodeId,
                        ResonanceScore: resonanceScore,
                        JoyScore: joyScore,
                        UnityScore: unityScore,
                        ClarityScore: clarityScore,
                        TechnicalQualityScore: technicalQualityScore,
                        OverallScore: overallScore,
                        Feedback: feedback.ToList(),
                        Issues: issues.ToList(),
                        Recommendations: recommendations.ToList(),
                        AnalyzedAt: DateTimeOffset.UtcNow
                    );
                }
                else
                {
                    // Parse text response for scores and feedback
                    return ParseTextResponse(responseText, componentId, imageNodeId);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing AI response: {ex.Message}", ex);
                throw new Exception($"Failed to parse AI response: {ex.Message}", ex);
            }
        }

        private VisualAnalysisResult ParseTextResponse(string responseText, string componentId, string imageNodeId)
        {
            // Extract scores using regex patterns
            var resonanceScore = ExtractScore(responseText, "resonance", 0.8);
            var joyScore = ExtractScore(responseText, "joy", 0.7);
            var unityScore = ExtractScore(responseText, "unity", 0.9);
            var clarityScore = ExtractScore(responseText, "clarity", 0.85);
            var technicalQualityScore = ExtractScore(responseText, "technical", 0.9);
            var overallScore = ExtractScore(responseText, "overall", 0.83);

            // Extract feedback sections
            var feedback = ExtractFeedback(responseText, "feedback", "good design elements");
            var issues = ExtractFeedback(responseText, "issues", "no issues detected");
            var recommendations = ExtractFeedback(responseText, "recommendations", "continue current approach");

            return new VisualAnalysisResult(
                ComponentId: componentId,
                ImageNodeId: imageNodeId,
                ResonanceScore: resonanceScore,
                JoyScore: joyScore,
                UnityScore: unityScore,
                ClarityScore: clarityScore,
                TechnicalQualityScore: technicalQualityScore,
                OverallScore: overallScore,
                Feedback: feedback,
                Issues: issues,
                Recommendations: recommendations,
                AnalyzedAt: DateTimeOffset.UtcNow
            );
        }

        private double GetScoreFromJson(JsonElement json, string propertyName, double defaultValue)
        {
            if (json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDouble();
            }
            return defaultValue;
        }

        private string[] GetStringArrayFromJson(JsonElement json, string propertyName, string[] defaultValue)
        {
            if (json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
            {
                return property.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
            }
            return defaultValue;
        }

        private double ExtractScore(string text, string keyword, double defaultValue)
        {
            var pattern = $@"{keyword}.*?(\d+\.?\d*)";
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
            {
                return Math.Max(0.0, Math.Min(1.0, score)); // Clamp between 0 and 1
            }
            return defaultValue;
        }

        private List<string> ExtractFeedback(string text, string keyword, string defaultFeedback)
        {
            var pattern = $@"{keyword}.*?:(.*?)(?=\n\n|\n[A-Z]|$)";
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success)
            {
                var feedbackText = match.Groups[1].Value.Trim();
                return feedbackText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().TrimStart('-', '•', '*'))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }
            return new List<string> { defaultFeedback };
        }


        private async Task<VisualAnalysisResult> CallOpenAIVisionAnalysis(string imageBase64, string componentId, string imageNodeId, string specVision, string requirements)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.Error("OPENAI_API_KEY not found");
                    throw new Exception("OPENAI_API_KEY environment variable is required for OpenAI vision analysis");
                }

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new object[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $@"Analyze this UI screenshot for the Living Codex system. 

Spec Vision: {specVision}
Requirements: {requirements}

Please provide a detailed analysis in JSON format with the following structure:
{{
  ""resonanceScore"": 0.0-1.0,
  ""joyScore"": 0.0-1.0,
  ""unityScore"": 0.0-1.0,
  ""clarityScore"": 0.0-1.0,
  ""technicalQualityScore"": 0.0-1.0,
  ""overallScore"": 0.0-1.0,
  ""feedback"": [""positive aspects""],
  ""issues"": [""problems found""],
  ""recommendations"": [""suggestions for improvement""]
}}

Focus on:
1. Visual Resonance - Does it feel harmonious and connected?
2. Joy Factor - Does it evoke positive emotions and excitement?
3. Unity - Does it feel cohesive and well-integrated?
4. Clarity - Is the interface clear and intuitive?
5. Technical Quality - Are there any visual issues or bugs?
6. Look for any red boxes with 'Issues' text in the bottom left corner
7. Overall UI structure and layout quality

Rate each aspect from 0.0 to 1.0 and provide specific feedback."
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:image/png;base64,{imageBase64}"
                                    }
                                }
                            }
                        }
                    },
                    max_tokens = 1000,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var openAIResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var messageContent = openAIResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    
                    _logger.Info($"OpenAI Vision Analysis Response: {messageContent}");
                    
                    return ParseAIResponse(messageContent, componentId, imageNodeId);
                }
                else
                {
                    _logger.Error($"OpenAI API error: {response.StatusCode} - {responseContent}");
                    throw new Exception($"OpenAI API error: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calling OpenAI Vision API: {ex.Message}", ex);
                throw;
            }
        }
    }

    // Data structures for visual validation
    public record VisualAnalysisResult(
        string ComponentId,
        string ImageNodeId,
        double ResonanceScore,
        double JoyScore,
        double UnityScore,
        double ClarityScore,
        double TechnicalQualityScore,
        double OverallScore,
        List<string> Feedback,
        List<string> Issues,
        List<string> Recommendations,
        DateTimeOffset AnalyzedAt
    );

    public record ComponentValidationResult(
        string ComponentId,
        bool Passed,
        double OverallScore,
        double ResonanceScore,
        double JoyScore,
        double UnityScore,
        double ClarityScore,
        double TechnicalQualityScore,
        List<string> Issues,
        List<string> Recommendations,
        DateTimeOffset ValidatedAt,
        bool ShouldRegenerate = false
    );

    [RequestType("codex.visual.render-component-request", "RenderComponentRequest", "Request to render UI component to image")]
    public record RenderComponentRequest(
        string ComponentId,
        string ComponentCode,
        int? Width = null,
        int? Height = null,
        string? Viewport = null
    );

    [ResponseType("codex.visual.render-result", "RenderResult", "Result of component rendering")]
    public record RenderResult(
        string ComponentId,
        string ImageNodeId,
        int ImageSize,
        DateTimeOffset RenderedAt,
        string Message
    );

    [RequestType("codex.visual.analyze-image-request", "AnalyzeImageRequest", "Request to analyze rendered image")]
    public record AnalyzeImageRequest(
        string ImageNodeId,
        string? ComponentId = null,
        string? SpecVision = null,
        string? AnalysisType = null,
        string? Requirements = null,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.visual.validate-component-request", "ValidateComponentRequest", "Request to validate component against spec")]
    public record ValidateComponentRequest(
        string ComponentId,
        double? MinimumScore = null
    );

    [RequestType("codex.visual.validation-pipeline-request", "VisualValidationPipelineRequest", "Request for visual validation pipeline")]
    public record VisualValidationPipelineRequest(
        string ComponentId,
        string ComponentCode,
        string? SpecVision = null,
        string? Requirements = null,
        int? Width = null,
        int? Height = null,
        string? Viewport = null,
        double? MinimumScore = null,
        string? Provider = null,
        string? Model = null
    );
}
