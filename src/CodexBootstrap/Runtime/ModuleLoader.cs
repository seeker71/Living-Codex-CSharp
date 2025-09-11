using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class ModuleLoader
{
    private readonly NodeRegistry _registry;
    private readonly IApiRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IModule> _loadedModules = new();
    private readonly Core.ILogger _logger;

    public ModuleLoader(NodeRegistry registry, IApiRouter router, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _router = router;
        _serviceProvider = serviceProvider;
        _logger = new Log4NetLogger(typeof(ModuleLoader));
    }

    public IReadOnlyList<IModule> GetLoadedModules() => _loadedModules.AsReadOnly();

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi)
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
                var module = ActivatorUtilities.CreateInstance(_serviceProvider, moduleType) as IModule;
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
        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IModule).IsAssignableFrom(type) && !type.IsAbstract && Activator.CreateInstance(type) is IModule module)
            {
                LoadModule(module);
            }
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
            _registry.Upsert(moduleNode);
            module.Register(_registry);
            module.RegisterApiHandlers(_router, _registry);
            
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
}