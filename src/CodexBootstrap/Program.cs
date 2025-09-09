using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Core services - only nodes and edges
builder.Services.AddSingleton<NodeRegistry>();
builder.Services.AddSingleton<ApiRouter>();
builder.Services.AddSingleton<IApiRouter>(sp => sp.GetRequiredService<ApiRouter>());

// Generic services
builder.Services.AddSingleton<ModuleLoader>(sp => 
    new ModuleLoader(sp.GetRequiredService<NodeRegistry>(), sp.GetRequiredService<IApiRouter>(), sp));
builder.Services.AddSingleton<RouteDiscovery>();
builder.Services.AddSingleton<CoreApiService>();

// HTTP client for adapters
builder.Services.AddHttpClient();

var app = builder.Build();

// Resolve services
var registry = app.Services.GetRequiredService<NodeRegistry>();
var router = app.Services.GetRequiredService<IApiRouter>();
var coreApi = app.Services.GetRequiredService<CoreApiService>();
var moduleLoader = app.Services.GetRequiredService<ModuleLoader>();
var routeDiscovery = app.Services.GetRequiredService<RouteDiscovery>();

// Load modules generically (includes API handler registration)
moduleLoader.LoadBuiltInModules();

// Load external modules from ./modules/*.dll
var moduleDir = Path.Combine(AppContext.BaseDirectory, "modules");
moduleLoader.LoadExternalModules(moduleDir);

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

// OpenAPI specifications for modules
app.MapGet("/openapi/{moduleId}", (string moduleId) =>
{
    try
    {
        // Get the module node to find the module instance
        var moduleNode = coreApi.GetModule(moduleId);
        if (moduleNode == null)
        {
            return Results.NotFound($"Module '{moduleId}' not found");
        }

        // Find the module instance that implements IOpenApiProvider
        var moduleInstances = moduleLoader.GetLoadedModules();
        var openApiModule = moduleInstances.OfType<IOpenApiProvider>()
            .FirstOrDefault(m => ((IModule)m).GetModuleNode().Id == moduleId);

        if (openApiModule == null)
        {
            return Results.NotFound($"Module '{moduleId}' does not support OpenAPI");
        }

        var openApiSpec = openApiModule.GetOpenApiSpec();
        return Results.Ok(openApiSpec);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to get OpenAPI spec for module '{moduleId}': {ex.Message}");
    }
});

// Dynamic API route — self‑describing invocation
app.MapPost("/route", (DynamicCall req) =>
{
    try
    {
        var result = coreApi.ExecuteDynamicCall(req);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(ex.Message);
    }
});

// Discover and register module-specific routes automatically
routeDiscovery.DiscoverAndRegisterRoutes(app);

app.Run();