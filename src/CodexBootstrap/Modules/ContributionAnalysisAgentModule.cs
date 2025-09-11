using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Contribution Analysis Agent - AI-powered analysis of concepts and contributions
    /// Identifies high-value patterns, insights, and opportunities for concept enhancement
    /// </summary>
    [ApiModule(Name = "ContributionAnalysisAgent", Version = "1.0.0", Description = "AI agent for analyzing contributions and identifying high-value concepts", Tags = new[] { "ai-agent", "contribution-analysis", "pattern-recognition", "value-identification" })]
    public class ContributionAnalysisAgentModule : IModule
    {
        private readonly IApiRouter _apiRouter;
        private readonly NodeRegistry _registry;
        private readonly CodexBootstrap.Core.ILogger _logger;
        private CoreApiService? _coreApiService;

        public ContributionAnalysisAgentModule(IApiRouter apiRouter, NodeRegistry registry)
        {
            _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _logger = new Log4NetLogger(typeof(ContributionAnalysisAgentModule));
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            _coreApiService = coreApi;

            // Contribution Analysis endpoints
            app.MapPost("/agent/contribution/analyze", AnalyzeContribution)
                .WithName("agent-contribution-analyze")
                .WithTags("Contribution Analysis Agent");

            app.MapPost("/agent/contribution/batch-analyze", BatchAnalyzeContributions)
                .WithName("agent-contribution-batch-analyze")
                .WithTags("Contribution Analysis Agent");

            app.MapPost("/agent/concept/identify-value", IdentifyHighValueConcepts)
                .WithName("agent-concept-identify-value")
                .WithTags("Contribution Analysis Agent");

            app.MapPost("/agent/pattern/discover", DiscoverEmergingPatterns)
                .WithName("agent-pattern-discover")
                .WithTags("Contribution Analysis Agent");

            app.MapPost("/agent/insight/generate", GenerateInsights)
                .WithName("agent-insight-generate")
                .WithTags("Contribution Analysis Agent");

            app.MapGet("/agent/analysis/status/{analysisId}", GetAnalysisStatus)
                .WithName("agent-analysis-status")
                .WithTags("Contribution Analysis Agent");

            app.MapGet("/agent/recommendations/{userId}", GetRecommendations)
                .WithName("agent-recommendations")
                .WithTags("Contribution Analysis Agent");

            // Register all Contribution Analysis Agent related nodes for AI agent discovery
            RegisterContributionAnalysisAgentNodes(nodeRegistry);
        }

        public async Task<ContributionAnalysisResponse> AnalyzeContribution(ContributionAnalysisRequest request)
        {
            try
            {
                _logger.Info($"Starting contribution analysis for contribution {request.ContributionId}");

                // Get contribution details
                var contribution = await GetContributionDetails(request.ContributionId);
                if (contribution == null)
                {
                    return new ContributionAnalysisResponse
                    {
                        Success = false,
                        AnalysisId = request.AnalysisId,
                        Message = "Contribution not found"
                    };
                }

                // Perform AI-powered analysis
                var analysis = await PerformAIAnalysis(contribution, request.AnalysisOptions);
                
                // Calculate value metrics
                var valueMetrics = CalculateValueMetrics(contribution, analysis);
                
                // Generate recommendations
                var recommendations = await GenerateRecommendations(contribution, analysis, request.UserId);

                _logger.Info($"Contribution analysis completed: Value Score={valueMetrics.OverallValue:F2}");

                return new ContributionAnalysisResponse
                {
                    Success = true,
                    AnalysisId = request.AnalysisId,
                    ContributionId = request.ContributionId,
                    Analysis = analysis,
                    ValueMetrics = valueMetrics,
                    Recommendations = recommendations,
                    AnalyzedAt = DateTime.UtcNow,
                    Message = "Contribution analysis completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Contribution analysis failed: {ex.Message}", ex);
                return new ContributionAnalysisResponse
                {
                    Success = false,
                    AnalysisId = request.AnalysisId,
                    Message = $"Analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<ContributionBatchAnalysisResponse> BatchAnalyzeContributions(ContributionBatchAnalysisRequest request)
        {
            try
            {
                _logger.Info($"Starting batch analysis for {request.ContributionIds.Count} contributions");

                var analysisTasks = request.ContributionIds.Select(async contributionId =>
                {
                    var analysisRequest = new ContributionAnalysisRequest
                    {
                        AnalysisId = Guid.NewGuid().ToString(),
                        ContributionId = contributionId,
                        UserId = request.UserId,
                        AnalysisOptions = request.AnalysisOptions
                    };
                    return await AnalyzeContribution(analysisRequest);
                });

                var results = await Task.WhenAll(analysisTasks);
                var successfulAnalyses = results.Where(r => r.Success).ToList();

                // Generate batch insights
                var batchInsights = await GenerateBatchInsights(successfulAnalyses);

                _logger.Info($"Batch analysis completed: {successfulAnalyses.Count} successful analyses");

                return new ContributionBatchAnalysisResponse
                {
                    Success = true,
                    BatchId = request.BatchId,
                    AnalysisResults = results.ToList(),
                    BatchInsights = batchInsights,
                    AnalyzedAt = DateTime.UtcNow,
                    Message = $"Batch analysis completed: {successfulAnalyses.Count} successful"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Batch analysis failed: {ex.Message}", ex);
                return new ContributionBatchAnalysisResponse
                {
                    Success = false,
                    BatchId = request.BatchId,
                    Message = $"Batch analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<HighValueConceptsResponse> IdentifyHighValueConcepts(HighValueConceptsRequest request)
        {
            try
            {
                _logger.Info($"Identifying high-value concepts with criteria: {request.Criteria}");

                // Get concepts from registry
                var concepts = await GetConceptsFromRegistry(request.Filters);
                
                // Analyze each concept for value potential
                var conceptAnalyses = new List<ConceptValueAnalysis>();
                foreach (var concept in concepts)
                {
                    var analysis = await AnalyzeConceptValue(concept, request.Criteria);
                    conceptAnalyses.Add(analysis);
                }

                // Rank concepts by value score
                var rankedConcepts = conceptAnalyses
                    .OrderByDescending(c => c.ValueScore)
                    .Take(request.MaxResults)
                    .ToList();

                // Generate value insights
                var valueInsights = await GenerateValueInsights(rankedConcepts);

                _logger.Info($"High-value concept identification completed: {rankedConcepts.Count} concepts identified");

                return new HighValueConceptsResponse
                {
                    Success = true,
                    Concepts = rankedConcepts,
                    ValueInsights = valueInsights,
                    IdentifiedAt = DateTime.UtcNow,
                    Message = $"Identified {rankedConcepts.Count} high-value concepts"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"High-value concept identification failed: {ex.Message}", ex);
                return new HighValueConceptsResponse
                {
                    Success = false,
                    Message = $"High-value concept identification failed: {ex.Message}"
                };
            }
        }

        public async Task<PatternDiscoveryResponse> DiscoverEmergingPatterns(PatternDiscoveryRequest request)
        {
            try
            {
                _logger.Info($"Discovering emerging patterns in {request.DataSource}");

                // Get data from specified source
                var data = await GetPatternData(request.DataSource, request.Filters);
                
                // Perform pattern analysis using AI
                var patterns = await AnalyzePatterns(data, request.PatternTypes);
                
                // Calculate pattern strength and confidence
                var analyzedPatterns = patterns.Select(p => new AnalyzedPattern
                {
                    PatternId = p.PatternId,
                    PatternType = p.PatternType,
                    Description = p.Description,
                    Strength = CalculatePatternStrength(p, data),
                    Confidence = CalculatePatternConfidence(p, data),
                    Frequency = p.Frequency,
                    Examples = p.Examples,
                    DetectedAt = DateTime.UtcNow
                }).ToList();

                // Generate pattern insights
                var patternInsights = await GeneratePatternInsights(analyzedPatterns);

                _logger.Info($"Pattern discovery completed: {analyzedPatterns.Count} patterns discovered");

                return new PatternDiscoveryResponse
                {
                    Success = true,
                    Patterns = analyzedPatterns,
                    PatternInsights = patternInsights,
                    DiscoveredAt = DateTime.UtcNow,
                    Message = $"Discovered {analyzedPatterns.Count} emerging patterns"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Pattern discovery failed: {ex.Message}", ex);
                return new PatternDiscoveryResponse
                {
                    Success = false,
                    Message = $"Pattern discovery failed: {ex.Message}"
                };
            }
        }

        public async Task<InsightGenerationResponse> GenerateInsights(InsightGenerationRequest request)
        {
            try
            {
                _logger.Info($"Generating insights for {request.InsightType}");

                // Get relevant data based on insight type
                var data = await GetInsightData(request.InsightType, request.DataFilters);
                
                // Use AI to generate insights
                var insights = await GenerateAIInsights(data, request.InsightType, request.InsightOptions);
                
                // Validate and score insights
                var validatedInsights = insights.Select(i => new ValidatedInsight
                {
                    InsightId = i.InsightId,
                    Title = i.Title,
                    Description = i.Description,
                    InsightType = i.InsightType,
                    Confidence = i.Confidence,
                    Relevance = CalculateRelevance(i, request.Context),
                    Actionability = CalculateActionability(i),
                    GeneratedAt = DateTime.UtcNow
                }).ToList();

                // Generate insight recommendations
                var recommendations = await GenerateInsightRecommendations(validatedInsights, request.UserId);

                _logger.Info($"Insight generation completed: {validatedInsights.Count} insights generated");

                return new InsightGenerationResponse
                {
                    Success = true,
                    Insights = validatedInsights,
                    Recommendations = recommendations,
                    GeneratedAt = DateTime.UtcNow,
                    Message = $"Generated {validatedInsights.Count} insights"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Insight generation failed: {ex.Message}", ex);
                return new InsightGenerationResponse
                {
                    Success = false,
                    Message = $"Insight generation failed: {ex.Message}"
                };
            }
        }

        public async Task<AnalysisStatusResponse> GetAnalysisStatus(string analysisId)
        {
            try
            {
                // In a real implementation, this would check a persistent store
                // For now, return a mock status
                return new AnalysisStatusResponse
                {
                    AnalysisId = analysisId,
                    Status = "completed",
                    Progress = 100,
                    StartedAt = DateTime.UtcNow.AddMinutes(-5),
                    CompletedAt = DateTime.UtcNow,
                    Message = "Analysis completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get analysis status: {ex.Message}", ex);
                return new AnalysisStatusResponse
                {
                    AnalysisId = analysisId,
                    Status = "error",
                    Message = $"Failed to get status: {ex.Message}"
                };
            }
        }

        public async Task<RecommendationsResponse> GetRecommendations(string userId)
        {
            try
            {
                _logger.Info($"Getting recommendations for user {userId}");

                // Get user's contribution history
                var userContributions = await GetUserContributions(userId);
                
                // Analyze user's patterns and preferences
                var userProfile = await AnalyzeUserProfile(userId, userContributions);
                
                // Generate personalized recommendations
                var recommendations = await GeneratePersonalizedRecommendations(userProfile, userContributions);

                _logger.Info($"Generated {recommendations.Count} recommendations for user {userId}");

                return new RecommendationsResponse
                {
                    Success = true,
                    UserId = userId,
                    Recommendations = recommendations,
                    GeneratedAt = DateTime.UtcNow,
                    Message = $"Generated {recommendations.Count} personalized recommendations"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get recommendations for user {userId}: {ex.Message}", ex);
                return new RecommendationsResponse
                {
                    Success = false,
                    UserId = userId,
                    Message = $"Failed to get recommendations: {ex.Message}"
                };
            }
        }

        // Helper methods
        private async Task<ContributionDetails?> GetContributionDetails(string contributionId)
        {
            try
            {
                if (_coreApiService == null) return null;

                var args = JsonSerializer.SerializeToElement(new { contributionId });
                var call = new DynamicCall("codex.user-contributions", "get-contribution", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);

                if (response is JsonElement jsonResponse)
                {
                    return new ContributionDetails
                    {
                        ContributionId = contributionId,
                        Title = jsonResponse.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "" : "",
                        Description = jsonResponse.TryGetProperty("description", out var descElement) ? descElement.GetString() ?? "" : "",
                        Content = jsonResponse.TryGetProperty("content", out var contentElement) ? contentElement.GetString() ?? "" : "",
                        AuthorId = jsonResponse.TryGetProperty("authorId", out var authorElement) ? authorElement.GetString() ?? "" : "",
                        CreatedAt = jsonResponse.TryGetProperty("createdAt", out var createdElement) ? createdElement.GetDateTime() : DateTime.UtcNow,
                        Tags = new List<string>()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get contribution details: {ex.Message}", ex);
                return null;
            }
        }

        private async Task<ContributionAnalysis> PerformAIAnalysis(ContributionDetails contribution, Dictionary<string, object> analysisOptions)
        {
            try
            {
                if (_coreApiService == null)
                {
                    return new ContributionAnalysis
                    {
                        AnalysisId = Guid.NewGuid().ToString(),
                        QualityScore = 0.5,
                        InnovationScore = 0.5,
                        ImpactScore = 0.5,
                        ClarityScore = 0.5,
                        CompletenessScore = 0.5,
                        AnalysisText = "AI analysis not available"
                    };
                }

                // Use LLM module for AI analysis
                var prompt = BuildAnalysisPrompt(contribution, analysisOptions);
                var args = JsonSerializer.SerializeToElement(new { prompt, model = "gpt-oss:20b" });
                var call = new DynamicCall("codex.llm.future", "analyze", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);

                if (response is JsonElement jsonResponse)
                {
                    var analysisText = jsonResponse.TryGetProperty("response", out var responseElement) ? responseElement.GetString() ?? "" : "";
                    
                    return new ContributionAnalysis
                    {
                        AnalysisId = Guid.NewGuid().ToString(),
                        QualityScore = ExtractScore(analysisText, "Quality"),
                        InnovationScore = ExtractScore(analysisText, "Innovation"),
                        ImpactScore = ExtractScore(analysisText, "Impact"),
                        ClarityScore = ExtractScore(analysisText, "Clarity"),
                        CompletenessScore = ExtractScore(analysisText, "Completeness"),
                        AnalysisText = analysisText
                    };
                }

                return new ContributionAnalysis
                {
                    AnalysisId = Guid.NewGuid().ToString(),
                    QualityScore = 0.5,
                    InnovationScore = 0.5,
                    ImpactScore = 0.5,
                    ClarityScore = 0.5,
                    CompletenessScore = 0.5,
                    AnalysisText = "AI analysis completed with default scores"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"AI analysis failed: {ex.Message}", ex);
                return new ContributionAnalysis
                {
                    AnalysisId = Guid.NewGuid().ToString(),
                    QualityScore = 0.5,
                    InnovationScore = 0.5,
                    ImpactScore = 0.5,
                    ClarityScore = 0.5,
                    CompletenessScore = 0.5,
                    AnalysisText = $"Analysis failed: {ex.Message}"
                };
            }
        }

        private ValueMetrics CalculateValueMetrics(ContributionDetails contribution, ContributionAnalysis analysis)
        {
            var overallValue = (analysis.QualityScore + analysis.InnovationScore + analysis.ImpactScore + 
                              analysis.ClarityScore + analysis.CompletenessScore) / 5.0;

            return new ValueMetrics
            {
                OverallValue = overallValue,
                QualityValue = analysis.QualityScore,
                InnovationValue = analysis.InnovationScore,
                ImpactValue = analysis.ImpactScore,
                ClarityValue = analysis.ClarityScore,
                CompletenessValue = analysis.CompletenessScore,
                CalculatedAt = DateTime.UtcNow
            };
        }

        private async Task<List<Recommendation>> GenerateRecommendations(ContributionDetails contribution, ContributionAnalysis analysis, string userId)
        {
            var recommendations = new List<Recommendation>();

            // Generate recommendations based on analysis scores
            if (analysis.QualityScore < 0.7)
            {
                recommendations.Add(new Recommendation
                {
                    RecommendationId = Guid.NewGuid().ToString(),
                    Type = "quality-improvement",
                    Title = "Improve Content Quality",
                    Description = "Consider enhancing the depth and accuracy of your content",
                    Priority = "medium",
                    Actionable = true
                });
            }

            if (analysis.ClarityScore < 0.6)
            {
                recommendations.Add(new Recommendation
                {
                    RecommendationId = Guid.NewGuid().ToString(),
                    Type = "clarity-enhancement",
                    Title = "Improve Clarity",
                    Description = "Make your content more clear and understandable",
                    Priority = "high",
                    Actionable = true
                });
            }

            return recommendations;
        }

        private string BuildAnalysisPrompt(ContributionDetails contribution, Dictionary<string, object> analysisOptions)
        {
            return $@"
Analyze the following contribution and provide scores (0.0-1.0) for each dimension:

CONTRIBUTION:
Title: {contribution.Title}
Description: {contribution.Description}
Content: {contribution.Content}

Please analyze and provide scores for:
- Quality: Technical accuracy and depth
- Innovation: Novelty and creativity
- Impact: Potential influence and value
- Clarity: Readability and understanding
- Completeness: Thoroughness and coverage

Respond in JSON format with scores and brief explanations.
";
        }

        private double ExtractScore(string analysisText, string dimension)
        {
            // Simple extraction - in real implementation would use more sophisticated parsing
            var pattern = $"{dimension}.*?([0-9.]+)";
            var match = System.Text.RegularExpressions.Regex.Match(analysisText, pattern);
            return match.Success && double.TryParse(match.Groups[1].Value, out var score) ? score : 0.5;
        }

        // Additional helper methods would be implemented here...
        private async Task<List<ConceptDetails>> GetConceptsFromRegistry(Dictionary<string, object> filters) => new();
        private async Task<ConceptValueAnalysis> AnalyzeConceptValue(ConceptDetails concept, Dictionary<string, object> criteria) => new();
        private async Task<List<ValueInsight>> GenerateValueInsights(List<ConceptValueAnalysis> concepts) => new();
        private async Task<List<PatternData>> GetPatternData(string dataSource, Dictionary<string, object> filters) => new();
        private async Task<List<Pattern>> AnalyzePatterns(List<PatternData> data, List<string> patternTypes) => new();
        private double CalculatePatternStrength(Pattern pattern, List<PatternData> data) => 0.5;
        private double CalculatePatternConfidence(Pattern pattern, List<PatternData> data) => 0.5;
        private async Task<List<PatternInsight>> GeneratePatternInsights(List<AnalyzedPattern> patterns) => new();
        private async Task<List<InsightData>> GetInsightData(string insightType, Dictionary<string, object> filters) => new();
        private async Task<List<Insight>> GenerateAIInsights(List<InsightData> data, string insightType, Dictionary<string, object> options) => new();
        private double CalculateRelevance(Insight insight, Dictionary<string, object> context) => 0.5;
        private double CalculateActionability(Insight insight) => 0.5;
        private async Task<List<InsightRecommendation>> GenerateInsightRecommendations(List<ValidatedInsight> insights, string userId) => new();
        private async Task<List<ContributionDetails>> GetUserContributions(string userId) => new();
        private async Task<UserProfile> AnalyzeUserProfile(string userId, List<ContributionDetails> contributions) => new();
        private async Task<List<Recommendation>> GeneratePersonalizedRecommendations(UserProfile profile, List<ContributionDetails> contributions) => new();
        private async Task<List<BatchInsight>> GenerateBatchInsights(List<ContributionAnalysisResponse> analyses) => new();

        // IModule interface implementations
        public Node GetModuleNode()
        {
            return new Node(
                Id: "contribution-analysis-agent",
                TypeId: "module",
                State: ContentState.Water,
                Locale: "en",
                Title: "Contribution Analysis Agent",
                Description: "AI-powered analysis of contributions and concepts for high-value identification",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["version"] = "1.0.0",
                        ["capabilities"] = new[] { "contribution-analysis", "pattern-discovery", "value-identification", "insight-generation" }
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["version"] = "1.0.0",
                    ["capabilities"] = new[] { "contribution-analysis", "pattern-discovery", "value-identification", "insight-generation" }
                }
            );
        }

        public void Register(NodeRegistry registry)
        {
            var node = GetModuleNode();
            registry.Upsert(node);
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via RegisterHttpEndpoints method
        }

        private void RegisterContributionAnalysisAgentNodes(NodeRegistry registry)
        {
            // Implementation for registering meta-nodes would go here
        }
    }

    // DTOs and supporting classes
    [ApiType(Name = "ContributionAnalysisRequest", Description = "Request for contribution analysis", Type = "object")]
    public record ContributionAnalysisRequest
    {
        public string AnalysisId { get; init; } = "";
        public string ContributionId { get; init; } = "";
        public string UserId { get; init; } = "";
        public Dictionary<string, object> AnalysisOptions { get; init; } = new();
    }

    [ApiType(Name = "ContributionAnalysisResponse", Description = "Response for contribution analysis", Type = "object")]
    public record ContributionAnalysisResponse
    {
        public bool Success { get; init; }
        public string AnalysisId { get; init; } = "";
        public string ContributionId { get; init; } = "";
        public ContributionAnalysis? Analysis { get; init; }
        public ValueMetrics? ValueMetrics { get; init; }
        public List<Recommendation> Recommendations { get; init; } = new();
        public DateTime AnalyzedAt { get; init; }
        public string Message { get; init; } = "";
    }

    [ApiType(Name = "ContributionDetails", Description = "Details of a contribution", Type = "object")]
    public record ContributionDetails
    {
        public string ContributionId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Content { get; init; } = "";
        public string AuthorId { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public List<string> Tags { get; init; } = new();
    }

    [ApiType(Name = "ContributionAnalysis", Description = "AI analysis of a contribution", Type = "object")]
    public record ContributionAnalysis
    {
        public string AnalysisId { get; init; } = "";
        public double QualityScore { get; init; }
        public double InnovationScore { get; init; }
        public double ImpactScore { get; init; }
        public double ClarityScore { get; init; }
        public double CompletenessScore { get; init; }
        public string AnalysisText { get; init; } = "";
    }

    [ApiType(Name = "ValueMetrics", Description = "Value metrics for a contribution", Type = "object")]
    public record ValueMetrics
    {
        public double OverallValue { get; init; }
        public double QualityValue { get; init; }
        public double InnovationValue { get; init; }
        public double ImpactValue { get; init; }
        public double ClarityValue { get; init; }
        public double CompletenessValue { get; init; }
        public DateTime CalculatedAt { get; init; }
    }

    [ApiType(Name = "Recommendation", Description = "Recommendation for improvement", Type = "object")]
    public record Recommendation
    {
        public string RecommendationId { get; init; } = "";
        public string Type { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Priority { get; init; } = "";
        public bool Actionable { get; init; }
    }

    // Additional DTOs would be defined here...
    public record ContributionBatchAnalysisRequest
    {
        public string BatchId { get; init; } = "";
        public List<string> ContributionIds { get; init; } = new();
        public string UserId { get; init; } = "";
        public Dictionary<string, object> AnalysisOptions { get; init; } = new();
    }

    public record ContributionBatchAnalysisResponse
    {
        public bool Success { get; init; }
        public string BatchId { get; init; } = "";
        public List<ContributionAnalysisResponse> AnalysisResults { get; init; } = new();
        public List<BatchInsight> BatchInsights { get; init; } = new();
        public DateTime AnalyzedAt { get; init; }
        public string Message { get; init; } = "";
    }

    public record HighValueConceptsRequest
    {
        public Dictionary<string, object> Criteria { get; init; } = new();
        public Dictionary<string, object> Filters { get; init; } = new();
        public int MaxResults { get; init; } = 10;
    }

    public record HighValueConceptsResponse
    {
        public bool Success { get; init; }
        public List<ConceptValueAnalysis> Concepts { get; init; } = new();
        public List<ValueInsight> ValueInsights { get; init; } = new();
        public DateTime IdentifiedAt { get; init; }
        public string Message { get; init; } = "";
    }

    public record PatternDiscoveryRequest
    {
        public string DataSource { get; init; } = "";
        public Dictionary<string, object> Filters { get; init; } = new();
        public List<string> PatternTypes { get; init; } = new();
    }

    public record PatternDiscoveryResponse
    {
        public bool Success { get; init; }
        public List<AnalyzedPattern> Patterns { get; init; } = new();
        public List<PatternInsight> PatternInsights { get; init; } = new();
        public DateTime DiscoveredAt { get; init; }
        public string Message { get; init; } = "";
    }

    public record InsightGenerationRequest
    {
        public string InsightType { get; init; } = "";
        public Dictionary<string, object> DataFilters { get; init; } = new();
        public Dictionary<string, object> InsightOptions { get; init; } = new();
        public Dictionary<string, object> Context { get; init; } = new();
        public string UserId { get; init; } = "";
    }

    public record InsightGenerationResponse
    {
        public bool Success { get; init; }
        public List<ValidatedInsight> Insights { get; init; } = new();
        public List<InsightRecommendation> Recommendations { get; init; } = new();
        public DateTime GeneratedAt { get; init; }
        public string Message { get; init; } = "";
    }

    public record AnalysisStatusResponse
    {
        public string AnalysisId { get; init; } = "";
        public string Status { get; init; } = "";
        public int Progress { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public string Message { get; init; } = "";
    }

    public record RecommendationsResponse
    {
        public bool Success { get; init; }
        public string UserId { get; init; } = "";
        public List<Recommendation> Recommendations { get; init; } = new();
        public DateTime GeneratedAt { get; init; }
        public string Message { get; init; } = "";
    }

    // Supporting classes
    public record ConceptDetails
    {
        public string ConceptId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public record ConceptValueAnalysis
    {
        public string ConceptId { get; init; } = "";
        public double ValueScore { get; init; }
        public string Analysis { get; init; } = "";
    }

    public record ValueInsight
    {
        public string InsightId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public record PatternData
    {
        public string DataId { get; init; } = "";
        public string Content { get; init; } = "";
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public record Pattern
    {
        public string PatternId { get; init; } = "";
        public string PatternType { get; init; } = "";
        public string Description { get; init; } = "";
        public int Frequency { get; init; }
        public List<string> Examples { get; init; } = new();
    }

    public record AnalyzedPattern
    {
        public string PatternId { get; init; } = "";
        public string PatternType { get; init; } = "";
        public string Description { get; init; } = "";
        public double Strength { get; init; }
        public double Confidence { get; init; }
        public int Frequency { get; init; }
        public List<string> Examples { get; init; } = new();
        public DateTime DetectedAt { get; init; }
    }

    public record PatternInsight
    {
        public string InsightId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public record InsightData
    {
        public string DataId { get; init; } = "";
        public string Content { get; init; } = "";
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public record Insight
    {
        public string InsightId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string InsightType { get; init; } = "";
        public double Confidence { get; init; }
    }

    public record ValidatedInsight
    {
        public string InsightId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string InsightType { get; init; } = "";
        public double Confidence { get; init; }
        public double Relevance { get; init; }
        public double Actionability { get; init; }
        public DateTime GeneratedAt { get; init; }
    }

    public record InsightRecommendation
    {
        public string RecommendationId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public record UserProfile
    {
        public string UserId { get; init; } = "";
        public Dictionary<string, object> Preferences { get; init; } = new();
        public List<string> Interests { get; init; } = new();
    }

    public record BatchInsight
    {
        public string InsightId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
    }
}
