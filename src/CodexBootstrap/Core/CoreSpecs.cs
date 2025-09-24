namespace CodexBootstrap.Core;

public static class CoreSpecs
{
    public static ModuleSpec CoreModuleSpec() => new(
        Id: "codex.core",
        Name: "Codex Core",
        Version: "0.1.0",
        Description: "Minimal self‑describing core for nodes, edges, types, and dynamic routing.",
        Title: "Codex Core",
        Dependencies: Array.Empty<ModuleRef>(),
        Types: new[]
        {
            new TypeSpec("Node", "Core node entity", new[]
            {
                new FieldSpec("id", "string", true, "Unique identifier", TypeKind.Primitive),
                new FieldSpec("typeId", "string", true, "Type identifier", TypeKind.Primitive),
                new FieldSpec("state", "string", true, "Content state (Ice/Water/Gas)", TypeKind.Enum, EnumValues: new[] { "Ice", "Water", "Gas" }),
                new FieldSpec("locale", "string", false, "Locale information", TypeKind.Primitive),
                new FieldSpec("title", "string", false, "Display title", TypeKind.Primitive),
                new FieldSpec("description", "string", false, "Description", TypeKind.Primitive),
                new FieldSpec("content", "ContentRef", false, "Content reference", TypeKind.Reference, ReferenceType: "ContentRef"),
                new FieldSpec("meta", "object", false, "Metadata dictionary", TypeKind.Object)
            }, TypeKind.Object),
            new TypeSpec("ContentRef", "Content reference", new[]
            {
                new FieldSpec("mediaType", "string", false, "MIME type", TypeKind.Primitive),
                new FieldSpec("inlineJson", "string", false, "Inline JSON content", TypeKind.Primitive),
                new FieldSpec("inlineBytes", "byte[]", false, "Inline binary content", TypeKind.Array, ArrayItemType: "byte"),
                new FieldSpec("externalUri", "string", false, "External URI", TypeKind.Primitive),
                new FieldSpec("selector", "string", false, "Content selector", TypeKind.Primitive),
                new FieldSpec("query", "string", false, "Query string", TypeKind.Primitive),
                new FieldSpec("headers", "object", false, "HTTP headers", TypeKind.Object),
                new FieldSpec("authRef", "string", false, "Authentication reference", TypeKind.Primitive),
                new FieldSpec("cacheKey", "string", false, "Cache key", TypeKind.Primitive)
            }, TypeKind.Object),
            new TypeSpec("Edge", "Relationship between nodes", new[]
            {
                new FieldSpec("fromId", "string", true, "Source node ID", TypeKind.Primitive),
                new FieldSpec("toId", "string", true, "Target node ID", TypeKind.Primitive),
                new FieldSpec("role", "string", true, "Relationship role", TypeKind.Primitive),
                new FieldSpec("roleId", "string", false, "Relationship type node id (codex.relationship.core)", TypeKind.Primitive),
                new FieldSpec("weight", "number", false, "Relationship weight", TypeKind.Primitive),
                new FieldSpec("meta", "object", false, "Edge metadata", TypeKind.Object)
            }, TypeKind.Object)
        },
        Apis: new[]
        {
            new ApiSpec("route", "POST", "/route", "Dynamic API dispatch", new[]
            {
                new ParameterSpec("moduleId", "string", true, "Module identifier"),
                new ParameterSpec("api", "string", true, "API name"),
                new ParameterSpec("args", "object", false, "API arguments")
            }),
            new ApiSpec("hydrate", "POST", "/hydrate/{id}", "Populate node content from its description", new[]
            {
                new ParameterSpec("id", "string", true, "Node identifier")
            })
        }
    );
}
