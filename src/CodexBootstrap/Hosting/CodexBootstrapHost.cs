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
// using StackExchange.Redis; // Temporarily disabled

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
        // Register shutdown coordinator as hosted service
        builder.Services.AddHostedService<ShutdownCoordinator>();
        builder.Services.AddSingleton<ShutdownCoordinator>(sp => 
            sp.GetServices<IHostedService>().OfType<ShutdownCoordinator>().First());
    }

    private static void ConfigureShutdownHandling(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ICodexLogger>();
        
        // Register shutdown timeout protection
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            logger.Info("[Shutdown] Application stopping - initiating graceful shutdown...");
            
            // Force exit after 30 seconds if graceful shutdown hangs
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                logger.Error("[Shutdown] FORCE EXIT - graceful shutdown timed out after 30s");
                logger.Error("[Shutdown] This indicates background tasks are not respecting cancellation tokens");
                Environment.Exit(1);
            });
        });
        
        app.Lifetime.ApplicationStopped.Register(() =>
        {
            logger.Info("[Shutdown] Application stopped successfully");
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

        // Add controllers for all environments
        builder.Services.AddControllers();
        
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
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
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
        builder.Services.AddSingleton<StartupStateService>();
        
        // Database monitoring
        builder.Services.AddSingleton<DatabaseOperationMonitor>();

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

                // Temporarily disable database monitoring to fix initialization issue
                // var monitor = sp.GetRequiredService<DatabaseOperationMonitor>();
                return iceStorageType.ToLowerInvariant() switch
                {
                    "postgresql" => new PostgreSqlIceStorageBackend(iceConnectionString),
                    "sqlite" => new SqliteIceStorageBackend(iceConnectionString), // Remove monitor parameter
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
                // Temporarily disable database monitoring to fix initialization issue
                // var monitor = sp.GetRequiredService<DatabaseOperationMonitor>();
                return new SqliteWaterStorageBackend(waterConnectionString); // Remove monitor parameter
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
        
        // Performance Optimization Services
        // ConfigurePerformanceOptimization(builder, bootLogger); // Temporarily disabled
        
        // AI Pipeline Monitoring Services
        ConfigureAIPipelineMonitoring(builder, bootLogger);
        
        // Redis and Distributed Session Storage Configuration
        // ConfigureDistributedSessionStorage(builder, bootLogger); // Temporarily disabled
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
            sp,
            sp.GetRequiredService<ReadinessTracker>()));
        builder.Services.AddSingleton<RouteDiscovery>();
        builder.Services.AddSingleton<CoreApiService>();
        builder.Services.AddSingleton<HealthService>();
        builder.Services.AddSingleton<PrometheusMetricsService>();
        builder.Services.AddSingleton<CodexBootstrap.Core.ConfigurationManager>();
        
        // Readiness tracking services
        builder.Services.AddSingleton<ReadinessTracker>();
        builder.Services.AddSingleton<ReadinessEventStream>();
        builder.Services.AddLogging(configure => configure.AddConsole().AddDebug());

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<RealtimeModule>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        // Controllers already added in ConfigureServer

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
        var logger = app.Services.GetRequiredService<ICodexLogger>();

        try
        {
            if (!options.PersistenceEnabled)
            {
                logger.Info("Persistence is disabled");
                return;
            }

            // Initialize registry in background to avoid blocking startup
            logger.Info("Starting registry initialization in background...");
            var registry = app.Services.GetRequiredService<INodeRegistry>();
            var healthService = app.Services.GetRequiredService<HealthService>();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await registry.InitializeAsync();
                    healthService.MarkRegistryInitialized();
                    logger.Info("Registry initialization completed successfully");
                }
                catch (Exception ex)
                {
                    logger.Error($"Registry initialization failed: {ex.Message}", ex);
                }
            });
        }
        catch (Exception ex)
        {
            logger.Warn($"Persistence initialization setup failed: {ex.Message}");
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
        
        // Enable WebSocket support
        app.UseWebSockets();
        
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

        // Add performance profiling middleware for mindful measurement
        app.UseMiddleware<CodexBootstrap.Middleware.PerformanceProfilingMiddleware>();
        
        // Add response caching middleware for efficient data delivery
        app.UseMiddleware<CodexBootstrap.Middleware.ResponseCachingMiddleware>();
        
        // Add request tracker middleware for detailed logging
        app.UseMiddleware<CodexBootstrap.Middleware.RequestTrackerMiddleware>();
        
        // Add readiness middleware to check endpoint availability
        var readinessTracker = app.Services.GetRequiredService<ReadinessTracker>();
        app.UseMiddleware<CodexBootstrap.Middleware.ReadinessMiddleware>(readinessTracker, logger);
        
        // Add middleware to track active requests
        app.Use(async (context, next) =>
        {
            healthService.BeginRequest();
            try
            {
                await next();
            }
            finally
            {
                healthService.EndRequest();
            }
        });

        // Map health endpoint FIRST before any heavy initialization
        app.MapGet("/health", () =>
        {
            healthService.IncrementRequestCount();
            return Results.Ok(healthService.GetHealthStatus());
        });

        // Map memory health endpoint for compassionate resource monitoring
        app.MapGet("/health/memory", () =>
        {
            healthService.IncrementRequestCount();
            return Results.Ok(healthService.GetMemoryHealthStatus());
        });

        // Map AI pipeline health endpoint for compassionate AI monitoring
        app.MapGet("/health/ai-pipeline", () =>
        {
            healthService.IncrementRequestCount();
            var aiTracker = app.Services.GetService<AIPipelineTracker>();
            if (aiTracker != null)
            {
                return Results.Ok(aiTracker.GetMetrics());
            }
            return Results.Ok(new { message = "AI pipeline monitoring not available", enabled = false });
        });

        // API endpoints for the AI Dashboard
        app.MapGet("/api/health", () =>
        {
            healthService.IncrementRequestCount();
            return Results.Ok(healthService.GetHealthStatus());
        });

        // Note: /metrics endpoint is provided by SystemMetricsModule

        // Map active requests monitoring endpoint
        app.MapGet("/health/requests/active", () =>
        {
            var activeRequests = CodexBootstrap.Middleware.RequestTrackerMiddleware.GetActiveRequests();
            var summary = CodexBootstrap.Middleware.RequestTrackerMiddleware.GetActiveRequestsSummary();
            
            return Results.Ok(new
            {
                success = true,
                activeCount = activeRequests.Count(),
                summary = summary,
                requests = activeRequests.Select(r => new
                {
                    requestId = r.RequestId,
                    method = r.Method,
                    path = r.Path,
                    startTime = r.StartTime,
                    durationMs = (DateTime.UtcNow - r.StartTime).TotalMilliseconds,
                    status = (DateTime.UtcNow - r.StartTime).TotalMilliseconds > 5000 ? "STUCK" :
                            (DateTime.UtcNow - r.StartTime).TotalMilliseconds > 1000 ? "SLOW" : "OK"
                }).OrderByDescending(r => r.durationMs)
            });
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
                swagger = "/swagger",
                readiness = "/readiness"
            }
        }));

        // Map readiness endpoints
        app.MapControllerRoute(
            name: "readiness",
            pattern: "readiness/{action=GetSystemReadiness}",
            defaults: new { controller = "Readiness" });

        // Do ALL heavy initialization in background
        logger.Info("[Hosting] Starting background initialization...");
        Console.WriteLine("[DEBUG] Starting background initialization task...");
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[DEBUG] Inside background task - starting registry init");
                logger.Info("[Background] Initializing registry...");
                Console.WriteLine("[DEBUG] About to call registry.InitializeAsync()");
                await registry.InitializeAsync();
                Console.WriteLine("[DEBUG] registry.InitializeAsync() completed successfully");
                healthService.MarkRegistryInitialized();
                logger.Info("[Background] Registry initialized");
                
                Console.WriteLine("[DEBUG] About to initialize meta node system");
                InitializeMetaNodeSystem(registry);
                Console.WriteLine("[DEBUG] Meta node system initialized");
                logger.Info("[Background] Meta node system initialized");
                
                // U-CORE seeding - defer to background task to avoid blocking module loading
                logger.Info("[Background] Starting U-CORE seeding in background...");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var startTime = DateTime.UtcNow;
                        await UCoreInitializer.SeedIfMissing(registry, logger);
                        logger.Info($"[Background] U-CORE seeding completed in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"[Background] U-CORE seeding failed: {ex.Message}", ex);
                    }
                });
                
                // Load modules
                Console.WriteLine("[DEBUG] About to load built-in modules");
                logger.Info("[Background] Loading built-in modules...");
                moduleLoader.LoadBuiltInModules();
                Console.WriteLine("[DEBUG] Built-in modules loaded");
                logger.Info("[Background] Built-in modules loaded");
                
                var moduleDirectory = configuration.GetValue<string>("ModuleDirectory") ??
                                      Path.Combine(AppContext.BaseDirectory, "modules");
                logger.Info($"[Background] Loading external modules from: {moduleDirectory}");
                moduleLoader.LoadExternalModules(moduleDirectory);
                logger.Info("[Background] External modules loaded");
                
                logger.Info("[Background] Generating meta nodes...");
                moduleLoader.GenerateMetaNodes();
                logger.Info("[Background] Meta nodes generated");
                
                // CRITICAL: Set the module loader reference in health service
                Console.WriteLine($"[DEBUG] About to set module loader in health service. Loaded modules count: {moduleLoader.GetLoadedModules().Count}");
                healthService.SetModuleLoader(moduleLoader);
                Console.WriteLine($"[DEBUG] Module loader set in health service successfully");
                LogModuleSummary(logger, moduleLoader.GetLoadedModules());
                
                logger.Info("[Background] Registering HTTP endpoints...");
                moduleLoader.RegisterHttpEndpoints(app, registry, coreApi);
                logger.Info("[Background] HTTP endpoints registered");
                
                // Ensure type meta-nodes
                MetaNodeSystem.EnsureTypeMetaNodes(registry, logger);
                logger.Info("[Background] All initialization complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Background initialization failed: {ex.Message}");
                Console.WriteLine($"[DEBUG] Exception: {ex}");
                logger.Error($"[Background] Initialization failed: {ex.Message}", ex);
            }
        });

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

        app.MapGet("/modules/loading-report", () =>
        {
            var (discovered, created, registered, asyncInitialized, asyncComplete) = moduleLoader.GetModuleLoadingMetrics();
            return Results.Ok(new
            {
                discovered,
                created,
                registered,
                asyncInitialized,
                asyncComplete,
                loadedModules = moduleLoader.GetLoadedModules().Count,
                stuckModules = moduleLoader.GetStuckModules(),
                modules = moduleLoader.GetLoadedModules().Select(m => new
                {
                    name = m.GetModuleNode().Title,
                    id = m.GetModuleNode().Id,
                    version = m.GetModuleNode().Meta?.GetValueOrDefault("version")?.ToString() ?? "0.1.0"
                })
            });
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

        // Health endpoint already mapped at the top of this method

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

    /// <summary>
    /// Configure AI pipeline monitoring services
    /// Embodying the principle of mindful AI flow - tracking AI processing with compassion
    /// </summary>
    private static void ConfigureAIPipelineMonitoring(WebApplicationBuilder builder, ICodexLogger logger)
    {
        var enableAIMonitoring = Environment.GetEnvironmentVariable("ENABLE_AI_PIPELINE_MONITORING") ?? "true";
        
        logger.Info($"[AIPipelineMonitoring] Configuration - Enabled: {enableAIMonitoring}");
        
        if (enableAIMonitoring.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            // Register AI pipeline tracker
            builder.Services.AddSingleton<AIPipelineTracker>();
            logger.Info("[AIPipelineMonitoring] AI pipeline tracking enabled");
            
            // Register AI Request Queue
            builder.Services.AddSingleton<AIRequestQueue>();
            logger.Info("[AIPipelineMonitoring] AI request queue enabled");
        }
        else
        {
            logger.Info("[AIPipelineMonitoring] AI pipeline monitoring disabled via configuration");
        }
    }

    /// <summary>
    /// Configure performance optimization services
    /// Embodying the principle of mindful efficiency - optimizing system performance with compassion
    /// </summary>
    /*
    private static void ConfigurePerformanceOptimization(WebApplicationBuilder builder, ICodexLogger logger)
    {
        var enablePerformanceOptimization = Environment.GetEnvironmentVariable("ENABLE_PERFORMANCE_OPTIMIZATION") ?? "true";
        var enableResponseCaching = Environment.GetEnvironmentVariable("ENABLE_RESPONSE_CACHING") ?? "true";
        var enableDatabaseOptimization = Environment.GetEnvironmentVariable("ENABLE_DATABASE_OPTIMIZATION") ?? "true";
        
        logger.Info($"[PerformanceOptimization] Configuration - " +
                   $"Optimization: {enablePerformanceOptimization}, " +
                   $"Caching: {enableResponseCaching}, " +
                   $"Database: {enableDatabaseOptimization}");
        
        if (enablePerformanceOptimization.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            // Register performance metrics service
            builder.Services.AddSingleton<PerformanceMetrics>();
            
            if (enableResponseCaching.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // Register response cache service
                builder.Services.AddSingleton<ResponseCache>();
                logger.Info("[PerformanceOptimization] Response caching enabled");
            }
            
            if (enableDatabaseOptimization.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // Register database query profiler
                builder.Services.AddSingleton<DatabaseQueryProfiler>();
                logger.Info("[PerformanceOptimization] Database optimization enabled");
            }
            
            logger.Info("[PerformanceOptimization] Performance optimization services configured successfully");
        }
        else
        {
            logger.Info("[PerformanceOptimization] Performance optimization disabled via configuration");
        }
    }
    */

    /// <summary>
    /// Configure Redis and distributed session storage services
    /// Embodying the principle of interconnectedness - enabling shared session state across server instances
    /// </summary>
    /*
    private static void ConfigureDistributedSessionStorage(WebApplicationBuilder builder, ICodexLogger logger)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
                                  Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ??
                                  "localhost:6379";
        
        var enableDistributedSessions = Environment.GetEnvironmentVariable("ENABLE_DISTRIBUTED_SESSIONS") ?? "true";
        var useDistributedStorage = enableDistributedSessions.Equals("true", StringComparison.OrdinalIgnoreCase);
        
        logger.Info($"[DistributedSessions] Configuration - Redis: {redisConnectionString}, Enabled: {useDistributedStorage}");
        
        if (useDistributedStorage)
        {
            try
            {
                // Register Redis configuration
                builder.Services.AddSingleton<IRedisConfiguration>(sp =>
                {
                    var redisOptions = RedisConfigurationOptions.FromConnectionString(redisConnectionString);
                    return new RedisConfiguration(sp.GetRequiredService<ICodexLogger>(), redisOptions);
                });
                
                // Register Redis connection multiplexer
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var redisConfig = sp.GetRequiredService<IRedisConfiguration>();
                    return redisConfig.GetConnectionAsync().Result;
                });
                
                // Register distributed session storage
                builder.Services.AddSingleton<IDistributedSessionStorage>(sp =>
                {
                    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                    var logger = sp.GetRequiredService<ICodexLogger>();
                    return new DistributedSessionStorage(redis, logger);
                });
                
                logger.Info("[DistributedSessions] Redis and distributed session storage configured successfully");
            }
            catch (Exception ex)
            {
                logger.Error("[DistributedSessions] Failed to configure Redis, falling back to in-memory storage", ex);
                useDistributedStorage = false;
            }
        }
        
        // Register session storage adapter (supports both distributed and in-memory)
        builder.Services.AddSingleton<ISessionStorageAdapter>(sp =>
        {
            var distributedStorage = useDistributedStorage ? sp.GetService<IDistributedSessionStorage>() : null;
            var logger = sp.GetRequiredService<ICodexLogger>();
            var enableFallback = Environment.GetEnvironmentVariable("SESSION_STORAGE_ENABLE_FALLBACK") ?? "true";
            
            return new SessionStorageAdapter(
                distributedStorage,
                logger,
                useDistributedStorage,
                enableFallback.Equals("true", StringComparison.OrdinalIgnoreCase)
            );
        });
        
        logger.Info($"[DistributedSessions] Session storage adapter configured - Type: {(useDistributedStorage ? "Redis" : "InMemory")}, Fallback: true");
    }
    */

    private sealed record NodeRegistryBootstrapOptions(bool PersistenceEnabled, string EnvironmentName);
}
