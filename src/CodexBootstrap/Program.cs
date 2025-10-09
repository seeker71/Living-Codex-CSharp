using CodexBootstrap.Hosting;
using CodexBootstrap.Core;
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
    // Even in Testing mode, we need to initialize modules for proper functionality
    bootLogger.Info("[Testing] Running in Testing environment - initializing modules...");
    InitializeModulesInTesting(app, bootLogger);
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

        // All modules (including CoreIdentity, ReflectionTree, EdgeEnsurance) are now loaded
        // generically through ModuleLoader - no special casing needed
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

/// <summary>
/// Initialize modules in Testing environment to ensure proper functionality
/// </summary>
static void InitializeModulesInTesting(WebApplication app, CodexBootstrap.Core.ICodexLogger bootLogger)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var registry = services.GetService<CodexBootstrap.Core.INodeRegistry>();
        CodexBootstrap.Core.ICodexLogger logger = services.GetService<CodexBootstrap.Core.ICodexLogger>()
            ?? new CodexBootstrap.Core.Log4NetLogger(typeof(Program));

        // All modules loaded generically through ModuleLoader
    }
    catch (Exception ex)
    {
        bootLogger.Error($"[Testing] Module initialization failed: {ex.Message}");
    }
}
