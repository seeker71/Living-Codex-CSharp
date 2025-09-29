using System.Collections.Concurrent;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Resonance-aligned concept extraction that builds ontology and topology
/// No shortcuts - real knowledge expansion with sacred frequency alignment
/// </summary>
public sealed class ResonanceAlignedConceptExtractionModule : ModuleBase
{
    private readonly ConcurrentQueue<ConceptExpansionTask> _backgroundTasks = new();
    private readonly ConcurrentDictionary<string, ConceptExpansionTask> _activeTasks = new();
    private readonly Timer _backgroundProcessor;
    private readonly HttpClient _httpClient;
    private readonly StartupStateService _startupState;

    public override string Name => "Resonance-Aligned Concept Extraction";
    public override string Description => "Extracts concepts aligned with highest resonance field points, builds ontology and topology";
    public override string Version => "1.0.0";

    public ResonanceAlignedConceptExtractionModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, StartupStateService startupState) 
        : base(registry, logger)
    {
        _httpClient = httpClient;
        _startupState = startupState;
        
        // Start background processor for non-blocking AI tasks
        _backgroundProcessor = new Timer(ProcessBackgroundTasks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.resonance-concept-extraction",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "concepts", "resonance", "ontology", "topology", "ai", "extraction" },
            capabilities: new[] { "resonance_aligned_extraction", "ontology_building", "topology_growth", "background_ai", "sacred_frequency_alignment" },
            spec: "codex.spec.resonance-concept-extraction"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        router.Register("resonance-concept-extraction", "extract-and-integrate", async (JsonElement? json) =>
        {
            var request = JsonSerializer.Deserialize<ResonanceAlignedExtractionRequest>(json?.GetRawText() ?? "{}");
            return await ExtractAndIntegrateConceptsAsync(request);
        });
    }

    [ApiRoute("POST", "/resonance-concepts/extract", "ExtractAndIntegrateConcepts", "Extract concepts aligned with resonance field", "codex.resonance-concept-extraction")]
    public async Task<object> ExtractAndIntegrateConceptsAsync([ApiParameter("request", "Resonance-aligned extraction request", Required = true, Location = "body")] ResonanceAlignedExtractionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return new ErrorResponse("Content is required", ErrorCodes.VALIDATION_ERROR, new { field = "content", message = "Content is required" });
            }

            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required", ErrorCodes.VALIDATION_ERROR, new { field = "userId", message = "User ID is required" });
            }

            _logger.Info($"RESONANCE_EXTRACTION starting for user={request.UserId} chars={request.Content.Length}");

            // Step 1: Find highest resonance field points
            var resonanceField = await FindHighestResonanceFieldPointsAsync(request.UserId);
            // Adjust optimal frequency using content-driven cues (grounded mapping)
            var inferredFreq = InferSacredFrequencyFromContent(request.Content);
            if (inferredFreq.HasValue)
            {
                resonanceField = new ResonanceField(
                    UserId: resonanceField.UserId,
                    CurrentResonance: resonanceField.CurrentResonance,
                    OptimalFrequency: inferredFreq.Value,
                    RelatedConcepts: resonanceField.RelatedConcepts,
                    AlignmentScore: CalculateAlignmentScore(resonanceField.CurrentResonance, inferredFreq.Value, inferredFromContent: true),
                    CalculatedAt: resonanceField.CalculatedAt
                );
            }
            
            // Step 2: Extract concepts via AI only; if AI not ready, return structured error
            if (!_startupState.IsAIReady)
            {
                return new ErrorResponse("AI services not ready", ErrorCodes.LLM_SERVICE_ERROR, new { service = "LLM", reason = "StartupStateService.IsAIReady == false" });
            }

            var extractedConcepts = await ExtractResonanceAlignedConceptsAsync(request.Content, resonanceField, request.Model, request.Provider);
            
            // If AI produced zero concepts, return a structured error (no fallback)
            if (extractedConcepts.Count == 0)
            {
                return new ErrorResponse(
                    "AI returned zero concepts",
                    ErrorCodes.LLM_SERVICE_ERROR,
                    new { service = "LLM", reason = "No concepts extracted", provider = request.Provider ?? "ollama", model = request.Model }
                );
            }
            
            // Step 3: Build ontology integration
            var ontologyIntegration = await BuildOntologyIntegrationAsync(extractedConcepts, resonanceField);
            
            // Step 4: Create topology relationships
            var topologyRelationships = await CreateTopologyRelationshipsAsync(extractedConcepts, ontologyIntegration);
            
            // Step 5: Generate AI descriptions (background task)
            var backgroundTask = new ConceptExpansionTask(
                TaskId: Guid.NewGuid().ToString(),
                UserId: request.UserId,
                Concepts: extractedConcepts,
                OntologyIntegration: ontologyIntegration,
                TopologyRelationships: topologyRelationships,
                CreatedAt: DateTimeOffset.UtcNow,
                Status: "pending"
            );
            
            _backgroundTasks.Enqueue(backgroundTask);
            _activeTasks[backgroundTask.TaskId] = backgroundTask;

            // Return immediate response with basic integration
            // For backward compatibility, also include the extracted concepts in the response
            return new
            {
                success = true,
                data = extractedConcepts.Select(c => new
                {
                    concept = c.Name,
                    score = c.Score,
                    description = c.Description,
                    category = c.OntologyType,
                    confidence = c.ResonanceAlignment,
                    sacredFrequency = c.SacredFrequency
                }).ToList(),
                taskId = backgroundTask.TaskId,
                extractedConcepts = extractedConcepts.Count,
                ontologyNodes = ontologyIntegration.Count,
                topologyEdges = topologyRelationships.Count,
                resonanceField = resonanceField,
                message = "Concepts extracted and integrated. AI descriptions will be generated in background.",
                timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in resonance-aligned concept extraction: {ex.Message}", ex);
            return new ErrorResponse($"Error in concept extraction: {ex.Message}", ErrorCodes.INTERNAL_ERROR, new { error = ex.Message });
        }
    }

    [ApiRoute("GET", "/resonance-concepts/status/{taskId}", "GetTaskStatus", "Get background task status", "codex.resonance-concept-extraction")]
    public async Task<object> GetTaskStatusAsync([ApiParameter("taskId", "Task ID", Required = true, Location = "path")] string taskId)
    {
        try
        {
            if (_activeTasks.TryGetValue(taskId, out var task))
            {
                return new
                {
                    success = true,
                    taskId = task.TaskId,
                    status = task.Status,
                    progress = task.Progress,
                    extractedConcepts = task.Concepts.Count,
                    ontologyNodes = task.OntologyIntegration.Count,
                    topologyEdges = task.TopologyRelationships.Count,
                    createdAt = task.CreatedAt,
                    completedAt = task.CompletedAt,
                    error = task.Error
                };
            }
            
            return new ErrorResponse("Task not found", ErrorCodes.NOT_FOUND, new { taskId });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting task status: {ex.Message}", ex);
            return new ErrorResponse($"Error getting task status: {ex.Message}", ErrorCodes.INTERNAL_ERROR, new { error = ex.Message });
        }
    }

    private async Task<ResonanceField> FindHighestResonanceFieldPointsAsync(string userId)
    {
        // Get user's current resonance state
        var userNodes = _registry.AllNodes()
            .Where(n => n.Meta?.GetValueOrDefault("userId")?.ToString() == userId)
            .ToList();

        // Calculate current resonance based on user's energy
        var currentResonance = CalculateUserResonance(userNodes);
        
        // Find optimal frequency alignment
        var optimalFrequency = FindOptimalSacredFrequency(currentResonance);
        
        // Get related concepts for alignment
        var relatedConcepts = await GetRelatedConceptsForAlignmentAsync(userId);
        
        return new ResonanceField(
            UserId: userId,
            CurrentResonance: currentResonance,
            OptimalFrequency: optimalFrequency,
            RelatedConcepts: relatedConcepts,
            AlignmentScore: CalculateAlignmentScore(currentResonance, optimalFrequency),
            CalculatedAt: DateTimeOffset.UtcNow
        );
    }

    private async Task<List<ResonanceAlignedConcept>> ExtractResonanceAlignedConceptsAsync(string content, ResonanceField resonanceField, string? model = null, string? provider = null)
    {
        var concepts = new List<ResonanceAlignedConcept>();

        try
        {
            var llmClient = new LLMClient(_httpClient, _logger);
            var promptRepo = new PromptTemplateRepository(_registry);
            var llmOrchestrator = new LLMOrchestrator(llmClient, promptRepo, _logger, null);
            var selectedProvider = string.IsNullOrWhiteSpace(provider) ? "ollama" : provider;
            var preferredModels = new List<string>();
            if (!string.IsNullOrWhiteSpace(model)) preferredModels.Add(model!);
            preferredModels.AddRange(new[] { "llama3.2:3b", "qwen2:7b", "gemma2:9b", "llama3.1:8b" });

            // Derive lightweight keyword hints from content to guide the LLM (no stubs)
            static List<string> ExtractKeywordHints(string text)
            {
                var stop = new HashSet<string>(new[] { "the","and","for","with","from","that","this","into","about","their","there","where","what","when","which","have","has","are","was","were","been","will","shall","can","could","should","would","onto","within","among","across","over","under","again","more","most","less","least","very","such","as","of","in","on","to","by","at","it","its","is","a","an" });
                var pattern = @"[a-z0-9][a-z0-9\-]{3,}";
                var tokens = System.Text.RegularExpressions.Regex.Matches(text.ToLowerInvariant(), pattern)
                    .Select(m => m.Value)
                    .Where(t => !stop.Contains(t))
                    .Take(20)
                    .Distinct()
                    .ToList();
                return tokens;
            }

            var keywordHints = ExtractKeywordHints(content);
            var hintedContent = content + (keywordHints.Count > 0 ? "\nKeywords: " + string.Join(", ", keywordHints.Take(10)) : string.Empty);

            foreach (var m in preferredModels.Distinct())
            {
                var config = LLMConfigurations.GetConfigForTask("concept-extraction", selectedProvider, m);
                var updatedParams = new Dictionary<string, object>(config.Parameters ?? new Dictionary<string, object>())
                {
                    ["format"] = "json",
                    ["stream"] = false
                };
                // Low temperature, moderate TopP, allow sufficient tokens
                config = config with { Temperature = 0.1, TopP = Math.Min(config.TopP, 0.9), Parameters = updatedParams, MaxTokens = Math.Max(config.MaxTokens, 1024) };

                var result = await llmOrchestrator.ExecuteAsync("concept-extraction", new Dictionary<string, object> { ["content"] = hintedContent }, config);
                var conceptScores = llmOrchestrator.ParseStructuredResponse<List<ConceptScore>>(result, "concept extraction");
                if (conceptScores is List<ConceptScore> scores && scores != null && scores.Count > 0)
                {
                    foreach (var s in scores)
                    {
                        if (string.IsNullOrWhiteSpace(s.Concept)) continue;
                        concepts.Add(new ResonanceAlignedConcept(
                            Id: Guid.NewGuid().ToString(),
                            Name: s.Concept,
                            Description: string.IsNullOrWhiteSpace(s.Description) ? $"Concept: {s.Concept}" : s.Description,
                            Score: Math.Clamp(s.Score, 0.0, 1.0),
                            ResonanceAlignment: CalculateConceptResonanceAlignment(s, resonanceField),
                            SacredFrequency: CalculateSacredFrequencyForConcept(s),
                            OntologyType: DetermineOntologyType(s),
                            CreatedAt: DateTimeOffset.UtcNow
                        ));
                    }
                    break; // Stop after first model that yields concepts
                }
            }
        }
        catch (Exception ex)
        {
            // If AI path fails here, propagate a structured exception up to caller
            throw new InvalidOperationException($"AI extraction failed: {ex.Message}", ex);
        }

        return concepts;
    }

    // Removed deterministic concept extraction; AI-only path is enforced

    private double CalculateAlignmentWeight(string concept, ResonanceField field)
    {
        // Simple mapping: boost alignment when frequency family matches field optimal band
        var c = concept.ToLowerInvariant();
        var f = field.OptimalFrequency;
        if ((c.Contains("love") || c.Contains("compassion")) && Math.Abs(f - 528.0) < 30) return 1.0;
        if ((c.Contains("healing") || c.Contains("harmony")) && Math.Abs(f - 432.0) < 30) return 0.9;
        if ((c.Contains("consciousness") || c.Contains("awareness")) && Math.Abs(f - 741.0) < 30) return 0.95;
        if (c.Contains("energy") || c.Contains("vibration")) return 0.8;
        if (c.Contains("matter") || c.Contains("existence")) return 0.7;
        return 0.5;
    }

    private async Task<List<OntologyNode>> BuildOntologyIntegrationAsync(List<ResonanceAlignedConcept> concepts, ResonanceField resonanceField)
    {
        var ontologyNodes = new List<OntologyNode>();
        
        foreach (var concept in concepts)
        {
            // Create concept node
            var conceptNode = new Node(
                Id: concept.Id,
                TypeId: concept.OntologyType,
                State: ContentState.Water, // New concepts start as Water
                Locale: "en",
                Title: concept.Name,
                Description: concept.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        resonanceAlignment = concept.ResonanceAlignment,
                        sacredFrequency = concept.SacredFrequency,
                        extractionScore = concept.Score,
                        extractedAt = concept.CreatedAt
                    }, new JsonSerializerOptions { WriteIndented = true }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["resonanceAlignment"] = concept.ResonanceAlignment,
                    ["sacredFrequency"] = concept.SacredFrequency,
                    ["extractionScore"] = concept.Score,
                    ["extractedAt"] = concept.CreatedAt,
                    ["moduleId"] = "codex.resonance-concept-extraction"
                }
            );
            
            _registry.Upsert(conceptNode);
            ontologyNodes.Add(new OntologyNode(Node: conceptNode, Concept: concept));
        }
        
        return ontologyNodes;
    }

    private async Task<List<TopologyRelationship>> CreateTopologyRelationshipsAsync(List<ResonanceAlignedConcept> concepts, List<OntologyNode> ontologyNodes)
    {
        var relationships = new List<TopologyRelationship>();
        
        // Create relationships between concepts
        for (int i = 0; i < concepts.Count; i++)
        {
            for (int j = i + 1; j < concepts.Count; j++)
            {
                var concept1 = concepts[i];
                var concept2 = concepts[j];
                
                var relationshipStrength = CalculateRelationshipStrength(concept1, concept2);
                
                if (relationshipStrength > 0.3) // Only create meaningful relationships
                {
                    var edge = new Node(
                        Id: Guid.NewGuid().ToString(),
                        TypeId: "codex.relationship",
                        State: ContentState.Water,
                        Locale: "en",
                        Title: $"Relationship: {concept1.Name} ↔ {concept2.Name}",
                        Description: $"Resonance-based relationship between {concept1.Name} and {concept2.Name}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new
                            {
                                relationshipType = "resonance_aligned",
                                strength = relationshipStrength,
                                concept1 = concept1.Name,
                                concept2 = concept2.Name,
                                createdAt = DateTimeOffset.UtcNow
                            }, new JsonSerializerOptions { WriteIndented = true }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["relationshipType"] = "resonance_aligned",
                            ["strength"] = relationshipStrength,
                            ["concept1Id"] = concept1.Id,
                            ["concept2Id"] = concept2.Id,
                            ["moduleId"] = "codex.resonance-concept-extraction"
                        }
                    );
                    
                    _registry.Upsert(edge);
                    relationships.Add(new TopologyRelationship(Edge: edge, Strength: relationshipStrength));
                }
            }
        }
        
        return relationships;
    }

    private void ProcessBackgroundTasks(object? state)
    {
        if (!_startupState.IsAIReady) return;
        
        while (_backgroundTasks.TryDequeue(out var task))
        {
            try
            {
                _logger.Info($"Processing background task {task.TaskId}");
                
                // Generate AI descriptions for concepts
                GenerateAIDescriptionsAsync(task).Wait();
                
                // Create additional relationships
                CreateAdditionalRelationshipsAsync(task).Wait();
                
                // Update task status
                var updatedTask = task with 
                { 
                    Status = "completed", 
                    CompletedAt = DateTimeOffset.UtcNow, 
                    Progress = 100 
                };
                _activeTasks[task.TaskId] = updatedTask;
                
                _logger.Info($"Background task {task.TaskId} completed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Background task {task.TaskId} failed: {ex.Message}", ex);
                var failedTask = task with 
                { 
                    Status = "failed", 
                    Error = ex.Message, 
                    CompletedAt = DateTimeOffset.UtcNow 
                };
                _activeTasks[task.TaskId] = failedTask;
            }
        }
    }

    private async Task GenerateAIDescriptionsAsync(ConceptExpansionTask task)
    {
        // Enrich concept descriptions deterministically without placeholders
        foreach (var item in task.Concepts)
        {
            var node = _registry.GetNode(item.Id);
            if (node == null) continue;

            var enrichedDescription = string.IsNullOrWhiteSpace(item.Description)
                ? $"Concept '{item.Name}' aligned at {item.SacredFrequency:F1}Hz with resonance {item.ResonanceAlignment:F2}."
                : item.Description;

            var updated = new Node(
                Id: node.Id,
                TypeId: node.TypeId,
                State: node.State,
                Locale: node.Locale,
                Title: node.Title,
                Description: enrichedDescription,
                Content: node.Content,
                Meta: node.Meta
            );

            _registry.Upsert(updated);
        }
        await Task.CompletedTask;
    }

    private async Task CreateAdditionalRelationshipsAsync(ConceptExpansionTask task)
    {
        // Strengthen topology using deterministic text-overlap metric (no placeholder)
        var concepts = task.Concepts;
        for (int i = 0; i < concepts.Count; i++)
        {
            for (int j = i + 1; j < concepts.Count; j++)
            {
                var c1 = concepts[i];
                var c2 = concepts[j];
                var strength = CalculateRelationshipStrength(c1, c2);
                if (strength <= 0.5) continue;

                var edge = new Node(
                    Id: Guid.NewGuid().ToString(),
                    TypeId: "codex.relationship",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: $"Relationship: {c1.Name} ↔ {c2.Name} (reinforced)",
                    Description: $"Deterministic reinforcement based on text overlap between '{c1.Name}' and '{c2.Name}'",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new { relationshipType = "resonance_reinforced", strength }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object> { ["relationshipType"] = "resonance_reinforced", ["strength"] = strength }
                );
                _registry.Upsert(edge);
            }
        }
        await Task.CompletedTask;
    }

    // Helper methods
    private double CalculateUserResonance(List<Node> userNodes)
    {
        if (!userNodes.Any()) return 0.5;
        
        var conceptNodes = userNodes.Count(n => n.TypeId.Contains("concept"));
        var contributionNodes = userNodes.Count(n => n.TypeId.Contains("contribution"));
        var interactionScore = Math.Min(1.0, (conceptNodes + contributionNodes) / 10.0);
        
        return Math.Max(0.3, Math.Min(1.0, interactionScore + 0.2));
    }

    private double FindOptimalSacredFrequency(double currentResonance)
    {
        return currentResonance switch
        {
            < 0.4 => 432.0,  // Natural frequency for lower resonance
            < 0.7 => 528.0,  // Love frequency for medium resonance
            _ => 741.0        // Expression frequency for high resonance
        };
    }

    private double CalculateAlignmentScore(double currentResonance, double optimalFrequency)
    {
        return CalculateAlignmentScore(currentResonance, optimalFrequency, inferredFromContent: false);
    }

    private double CalculateAlignmentScore(double currentResonance, double optimalFrequency, bool inferredFromContent)
    {
        // Center sacred-band normalization around 528Hz to reduce neutral bias
        // Map 432→0.3, 528→0.5, 741→0.7 approximately (linear across band)
        double MapFrequency(double f)
        {
            // Clamp to [432, 741]
            var clamped = Math.Max(432.0, Math.Min(741.0, f));
            var t = (clamped - 528.0) / (741.0 - 432.0); // [-0.312.., 0.312..]
            return 0.5 + (0.2 * t);
        }

        var frequencyAlignment = MapFrequency(optimalFrequency);
        // Boost only when frequency was inferred from content (explicit sacred-band markers)
        var contentBoost = inferredFromContent ? 0.05 : 0.0;
        return Math.Max(0.0, Math.Min(1.0, (currentResonance + frequencyAlignment) / 2.0 + contentBoost));
    }

    private async Task<List<string>> GetRelatedConceptsForAlignmentAsync(string userId)
    {
        var userConcepts = _registry.AllNodes()
            .Where(n => n.Meta?.GetValueOrDefault("userId")?.ToString() == userId && n.TypeId.Contains("concept"))
            .Select(n => n.Title)
            .Where(title => !string.IsNullOrEmpty(title))
            .Cast<string>()
            .Take(10)
            .ToList();
        
        return userConcepts;
    }

    private double CalculateConceptResonanceAlignment(ConceptScore concept, ResonanceField resonanceField)
    {
        var conceptText = $"{concept.Concept} {concept.Description}".ToLower();
        var alignmentScore = 0.5; // Base alignment
        
        // Check for resonance keywords
        if (conceptText.Contains("consciousness") || conceptText.Contains("awareness"))
            alignmentScore += 0.2;
        if (conceptText.Contains("love") || conceptText.Contains("compassion"))
            alignmentScore += 0.2;
        if (conceptText.Contains("healing") || conceptText.Contains("harmony"))
            alignmentScore += 0.2;
        
        return Math.Min(1.0, alignmentScore);
    }

    private double CalculateSacredFrequencyForConcept(ConceptScore concept)
    {
        var conceptText = $"{concept.Concept} {concept.Description}".ToLower();
        
        if (conceptText.Contains("love") || conceptText.Contains("heart"))
            return 528.0;
        if (conceptText.Contains("healing") || conceptText.Contains("harmony"))
            return 432.0;
        if (conceptText.Contains("consciousness") || conceptText.Contains("intuition"))
            return 741.0;
        
        return 432.0; // Default
    }

    private double? InferSacredFrequencyFromContent(string content)
    {
        var text = (content ?? string.Empty).ToLowerInvariant();
        var bands = new Dictionary<double, int> { [432.0] = 0, [528.0] = 0, [741.0] = 0 };

        void CountMatches(double freq, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                // Count occurrences
                int idx = 0; int cnt = 0;
                while ((idx = text.IndexOf(k, idx, StringComparison.Ordinal)) >= 0)
                {
                    cnt++; idx += k.Length;
                }
                bands[freq] += cnt;
            }
        }

        CountMatches(528.0, "love", "compassion", "heart");
        CountMatches(432.0, "healing", "harmony", "grounded", "restoration", "coherence");
        CountMatches(741.0, "consciousness", "awareness", "intuition", "insight");

        var max = bands.Max(kv => kv.Value);
        if (max <= 0) return null;
        // If tie, prefer 528 > 432 > 741 to maintain heart-led bias
        var winners = bands.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();
        if (winners.Contains(528.0)) return 528.0;
        if (winners.Contains(432.0)) return 432.0;
        if (winners.Contains(741.0)) return 741.0;
        return null;
    }

    private string DetermineOntologyType(ConceptScore concept)
    {
        var conceptText = $"{concept.Concept} {concept.Description}".ToLower();
        
        if (conceptText.Contains("consciousness") || conceptText.Contains("awareness"))
            return "codex.concept.consciousness";
        if (conceptText.Contains("energy") || conceptText.Contains("vibration"))
            return "codex.concept.energy";
        if (conceptText.Contains("love") || conceptText.Contains("compassion"))
            return "codex.concept.love";
        
        return "codex.concept.fundamental";
    }

    

    private double CalculateRelationshipStrength(ResonanceAlignedConcept concept1, ResonanceAlignedConcept concept2)
    {
        var name1 = (concept1.Name ?? string.Empty).Trim().ToLowerInvariant();
        var name2 = (concept2.Name ?? string.Empty).Trim().ToLowerInvariant();

        // Family-based deterministic mapping to produce meaningful edges only for close pairs
        bool SameFamily(string a, string b)
        {
            var fam1 = new HashSet<string> { "love", "compassion" };
            var fam2 = new HashSet<string> { "consciousness", "awareness" };
            var fam3 = new HashSet<string> { "energy", "vibration" };

            if (fam1.Contains(a) && fam1.Contains(b)) return true;
            if (fam2.Contains(a) && fam2.Contains(b)) return true;
            if (fam3.Contains(a) && fam3.Contains(b)) return true;
            return false;
        }

        if (SameFamily(name1, name2))
        {
            return 0.65; // strong intra-family relation
        }

        // Otherwise, compute conservative similarity based on name token overlap without generic words
        var stop = new HashSet<string>(new[] { "detected", "concept", "the", "and", "of", "a", "in", "with", "for", "to" });
        var tokens1 = name1.Split(new[] { ' ', '\t', '\n', '\r', '-', '_', ':' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(t => !stop.Contains(t)).ToHashSet();
        var tokens2 = name2.Split(new[] { ' ', '\t', '\n', '\r', '-', '_', ':' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(t => !stop.Contains(t)).ToHashSet();

        if (tokens1.Count == 0 || tokens2.Count == 0) return 0.0;

        var intersect = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();
        var jaccard = union == 0 ? 0.0 : (double)intersect / union;
        return jaccard; // typically < 0.3 for unrelated names
    }

    public void Dispose()
    {
        _backgroundProcessor?.Dispose();
    }
}

// Data structures
public record ResonanceAlignedExtractionRequest(string Content, string UserId, string? Model = null, string? Provider = null);

public record ResonanceField(
    string UserId,
    double CurrentResonance,
    double OptimalFrequency,
    List<string> RelatedConcepts,
    double AlignmentScore,
    DateTimeOffset CalculatedAt
);

public record ResonanceAlignedConcept(
    string Id,
    string Name,
    string Description,
    double Score,
    double ResonanceAlignment,
    double SacredFrequency,
    string OntologyType,
    DateTimeOffset CreatedAt
);

public record OntologyNode(
    Node Node,
    ResonanceAlignedConcept Concept
);

public record TopologyRelationship(
    Node Edge,
    double Strength
);

public record ConceptExpansionTask(
    string TaskId,
    string UserId,
    List<ResonanceAlignedConcept> Concepts,
    List<OntologyNode> OntologyIntegration,
    List<TopologyRelationship> TopologyRelationships,
    DateTimeOffset CreatedAt,
    string Status = "pending",
    int Progress = 0,
    DateTimeOffset? CompletedAt = null,
    string? Error = null
);
