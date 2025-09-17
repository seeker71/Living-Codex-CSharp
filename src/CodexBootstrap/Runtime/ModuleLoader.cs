using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class ModuleLoader
{
    private readonly INodeRegistry _registry;
    private readonly IApiRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IModule> _loadedModules = new();
    private readonly Core.ICodexLogger _logger;

    public ModuleLoader(INodeRegistry registry, IApiRouter router, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _router = router;
        _serviceProvider = serviceProvider;
        _logger = new Log4NetLogger(typeof(ModuleLoader));
    }

    public IReadOnlyList<IModule> GetLoadedModules() => _loadedModules.AsReadOnly();

    public void GenerateMetaNodes()
    {
        _logger.Info("Starting meta-node generation for all loaded modules...");
        
        try
        {
            // Generate meta-nodes for all loaded modules
            foreach (var module in _loadedModules)
            {
                GenerateClassMetaNode(module.GetType());
            }
            
            // Generate meta-nodes for spec files
            GenerateSpecFileMetaNodes();
            
            _logger.Info($"Meta-node generation completed for {_loadedModules.Count} modules");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during meta-node generation: {ex.Message}", ex);
        }
    }

    public void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi)
    {
        foreach (var module in _loadedModules)
        {
            try
            {
                module.RegisterHttpEndpoints(app, registry, coreApi, this);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register HTTP endpoints for module {module.GetModuleNode().Id}: {ex.Message}", ex);
            }
        }
    }

    public void LoadBuiltInModules()
    {
        // Load all built-in modules from the Modules namespace
        var moduleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
            .Where(t => t.Namespace?.StartsWith("CodexBootstrap.Modules") == true);

        _logger.Info($"Found {moduleTypes.Count()} module types to load");

        // Debug: List all found module types
        foreach (var moduleType in moduleTypes)
        {
            _logger.Info($"  - {moduleType.Name} (Namespace: {moduleType.Namespace})");
        }

        foreach (var moduleType in moduleTypes)
        {
            try
            {
                _logger.Info($"Attempting to load module: {moduleType.Name}");
                var module = CreateModule(moduleType);
                if (module != null)
                {
                    _logger.Info($"Successfully created module: {moduleType.Name}");
                    LoadModule(module);
                }
                else
                {
                    _logger.Warn($"Failed to create module: {moduleType.Name} - returned null");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load module {moduleType.Name}: {ex.Message}", ex);
            }
        }
    }

    public void LoadExternalModules(string moduleDirectory)
    {
        if (!Directory.Exists(moduleDirectory))
            return;

        foreach (var dll in Directory.GetFiles(moduleDirectory, "*.dll"))
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                LoadModulesFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load assembly {dll}: {ex.Message}", ex);
            }
        }
    }

    private void LoadModulesFromAssembly(Assembly assembly)
    {
        var moduleTypes = new List<Type>();
        
        // Phase 1: Discover all module types
        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IModule).IsAssignableFrom(type) && !type.IsAbstract)
            {
                moduleTypes.Add(type);
            }
        }
        
        // Phase 2: Create all modules with required dependencies
        var modules = new List<IModule>();
        foreach (var moduleType in moduleTypes)
        {
            try
            {
                var module = CreateModule(moduleType);
                if (module != null)
                {
                    modules.Add(module);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create module {moduleType.Name}: {ex.Message}", ex);
            }
        }
        
        // Phase 3: Setup inter-module communication
        foreach (var module in modules)
        {
            try
            {
                module.SetupInterModuleCommunication(_serviceProvider);
                _logger.Info($"Setup inter-module communication for {module.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to setup inter-module communication for {module.GetType().Name}: {ex.Message}", ex);
            }
        }
        
        // Phase 4: Register all modules
        foreach (var module in modules)
        {
            LoadModule(module);
        }
    }
    
    /// <summary>
    /// Create a module instance with required dependencies
    /// All modules must have constructor(NodeRegistry, ICodexLogger, HttpClient)
    /// </summary>
    private IModule? CreateModule(Type moduleType)
    {
        try
        {
            // Single constructor pattern: (INodeRegistry, ICodexLogger, HttpClient)
            var constructor = moduleType.GetConstructor(new[] { typeof(INodeRegistry), typeof(ICodexLogger), typeof(HttpClient) });
            if (constructor != null)
            {
                var logger = _serviceProvider.GetRequiredService<ICodexLogger>();
                var httpClient = _serviceProvider.GetService<HttpClient>() ?? new HttpClient();
                _logger.Info($"Creating module {moduleType.Name} with NodeRegistry instance: {_registry.GetHashCode()}");
                return (IModule)constructor.Invoke(new object[] { _registry, logger, httpClient });
            }
            
            _logger.Error($"No suitable constructor found for module {moduleType.Name}. Expected: (INodeRegistry, ICodexLogger, HttpClient)");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create module {moduleType.Name}: {ex.Message}", ex);
            return null;
        }
    }

    public void LoadModule(IModule module)
    {
        try
        {
            // Check if module is already loaded to prevent duplicates
            if (_loadedModules.Any(m => m == module))
            {
                _logger.Warn($"Module {module.GetType().Name} is already loaded, skipping duplicate load");
                return;
            }

            _logger.Info($"Loading module: {module.GetType().Name}");
            var moduleNode = module.GetModuleNode();
            _logger.Info($"Module node ID: {moduleNode.Id}");
            
            // Use enhanced module registration with spec tracking
            try
            {
                // Register the module and its meta nodes with spec tracking
                module.Register(_registry);
                _logger.Info($"Successfully registered module {module.GetType().Name} with enhanced registration");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register module {module.GetType().Name} with enhanced registration: {ex.Message}", ex);
                // Fallback to basic registration
                _registry.Upsert(moduleNode);
                try
                {
                    module.Register(_registry);
                    _logger.Info($"Successfully registered module {module.GetType().Name} with basic registration");
                }
                catch (Exception ex2)
                {
                    _logger.Error($"Failed to register module {module.GetType().Name} with basic registration: {ex2.Message}", ex2);
                }
            }
            
            // Register API handlers
            try
            {
                module.RegisterApiHandlers(_router, _registry);
                _logger.Info($"Successfully registered API handlers for module {module.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register API handlers for module {module.GetType().Name}: {ex.Message}", ex);
                // Continue loading even if API handler registration fails
            }
            
            // Register record types as meta nodes if the module supports it
            RegisterModuleRecordTypes(module, moduleNode);
            
            // Track loaded modules
            _loadedModules.Add(module);
            
            var name = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title;
            var version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0";
            _logger.Info($"Successfully loaded module: {name} v{version}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading module {module.GetType().Name}: {ex.Message}", ex);
            throw;
        }
    }

    private void RegisterModuleRecordTypes(IModule module, Node moduleNode)
    {
        try
        {
            // Extract record types from the module's assembly
            var moduleType = module.GetType();
            var assembly = moduleType.Assembly;
            
            // Find all record types in the same namespace as the module
            var recordTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == moduleType.Namespace)
                .Where(t => t.Name.EndsWith("Request") || t.Name.EndsWith("Response") || t.Name.EndsWith("Record") || t.Name.EndsWith("Data"))
                .Select(t => t.Name)
                .ToArray();

            if (recordTypes.Length > 0)
            {
                _logger.Info($"Registering {recordTypes.Length} record types for module {moduleNode.Id}");
                
                foreach (var recordType in recordTypes)
                {
                    var metaNode = new Node(
                        Id: $"{moduleNode.Id}.meta.{recordType.ToLower()}",
                        TypeId: "codex.meta/type",
                        State: ContentState.Water,
                        Locale: "en",
                        Title: $"{recordType} Record Type",
                        Description: $"Meta-node definition for {recordType} record type used by {moduleNode.Title}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new
                            {
                                recordType = recordType,
                                moduleId = moduleNode.Id,
                                moduleName = moduleNode.Title,
                                definedAt = DateTime.UtcNow
                            }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["recordType"] = recordType,
                            ["moduleId"] = moduleNode.Id,
                            ["moduleName"] = moduleNode.Title,
                            ["parentModule"] = moduleNode.Id,
                            ["definedAt"] = DateTime.UtcNow
                        }
                    );
                    _registry.Upsert(metaNode);
                    
                    // Create edge from module to meta node
                    var edge = new Edge(
                        FromId: moduleNode.Id,
                        ToId: metaNode.Id,
                        Role: "defines",
                        Weight: 1.0,
                        Meta: new Dictionary<string, object>
                        {
                            ["relationship"] = "module-defines-record-type",
                            ["recordType"] = recordType
                        }
                    );
                    _registry.Upsert(edge);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to register record types for module {moduleNode.Id}: {ex.Message}");
        }
    }

    private void GenerateClassMetaNode(Type classType)
    {
        try
        {
            var className = classType.Name;
            var namespaceName = classType.Namespace ?? "Unknown";
            var fullName = $"{namespaceName}.{className}";
            
            // Create class meta-node
            var classNode = new Node(
                Id: $"meta.class.{fullName}",
                TypeId: "meta.class",
                State: ContentState.Water,
                Locale: "en",
                Title: className,
                Description: $"Meta-node for class {className}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new { className, namespaceName, fullName }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = className,
                    ["namespace"] = namespaceName,
                    ["fullName"] = fullName,
                    ["isAbstract"] = classType.IsAbstract,
                    ["isInterface"] = classType.IsInterface,
                    ["isGeneric"] = classType.IsGenericType,
                    ["baseType"] = classType.BaseType?.FullName ?? "System.Object",
                    ["interfaces"] = classType.GetInterfaces().Select(i => i.FullName).ToArray(),
                    ["assembly"] = classType.Assembly.FullName ?? "Unknown"
                }
            );
            
            _registry.Upsert(classNode);
            
            // Generate method meta-nodes
            GenerateMethodMetaNodes(classType);
            
            // Generate API route meta-nodes for modules
            if (typeof(IModule).IsAssignableFrom(classType))
            {
                GenerateApiRouteMetaNodes(classType);
            }
            
            _logger.Info($"Generated meta-nodes for class: {className}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating meta-nodes for class {classType.Name}: {ex.Message}", ex);
        }
    }

    private void GenerateMethodMetaNodes(Type classType)
    {
        var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName); // Exclude properties, events, etc.
            
        foreach (var method in methods)
        {
            try
            {
                var methodName = method.Name;
                var parameters = method.GetParameters().Select(p => new
                {
                    name = p.Name ?? "unknown",
                    type = p.ParameterType.Name,
                    isOptional = p.HasDefaultValue
                }).ToArray();
                
                var methodNode = new Node(
                    Id: $"meta.method.{classType.FullName}.{methodName}",
                    TypeId: "meta.method",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: methodName,
                    Description: $"Method {methodName} in {classType.Name}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new { methodName, className = classType.Name, parameters }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["name"] = methodName,
                        ["className"] = classType.Name,
                        ["returnType"] = method.ReturnType.Name,
                        ["parameters"] = parameters,
                        ["isStatic"] = method.IsStatic,
                        ["isVirtual"] = method.IsVirtual,
                        ["isAbstract"] = method.IsAbstract,
                        ["isAsync"] = method.ReturnType.Name.StartsWith("Task")
                    }
                );
                
                _registry.Upsert(methodNode);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error generating meta-node for method {method.Name}: {ex.Message}", ex);
            }
        }
    }

    private void GenerateApiRouteMetaNodes(Type moduleType)
    {
        try
        {
            var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                var routeAttributes = method.GetCustomAttributes<ApiRouteAttribute>();
                
                foreach (var attr in routeAttributes)
                {
                    var routeNode = new Node(
                        Id: $"meta.route.{moduleType.Name}.{method.Name}",
                        TypeId: "meta.route",
                        State: ContentState.Water,
                        Locale: "en",
                        Title: $"{attr.Verb} {attr.Route}",
                        Description: $"API route {attr.Verb} {attr.Route} in {moduleType.Name}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new { 
                                verb = attr.Verb, 
                                route = attr.Route, 
                                methodName = method.Name, 
                                className = moduleType.Name 
                            }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["httpMethod"] = attr.Verb,
                            ["route"] = attr.Route,
                            ["methodName"] = method.Name,
                            ["className"] = moduleType.Name,
                            ["description"] = attr.Description ?? "",
                            ["isAsync"] = method.ReturnType.Name.StartsWith("Task")
                        }
                    );
                    
                    _registry.Upsert(routeNode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating API route meta-nodes for {moduleType.Name}: {ex.Message}", ex);
        }
    }

    private void GenerateSpecFileMetaNodes()
    {
        try
        {
            // Look for spec files in the project root
            // Try multiple possible locations
            var possibleRoots = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")), // From bin/Debug/net6.0
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")), // From bin/Release/net6.0
                Directory.GetCurrentDirectory(), // Current working directory
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..")) // Two levels up from current
            };
            
            var projectRoot = possibleRoots.FirstOrDefault(Directory.Exists);
            if (projectRoot == null)
            {
                projectRoot = Directory.GetCurrentDirectory();
            }
            
            _logger.Info($"Looking for spec files in: {projectRoot}");
            _logger.Info($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            _logger.Info($"Current Directory: {Directory.GetCurrentDirectory()}");
            
            // Look for spec files in multiple locations
            var specDirectories = new[]
            {
                projectRoot,
                Path.Combine(projectRoot, "specs"),
                Path.Combine(projectRoot, "docs"),
                Path.Combine(projectRoot, "documentation")
            };
            
            var specFiles = new List<string>();
            
            foreach (var specDir in specDirectories)
            {
                if (Directory.Exists(specDir))
                {
                    specFiles.AddRange(Directory.GetFiles(specDir, "*.md", SearchOption.AllDirectories));
                    specFiles.AddRange(Directory.GetFiles(specDir, "*.spec", SearchOption.AllDirectories));
                    specFiles.AddRange(Directory.GetFiles(specDir, "*.txt", SearchOption.AllDirectories));
                }
            }
            
            // Filter spec files - be more inclusive but exclude README files
            var filteredSpecFiles = specFiles
                .Where(f => {
                    var fileName = Path.GetFileName(f);
                    return !fileName.StartsWith("README", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.StartsWith("CHANGELOG", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.StartsWith("CONTRIBUTING", StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();
            
            _logger.Info($"Found {filteredSpecFiles.Length} spec files to process");
            foreach (var specFile in filteredSpecFiles)
            {
                _logger.Info($"  - {Path.GetFileName(specFile)}");
            }
            
            foreach (var specFile in filteredSpecFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(specFile);
                    var content = File.ReadAllText(specFile);
                    var sections = ExtractSections(content);
                    
                    var specNode = new Node(
                        Id: $"meta.spec.{fileName}",
                        TypeId: "meta.spec",
                        State: ContentState.Water,
                        Locale: "en",
                        Title: fileName,
                        Description: $"Specification file: {fileName}",
                        Content: new ContentRef(
                            MediaType: "text/markdown",
                            InlineJson: null,
                            InlineBytes: System.Text.Encoding.UTF8.GetBytes(content),
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["fileName"] = fileName,
                            ["filePath"] = specFile,
                            ["sections"] = sections,
                            ["size"] = content.Length,
                            ["lastModified"] = File.GetLastWriteTime(specFile)
                        }
                    );
                    
                    _registry.Upsert(specNode);
                    
                    // Create section nodes
                    foreach (var section in sections)
                    {
                        var sectionNode = new Node(
                            Id: $"meta.spec.section.{fileName}.{section.Key}",
                            TypeId: "meta.spec.section",
                            State: ContentState.Water,
                            Locale: "en",
                            Title: section.Key,
                            Description: $"Section '{section.Key}' from {fileName}",
                            Content: new ContentRef(
                                MediaType: "text/markdown",
                                InlineJson: null,
                                InlineBytes: System.Text.Encoding.UTF8.GetBytes(section.Value),
                                ExternalUri: null
                            ),
                            Meta: new Dictionary<string, object>
                            {
                                ["fileName"] = fileName,
                                ["sectionName"] = section.Key,
                                ["contentLength"] = section.Value.Length
                            }
                        );
                        
                        _registry.Upsert(sectionNode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing spec file {specFile}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating spec file meta-nodes: {ex.Message}", ex);
        }
    }

    private Dictionary<string, string> ExtractSections(string content)
    {
        var sections = new Dictionary<string, string>();
        var lines = content.Split('\n');
        var currentSection = "Introduction";
        var currentContent = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
            {
                // Save previous section
                if (currentContent.Count > 0)
                {
                    sections[currentSection] = string.Join("\n", currentContent).Trim();
                }
                
                // Start new section
                currentSection = line.TrimStart('#', ' ').Trim();
                currentContent.Clear();
            }
            else
            {
                currentContent.Add(line);
            }
        }
        
        // Save last section
        if (currentContent.Count > 0)
        {
            sections[currentSection] = string.Join("\n", currentContent).Trim();
        }
        
        return sections;
    }
}