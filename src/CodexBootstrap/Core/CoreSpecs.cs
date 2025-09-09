namespace CodexBootstrap.Core;

public static class CoreSpecs
{
    public static ModuleSpec CoreModuleSpec() => new(
        Id: "codex.core",
        Version: "0.1.0",
        Name: "Codex Core",
        Description: "Minimal self‑describing core for nodes, edges, types, and dynamic routing.",
        Dependencies: Array.Empty<ModuleRef>(),
        Types: new[]
        {
            new TypeSpec("Node","object", new()
            {
                ["id"] = new("id","string"),
                ["typeId"] = new("typeId","string"),
                ["state"] = new("state","string"),
                ["locale"] = new("locale","string"),
                ["title"] = new("title","string"),
                ["description"] = new("description","string"),
                ["content"] = new("content","ref", Ref:nameof(ContentRef)),
                ["meta"] = new("meta","object")
            }),
            new TypeSpec(nameof(ContentRef),"object", new()
            {
                ["mediaType"] = new("mediaType","string"),
                ["inlineJson"] = new("inlineJson","string"),
                ["inlineBytes"] = new("inlineBytes","string"),
                ["externalUri"] = new("externalUri","string")
            }),
            new TypeSpec("Edge","object", new()
            {
                ["fromId"] = new("fromId","string"),
                ["toId"] = new("toId","string"),
                ["role"] = new("role","string"),
                ["weight"] = new("weight","number"),
                ["meta"] = new("meta","object")
            }),
        },
        Apis: new[]
        {
            new ApiSpec("route","/route","Dynamic API dispatch", new("DynamicCall","object"), new("Any","object")),
            new ApiSpec("vaporize","/vaporize/{id}","Populate node content from its description", new("id","string"), new("Node","ref", Ref:"Node"))
        }
    );
}
