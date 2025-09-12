using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;
using log4net;
using log4net.Config;

// Configure log4net
var configFile = new FileInfo("log4net.config");
if (configFile.Exists)
{
    XmlConfigurator.Configure(configFile);
    Console.WriteLine($"Log4net configured with config file: {configFile.FullName}");
}
else
{
    Console.WriteLine($"Log4net config file not found at: {configFile.FullName}");
    // Fallback to basic configuration
    BasicConfigurator.Configure();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

        // Core services - only nodes and edges
        // Storage backend will be configured by StorageModule
        builder.Services.AddSingleton<NodeRegistry>(sp =>
        {
            // Start with a basic NodeRegistry, will be replaced by PersistentNodeRegistry
            // when StorageModule is loaded
            return new NodeRegistry();
        });
        builder.Services.AddSingleton<ApiRouter>();
        builder.Services.AddSingleton<IApiRouter>(sp => sp.GetRequiredService<ApiRouter>());



// Generic services
builder.Services.AddSingleton<ModuleLoader>(sp => 
    new ModuleLoader(sp.GetRequiredService<NodeRegistry>(), sp.GetRequiredService<IApiRouter>(), sp));
builder.Services.AddSingleton<RouteDiscovery>();
builder.Services.AddSingleton<CoreApiService>();
builder.Services.AddSingleton<HealthService>();
builder.Services.AddSingleton<ConfigurationManager>();

// HTTP client for adapters
builder.Services.AddHttpClient();

var app = builder.Build();

// Resolve services
var registry = app.Services.GetRequiredService<NodeRegistry>();
var router = app.Services.GetRequiredService<IApiRouter>();
var coreApi = app.Services.GetRequiredService<CoreApiService>();
var moduleLoader = app.Services.GetRequiredService<ModuleLoader>();
var routeDiscovery = app.Services.GetRequiredService<RouteDiscovery>();
var healthService = app.Services.GetRequiredService<HealthService>();

// Initialize meta-node system
InitializeMetaNodeSystem(registry);

// Load all built-in modules using the standardized approach
moduleLoader.LoadBuiltInModules();

// Load external modules from ./modules/*.dll
var moduleDir = Path.Combine(AppContext.BaseDirectory, "modules");
moduleLoader.LoadExternalModules(moduleDir);

// Generate meta-nodes for all loaded modules and spec files
moduleLoader.GenerateMetaNodes();

// Display comprehensive module loading summary
var loadedModules = moduleLoader.GetLoadedModules();
var logger = new Log4NetLogger("Program");
logger.Info($"Module Loading Summary:");
logger.Info($"  Successfully loaded: {loadedModules.Count} modules");
foreach (var module in loadedModules)
{
    var moduleNode = module.GetModuleNode();
    var name = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title;
    var version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0";
    logger.Info($"  {name} v{version} ({moduleNode.Id})");
}

// Register HTTP endpoints from all modules
moduleLoader.RegisterHttpEndpoints(app, registry, coreApi);

// ---- Core API Routes (Node/Edge only) ----
app.MapGet("/nodes", () => coreApi.GetNodes());
app.MapGet("/nodes/{id}", (string id) => 
{
    var node = coreApi.GetNode(id);
    return node != null ? Results.Ok(node) : Results.NotFound();
});
app.MapPost("/nodes", (Node node) => Results.Ok(coreApi.UpsertNode(node)));

app.MapGet("/edges", () => coreApi.GetEdges());
app.MapPost("/edges", (Edge edge) => Results.Ok(coreApi.UpsertEdge(edge)));

app.MapGet("/nodes/type/{typeId}", (string typeId) => coreApi.GetNodesByType(typeId));
app.MapGet("/edges/from/{fromId}", (string fromId) => coreApi.GetEdgesFrom(fromId));
app.MapGet("/edges/to/{toId}", (string toId) => coreApi.GetEdgesTo(toId));

// Module discovery
app.MapGet("/modules", () => coreApi.GetModules());
app.MapGet("/modules/{id}", (string id) => 
{
    var module = coreApi.GetModule(id);
    return module != null ? Results.Ok(module) : Results.NotFound();
});

// Module loading report
app.MapGet("/modules/loading-report", () => new {
    loadedModules = moduleLoader.GetLoadedModules().Count,
    modules = moduleLoader.GetLoadedModules().Select(m => new {
        name = m.GetModuleNode().Title,
        id = m.GetModuleNode().Id,
        version = m.GetModuleNode().Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0"
    })
});

// Health endpoint
app.MapGet("/health", () =>
{
    healthService.IncrementRequestCount();
    return Results.Ok(healthService.GetHealthStatus());
});

// Dynamic API route — self‑describing invocation
app.MapPost("/route", async (DynamicCall req) =>
{
    try
    {
        var result = await coreApi.ExecuteDynamicCall(req);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(ex.Message);
    }
});

// Discover and register attribute-based API routes
ApiRouteDiscovery.DiscoverAndRegisterRoutes(app, router, registry);

// Note: Traditional route discovery is disabled to prevent duplicate route registration
// routeDiscovery.DiscoverAndRegisterRoutes(app);

app.Run();

// Initialize meta-node system
static void InitializeMetaNodeSystem(NodeRegistry registry)
{
    // Register attribute-based meta-nodes
    // MetaNodeDiscovery is temporarily disabled
    // var assembly = Assembly.GetExecutingAssembly();
    // foreach (var node in MetaNodeDiscovery.DiscoverMetaNodes(assembly))
    // {
    //     registry.Upsert(node);
    // }

    // Register legacy meta-nodes for backward compatibility
    foreach (var node in MetaNodeSystem.CreateCoreMetaNodes())
    {
        registry.Upsert(node);
    }

    // Register response meta-nodes
    foreach (var node in MetaNodeSystem.CreateResponseMetaNodes())
    {
        registry.Upsert(node);
    }

    // Register request meta-nodes
    foreach (var node in MetaNodeSystem.CreateRequestMetaNodes())
    {
        registry.Upsert(node);
    }
}
