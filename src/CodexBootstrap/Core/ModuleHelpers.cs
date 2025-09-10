using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Module Helpers - Utility functions for module operations
/// </summary>
public static class ModuleHelpers
{
    /// <summary>
    /// Create a module node with standard structure
    /// </summary>
    public static Node CreateModuleNode(string moduleId, string name, string version, string description)
    {
        return new Node(
            Id: moduleId,
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = moduleId,
                    Name = name,
                    Version = version,
                    Description = description
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["name"] = name,
                ["version"] = version,
                ["description"] = description,
                ["createdAt"] = DateTime.UtcNow
            }
        );
    }

    /// <summary>
    /// Register an API handler with the router
    /// </summary>
    public static void RegisterApiHandler(IApiRouter router, string moduleId, string apiName, Func<JsonElement?, Task<object>> handler)
    {
        router.Register(moduleId, apiName, handler);
    }
}
