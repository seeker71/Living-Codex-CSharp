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

// Load environment variables from .env file
try
{
    var envFile = Path.Combine(Directory.GetCurrentDirectory(), "../../.env");
    if (File.Exists(envFile))
    {
        foreach (var line in File.ReadAllLines(envFile))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
        Console.WriteLine($".env file loaded from: {envFile}");
    }
    else
    {
        Console.WriteLine($".env file not found at: {envFile}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading .env file: {ex.Message}");
}

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

// Get the URLs from ASP.NET Core configuration (includes --urls argument)
var urls = builder.Configuration["urls"] ?? "http://localhost:5001";
var baseUrl = urls.Split(';')[0]; // Take the first URL if multiple are specified

// Parse the port from the URL with error handling
int port = 5001; // Default port
try
{
    if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
    {
        port = uri.Port;
    }
    else
    {
        // Fallback to environment variable or default
        var portEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrEmpty(portEnv) && portEnv.Contains(":"))
        {
            var portStr = portEnv.Split(':').LastOrDefault();
            if (int.TryParse(portStr, out var envPort))
            {
                port = envPort;
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not parse URL '{baseUrl}', using default port 5001. Error: {ex.Message}");
}

// Configure Kestrel to listen on the specified port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port); // HTTP only
});

// Disable HTTPS redirect
builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
{
    options.RedirectStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 5001;
});

// Enable hot reload in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
}

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
        builder.Services.AddSingleton<ModuleCommunicationWrapper>();

        // Generic services with interface registration
        builder.Services.AddSingleton<ModuleLoader>(sp => 
            new ModuleLoader(sp.GetRequiredService<NodeRegistry>(), sp.GetRequiredService<IApiRouter>(), sp));
        builder.Services.AddSingleton<RouteDiscovery>();
        builder.Services.AddSingleton<CoreApiService>();
        builder.Services.AddSingleton<HealthService>();
        builder.Services.AddSingleton<CodexBootstrap.Core.ConfigurationManager>();
        
        // Add generic logging
        builder.Services.AddLogging(configure => configure.AddConsole().AddDebug());
        
        // Add SignalR services for RealtimeModule
        builder.Services.AddSignalR();
        
        
        // Add Swagger/OpenAPI services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Living Codex API",
                Version = "v1",
                Description = "Consciousness-expanding, fractal-based system implementing the U-CORE framework",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Living Codex",
                    Email = "contact@livingcodex.org"
                }
            });
            
            // Resolve conflicting actions by using the first one
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            
            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Add OAuth Authentication (only if configured)
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var microsoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
        
        if (!string.IsNullOrEmpty(googleClientId) || !string.IsNullOrEmpty(microsoftClientId))
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = !string.IsNullOrEmpty(googleClientId) ? "Google" : "Microsoft";
            })
            .AddCookie("Cookies", options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None; // Allow HTTP in development
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/oauth/google";
                options.LogoutPath = "/oauth/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
                options.Cookie.Name = "LivingCodex.Auth";
                options.Cookie.Path = "/";
                options.Cookie.Domain = "localhost";
            });
            
            if (!string.IsNullOrEmpty(googleClientId))
            {
                builder.Services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        options.ClientId = googleClientId;
                        options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "";
                        options.CallbackPath = "/oauth/callback/google";
                        options.SaveTokens = true;
                        options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                        options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                        options.CorrelationCookie.Name = "LivingCodex.Correlation.Google";
                        options.CorrelationCookie.HttpOnly = true;
                        options.CorrelationCookie.IsEssential = true;
                        options.CorrelationCookie.Path = "/";
                        options.CorrelationCookie.Domain = "localhost";
                        options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(10);
                        options.Events.OnCreatingTicket = context =>
                        {
                            // Store user information in claims
                            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                            if (identity != null)
                            {
                                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                                var name = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                                var picture = context.Principal?.FindFirst("picture")?.Value;
                                
                                if (!string.IsNullOrEmpty(email))
                                    identity.AddClaim(new System.Security.Claims.Claim("email", email));
                                if (!string.IsNullOrEmpty(name))
                                    identity.AddClaim(new System.Security.Claims.Claim("name", name));
                                if (!string.IsNullOrEmpty(picture))
                                    identity.AddClaim(new System.Security.Claims.Claim("picture", picture));
                            }
                            return Task.CompletedTask;
                        };
                    });
            }
            
            if (!string.IsNullOrEmpty(microsoftClientId))
            {
                builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
                {
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET") ?? "";
                    options.CallbackPath = "/oauth/callback/microsoft";
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                    options.CorrelationCookie.Name = "LivingCodex.Correlation.Microsoft";
                    options.CorrelationCookie.HttpOnly = true;
                    options.CorrelationCookie.IsEssential = true;
                    options.CorrelationCookie.Path = "/";
                    options.CorrelationCookie.Domain = "localhost";
                    options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(10);
                });
            }
        }
        
        // Register custom logger interface
        builder.Services.AddSingleton<CodexBootstrap.Core.ICodexLogger, Log4NetLogger>(sp => 
            new Log4NetLogger("Program"));


        // Register self-updating system services
        builder.Services.AddSingleton<ModuleCompiler>();
        builder.Services.AddSingleton<HotReloadManager>();
        builder.Services.AddSingleton<SelfUpdateSystem>();
        builder.Services.AddSingleton<StableCore>();
        
        // Register spec-driven architecture services
        builder.Services.AddSingleton<SpecDrivenArchitecture>();
        
        // Register missing services for modules
        builder.Services.AddSingleton<DynamicAttributionSystem>();
        builder.Services.AddSingleton<EndpointGenerator>();
        builder.Services.AddSingleton<object>(sp => new object()); // Placeholder for UCoreResonanceEngine

        // Register HttpClient for SecurityModule REST API calls
        builder.Services.AddHttpClient<CodexBootstrap.Modules.SecurityModule>();

// HTTP client for adapters
builder.Services.AddHttpClient();

var app = builder.Build();

// Initialize global configuration with the application's base URL
GlobalConfiguration.Initialize(baseUrl);

// Disable HTTPS redirection
app.Use(async (context, next) =>
{
    if (context.Request.Scheme == "https")
    {
        context.Request.Scheme = "http";
    }
    await next();
});

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

// Add Authentication middleware
app.UseAuthentication();
app.UseAuthorization();


// Add Swagger middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Living Codex API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Living Codex API Documentation";
    });
}

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

// Inject ModuleLoader into HealthService for accurate module counting
healthService.SetModuleLoader(moduleLoader);

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

// Root endpoint
app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "Living Codex API",
        version = "1.0.0",
        status = "running",
        timestamp = DateTime.UtcNow,
        endpoints = new
        {
            health = "/health",
            modules = "/modules",
            nodes = "/nodes",
            swagger = "/swagger"
        }
    });
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

// Initialize port configuration service
var portConfig = new PortConfigurationService(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development");

try
{
    // Validate port configuration
    portConfig.ValidateConfiguration();
    Console.WriteLine(portConfig.GetConfigurationSummary());
}
catch (Exception ex)
{
    Console.WriteLine($"Port configuration warning: {ex.Message}");
    Console.WriteLine("Continuing with fallback configuration...");
}

// Get the configured port for this service
var configuredPort = portConfig.GetPort("codex-bootstrap");
var url = $"http://localhost:{configuredPort}";

Console.WriteLine($"Starting Living Codex on {url}");

// Only run the server if not in test environment
if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Testing", StringComparison.OrdinalIgnoreCase) == true)
{
    app.Run(url);
}

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
    var assembly = Assembly.GetExecutingAssembly();
    foreach (var node in MetaNodeDiscovery.DiscoverMetaNodes(assembly))
    {
        registry.Upsert(node);
    }

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

// Make Program class accessible for testing
public partial class Program { }
