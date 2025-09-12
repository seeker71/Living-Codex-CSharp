using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Relations module specific response types
public record RelationRegistrationResponse(string RelationId, bool Success, string Message = "Relation registered successfully");
public record ValidationResponse(string NodeId, bool Success, string Message = "Validation completed", RelationsValidationResult? Result = null);

// Relations module data structures - all self-contained
public sealed record RelationsRelation(
    string FromType,
    string ToType,
    string Role,
    RelationsCardinality Cardinality,
    string? Description = null,
    Dictionary<string, object>? Constraints = null
);

public enum RelationsCardinality
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}

public sealed record RelationsValidationRule(
    string Name,
    string Description,
    string Expression,
    RelationsValidationSeverity Severity
);

public enum RelationsValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record RelationsValidationResult(
    bool IsValid,
    IReadOnlyList<RelationsValidationIssue> Issues
);

public sealed record RelationsValidationIssue(
    string Rule,
    string Message,
    RelationsValidationSeverity Severity,
    string? Path = null
);

public sealed record RelationRegistration(
    string FromType,
    string ToType,
    string Role,
    RelationsCardinality Cardinality,
    string? Description = null,
    Dictionary<string, object>? Constraints = null
);

public sealed record ValidationRequest(
    string NodeId,
    IReadOnlyList<string>? RuleNames = null,
    bool IncludeWarnings = true
);

public sealed class RelationsModule : IModule
{
    private readonly NodeRegistry _registry;

    public RelationsModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.relations",
            name: "Relations Module",
            version: "0.1.0",
            description: "Module for modeling relations, constraints, and validation rules.",
            capabilities: new[] { "relations", "constraints", "validation", "modeling" },
            tags: new[] { "relations", "constraints", "validation", "modeling" },
            specReference: "codex.spec.relations"
        );
    }


    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register Relation type definition as node
        var relationType = new Node(
            Id: "codex.relations/relation",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Relation Type",
            Description: "Represents a relationship between two types with cardinality constraints",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "Relation",
                    fields = new[]
                    {
                        new { name = "fromType", type = "string", required = true, description = "Source type identifier" },
                        new { name = "toType", type = "string", required = true, description = "Target type identifier" },
                        new { name = "role", type = "string", required = true, description = "Relationship role" },
                        new { name = "cardinality", type = "Cardinality", required = true, description = "Relationship cardinality" },
                        new { name = "description", type = "string", required = false, description = "Relationship description" },
                        new { name = "constraints", type = "object", required = false, description = "Additional constraints" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.relations",
                ["typeName"] = "Relation"
            }
        );
        registry.Upsert(relationType);

        // Register ValidationRule type definition as node
        var validationRuleType = new Node(
            Id: "codex.relations/validationrule",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "ValidationRule Type",
            Description: "Represents a validation rule with expression and severity",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "ValidationRule",
                    fields = new[]
                    {
                        new { name = "name", type = "string", required = true, description = "Rule name" },
                        new { name = "description", type = "string", required = true, description = "Rule description" },
                        new { name = "expression", type = "string", required = true, description = "Validation expression" },
                        new { name = "severity", type = "ValidationSeverity", required = true, description = "Rule severity" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.relations",
                ["typeName"] = "ValidationRule"
            }
        );
        registry.Upsert(validationRuleType);

        // Register API nodes for RouteDiscovery
        var registerRelationApi = NodeStorage.CreateApiNode("codex.relations", "register-relation", "/relations/register", "Register a new relation between types");
        var validateApi = NodeStorage.CreateApiNode("codex.relations", "validate", "/relations/validate", "Validate a node against registered relations and constraints");

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.relations", "register-relation"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.relations", "validate"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.relations", "register-relation", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request body");
                }

                var relationJson = args.Value.TryGetProperty("relation", out var relationElement) ? relationElement.GetRawText() : null;

                if (string.IsNullOrEmpty(relationJson))
                {
                    return new ErrorResponse("Relation information is required");
                }

                var registration = await Task.Run(() => JsonSerializer.Deserialize<RelationRegistration>(relationJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                }));

                if (registration == null)
                {
                    return new ErrorResponse("Invalid relation registration");
                }

                var relationId = $"relation-{Guid.NewGuid()}";
                var relation = new RelationsRelation(
                    FromType: registration.FromType,
                    ToType: registration.ToType,
                    Role: registration.Role,
                    Cardinality: registration.Cardinality,
                    Description: registration.Description,
                    Constraints: registration.Constraints
                );

                // Store relation as node
                var relationNode = new Node(
                    Id: relationId,
                    TypeId: "codex.relations/relation",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: $"{registration.FromType} -> {registration.ToType}",
                    Description: registration.Description ?? $"Relation from {registration.FromType} to {registration.ToType}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(relation, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["moduleId"] = "codex.relations",
                        ["fromType"] = registration.FromType,
                        ["toType"] = registration.ToType,
                        ["role"] = registration.Role,
                        ["cardinality"] = registration.Cardinality.ToString()
                    }
                );
                registry.Upsert(relationNode);

                // Create edge representing the relation
                var relationEdge = new Edge(
                    FromId: $"type-{registration.FromType}",
                    ToId: $"type-{registration.ToType}",
                    Role: registration.Role,
                    Weight: 1.0,
                    Meta: new Dictionary<string, object>
                    {
                        ["relationId"] = relationId,
                        ["cardinality"] = registration.Cardinality.ToString(),
                        ["moduleId"] = "codex.relations"
                    }
                );
                registry.Upsert(relationEdge);

                return new RelationRegistrationResponse(RelationId: relationId, Success: true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to register relation: {ex.Message}");
            }
        });

        router.Register("codex.relations", "validate", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var nodeId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(nodeId))
                {
                    return new ErrorResponse("Node ID is required");
                }

                // Get the node to validate
                if (!registry.TryGet(nodeId, out var node))
                {
                    return new ErrorResponse($"Node '{nodeId}' not found");
                }

                // Perform validation
                var validationResult = await ValidateNode(node, registry);

                return new ValidationResponse(
                    NodeId: nodeId,
                    Success: validationResult.IsValid,
                    Result: validationResult
                );
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to validate node: {ex.Message}");
            }
        });
    }

    private async Task<RelationsValidationResult> ValidateNode(Node node, NodeRegistry registry)
    {
        var issues = new List<RelationsValidationIssue>();

        // Get all relations for this node's type
        var relations = registry.GetEdgesFrom($"type-{node.TypeId}")
            .Where(e => e.Meta?.GetValueOrDefault("moduleId")?.ToString() == "codex.relations")
            .ToList();

        // Basic validation rules
        await Task.Run(() => ValidateBasicRules(node, issues));
        await Task.Run(() => ValidateRelations(node, relations, registry, issues));

        return new RelationsValidationResult(
            IsValid: !issues.Any(i => i.Severity == RelationsValidationSeverity.Error),
            Issues: issues
        );
    }

    private void ValidateBasicRules(Node node, List<RelationsValidationIssue> issues)
    {
        // Rule 1: Node must have an ID
        if (string.IsNullOrEmpty(node.Id))
        {
            issues.Add(new RelationsValidationIssue(
                Rule: "RequiredField",
                Message: "Node must have a non-empty ID",
                Severity: RelationsValidationSeverity.Error,
                Path: "id"
            ));
        }

        // Rule 2: Node must have a TypeId
        if (string.IsNullOrEmpty(node.TypeId))
        {
            issues.Add(new RelationsValidationIssue(
                Rule: "RequiredField",
                Message: "Node must have a non-empty TypeId",
                Severity: RelationsValidationSeverity.Error,
                Path: "typeId"
            ));
        }

        // Rule 3: Node must have a valid state
        if (!Enum.IsDefined(typeof(ContentState), node.State))
        {
            issues.Add(new RelationsValidationIssue(
                Rule: "ValidState",
                Message: $"Node state '{node.State}' is not valid",
                Severity: RelationsValidationSeverity.Error,
                Path: "state"
            ));
        }

        // Rule 4: Content must be valid if present
        if (node.Content != null)
        {
            if (string.IsNullOrEmpty(node.Content.MediaType))
            {
                issues.Add(new RelationsValidationIssue(
                    Rule: "ValidContent",
                    Message: "Content must have a MediaType when present",
                    Severity: RelationsValidationSeverity.Warning,
                    Path: "content.mediaType"
                ));
            }
        }
    }

    private void ValidateRelations(Node node, List<Edge> relations, NodeRegistry registry, List<RelationsValidationIssue> issues)
    {
        foreach (var relation in relations)
        {
            var cardinality = relation.Meta?.GetValueOrDefault("cardinality")?.ToString();
            var role = relation.Role;

            // Validate cardinality constraints
            if (!string.IsNullOrEmpty(cardinality) && Enum.TryParse<RelationsCardinality>(cardinality, out var cardinalityEnum))
            {
                ValidateCardinality(node, relation, cardinalityEnum, registry, issues);
            }

            // Validate role-specific constraints
            ValidateRoleConstraints(node, relation, registry, issues);
        }
    }

    private void ValidateCardinality(Node node, Edge relation, RelationsCardinality cardinality, NodeRegistry registry, List<RelationsValidationIssue> issues)
    {
        var relatedNodes = registry.GetEdgesFrom(node.Id)
            .Where(e => e.Role == relation.Role)
            .ToList();

        switch (cardinality)
        {
            case RelationsCardinality.OneToOne:
                if (relatedNodes.Count > 1)
                {
                    issues.Add(new RelationsValidationIssue(
                        Rule: "CardinalityViolation",
                        Message: $"One-to-one relation '{relation.Role}' has {relatedNodes.Count} targets (expected 1)",
                        Severity: RelationsValidationSeverity.Error,
                        Path: $"relations.{relation.Role}"
                    ));
                }
                break;

            case RelationsCardinality.OneToMany:
                // One-to-many is always valid (0 or more targets)
                break;

            case RelationsCardinality.ManyToOne:
                // Many-to-one is always valid (0 or more sources)
                break;

            case RelationsCardinality.ManyToMany:
                // Many-to-many is always valid
                break;
        }
    }

    private void ValidateRoleConstraints(Node node, Edge relation, NodeRegistry registry, List<RelationsValidationIssue> issues)
    {
        // Add role-specific validation logic here
        // For now, just check that the role is not empty
        if (string.IsNullOrEmpty(relation.Role))
        {
            issues.Add(new RelationsValidationIssue(
                Rule: "ValidRole",
                Message: "Relation role cannot be empty",
                Severity: RelationsValidationSeverity.Error,
                Path: "relations.role"
            ));
        }
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Relations module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint
    }
}
