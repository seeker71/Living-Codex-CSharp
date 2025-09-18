using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Graph Query Module - Provides graph-based querying and discovery using existing system infrastructure
/// Reuses HydrateModule for file loading, SpecReflectionModule for codex.meta/nodes, and CoreApiService for queries
/// </summary>
public class GraphQueryModule : ModuleBase
{

    public override string Name => "Graph Query Module";
    public override string Description => "Provides graph-based querying and discovery using existing system infrastructure";
    public override string Version => "1.0.0";

    public GraphQueryModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }


    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.graph.query",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "graph", "query", "discovery" },
            capabilities: new[] { "graph-querying", "node-discovery", "relationship-analysis" },
            spec: "codex.spec.graph-query"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _apiRouter = router;
        // API handlers are now registered automatically by the attribute discovery system
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store CoreApiService reference for inter-module communication
        _coreApiService = coreApi;
        
        // Auto-load system files on startup using existing HydrateModule
        _ = Task.Run(async () => await LoadSystemFilesUsingExistingModulesAsync());
    }

    /// <summary>
    /// Query the graph using XPath-like syntax
    /// </summary>
    [ApiRoute("POST", "/graph/query", "graph-query", "Query the graph using XPath-like syntax", "codex.graph.query")]
    public async Task<object> QueryGraph([ApiParameter("request", "Graph query request", Required = true, Location = "body")] GraphQueryRequest request)
    {
        try
        {
            // Try to get CoreApiService from service provider if not available
            if (_coreApiService == null && _serviceProvider != null)
            {
                _coreApiService = _serviceProvider.GetService<CoreApiService>();
            }
            
            if (_coreApiService == null)
            {
                return ResponseHelpers.CreateErrorResponse("CoreApiService not available", "SERVICE_UNAVAILABLE");
            }

            var results = await ExecuteGraphQuery(request.Query, request.Filters);
            
            return new GraphQueryResponse(
                Success: true,
                Results: results,
                Count: results.Count,
                Query: request.Query,
                Message: $"Found {results.Count} results for query '{request.Query}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to execute graph query: {ex.Message}", "GRAPH_QUERY_ERROR");
        }
    }

    /// <summary>
    /// Find connections between concepts using graph traversal
    /// </summary>
    [ApiRoute("POST", "/graph/connections", "graph-connections", "Find connections between concepts", "codex.graph.query")]
    public async Task<object> FindConnections([ApiParameter("request", "Connection discovery request", Required = true, Location = "body")] ConnectionDiscoveryRequest request)
    {
        try
        {
            if (_coreApiService == null)
            {
                return ResponseHelpers.CreateErrorResponse("CoreApiService not available", "SERVICE_UNAVAILABLE");
            }

            var connections = await FindConceptConnections(request.SourceConceptId, request.TargetConceptId, request.MaxDepth, request.RelationshipTypes);
            
            return new ConnectionDiscoveryResponse(
                Success: true,
                Connections: connections,
                Count: connections.Count,
                SourceConceptId: request.SourceConceptId,
                TargetConceptId: request.TargetConceptId,
                Message: $"Found {connections.Count} connections between concepts"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to find connections: {ex.Message}", "CONNECTION_DISCOVERY_ERROR");
        }
    }

    /// <summary>
    /// Get system overview using existing CoreApiService
    /// </summary>
    [ApiRoute("GET", "/graph/overview", "graph-overview", "Get system overview and statistics", "codex.graph.query")]
    public async Task<object> GetSystemOverview()
    {
        try
        {
            // Try to get CoreApiService from service provider if not available
            if (_coreApiService == null && _serviceProvider != null)
            {
                _coreApiService = _serviceProvider.GetService<CoreApiService>();
            }
            
            if (_coreApiService == null)
            {
                return ResponseHelpers.CreateErrorResponse("CoreApiService not available", "SERVICE_UNAVAILABLE");
            }

            // Use existing CoreApiService methods
            var allNodes = _coreApiService.GetNodes();
            var allEdges = _coreApiService.GetEdges();
            var modules = _coreApiService.GetModules();

            var overview = new SystemOverview(
                TotalNodes: allNodes.Count,
                TotalEdges: allEdges.Count,
                ModuleCount: modules.Count,
                NodeTypes: allNodes.GroupBy(n => n.TypeId).ToDictionary(g => g.Key, g => g.Count()),
                EdgeTypes: allEdges.GroupBy(e => e.Role).ToDictionary(g => g.Key, g => g.Count()),
                ModuleTypes: modules.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => 1),
                LastUpdated: DateTime.UtcNow
            );

            return new SystemOverviewResponse(
                Success: true,
                Overview: overview,
                Message: "System overview retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get system overview: {ex.Message}", "SYSTEM_OVERVIEW_ERROR");
        }
    }

    /// <summary>
    /// Search nodes by content using existing infrastructure
    /// </summary>
    [ApiRoute("GET", "/graph/search", "graph-search", "Search nodes by content", "codex.graph.query")]
    public async Task<object> SearchNodes([ApiParameter("query", "Search query", Required = true, Location = "query")] string query, [ApiParameter("nodeType", "Node type filter", Required = false, Location = "query")] string? nodeType = null)
    {
        try
        {
            if (_coreApiService == null)
            {
                return ResponseHelpers.CreateErrorResponse("CoreApiService not available", "SERVICE_UNAVAILABLE");
            }

            var allNodes = _coreApiService.GetNodes();
            var results = new List<NodeSearchResult>();

            foreach (var node in allNodes)
            {
                if (nodeType != null && node.TypeId != nodeType)
                    continue;

                var matches = SearchInNode(node, query);
                if (matches.Any())
                {
                    results.Add(new NodeSearchResult(
                        NodeId: node.Id,
                        NodeType: node.TypeId,
                        Title: node.Title ?? "Unknown",
                        Matches: matches,
                        RelevanceScore: CalculateRelevanceScore(query, matches)
                    ));
                }
            }

            // Sort by relevance score
            results = results.OrderByDescending(r => r.RelevanceScore).ToList();

            return new NodeSearchResponse(
                Success: true,
                Results: results,
                Count: results.Count,
                Query: query,
                Message: $"Found {results.Count} nodes matching '{query}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to search nodes: {ex.Message}", "NODE_SEARCH_ERROR");
        }
    }

    /// <summary>
    /// Get node relationships using existing CoreApiService
    /// </summary>
    [ApiRoute("GET", "/graph/relationships/{nodeId}", "graph-relationships", "Get node relationships", "codex.graph.query")]
    public async Task<object> GetNodeRelationships([ApiParameter("nodeId", "Node ID", Required = true, Location = "path")] string nodeId, [ApiParameter("depth", "Relationship depth", Required = false, Location = "query")] int depth = 1)
    {
        try
        {
            if (_coreApiService == null)
            {
                return ResponseHelpers.CreateErrorResponse("CoreApiService not available", "SERVICE_UNAVAILABLE");
            }

            var relationships = await GetNodeRelationshipsRecursive(nodeId, depth, new HashSet<string>());
            
            return new NodeRelationshipsResponse(
                Success: true,
                NodeId: nodeId,
                Relationships: relationships,
                Count: relationships.Count,
                Depth: depth,
                Message: $"Found {relationships.Count} relationships for node '{nodeId}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get node relationships: {ex.Message}", "NODE_RELATIONSHIPS_ERROR");
        }
    }

    /// <summary>
    /// Load system files using existing HydrateModule and AdapterModule
    /// </summary>
    private async Task LoadSystemFilesUsingExistingModulesAsync()
    {
        try
        {
            if (_coreApiService == null) return;

            // Use existing HydrateModule to load files
            var projectRoot = Directory.GetCurrentDirectory();
            var specFiles = Directory.GetFiles(projectRoot, "*.md", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileName(f).StartsWith("README") && 
                           !Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var specFile in specFiles)
            {
                try
                {
                    // Create a node for the spec file using existing patterns
                    var relativePath = Path.GetRelativePath(projectRoot, specFile);
                    var fileName = Path.GetFileName(specFile);
                    var content = await File.ReadAllTextAsync(specFile);

                    var fileNode = new Node(
                        Id: $"file:{relativePath.Replace(Path.DirectorySeparatorChar, '.')}",
                        TypeId: "codex.file",
                        State: ContentState.Ice,
                        Locale: "en",
                        Title: fileName,
                        Description: $"System file: {fileName}",
                        Content: new ContentRef(
                            MediaType: "text/markdown",
                            InlineJson: content,
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["fileType"] = "markdown",
                            ["filePath"] = specFile,
                            ["relativePath"] = relativePath,
                            ["fileName"] = fileName,
                            ["loadedAt"] = DateTime.UtcNow,
                            ["lastModified"] = File.GetLastWriteTime(specFile),
                            ["size"] = content.Length,
                            ["lineCount"] = content.Split('\n').Length
                        }
                    );

                    _registry.Upsert(fileNode);

                    // Generate codex.meta/nodes for sections using existing patterns
                    await GenerateMetaNodesForFileAsync(fileNode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading spec file {specFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading system files: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate codex.meta/nodes for file using existing SpecReflectionModule patterns
    /// </summary>
    private async Task GenerateMetaNodesForFileAsync(Node fileNode)
    {
        try
        {
            var content = fileNode.Content?.InlineJson?.ToString() ?? "";
            var sections = ExtractSpecSections(content);

            foreach (var section in sections)
            {
                var sectionTitle = section.GetValueOrDefault("title", "Unknown").ToString() ?? "Unknown";
                var level = section.GetValueOrDefault("level", 1) as int? ?? 1;
                
                var metaNode = new Node(
                    Id: $"codex.meta/section.{sectionTitle.Replace(' ', '_').ToLower()}",
                    TypeId: "codex.meta/section",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: sectionTitle,
                    Description: $"Section: {sectionTitle}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(section),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["type"] = "section",
                        ["title"] = sectionTitle,
                        ["level"] = level,
                        ["fileId"] = fileNode.Id,
                        ["filePath"] = fileNode.Meta?.GetValueOrDefault("filePath", "").ToString() ?? ""
                    }
                );
                _registry.Upsert(metaNode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating codex.meta/nodes for file {fileNode.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract spec sections using existing patterns
    /// </summary>
    private List<Dictionary<string, object>> ExtractSpecSections(string content)
    {
        var sections = new List<Dictionary<string, object>>();
        var lines = content.Split('\n');
        var currentSection = new Dictionary<string, object>();
        var currentContent = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
            {
                if (currentSection.Any())
                {
                    currentSection["content"] = string.Join("\n", currentContent);
                    sections.Add(currentSection);
                }
                currentSection = new Dictionary<string, object>
                {
                    ["title"] = line.TrimStart('#', ' '),
                    ["level"] = line.TakeWhile(c => c == '#').Count()
                };
                currentContent = new List<string>();
            }
            else
            {
                currentContent.Add(line);
            }
        }

        if (currentSection.Any())
        {
            currentSection["content"] = string.Join("\n", currentContent);
            sections.Add(currentSection);
        }

        return sections;
    }

    /// <summary>
    /// Execute graph query using existing CoreApiService methods
    /// </summary>
    private async Task<List<GraphQueryResult>> ExecuteGraphQuery(string query, Dictionary<string, object>? filters)
    {
        var results = new List<GraphQueryResult>();
        
        // Parse XPath-like query
        if (query.StartsWith("/nodes"))
        {
            var allNodes = _coreApiService!.GetNodes();
            foreach (var node in allNodes)
            {
                if (MatchesFilters(node, filters))
                {
                    results.Add(new GraphQueryResult(
                        NodeId: node.Id,
                        NodeType: node.TypeId,
                        Title: node.Title ?? "Unknown",
                        Description: node.Description ?? "",
                        Metadata: node.Meta ?? new Dictionary<string, object>(),
                        Content: node.Content?.InlineJson?.ToString() ?? ""
                    ));
                }
            }
        }
        else if (query.StartsWith("/edges"))
        {
            var allEdges = _coreApiService!.GetEdges();
            foreach (var edge in allEdges)
            {
                results.Add(new GraphQueryResult(
                    NodeId: $"{edge.FromId}->{edge.ToId}",
                    NodeType: "edge",
                    Title: $"Edge: {edge.Role}",
                    Description: $"Connection from {edge.FromId} to {edge.ToId}",
                    Metadata: new Dictionary<string, object>
                    {
                        ["fromId"] = edge.FromId,
                        ["toId"] = edge.ToId,
                        ["role"] = edge.Role
                    },
                    Content: ""
                ));
            }
        }

        return results;
    }

    /// <summary>
    /// Find concept connections using graph traversal
    /// </summary>
    private async Task<List<ConceptConnection>> FindConceptConnections(string sourceConceptId, string? targetConceptId, int maxDepth, List<string>? relationshipTypes)
    {
        var connections = new List<ConceptConnection>();
        var visited = new HashSet<string>();
        var queue = new Queue<(string nodeId, int depth, List<string> path)>();
        
        queue.Enqueue((sourceConceptId, 0, new List<string> { sourceConceptId }));

        while (queue.Count > 0)
        {
            var (currentNodeId, currentDepth, currentPath) = queue.Dequeue();
            
            if (currentDepth >= maxDepth || visited.Contains(currentNodeId))
                continue;

            visited.Add(currentNodeId);

            // Get edges from current node
            var outgoingEdges = _coreApiService!.GetEdgesFrom(currentNodeId);
            foreach (var edge in outgoingEdges)
            {
                if (relationshipTypes != null && !relationshipTypes.Contains(edge.Role))
                    continue;

                var newPath = new List<string>(currentPath) { edge.ToId };
                var connection = new ConceptConnection(
                    SourceId: sourceConceptId,
                    TargetId: edge.ToId,
                    Path: newPath,
                    Depth: currentDepth + 1,
                    RelationshipType: edge.Role,
                    IsDirect: currentDepth == 0
                );

                connections.Add(connection);

                // If we found the target concept, we can stop
                if (targetConceptId != null && edge.ToId == targetConceptId)
                    continue;

                // Continue traversal
                if (currentDepth + 1 < maxDepth)
                {
                    queue.Enqueue((edge.ToId, currentDepth + 1, newPath));
                }
            }
        }

        return connections;
    }

    /// <summary>
    /// Get node relationships recursively
    /// </summary>
    private async Task<List<NodeRelationship>> GetNodeRelationshipsRecursive(string nodeId, int depth, HashSet<string> visited)
    {
        var relationships = new List<NodeRelationship>();
        
        if (depth <= 0 || visited.Contains(nodeId))
            return relationships;

        visited.Add(nodeId);

        // Get outgoing edges
        var outgoingEdges = _coreApiService!.GetEdgesFrom(nodeId);
        foreach (var edge in outgoingEdges)
        {
            var targetNode = _coreApiService.GetNode(edge.ToId);
            relationships.Add(new NodeRelationship(
                SourceId: nodeId,
                TargetId: edge.ToId,
                RelationshipType: edge.Role,
                TargetNode: targetNode,
                Depth: depth
            ));

            // Recursively get relationships
            if (depth > 1)
            {
                var subRelationships = await GetNodeRelationshipsRecursive(edge.ToId, depth - 1, visited);
                relationships.AddRange(subRelationships);
            }
        }

        return relationships;
    }

    /// <summary>
    /// Search in node content
    /// </summary>
    private List<ContentMatch> SearchInNode(Node node, string query)
    {
        var matches = new List<ContentMatch>();
        var content = node.Content?.InlineJson?.ToString() ?? "";

        // Search in content
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(new ContentMatch(
                    Line: i + 1,
                    Content: lines[i].Trim(),
                    Context: string.Join("\n", lines.Skip(Math.Max(0, i - 2)).Take(5))
                ));
            }
        }

        // Search in metadata
        if (node.Meta != null)
        {
            foreach (var kvp in node.Meta)
            {
                if (kvp.Value?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matches.Add(new ContentMatch(
                        Line: 0,
                        Content: $"{kvp.Key}: {kvp.Value}",
                        Context: "Metadata"
                    ));
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Calculate relevance score
    /// </summary>
    private double CalculateRelevanceScore(string query, List<ContentMatch> matches)
    {
        if (!matches.Any()) return 0.0;

        var exactMatches = matches.Count(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
        var totalMatches = matches.Count;
        
        return (double)exactMatches / totalMatches;
    }

    /// <summary>
    /// Check if node matches filters
    /// </summary>
    private bool MatchesFilters(Node node, Dictionary<string, object>? filters)
    {
        if (filters == null) return true;

        foreach (var filter in filters)
        {
            if (filter.Key == "typeId" && node.TypeId != filter.Value.ToString())
                return false;
            
            if (filter.Key == "state" && node.State.ToString() != filter.Value.ToString())
                return false;

            if (node.Meta?.ContainsKey(filter.Key) == true)
            {
                var metaValue = node.Meta[filter.Key]?.ToString();
                if (metaValue != filter.Value.ToString())
                    return false;
            }
        }

        return true;
    }
}

// Data models
public record GraphQueryRequest(
    string Query,
    Dictionary<string, object>? Filters = null
);

public record GraphQueryResponse(
    bool Success,
    List<GraphQueryResult> Results,
    int Count,
    string Query,
    string Message
);

public record GraphQueryResult(
    string NodeId,
    string NodeType,
    string Title,
    string Description,
    Dictionary<string, object> Metadata,
    string Content
);

public record ConnectionDiscoveryRequest(
    string SourceConceptId,
    string? TargetConceptId = null,
    int MaxDepth = 3,
    List<string>? RelationshipTypes = null
);

public record ConnectionDiscoveryResponse(
    bool Success,
    List<ConceptConnection> Connections,
    int Count,
    string SourceConceptId,
    string? TargetConceptId,
    string Message
);

public record ConceptConnection(
    string SourceId,
    string TargetId,
    List<string> Path,
    int Depth,
    string RelationshipType,
    bool IsDirect
);

public record SystemOverview(
    int TotalNodes,
    int TotalEdges,
    int ModuleCount,
    Dictionary<string, int> NodeTypes,
    Dictionary<string, int> EdgeTypes,
    Dictionary<string, int> ModuleTypes,
    DateTime LastUpdated
);

public record SystemOverviewResponse(
    bool Success,
    SystemOverview Overview,
    string Message
);

public record NodeSearchResult(
    string NodeId,
    string NodeType,
    string Title,
    List<ContentMatch> Matches,
    double RelevanceScore
);

public record NodeSearchResponse(
    bool Success,
    List<NodeSearchResult> Results,
    int Count,
    string Query,
    string Message
);

public record NodeRelationshipsResponse(
    bool Success,
    string NodeId,
    List<NodeRelationship> Relationships,
    int Count,
    int Depth,
    string Message
);

public record NodeRelationship(
    string SourceId,
    string TargetId,
    string RelationshipType,
    Node? TargetNode,
    int Depth
);

public record ContentMatch(
    int Line,
    string Content,
    string Context
);
