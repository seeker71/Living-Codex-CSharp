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
                new FieldSpec("id", "string", true, "Unique identifier"),
                new FieldSpec("typeId", "string", true, "Type identifier"),
                new FieldSpec("state", "string", true, "Content state (Ice/Water/Gas)"),
                new FieldSpec("locale", "string", false, "Locale information"),
                new FieldSpec("title", "string", false, "Display title"),
                new FieldSpec("description", "string", false, "Description"),
                new FieldSpec("content", "ContentRef", false, "Content reference"),
                new FieldSpec("meta", "object", false, "Metadata dictionary")
            }),
            new TypeSpec("ContentRef", "Content reference", new[]
            {
                new FieldSpec("mediaType", "string", false, "MIME type"),
                new FieldSpec("inlineJson", "string", false, "Inline JSON content"),
                new FieldSpec("inlineBytes", "byte[]", false, "Inline binary content"),
                new FieldSpec("externalUri", "string", false, "External URI"),
                new FieldSpec("selector", "string", false, "Content selector"),
                new FieldSpec("query", "string", false, "Query string"),
                new FieldSpec("headers", "object", false, "HTTP headers"),
                new FieldSpec("authRef", "string", false, "Authentication reference"),
                new FieldSpec("cacheKey", "string", false, "Cache key")
            }),
            new TypeSpec("Edge", "Relationship between nodes", new[]
            {
                new FieldSpec("fromId", "string", true, "Source node ID"),
                new FieldSpec("toId", "string", true, "Target node ID"),
                new FieldSpec("role", "string", true, "Relationship role"),
                new FieldSpec("weight", "number", false, "Relationship weight"),
                new FieldSpec("meta", "object", false, "Edge metadata")
            })
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
