using CodexBootstrap.Hosting;
using System;
using System.IO;

static void LoadDotEnvIfPresent()
{
    try
    {
        var root = AppContext.BaseDirectory;
        // Walk up to repo root if running from bin
        var probe = root;
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(probe, ".env");
            if (File.Exists(candidate))
            {
                foreach (var line in File.ReadAllLines(candidate))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                    var idx = trimmed.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = trimmed.Substring(0, idx).Trim();
                    var val = trimmed.Substring(idx + 1).Trim();
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
                break;
            }
            probe = Path.GetFullPath(Path.Combine(probe, ".."));
        }
    }
    catch { /* best effort */ }
}

BootstrapEnvironment.Initialize();

// Ensure .env variables (like OPENAI_API_KEY) are loaded when running via `dotnet run`
LoadDotEnvIfPresent();

// Debug: Check if OPENAI_API_KEY was loaded
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var bootLogger = new CodexBootstrap.Core.Log4NetLogger(typeof(Program));
bootLogger.Info($"OPENAI_API_KEY loaded: {!string.IsNullOrEmpty(apiKey)}");

var builder = WebApplication.CreateBuilder(args);
CodexBootstrapHost.ConfigureBuilder(builder, args);

var app = builder.Build();
var hostingUrl = CodexBootstrapHost.ConfigureApp(app);

bootLogger.Info($"Starting Living Codex on {hostingUrl}");

// In Testing environment, respect ASPNETCORE_URLS/--urls provided by the test harness
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
{
    app.Run();
}
else
{
    // Fast startup: Initialize only essential services synchronously
    try
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var registry = services.GetService<CodexBootstrap.Core.INodeRegistry>();
        CodexBootstrap.Core.ICodexLogger logger = services.GetService<CodexBootstrap.Core.ICodexLogger>()
            ?? new CodexBootstrap.Core.Log4NetLogger(typeof(Program));

        if (registry != null)
        {
            // ESSENTIAL: Initialize core identity module synchronously (fast)
            try
            {
                var coreIdentity = new CodexBootstrap.Modules.CoreIdentityModule(registry, logger);
                coreIdentity.Register(registry);
                bootLogger.Info("[Startup] Core Identity Module initialized - core identity node ensured");
            }
            catch (Exception ex)
            {
                bootLogger.Warn($"[Startup] Core Identity Module initialization failed: {ex.Message}");
            }

            // BACKGROUND: Start expensive initialization tasks asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    bootLogger.Info("[Background] Starting expensive initialization tasks...");
                    var startTime = DateTime.UtcNow;

                    // Build reflection tree for current AppDomain assemblies
                    var reflection = new CodexBootstrap.Modules.ReflectionTreeModule(registry, logger);
                    var buildTask = reflection.BuildCompleteReflectionTreeAsync();
                    await (buildTask as System.Threading.Tasks.Task<object>);
                    bootLogger.Info($"[Background] Reflection tree built in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");

                    // Ensure edges across the graph (meta-node links, shared metadata, U-CORE)
                    try
                    {
                        var edgeEnsurance = new CodexBootstrap.Modules.EdgeEnsuranceModule(registry, logger);
                        var ensureTask = edgeEnsurance.EnsureAllEdgesAsync();
                        await (ensureTask as System.Threading.Tasks.Task<object>);
                        bootLogger.Info($"[Background] Edge ensurance completed in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
                    }
                    catch (Exception ex)
                    {
                        bootLogger.Warn($"[Background] EdgeEnsurance failed: {ex.Message}");
                    }

                    bootLogger.Info($"[Background] All initialization tasks completed in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    bootLogger.Error($"[Background] Background initialization failed: {ex.Message}");
                    logger.Error($"Background initialization failed: {ex.Message}", ex);
                }
            });
        }
    }
    catch (Exception ex)
    {
        bootLogger.Error($"[Startup] Essential initialization failed: {ex.Message}");
    }

    // Mark startup as complete and AI as ready
    try
    {
        using var scope = app.Services.CreateScope();
        var startupState = scope.ServiceProvider.GetService<CodexBootstrap.Core.StartupStateService>();
        if (startupState != null)
        {
            startupState.MarkStartupComplete();
            startupState.MarkAIReady();
            bootLogger.Info("[Startup] Startup state marked as complete, AI services ready");
        }
    }
    catch (Exception ex)
    {
        bootLogger.Warn($"[Startup] Failed to mark startup complete: {ex.Message}");
    }

    bootLogger.Info("[Startup] About to start HTTP server...");
    app.Run();
    bootLogger.Info("[Startup] HTTP server started successfully!");
}
