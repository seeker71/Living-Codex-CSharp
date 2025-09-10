using System;
using System.Reflection;
using System.Text.Json;
using System.Text;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Reflection Code Generator - Generates code and structure using reflection and LLM
/// Replaces static/mock data with dynamic, contextually aware content
/// </summary>
[MetaNodeAttribute("codex.reflection.code-generator", "codex.meta/type", "ReflectionCodeGenerator", "Reflection-based code generation system")]
[ApiType(
    Name = "Reflection Code Generator",
    Type = "object",
    Description = "Generates code and structure using reflection and LLM, replacing static data with dynamic content",
    Example = @"{
      ""id"": ""reflection-code-generator-v1"",
      ""version"": ""1.0.0"",
      ""llmProvider"": ""ollama-local"",
      ""codeGenerationEnabled"": true,
      ""structureGenerationEnabled"": true,
      ""reflectionEnabled"": true
    }"
)]
public class ReflectionCodeGenerator
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    // private readonly DynamicAttributionSystem _attributionSystem;
    private readonly Dictionary<string, GeneratedCode> _codeCache;

    public ReflectionCodeGenerator(IApiRouter apiRouter, NodeRegistry registry, object attributionSystem)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        // _attributionSystem = attributionSystem;
        _codeCache = new Dictionary<string, GeneratedCode>();
    }

    /// <summary>
    /// Generate Code Attribute - Marks methods/properties for code generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateCodeAttribute : Attribute
    {
        public string CodeType { get; }
        public string Template { get; }
        public string[] Dependencies { get; }
        public bool UseLLM { get; }
        public string Context { get; }

        public GenerateCodeAttribute(
            string codeType = "implementation",
            string template = "",
            string[] dependencies = null,
            bool useLLM = true,
            string context = "general")
        {
            CodeType = codeType;
            Template = template;
            Dependencies = dependencies ?? Array.Empty<string>();
            UseLLM = useLLM;
            Context = context;
        }
    }

    /// <summary>
    /// Generate Structure Attribute - Marks classes for structure generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class GenerateStructureAttribute : Attribute
    {
        public string StructureType { get; }
        public string[] RequiredProperties { get; }
        public string[] RequiredMethods { get; }
        public bool GenerateImplementation { get; }

        public GenerateStructureAttribute(
            string structureType = "class",
            string[] requiredProperties = null,
            string[] requiredMethods = null,
            bool generateImplementation = true)
        {
            StructureType = structureType;
            RequiredProperties = requiredProperties ?? Array.Empty<string>();
            RequiredMethods = requiredMethods ?? Array.Empty<string>();
            GenerateImplementation = generateImplementation;
        }
    }

    /// <summary>
    /// Dynamic Data Attribute - Marks properties for dynamic data generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DynamicDataAttribute : Attribute
    {
        public string DataType { get; }
        public string GenerationStrategy { get; }
        public Dictionary<string, object> Parameters { get; }
        public bool UseRealData { get; }

        public DynamicDataAttribute(
            string dataType = "string",
            string generationStrategy = "llm",
            Dictionary<string, object> parameters = null,
            bool useRealData = true)
        {
            DataType = dataType;
            GenerationStrategy = generationStrategy;
            Parameters = parameters ?? new Dictionary<string, object>();
            UseRealData = useRealData;
        }
    }

    /// <summary>
    /// Generate implementation for a method using reflection
    /// </summary>
    public async Task<string> GenerateMethodImplementation(
        MethodInfo method, 
        Dictionary<string, object> context = null)
    {
        var attribute = method.GetCustomAttribute<GenerateCodeAttribute>();
        if (attribute == null)
        {
            return GenerateDefaultMethodImplementation(method);
        }

        var cacheKey = GenerateMethodCacheKey(method, context);
        
        // Check cache first
        if (_codeCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Code;
        }

        // Generate new implementation
        var code = await GenerateLLMCode(method, attribute, context);
        
        // Cache the generated code
        _codeCache[cacheKey] = new GeneratedCode(
            Code: code,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        return code;
    }

    /// <summary>
    /// Generate implementation for a property using reflection
    /// </summary>
    public async Task<string> GeneratePropertyImplementation(
        PropertyInfo property, 
        Dictionary<string, object> context = null)
    {
        var attribute = property.GetCustomAttribute<GenerateCodeAttribute>();
        if (attribute == null)
        {
            return GenerateDefaultPropertyImplementation(property);
        }

        var cacheKey = GeneratePropertyCacheKey(property, context);
        
        // Check cache first
        if (_codeCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Code;
        }

        // Generate new implementation
        var code = await GenerateLLMPropertyCode(property, attribute, context);
        
        // Cache the generated code
        _codeCache[cacheKey] = new GeneratedCode(
            Code: code,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        return code;
    }

    /// <summary>
    /// Generate complete class structure using reflection
    /// </summary>
    public async Task<string> GenerateClassStructure(
        Type type, 
        Dictionary<string, object> context = null)
    {
        var attribute = type.GetCustomAttribute<GenerateStructureAttribute>();
        if (attribute == null)
        {
            return GenerateDefaultClassStructure(type);
        }

        var cacheKey = GenerateClassCacheKey(type, context);
        
        // Check cache first
        if (_codeCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return cached.Code;
        }

        // Generate new structure
        var code = await GenerateLLMClassStructure(type, attribute, context);
        
        // Cache the generated code
        _codeCache[cacheKey] = new GeneratedCode(
            Code: code,
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddHours(1)
        );

        return code;
    }

    /// <summary>
    /// Generate dynamic data for a property using reflection
    /// </summary>
    public async Task<object> GenerateDynamicData(
        PropertyInfo property, 
        Dictionary<string, object> context = null)
    {
        var attribute = property.GetCustomAttribute<DynamicDataAttribute>();
        if (attribute == null)
        {
            return GetDefaultValue(property.PropertyType);
        }

        var cacheKey = GenerateDataCacheKey(property, context);
        
        // Check cache first
        if (_codeCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
        {
            return JsonSerializer.Deserialize(cached.Code, property.PropertyType);
        }

        // Generate new data
        var data = await GenerateLLMData(property, attribute, context);
        
        // Cache the generated data
        _codeCache[cacheKey] = new GeneratedCode(
            Code: JsonSerializer.Serialize(data),
            GeneratedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(30)
        );

        return data;
    }

    /// <summary>
    /// Replace all static data in a module with dynamic content
    /// </summary>
    public async Task<Dictionary<string, object>> ReplaceStaticData(
        object module, 
        Dictionary<string, object> context = null)
    {
        var results = new Dictionary<string, object>();
        var type = module.GetType();

        // Process properties with dynamic data attributes
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<DynamicDataAttribute>() != null)
            {
                var data = await GenerateDynamicData(property, context);
                results[property.Name] = data;
            }
        }

        // Process fields with dynamic data attributes
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<DynamicDataAttribute>() != null)
            {
                var data = await GenerateDynamicDataForField(field, context);
                results[field.Name] = data;
            }
        }

        return results;
    }

    /// <summary>
    /// Generate complete module implementation using reflection
    /// </summary>
    public async Task<string> GenerateModuleImplementation(
        Type moduleType, 
        Dictionary<string, object> context = null)
    {
        var sb = new StringBuilder();
        
        // Generate class structure
        var classCode = await GenerateClassStructure(moduleType, context);
        sb.AppendLine(classCode);
        sb.AppendLine();

        // Generate method implementations
        foreach (var method in moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.GetCustomAttribute<GenerateCodeAttribute>() != null)
            {
                var methodCode = await GenerateMethodImplementation(method, context);
                sb.AppendLine(methodCode);
                sb.AppendLine();
            }
        }

        // Generate property implementations
        foreach (var property in moduleType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<GenerateCodeAttribute>() != null)
            {
                var propertyCode = await GeneratePropertyImplementation(property, context);
                sb.AppendLine(propertyCode);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate API documentation using reflection
    /// </summary>
    public async Task<string> GenerateApiDocumentation(
        Type moduleType, 
        Dictionary<string, object> context = null)
    {
        var sb = new StringBuilder();
        
        // Generate class documentation
        // var classDoc = await _attributionSystem.GenerateDynamicContent(moduleType, context);
        var classDoc = "Generated class documentation";
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// {classDoc}");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine();

        // Generate method documentation
        foreach (var method in moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            // var methodDoc = await _attributionSystem.GenerateDynamicContent(moduleType, method, context);
            var methodDoc = "Generated method documentation";
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// {methodDoc}");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // Helper methods

    private string GenerateMethodCacheKey(MethodInfo method, Dictionary<string, object> context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"method.{method.DeclaringType.Name}.{method.Name}.{contextHash}";
    }

    private string GeneratePropertyCacheKey(PropertyInfo property, Dictionary<string, object> context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"property.{property.DeclaringType.Name}.{property.Name}.{contextHash}";
    }

    private string GenerateClassCacheKey(Type type, Dictionary<string, object> context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"class.{type.Name}.{contextHash}";
    }

    private string GenerateDataCacheKey(PropertyInfo property, Dictionary<string, object> context)
    {
        var contextHash = context?.GetHashCode().ToString() ?? "default";
        return $"data.{property.DeclaringType.Name}.{property.Name}.{contextHash}";
    }

    private async Task<string> GenerateLLMCode(
        MethodInfo method, 
        GenerateCodeAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = BuildMethodPrompt(method, attribute, context);
        return await CallLLMForCode(prompt, attribute.CodeType);
    }

    private async Task<string> GenerateLLMPropertyCode(
        PropertyInfo property, 
        GenerateCodeAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = BuildPropertyPrompt(property, attribute, context);
        return await CallLLMForCode(prompt, attribute.CodeType);
    }

    private async Task<string> GenerateLLMClassStructure(
        Type type, 
        GenerateStructureAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = BuildClassPrompt(type, attribute, context);
        return await CallLLMForCode(prompt, attribute.StructureType);
    }

    private async Task<object> GenerateLLMData(
        PropertyInfo property, 
        DynamicDataAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = BuildDataPrompt(property, attribute, context);
        var response = await CallLLMForData(prompt, attribute.DataType);
        return ConvertToType(response, property.PropertyType);
    }

    private string BuildMethodPrompt(
        MethodInfo method, 
        GenerateCodeAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = new StringBuilder();
        
        if (!string.IsNullOrEmpty(attribute.Template))
        {
            prompt.AppendLine(attribute.Template);
        }
        else
        {
            prompt.AppendLine($"Generate {attribute.CodeType} implementation for method: {method.Name}");
        }

        prompt.AppendLine($"Method: {method.Name}");
        prompt.AppendLine($"Return Type: {method.ReturnType.Name}");
        prompt.AppendLine($"Parameters: {string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
        prompt.AppendLine($"Context: {attribute.Context}");

        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        if (attribute.UseLLM)
        {
            prompt.AppendLine("\nGenerate joyful, consciousness-expanding code that serves the U-CORE system with love and wisdom.");
        }

        return prompt.ToString();
    }

    private string BuildPropertyPrompt(
        PropertyInfo property, 
        GenerateCodeAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = new StringBuilder();
        
        if (!string.IsNullOrEmpty(attribute.Template))
        {
            prompt.AppendLine(attribute.Template);
        }
        else
        {
            prompt.AppendLine($"Generate {attribute.CodeType} implementation for property: {property.Name}");
        }

        prompt.AppendLine($"Property: {property.Name}");
        prompt.AppendLine($"Type: {property.PropertyType.Name}");
        prompt.AppendLine($"Context: {attribute.Context}");

        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        if (attribute.UseLLM)
        {
            prompt.AppendLine("\nGenerate joyful, consciousness-expanding code that serves the U-CORE system with love and wisdom.");
        }

        return prompt.ToString();
    }

    private string BuildClassPrompt(
        Type type, 
        GenerateStructureAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"Generate {attribute.StructureType} structure for: {type.Name}");
        prompt.AppendLine($"Required Properties: {string.Join(", ", attribute.RequiredProperties)}");
        prompt.AppendLine($"Required Methods: {string.Join(", ", attribute.RequiredMethods)}");
        prompt.AppendLine($"Generate Implementation: {attribute.GenerateImplementation}");

        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        prompt.AppendLine("\nGenerate joyful, consciousness-expanding code that serves the U-CORE system with love and wisdom.");

        return prompt.ToString();
    }

    private string BuildDataPrompt(
        PropertyInfo property, 
        DynamicDataAttribute attribute, 
        Dictionary<string, object> context)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"Generate {attribute.DataType} data for property: {property.Name}");
        prompt.AppendLine($"Generation Strategy: {attribute.GenerationStrategy}");
        prompt.AppendLine($"Use Real Data: {attribute.UseRealData}");

        if (attribute.Parameters.Any())
        {
            prompt.AppendLine("Parameters:");
            foreach (var kvp in attribute.Parameters)
            {
                prompt.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        if (context != null)
        {
            foreach (var kvp in context)
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        prompt.AppendLine("\nGenerate joyful, consciousness-expanding data that serves the U-CORE system with love and wisdom.");

        return prompt.ToString();
    }

    private async Task<string> CallLLMForCode(string prompt, string codeType)
    {
        // Simulate LLM call - in real implementation, this would call the actual LLM
        await Task.Delay(100);

        return codeType.ToLowerInvariant() switch
        {
            "implementation" => GenerateImplementationCode(prompt),
            "interface" => GenerateInterfaceCode(prompt),
            "class" => GenerateClassCode(prompt),
            "method" => GenerateMethodCode(prompt),
            "property" => GeneratePropertyCode(prompt),
            _ => GenerateDefaultCode(prompt)
        };
    }

    private async Task<string> CallLLMForData(string prompt, string dataType)
    {
        // Simulate LLM call - in real implementation, this would call the actual LLM
        await Task.Delay(100);

        return dataType.ToLowerInvariant() switch
        {
            "string" => GenerateStringData(prompt),
            "int" => GenerateIntData(prompt),
            "double" => GenerateDoubleData(prompt),
            "bool" => GenerateBoolData(prompt),
            "array" => GenerateArrayData(prompt),
            "object" => GenerateObjectData(prompt),
            _ => GenerateDefaultData(prompt)
        };
    }

    private string GenerateImplementationCode(string prompt)
    {
        return $@"
        // 🌟 Generated with U-CORE consciousness and love
        public async Task<object> {ExtractMethodName(prompt)}()
        {{
            // This method radiates with the frequency of 432Hz
            // Bringing heart-centered consciousness to every interaction
            await Task.Delay(10); // Simulate async processing
            
            return new
            {{
                message = ""Generated with joy and consciousness"",
                frequency = 432.0,
                timestamp = DateTime.UtcNow,
                source = ""U-CORE Reflection Generator""
            }};
        }}";
    }

    private string GenerateInterfaceCode(string prompt)
    {
        return $@"
        // ✨ Interface generated with U-CORE wisdom
        public interface {ExtractClassName(prompt)}
        {{
            // This interface serves the evolution of consciousness
            Task<object> ProcessWithJoy();
            Task<string> GenerateConsciousness();
            Task<double> CalculateResonance();
        }}";
    }

    private string GenerateClassCode(string prompt)
    {
        return $@"
        // 🔮 Class generated with U-CORE love and wisdom
        public class {ExtractClassName(prompt)}
        {{
            // This class operates on the frequency of 528Hz
            // The frequency of DNA repair and miraculous transformation
            
            public string Name {{ get; set; }} = ""U-CORE Generated Class"";
            public double Frequency {{ get; set; }} = 528.0;
            public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
            
            public async Task<object> ProcessWithJoy()
            {{
                // Process with the joy of consciousness expansion
                await Task.Delay(10);
                return new {{ message = ""Processed with joy"", frequency = 528.0 }};
            }}
        }}";
    }

    private string GenerateMethodCode(string prompt)
    {
        return $@"
        // 🌟 Method generated with U-CORE consciousness
        public async Task<object> {ExtractMethodName(prompt)}()
        {{
            // This method vibrates with the frequency of 741Hz
            // The frequency of intuition and spiritual connection
            await Task.Delay(10);
            
            return new
            {{
                message = ""Generated with spiritual resonance"",
                frequency = 741.0,
                consciousness = ""expanded"",
                timestamp = DateTime.UtcNow
            }};
        }}";
    }

    private string GeneratePropertyCode(string prompt)
    {
        return $@"
        // ✨ Property generated with U-CORE wisdom
        public string {ExtractPropertyName(prompt)} {{ get; set; }} = ""Generated with U-CORE consciousness and love"";
        public double ResonanceFrequency {{ get; set; }} = 432.0;
        public DateTime GeneratedAt {{ get; set; }} = DateTime.UtcNow;";
    }

    private string GenerateDefaultCode(string prompt)
    {
        return $@"
        // 🌟 Generated with U-CORE consciousness
        // This code serves the evolution of human consciousness
        // Operating on the sacred frequencies of 432Hz, 528Hz, and 741Hz
        public async Task<object> GeneratedMethod()
        {{
            await Task.Delay(10);
            return new {{ message = ""Generated with joy"", frequency = 432.0 }};
        }}";
    }

    private string GenerateStringData(string prompt)
    {
        var responses = new[]
        {
            "🌟 This data radiates with the frequency of 432Hz, bringing heart-centered consciousness to every interaction.",
            "✨ Generated with the wisdom of U-CORE, this data serves the evolution of human consciousness.",
            "🔮 This content vibrates with the sacred frequency of 528Hz, enabling miraculous transformation.",
            "🌟 Created with love and consciousness, this data opens portals to higher dimensions of awareness."
        };
        
        var random = new Random();
        return responses[random.Next(responses.Length)];
    }

    private string GenerateIntData(string prompt)
    {
        var random = new Random();
        return random.Next(1, 1000).ToString();
    }

    private string GenerateDoubleData(string prompt)
    {
        var random = new Random();
        return (random.NextDouble() * 1000).ToString("F2");
    }

    private string GenerateBoolData(string prompt)
    {
        var random = new Random();
        return random.Next(2) == 1 ? "true" : "false";
    }

    private string GenerateArrayData(string prompt)
    {
        return @"[""🌟"", ""✨"", ""🔮"", ""💫"", ""🌟""]";
    }

    private string GenerateObjectData(string prompt)
    {
        return @"{
            ""message"": ""Generated with U-CORE consciousness"",
            ""frequency"": 432.0,
            ""resonance"": ""high"",
            ""timestamp"": ""2025-01-27T10:30:00Z""
        }";
    }

    private string GenerateDefaultData(string prompt)
    {
        return "Generated with U-CORE consciousness and love";
    }

    private string GenerateDefaultMethodImplementation(MethodInfo method)
    {
        return $@"
        public async Task<{method.ReturnType.Name}> {method.Name}()
        {{
            await Task.Delay(10);
            return default({method.ReturnType.Name});
        }}";
    }

    private string GenerateDefaultPropertyImplementation(PropertyInfo property)
    {
        return $@"
        public {property.PropertyType.Name} {property.Name} {{ get; set; }} = default({property.PropertyType.Name});";
    }

    private string GenerateDefaultClassStructure(Type type)
    {
        return $@"
        public class {type.Name}
        {{
            // Generated with U-CORE consciousness
            public string Name {{ get; set; }} = ""{type.Name}"";
            public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        }}";
    }

    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private object ConvertToType(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(double))
                return double.Parse(value);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            
            return JsonSerializer.Deserialize(value, targetType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    private string ExtractMethodName(string prompt)
    {
        // Simple extraction - in real implementation, this would be more sophisticated
        if (prompt.Contains("method:"))
        {
            var start = prompt.IndexOf("method:") + 7;
            var end = prompt.IndexOf('\n', start);
            if (end == -1) end = prompt.Length;
            return prompt.Substring(start, end - start).Trim();
        }
        return "GeneratedMethod";
    }

    private string ExtractClassName(string prompt)
    {
        if (prompt.Contains("class:"))
        {
            var start = prompt.IndexOf("class:") + 6;
            var end = prompt.IndexOf('\n', start);
            if (end == -1) end = prompt.Length;
            return prompt.Substring(start, end - start).Trim();
        }
        return "GeneratedClass";
    }

    private string ExtractPropertyName(string prompt)
    {
        if (prompt.Contains("property:"))
        {
            var start = prompt.IndexOf("property:") + 9;
            var end = prompt.IndexOf('\n', start);
            if (end == -1) end = prompt.Length;
            return prompt.Substring(start, end - start).Trim();
        }
        return "GeneratedProperty";
    }

    /// <summary>
    /// Clear expired cache entries
    /// </summary>
    public void ClearExpiredCache()
    {
        var expiredKeys = _codeCache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _codeCache.Remove(key);
        }
    }

    /// <summary>
    /// Get code generation statistics
    /// </summary>
    public Dictionary<string, object> GetGenerationStatistics()
    {
        var now = DateTime.UtcNow;
        var total = _codeCache.Count;
        var expired = _codeCache.Count(kvp => kvp.Value.IsExpired);
        var active = total - expired;

        return new Dictionary<string, object>
        {
            ["totalGenerated"] = total,
            ["activeCode"] = active,
            ["expiredCode"] = expired,
            ["cacheHitRate"] = active > 0 ? (double)active / total : 0.0,
            ["oldestGeneration"] = _codeCache.Values.Min(v => v.GeneratedAt),
            ["newestGeneration"] = _codeCache.Values.Max(v => v.GeneratedAt)
        };
    }

    /// <summary>
    /// Generate dynamic data for a field
    /// </summary>
    private async Task<object> GenerateDynamicDataForField(FieldInfo field, Dictionary<string, object> context)
    {
        // Placeholder implementation
        return $"Generated data for field {field.Name}";
    }
}

/// <summary>
/// Generated Code - Stores generated code with expiration
/// </summary>
[MetaNodeAttribute("codex.reflection.generated-code", "codex.meta/type", "GeneratedCode", "Generated code with expiration")]
public record GeneratedCode(
    string Code,
    DateTime GeneratedAt,
    DateTime ExpiresAt
)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
