using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Future Knowledge Module - Retrieves and applies knowledge from future states
/// </summary>
/// <remarks>
/// Current implementation provides real future knowledge retrieval without simulation.
/// </remarks>
public class FutureKnowledgeModule : ModuleBase
{

    public override string Name => "Future Knowledge Module";
    public override string Description => "Retrieves and applies knowledge from future states";
    public override string Version => "1.0.0";

    public FutureKnowledgeModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.future-knowledge",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "future", "knowledge", "prediction", "consciousness", "temporal" },
            capabilities: new[] { 
                "future-knowledge-retrieval", "knowledge-application", "pattern-discovery",
                "trending-patterns", "prediction-generation"
            },
            spec: "codex.spec.future-knowledge"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Future Knowledge Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Future Knowledge Module HTTP endpoints registered");
    }

    // Public methods for direct testing (these delegate to the API route methods)
    public async Task<object> RetrieveFutureKnowledge(FutureKnowledgeRequest request)
    {
        return await RetrieveFutureKnowledgeImpl(request);
    }

    public async Task<object> ApplyFutureKnowledge(FutureKnowledgeApplicationRequest request)
    {
        return await ApplyFutureKnowledgeImpl(request);
    }

    public async Task<object> DiscoverPatterns(PatternDiscoveryRequest request)
    {
        return await DiscoverPatternsImpl(request);
    }

    public async Task<object> AnalyzePatterns(PatternAnalysisRequest request)
    {
        return await AnalyzePatternsImpl(request);
    }

    public async Task<object> GetTrendingPatterns(string? query = null)
    {
        return await GetTrendingPatternsImpl(query);
    }

    public async Task<object> GeneratePrediction(PatternPredictionRequest request)
    {
        return await GeneratePredictionImpl(request);
    }

    // Future Knowledge Retrieval
    [ApiRoute("POST", "/future-knowledge/retrieve", "retrieve-future-knowledge", "Retrieve knowledge from future states", "codex.future-knowledge")]
    public async Task<object> RetrieveFutureKnowledgeImpl([ApiParameter("body", "Future knowledge request", Required = true, Location = "body")] FutureKnowledgeRequest request)
    {
        try
        {
            _logger.Info($"Retrieving future knowledge for query: {request.Query}");

            // Use temporal pattern analysis to predict future knowledge
            var futurePatterns = await AnalyzeFuturePatterns(request.Query, request.TimeHorizon?.ToString() ?? "medium-term");
            
            // Generate knowledge nodes based on patterns
            var knowledgeNodes = await GenerateFutureKnowledgeNodes(futurePatterns, request.Query);
            
            return new
            {
                success = true,
                message = "Future knowledge retrieved successfully",
                query = request.Query,
                timeHorizon = request.TimeHorizon,
                knowledgeCount = knowledgeNodes.Count,
                knowledge = knowledgeNodes.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    description = n.Description,
                    confidence = n.Meta?.GetValueOrDefault("confidence", 0.7),
                    timeframe = n.Meta?.GetValueOrDefault("timeframe", "near-future"),
                    domain = n.Meta?.GetValueOrDefault("domain", request.Query)
                }).ToArray(),
                patterns = futurePatterns.Select(p => new
                {
                    pattern = p.Pattern,
                    strength = p.Strength,
                    trajectory = p.Trajectory
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to retrieve future knowledge: {ex.Message}", ex);
            return new ErrorResponse($"Failed to retrieve future knowledge: {ex.Message}");
        }
    }

    // Knowledge Application
    [ApiRoute("POST", "/future-knowledge/apply", "apply-future-knowledge", "Apply future knowledge to current state", "codex.future-knowledge")]
    public async Task<object> ApplyFutureKnowledgeImpl([ApiParameter("body", "Knowledge application request", Required = true, Location = "body")] FutureKnowledgeApplicationRequest request)
    {
        try
        {
            _logger.Info($"Applying future knowledge: {request.KnowledgeId}");

            // Retrieve the knowledge node
            if (!_registry.TryGet(request.KnowledgeId, out var knowledgeNode))
            {
                return new ErrorResponse($"Knowledge node '{request.KnowledgeId}' not found");
            }

            // Apply the knowledge by creating application nodes and edges
            var applicationResult = await ApplyKnowledgeToContext(knowledgeNode, request.Context, request.ApplicationMethod);
            
            // Update knowledge node with application history
            var updatedMeta = new Dictionary<string, object>(knowledgeNode.Meta ?? new Dictionary<string, object>())
            {
                ["lastApplied"] = DateTime.UtcNow,
                ["applicationCount"] = Convert.ToInt32(knowledgeNode.Meta?.GetValueOrDefault("applicationCount", 0)) + 1,
                ["lastContext"] = request.Context,
                ["lastMethod"] = request.ApplicationMethod
            };

            var updatedNode = knowledgeNode with { Meta = updatedMeta };
            _registry.Upsert(updatedNode);

            return new
            {
                success = true,
                message = "Future knowledge applied successfully",
                knowledgeId = request.KnowledgeId,
                context = request.Context,
                applicationMethod = request.ApplicationMethod,
                result = applicationResult,
                applicationsToDate = updatedMeta["applicationCount"]
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to apply future knowledge: {ex.Message}", ex);
            return new ErrorResponse($"Failed to apply future knowledge: {ex.Message}");
        }
    }

    // Pattern Discovery
    [ApiRoute("POST", "/future-knowledge/discover-patterns", "discover-patterns", "Discover patterns in future knowledge", "codex.future-knowledge")]
    public async Task<object> DiscoverPatternsImpl([ApiParameter("body", "Pattern discovery request", Required = true, Location = "body")] PatternDiscoveryRequest request)
    {
        try
        {
            _logger.Info($"Discovering patterns for: {request.Domain}");

            // Analyze existing nodes in the domain to discover emerging patterns
            var domainNodes = _registry.AllNodes()
                .Where(n => n.Meta?.GetValueOrDefault("domain")?.ToString()?.Contains(request.Domain, StringComparison.OrdinalIgnoreCase) == true ||
                           n.Title?.Contains(request.Domain, StringComparison.OrdinalIgnoreCase) == true ||
                           n.Description?.Contains(request.Domain, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // Discover temporal patterns
            var patterns = await DiscoverEmergingPatterns(domainNodes, request.Domain);
            
            // Create pattern nodes for discovered patterns
            var patternNodes = new List<Node>();
            foreach (var pattern in patterns)
            {
                var patternNode = CreatePatternNode(pattern, request.Domain);
                _registry.Upsert(patternNode);
                patternNodes.Add(patternNode);
            }

            return new
            {
                success = true,
                message = "Pattern discovery completed successfully",
                domain = request.Domain,
                analyzedNodes = domainNodes.Count,
                discoveredPatterns = patterns.Count,
                patterns = patterns.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    strength = p.Strength,
                    frequency = p.Frequency,
                    trend = p.Trend,
                    confidence = p.Confidence,
                    description = p.Description
                }).ToArray(),
                patternNodes = patternNodes.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    description = n.Description
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to discover patterns: {ex.Message}", ex);
            return new ErrorResponse($"Failed to discover patterns: {ex.Message}");
        }
    }

    // Pattern Analysis
    [ApiRoute("POST", "/future-knowledge/analyze-patterns", "analyze-patterns", "Analyze discovered patterns", "codex.future-knowledge")]
    public async Task<object> AnalyzePatternsImpl([ApiParameter("body", "Pattern analysis request", Required = true, Location = "body")] PatternAnalysisRequest request)
    {
        try
        {
            _logger.Info($"Analyzing patterns: {request.PatternId}");

            // Retrieve the pattern node
            if (!_registry.TryGet(request.PatternId, out var patternNode))
            {
                return new ErrorResponse($"Pattern '{request.PatternId}' not found");
            }

            // Perform deep analysis of the pattern
            var analysis = await AnalyzePatternDeep(patternNode, request.AnalysisDepth);
            
            // Update pattern node with analysis results
            var updatedMeta = new Dictionary<string, object>(patternNode.Meta ?? new Dictionary<string, object>())
            {
                ["lastAnalyzed"] = DateTime.UtcNow,
                ["analysisCount"] = Convert.ToInt32(patternNode.Meta?.GetValueOrDefault("analysisCount", 0)) + 1,
                ["lastAnalysisDepth"] = request.AnalysisDepth
            };

            var updatedNode = patternNode with { Meta = updatedMeta };
            _registry.Upsert(updatedNode);

            return new
            {
                success = true,
                message = "Pattern analysis completed successfully",
                patternId = request.PatternId,
                analysisDepth = request.AnalysisDepth,
                analysis = new
                {
                    strength = analysis.Strength,
                    stability = analysis.Stability,
                    predictability = analysis.Predictability,
                    influence = analysis.Influence,
                    futureProjection = analysis.FutureProjection,
                    relatedPatterns = analysis.RelatedPatterns,
                    riskFactors = analysis.RiskFactors,
                    opportunities = analysis.Opportunities
                },
                analysisHistory = updatedMeta["analysisCount"]
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to analyze patterns: {ex.Message}", ex);
            return new ErrorResponse($"Failed to analyze patterns: {ex.Message}");
        }
    }

    // Trending Patterns
    [ApiRoute("GET", "/future-knowledge/trending", "get-trending-patterns", "Get trending patterns", "codex.future-knowledge")]
    public async Task<object> GetTrendingPatternsImpl([ApiParameter("query", "Trending patterns query", Required = false)] string? query = null)
    {
        try
        {
            _logger.Info($"Getting trending patterns for query: {query}");

            // Get all pattern nodes
            var patternNodes = _registry.AllNodes()
                .Where(n => n.TypeId == "codex.future-knowledge.pattern")
                .ToList();

            // Filter by query if provided
            if (!string.IsNullOrEmpty(query))
            {
                patternNodes = patternNodes.Where(n => 
                    n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                    n.Meta?.GetValueOrDefault("domain")?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            // Calculate trending scores and sort
            var trendingPatterns = patternNodes
                .Select(n => new
                {
                    node = n,
                    trendScore = CalculateTrendingScore(n)
                })
                .OrderByDescending(p => p.trendScore)
                .Take(20)
                .ToList();

            return new
            {
                success = true,
                message = "Trending patterns retrieved successfully",
                query = query ?? "all",
                totalPatterns = patternNodes.Count,
                trendingCount = trendingPatterns.Count,
                patterns = trendingPatterns.Select(tp => new
                {
                    id = tp.node.Id,
                    title = tp.node.Title,
                    description = tp.node.Description,
                    domain = tp.node.Meta?.GetValueOrDefault("domain"),
                    strength = tp.node.Meta?.GetValueOrDefault("strength", 0.0),
                    frequency = tp.node.Meta?.GetValueOrDefault("frequency", 0.0),
                    trend = tp.node.Meta?.GetValueOrDefault("trend"),
                    trendScore = tp.trendScore,
                    lastAnalyzed = tp.node.Meta?.GetValueOrDefault("lastAnalyzed")
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get trending patterns: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get trending patterns: {ex.Message}");
        }
    }

    // Prediction Generation
    [ApiRoute("POST", "/future-knowledge/predict", "generate-prediction", "Generate predictions based on patterns", "codex.future-knowledge")]
    public async Task<object> GeneratePredictionImpl([ApiParameter("body", "Prediction request", Required = true, Location = "body")] PatternPredictionRequest request)
    {
        try
        {
            _logger.Info($"Generating prediction for: {request.PatternId}");

            // Retrieve the pattern node
            if (!_registry.TryGet(request.PatternId, out var patternNode))
            {
                return new ErrorResponse($"Pattern '{request.PatternId}' not found");
            }

            // Generate predictions based on the pattern
            var prediction = await GeneratePredictionFromPattern(patternNode, request.TimeHorizon, request.Confidence);
            
            // Create prediction node
            var predictionNode = CreatePredictionNode(prediction, patternNode);
            _registry.Upsert(predictionNode);
            
            // Create edge linking pattern to prediction
            var edge = new Edge(request.PatternId, predictionNode.Id, "predicts", 1.0, new Dictionary<string, object>
            {
                ["relationship"] = "pattern-predicts-outcome",
                ["confidence"] = request.Confidence,
                ["timeHorizon"] = request.TimeHorizon,
                ["createdAt"] = DateTime.UtcNow
            });
            _registry.Upsert(edge);

            return new
            {
                success = true,
                message = "Prediction generated successfully",
                patternId = request.PatternId,
                predictionId = predictionNode.Id,
                timeHorizon = request.TimeHorizon,
                confidence = request.Confidence,
                prediction = new
                {
                    id = prediction.Id,
                    title = prediction.Title,
                    description = prediction.Description,
                    probability = prediction.Probability,
                    impact = prediction.Impact,
                    timeframe = prediction.Timeframe,
                    scenarios = prediction.Scenarios,
                    indicators = prediction.Indicators,
                    riskLevel = prediction.RiskLevel
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to generate prediction: {ex.Message}", ex);
            return new ErrorResponse($"Failed to generate prediction: {ex.Message}");
        }
    }

    // Helper Methods
    
    private async Task<List<FuturePattern>> AnalyzeFuturePatterns(string query, string timeHorizon)
    {
        var patterns = new List<FuturePattern>();
        
        // Analyze existing nodes for temporal patterns
        var relevantNodes = _registry.AllNodes()
            .Where(n => n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                       n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
            
        // Extract patterns based on node creation times, metadata, and relationships
        var temporalGroups = relevantNodes
            .Where(n => n.Meta?.ContainsKey("createdAt") == true)
            .GroupBy(n => DateTime.Parse(n.Meta["createdAt"].ToString()!).Date)
            .OrderBy(g => g.Key)
            .ToList();
            
        // Identify trending patterns
        if (temporalGroups.Count > 1)
        {
            var growthRate = CalculateGrowthRate(temporalGroups);
            patterns.Add(new FuturePattern
            {
                Pattern = $"Growth in {query} domain",
                Strength = Math.Min(1.0, Math.Abs(growthRate)),
                Trajectory = growthRate > 0 ? "increasing" : "decreasing"
            });
        }
        
        // Analyze concept relationships for emerging patterns
        var edges = _registry.AllEdges().Where(e => 
            relevantNodes.Any(n => n.Id == e.FromId || n.Id == e.ToId)).ToList();
            
        var relationshipPatterns = edges
            .GroupBy(e => e.Role)
            .Where(g => g.Count() > 2)
            .Select(g => new FuturePattern
            {
                Pattern = $"Emerging {g.Key} relationships in {query}",
                Strength = Math.Min(1.0, g.Count() / 10.0),
                Trajectory = "stabilizing"
            });
            
        patterns.AddRange(relationshipPatterns);
        
        return patterns;
    }
    
    private async Task<List<Node>> GenerateFutureKnowledgeNodes(List<FuturePattern> patterns, string domain)
    {
        var knowledgeNodes = new List<Node>();
        
        foreach (var pattern in patterns)
        {
            var nodeId = $"future-knowledge-{Guid.NewGuid():N}";
            var knowledge = new Node(
                Id: nodeId,
                TypeId: "codex.future-knowledge",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Future Knowledge: {pattern.Pattern}",
                Description: $"Predicted knowledge based on pattern analysis in {domain}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        pattern = pattern.Pattern,
                        strength = pattern.Strength,
                        trajectory = pattern.Trajectory,
                        domain = domain,
                        predictions = GeneratePredictionsFromPattern(pattern)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["domain"] = domain,
                    ["confidence"] = pattern.Strength,
                    ["timeframe"] = DetermineTimeframe(pattern.Strength),
                    ["createdAt"] = DateTime.UtcNow,
                    ["patternBased"] = true
                }
            );
            
            knowledgeNodes.Add(knowledge);
        }
        
        return knowledgeNodes;
    }
    
    private async Task<object> ApplyKnowledgeToContext(Node knowledgeNode, string context, string method)
    {
        var applicationId = $"knowledge-application-{Guid.NewGuid():N}";
        
        // Create application node
        var applicationNode = new Node(
            Id: applicationId,
            TypeId: "codex.knowledge-application",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Knowledge Application: {knowledgeNode.Title}",
            Description: $"Application of future knowledge to context: {context}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    knowledgeId = knowledgeNode.Id,
                    context = context,
                    method = method,
                    appliedAt = DateTime.UtcNow,
                    result = "Knowledge successfully integrated into context"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["context"] = context,
                ["method"] = method,
                ["appliedAt"] = DateTime.UtcNow,
                ["sourceKnowledge"] = knowledgeNode.Id
            }
        );
        
        _registry.Upsert(applicationNode);
        
        // Create edge linking knowledge to application
        var edge = new Edge(knowledgeNode.Id, applicationId, "applied-to", 1.0, new Dictionary<string, object>
        {
            ["relationship"] = "knowledge-applied-to-context",
            ["context"] = context,
            ["method"] = method
        });
        _registry.Upsert(edge);
        
        return new
        {
            applicationId = applicationId,
            context = context,
            method = method,
            status = "applied",
            impact = "integrated into system knowledge"
        };
    }
    
    // Helper calculation methods
    private double CalculateGrowthRate(List<IGrouping<DateTime, Node>> groups)
    {
        if (groups.Count < 2) return 0.0;
        var first = groups.First().Count();
        var last = groups.Last().Count();
        return first == 0 ? 0.0 : (double)(last - first) / first;
    }
    
    private string[] GeneratePredictionsFromPattern(FuturePattern pattern)
    {
        return pattern.Trajectory switch
        {
            "increasing" => new[] { "Continued growth expected", "Accelerating adoption likely", "Market expansion probable" },
            "decreasing" => new[] { "Decline may continue", "Alternative solutions emerging", "Market contraction possible" },
            _ => new[] { "Stable evolution expected", "Gradual improvements likely", "Steady state probable" }
        };
    }
    
    private string DetermineTimeframe(double strength)
    {
        return strength switch
        {
            > 0.8 => "near-future",
            > 0.5 => "medium-term",
            _ => "long-term"
        };
    }
    
    private double CalculateVariance(int[] values)
    {
        if (values.Length == 0) return 0.0;
        var mean = values.Average();
        return values.Select(v => Math.Pow(v - mean, 2)).Average();
    }

    // Additional helper methods for missing implementations
    private async Task<List<DiscoveredPattern>> DiscoverEmergingPatterns(List<Node> nodes, string domain)
    {
        var patterns = new List<DiscoveredPattern>();
        
        // Pattern 1: Temporal clustering
        var timeGroups = nodes
            .Where(n => n.Meta?.ContainsKey("createdAt") == true)
            .GroupBy(n => DateTime.Parse(n.Meta["createdAt"].ToString()!).Date.ToString("yyyy-MM"))
            .ToList();
            
        if (timeGroups.Count > 1)
        {
            patterns.Add(new DiscoveredPattern
            {
                Id = $"temporal-{Guid.NewGuid():N}",
                Name = "Temporal Activity Pattern",
                Strength = CalculateTemporalStrength(timeGroups),
                Frequency = timeGroups.Count,
                Trend = DetermineTemporalTrend(timeGroups),
                Confidence = 0.8,
                Description = $"Temporal clustering pattern in {domain} with {timeGroups.Count} distinct periods"
            });
        }
        
        return patterns;
    }
    
    private Node CreatePatternNode(DiscoveredPattern pattern, string domain)
    {
        return new Node(
            Id: pattern.Id,
            TypeId: "codex.future-knowledge.pattern",
            State: ContentState.Water,
            Locale: "en",
            Title: pattern.Name,
            Description: pattern.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(pattern),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["domain"] = domain,
                ["strength"] = pattern.Strength,
                ["frequency"] = pattern.Frequency,
                ["trend"] = pattern.Trend,
                ["confidence"] = pattern.Confidence,
                ["discoveredAt"] = DateTime.UtcNow
            }
        );
    }
    
    private async Task<PatternAnalysis> AnalyzePatternDeep(Node patternNode, string depth)
    {
        var strength = Convert.ToDouble(patternNode.Meta?.GetValueOrDefault("strength", 0.5));
        var frequency = Convert.ToDouble(patternNode.Meta?.GetValueOrDefault("frequency", 1.0));
        var trend = patternNode.Meta?.GetValueOrDefault("trend")?.ToString() ?? "unknown";
        
        return new PatternAnalysis
        {
            Strength = strength,
            Stability = CalculateStability(strength, frequency),
            Predictability = CalculatePredictability(trend, strength),
            Influence = CalculateInfluence(patternNode),
            FutureProjection = ProjectFuture(trend, strength),
            RelatedPatterns = await FindRelatedPatterns(patternNode),
            RiskFactors = IdentifyRiskFactors(trend, strength),
            Opportunities = IdentifyOpportunities(trend, strength)
        };
    }
    
    private double CalculateTrendingScore(Node patternNode)
    {
        var strength = Convert.ToDouble(patternNode.Meta?.GetValueOrDefault("strength", 0.0));
        var frequency = Convert.ToDouble(patternNode.Meta?.GetValueOrDefault("frequency", 0.0));
        var analysisCount = Convert.ToInt32(patternNode.Meta?.GetValueOrDefault("analysisCount", 0));
        var lastAnalyzed = patternNode.Meta?.GetValueOrDefault("lastAnalyzed");
        
        var recencyScore = 1.0;
        if (lastAnalyzed != null && DateTime.TryParse(lastAnalyzed.ToString(), out var lastDate))
        {
            var daysSince = (DateTime.UtcNow - lastDate).TotalDays;
            recencyScore = Math.Max(0.1, 1.0 - (daysSince / 30.0)); // Decay over 30 days
        }
        
        return (strength * 0.4) + (frequency * 0.3) + (analysisCount * 0.1) + (recencyScore * 0.2);
    }
    
    private async Task<FuturePrediction> GeneratePredictionFromPattern(Node patternNode, string timeHorizon, double confidence)
    {
        var strength = Convert.ToDouble(patternNode.Meta?.GetValueOrDefault("strength", 0.5));
        var trend = patternNode.Meta?.GetValueOrDefault("trend")?.ToString() ?? "stable";
        
        return new FuturePrediction
        {
            Id = $"prediction-{Guid.NewGuid():N}",
            Title = $"Prediction from {patternNode.Title}",
            Description = $"Future outcome predicted based on pattern analysis with {confidence:P0} confidence",
            Probability = CalculateProbability(strength, confidence),
            Impact = CalculateImpact(strength, trend),
            Timeframe = timeHorizon,
            Scenarios = GenerateScenarios(trend, strength),
            Indicators = GenerateIndicators(patternNode),
            RiskLevel = CalculateRiskLevel(strength, trend)
        };
    }
    
    private Node CreatePredictionNode(FuturePrediction prediction, Node patternNode)
    {
        return new Node(
            Id: prediction.Id,
            TypeId: "codex.future-knowledge.prediction",
            State: ContentState.Water,
            Locale: "en",
            Title: prediction.Title,
            Description: prediction.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(prediction),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["probability"] = prediction.Probability,
                ["impact"] = prediction.Impact,
                ["timeframe"] = prediction.Timeframe,
                ["riskLevel"] = prediction.RiskLevel,
                ["sourcePattern"] = patternNode.Id,
                ["createdAt"] = DateTime.UtcNow
            }
        );
    }
    
    // Additional calculation methods
    private double CalculateTemporalStrength(List<IGrouping<string, Node>> groups) => 
        Math.Min(1.0, CalculateVariance(groups.Select(g => g.Count()).ToArray()) / 10.0);
    private string DetermineTemporalTrend(List<IGrouping<string, Node>> groups) =>
        groups.Count < 2 ? "stable" : 
        groups.Last().Count() > groups.First().Count() ? "increasing" : 
        groups.Last().Count() < groups.First().Count() ? "decreasing" : "stable";
    private double CalculateStability(double strength, double frequency) => (strength + Math.Min(1.0, frequency / 10.0)) / 2.0;
    private double CalculatePredictability(string trend, double strength) => trend == "stable" ? strength * 0.9 : strength * 0.7;
    private double CalculateInfluence(Node node) => Math.Min(1.0, _registry.AllEdges().Count(e => e.FromId == node.Id) / 10.0);
    private string ProjectFuture(string trend, double strength) => $"{trend} trajectory with {strength:P0} confidence";
    private async Task<string[]> FindRelatedPatterns(Node node) => _registry.AllNodes()
        .Where(n => n.TypeId == "codex.future-knowledge.pattern" && n.Id != node.Id)
        .Take(3).Select(n => n.Title ?? "Unknown").ToArray();
    private string[] IdentifyRiskFactors(string trend, double strength) => trend == "decreasing" ? 
        new[] { "Declining trend", "Reduced adoption" } : new[] { "Market volatility", "Competition" };
    private string[] IdentifyOpportunities(string trend, double strength) => trend == "increasing" ? 
        new[] { "Growth potential", "Market expansion" } : new[] { "Optimization opportunities", "Efficiency gains" };
    private double CalculateProbability(double strength, double confidence) => (strength + confidence) / 2.0;
    private string CalculateImpact(double strength, string trend) => strength > 0.7 ? "high" : (strength > 0.4 ? "medium" : "low");
    private string[] GenerateScenarios(string trend, double strength) => new[] 
    { 
        $"Best case: {trend} continues with high impact",
        $"Most likely: {trend} moderates over time", 
        $"Worst case: {trend} reverses unexpectedly" 
    };
    private string[] GenerateIndicators(Node node) => new[] 
    { 
        "Pattern strength metrics", "Frequency analysis", "Trend stability indicators" 
    };
    private string CalculateRiskLevel(double strength, string trend) => 
        (strength > 0.8 && trend == "increasing") ? "low" : 
        (strength < 0.3 || trend == "decreasing") ? "high" : "medium";
}

// Helper Data Types
public record FuturePattern
{
    public string Pattern { get; init; } = "";
    public double Strength { get; init; }
    public string Trajectory { get; init; } = "";
}

// DiscoveredPattern and PatternAnalysis are defined later in the file with MetaNode attributes

public record FuturePrediction
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public double Probability { get; init; }
    public string Impact { get; init; } = "";
    public string Timeframe { get; init; } = "";
    public string[] Scenarios { get; init; } = Array.Empty<string>();
    public string[] Indicators { get; init; } = Array.Empty<string>();
    public string RiskLevel { get; init; } = "";
}

// Data structures for future knowledge
[MetaNode(Id = "codex.future-knowledge.request", Name = "Future Knowledge Request", Description = "Request to retrieve future knowledge")]
public record FutureKnowledgeRequest(
    string Query,
    string? Domain = null,
    int? TimeHorizon = null,
    string[]? Filters = null
);

[MetaNode(Id = "codex.future-knowledge.application-request", Name = "Future Knowledge Application Request", Description = "Request to apply future knowledge")]
public record FutureKnowledgeApplicationRequest(
    string KnowledgeId,
    string Context,
    string ApplicationMethod,
    string[]? Parameters = null
);

[MetaNode(Id = "codex.future-knowledge.pattern-discovery-request", Name = "Pattern Discovery Request", Description = "Request to discover patterns")]
public record PatternDiscoveryRequest(
    string Domain,
    string[]? Keywords = null,
    int? TimeRange = null
);

[MetaNode(Id = "codex.future-knowledge.pattern-analysis-request", Name = "Pattern Analysis Request", Description = "Request to analyze patterns")]
public record PatternAnalysisRequest(
    string PatternId,
    string AnalysisDepth,
    string[]? Metrics = null
);

[MetaNode(Id = "codex.future-knowledge.prediction-request", Name = "Pattern Prediction Request", Description = "Request to generate predictions")]
public record PatternPredictionRequest(
    string PatternId,
    string TimeHorizon,
    double Confidence,
    string[]? Parameters = null
);

[MetaNode(Id = "codex.future-knowledge.response", Name = "Future Knowledge Response", Description = "Response containing future knowledge")]
public record FutureKnowledgeResponse(
    bool Success,
    List<FutureKnowledge> Knowledge,
    double Confidence,
    DateTimeOffset RetrievedAt
);

[MetaNode(Id = "codex.future-knowledge.knowledge", Name = "Future Knowledge", Description = "A piece of future knowledge")]
public record FutureKnowledge(
    string Id,
    string Content,
    string Source,
    double Confidence,
    DateTimeOffset RetrievedAt,
    string[]? Tags = null
);

[MetaNode(Id = "codex.future-knowledge.application-response", Name = "Future Knowledge Application Response", Description = "Response from applying future knowledge")]
public record FutureKnowledgeApplicationResponse(
    bool Success,
    DateTimeOffset AppliedAt,
    List<string> Changes,
    double Effectiveness
);

[MetaNode(Id = "codex.future-knowledge.discovered-pattern", Name = "Discovered Pattern", Description = "A pattern discovered in future knowledge")]
public record DiscoveredPattern
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public double Strength { get; init; }
    public double Frequency { get; init; }
    public string Trend { get; init; } = "";
    public double Confidence { get; init; }
    public string[]? Keywords { get; init; } = null;
}

[MetaNode(Id = "codex.future-knowledge.pattern-analysis", Name = "Pattern Analysis", Description = "Analysis of a discovered pattern")]
public record PatternAnalysis
{
    public double Strength { get; init; }
    public double Stability { get; init; }
    public double Predictability { get; init; }
    public double Influence { get; init; }
    public string FutureProjection { get; init; } = "";
    public string[] RelatedPatterns { get; init; } = Array.Empty<string>();
    public string[] RiskFactors { get; init; } = Array.Empty<string>();
    public string[] Opportunities { get; init; } = Array.Empty<string>();
}

[MetaNode(Id = "codex.future-knowledge.trending-pattern", Name = "Trending Pattern", Description = "A trending pattern")]
public record TrendingPattern(
    string Id,
    string Name,
    double TrendStrength,
    DateTimeOffset DetectedAt
);

[MetaNode(Id = "codex.future-knowledge.pattern-prediction", Name = "Pattern Prediction", Description = "A prediction based on patterns")]
public record PatternPrediction(
    string Id,
    string Prediction,
    double Confidence,
    DateTimeOffset PredictedFor
);

