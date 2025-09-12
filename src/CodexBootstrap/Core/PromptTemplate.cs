using System.Collections.Generic;
using System.Text.Json;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Configurable prompt template system for LLM interactions
    /// </summary>
    public record PromptTemplate(
        string Id,
        string Name,
        string Template,
        Dictionary<string, object> DefaultParameters,
        CodexBootstrap.Modules.LLMConfig DefaultLLMConfig,
        string Category = "general"
    )
    {
        public string Render(Dictionary<string, object> parameters)
        {
            var template = Template;
            var allParams = new Dictionary<string, object>(DefaultParameters);
            
            // Override with provided parameters
            foreach (var param in parameters)
            {
                allParams[param.Key] = param.Value;
            }

            // Simple template rendering
            foreach (var param in allParams)
            {
                var placeholder = $"{{{param.Key}}}";
                var value = param.Value?.ToString() ?? "";
                template = template.Replace(placeholder, value);
            }

            return template;
        }
    }

    /// <summary>
    /// Repository for managing prompt templates as nodes
    /// </summary>
    public class PromptTemplateRepository
    {
        private readonly NodeRegistry _registry;

        public PromptTemplateRepository(NodeRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterTemplate(PromptTemplate template)
        {
            var node = NodeHelpers.CreateNode(
                id: $"prompt.{template.Id}",
                typeId: "codex.meta/prompt-template",
                state: ContentState.Ice,
                title: template.Name,
                description: $"Prompt template for {template.Category}",
                content: NodeHelpers.CreateJsonContent(new
                {
                    template = template.Template,
                    defaultParameters = template.DefaultParameters,
                    defaultLLMConfig = template.DefaultLLMConfig,
                    category = template.Category
                })
            );
            
            _registry.Upsert(node);
        }

        public PromptTemplate? GetTemplate(string id)
        {
            var node = _registry.GetNode($"prompt.{id}");
            if (node == null) return null;

            try
            {
                // Access the JSON content properly from ContentRef
                var jsonContent = node.Content.InlineJson ?? "{}";
                var content = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                if (content == null) return null;

                var rawPrompt = content.TryGetValue("template", out var template) ? template?.ToString() ?? "" : "";
                var defaultParams = content.TryGetValue("defaultParameters", out var params_) ? 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(params_)) ?? new() : new();
                var defaultLLMConfig = content.TryGetValue("defaultLLMConfig", out var llmConfigObj) ? 
                    JsonSerializer.Deserialize<CodexBootstrap.Modules.LLMConfig>(JsonSerializer.Serialize(llmConfigObj)) : null;

                return new PromptTemplate(
                    Id: id,
                    Name: node.Title,
                    Template: rawPrompt,
                    DefaultParameters: defaultParams,
                    DefaultLLMConfig: defaultLLMConfig ?? new("", "", "", "", "", "", 0, 0, 0, new()),
                    Category: content.TryGetValue("category", out var cat) ? cat?.ToString() ?? "" : "general"
                );
            }
            catch (Exception ex)
            {
                // Log the error and return null for corrupted templates
                Console.WriteLine($"Error parsing prompt template {id}: {ex.Message}");
                return null;
            }
        }

        public List<PromptTemplate> GetTemplatesByCategory(string category)
        {
            var nodes = _registry.GetNodesByType("codex.meta/prompt-template");
            var templates = new List<PromptTemplate>();

            foreach (var node in nodes)
            {
                try
                {
                    var content = JsonSerializer.Deserialize<Dictionary<string, object>>(node.Content.ToString());
                    if (content?["category"]?.ToString() == category)
                    {
                        var id = node.Id.Replace("prompt.", "");
                        var template = GetTemplate(id);
                        if (template != null) templates.Add(template);
                    }
                }
                catch
                {
                    // Skip invalid templates
                }
            }

            return templates;
        }
    }
}
