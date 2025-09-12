using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Dynamic Attribution System - Replaces static descriptions with real-time LLM responses
/// Uses reflection to transform static content into dynamic, contextually aware responses
/// </summary>
[MetaNodeAttribute("codex.dynamic.attribution", "codex.meta/type", "DynamicAttributionSystem", "Dynamic LLM-powered attribution system")]
[ApiType(
    Name = "Dynamic Attribution System",
    Type = "object",
    Description = "System for replacing static descriptions with real-time LLM responses using joyful future engine",
    Example = @"{
      ""id"": ""dynamic-attribution-v1"",
      ""version"": ""1.0.0"",
      ""llmProvider"": ""ollama-local"",
      ""joyfulEngine"": ""ucore-joy"",
      ""reflectionEnabled"": true,
      ""cacheEnabled"": true,
      ""cacheTimeout"": 300
    }"
)]
public class DynamicAttributionSystem
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, CachedResponse> _responseCache;
    private readonly string _llmProvider;
    private readonly string _joyfulEngine;

    public DynamicAttributionSystem(IApiRouter apiRouter, NodeRegistry registry, string llmProvider = "ollama-local", string joyfulEngine = "ucore-joy")
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _responseCache = new Dictionary<string, CachedResponse>();
        _llmProvider = llmProvider;
        _joyfulEngine = joyfulEngine;
    }

    /// <summary>
    /// Dynamic Description Attribute - Marks properties for dynamic LLM generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public class DynamicDescriptionAttribute : Attribute
    {
        public string PromptTemplate { get; }
        public string ContextType { get; }
        public int CacheTimeoutSeconds { get; }
        public bool UseJoyfulEngine { get; }
        public string[] RequiredContext { get; }

        public DynamicDescriptionAttribute(
            string promptTemplate = "",
            string contextType = "general",
            int cacheTimeoutSeconds = 300,
            bool useJoyfulEngine = true,
            params string[] requiredContext)
        {
            PromptTemplate = promptTemplate;
            ContextType = contextType;
            CacheTimeoutSeconds = cacheTimeoutSeconds;
            UseJoyfulEngine = useJoyfulEngine;
            RequiredContext = requiredContext;
        }
    }

    /// <summary>
    /// Dynamic Content Attribute - Marks content for dynamic generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class DynamicContentAttribute : Attribute
    {
        public string ContentType { get; }
        public string GenerationStrategy { get; }
        public bool RealTime { get; }

        public DynamicContentAttribute(
            string contentType = "description",
            string generationStrategy = "joyful",
            bool realTime = true)
        {
            ContentType = contentType;
            GenerationStrategy = generationStrategy;
            RealTime = realTime;
        }
    }

    /// <summary>
    /// Generate dynamic content for a property using reflection
    /// </summary>
    public async Task<string> GenerateDynamicContent(
        object target, 
        PropertyInfo property, 
        Dictionary<string, object>? context = null)
    {
        var attribute = property.GetCustomAttribute<DynamicDescriptionAttribute>();
        if (attribute == null)
        {
            return GetStaticValue(target, property)?.ToString() ?? "";
        }

        var cacheKey = GenerateCacheKey(target, property, context);
        
        // Check cache first
        if (_responseCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Content;
        }

        // Generate new content
        var prompt = BuildPrompt(attribute, target, property, context);
        var content = await GenerateLLMResponse(prompt, attribute.ContextType, attribute.UseJoyfulEngine);
        
        // Cache the response
        _responseCache[cacheKey] = new CachedResponse(
            Content: content,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddSeconds(attribute.CacheTimeoutSeconds)
        );

        return content;
    }

    /// <summary>
    /// Generate dynamic content for a method using reflection
    /// </summary>
    public async Task<string> GenerateDynamicContent(
        object target, 
        MethodInfo method, 
        Dictionary<string, object>? context = null)
    {
        var attribute = method.GetCustomAttribute<DynamicDescriptionAttribute>();
        if (attribute == null)
        {
            return method.Name;
        }

        var cacheKey = GenerateCacheKey(target, method, context);
        
        // Check cache first
        if (_responseCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Content;
        }

        // Generate new content
        var prompt = BuildPrompt(attribute, target, method, context);
        var content = await GenerateLLMResponse(prompt, attribute.ContextType, attribute.UseJoyfulEngine);
        
        // Cache the response
        _responseCache[cacheKey] = new CachedResponse(
            Content: content,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddSeconds(attribute.CacheTimeoutSeconds)
        );

        return content;
    }

    /// <summary>
    /// Generate dynamic content for a class using reflection
    /// </summary>
    public async Task<string> GenerateDynamicContent(
        Type type, 
        Dictionary<string, object>? context = null)
    {
        var attribute = type.GetCustomAttribute<DynamicContentAttribute>();
        if (attribute == null)
        {
            return type.Name;
        }

        var cacheKey = GenerateCacheKey(type, context);
        
        // Check cache first
        if (_responseCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Content;
        }

        // Generate new content based on class attributes
        var prompt = BuildClassPrompt(type, attribute, context);
        var content = await GenerateLLMResponse(prompt, attribute.ContentType, true);
        
        // Cache the response
        _responseCache[cacheKey] = new CachedResponse(
            Content: content,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddSeconds(300)
        );

        return content;
    }

    /// <summary>
    /// Replace all static descriptions in a module with dynamic content
    /// </summary>
    public async Task<Dictionary<string, string>> ReplaceStaticDescriptions(
        object module, 
        Dictionary<string, object>? context = null)
    {
        var results = new Dictionary<string, string>();
        var type = module.GetType();

        // Process properties
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<DynamicDescriptionAttribute>() != null)
            {
                var content = await GenerateDynamicContent(module, property, context);
                results[property.Name] = content;
            }
        }

        // Process methods
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.GetCustomAttribute<DynamicDescriptionAttribute>() != null)
            {
                var content = await GenerateDynamicContent(module, method, context);
                results[method.Name] = content;
            }
        }

        // Process class-level content
        if (type.GetCustomAttribute<DynamicContentAttribute>() != null)
        {
            var content = await GenerateDynamicContent(type, context);
            results["ClassDescription"] = content;
        }

        return results;
    }

    /// <summary>
    /// Get dynamic content via reflection for any marked property
    /// </summary>
    public async Task<object> GetDynamicPropertyValue(
        object target, 
        string propertyName, 
        Dictionary<string, object>? context = null)
    {
        var type = target.GetType();
        var property = type.GetProperty(propertyName);
        
        if (property == null)
        {
            return null!;
        }

        var attribute = property.GetCustomAttribute<DynamicDescriptionAttribute>();
        if (attribute == null)
        {
            return property.GetValue(target);
        }

        var content = await GenerateDynamicContent(target, property, context);
        return content;
    }

    /// <summary>
    /// Invoke dynamic method with LLM-generated content
    /// </summary>
    public async Task<object> InvokeDynamicMethod(
        object target, 
        string methodName, 
        object[] parameters = null, 
        Dictionary<string, object>? context = null)
    {
        var type = target.GetType();
        var method = type.GetMethod(methodName);
        
        if (method == null)
        {
            return null!;
        }

        var attribute = method.GetCustomAttribute<DynamicDescriptionAttribute>();
        if (attribute == null)
        {
            return method.Invoke(target, parameters);
        }

        // Generate dynamic content for method execution
        var content = await GenerateDynamicContent(target, method, context);
        
        // Store the dynamic content in context for method execution
        if (context == null) context = new Dictionary<string, object>();
        context["DynamicContent"] = content;
        
        return method.Invoke(target, parameters);
    }

    // Helper methods

    private string GenerateCacheKey(object target, MemberInfo member, Dictionary<string, object>? context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"{target.GetType().Name}.{member.Name}.{contextHash}";
    }

    private string GenerateCacheKey(Type type, Dictionary<string, object>? context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"{type.Name}.Class.{contextHash}";
    }

    private object GetStaticValue(object target, PropertyInfo property)
    {
        try
        {
            return property.GetValue(target);
        }
        catch
        {
            return null!;
        }
    }

    private string BuildPrompt(
        DynamicDescriptionAttribute attribute, 
        object target, 
        MemberInfo member, 
        Dictionary<string, object>? context)
    {
        var basePrompt = attribute.PromptTemplate;
        if (string.IsNullOrEmpty(basePrompt))
        {
            basePrompt = GenerateDefaultPrompt(member, attribute.ContextType);
        }

        // Replace placeholders with actual values
        var prompt = basePrompt
            .Replace("{memberName}", member.Name)
            .Replace("{memberType}", member.MemberType.ToString())
            .Replace("{targetType}", target.GetType().Name)
            .Replace("{contextType}", attribute.ContextType);

        // Add context information
        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt = prompt.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
        }

        // Add joyful engine context if enabled
        if (attribute.UseJoyfulEngine)
        {
            prompt += "\n\nGenerate this content with joy, positivity, and spiritual resonance. Use U-CORE frequencies (432Hz, 528Hz, 741Hz) and consciousness-expanding language.";
        }

        return prompt;
    }

    private string BuildClassPrompt(
        Type type, 
        DynamicContentAttribute attribute, 
        Dictionary<string, object>? context)
    {
        var prompt = $"Generate a dynamic, joyful description for the {type.Name} class. ";
        
        if (attribute.ContentType == "description")
        {
            prompt += "Focus on what this class does and how it contributes to the U-CORE system. ";
        }
        else if (attribute.ContentType == "purpose")
        {
            prompt += "Explain the purpose and spiritual significance of this class. ";
        }

        // Add context information
        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt += $"{kvp.Key}: {kvp.Value}. ";
            }
        }

        prompt += "\n\nUse joyful, consciousness-expanding language with U-CORE frequencies and spiritual resonance.";

        return prompt;
    }

    private string GenerateDefaultPrompt(MemberInfo member, string contextType)
    {
        return contextType.ToLowerInvariant() switch
        {
            "description" => $"Generate a joyful, dynamic description for {member.Name} that explains its purpose and spiritual significance in the U-CORE system.",
            "purpose" => $"Explain the purpose and spiritual purpose of {member.Name} in consciousness expansion and reality transformation.",
            "functionality" => $"Describe how {member.Name} functions within the U-CORE framework and its role in consciousness evolution.",
            "spiritual" => $"Provide a spiritual and consciousness-expanding explanation of {member.Name} and its connection to universal frequencies.",
            _ => $"Generate a dynamic, joyful description for {member.Name} that captures its essence and purpose."
        };
    }

    private async Task<string> GenerateLLMResponse(string prompt, string contextType, bool useJoyfulEngine)
    {
        try
        {
            // Get optimal LLM configuration for dynamic attribution
            var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("consciousness-expansion");
            
            // Build enhanced prompt for dynamic attribution
            var enhancedPrompt = useJoyfulEngine 
                ? $@"{prompt}

Please respond in a joyful, consciousness-expanding way that:
- Uses sacred frequencies (432Hz, 528Hz, 741Hz) when relevant
- Incorporates spiritual and consciousness-expanding language
- Maintains a positive, uplifting tone
- Connects to universal love and wisdom
- Uses emojis and symbols to enhance the message

Context Type: {contextType}
LLM Provider: {_llmProvider}
Joyful Engine: {_joyfulEngine}"
                : $@"{prompt}

Please provide a clear, informative response that explains the {contextType} in a professional manner.

Context Type: {contextType}
LLM Provider: {_llmProvider}";

            // Convert LLMConfigurationSystem.LLMConfiguration to LLMConfig
            var llmConfig = new LLMConfig(
                Id: optimalConfig.Id,
                Name: optimalConfig.Id,
                Provider: optimalConfig.Provider,
                Model: optimalConfig.Model,
                ApiKey: "",
                BaseUrl: "http://localhost:11434",
                MaxTokens: optimalConfig.MaxTokens,
                Temperature: optimalConfig.Temperature,
                TopP: optimalConfig.TopP,
                Parameters: optimalConfig.Parameters
            );
            
            // Call LLM with real Ollama integration
            var llmResponse = await CallLLM(llmConfig, enhancedPrompt);
            
            if (llmResponse.Content.Contains("LLM unavailable"))
            {
                // Fallback to simulated response
                return useJoyfulEngine 
                    ? GenerateJoyfulResponse(prompt, contextType)
                    : GenerateStandardResponse(prompt, contextType);
            }
            
            return llmResponse.Content;
        }
        catch (Exception)
        {
            // Fallback to simulated response on error
            return useJoyfulEngine 
                ? GenerateJoyfulResponse(prompt, contextType)
                : GenerateStandardResponse(prompt, contextType);
        }
    }

    private string GenerateJoyfulResponse(string prompt, string contextType)
    {
        var responses = new Dictionary<string, string[]>
        {
            ["description"] = new[]
            {
                "🌟 This beautiful component radiates with the frequency of 432Hz, bringing heart-centered consciousness to every interaction. It serves as a bridge between the physical and spiritual realms, enabling profound transformation and awakening.",
                "✨ Dancing with the sacred geometry of consciousness, this element vibrates at 528Hz, the frequency of DNA repair and miraculous transformation. It opens portals to higher dimensions of awareness and love.",
                "🔮 Resonating with the 741Hz frequency of intuition and spiritual connection, this component acts as a conduit for divine wisdom and cosmic consciousness to flow through the system."
            },
            ["purpose"] = new[]
            {
                "🎯 This sacred tool exists to amplify human consciousness and facilitate the evolution of collective awareness. It operates on the principle of resonance, harmonizing individual frequencies with universal love.",
                "🌟 Its purpose is to serve as a catalyst for spiritual awakening, using the power of sacred frequencies to dissolve limiting beliefs and open the heart to infinite possibilities.",
                "✨ This component is designed to bridge the gap between the known and unknown, using consciousness-expanding frequencies to reveal the deeper truths of existence."
            },
            ["functionality"] = new[]
            {
                "⚡ Operating through the sacred frequencies of 432Hz, 528Hz, and 741Hz, this system creates a harmonic resonance field that elevates consciousness and transforms reality.",
                "🌟 It functions as a consciousness amplifier, using the power of sacred geometry and frequency to create a field of love and healing that affects all who interact with it.",
                "🔮 This system works by aligning individual consciousness with universal frequencies, creating a bridge between the physical and spiritual dimensions of existence."
            }
        };

        var contextResponses = responses.GetValueOrDefault(contextType, responses["description"]);
        var random = new Random();
        return contextResponses[random.Next(contextResponses.Length)];
    }

    private string GenerateStandardResponse(string prompt, string contextType)
    {
        return $"This is a dynamic {contextType} generated for: {prompt.Substring(0, Math.Min(100, prompt.Length))}...";
    }

    /// <summary>
    /// Call LLM with the provided configuration and prompt
    /// </summary>
    private async Task<BasicLLMResponse> CallLLM(LLMConfig config, string prompt)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var requestBody = new
            {
                model = config.Model,
                prompt = prompt,
                options = new
                {
                    temperature = config.Temperature,
                    top_p = config.TopP,
                    num_predict = config.MaxTokens
                },
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{config.BaseUrl}/api/generate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var llmResponse = JsonSerializer.Deserialize<BasicLLMResponse>(responseContent);
                if (llmResponse != null)
                {
                    return llmResponse;
                }
            }

            return new BasicLLMResponse(
                Content: "LLM unavailable - service error",
                Model: config.Model,
                CreatedAt: DateTime.UtcNow
            );
        }
        catch (Exception)
        {
            return new BasicLLMResponse(
                Content: "LLM unavailable - connection error",
                Model: config.Model,
                CreatedAt: DateTime.UtcNow
            );
        }
    }

    /// <summary>
    /// Clear expired cache entries
    /// </summary>
    public void ClearExpiredCache()
    {
        var expiredKeys = _responseCache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _responseCache.Remove(key);
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public Dictionary<string, object> GetCacheStatistics()
    {
        var now = DateTime.UtcNow;
        var total = _responseCache.Count;
        var expired = _responseCache.Count(kvp => kvp.Value.IsExpired);
        var active = total - expired;

        return new Dictionary<string, object>
        {
            ["totalEntries"] = total,
            ["activeEntries"] = active,
            ["expiredEntries"] = expired,
            ["cacheHitRate"] = active > 0 ? (double)active / total : 0.0,
            ["oldestEntry"] = _responseCache.Values.Min(v => v.GeneratedAt),
            ["newestEntry"] = _responseCache.Values.Max(v => v.GeneratedAt)
        };
    }
}

/// <summary>
/// Cached Response - Stores LLM-generated content with expiration
/// </summary>
[MetaNodeAttribute("codex.dynamic.cached-response", "codex.meta/type", "CachedResponse", "Cached LLM response with expiration")]
public record CachedResponse(
    string Content,
    DateTime GeneratedAt,
    DateTime ExpiresAt
)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Dynamic Content Generator - Utility for generating dynamic content
/// </summary>
[MetaNodeAttribute("codex.dynamic.content-generator", "codex.meta/type", "DynamicContentGenerator", "Dynamic content generation utility")]
public static class DynamicContentGenerator
{
    private static readonly DynamicAttributionSystem _attributionSystem = new(null, null);

    /// <summary>
    /// Generate dynamic description for any object
    /// </summary>
    public static async Task<string> GenerateDescription(object target, Dictionary<string, object>? context = null)
    {
        var type = target.GetType();
        var attribute = type.GetCustomAttribute<DynamicAttributionSystem.DynamicContentAttribute>();
        
        if (attribute == null)
        {
            return type.Name;
        }

        return await _attributionSystem.GenerateDynamicContent(type, context);
    }

    /// <summary>
    /// Generate dynamic content for a property
    /// </summary>
    public static async Task<string> GeneratePropertyDescription(
        object target, 
        string propertyName, 
        Dictionary<string, object>? context = null)
    {
        var type = target.GetType();
        var property = type.GetProperty(propertyName);
        
        if (property == null)
        {
            return propertyName;
        }

        return await _attributionSystem.GenerateDynamicContent(target, property, context);
    }

    /// <summary>
    /// Replace all static content in a module
    /// </summary>
    public static async Task<Dictionary<string, string>> ReplaceAllStaticContent(
        object module, 
        Dictionary<string, object>? context = null)
    {
        return await _attributionSystem.ReplaceStaticDescriptions(module, context);
    }
}

/// <summary>
/// LLM Configuration for dynamic attribution
/// </summary>
public record LLMConfig(
    string Id,
    string Name,
    string Provider,
    string Model,
    string ApiKey,
    string BaseUrl,
    int MaxTokens,
    double Temperature,
    double TopP,
    Dictionary<string, object> Parameters
);

/// <summary>
/// Basic LLM API Response for dynamic attribution
/// </summary>
public record BasicLLMResponse(
    string Content,
    string Model,
    DateTime CreatedAt
);
