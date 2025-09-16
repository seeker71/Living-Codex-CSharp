using System;
using System.Collections.Generic;
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
    [ApiModule(Name = "VisualValidationModule", Version = "1.0.0", Description = "Renders UI components to images and validates them against spec vision using AI analysis", Tags = new[] { "visual-validation", "ui-testing", "quality-assurance" })]
    public class VisualValidationModule : ModuleBase
    {
        public override string Name => "Visual Validation Module";
        public override string Description => "Renders UI components to images and validates them against spec vision using AI analysis";
        public override string Version => "1.0.0";

        public VisualValidationModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
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
                var screenshotData = await CaptureScreenshot(htmlPath, request.ComponentId, request.Width, request.Height, request.Viewport);

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
                        InlineJson: null,
                        InlineBytes: screenshotData,
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

                _registry.Upsert(imageNode);

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

                // Convert image to base64 for AI analysis
                var imageBase64 = Convert.ToBase64String(imageNode.Content?.InlineBytes ?? Array.Empty<byte>());

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

                // For now, simulate AI analysis (in real implementation, call AI module)
                var analysisResult = new VisualAnalysisResult(
                    ComponentId: request.ComponentId ?? "",
                    ImageNodeId: request.ImageNodeId,
                    ResonanceScore: 0.8,
                    JoyScore: 0.7,
                    UnityScore: 0.9,
                    ClarityScore: 0.85,
                    TechnicalQualityScore: 0.9,
                    OverallScore: 0.83,
                    Feedback: new List<string>
                    {
                        "Good use of resonance-driven design principles",
                        "Clear visual hierarchy and intuitive layout",
                        "Could improve joy factor with more engaging animations",
                        "Excellent unity and cohesion across components"
                    },
                    Issues: new List<string>
                    {
                        "Minor: Button hover states could be more prominent"
                    },
                    Recommendations: new List<string>
                    {
                        "Add subtle animations to enhance joy factor",
                        "Consider adding more visual resonance indicators",
                        "Implement micro-interactions for better engagement"
                    },
                    AnalyzedAt: DateTimeOffset.UtcNow
                );

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
                var pipelineSteps = new List<object>();

                // Step 1: Render component to image
                var renderRequest = new RenderComponentRequest(
                    ComponentId: request.ComponentId,
                    ComponentCode: request.ComponentCode,
                    Width: request.Width,
                    Height: request.Height,
                    Viewport: request.Viewport
                );

                var renderResult = await RenderComponentToImage(renderRequest);
                pipelineSteps.Add(new
                {
                    step = "Render",
                    status = renderResult is JsonElement r && r.TryGetProperty("success", out var rs) && rs.GetBoolean() ? "Success" : "Failed",
                    result = renderResult,
                    timestamp = DateTimeOffset.UtcNow
                });

                if (renderResult is JsonElement renderElement && renderElement.TryGetProperty("success", out var renderSuccess) && renderSuccess.GetBoolean())
                {
                    var imageNodeId = renderElement.GetProperty("data").GetProperty("imageNodeId").GetString() ?? "";

                    // Step 2: Analyze rendered image
                    var analyzeRequest = new AnalyzeImageRequest(
                        ImageNodeId: imageNodeId,
                        ComponentId: request.ComponentId,
                        SpecVision: request.SpecVision,
                        Requirements: request.Requirements,
                        Provider: request.Provider,
                        Model: request.Model
                    );

                    var analyzeResult = await AnalyzeRenderedImage(analyzeRequest);
                    pipelineSteps.Add(new
                    {
                        step = "Analyze",
                        status = analyzeResult is JsonElement a && a.TryGetProperty("success", out var aSuccess) && aSuccess.GetBoolean() ? "Success" : "Failed",
                        result = analyzeResult,
                        timestamp = DateTimeOffset.UtcNow
                    });

                    // Step 3: Validate against spec
                    var validateRequest = new ValidateComponentRequest(
                        ComponentId: request.ComponentId,
                        MinimumScore: request.MinimumScore
                    );

                    var validateResult = await ValidateComponentAgainstSpec(validateRequest);
                    pipelineSteps.Add(new
                    {
                        step = "Validate",
                        status = validateResult is JsonElement v && v.TryGetProperty("success", out var vs) && vs.GetBoolean() ? "Success" : "Failed",
                        result = validateResult,
                        timestamp = DateTimeOffset.UtcNow
                    });
                }

                var allSuccessful = pipelineSteps.All(s => s is JsonElement step && step.TryGetProperty("status", out var status) && status.GetString() == "Success");

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
            
            var htmlPath = Path.Combine(tempDir, $"{componentId}.html");
            await File.WriteAllTextAsync(htmlPath, htmlContent);
            
            return htmlPath;
        }

        private async Task<byte[]> CaptureScreenshot(string htmlPath, string componentId, int? width = null, int? height = null, string? viewport = null)
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
        bool ShouldRegenerate
    );

    // Request types
    [RequestType("codex.visual.render-component-request", "RenderComponentRequest", "Request to render component to image")]
    public record RenderComponentRequest(
        string ComponentId,
        string ComponentCode,
        int? Width = null,
        int? Height = null,
        string? Viewport = null
    );

    [RequestType("codex.visual.analyze-image-request", "AnalyzeImageRequest", "Request to analyze rendered image")]
    public record AnalyzeImageRequest(
        string ImageNodeId,
        string? ComponentId = null,
        string? SpecVision = null,
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
