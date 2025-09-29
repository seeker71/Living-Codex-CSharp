using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// Helper functions for common node operations to reduce repetition and ensure consistency
/// </summary>
public static class NodeHelpers
{
    /// <summary>
    /// Creates a meta-node for type definitions
    /// </summary>
    public static Node CreateTypeMetaNode(string typeId, string name, string description, 
        IReadOnlyList<FieldSpec>? fields = null, TypeKind kind = TypeKind.Object)
    {
        return new Node(
            Id: typeId,
            TypeId: "codex.meta/type",
            State: ContentState.Water,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = name,
                    description = description,
                    kind = kind.ToString().ToLowerInvariant(),
                    fields = fields?.Select(f => new
                    {
                        name = f.Name,
                        type = f.Type,
                        required = f.Required,
                        description = f.Description,
                        kind = f.Kind.ToString().ToLowerInvariant(),
                        arrayItemType = f.ArrayItemType,
                        referenceType = f.ReferenceType,
                        enumValues = f.EnumValues
                    }).ToArray()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["typeName"] = name,
                ["kind"] = kind.ToString().ToLowerInvariant(),
                ["isMetaNode"] = true
            }
        );
    }

    /// <summary>
    /// Creates a meta-node for API definitions
    /// </summary>
    public static Node CreateApiMetaNode(string apiId, string moduleId, string apiName, 
        string route, string description, IReadOnlyList<ParameterSpec>? parameters = null)
    {
        return new Node(
            Id: apiId,
            TypeId: "codex.meta/api",
            State: ContentState.Water,
            Locale: "en",
            Title: apiName,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    moduleId = moduleId,
                    apiName = apiName,
                    route = route,
                    description = description,
                    parameters = parameters?.Select(p => new
                    {
                        name = p.Name,
                        type = p.Type,
                        required = p.Required,
                        description = p.Description
                    }).ToArray()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["apiName"] = apiName,
                ["route"] = route,
                ["isMetaNode"] = true
            }
        );
    }

    /// <summary>
    /// Creates a meta-node for module definitions
    /// </summary>
    public static Node CreateModuleMetaNode(string moduleId, string name, string version, 
        string description, IReadOnlyList<ModuleRef>? dependencies = null)
    {
        return new Node(
            Id: moduleId,
            TypeId: "codex.meta/module",
            State: ContentState.Water,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    id = moduleId,
                    name = name,
                    version = version,
                    description = description,
                    dependencies = dependencies?.Select(d => new
                    {
                        id = d.Id,
                        version = d.Version
                    }).ToArray()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["version"] = version,
                ["name"] = name,
                ["isMetaNode"] = true
            }
        );
    }

    /// <summary>
    /// Creates a state meta-node for content states
    /// </summary>
    public static Node CreateStateMetaNode(string stateId, string name, string description, 
        string parentType = "ContentState")
    {
        return new Node(
            Id: stateId,
            TypeId: "codex.meta/state",
            State: ContentState.Water,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = name,
                    description = description,
                    parentType = parentType,
                    value = name.ToLowerInvariant()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["stateName"] = name,
                ["parentType"] = parentType,
                ["value"] = name.ToLowerInvariant(),
                ["isMetaNode"] = true
            }
        );
    }

    /// <summary>
    /// Creates a response meta-node for API responses
    /// </summary>
    public static Node CreateResponseMetaNode(string responseId, string name, string description, 
        IReadOnlyList<FieldSpec>? fields = null)
    {
        return new Node(
            Id: responseId,
            TypeId: "codex.meta/response",
            State: ContentState.Water,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = name,
                    description = description,
                    fields = fields?.Select(f => new
                    {
                        name = f.Name,
                        type = f.Type,
                        required = f.Required,
                        description = f.Description
                    }).ToArray()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["responseName"] = name,
                ["isMetaNode"] = true
            }
        );
    }

    /// <summary>
    /// Creates an edge between two nodes
    /// </summary>
    public static Edge CreateEdge(string fromId, string toId, string role, double? weight = null, 
        Dictionary<string, object>? meta = null, string? roleId = null)
    {
        var m = meta ?? new Dictionary<string, object>();
        if (roleId != null)
        {
            m["roleId"] = roleId;
        }
        return new Edge(
            FromId: fromId,
            ToId: toId,
            Role: role,
            RoleId: roleId,
            Weight: weight ?? 1.0,
            Meta: m
        );
    }

    /// <summary>
    /// Attempts to resolve a relationship role string to a relationship type node id (codex.relationship.core)
    /// </summary>
    public static string? TryResolveRoleId(INodeRegistry registry, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(role)) return null;
            var rels = registry.GetNodesByType("codex.relationship.core");
            var match = rels.FirstOrDefault(n => string.Equals(n.Title, role, StringComparison.OrdinalIgnoreCase))
                        ?? rels.FirstOrDefault(n => (n.Meta != null && n.Meta.TryGetValue("name", out var v) && string.Equals(v?.ToString(), role, StringComparison.OrdinalIgnoreCase)))
                        ?? rels.FirstOrDefault(n => n.Id.EndsWith(role, StringComparison.OrdinalIgnoreCase));
            return match?.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a module-to-api edge
    /// </summary>
    public static Edge CreateModuleApiEdge(string moduleId, string apiId)
    {
        return CreateEdge(moduleId, apiId, "exposes", 1.0, new Dictionary<string, object>
        {
            ["relationship"] = "module-exposes-api"
        });
    }

    /// <summary>
    /// Creates a type-to-field edge
    /// </summary>
    public static Edge CreateTypeFieldEdge(string typeId, string fieldId)
    {
        return CreateEdge(typeId, fieldId, "has_field", 1.0, new Dictionary<string, object>
        {
            ["relationship"] = "type-has-field"
        });
    }

    /// <summary>
    /// Creates a module-to-type edge
    /// </summary>
    public static Edge CreateModuleTypeEdge(string moduleId, string typeId)
    {
        return CreateEdge(moduleId, typeId, "defines", 1.0, new Dictionary<string, object>
        {
            ["relationship"] = "module-defines-type"
        });
    }

    /// <summary>
    /// Creates a module-to-response edge
    /// </summary>
    public static Edge CreateModuleResponseEdge(string moduleId, string responseId)
    {
        return CreateEdge(moduleId, responseId, "uses", 1.0, new Dictionary<string, object>
        {
            ["relationship"] = "module-uses-response"
        });
    }

    /// <summary>
    /// Gets a field value from node meta with type safety
    /// </summary>
    public static T? GetMetaValue<T>(Node node, string key, T? defaultValue = default)
    {
        if (node.Meta?.TryGetValue(key, out var value) == true)
        {
            if (value is T directValue)
                return directValue;
            
            if (value is JsonElement jsonElement)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a field value in node meta
    /// </summary>
    public static Dictionary<string, object> SetMetaValue(Dictionary<string, object>? meta, string key, object value)
    {
        var result = meta is null ? new Dictionary<string, object>() : new Dictionary<string, object>(meta);
        result[key] = value;
        return result;
    }

    /// <summary>
    /// Creates a content reference with JSON content
    /// </summary>
    public static ContentRef CreateJsonContent(object content, string? mediaType = "application/json")
    {
        return new ContentRef(
            MediaType: mediaType,
            InlineJson: JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
            }),
            InlineBytes: null,
            ExternalUri: null
        );
    }

    /// <summary>
    /// Creates a content reference with external URI
    /// </summary>
    public static ContentRef CreateExternalContent(string uri, string? mediaType = null, 
        Dictionary<string, string>? headers = null, string? authRef = null)
    {
        return new ContentRef(
            MediaType: mediaType,
            InlineJson: null,
            InlineBytes: null,
            ExternalUri: new Uri(uri),
            Headers: headers,
            AuthRef: authRef
        );
    }

    /// <summary>
    /// Creates a node with standard meta fields
    /// </summary>
    public static Node CreateNode(string id, string typeId, ContentState state, string? title = null, 
        string? description = null, ContentRef? content = null, Dictionary<string, object>? additionalMeta = null)
    {
        var meta = new Dictionary<string, object>
        {
            ["createdAt"] = DateTime.UtcNow,
            ["state"] = state.ToString().ToLowerInvariant() // Keep lowercase for meta, but enum serialization will handle the actual State property
        };

        if (additionalMeta != null)
        {
            foreach (var kvp in additionalMeta)
            {
                meta[kvp.Key] = kvp.Value;
            }
        }

        return new Node(
            Id: id,
            TypeId: typeId,
            State: state,
            Locale: "en",
            Title: title,
            Description: description,
            Content: content,
            Meta: meta
        );
    }
}
