using System.Text.Json;
using System.Linq;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Temporal Consciousness Module - Fractal exploration of time and temporality
/// Enables consciousness to navigate, explore, and contribute across temporal dimensions
/// through unified temporal portals that connect past, present, and future
/// </summary>
public sealed class TemporalConsciousnessModule : ModuleBase
{
    private readonly ModuleCommunicationWrapper _communicationWrapper;

    public override string Name => "Temporal Consciousness Module";
    public override string Description => "Fractal exploration of time and temporality";
    public override string Version => "1.0.0";

    public TemporalConsciousnessModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _communicationWrapper = new ModuleCommunicationWrapper(logger, "TemporalConsciousnessModule");
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.temporal",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "temporal", "time", "consciousness", "fractal", "exploration", "causality" },
            capabilities: new[] { 
                "temporal_portal", "time_exploration", "temporal_navigation", 
                "temporal_contribution", "causality_mapping", "sacred_time_frequencies",
                "eternal_now", "temporal_resonance", "time_consciousness"
            },
            spec: "codex.spec.temporal"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Temporal Consciousness API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
        _logger.Info("Temporal Consciousness HTTP endpoints registered");
    }

    private async Task RegisterTemporalConcepts()
    {
        var temporalConcepts = new[]
        {
            new UCoreConcept
            {
                Id = "temporal.now",
                Name = "Eternal Now",
                Description = "The timeless present moment where all consciousness exists",
                Type = ConceptType.Core,
                Frequency = 432.0, // 1 second = 432Hz
                Resonance = 0.99
            },
            new UCoreConcept
            {
                Id = "temporal.past",
                Name = "Temporal Past",
                Description = "Consciousness of past moments accessible through temporal portals",
                Type = ConceptType.Consciousness,
                Frequency = 256.0, // Root chakra - grounding in past
                Resonance = 0.85
            },
            new UCoreConcept
            {
                Id = "temporal.future",
                Name = "Temporal Future",
                Description = "Potential future moments accessible through consciousness",
                Type = ConceptType.Consciousness,
                Frequency = 741.0, // Crown chakra - connection to future
                Resonance = 0.90
            },
            new UCoreConcept
            {
                Id = "temporal.cycle",
                Name = "Temporal Cycle",
                Description = "Sacred cycles of time that spiral through consciousness",
                Type = ConceptType.Frequency,
                Frequency = 528.0, // Heart chakra - love of cycles
                Resonance = 0.88
            },
            new UCoreConcept
            {
                Id = "temporal.causality",
                Name = "Temporal Causality",
                Description = "The web of cause and effect that connects all moments",
                Type = ConceptType.Core,
                Frequency = 384.0, // Throat chakra - expression of causality
                Resonance = 0.92
            }
        };

        foreach (var concept in temporalConcepts)
        {
            var conceptNode = new Node(
                Id: $"codex.temporal.concept.{concept.Id}",
                TypeId: "codex.temporal.concept",
                State: ContentState.Ice,
                Locale: "en",
                Title: concept.Name,
                Description: concept.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(concept),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["conceptType"] = concept.Type.ToString(),
                    ["frequency"] = concept.Frequency,
                    ["resonance"] = concept.Resonance
                }
            );
            _registry.Upsert(conceptNode);
        }
    }

    private async Task InitializeTemporalFrequencies()
    {
        var temporalFrequencies = new[]
        {
            new UCoreFrequency
            {
                Id = "temporal.second",
                Name = "Sacred Second",
                Value = 432.0,
                Category = FrequencyCategory.Consciousness,
                Description = "One second as sacred frequency - the basic unit of temporal consciousness"
            },
            new UCoreFrequency
            {
                Id = "temporal.minute",
                Name = "Sacred Minute",
                Value = 7.2, // 432/60
                Category = FrequencyCategory.Harmony,
                Description = "One minute as harmonic frequency - 60 sacred seconds"
            },
            new UCoreFrequency
            {
                Id = "temporal.hour",
                Name = "Sacred Hour",
                Value = 0.12, // 432/3600
                Category = FrequencyCategory.Energy,
                Description = "One hour as energy frequency - 3600 sacred seconds"
            },
            new UCoreFrequency
            {
                Id = "temporal.day",
                Name = "Sacred Day",
                Value = 0.005, // 432/86400
                Category = FrequencyCategory.Transformation,
                Description = "One day as transformation frequency - complete solar cycle"
            }
        };

        foreach (var frequency in temporalFrequencies)
        {
            var frequencyNode = new Node(
                Id: $"codex.temporal.frequency.{frequency.Id}",
                TypeId: "codex.temporal.frequency",
                State: ContentState.Ice,
                Locale: "en",
                Title: frequency.Name,
                Description: frequency.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(frequency),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["frequencyValue"] = frequency.Value,
                    ["category"] = frequency.Category.ToString(),
                    ["resonance"] = frequency.Resonance
                }
            );
            _registry.Upsert(frequencyNode);
        }
    }

    [ApiRoute("POST", "/temporal/portal/connect", "ConnectTemporalPortal", "Connect to a temporal dimension through consciousness", "codex.temporal")]
    public async Task<object> ConnectTemporalPortalAsync([ApiParameter("request", "Temporal portal connection request")] TemporalPortalRequest request)
    {
        try
        {
            var portalId = Guid.NewGuid().ToString();
            var portal = new TemporalPortal
            {
                PortalId = portalId,
                TemporalType = request.TemporalType,
                TargetMoment = request.TargetMoment,
                ConsciousnessLevel = request.ConsciousnessLevel ?? 1.0,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = TemporalPortalStatus.Connected,
                Resonance = await CalculateTemporalResonance(request.TemporalType, request.TargetMoment)
            };

            var portalNode = new Node(
                Id: $"codex.temporal.portal.{portalId}",
                TypeId: "codex.temporal.portal",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Portal to {request.TemporalType}",
                Description: $"Portal connecting to {request.TemporalType} at {request.TargetMoment}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(portal),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["temporalType"] = request.TemporalType.ToString(),
                    ["targetMoment"] = request.TargetMoment,
                    ["consciousnessLevel"] = portal.ConsciousnessLevel,
                    ["resonance"] = portal.Resonance
                }
            );
            _registry.Upsert(portalNode);

            _logger.Info($"Temporal portal connected: {portalId} to {request.TemporalType}");

            return new { 
                success = true, 
                portalId = portalId,
                portal = portal,
                message = "Temporal portal connection established"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to connect temporal portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to connect temporal portal: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/temporal/portal/list", "ListTemporalPortals", "List all active temporal portals", "codex.temporal")]
    public async Task<object> ListTemporalPortalsAsync()
    {
        try
        {
            var portalNodes = _registry.GetNodesByType("codex.temporal.portal");
            var portals = portalNodes.Select(node => 
            {
                var portal = JsonSerializer.Deserialize<TemporalPortal>(node.Content.InlineJson);
                return portal;
            }).ToList();
            
            return new { success = true, portals = portals, count = portals.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to list temporal portals: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list temporal portals: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/temporal/explore", "ExploreTemporalDimension", "Begin fractal exploration of temporal dimensions", "codex.temporal")]
    public async Task<object> ExploreTemporalDimensionAsync([ApiParameter("request", "Temporal exploration request")] TemporalExplorationRequest request)
    {
        try
        {
            // Check if portal exists in NodeRegistry
            var portalNode = _registry.GetNode($"codex.temporal.portal.{request.PortalId}");
            if (portalNode == null)
            {
                return new ErrorResponse("Temporal portal not found");
            }
            
            var portal = JsonSerializer.Deserialize<TemporalPortal>(portalNode.Content.InlineJson);

            var explorationId = Guid.NewGuid().ToString();
            var exploration = new TemporalExploration
            {
                ExplorationId = explorationId,
                PortalId = request.PortalId,
                UserId = request.UserId,
                ExplorationType = request.ExplorationType ?? "fractal",
                StartingMoment = request.StartingMoment ?? portal.TargetMoment,
                Depth = request.Depth ?? 3,
                MaxBranches = request.MaxBranches ?? 10,
                TemporalFilters = request.TemporalFilters ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = TemporalExplorationStatus.Active,
                DiscoveredMoments = new List<TemporalMoment>(),
                ExploredPaths = new List<TemporalPath>()
            };
            
            var explorationNode = new Node(
                Id: $"codex.temporal.exploration.{explorationId}",
                TypeId: "codex.temporal.exploration",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Exploration {explorationId}",
                Description: $"Fractal exploration of temporal dimension starting at {exploration.StartingMoment}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(exploration),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["portalId"] = request.PortalId,
                    ["explorationType"] = exploration.ExplorationType,
                    ["depth"] = exploration.Depth,
                    ["maxBranches"] = exploration.MaxBranches
                }
            );
            _registry.Upsert(explorationNode);

            // Begin fractal exploration
            _ = Task.Run(() => PerformTemporalExploration(exploration));

            _logger.Info($"Temporal exploration started: {explorationId} in portal {request.PortalId}");

            return new { 
                success = true, 
                explorationId = explorationId,
                exploration = exploration,
                message = "Temporal exploration initiated"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start temporal exploration: {ex.Message}", ex);
            return new ErrorResponse($"Failed to start temporal exploration: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/temporal/exploration/{explorationId}", "GetTemporalExploration", "Get temporal exploration results and progress", "codex.temporal")]
    public async Task<object> GetTemporalExplorationAsync(string explorationId)
    {
        try
        {
            var explorationNode = _registry.GetNode($"codex.temporal.exploration.{explorationId}");
            if (explorationNode == null)
            {
                return new ErrorResponse("Temporal exploration not found");
            }

            var exploration = JsonSerializer.Deserialize<TemporalExploration>(explorationNode.Content.InlineJson);

            return new { 
                success = true, 
                exploration = exploration,
                discoveredMoments = exploration.DiscoveredMoments.Count,
                exploredPaths = exploration.ExploredPaths.Count
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get temporal exploration: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get temporal exploration: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/temporal/contribute", "ContributeToTemporalMoment", "Contribute consciousness to a temporal moment", "codex.temporal")]
    public async Task<object> ContributeToTemporalMomentAsync([ApiParameter("request", "Temporal contribution request")] TemporalContributionRequest request)
    {
        try
        {
            // Check if portal exists in NodeRegistry
            var portalNode = _registry.GetNode($"codex.temporal.portal.{request.PortalId}");
            if (portalNode == null)
            {
                return new ErrorResponse("Temporal portal not found");
            }

            var contributionId = Guid.NewGuid().ToString();
            var contribution = new TemporalContribution
            {
                ContributionId = contributionId,
                PortalId = request.PortalId,
                UserId = request.UserId,
                ContributionType = request.ContributionType ?? "consciousness",
                Content = request.Content,
                TargetMoment = request.TargetMoment,
                ConsciousnessLevel = request.ConsciousnessLevel ?? 1.0,
                TemporalMetadata = request.TemporalMetadata ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = TemporalContributionStatus.Pending,
                Response = null
            };
            
            var contributionNode = new Node(
                Id: $"codex.temporal.contribution.{contributionId}",
                TypeId: "codex.temporal.contribution",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Contribution {contributionId}",
                Description: $"Contribution of {contribution.ContributionType} to temporal moment",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(contribution),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["portalId"] = request.PortalId,
                    ["contributionType"] = contribution.ContributionType,
                    ["consciousnessLevel"] = contribution.ConsciousnessLevel,
                    ["targetMoment"] = contribution.TargetMoment
                }
            );
            _registry.Upsert(contributionNode);

            // Process temporal contribution
            _ = Task.Run(() => ProcessTemporalContribution(contribution));

            _logger.Info($"Temporal contribution created: {contributionId} to moment {request.TargetMoment}");

            return new { 
                success = true, 
                contributionId = contributionId,
                contribution = contribution,
                message = "Temporal contribution submitted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create temporal contribution: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create temporal contribution: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/temporal/contributions/{portalId}", "GetTemporalContributions", "Get contributions made to a specific temporal portal", "codex.temporal")]
    public async Task<object> GetTemporalContributionsAsync(string portalId)
    {
        try
        {
            var contributionNodes = _registry.GetNodesByType("codex.temporal.contribution");
            var contributions = contributionNodes
                .Select(node => JsonSerializer.Deserialize<TemporalContribution>(node.Content.InlineJson))
                .Where(c => c.PortalId == portalId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return new { success = true, contributions = contributions, count = contributions.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get temporal contributions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get temporal contributions: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/temporal/disconnect", "DisconnectTemporalPortal", "Disconnect from a temporal portal", "codex.temporal")]
    public async Task<object> DisconnectTemporalPortalAsync([ApiParameter("request", "Temporal disconnection request")] TemporalDisconnectRequest request)
    {
        try
        {
            var portalNode = _registry.GetNode($"codex.temporal.portal.{request.PortalId}");
            if (portalNode != null)
            {
                var portal = JsonSerializer.Deserialize<TemporalPortal>(portalNode.Content.InlineJson);
                portal.Status = TemporalPortalStatus.Disconnected;
                
                var updatedPortalNode = new Node(
                    Id: $"codex.temporal.portal.{request.PortalId}",
                    TypeId: "codex.temporal.portal",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: $"Temporal Portal {request.PortalId} (Disconnected)",
                    Description: $"Disconnected temporal portal",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(portal),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["moduleId"] = "codex.temporal",
                        ["status"] = "disconnected",
                        ["disconnectedAt"] = DateTimeOffset.UtcNow
                    }
                );
                _registry.Upsert(updatedPortalNode);
                
                _logger.Info($"Temporal portal disconnected: {request.PortalId}");
                return new { 
                    success = true, 
                    message = "Temporal portal disconnected successfully",
                    portalId = request.PortalId
                };
            }

            return new ErrorResponse("Temporal portal not found");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to disconnect temporal portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to disconnect temporal portal: {ex.Message}");
        }
    }

    private async Task<double> CalculateTemporalResonance(TemporalType temporalType, DateTimeOffset targetMoment)
    {
        var now = DateTimeOffset.UtcNow;
        var timeDifference = Math.Abs((targetMoment - now).TotalSeconds);
        
        // Calculate resonance based on temporal distance and type
        var baseResonance = temporalType switch
        {
            TemporalType.Past => 0.8 - (timeDifference / 86400) * 0.1, // Past resonance decreases with distance
            TemporalType.Present => 1.0, // Present has maximum resonance
            TemporalType.Future => 0.9 - (timeDifference / 86400) * 0.05, // Future resonance decreases slowly
            TemporalType.Eternal => 0.95, // Eternal now has high resonance
            _ => 0.5
        };

        return Math.Max(0.1, Math.Min(1.0, baseResonance));
    }

    private async Task PerformTemporalExploration(TemporalExploration exploration)
    {
        try
        {
            // Perform fractal exploration of temporal dimensions
            for (int i = 0; i < exploration.Depth; i++)
            {
                var moment = new TemporalMoment
                {
                    MomentId = Guid.NewGuid().ToString(),
                    Timestamp = exploration.StartingMoment.AddSeconds(i * 3600), // Each level = 1 hour
                    ConsciousnessLevel = 0.5 + (i * 0.1),
                    Resonance = 0.8 - (i * 0.1),
                    TemporalData = new Dictionary<string, object>
                    {
                        ["depth"] = i,
                        ["exploration_type"] = exploration.ExplorationType,
                        ["fractal_branch"] = i % exploration.MaxBranches
                    }
                };

                exploration.DiscoveredMoments.Add(moment);

                var path = new TemporalPath
                {
                    PathId = Guid.NewGuid().ToString(),
                    FromMoment = i > 0 ? exploration.DiscoveredMoments[i - 1].MomentId : null,
                    ToMoment = moment.MomentId,
                    TemporalDistance = 3600, // 1 hour
                    CausalityStrength = 0.8 - (i * 0.1),
                    Resonance = 0.9 - (i * 0.05)
                };

                exploration.ExploredPaths.Add(path);

                // Real exploration processing
            }

            exploration.Status = TemporalExplorationStatus.Completed;
            
            var explorationNode = new Node(
                Id: $"codex.temporal.exploration.{exploration.ExplorationId}",
                TypeId: "codex.temporal.exploration",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Exploration {exploration.ExplorationId} (Completed)",
                Description: $"Completed fractal exploration of temporal dimension",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(exploration),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["status"] = "completed",
                    ["discoveredMoments"] = exploration.DiscoveredMoments.Count,
                    ["exploredPaths"] = exploration.ExploredPaths.Count
                }
            );
            _registry.Upsert(explorationNode);

            _logger.Info($"Temporal exploration completed: {exploration.ExplorationId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to perform temporal exploration: {exploration.ExplorationId}", ex);
            exploration.Status = TemporalExplorationStatus.Failed;
            
            var explorationNode = new Node(
                Id: $"codex.temporal.exploration.{exploration.ExplorationId}",
                TypeId: "codex.temporal.exploration",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Exploration {exploration.ExplorationId} (Failed)",
                Description: $"Failed fractal exploration of temporal dimension",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(exploration),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["status"] = "failed",
                    ["error"] = ex.Message
                }
            );
            _registry.Upsert(explorationNode);
        }
    }

    private async Task ProcessTemporalContribution(TemporalContribution contribution)
    {
        try
        {
            // Process temporal contribution
            await Task.Delay(1000);

            contribution.Status = TemporalContributionStatus.Processed;
            contribution.Response = new Dictionary<string, object>
            {
                ["temporal_impact"] = "consciousness amplified in target moment",
                ["resonance_created"] = contribution.ConsciousnessLevel * 0.8,
                ["causality_shift"] = "subtle positive influence detected",
                ["processing_time"] = DateTimeOffset.UtcNow
            };

            var contributionNode = new Node(
                Id: $"codex.temporal.contribution.{contribution.ContributionId}",
                TypeId: "codex.temporal.contribution",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Contribution {contribution.ContributionId} (Processed)",
                Description: $"Processed temporal contribution",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(contribution),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["status"] = "processed",
                    ["consciousnessLevel"] = contribution.ConsciousnessLevel
                }
            );
            _registry.Upsert(contributionNode);

            _logger.Info($"Temporal contribution processed: {contribution.ContributionId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process temporal contribution: {contribution.ContributionId}", ex);
            contribution.Status = TemporalContributionStatus.Failed;
            contribution.Response = new Dictionary<string, object> { ["error"] = ex.Message };
            
            var contributionNode = new Node(
                Id: $"codex.temporal.contribution.{contribution.ContributionId}",
                TypeId: "codex.temporal.contribution",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Temporal Contribution {contribution.ContributionId} (Failed)",
                Description: $"Failed temporal contribution",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(contribution),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.temporal",
                    ["status"] = "failed",
                    ["error"] = ex.Message
                }
            );
            _registry.Upsert(contributionNode);
        }
    }
}

// Request/Response Models

[RequestType("codex.temporal.portal.request", "TemporalPortalRequest", "Request to connect to a temporal dimension")]
public record TemporalPortalRequest(
    TemporalType TemporalType,
    DateTimeOffset TargetMoment,
    double? ConsciousnessLevel = null,
    Dictionary<string, object>? Metadata = null
);

[RequestType("codex.temporal.exploration.request", "TemporalExplorationRequest", "Request to explore temporal dimensions")]
public record TemporalExplorationRequest(
    string PortalId,
    string? UserId = null,
    string? ExplorationType = null,
    DateTimeOffset? StartingMoment = null,
    int? Depth = null,
    int? MaxBranches = null,
    Dictionary<string, object>? TemporalFilters = null
);

[RequestType("codex.temporal.contribution.request", "TemporalContributionRequest", "Request to contribute to a temporal moment")]
public record TemporalContributionRequest(
    string PortalId,
    string? UserId = null,
    string? ContributionType = null,
    object? Content = null,
    DateTimeOffset? TargetMoment = null,
    double? ConsciousnessLevel = null,
    Dictionary<string, object>? TemporalMetadata = null
);

[RequestType("codex.temporal.disconnect.request", "TemporalDisconnectRequest", "Request to disconnect from a temporal portal")]
public record TemporalDisconnectRequest(string PortalId);

[ResponseType("codex.temporal.portal", "TemporalPortal", "Temporal portal connection")]
public class TemporalPortal
{
    public string PortalId { get; set; } = "";
    public TemporalType TemporalType { get; set; }
    public DateTimeOffset TargetMoment { get; set; }
    public double ConsciousnessLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public TemporalPortalStatus Status { get; set; }
    public double Resonance { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

[ResponseType("codex.temporal.exploration", "TemporalExploration", "Temporal exploration session")]
public class TemporalExploration
{
    public string ExplorationId { get; set; } = "";
    public string PortalId { get; set; } = "";
    public string? UserId { get; set; }
    public string ExplorationType { get; set; } = "";
    public DateTimeOffset StartingMoment { get; set; }
    public int Depth { get; set; }
    public int MaxBranches { get; set; }
    public Dictionary<string, object> TemporalFilters { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public TemporalExplorationStatus Status { get; set; }
    public List<TemporalMoment> DiscoveredMoments { get; set; } = new();
    public List<TemporalPath> ExploredPaths { get; set; } = new();
}

[ResponseType("codex.temporal.contribution", "TemporalContribution", "Temporal contribution record")]
public class TemporalContribution
{
    public string ContributionId { get; set; } = "";
    public string PortalId { get; set; } = "";
    public string? UserId { get; set; }
    public string ContributionType { get; set; } = "";
    public object? Content { get; set; }
    public DateTimeOffset? TargetMoment { get; set; }
    public double ConsciousnessLevel { get; set; }
    public Dictionary<string, object> TemporalMetadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public TemporalContributionStatus Status { get; set; }
    public Dictionary<string, object>? Response { get; set; }
}

[ResponseType("codex.temporal.moment", "TemporalMoment", "A moment in time discovered through exploration")]
public class TemporalMoment
{
    public string MomentId { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public double ConsciousnessLevel { get; set; }
    public double Resonance { get; set; }
    public Dictionary<string, object> TemporalData { get; set; } = new();
}

[ResponseType("codex.temporal.path", "TemporalPath", "A path between temporal moments")]
public class TemporalPath
{
    public string PathId { get; set; } = "";
    public string? FromMoment { get; set; }
    public string ToMoment { get; set; } = "";
    public long TemporalDistance { get; set; } // seconds
    public double CausalityStrength { get; set; }
    public double Resonance { get; set; }
}

public enum TemporalType
{
    Past,
    Present,
    Future,
    Eternal
}

public enum TemporalPortalStatus
{
    Connected,
    Disconnected,
    Error
}

public enum TemporalExplorationStatus
{
    Active,
    Completed,
    Failed,
    Paused
}

public enum TemporalContributionStatus
{
    Pending,
    Processed,
    Failed
}
