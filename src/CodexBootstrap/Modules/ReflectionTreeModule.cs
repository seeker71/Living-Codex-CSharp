using System.Reflection;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Module that uses .NET reflection to build a complete reference tree of assemblies and links them to U-CORE nodes
/// </summary>
[MetaNode(Id = "codex.reflection-tree", Name = "Reflection Tree Module", Description = "Builds complete reference tree of assemblies using reflection")]
public sealed class ReflectionTreeModule : ModuleBase
{
    public override string Name => "Reflection Tree Module";
    public override string Description => "Builds complete reference tree of assemblies using reflection and links to U-CORE nodes";
    public override string Version => "1.0.0";

    // use ModuleBase._registry

    public ReflectionTreeModule(INodeRegistry registry, ICodexLogger logger) : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.reflection-tree",
            name: "Reflection Tree Module",
            version: "1.0.0",
            description: "Builds complete reference tree of assemblies using reflection and links to U-CORE nodes",
            tags: new[] { "reflection", "assemblies", "types", "methods", "ucore", "documentation" },
            capabilities: new[] { "assembly-analysis", "type-discovery", "method-mapping", "ucore-linking" },
            spec: "codex.spec.reflection-tree"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        router.Register("codex.reflection-tree", "build-complete-tree", async args =>
        {
            return await BuildCompleteReflectionTreeAsync();
        });

        router.Register("codex.reflection-tree", "analyze-assembly", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing assembly name");
            var assemblyName = args.Value.GetString();
            if (string.IsNullOrEmpty(assemblyName)) return new ErrorResponse("Invalid assembly name");
            return await AnalyzeAssemblyAsync(assemblyName);
        });

        router.Register("codex.reflection-tree", "link-to-ucore", async args =>
        {
            return await LinkAllToUCoreAsync();
        });
    }

    [ApiRoute("POST", "/reflection/build-tree", "Build Complete Tree", "Builds complete reflection tree of all loaded assemblies", "codex.reflection-tree")]
    public async Task<object> BuildCompleteReflectionTreeAsync()
    {
        try
        {
            var results = new List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var assemblyResults = await AnalyzeAssemblyInternalAsync(assembly);
                    results.AddRange(assemblyResults);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to analyze assembly {assembly.FullName}: {ex.Message}");
                    results.Add($"Failed to analyze assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return new SuccessResponse($"Built reflection tree for {assemblies.Length} assemblies. Created {results.Count} nodes.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error building complete reflection tree: {ex.Message}", ex);
            return new ErrorResponse($"Error building complete reflection tree: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/reflection/analyze/{assemblyName}", "Analyze Assembly", "Analyzes a specific assembly", "codex.reflection-tree")]
    public async Task<object> AnalyzeAssemblyAsync([ApiParameter("assemblyName", "Assembly name to analyze", Required = true, Location = "path")] string assemblyName)
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName || a.FullName == assemblyName);
            
            if (assembly == null)
            {
                return new ErrorResponse($"Assembly {assemblyName} not found");
            }

            var results = await AnalyzeAssemblyInternalAsync(assembly);
            return new SuccessResponse($"Analyzed assembly {assemblyName}. Created {results.Count} nodes.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error analyzing assembly {assemblyName}: {ex.Message}", ex);
            return new ErrorResponse($"Error analyzing assembly: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/reflection/link-ucore", "Link to U-CORE", "Links all reflection nodes to U-CORE concepts", "codex.reflection-tree")]
    public async Task<object> LinkAllToUCoreAsync()
    {
        try
        {
            var results = new List<string>();
            
            // Get all reflection nodes
            var assemblyNodes = _registry.GetNodesByType("codex.reflection/assembly");
            var typeNodes = _registry.GetNodesByType("codex.reflection/type");
            var methodNodes = _registry.GetNodesByType("codex.reflection/method");
            var propertyNodes = _registry.GetNodesByType("codex.reflection/property");
            var fieldNodes = _registry.GetNodesByType("codex.reflection/field");

            // Link assemblies to U-CORE
            foreach (var assemblyNode in assemblyNodes)
            {
                var ucoreTarget = MapToUCoreConcept(assemblyNode, "assembly");
                if (ucoreTarget != null)
                {
                    var edge = NodeHelpers.CreateEdge(
                        assemblyNode.Id,
                        ucoreTarget,
                        "maps_to_concept",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["relationship"] = "assembly-maps-to-ucore",
                            ["mappingType"] = "reflection",
                            ["createdAt"] = DateTimeOffset.UtcNow
                        }
                    );
                    _registry.Upsert(edge);
                    results.Add($"Linked assembly {assemblyNode.Id} to U-CORE concept {ucoreTarget}");
                }
            }

            // Link types to U-CORE
            foreach (var typeNode in typeNodes)
            {
                var ucoreTarget = MapToUCoreConcept(typeNode, "type");
                if (ucoreTarget != null)
                {
                    var edge = NodeHelpers.CreateEdge(
                        typeNode.Id,
                        ucoreTarget,
                        "maps_to_concept",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["relationship"] = "type-maps-to-ucore",
                            ["mappingType"] = "reflection",
                            ["createdAt"] = DateTimeOffset.UtcNow
                        }
                    );
                    _registry.Upsert(edge);
                    results.Add($"Linked type {typeNode.Id} to U-CORE concept {ucoreTarget}");
                }
            }

            // Link methods to U-CORE
            foreach (var methodNode in methodNodes)
            {
                var ucoreTarget = MapToUCoreConcept(methodNode, "method");
                if (ucoreTarget != null)
                {
                    var edge = NodeHelpers.CreateEdge(
                        methodNode.Id,
                        ucoreTarget,
                        "maps_to_concept",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["relationship"] = "method-maps-to-ucore",
                            ["mappingType"] = "reflection",
                            ["createdAt"] = DateTimeOffset.UtcNow
                        }
                    );
                    _registry.Upsert(edge);
                    results.Add($"Linked method {methodNode.Id} to U-CORE concept {ucoreTarget}");
                }
            }

            return new SuccessResponse($"Linked all reflection nodes to U-CORE. Created {results.Count} connections.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error linking to U-CORE: {ex.Message}", ex);
            return new ErrorResponse($"Error linking to U-CORE: {ex.Message}");
        }
    }

    private async Task<List<string>> AnalyzeAssemblyInternalAsync(Assembly assembly)
    {
        var results = new List<string>();
        
        try
        {
            // Create assembly node
            var assemblyNode = CreateAssemblyNode(assembly);
            _registry.Upsert(assemblyNode);
            results.Add($"Created assembly node: {assemblyNode.Id}");

            // Analyze all types in the assembly
            var types = assembly.GetTypes().Where(t => !t.IsSpecialName).ToArray();
            
            foreach (var type in types)
            {
                try
                {
                    // Create type node
                    var typeNode = CreateTypeNode(type, assemblyNode.Id);
                    _registry.Upsert(typeNode);
                    results.Add($"Created type node: {typeNode.Id}");

                    // Create edge from assembly to type
                    var assemblyTypeEdge = NodeHelpers.CreateEdge(
                        assemblyNode.Id,
                        typeNode.Id,
                        "contains",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["relationship"] = "assembly-contains-type",
                            ["typeName"] = type.Name,
                            ["namespace"] = type.Namespace
                        }
                    );
                    _registry.Upsert(assemblyTypeEdge);

                    // Analyze methods
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                        .Where(m => !m.IsSpecialName).ToArray();
                    
                    foreach (var method in methods)
                    {
                        var methodNode = CreateMethodNode(method, typeNode.Id);
                        _registry.Upsert(methodNode);
                        results.Add($"Created method node: {methodNode.Id}");

                        // Create edge from type to method
                        var typeMethodEdge = NodeHelpers.CreateEdge(
                            typeNode.Id,
                            methodNode.Id,
                            "contains",
                            1.0,
                            new Dictionary<string, object>
                            {
                                ["relationship"] = "type-contains-method",
                                ["methodName"] = method.Name,
                                ["isStatic"] = method.IsStatic,
                                ["isPublic"] = method.IsPublic
                            }
                        );
                        _registry.Upsert(typeMethodEdge);
                    }

                    // Analyze properties
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    
                    foreach (var property in properties)
                    {
                        var propertyNode = CreatePropertyNode(property, typeNode.Id);
                        _registry.Upsert(propertyNode);
                        results.Add($"Created property node: {propertyNode.Id}");

                        // Create edge from type to property
                        var typePropertyEdge = NodeHelpers.CreateEdge(
                            typeNode.Id,
                            propertyNode.Id,
                            "contains",
                            1.0,
                            new Dictionary<string, object>
                            {
                                ["relationship"] = "type-contains-property",
                                ["propertyName"] = property.Name,
                                ["propertyType"] = property.PropertyType.Name
                            }
                        );
                        _registry.Upsert(typePropertyEdge);
                    }

                    // Analyze fields
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                        .Where(f => !f.IsSpecialName).ToArray();
                    
                    foreach (var field in fields)
                    {
                        var fieldNode = CreateFieldNode(field, typeNode.Id);
                        _registry.Upsert(fieldNode);
                        results.Add($"Created field node: {fieldNode.Id}");

                        // Create edge from type to field
                        var typeFieldEdge = NodeHelpers.CreateEdge(
                            typeNode.Id,
                            fieldNode.Id,
                            "contains",
                            1.0,
                            new Dictionary<string, object>
                            {
                                ["relationship"] = "type-contains-field",
                                ["fieldName"] = field.Name,
                                ["fieldType"] = field.FieldType.Name
                            }
                        );
                        _registry.Upsert(typeFieldEdge);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to analyze type {type.Name}: {ex.Message}");
                    results.Add($"Failed to analyze type {type.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to analyze assembly {assembly.FullName}: {ex.Message}");
            results.Add($"Failed to analyze assembly {assembly.FullName}: {ex.Message}");
        }

        return results;
    }

    private Node CreateAssemblyNode(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? "unknown";
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        var assemblyId = $"codex.reflection.assembly.{assemblyName}.{assemblyVersion}.{Guid.NewGuid():N}";
        
        return new Node(
            Id: assemblyId,
            TypeId: "codex.reflection/assembly",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Assembly: {assembly.GetName().Name}",
            Description: $"Reflection analysis of assembly {assembly.GetName().Name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = assembly.GetName().Name,
                    fullName = assembly.FullName,
                    version = assembly.GetName().Version?.ToString(),
                    location = assembly.Location,
                    types = assembly.GetTypes().Length,
                    methods = assembly.GetTypes().SelectMany(t => t.GetMethods()).Count(),
                    properties = assembly.GetTypes().SelectMany(t => t.GetProperties()).Count(),
                    fields = assembly.GetTypes().SelectMany(t => t.GetFields()).Count()
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["assemblyName"] = assembly.GetName().Name,
                ["fullName"] = assembly.FullName,
                ["version"] = assembly.GetName().Version?.ToString(),
                ["location"] = assembly.Location,
                ["analyzedAt"] = DateTimeOffset.UtcNow,
                ["typeCount"] = assembly.GetTypes().Length
            }
        );
    }

    private Node CreateTypeNode(Type type, string assemblyId)
    {
        var typeFullName = type.FullName ?? type.Name;
        var typeHash = typeFullName.GetHashCode().ToString("X8");
        var typeId = $"codex.reflection.type.{typeFullName.Replace(".", "_").Replace("+", "_")}.{typeHash}.{Guid.NewGuid():N}";
        
        return new Node(
            Id: typeId,
            TypeId: "codex.reflection/type",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Type: {type.Name}",
            Description: $"Reflection analysis of type {type.FullName}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = type.Name,
                    fullName = type.FullName,
                    @namespace = type.Namespace,
                    baseType = type.BaseType?.FullName,
                    interfaces = type.GetInterfaces().Select(i => i.FullName).ToArray(),
                    isClass = type.IsClass,
                    isInterface = type.IsInterface,
                    isEnum = type.IsEnum,
                    isValueType = type.IsValueType,
                    isAbstract = type.IsAbstract,
                    isSealed = type.IsSealed,
                    isPublic = type.IsPublic,
                    methods = type.GetMethods().Length,
                    properties = type.GetProperties().Length,
                    fields = type.GetFields().Length
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["typeName"] = type.Name,
                ["fullName"] = type.FullName,
                ["namespace"] = type.Namespace,
                ["assemblyId"] = assemblyId,
                ["isClass"] = type.IsClass,
                ["isInterface"] = type.IsInterface,
                ["isEnum"] = type.IsEnum,
                ["isValueType"] = type.IsValueType,
                ["isAbstract"] = type.IsAbstract,
                ["isSealed"] = type.IsSealed,
                ["isPublic"] = type.IsPublic,
                ["analyzedAt"] = DateTimeOffset.UtcNow
            }
        );
    }

    private Node CreateMethodNode(MethodInfo method, string typeId)
    {
        var declaringType = method.DeclaringType?.FullName ?? "unknown";
        var methodName = method.Name;
        var parameterTypes = string.Join("_", method.GetParameters().Select(p => p.ParameterType.Name));
        var methodSignature = $"{declaringType}.{methodName}({parameterTypes})";
        var methodHash = methodSignature.GetHashCode().ToString("X8");
        var methodId = $"codex.reflection.method.{methodSignature.Replace(".", "_").Replace("+", "_").Replace("(", "_").Replace(")", "_")}.{methodHash}.{Guid.NewGuid():N}";
        
        return new Node(
            Id: methodId,
            TypeId: "codex.reflection/method",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Method: {method.Name}",
            Description: $"Reflection analysis of method {method.Name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = method.Name,
                    returnType = method.ReturnType.FullName,
                    parameters = method.GetParameters().Select(p => new
                    {
                        name = p.Name,
                        type = p.ParameterType.FullName,
                        isOptional = p.IsOptional,
                        defaultValue = p.DefaultValue
                    }).ToArray(),
                    isStatic = method.IsStatic,
                    isPublic = method.IsPublic,
                    isPrivate = method.IsPrivate,
                    isProtected = method.IsFamily,
                    isAbstract = method.IsAbstract,
                    isVirtual = method.IsVirtual,
                    isOverride = method.GetBaseDefinition() != method
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["methodName"] = method.Name,
                ["returnType"] = method.ReturnType.FullName,
                ["typeId"] = typeId,
                ["isStatic"] = method.IsStatic,
                ["isPublic"] = method.IsPublic,
                ["isPrivate"] = method.IsPrivate,
                ["isProtected"] = method.IsFamily,
                ["isAbstract"] = method.IsAbstract,
                ["isVirtual"] = method.IsVirtual,
                ["isOverride"] = method.GetBaseDefinition() != method,
                ["parameterCount"] = method.GetParameters().Length,
                ["analyzedAt"] = DateTimeOffset.UtcNow
            }
        );
    }

    private Node CreatePropertyNode(PropertyInfo property, string typeId)
    {
        var declaringType = property.DeclaringType?.FullName ?? "unknown";
        var propertyName = property.Name;
        var propertySignature = $"{declaringType}.{propertyName}";
        var propertyHash = propertySignature.GetHashCode().ToString("X8");
        var propertyId = $"codex.reflection.property.{propertySignature.Replace(".", "_").Replace("+", "_")}.{propertyHash}.{Guid.NewGuid():N}";
        
        return new Node(
            Id: propertyId,
            TypeId: "codex.reflection/property",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Property: {property.Name}",
            Description: $"Reflection analysis of property {property.Name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = property.Name,
                    propertyType = property.PropertyType.FullName,
                    canRead = property.CanRead,
                    canWrite = property.CanWrite,
                    getMethod = property.GetMethod?.Name,
                    setMethod = property.SetMethod?.Name,
                    isStatic = property.GetMethod?.IsStatic ?? false,
                    isPublic = property.GetMethod?.IsPublic ?? false
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["propertyName"] = property.Name,
                ["propertyType"] = property.PropertyType.FullName,
                ["typeId"] = typeId,
                ["canRead"] = property.CanRead,
                ["canWrite"] = property.CanWrite,
                ["isStatic"] = property.GetMethod?.IsStatic ?? false,
                ["isPublic"] = property.GetMethod?.IsPublic ?? false,
                ["analyzedAt"] = DateTimeOffset.UtcNow
            }
        );
    }

    private Node CreateFieldNode(FieldInfo field, string typeId)
    {
        var declaringType = field.DeclaringType?.FullName ?? "unknown";
        var fieldName = field.Name;
        var fieldSignature = $"{declaringType}.{fieldName}";
        var fieldHash = fieldSignature.GetHashCode().ToString("X8");
        var fieldId = $"codex.reflection.field.{fieldSignature.Replace(".", "_").Replace("+", "_")}.{fieldHash}.{Guid.NewGuid():N}";
        
        return new Node(
            Id: fieldId,
            TypeId: "codex.reflection/field",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Field: {field.Name}",
            Description: $"Reflection analysis of field {field.Name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = field.Name,
                    fieldType = field.FieldType.FullName,
                    isStatic = field.IsStatic,
                    isPublic = field.IsPublic,
                    isPrivate = field.IsPrivate,
                    isProtected = field.IsFamily,
                    isReadOnly = field.IsInitOnly,
                    isLiteral = field.IsLiteral,
                    isConstant = field.IsLiteral && field.IsStatic
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["fieldName"] = field.Name,
                ["fieldType"] = field.FieldType.FullName,
                ["typeId"] = typeId,
                ["isStatic"] = field.IsStatic,
                ["isPublic"] = field.IsPublic,
                ["isPrivate"] = field.IsPrivate,
                ["isProtected"] = field.IsFamily,
                ["isReadOnly"] = field.IsInitOnly,
                ["isLiteral"] = field.IsLiteral,
                ["isConstant"] = field.IsLiteral && field.IsStatic,
                ["analyzedAt"] = DateTimeOffset.UtcNow
            }
        );
    }

    private string? MapToUCoreConcept(Node node, string nodeType)
    {
        var text = $"{node.Title} {node.Description}".ToLowerInvariant();
        var meta = node.Meta;
        
        // Map based on node type and content
        return nodeType switch
        {
            "assembly" => MapAssemblyToUCore(text, meta),
            "type" => MapTypeToUCore(text, meta),
            "method" => MapMethodToUCore(text, meta),
            "property" => MapPropertyToUCore(text, meta),
            "field" => MapFieldToUCore(text, meta),
            _ => "u-core-concept-knowledge" // Default fallback
        };
    }

    private string? MapAssemblyToUCore(string text, Dictionary<string, object>? meta)
    {
        var assemblyName = meta?.GetValueOrDefault("assemblyName")?.ToString()?.ToLowerInvariant() ?? "";
        
        if (assemblyName.Contains("codex") || assemblyName.Contains("bootstrap"))
            return "u-core-concept-system";
        if (assemblyName.Contains("test") || assemblyName.Contains("spec"))
            return "u-core-concept-testing";
        if (assemblyName.Contains("web") || assemblyName.Contains("http"))
            return "u-core-concept-communication";
        if (assemblyName.Contains("data") || assemblyName.Contains("storage"))
            return "u-core-concept-data";
        
        return "u-core-concept-knowledge";
    }

    private string? MapTypeToUCore(string text, Dictionary<string, object>? meta)
    {
        var typeName = meta?.GetValueOrDefault("typeName")?.ToString()?.ToLowerInvariant() ?? "";
        var isClass = meta?.GetValueOrDefault("isClass") as bool? ?? false;
        var isInterface = meta?.GetValueOrDefault("isInterface") as bool? ?? false;
        var isEnum = meta?.GetValueOrDefault("isEnum") as bool? ?? false;
        
        if (isInterface)
            return "u-core-concept-interface";
        if (isEnum)
            return "u-core-concept-enumeration";
        if (typeName.Contains("module") || typeName.Contains("service"))
            return "u-core-concept-module";
        if (typeName.Contains("controller") || typeName.Contains("handler"))
            return "u-core-concept-controller";
        if (typeName.Contains("data") || typeName.Contains("model"))
            return "u-core-concept-data";
        if (typeName.Contains("config") || typeName.Contains("setting"))
            return "u-core-concept-configuration";
        
        return "u-core-concept-type";
    }

    private string? MapMethodToUCore(string text, Dictionary<string, object>? meta)
    {
        var methodName = meta?.GetValueOrDefault("methodName")?.ToString()?.ToLowerInvariant() ?? "";
        var isPublic = meta?.GetValueOrDefault("isPublic") as bool? ?? false;
        var isStatic = meta?.GetValueOrDefault("isStatic") as bool? ?? false;
        
        if (methodName.StartsWith("get") || methodName.StartsWith("is") || methodName.StartsWith("has"))
            return "u-core-concept-accessor";
        if (methodName.StartsWith("set") || methodName.StartsWith("update") || methodName.StartsWith("modify"))
            return "u-core-concept-mutator";
        if (methodName.StartsWith("create") || methodName.StartsWith("new") || methodName.StartsWith("build"))
            return "u-core-concept-constructor";
        if (methodName.StartsWith("delete") || methodName.StartsWith("remove") || methodName.StartsWith("destroy"))
            return "u-core-concept-destructor";
        if (methodName.StartsWith("process") || methodName.StartsWith("handle") || methodName.StartsWith("execute"))
            return "u-core-concept-processor";
        if (isStatic)
            return "u-core-concept-utility";
        
        return "u-core-concept-method";
    }

    private string? MapPropertyToUCore(string text, Dictionary<string, object>? meta)
    {
        var propertyName = meta?.GetValueOrDefault("propertyName")?.ToString()?.ToLowerInvariant() ?? "";
        var canRead = meta?.GetValueOrDefault("canRead") as bool? ?? false;
        var canWrite = meta?.GetValueOrDefault("canWrite") as bool? ?? false;
        
        if (canRead && canWrite)
            return "u-core-concept-property";
        if (canRead && !canWrite)
            return "u-core-concept-readonly-property";
        if (!canRead && canWrite)
            return "u-core-concept-writeonly-property";
        
        return "u-core-concept-property";
    }

    private string? MapFieldToUCore(string text, Dictionary<string, object>? meta)
    {
        var fieldName = meta?.GetValueOrDefault("fieldName")?.ToString()?.ToLowerInvariant() ?? "";
        var isConstant = meta?.GetValueOrDefault("isConstant") as bool? ?? false;
        var isReadOnly = meta?.GetValueOrDefault("isReadOnly") as bool? ?? false;
        
        if (isConstant)
            return "u-core-concept-constant";
        if (isReadOnly)
            return "u-core-concept-readonly-field";
        
        return "u-core-concept-field";
    }
}
