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
Console.WriteLine($"OPENAI_API_KEY loaded: {!string.IsNullOrEmpty(apiKey)}");

var builder = WebApplication.CreateBuilder(args);
CodexBootstrapHost.ConfigureBuilder(builder, args);

var app = builder.Build();
var hostingUrl = CodexBootstrapHost.ConfigureApp(app);

Console.WriteLine($"Starting Living Codex on {hostingUrl}");

// In Testing environment, respect ASPNETCORE_URLS/--urls provided by the test harness
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
{
    app.Run();
}
else
{
    try
    {
        // Startup reflection: generate app/meta nodes for loaded assemblies
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var registry = services.GetService<CodexBootstrap.Core.INodeRegistry>();
            CodexBootstrap.Core.ICodexLogger logger = services.GetService<CodexBootstrap.Core.ICodexLogger>()
                ?? new CodexBootstrap.Core.Log4NetLogger(typeof(Program));

            if (registry != null)
            {
                // FIRST: Initialize core identity module to ensure core identity node exists
                try
                {
                    var coreIdentity = new CodexBootstrap.Modules.CoreIdentityModule(registry, logger);
                    coreIdentity.Register(registry);
                    Console.WriteLine("[Startup] Core Identity Module initialized - core identity node ensured");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Startup] Core Identity Module initialization failed: {ex.Message}");
                }

                // SECOND: Build reflection tree for current AppDomain assemblies
                var reflection = new CodexBootstrap.Modules.ReflectionTreeModule(registry, logger);
                var buildTask = reflection.BuildCompleteReflectionTreeAsync();
                (buildTask as System.Threading.Tasks.Task<object>)?.GetAwaiter().GetResult();

                // THIRD: Ensure edges across the graph (meta-node links, shared metadata, U-CORE)
                try
                {
                    var edgeEnsurance = new CodexBootstrap.Modules.EdgeEnsuranceModule(registry, logger);
                    var ensureTask = edgeEnsurance.EnsureAllEdgesAsync();
                    (ensureTask as System.Threading.Tasks.Task<object>)?.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Startup] EdgeEnsurance failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Startup] Reflection graph initialization failed: {ex.Message}");
        }

        app.Run(hostingUrl);
    }
    catch
    {
        app.Run(hostingUrl);
    }
}
