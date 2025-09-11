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

        // Authentication and Authorization services
        builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        builder.Services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
        builder.Services.AddSingleton<IPermissionRepository, InMemoryPermissionRepository>();
        
        // JWT settings - in production, these should come from configuration
        var jwtSettings = new JwtSettings(
            SecretKey: Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-key-that-is-at-least-32-characters-long",
            Issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CodexBootstrap",
            Audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CodexBootstrap",
            ExpirationMinutes: int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60")
        );
        builder.Services.AddSingleton(jwtSettings);
        
        builder.Services.AddSingleton<IAuthenticationService, JwtAuthenticationService>();
        builder.Services.AddSingleton<IAuthorizationService, RoleBasedAuthorizationService>();


// Generic services
builder.Services.AddSingleton<ModuleLoader>(sp => 
    new ModuleLoader(sp.GetRequiredService<NodeRegistry>(), sp.GetRequiredService<IApiRouter>(), sp));
builder.Services.AddSingleton<RouteDiscovery>();
builder.Services.AddSingleton<CoreApiService>();
builder.Services.AddSingleton<HealthService>();

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

// Load only absolutely core modules statically
LoadCoreModules(moduleLoader, app.Services, registry);

// Load external modules from ./modules/*.dll
var moduleDir = Path.Combine(AppContext.BaseDirectory, "modules");
moduleLoader.LoadExternalModules(moduleDir);

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

// Load only absolutely core modules that are essential for basic system operation
static void LoadCoreModules(ModuleLoader moduleLoader, IServiceProvider serviceProvider, NodeRegistry registry)
{
    var logger = new Log4NetLogger("Program");
    logger.Info("Loading absolutely core modules...");
    
    // Define absolutely core module types that must be loaded statically
    var coreModuleTypes = new[]
    {
        "CodexBootstrap.Modules.CoreModule",
        "CodexBootstrap.Modules.StorageModule", 
        "CodexBootstrap.Modules.PhaseModule",
        "CodexBootstrap.Modules.DeltaModule",
        "CodexBootstrap.Modules.SpecModule",
        "CodexBootstrap.Modules.HydrateModule"
    };
    
    var assembly = Assembly.GetExecutingAssembly();
    var loadedCount = 0;
    
    foreach (var moduleTypeName in coreModuleTypes)
    {
        try
        {
            var moduleType = assembly.GetType(moduleTypeName);
            if (moduleType != null && typeof(IModule).IsAssignableFrom(moduleType))
            {
                logger.Info($"Loading core module: {moduleTypeName}");
                var module = ActivatorUtilities.CreateInstance(serviceProvider, moduleType) as IModule;
                if (module != null)
                {
                    // Special handling for StorageModule - replace NodeRegistry with PersistentNodeRegistry
                    if (module is StorageModule storageModule)
                    {
                        var storageBackend = storageModule.GetStorageBackend();
                        if (storageBackend != null)
                        {
                            // Replace the basic NodeRegistry with PersistentNodeRegistry
                            var persistentRegistry = new PersistentNodeRegistry(storageBackend);
                            
                            // Update the service provider to use the persistent registry
                            // This is a bit of a hack, but necessary for the current architecture
                            logger.Info("Replacing NodeRegistry with PersistentNodeRegistry");
                            
                            // Initialize the persistent storage
                            persistentRegistry.InitializeAsync().Wait();
                            logger.Info("Persistent storage initialized");
                            
                            // Update the registry reference
                            registry = persistentRegistry;
                        }
                    }
                    
                    moduleLoader.LoadModule(module);
                    loadedCount++;
                    logger.Info($"Successfully loaded core module: {moduleTypeName}");
                }
                else
                {
                    logger.Warn($"Failed to create core module: {moduleTypeName}");
                }
            }
            else
            {
                logger.Warn($"Core module type not found: {moduleTypeName}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load core module {moduleTypeName}: {ex.Message}", ex);
        }
    }
    
    logger.Info($"Loaded {loadedCount} core modules");
}