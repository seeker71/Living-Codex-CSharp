using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Registry Module - Central registry for all concepts across services
/// Manages concept registration, discovery, versioning, and cross-service synchronization
/// </summary>
public class ConceptRegistryModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, ConceptRegistryEntry> _conceptRegistry = new();
    private readonly Dictionary<string, List<string>> _serviceConcepts = new();
    private readonly Dictionary<string, ConceptVersion> _conceptVersions = new();
    private readonly Dictionary<string, List<ConceptRelationship>> _conceptRelationships = new();
    private CoreApiService? _coreApiService;

    public ConceptRegistryModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public ConceptRegistryModule() : this(new NodeRegistry()) { }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.concept-registry",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Registry Module",
            Description: "Central registry for all concepts across services with version management and cross-service synchronization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "relationship-management" },
                    endpoints = new[] { "register-concept", "discover-concepts", "get-concept", "update-concept", "sync-concepts" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Concept Registry Module",
                ["version"] = "1.0.0",
                ["type"] = "registry",
                ["capabilities"] = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync" }
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _coreApiService = coreApi;
        
        // Register all Concept Registry related nodes for AI agent discovery
        RegisterConceptRegistryNodes(registry);
    }

    /// <summary>
    /// Register all Concept Registry related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterConceptRegistryNodes(NodeRegistry registry)
    {
        // Register Concept Registry module node
        var conceptRegistryNode = new Node(
            Id: "codex.concept-registry",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Registry Module",
            Description: "Central registry for all concepts across services with version management and cross-service synchronization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "relationship-management" },
                    endpoints = new[] { "register-concept", "discover-concepts", "get-concept", "update-concept", "sync-concepts", "create-relationship" },
                    integration = "cross-service-concept-management"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Concept Registry Module",
                ["version"] = "1.0.0",
                ["type"] = "registry",
                ["parentModule"] = "codex.concept-registry",
                ["capabilities"] = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync" }
            }
        );
        registry.Upsert(conceptRegistryNode);

        // Register Concept Registry routes as nodes
        RegisterConceptRegistryRoutes(registry);
        
        // Register Concept Registry DTOs as nodes
        RegisterConceptRegistryDTOs(registry);
        
        // Register Concept Registry classes as nodes
        RegisterConceptRegistryClasses(registry);
    }

    /// <summary>
    /// Register Concept Registry routes as discoverable nodes
    /// </summary>
    private void RegisterConceptRegistryRoutes(NodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/concept/register", method = "POST", name = "concept-register", description = "Register a new concept" },
            new { path = "/concept/discover", method = "GET", name = "concept-discover", description = "Discover concepts by criteria" },
            new { path = "/concept/{conceptId}", method = "GET", name = "concept-get", description = "Get concept by ID" },
            new { path = "/concept/{conceptId}", method = "PUT", name = "concept-update", description = "Update concept" },
            new { path = "/concept/{conceptId}/sync", method = "POST", name = "concept-sync", description = "Sync concept across services" },
            new { path = "/concept/{conceptId}/versions", method = "GET", name = "concept-versions", description = "Get concept version history" },
            new { path = "/concept/{conceptId}/relationships", method = "GET", name = "concept-relationships", description = "Get concept relationships" },
            new { path = "/concept/relationship", method = "POST", name = "concept-relationship-create", description = "Create concept relationship" },
            new { path = "/concept/service/{serviceId}", method = "GET", name = "concept-service", description = "Get concepts by service" },
            new { path = "/concept/search", method = "POST", name = "concept-search", description = "Search concepts with advanced criteria" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"concept-registry.route.{route.name}",
                TypeId: "meta.route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"Concept Registry route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetConceptRouteParameters(route.name),
                        responseType = GetConceptRouteResponseType(route.name),
                        example = GetConceptRouteExample(route.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = route.name,
                    ["path"] = route.path,
                    ["method"] = route.method,
                    ["description"] = route.description,
                    ["module"] = "codex.concept-registry",
                    ["parentModule"] = "codex.concept-registry"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register Concept Registry DTOs as discoverable nodes
    /// </summary>
    private void RegisterConceptRegistryDTOs(NodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "ConceptRegistrationRequest", description = "Request to register a new concept", properties = new[] { "ConceptId", "ServiceId", "Concept", "Metadata", "Tags" } },
            new { name = "ConceptRegistrationResponse", description = "Response from concept registration", properties = new[] { "Success", "ConceptId", "Version", "Message" } },
            new { name = "ConceptDiscoveryRequest", description = "Request to discover concepts", properties = new[] { "ServiceId", "Tags", "Type", "Status", "Limit" } },
            new { name = "ConceptDiscoveryResponse", description = "Response from concept discovery", properties = new[] { "Success", "Concepts", "Count", "Message" } },
            new { name = "ConceptSyncRequest", description = "Request to sync concept across services", properties = new[] { "ConceptId", "TargetServices", "SyncOptions" } },
            new { name = "ConceptSyncResponse", description = "Response from concept sync", properties = new[] { "Success", "SyncedServices", "Conflicts", "Message" } },
            new { name = "ConceptUpdateRequest", description = "Request to update a concept", properties = new[] { "ConceptId", "Updates", "Version", "Reason" } },
            new { name = "ConceptUpdateResponse", description = "Response from concept update", properties = new[] { "Success", "ConceptId", "NewVersion", "Message" } },
            new { name = "ConceptRelationshipRequest", description = "Request to create concept relationship", properties = new[] { "SourceConceptId", "TargetConceptId", "RelationshipType", "Strength" } },
            new { name = "ConceptRelationshipResponse", description = "Response from concept relationship creation", properties = new[] { "Success", "RelationshipId", "Message" } },
            new { name = "ConceptSearchRequest", description = "Request to search concepts", properties = new[] { "Query", "Filters", "SortBy", "Limit", "Offset" } },
            new { name = "ConceptSearchResponse", description = "Response from concept search", properties = new[] { "Success", "Concepts", "TotalCount", "Message" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"concept-registry.dto.{dto.name}",
                TypeId: "meta.type",
                State: ContentState.Ice,
                Locale: "en",
                Title: dto.name,
                Description: dto.description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = dto.name,
                        description = dto.description,
                        properties = dto.properties,
                        type = "record",
                        module = "codex.concept-registry",
                        usage = GetConceptDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.concept-registry",
                    ["parentModule"] = "codex.concept-registry",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    /// <summary>
    /// Register Concept Registry classes as discoverable nodes
    /// </summary>
    private void RegisterConceptRegistryClasses(NodeRegistry registry)
    {
        var classes = new[]
        {
            new { name = "ConceptRegistryEntry", description = "Entry in the concept registry", properties = new[] { "ConceptId", "ServiceId", "Concept", "Version", "CreatedAt", "UpdatedAt", "Status" } },
            new { name = "ConceptVersion", description = "Version information for a concept", properties = new[] { "Version", "CreatedAt", "Changes", "Author", "Reason" } },
            new { name = "ConceptRelationship", description = "Relationship between concepts", properties = new[] { "RelationshipId", "SourceConceptId", "TargetConceptId", "Type", "Strength", "CreatedAt" } },
            new { name = "ConceptMetadata", description = "Metadata for a concept", properties = new[] { "Tags", "Categories", "Language", "Culture", "Complexity" } }
        };

        foreach (var cls in classes)
        {
            var classNode = new Node(
                Id: $"concept-registry.class.{cls.name}",
                TypeId: "meta.class",
                State: ContentState.Ice,
                Locale: "en",
                Title: cls.name,
                Description: cls.description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = cls.name,
                        description = cls.description,
                        properties = cls.properties,
                        type = "class",
                        module = "codex.concept-registry",
                        usage = GetConceptClassUsage(cls.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = cls.name,
                    ["description"] = cls.description,
                    ["type"] = "class",
                    ["module"] = "codex.concept-registry",
                    ["parentModule"] = "codex.concept-registry",
                    ["properties"] = cls.properties
                }
            );
            registry.Upsert(classNode);
        }
    }

    // Helper methods for AI agent generation
    private object GetConceptRouteParameters(string routeName)
    {
        return routeName switch
        {
            "concept-register" => new
            {
                request = new { type = "ConceptRegistrationRequest", required = true, location = "body", description = "Concept registration details" }
            },
            "concept-discover" => new
            {
                serviceId = new { type = "string", required = false, location = "query", description = "Filter by service ID" },
                tags = new { type = "string[]", required = false, location = "query", description = "Filter by tags" },
                type = new { type = "string", required = false, location = "query", description = "Filter by concept type" },
                limit = new { type = "int", required = false, location = "query", description = "Maximum number of results" }
            },
            "concept-get" => new
            {
                conceptId = new { type = "string", required = true, location = "path", description = "Concept ID" }
            },
            "concept-update" => new
            {
                conceptId = new { type = "string", required = true, location = "path", description = "Concept ID" },
                request = new { type = "ConceptUpdateRequest", required = true, location = "body", description = "Update details" }
            },
            "concept-sync" => new
            {
                conceptId = new { type = "string", required = true, location = "path", description = "Concept ID" },
                request = new { type = "ConceptSyncRequest", required = true, location = "body", description = "Sync details" }
            },
            "concept-versions" => new
            {
                conceptId = new { type = "string", required = true, location = "path", description = "Concept ID" }
            },
            "concept-relationships" => new
            {
                conceptId = new { type = "string", required = true, location = "path", description = "Concept ID" }
            },
            "concept-relationship-create" => new
            {
                request = new { type = "ConceptRelationshipRequest", required = true, location = "body", description = "Relationship details" }
            },
            "concept-service" => new
            {
                serviceId = new { type = "string", required = true, location = "path", description = "Service ID" }
            },
            "concept-search" => new
            {
                request = new { type = "ConceptSearchRequest", required = true, location = "body", description = "Search criteria" }
            },
            _ => new { }
        };
    }

    private string GetConceptRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "concept-register" => "ConceptRegistrationResponse",
            "concept-discover" => "ConceptDiscoveryResponse",
            "concept-get" => "ConceptRegistryEntry",
            "concept-update" => "ConceptUpdateResponse",
            "concept-sync" => "ConceptSyncResponse",
            "concept-versions" => "ConceptVersion[]",
            "concept-relationships" => "ConceptRelationship[]",
            "concept-relationship-create" => "ConceptRelationshipResponse",
            "concept-service" => "ConceptDiscoveryResponse",
            "concept-search" => "ConceptSearchResponse",
            _ => "object"
        };
    }

    private object GetConceptRouteExample(string routeName)
    {
        return routeName switch
        {
            "concept-register" => new
            {
                request = new
                {
                    conceptId = "unity-concept-001",
                    serviceId = "translation-service-1",
                    concept = new
                    {
                        id = "unity-concept-001",
                        title = "Unity",
                        description = "The state of being one or united",
                        content = "Unity represents the fundamental principle of oneness..."
                    },
                    metadata = new
                    {
                        tags = new[] { "philosophy", "spirituality", "oneness" },
                        categories = new[] { "core-concept" },
                        language = "en",
                        culture = "universal"
                    },
                    tags = new[] { "unity", "oneness", "philosophy" }
                },
                response = new
                {
                    success = true,
                    conceptId = "unity-concept-001",
                    version = "1.0.0",
                    message = "Concept registered successfully"
                }
            },
            "concept-discover" => new
            {
                response = new
                {
                    success = true,
                    concepts = new[]
                    {
                        new
                        {
                            conceptId = "unity-concept-001",
                            serviceId = "translation-service-1",
                            concept = new { title = "Unity", description = "The state of being one" },
                            version = "1.0.0",
                            createdAt = "2024-01-01T00:00:00Z",
                            status = "active"
                        }
                    },
                    count = 1,
                    message = "Found 1 concepts"
                }
            },
            "concept-get" => new
            {
                response = new
                {
                    conceptId = "unity-concept-001",
                    serviceId = "translation-service-1",
                    concept = new
                    {
                        id = "unity-concept-001",
                        title = "Unity",
                        description = "The state of being one or united",
                        content = "Unity represents the fundamental principle of oneness..."
                    },
                    version = "1.0.0",
                    createdAt = "2024-01-01T00:00:00Z",
                    updatedAt = "2024-01-01T00:00:00Z",
                    status = "active"
                }
            },
            _ => new { }
        };
    }

    private string GetConceptDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "ConceptRegistrationRequest" => "Used to register a new concept in the registry. Contains concept details, metadata, and tags.",
            "ConceptRegistrationResponse" => "Returned when a concept is successfully registered. Contains the assigned concept ID and version.",
            "ConceptDiscoveryRequest" => "Used to discover concepts based on various criteria like service, tags, or type.",
            "ConceptDiscoveryResponse" => "Returned when discovering concepts. Contains the list of matching concepts and count.",
            "ConceptSyncRequest" => "Used to synchronize a concept across multiple services. Specifies target services and sync options.",
            "ConceptSyncResponse" => "Returned when syncing concepts. Contains information about synced services and any conflicts.",
            "ConceptUpdateRequest" => "Used to update an existing concept. Contains the updates and version information.",
            "ConceptUpdateResponse" => "Returned when updating a concept. Contains the new version and confirmation.",
            "ConceptRelationshipRequest" => "Used to create relationships between concepts. Specifies source, target, and relationship type.",
            "ConceptRelationshipResponse" => "Returned when creating concept relationships. Contains the relationship ID and confirmation.",
            "ConceptSearchRequest" => "Used to search concepts with advanced criteria including filters and sorting.",
            "ConceptSearchResponse" => "Returned when searching concepts. Contains matching concepts and total count.",
            _ => "Concept Registry data transfer object"
        };
    }

    private string GetConceptClassUsage(string className)
    {
        return className switch
        {
            "ConceptRegistryEntry" => "Represents an entry in the concept registry. Contains concept data, version, and metadata.",
            "ConceptVersion" => "Represents version information for a concept. Tracks changes and history.",
            "ConceptRelationship" => "Represents a relationship between two concepts. Includes type and strength.",
            "ConceptMetadata" => "Contains metadata for a concept including tags, categories, and cultural information.",
            _ => "Concept Registry class"
        };
    }

    // API Route implementations will be added here...
    // (The actual API route methods would be implemented with [ApiRoute] attributes)
}

// Data Transfer Objects for Concept Registry
public record ConceptRegistrationRequest(
    string ConceptId,
    string ServiceId,
    ConceptNode Concept,
    ConceptMetadata? Metadata,
    string[]? Tags
);

public record ConceptRegistrationResponse(
    bool Success,
    string ConceptId,
    string Version,
    string Message
);

public record ConceptDiscoveryRequest(
    string? ServiceId,
    string[]? Tags,
    string? Type,
    string? Status,
    int? Limit
);

public record ConceptDiscoveryResponse(
    bool Success,
    List<ConceptRegistryEntry> Concepts,
    int Count,
    string Message
);

public record ConceptSyncRequest(
    string ConceptId,
    string[]? TargetServices,
    ConceptSyncOptions? SyncOptions
);

public record ConceptSyncResponse(
    bool Success,
    string[] SyncedServices,
    List<ConceptConflict>? Conflicts,
    string Message
);

public record ConceptUpdateRequest(
    string ConceptId,
    ConceptNode Updates,
    string? Version,
    string? Reason
);

public record ConceptUpdateResponse(
    bool Success,
    string ConceptId,
    string NewVersion,
    string Message
);

public record ConceptRelationshipRequest(
    string SourceConceptId,
    string TargetConceptId,
    string RelationshipType,
    double? Strength
);

public record ConceptRelationshipResponse(
    bool Success,
    string RelationshipId,
    string Message
);


// Classes for Concept Registry
public class ConceptRegistryEntry
{
    public string ConceptId { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public ConceptNode Concept { get; set; } = new("", "", "", null);
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "active";
    public ConceptMetadata? Metadata { get; set; }
}

public class ConceptVersion
{
    public string Version { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Changes { get; set; } = "";
    public string Author { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class ConceptRelationship
{
    public string RelationshipId { get; set; } = "";
    public string SourceConceptId { get; set; } = "";
    public string TargetConceptId { get; set; } = "";
    public string Type { get; set; } = "";
    public double Strength { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConceptMetadata
{
    public string[]? Tags { get; set; }
    public string[]? Categories { get; set; }
    public string? Language { get; set; }
    public string? Culture { get; set; }
    public string? Complexity { get; set; }
}

public class ConceptSyncOptions
{
    public bool ForceSync { get; set; }
    public bool ResolveConflicts { get; set; }
    public string? ConflictResolutionStrategy { get; set; }
}

public class ConceptConflict
{
    public string ServiceId { get; set; } = "";
    public string ConflictType { get; set; } = "";
    public string Description { get; set; } = "";
    public object? ConflictingData { get; set; }
}

