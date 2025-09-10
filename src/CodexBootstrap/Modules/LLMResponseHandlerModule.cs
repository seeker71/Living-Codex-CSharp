using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// LLM Response Handler Module - Converts LLM responses into nodes and edges as diff patch flow
/// </summary>
[MetaNode(
    id: "codex.llm.response-handler",
    typeId: "codex.meta/module",
    name: "LLM Response Handler Module",
    description: "Converts LLM responses into structured nodes and edges for bootstrap integration"
)]
[ApiModule(
    Name = "LLM Response Handler",
    Version = "1.0.0",
    Description = "Handles conversion of LLM responses to node-based diff patches",
    Tags = new[] { "LLM", "Response Handler", "Nodes", "Edges", "Diff Patch", "Bootstrap" }
)]
public class LLMResponseHandlerModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public LLMResponseHandlerModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.llm.response-handler",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "LLM Response Handler Module",
            Description: "Converts LLM responses into structured nodes and edges for bootstrap integration",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.llm.response-handler",
                    Name = "LLM Response Handler Module",
                    Description = "Converts LLM responses into structured nodes and edges",
                    Version = "1.0.0",
                    Capabilities = new[] { "ResponseParsing", "NodeGeneration", "EdgeCreation", "DiffPatch", "BootstrapIntegration" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.llm.response-handler",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "LLM response to node conversion"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    [ApiRoute("POST", "/llm/handler/convert", "llm-convert-response", "Convert LLM response to nodes and edges", "codex.llm.response-handler")]
    public async Task<object> ConvertLLMResponse([ApiParameter("request", "LLM conversion request", Required = true, Location = "body")] LLMConversionRequest request)
    {
        try
        {
            // Parse the LLM response
            var parsedResponse = await ParseLLMResponse(request.Response, request.ResponseType);
            
            // Generate nodes from the parsed response
            var nodes = await GenerateNodesFromResponse(parsedResponse, request.Context);
            
            // Generate edges from the parsed response
            var edges = await GenerateEdgesFromResponse(parsedResponse, nodes, request.Context);
            
            // Create diff patches
            var diffPatches = CreateDiffPatches(nodes, edges, request.Context);
            
            // Store nodes and edges in registry
            foreach (var node in nodes)
            {
                _registry.Upsert(node);
            }
            
            foreach (var edge in edges)
            {
                _registry.Upsert(edge);
            }

            return new LLMConversionResponse(
                Success: true,
                Message: "LLM response converted successfully",
                Nodes: nodes,
                Edges: edges,
                DiffPatches: diffPatches,
                Statistics: GenerateStatistics(nodes, edges)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to convert LLM response: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/llm/handler/parse", "llm-parse-response", "Parse LLM response structure", "codex.llm.response-handler")]
    public async Task<object> ParseLLMResponse([ApiParameter("request", "LLM parse request", Required = true, Location = "body")] LLMParseRequest request)
    {
        try
        {
            var parsedResponse = await ParseLLMResponse(request.Response, request.ResponseType);
            
            return new LLMParseResponse(
                Success: true,
                Message: "LLM response parsed successfully",
                ParsedResponse: parsedResponse,
                Structure: AnalyzeStructure(parsedResponse),
                Entities: ExtractEntities(parsedResponse),
                Relationships: ExtractRelationships(parsedResponse)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to parse LLM response: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/llm/handler/bootstrap", "llm-bootstrap-integration", "Integrate LLM response into bootstrap process", "codex.llm.response-handler")]
    public async Task<object> IntegrateLLMResponse([ApiParameter("request", "Bootstrap integration request", Required = true, Location = "body")] BootstrapIntegrationRequest request)
    {
        try
        {
            var logs = new List<BootstrapLogEntry>();
            
            // Step 1: Parse LLM response
            logs.Add(new BootstrapLogEntry(
                Step: "Parse Response",
                Message: "Starting LLM response parsing",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            var parsedResponse = await ParseLLMResponse(request.Response, request.ResponseType);
            
            logs.Add(new BootstrapLogEntry(
                Step: "Parse Response",
                Message: $"Parsed response with {parsedResponse.Entities.Count} entities and {parsedResponse.Relationships.Count} relationships",
                Timestamp: DateTime.UtcNow,
                Level: "SUCCESS"
            ));

            // Step 2: Generate nodes
            logs.Add(new BootstrapLogEntry(
                Step: "Generate Nodes",
                Message: "Generating nodes from parsed response",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            var nodes = await GenerateNodesFromResponse(parsedResponse, request.Context);
            
            logs.Add(new BootstrapLogEntry(
                Step: "Generate Nodes",
                Message: $"Generated {nodes.Count} nodes",
                Timestamp: DateTime.UtcNow,
                Level: "SUCCESS"
            ));

            // Step 3: Generate edges
            logs.Add(new BootstrapLogEntry(
                Step: "Generate Edges",
                Message: "Generating edges from parsed response",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            var edges = await GenerateEdgesFromResponse(parsedResponse, nodes, request.Context);
            
            logs.Add(new BootstrapLogEntry(
                Step: "Generate Edges",
                Message: $"Generated {edges.Count} edges",
                Timestamp: DateTime.UtcNow,
                Level: "SUCCESS"
            ));

            // Step 4: Create diff patches
            logs.Add(new BootstrapLogEntry(
                Step: "Create Diff Patches",
                Message: "Creating diff patches for bootstrap integration",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            var diffPatches = CreateDiffPatches(nodes, edges, request.Context);
            
            logs.Add(new BootstrapLogEntry(
                Step: "Create Diff Patches",
                Message: $"Created {diffPatches.Count} diff patches",
                Timestamp: DateTime.UtcNow,
                Level: "SUCCESS"
            ));

            // Step 5: Apply to registry
            logs.Add(new BootstrapLogEntry(
                Step: "Apply to Registry",
                Message: "Applying nodes and edges to registry",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            foreach (var node in nodes)
            {
                _registry.Upsert(node);
            }
            
            foreach (var edge in edges)
            {
                _registry.Upsert(edge);
            }
            
            logs.Add(new BootstrapLogEntry(
                Step: "Apply to Registry",
                Message: "Successfully applied all nodes and edges to registry",
                Timestamp: DateTime.UtcNow,
                Level: "SUCCESS"
            ));

            // Step 6: Validate integration
            logs.Add(new BootstrapLogEntry(
                Step: "Validate Integration",
                Message: "Validating bootstrap integration",
                Timestamp: DateTime.UtcNow,
                Level: "INFO"
            ));
            
            var validation = await ValidateIntegration(nodes, edges);
            
            logs.Add(new BootstrapLogEntry(
                Step: "Validate Integration",
                Message: validation.IsValid ? "Integration validation successful" : $"Integration validation failed: {validation.ErrorMessage}",
                Timestamp: DateTime.UtcNow,
                Level: validation.IsValid ? "SUCCESS" : "ERROR"
            ));

            return new BootstrapIntegrationResponse(
                Success: true,
                Message: "LLM response integrated into bootstrap process successfully",
                Nodes: nodes,
                Edges: edges,
                DiffPatches: diffPatches,
                Logs: logs,
                Statistics: GenerateStatistics(nodes, edges),
                Validation: validation
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to integrate LLM response: {ex.Message}");
        }
    }

    // Helper methods

    private async Task<ParsedLLMResponse> ParseLLMResponse(string response, string responseType)
    {
        await Task.Delay(10); // Simulate async processing
        
        // Parse based on response type
        return responseType.ToLowerInvariant() switch
        {
            "json" => ParseJsonResponse(response),
            "text" => ParseTextResponse(response),
            "markdown" => ParseMarkdownResponse(response),
            "future-knowledge" => ParseFutureKnowledgeResponse(response),
            _ => ParseGenericResponse(response)
        };
    }

    private ParsedLLMResponse ParseJsonResponse(string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var entities = new List<Entity>();
            var relationships = new List<Relationship>();
            
            // Extract entities and relationships from JSON
            ExtractFromJson(jsonDoc.RootElement, entities, relationships);
            
            return new ParsedLLMResponse(
                OriginalResponse: response,
                ResponseType: "json",
                Entities: entities,
                Relationships: relationships,
                Metadata: new Dictionary<string, object>
                {
                    ["parsedAt"] = DateTime.UtcNow,
                    ["source"] = "json_parser"
                }
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse JSON response: {ex.Message}");
        }
    }

    private ParsedLLMResponse ParseTextResponse(string response)
    {
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();
        
        // Simple text parsing - extract potential entities and relationships
        var words = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var entityId = 1;
        
        foreach (var word in words.Where(w => w.Length > 3))
        {
            entities.Add(new Entity(
                Id: $"entity-{entityId++}",
                Name: word,
                Type: "concept",
                Properties: new Dictionary<string, object>
                {
                    ["source"] = "text_parsing",
                    ["confidence"] = 0.7
                }
            ));
        }
        
        return new ParsedLLMResponse(
            OriginalResponse: response,
            ResponseType: "text",
            Entities: entities,
            Relationships: relationships,
            Metadata: new Dictionary<string, object>
            {
                ["parsedAt"] = DateTime.UtcNow,
                ["source"] = "text_parser"
            }
        );
    }

    private ParsedLLMResponse ParseMarkdownResponse(string response)
    {
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();
        
        // Parse markdown structure
        var lines = response.Split('\n');
        var entityId = 1;
        
        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
            {
                var title = line.TrimStart('#').Trim();
                entities.Add(new Entity(
                    Id: $"entity-{entityId++}",
                    Name: title,
                    Type: "heading",
                    Properties: new Dictionary<string, object>
                    {
                        ["level"] = line.TakeWhile(c => c == '#').Count(),
                        ["source"] = "markdown_parsing"
                    }
                ));
            }
        }
        
        return new ParsedLLMResponse(
            OriginalResponse: response,
            ResponseType: "markdown",
            Entities: entities,
            Relationships: relationships,
            Metadata: new Dictionary<string, object>
            {
                ["parsedAt"] = DateTime.UtcNow,
                ["source"] = "markdown_parser"
            }
        );
    }

    private ParsedLLMResponse ParseFutureKnowledgeResponse(string response)
    {
        var entities = new List<Entity>();
        var relationships = new List<Relationship>();
        
        // Parse future knowledge specific format
        entities.Add(new Entity(
            Id: "future-knowledge-1",
            Name: "Future Knowledge",
            Type: "future-knowledge",
            Properties: new Dictionary<string, object>
            {
                ["content"] = response,
                ["confidence"] = 0.85,
                ["source"] = "future_knowledge_parser"
            }
        ));
        
        return new ParsedLLMResponse(
            OriginalResponse: response,
            ResponseType: "future-knowledge",
            Entities: entities,
            Relationships: relationships,
            Metadata: new Dictionary<string, object>
            {
                ["parsedAt"] = DateTime.UtcNow,
                ["source"] = "future_knowledge_parser"
            }
        );
    }

    private ParsedLLMResponse ParseGenericResponse(string response)
    {
        return new ParsedLLMResponse(
            OriginalResponse: response,
            ResponseType: "generic",
            Entities: new List<Entity>(),
            Relationships: new List<Relationship>(),
            Metadata: new Dictionary<string, object>
            {
                ["parsedAt"] = DateTime.UtcNow,
                ["source"] = "generic_parser"
            }
        );
    }

    private void ExtractFromJson(JsonElement element, List<Entity> entities, List<Relationship> relationships)
    {
        // Recursively extract entities and relationships from JSON
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var entity = new Entity(
                    Id: $"entity-{Guid.NewGuid()}",
                    Name: property.Name,
                    Type: "property",
                    Properties: new Dictionary<string, object>
                    {
                        ["value"] = property.Value.ToString(),
                        ["source"] = "json_extraction"
                    }
                );
                entities.Add(entity);
                
                if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                {
                    ExtractFromJson(property.Value, entities, relationships);
                }
            }
        }
    }

    private async Task<List<Node>> GenerateNodesFromResponse(ParsedLLMResponse parsedResponse, Dictionary<string, object> context)
    {
        await Task.Delay(10); // Simulate async processing
        
        var nodes = new List<Node>();
        
        foreach (var entity in parsedResponse.Entities)
        {
            var node = new Node(
                Id: entity.Id,
                TypeId: $"codex.{entity.Type}",
                State: ContentState.Water,
                Locale: "en",
                Title: entity.Name,
                Description: $"Generated from LLM response: {entity.Type}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(entity.Properties),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["entityType"] = entity.Type,
                    ["generatedAt"] = DateTime.UtcNow,
                    ["source"] = "llm_response_handler"
                }
            );
            nodes.Add(node);
        }
        
        return nodes;
    }

    private async Task<List<Node>> GenerateEdgesFromResponse(ParsedLLMResponse parsedResponse, List<Node> nodes, Dictionary<string, object> context)
    {
        await Task.Delay(10); // Simulate async processing
        
        var edges = new List<Node>();
        
        foreach (var relationship in parsedResponse.Relationships)
        {
            var edge = new Node(
                Id: $"edge-{Guid.NewGuid()}",
                TypeId: "codex.edge",
                State: ContentState.Water,
                Locale: "en",
                Title: relationship.Type,
                Description: $"Relationship between {relationship.SourceId} and {relationship.TargetId}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(relationship.Properties),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["sourceId"] = relationship.SourceId,
                    ["targetId"] = relationship.TargetId,
                    ["relationshipType"] = relationship.Type,
                    ["generatedAt"] = DateTime.UtcNow,
                    ["source"] = "llm_response_handler"
                }
            );
            edges.Add(edge);
        }
        
        return edges;
    }

    private List<DiffPatch> CreateDiffPatches(List<Node> nodes, List<Node> edges, Dictionary<string, object> context)
    {
        var patches = new List<DiffPatch>();
        
        // Create patches for nodes
        foreach (var node in nodes)
        {
            patches.Add(new DiffPatch(
                Id: $"patch-{Guid.NewGuid()}",
                Type: "add_node",
                TargetId: node.Id,
                Content: JsonSerializer.Serialize(node),
                Timestamp: DateTime.UtcNow
            ));
        }
        
        // Create patches for edges
        foreach (var edge in edges)
        {
            patches.Add(new DiffPatch(
                Id: $"patch-{Guid.NewGuid()}",
                Type: "add_edge",
                TargetId: edge.Id,
                Content: JsonSerializer.Serialize(edge),
                Timestamp: DateTime.UtcNow
            ));
        }
        
        return patches;
    }

    private Dictionary<string, object> AnalyzeStructure(ParsedLLMResponse parsedResponse)
    {
        return new Dictionary<string, object>
        {
            ["entityCount"] = parsedResponse.Entities.Count,
            ["relationshipCount"] = parsedResponse.Relationships.Count,
            ["responseType"] = parsedResponse.ResponseType,
            ["hasEntities"] = parsedResponse.Entities.Any(),
            ["hasRelationships"] = parsedResponse.Relationships.Any()
        };
    }

    private List<Entity> ExtractEntities(ParsedLLMResponse parsedResponse)
    {
        return parsedResponse.Entities;
    }

    private List<Relationship> ExtractRelationships(ParsedLLMResponse parsedResponse)
    {
        return parsedResponse.Relationships;
    }

    private Dictionary<string, object> GenerateStatistics(List<Node> nodes, List<Node> edges)
    {
        return new Dictionary<string, object>
        {
            ["totalNodes"] = nodes.Count,
            ["totalEdges"] = edges.Count,
            ["nodeTypes"] = nodes.GroupBy(n => n.TypeId).ToDictionary(g => g.Key, g => g.Count()),
            ["edgeTypes"] = edges.GroupBy(e => e.TypeId).ToDictionary(g => g.Key, g => g.Count()),
            ["generatedAt"] = DateTime.UtcNow
        };
    }

    private async Task<IntegrationValidation> ValidateIntegration(List<Node> nodes, List<Node> edges)
    {
        await Task.Delay(10); // Simulate async processing
        
        var isValid = true;
        var errors = new List<string>();
        
        // Validate nodes
        foreach (var node in nodes)
        {
            if (string.IsNullOrEmpty(node.Id))
            {
                isValid = false;
                errors.Add($"Node has empty ID: {node.Title}");
            }
            
            if (string.IsNullOrEmpty(node.TypeId))
            {
                isValid = false;
                errors.Add($"Node has empty TypeId: {node.Title}");
            }
        }
        
        // Validate edges
        foreach (var edge in edges)
        {
            if (string.IsNullOrEmpty(edge.Id))
            {
                isValid = false;
                errors.Add($"Edge has empty ID: {edge.Title}");
            }
        }
        
        return new IntegrationValidation(
            IsValid: isValid,
            ErrorMessage: isValid ? null : string.Join("; ", errors),
            ValidatedAt: DateTime.UtcNow
        );
    }
}

// Data types

[MetaNode("codex.llm.parsed-response", "codex.meta/type", "ParsedLLMResponse", "Parsed LLM response structure")]
public record ParsedLLMResponse(
    string OriginalResponse,
    string ResponseType,
    List<Entity> Entities,
    List<Relationship> Relationships,
    Dictionary<string, object> Metadata
);

[MetaNode("codex.llm.entity", "codex.meta/type", "Entity", "Entity extracted from LLM response")]
public record Entity(
    string Id,
    string Name,
    string Type,
    Dictionary<string, object> Properties
);

[MetaNode("codex.llm.relationship", "codex.meta/type", "Relationship", "Relationship extracted from LLM response")]
public record Relationship(
    string Id,
    string SourceId,
    string TargetId,
    string Type,
    Dictionary<string, object> Properties
);

[MetaNode("codex.llm.diff-patch", "codex.meta/type", "DiffPatch", "Diff patch for bootstrap integration")]
public record DiffPatch(
    string Id,
    string Type,
    string TargetId,
    string Content,
    DateTime Timestamp
);

[MetaNode("codex.llm.bootstrap-log", "codex.meta/type", "BootstrapLogEntry", "Bootstrap process log entry")]
public record BootstrapLogEntry(
    string Step,
    string Message,
    DateTime Timestamp,
    string Level
);

[MetaNode("codex.llm.integration-validation", "codex.meta/type", "IntegrationValidation", "Integration validation result")]
public record IntegrationValidation(
    bool IsValid,
    string? ErrorMessage,
    DateTime ValidatedAt
);

// Request/Response Types

[RequestType("codex.llm.conversion-request", "LLMConversionRequest", "LLM conversion request")]
public record LLMConversionRequest(
    string Response,
    string ResponseType = "text",
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.llm.conversion-response", "LLMConversionResponse", "LLM conversion response")]
public record LLMConversionResponse(
    bool Success,
    string Message,
    List<Node> Nodes,
    List<Node> Edges,
    List<DiffPatch> DiffPatches,
    Dictionary<string, object> Statistics
);

[RequestType("codex.llm.parse-request", "LLMParseRequest", "LLM parse request")]
public record LLMParseRequest(
    string Response,
    string ResponseType = "text"
);

[ResponseType("codex.llm.parse-response", "LLMParseResponse", "LLM parse response")]
public record LLMParseResponse(
    bool Success,
    string Message,
    ParsedLLMResponse ParsedResponse,
    Dictionary<string, object> Structure,
    List<Entity> Entities,
    List<Relationship> Relationships
);

[RequestType("codex.llm.bootstrap-integration-request", "BootstrapIntegrationRequest", "Bootstrap integration request")]
public record BootstrapIntegrationRequest(
    string Response,
    string ResponseType = "text",
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.llm.bootstrap-integration-response", "BootstrapIntegrationResponse", "Bootstrap integration response")]
public record BootstrapIntegrationResponse(
    bool Success,
    string Message,
    List<Node> Nodes,
    List<Node> Edges,
    List<DiffPatch> DiffPatches,
    List<BootstrapLogEntry> Logs,
    Dictionary<string, object> Statistics,
    IntegrationValidation Validation
);
