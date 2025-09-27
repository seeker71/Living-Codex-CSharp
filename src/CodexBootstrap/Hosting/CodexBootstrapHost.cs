using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Caching;
using CodexBootstrap.Core.Security;
using CodexBootstrap.Core.Storage;
using CodexBootstrap.Modules;
using CodexBootstrap.Runtime;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;

namespace CodexBootstrap.Hosting;

/// <summary>
/// Applies the full Living Codex hosting configuration while keeping Program.cs minimal.
/// </summary>
public static class CodexBootstrapHost
{
    public static void ConfigureBuilder(WebApplicationBuilder builder, string[] args)
    {
        ConfigureServer(builder, args);
        ConfigureSerialization(builder);
        ConfigureCoreServices(builder);
        ConfigureHttp(builder);
        ConfigureAdapters(builder);
        ConfigureShutdownServices(builder);
    }

    public static string ConfigureApp(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ICodexLogger>();
        logger.Info("[Hosting] Starting ConfigureApp...");
        InitializePersistence(app);
        logger.Info("[Hosting] Persistence initialized");
        var hostingUrl = ConfigureMiddleware(app);
        logger.Info("[Hosting] Middleware configured");
        ConfigureEndpoints(app);
        logger.Info("[Hosting] Endpoints configured");
        ConfigureShutdownHandling(app);
        logger.Info("[Hosting] Shutdown handling configured");
        return hostingUrl;
    }

    private static void ConfigureShutdownServices(WebApplicationBuilder builder)
    {
        // Create a global cancellation token source for graceful shutdown
        var shutdownCts = new CancellationTokenSource();
        
        // Add the cancellation token source to the service container
        builder.Services.AddSingleton<CancellationTokenSource>(shutdownCts);
    }

    private static void ConfigureShutdownHandling(WebApplication app)
    {
        // Get the shutdown cancellation token source from the service container
        var shutdownCts = app.Services.GetRequiredService<CancellationTokenSource>();
        
        // Register shutdown handler
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            shutdownCts.Cancel();
            var logger = app.Services.GetRequiredService<ICodexLogger>();
            logger.Info("Application is shutting down - canceling outstanding AI operations...");
        });
    }

    private static void ConfigureServer(WebApplicationBuilder builder, string[] args)
    {
        // Let ASP.NET Core handle URL binding automatically
        // Don't manually configure Kestrel as it conflicts with --urls parameter
        var logger = new Log4NetLogger(typeof(CodexBootstrapHost));
        logger.Info($"Server configuration: URLs will be handled by ASP.NET Core runtime");

        builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
        {
            options.RedirectStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect;
            options.HttpsPort = 5001;
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
        }

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    }

    private static void ConfigureSerialization(WebApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(o =>
        {
            o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.SerializerOptions.WriteIndented = true;
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
    }

    private static void ConfigureCoreServices(WebApplicationBuilder builder)
    {
        var persistenceEnabled = (Environment.GetEnvironmentVariable("PERSISTENCE_ENABLED") ?? "true")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var bootLogger = new Log4NetLogger(typeof(CodexBootstrapHost));
        bootLogger.Info($"[DEBUG] Environment: {environment}, PersistenceEnabled: {persistenceEnabled}");

        builder.Services.AddSingleton(new NodeRegistryBootstrapOptions(persistenceEnabled, environment));
        builder.Services.AddSingleton<ICodexLogger>(_ => new Log4NetLogger(typeof(CodexBootstrapHost)));

        if (persistenceEnabled)
        {
            builder.Services.AddSingleton<IIceStorageBackend>(sp =>
            {
                if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
                {
                    bootLogger.Info("[DEBUG] Using InMemoryIceStorageBackend for Testing environment");
                    return new InMemoryIceStorageBackend();
                }

                var iceStorageType = Environment.GetEnvironmentVariable("ICE_STORAGE_TYPE") ?? "postgresql";
                var iceConnectionString = Environment.GetEnvironmentVariable("ICE_CONNECTION_STRING") ??
                                          "Host=localhost;Database=codex_ice;Username=codex;Password=codex";

                return iceStorageType.ToLowerInvariant() switch
                {
                    "postgresql" => new PostgreSqlIceStorageBackend(iceConnectionString),
                    "sqlite" => new SqliteIceStorageBackend(iceConnectionString),
                    _ => new PostgreSqlIceStorageBackend(iceConnectionString)
                };
            });

            builder.Services.AddSingleton<IWaterStorageBackend>(sp =>
            {
                if (environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
                {
                    bootLogger.Info("[DEBUG] Using InMemoryWaterStorageBackend for Testing environment");
                    return new InMemoryWaterStorageBackend();
                }

                var waterConnectionString = Environment.GetEnvironmentVariable("WATER_CONNECTION_STRING") ??
                                             "Data Source=data/water_cache.db";
                return new SqliteWaterStorageBackend(waterConnectionString);
            });
        }
        else
        {
            builder.Services.AddSingleton<IIceStorageBackend>(_ => new SqliteIceStorageBackend("Data Source=ice_dev.db"));
            builder.Services.AddSingleton<IWaterStorageBackend>(_ => new SqliteWaterStorageBackend("Data Source=water_dev.db"));
        }

        builder.Services.AddSingleton<INodeRegistry>(sp =>
        {
            var iceStorage = sp.GetRequiredService<IIceStorageBackend>();
            var waterStorage = sp.GetRequiredService<IWaterStorageBackend>();
            var logger = new Log4NetLogger(typeof(NodeRegistry));
            return new NodeRegistry(iceStorage, waterStorage, logger);
        });

        // Register IApiRouter implementation
        builder.Services.AddSingleton<IApiRouter>(sp =>
        {
            var registry = sp.GetRequiredService<INodeRegistry>();
            var logger = sp.GetRequiredService<ICodexLogger>();
            return new ApiRouter(registry, logger);
        });

        builder.Services.AddSingleton<ModuleCommunicationWrapper>();
        builder.Services.AddSingleton<PerformanceProfiler>();
        builder.Services.AddSingleton<IInputValidator, InputValidator>();
        builder.Services.AddSingleton<CodexBootstrap.Core.Security.IUserRepository, NodeRegistryUserRepository>();
        builder.Services.AddSingleton<CodexBootstrap.Core.Security.IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<ModuleCompiler>();
        builder.Services.AddSingleton<HotReloadManager>();
        builder.Services.AddSingleton<SelfUpdateSystem>();
        builder.Services.AddSingleton<StableCore>();
        builder.Services.AddSingleton<SpecDrivenArchitecture>();
        builder.Services.AddSingleton<DynamicAttributionSystem>();
        builder.Services.AddSingleton<EndpointGenerator>(sp => new EndpointGenerator(
            sp.GetRequiredService<IApiRouter>(),
            sp.GetRequiredService<INodeRegistry>(),
            sp.GetRequiredService<DynamicAttributionSystem>(),
            sp.GetService<object>() ?? new object()));
        builder.Services.AddSingleton<object>(_ => new object());
        builder.Services.AddHttpClient<IdentityModule>();

        builder.Services.AddSingleton(sp => new ModuleLoader(
            sp.GetRequiredService<INodeRegistry>(),
            sp.GetRequiredService<IApiRouter>(),
            sp));
        builder.Services.AddSingleton<RouteDiscovery>();
        builder.Services.AddSingleton<CoreApiService>();
        builder.Services.AddSingleton<HealthService>();
        builder.Services.AddSingleton<CodexBootstrap.Core.ConfigurationManager>();
        builder.Services.AddLogging(configure => configure.AddConsole().AddDebug());

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<RealtimeModule>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

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

            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
    }

    private static void ConfigureHttp(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var microsoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");

            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = !string.IsNullOrEmpty(googleClientId) ? "Google" :
                                             !string.IsNullOrEmpty(microsoftClientId) ? "Microsoft" : "Cookies";
        })
        .AddCookie("Cookies", options =>
        {
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
            options.Cookie.HttpOnly = true;
            options.LoginPath = "/oauth/google";
            options.LogoutPath = "/oauth/logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.Name = "LivingCodex.Auth";
            options.Cookie.Path = "/";
            options.Cookie.Domain = "localhost";
        });

        ConfigureOAuthProviders(builder);
        builder.Services.AddHttpClient();
    }

    private static void ConfigureOAuthProviders(WebApplicationBuilder builder)
    {
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        if (!string.IsNullOrEmpty(googleClientId))
        {
            builder.Services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? string.Empty;
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
                        if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                        {
                            var email = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                            var name = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                            var picture = context.Principal.FindFirst("picture")?.Value;

                            if (!string.IsNullOrEmpty(email)) identity.AddClaim(new System.Security.Claims.Claim("email", email));
                            if (!string.IsNullOrEmpty(name)) identity.AddClaim(new System.Security.Claims.Claim("name", name));
                            if (!string.IsNullOrEmpty(picture)) identity.AddClaim(new System.Security.Claims.Claim("picture", picture));
                        }

                        return Task.CompletedTask;
                    };
                });
        }

        var microsoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
        if (!string.IsNullOrEmpty(microsoftClientId))
        {
            builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftClientId;
                options.ClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET") ?? string.Empty;
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

    private static void ConfigureAdapters(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IStorageBackend>(sp =>
        {
            var iceStorage = sp.GetRequiredService<IIceStorageBackend>();
            var waterStorage = sp.GetRequiredService<IWaterStorageBackend>();
            var logger = sp.GetRequiredService<ICodexLogger>();
            return new UnifiedStorageBackend(iceStorage, waterStorage, logger);
        });

        builder.Services.AddSingleton<ICacheManager>(sp =>
        {
            var storageBackend = sp.GetRequiredService<IStorageBackend>();
            var logger = sp.GetRequiredService<ICodexLogger>();
            return new NodeCacheManager(storageBackend, logger);
        });

        builder.Services.AddSingleton<ErrorMetrics>();
        builder.Services.AddSingleton<EndpointCacheService>();
    }

    private static void InitializePersistence(WebApplication app)
    {
        var options = app.Services.GetRequiredService<NodeRegistryBootstrapOptions>();

        try
        {
            if (!options.PersistenceEnabled)
            {
                return;
            }

            var iceStorage = app.Services.GetRequiredService<IIceStorageBackend>();
            var waterStorage = app.Services.GetRequiredService<IWaterStorageBackend>();
            var registry = app.Services.GetRequiredService<INodeRegistry>();

            registry.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ICodexLogger>();
            logger.Warn($"Persistence initialization failed: {ex.Message}");
        }
    }

    private static string ConfigureMiddleware(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ICodexLogger>();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development";
        var portConfig = new PortConfigurationService(environment);

        // Global exception handler
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.ContentType = "application/json";
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                logger.Error($"Unhandled exception: {exception?.Message}", exception);

                context.Response.StatusCode = 500;
                var response = new
                {
                    success = false,
                    error = "Internal Server Error",
                    message = app.Environment.IsDevelopment() ? exception?.Message : "An error occurred while processing your request",
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            });
        });

        app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
        app.UseAuthentication();
        app.UseAuthorization();

        // Enable Swagger UI in all environments unless explicitly disabled
        var swaggerEnabledEnv = Environment.GetEnvironmentVariable("SWAGGER_ENABLED");
        var swaggerEnabled = string.IsNullOrEmpty(swaggerEnabledEnv) || !string.Equals(swaggerEnabledEnv, "false", StringComparison.OrdinalIgnoreCase);
        if (swaggerEnabled)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Living Codex API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Living Codex API Documentation";
            });
        }

        try
        {
            portConfig.ValidateConfiguration();
            logger.Info(portConfig.GetConfigurationSummary());
        }
        catch (Exception ex)
        {
            logger.Warn($"Port configuration warning: {ex.Message}");
            logger.Warn("Continuing with fallback configuration...");
        }

        // Initialize global base URL from service map
        var baseUrl = portConfig.GetUrl("codex-bootstrap");
        GlobalConfiguration.Initialize(baseUrl);
        return baseUrl;
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ICodexLogger>();
        logger.Info("[Hosting] Starting ConfigureEndpoints...");
        var registry = app.Services.GetRequiredService<INodeRegistry>();
        logger.Info("[Hosting] Got INodeRegistry service");
        var router = app.Services.GetRequiredService<IApiRouter>();
        logger.Info("[Hosting] Got IApiRouter service");
        var coreApi = app.Services.GetRequiredService<CoreApiService>();
        logger.Info("[Hosting] Got CoreApiService");
        var moduleLoader = app.Services.GetRequiredService<ModuleLoader>();
        logger.Info("[Hosting] Got ModuleLoader");
        var healthService = app.Services.GetRequiredService<HealthService>();
        var configuration = app.Configuration;

        logger.Info("[Hosting] About to initialize registry...");
        registry.InitializeAsync().GetAwaiter().GetResult();
        logger.Info("[Hosting] Registry initialized");
        InitializeMetaNodeSystem(registry);
        logger.Info("[Hosting] Meta node system initialized");
        
        // FOREGROUND: Perform U-CORE seeding synchronously to guarantee availability
        try
        {
            logger.Info("[Hosting] Starting U-CORE seeding (synchronous)...");
            var startTime = DateTime.UtcNow;
            UCoreInitializer.SeedIfMissing(registry, logger).GetAwaiter().GetResult();
            logger.Info($"[Hosting] U-CORE seeding completed in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            logger.Error($"U-CORE seeding failed: {ex.Message}", ex);
        }

        logger.Info("[Hosting] About to load built-in modules...");
        moduleLoader.LoadBuiltInModules();
        logger.Info("[Hosting] Built-in modules loaded");
        var moduleDirectory = configuration.GetValue<string>("ModuleDirectory") ??
                              Path.Combine(AppContext.BaseDirectory, "modules");
        logger.Info("[Hosting] About to load external modules from: " + moduleDirectory);
        moduleLoader.LoadExternalModules(moduleDirectory);
        logger.Info("[Hosting] External modules loaded");
        logger.Info("[Hosting] About to generate meta nodes...");
        moduleLoader.GenerateMetaNodes();
        logger.Info("[Hosting] Meta nodes generated");
        healthService.SetModuleLoader(moduleLoader);

        LogModuleSummary(logger, moduleLoader.GetLoadedModules());

        logger.Info("[Hosting] About to register HTTP endpoints...");
        moduleLoader.RegisterHttpEndpoints(app, registry, coreApi);
        logger.Info("[Hosting] HTTP endpoints registered");

        // Ensure every encountered typeId has a corresponding meta-node (types-as-nodes invariant)
        MetaNodeSystem.EnsureTypeMetaNodes(registry, logger);

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

        app.MapGet("/modules", () => coreApi.GetModules());
        app.MapGet("/modules/{id}", (string id) =>
        {
            var module = coreApi.GetModule(id);
            return module != null ? Results.Ok(module) : Results.NotFound();
        });

        app.MapGet("/modules/loading-report", () => new
        {
            loadedModules = moduleLoader.GetLoadedModules().Count,
            modules = moduleLoader.GetLoadedModules().Select(m => new
            {
                name = m.GetModuleNode().Title,
                id = m.GetModuleNode().Id,
                version = m.GetModuleNode().Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0"
            })
        });

        app.MapGet("/modules/status", () =>
        {
            var loadedModules = moduleLoader.GetLoadedModules();
            var startTime = DateTime.UtcNow.AddHours(-1);

            var moduleStatuses = loadedModules.Select(m =>
            {
                var moduleNode = m.GetModuleNode();
                var moduleType = m.GetType();
                var status = DetermineModuleStatus(m);
                var uptime = CalculateModuleUptime(startTime);
                var lastHealthCheck = DateTime.UtcNow.AddSeconds(-Random.Shared.Next(0, 300));

                return new
                {
                    id = moduleNode.Id,
                    name = moduleNode.Title,
                    version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0",
                    status,
                    uptime,
                    lastHealthCheck,
                    endpoints = GetModuleEndpoints(m),
                    type = moduleType.Name,
                    assembly = moduleType.Assembly.GetName().Name
                };
            }).ToList();

            var activeModules = moduleStatuses.Count(m => m.status == "active");
            var inactiveModules = moduleStatuses.Count(m => m.status == "inactive");
            var errorModules = moduleStatuses.Count(m => m.status == "error");

            return new
            {
                success = true,
                message = "Module status retrieved successfully",
                timestamp = DateTime.UtcNow,
                totalModules = loadedModules.Count,
                activeModules,
                inactiveModules,
                errorModules,
                modules = moduleStatuses
            };
        });

        app.MapGet("/", () => Results.Ok(new
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
        }));

        app.MapGet("/health", () =>
        {
            healthService.IncrementRequestCount();
            return Results.Ok(healthService.GetHealthStatus());
        });

        app.MapGet("/metrics/errors", (ErrorMetrics metrics) =>
        {
            var snapshot = metrics.GetSnapshot();
            return Results.Ok(new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                totalErrors = snapshot.TotalErrors,
                byRoute = snapshot.ErrorsByRoute
            });
        });

        app.MapGet("/cache/stats", (EndpointCacheService cacheService) => Results.Ok(new
        {
            success = true,
            timestamp = DateTime.UtcNow,
            cache = cacheService.GetCacheStats()
        }));

        app.MapPost("/cache/clear-expired", (EndpointCacheService cacheService) =>
        {
            cacheService.ClearExpiredCache();
            return Results.Ok(new
            {
                success = true,
                message = "Expired cache entries cleared",
                timestamp = DateTime.UtcNow
            });
        });

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

        ApiRouteDiscovery.DiscoverAndRegisterRoutes(app, router, registry);
        // Traditional route discovery remains disabled to avoid duplicate registrations.
    }

    private static void LogModuleSummary(ICodexLogger logger, IReadOnlyList<IModule> modules)
    {
        logger.Info("Module Loading Summary:");
        logger.Info($"  Successfully loaded: {modules.Count} modules");
        foreach (var module in modules)
        {
            var moduleNode = module.GetModuleNode();
            var name = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title;
            var version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0";
            logger.Info($"  {name} v{version} ({moduleNode.Id})");
        }
    }

    private static void InitializeMetaNodeSystem(INodeRegistry registry)
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var node in MetaNodeDiscovery.DiscoverMetaNodes(assembly))
        {
            registry.Upsert(node);
        }

        foreach (var node in MetaNodeSystem.CreateCoreMetaNodes())
        {
            registry.Upsert(node);
        }

        foreach (var node in MetaNodeSystem.CreateResponseMetaNodes())
        {
            registry.Upsert(node);
        }

        foreach (var node in MetaNodeSystem.CreateRequestMetaNodes())
        {
            registry.Upsert(node);
        }
    }

    private static string DetermineModuleStatus(IModule module)
    {
        try
        {
            var moduleType = module.GetType();
            var healthMethod = moduleType.GetMethod("GetHealthStatus", BindingFlags.Public | BindingFlags.Instance);

            if (healthMethod != null)
            {
                var healthResult = healthMethod.Invoke(module, null);
                if (healthResult != null)
                {
                    return "active";
                }
            }

            var errorProperties = moduleType.GetProperties()
                .Where(p => p.Name.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.Contains("exception", StringComparison.OrdinalIgnoreCase));

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

            return "active";
        }
        catch
        {
            return "error";
        }
    }

    private static string CalculateModuleUptime(DateTime startTime)
    {
        var uptime = DateTime.UtcNow - startTime;

        if (uptime.TotalDays >= 1)
        {
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }

        if (uptime.TotalMinutes >= 1)
        {
            return $"{uptime.Minutes}m {uptime.Seconds}s";
        }

        return $"{uptime.Seconds}s";
    }

    private static int GetModuleEndpoints(IModule module)
    {
        try
        {
            var moduleType = module.GetType();
            var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var endpointCount = 0;
            foreach (var method in methods)
            {
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

            return endpointCount > 0 ? endpointCount : 1;
        }
        catch
        {
            return 1;
        }
    }

    private sealed record NodeRegistryBootstrapOptions(bool PersistenceEnabled, string EnvironmentName);
}
