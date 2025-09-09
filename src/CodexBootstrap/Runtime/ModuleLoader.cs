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

    public ModuleLoader(NodeRegistry registry, IApiRouter router, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _router = router;
        _serviceProvider = serviceProvider;
    }

    public void LoadBuiltInModules()
    {
        // Load all built-in modules from the Modules namespace
        var moduleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
            .Where(t => t.Namespace?.StartsWith("CodexBootstrap.Modules") == true);

        foreach (var moduleType in moduleTypes)
        {
            try
            {
                var module = ActivatorUtilities.CreateInstance(_serviceProvider, moduleType) as IModule;
                if (module != null)
                {
                    LoadModule(module);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load module {moduleType.Name}: {ex.Message}");
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
                Console.WriteLine($"Failed to load assembly {dll}: {ex.Message}");
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

    private void LoadModule(IModule module)
    {
        var moduleNode = module.GetModuleNode();
        _registry.Upsert(moduleNode);
        module.Register(_registry);
        module.RegisterApiHandlers(_router, _registry);
        
        var name = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title;
        var version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0";
        Console.WriteLine($"Loaded module: {name} v{version}");
    }
}