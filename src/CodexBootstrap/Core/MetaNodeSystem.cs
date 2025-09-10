using System.Text.Json;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// System for managing meta-nodes that represent all internal states and types
/// </summary>
public static class MetaNodeSystem
{
    /// <summary>
    /// Creates all core meta-nodes for the system
    /// </summary>
    public static IEnumerable<Node> CreateCoreMetaNodes()
    {
        var nodes = new List<Node>();

        // Content State meta-nodes
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/state/ice", "Ice", "Frozen, immutable state"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/state/water", "Water", "Liquid, mutable state"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/state/gas", "Gas", "Transient, derivable state"));

        // Type Kind meta-nodes
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/type-kind/object", "Object", "Object type", "TypeKind"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/type-kind/array", "Array", "Array type", "TypeKind"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/type-kind/reference", "Reference", "Reference type", "TypeKind"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/type-kind/enum", "Enum", "Enumeration type", "TypeKind"));
        nodes.Add(NodeHelpers.CreateStateMetaNode("codex.meta/type-kind/primitive", "Primitive", "Primitive type", "TypeKind"));

        // Core type meta-nodes
        nodes.Add(CreateNodeTypeMetaNode());
        nodes.Add(CreateEdgeTypeMetaNode());
        nodes.Add(CreateContentRefTypeMetaNode());
        nodes.Add(CreateTypeSpecTypeMetaNode());
        nodes.Add(CreateFieldSpecTypeMetaNode());
        nodes.Add(CreateApiSpecTypeMetaNode());
        nodes.Add(CreateModuleSpecTypeMetaNode());

        // Response type meta-nodes
        nodes.Add(CreateErrorResponseTypeMetaNode());
        nodes.Add(CreateSuccessResponseTypeMetaNode());

        return nodes;
    }

    /// <summary>
    /// Creates meta-nodes for all response types used in modules
    /// </summary>
    public static IEnumerable<Node> CreateResponseMetaNodes()
    {
        var nodes = new List<Node>();

        // Breath module responses
        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/expand", "ExpandResponse", 
            "Response for expand operations", new[]
            {
                new FieldSpec("id", "string", true, "Node identifier"),
                new FieldSpec("phase", "string", true, "Current phase"),
                new FieldSpec("expanded", "boolean", true, "Whether expansion succeeded"),
                new FieldSpec("message", "string", false, "Optional message")
            }));

        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/validate", "ValidateResponse", 
            "Response for validate operations", new[]
            {
                new FieldSpec("id", "string", true, "Node identifier"),
                new FieldSpec("valid", "boolean", true, "Whether validation succeeded"),
                new FieldSpec("message", "string", true, "Validation message")
            }));

        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/contract", "ContractResponse", 
            "Response for contract operations", new[]
            {
                new FieldSpec("id", "string", true, "Node identifier"),
                new FieldSpec("phase", "string", true, "Current phase"),
                new FieldSpec("contracted", "boolean", true, "Whether contraction succeeded"),
                new FieldSpec("message", "string", false, "Optional message")
            }));

        // Concept module responses
        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/concept-create", "ConceptCreateResponse", 
            "Response for concept creation", new[]
            {
                new FieldSpec("success", "boolean", true, "Whether creation succeeded"),
                new FieldSpec("conceptId", "string", true, "Created concept identifier"),
                new FieldSpec("message", "string", true, "Response message")
            }));

        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/concept-define", "ConceptDefineResponse", 
            "Response for concept definition", new[]
            {
                new FieldSpec("conceptId", "string", true, "Concept identifier"),
                new FieldSpec("properties", "object", true, "Defined properties"),
                new FieldSpec("message", "string", true, "Response message")
            }));

        // User concept module responses
        nodes.Add(NodeHelpers.CreateResponseMetaNode("codex.meta/response/user-concept-link", "UserConceptLinkResponse", 
            "Response for user-concept linking", new[]
            {
                new FieldSpec("success", "boolean", true, "Whether linking succeeded"),
                new FieldSpec("relationshipId", "string", true, "Relationship identifier"),
                new FieldSpec("relationshipType", "string", true, "Type of relationship"),
                new FieldSpec("weight", "number", true, "Relationship weight"),
                new FieldSpec("message", "string", true, "Response message")
            }));

        return nodes;
    }

    /// <summary>
    /// Creates meta-nodes for all request types used in modules
    /// </summary>
    public static IEnumerable<Node> CreateRequestMetaNodes()
    {
        var nodes = new List<Node>();

        // Breath module requests
        nodes.Add(NodeHelpers.CreateTypeMetaNode("codex.meta/type/breath-loop-request", "BreathLoopRequest", 
            "Request for breath loop operations", new[]
            {
                new FieldSpec("id", "string", true, "Node identifier"),
                new FieldSpec("operations", "array", false, "List of operations to perform")
            }));

        // Concept module requests
        nodes.Add(NodeHelpers.CreateTypeMetaNode("codex.meta/type/concept-create-request", "ConceptCreateRequest", 
            "Request for concept creation", new[]
            {
                new FieldSpec("name", "string", true, "Concept name"),
                new FieldSpec("description", "string", true, "Concept description"),
                new FieldSpec("domain", "string", true, "Concept domain"),
                new FieldSpec("complexity", "string", true, "Concept complexity"),
                new FieldSpec("tags", "array", true, "Concept tags")
            }));

        // User concept module requests
        nodes.Add(NodeHelpers.CreateTypeMetaNode("codex.meta/type/user-concept-link-request", "UserConceptLinkRequest", 
            "Request for user-concept linking", new[]
            {
                new FieldSpec("userId", "string", true, "User identifier"),
                new FieldSpec("conceptId", "string", true, "Concept identifier"),
                new FieldSpec("relationshipType", "string", true, "Type of relationship"),
                new FieldSpec("weight", "number", true, "Relationship weight")
            }));

        return nodes;
    }

    private static Node CreateNodeTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/node", "Node", 
            "Core node entity", new[]
            {
                new FieldSpec("id", "string", true, "Unique identifier"),
                new FieldSpec("typeId", "string", true, "Type identifier"),
                new FieldSpec("state", "string", true, "Content state"),
                new FieldSpec("locale", "string", false, "Locale information"),
                new FieldSpec("title", "string", false, "Display title"),
                new FieldSpec("description", "string", false, "Description"),
                new FieldSpec("content", "ContentRef", false, "Content reference"),
                new FieldSpec("meta", "object", false, "Metadata dictionary")
            });
    }

    private static Node CreateEdgeTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/edge", "Edge", 
            "Core edge entity", new[]
            {
                new FieldSpec("fromId", "string", true, "Source node identifier"),
                new FieldSpec("toId", "string", true, "Target node identifier"),
                new FieldSpec("role", "string", true, "Edge role"),
                new FieldSpec("weight", "number", false, "Edge weight"),
                new FieldSpec("meta", "object", false, "Edge metadata")
            });
    }

    private static Node CreateContentRefTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/content-ref", "ContentRef", 
            "Content reference", new[]
            {
                new FieldSpec("mediaType", "string", false, "MIME type"),
                new FieldSpec("inlineJson", "string", false, "Inline JSON content"),
                new FieldSpec("inlineBytes", "array", false, "Inline binary content"),
                new FieldSpec("externalUri", "string", false, "External URI"),
                new FieldSpec("selector", "string", false, "Content selector"),
                new FieldSpec("query", "string", false, "Content query"),
                new FieldSpec("headers", "object", false, "HTTP headers"),
                new FieldSpec("authRef", "string", false, "Authentication reference"),
                new FieldSpec("cacheKey", "string", false, "Cache key")
            });
    }

    private static Node CreateTypeSpecTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/type-spec", "TypeSpec", 
            "Type specification", new[]
            {
                new FieldSpec("name", "string", true, "Type name"),
                new FieldSpec("description", "string", false, "Type description"),
                new FieldSpec("fields", "array", false, "Field specifications"),
                new FieldSpec("kind", "string", true, "Type kind"),
                new FieldSpec("arrayItemType", "string", false, "Array item type"),
                new FieldSpec("referenceType", "string", false, "Reference type"),
                new FieldSpec("enumValues", "array", false, "Enum values")
            });
    }

    private static Node CreateFieldSpecTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/field-spec", "FieldSpec", 
            "Field specification", new[]
            {
                new FieldSpec("name", "string", true, "Field name"),
                new FieldSpec("type", "string", true, "Field type"),
                new FieldSpec("required", "boolean", true, "Whether field is required"),
                new FieldSpec("description", "string", false, "Field description"),
                new FieldSpec("kind", "string", true, "Field kind"),
                new FieldSpec("arrayItemType", "string", false, "Array item type"),
                new FieldSpec("referenceType", "string", false, "Reference type"),
                new FieldSpec("enumValues", "array", false, "Enum values")
            });
    }

    private static Node CreateApiSpecTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/api-spec", "ApiSpec", 
            "API specification", new[]
            {
                new FieldSpec("name", "string", true, "API name"),
                new FieldSpec("verb", "string", true, "HTTP verb"),
                new FieldSpec("route", "string", true, "API route"),
                new FieldSpec("description", "string", false, "API description"),
                new FieldSpec("parameters", "array", false, "API parameters")
            });
    }

    private static Node CreateModuleSpecTypeMetaNode()
    {
        return NodeHelpers.CreateTypeMetaNode("codex.meta/type/module-spec", "ModuleSpec", 
            "Module specification", new[]
            {
                new FieldSpec("id", "string", true, "Module identifier"),
                new FieldSpec("name", "string", true, "Module name"),
                new FieldSpec("version", "string", true, "Module version"),
                new FieldSpec("description", "string", false, "Module description"),
                new FieldSpec("title", "string", false, "Module title"),
                new FieldSpec("dependencies", "array", false, "Module dependencies"),
                new FieldSpec("types", "array", false, "Module types"),
                new FieldSpec("apis", "array", false, "Module APIs")
            });
    }

    private static Node CreateErrorResponseTypeMetaNode()
    {
        return NodeHelpers.CreateResponseMetaNode("codex.meta/response/error", "ErrorResponse", 
            "Error response", new[]
            {
                new FieldSpec("error", "string", true, "Error message"),
                new FieldSpec("code", "string", false, "Error code"),
                new FieldSpec("details", "object", false, "Error details")
            });
    }

    private static Node CreateSuccessResponseTypeMetaNode()
    {
        return NodeHelpers.CreateResponseMetaNode("codex.meta/response/success", "SuccessResponse", 
            "Success response", new[]
            {
                new FieldSpec("success", "boolean", true, "Success indicator"),
                new FieldSpec("message", "string", false, "Success message"),
                new FieldSpec("data", "object", false, "Response data")
            });
    }
}

