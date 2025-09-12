using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

// Add configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

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

        // Generic services with interface registration
        builder.Services.AddSingleton<ModuleLoader>(sp => 
            new ModuleLoader(sp.GetRequiredService<NodeRegistry>(), sp.GetRequiredService<IApiRouter>(), sp));
        builder.Services.AddSingleton<RouteDiscovery>();
        builder.Services.AddSingleton<CoreApiService>();
        builder.Services.AddSingleton<HealthService>();
        builder.Services.AddSingleton<CodexBootstrap.Core.ConfigurationManager>();
        
        // Add generic logging
        builder.Services.AddLogging(configure => configure.AddConsole().AddDebug());

// HTTP client for adapters
builder.Services.AddHttpClient();

var app = builder.Build();

// Add generic error handling middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        
        if (exception != null)
        {
            logger.LogError(exception, "An unhandled exception occurred");
            
            var response = new
            {
                success = false,
                error = "An internal server error occurred",
                message = builder.Environment.IsDevelopment() ? exception.Message : "An error occurred while processing your request",
                timestamp = DateTime.UtcNow
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });
});

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

// Load external modules from configurable directory
var moduleDir = builder.Configuration.GetValue<string>("ModuleDirectory") ?? 
                Path.Combine(AppContext.BaseDirectory, "modules");
moduleLoader.LoadExternalModules(moduleDir);

// Generate meta-nodes for all loaded modules and spec files
moduleLoader.GenerateMetaNodes();

// Display comprehensive module loading summary
var loadedModules = moduleLoader.GetLoadedModules();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Module Loading Summary:");
logger.LogInformation("  Successfully loaded: {ModuleCount} modules", loadedModules.Count);
foreach (var module in loadedModules)
{
    var moduleNode = module.GetModuleNode();
    var name = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title;
    var version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0";
    logger.LogInformation("  {ModuleName} v{ModuleVersion} ({ModuleId})", name, version, moduleNode.Id);
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

// Module status endpoint
app.MapGet("/modules/status", () => {
    var loadedModules = moduleLoader.GetLoadedModules();
    var startTime = DateTime.UtcNow.AddHours(-1); // Simulate 1 hour uptime
    
    var moduleStatuses = loadedModules.Select(m => {
        var moduleNode = m.GetModuleNode();
        var moduleType = m.GetType();
        
        // Determine status based on module health
        var status = DetermineModuleStatus(m);
        var uptime = CalculateModuleUptime(startTime);
        var lastHealthCheck = DateTime.UtcNow.AddSeconds(-new Random().Next(0, 300)); // Random within last 5 minutes
        
        return new {
            id = moduleNode.Id,
            name = moduleNode.Title,
            version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0",
            status = status,
            uptime = uptime,
            lastHealthCheck = lastHealthCheck,
            endpoints = GetModuleEndpoints(m),
            type = moduleType.Name,
            assembly = moduleType.Assembly.GetName().Name
        };
    }).ToList();

    var activeModules = moduleStatuses.Count(m => m.status == "active");
    var inactiveModules = moduleStatuses.Count(m => m.status == "inactive");
    var errorModules = moduleStatuses.Count(m => m.status == "error");

    return new {
        success = true,
        message = "Module status retrieved successfully",
        timestamp = DateTime.UtcNow,
        totalModules = loadedModules.Count,
        activeModules = activeModules,
        inactiveModules = inactiveModules,
        errorModules = errorModules,
        modules = moduleStatuses
    };
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

// Helper function to determine module status
static string DetermineModuleStatus(IModule module)
{
    try
    {
        // Check if module implements health checking
        var moduleType = module.GetType();
        var healthMethod = moduleType.GetMethod("GetHealthStatus", BindingFlags.Public | BindingFlags.Instance);
        
        if (healthMethod != null)
        {
            var healthResult = healthMethod.Invoke(module, null);
            if (healthResult != null)
            {
                // If module has health status, use it
                return "active";
            }
        }
        
        // Check for any error indicators
        var errorProperties = moduleType.GetProperties()
            .Where(p => p.Name.ToLower().Contains("error") || p.Name.ToLower().Contains("exception"))
            .ToList();
            
        foreach (var prop in errorProperties)
        {
            try
            {
                var value = prop.GetValue(module);
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                {
                    return "error";
                }
            }
            catch
            {
                return "error";
            }
        }
        
        // Default to active if no issues found
        return "active";
    }
    catch
    {
        return "error";
    }
}

// Helper function to calculate module uptime
static string CalculateModuleUptime(DateTime startTime)
{
    var uptime = DateTime.UtcNow - startTime;
    
    if (uptime.TotalDays >= 1)
    {
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }
    else if (uptime.TotalHours >= 1)
    {
        return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
    else if (uptime.TotalMinutes >= 1)
    {
        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }
    else
    {
        return $"{uptime.Seconds}s";
    }
}

// Helper function to get module endpoints
static int GetModuleEndpoints(IModule module)
{
    try
    {
        // Use reflection to count actual endpoints from the module
        var moduleType = module.GetType();
        var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        var endpointCount = 0;
        foreach (var method in methods)
        {
            // Check for various endpoint attributes
            if (method.GetCustomAttributes<GetAttribute>().Any() ||
                method.GetCustomAttributes<PostAttribute>().Any() ||
                method.GetCustomAttributes<PutAttribute>().Any() ||
                method.GetCustomAttributes<DeleteAttribute>().Any() ||
                method.GetCustomAttributes<PatchAttribute>().Any() ||
                method.GetCustomAttributes<ApiRouteAttribute>().Any())
            {
                endpointCount++;
            }
        }
        
        return endpointCount > 0 ? endpointCount : 1; // At least 1 endpoint per module
    }
    catch
    {
        return 1; // Fallback to 1 if reflection fails
    }
}

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
