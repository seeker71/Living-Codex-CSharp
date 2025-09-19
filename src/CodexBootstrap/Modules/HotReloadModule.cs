using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Enhanced Hot Reload Module - Real-time component and spec watching with AI-driven updates
/// </summary>
[MetaNode(Id = "codex.hot-reload", Name = "Hot Reload Module", Description = "Real-time component hot-swapping and spec watching with AI-driven updates")]
public sealed class HotReloadModule : ModuleBase
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private readonly Dictionary<string, ComponentDefinition> _componentRegistry = new();
    private readonly Queue<HotReloadEvent> _reloadHistory = new();
    private readonly object _lock = new();
    private bool _isWatching = false;

    public override string Name => "Hot Reload Module";
    public override string Description => "Real-time component hot-swapping and spec watching with AI-driven updates";
    public override string Version => "1.0.0";

    public HotReloadModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.hot-reload",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "hot-reload", "file-watching", "component-swapping", "ai-driven", "real-time" },
            capabilities: new[] { 
                "file-watching", "component-hot-swap", "spec-watching", "ai-regeneration", 
                "real-time-updates", "rollback-support", "change-detection" 
            },
            spec: "codex.spec.hot-reload"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _apiRouter = router;
        
        // Register internal API handlers for cross-module communication
        router.Register("hot-reload", "start-watching", async (System.Text.Json.JsonElement? json) => 
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<WatchConfig>(json?.GetRawText() ?? "{}");
            return await StartWatching(config);
        });
        
        router.Register("hot-reload", "stop-watching", async (System.Text.Json.JsonElement? json) => 
        {
            return await StopWatching();
        });
        
        router.Register("hot-reload", "get-status", async (System.Text.Json.JsonElement? json) => 
        {
            return await GetStatus();
        });
        
        router.Register("hot-reload", "regenerate-component", async (System.Text.Json.JsonElement? json) => 
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<RegenerationRequest>(json?.GetRawText() ?? "{}");
            return await RegenerateComponent(request);
        });
        
        router.Register("hot-reload", "hot-swap-component", async (System.Text.Json.JsonElement? json) => 
        {
            var request = System.Text.Json.JsonSerializer.Deserialize<HotSwapRequest>(json?.GetRawText() ?? "{}");
            return await HotSwapComponent(request);
        });
        
        router.Register("hot-reload", "get-history", async (System.Text.Json.JsonElement? json) => 
        {
            var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json?.GetRawText() ?? "{}");
            var limit = parameters?.GetValueOrDefault("limit", 50) is System.Text.Json.JsonElement limitElement ? limitElement.GetInt32() : 50;
            return await GetHistory(limit);
        });
        
        _logger.Info("Hot Reload Module API handlers registered for cross-module communication");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attribute-based routing
        _logger.Info("Hot Reload Module HTTP endpoints registered");
    }

    // Start watching for file changes
    [ApiRoute("POST", "/hot-reload/start", "start-watching", "Start hot-reload file watching", "codex.hot-reload")]
    public async Task<object> StartWatching([ApiParameter("body", "Watch configuration", Required = true, Location = "body")] WatchConfig config)
    {
        try
        {
            if (_isWatching)
            {
                return new { success = false, error = "Already watching for changes" };
            }

            var watchPaths = config.Paths ?? new List<string> 
            { 
                "../../living-codex-ui/src/app",
                "../../living-codex-ui/src/components",
                "../../specs",
                "./Modules"
            };

            foreach (var path in watchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    var watcher = new FileSystemWatcher(fullPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName
                    };

                    // Watch for different file types
                    if (path.Contains("living-codex-ui"))
                    {
                        watcher.Filter = "*.tsx";
                        watcher.Changed += OnUIComponentChanged;
                        watcher.Created += OnUIComponentCreated;
                    }
                    else if (path.Contains("specs"))
                    {
                        watcher.Filter = "*.md";
                        watcher.Changed += OnSpecChanged;
                    }
                    else if (path.Contains("Modules"))
                    {
                        watcher.Filter = "*.cs";
                        watcher.Changed += OnModuleChanged;
                    }

                    watcher.EnableRaisingEvents = true;
                    _watchers[path] = watcher;
                    _logger.Info($"Started watching: {fullPath}");
                }
            }

            _isWatching = true;
            return new 
            { 
                success = true, 
                message = "Hot-reload watching started",
                watchedPaths = watchPaths.Count,
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error starting hot-reload watching: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Stop watching for file changes
    [ApiRoute("POST", "/hot-reload/stop", "stop-watching", "Stop hot-reload file watching", "codex.hot-reload")]
    public async Task<object> StopWatching()
    {
        try
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
            _isWatching = false;

            return new 
            { 
                success = true, 
                message = "Hot-reload watching stopped",
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error stopping hot-reload watching: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Get hot-reload status
    [ApiRoute("GET", "/hot-reload/status", "get-status", "Get hot-reload status", "codex.hot-reload")]
    public async Task<object> GetStatus()
    {
        try
        {
            return new
            {
                success = true,
                isWatching = _isWatching,
                watchedPaths = _watchers.Count,
                componentCount = _componentRegistry.Count,
                recentEvents = _reloadHistory.TakeLast(10).ToArray(),
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting hot-reload status: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Manually trigger component regeneration
    [ApiRoute("POST", "/hot-reload/regenerate", "regenerate-component", "AI-regenerate component from spec", "codex.hot-reload")]
    public async Task<object> RegenerateComponent([ApiParameter("body", "Regeneration request", Required = true, Location = "body")] RegenerationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ComponentId))
            {
                return new { success = false, error = "Component ID is required" };
            }

            // Call AI module to regenerate the component
            var aiRequest = new
            {
                componentId = request.ComponentId,
                lensSpec = request.LensSpec ?? "Enhanced component with improved UX",
                componentType = request.ComponentType ?? "list",
                requirements = request.Requirements ?? "TypeScript + Tailwind, modern design, accessibility",
                provider = request.Provider ?? "openai",
                model = request.Model ?? "gpt-5-codex"
            };

            using var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(aiRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("http://localhost:5002/ai/generate-ui-component", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (aiResponse.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    var generatedCode = aiResponse.GetProperty("data").GetProperty("generatedCode").GetString();
                    
                    // Track the regeneration event
                    var reloadEvent = new HotReloadEvent
                    {
                        Type = "component-regeneration",
                        ComponentId = request.ComponentId,
                        Timestamp = DateTime.UtcNow,
                        Success = true,
                        Details = $"AI-regenerated using {request.Provider}/{request.Model}"
                    };
                    
                    lock (_lock)
                    {
                        _reloadHistory.Enqueue(reloadEvent);
                        if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
                    }

                    return new
                    {
                        success = true,
                        componentId = request.ComponentId,
                        generatedCode = generatedCode,
                        aiProvider = request.Provider,
                        aiModel = request.Model,
                        timestamp = DateTime.UtcNow
                    };
                }
            }

            return new { success = false, error = "AI component generation failed" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error regenerating component: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Hot-swap an existing component
    [ApiRoute("POST", "/hot-reload/swap", "hot-swap-component", "Hot-swap component code", "codex.hot-reload")]
    public async Task<object> HotSwapComponent([ApiParameter("body", "Hot-swap request", Required = true, Location = "body")] HotSwapRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ComponentPath) || string.IsNullOrEmpty(request.NewCode))
            {
                return new { success = false, error = "Component path and new code are required" };
            }

            var fullPath = Path.GetFullPath(request.ComponentPath);
            if (!File.Exists(fullPath))
            {
                return new { success = false, error = "Component file not found" };
            }

            // Backup original
            var backupPath = $"{fullPath}.backup.{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            File.Copy(fullPath, backupPath);

            // Write new code
            await File.WriteAllTextAsync(fullPath, request.NewCode);

            // Track the swap event
            var reloadEvent = new HotReloadEvent
            {
                Type = "component-hot-swap",
                ComponentId = Path.GetFileNameWithoutExtension(fullPath),
                Timestamp = DateTime.UtcNow,
                Success = true,
                Details = $"Hot-swapped {fullPath}, backup: {backupPath}"
            };
            
            lock (_lock)
            {
                _reloadHistory.Enqueue(reloadEvent);
                if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
            }

            return new
            {
                success = true,
                componentPath = fullPath,
                backupPath = backupPath,
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error hot-swapping component: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Get hot-reload history
    [ApiRoute("GET", "/hot-reload/history", "get-history", "Get hot-reload event history", "codex.hot-reload")]
    public async Task<object> GetHistory([ApiParameter("limit", "Number of events to return", Required = false, Location = "query")] int limit = 50)
    {
        try
        {
            lock (_lock)
            {
                var events = _reloadHistory.TakeLast(limit).ToArray();
                return new
                {
                    success = true,
                    events = events,
                    totalEvents = _reloadHistory.Count,
                    timestamp = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting hot-reload history: {ex.Message}", ex);
            return new { success = false, error = ex.Message };
        }
    }

    // Event handlers for file system watching
    private async void OnUIComponentChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.Info($"UI component changed: {e.FullPath}");
            
            // Trigger real-time notification to UI
            await NotifyUIChange(e.FullPath, "component-changed");
            
            var reloadEvent = new HotReloadEvent
            {
                Type = "ui-component-changed",
                ComponentId = Path.GetFileNameWithoutExtension(e.FullPath),
                Timestamp = DateTime.UtcNow,
                Success = true,
                Details = $"File changed: {e.FullPath}"
            };
            
            lock (_lock)
            {
                _reloadHistory.Enqueue(reloadEvent);
                if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling UI component change: {ex.Message}", ex);
        }
    }

    private async void OnUIComponentCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.Info($"UI component created: {e.FullPath}");
            
            // Trigger real-time notification to UI
            await NotifyUIChange(e.FullPath, "component-created");
            
            var reloadEvent = new HotReloadEvent
            {
                Type = "ui-component-created",
                ComponentId = Path.GetFileNameWithoutExtension(e.FullPath),
                Timestamp = DateTime.UtcNow,
                Success = true,
                Details = $"File created: {e.FullPath}"
            };
            
            lock (_lock)
            {
                _reloadHistory.Enqueue(reloadEvent);
                if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling UI component creation: {ex.Message}", ex);
        }
    }

    private async void OnSpecChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.Info($"Spec changed: {e.FullPath}");
            
            // Auto-regenerate affected components if enabled
            if (e.Name?.EndsWith(".md") == true)
            {
                await ProcessSpecChange(e.FullPath);
            }
            
            var reloadEvent = new HotReloadEvent
            {
                Type = "spec-changed",
                ComponentId = Path.GetFileNameWithoutExtension(e.FullPath),
                Timestamp = DateTime.UtcNow,
                Success = true,
                Details = $"Spec changed: {e.FullPath}"
            };
            
            lock (_lock)
            {
                _reloadHistory.Enqueue(reloadEvent);
                if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling spec change: {ex.Message}", ex);
        }
    }

    private async void OnModuleChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _logger.Info($"Module changed: {e.FullPath}");
            
            // Trigger module hot-reload if it's not a core module
            var moduleName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (!IsStableModule(moduleName))
            {
                await TriggerModuleReload(e.FullPath);
            }
            
            var reloadEvent = new HotReloadEvent
            {
                Type = "module-changed",
                ComponentId = moduleName,
                Timestamp = DateTime.UtcNow,
                Success = true,
                Details = $"Module changed: {e.FullPath}"
            };
            
            lock (_lock)
            {
                _reloadHistory.Enqueue(reloadEvent);
                if (_reloadHistory.Count > 100) _reloadHistory.Dequeue();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling module change: {ex.Message}", ex);
        }
    }

    private async Task NotifyUIChange(string filePath, string changeType)
    {
        try
        {
            // Send real-time notification to connected UI clients
            var notification = new
            {
                type = changeType,
                filePath = filePath,
                componentId = Path.GetFileNameWithoutExtension(filePath),
                timestamp = DateTime.UtcNow
            };

            // Use RealtimeModule to broadcast if available
            // This would integrate with WebSocket/SignalR connections
            _logger.Debug($"UI change notification: {JsonSerializer.Serialize(notification)}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error notifying UI change: {ex.Message}", ex);
        }
    }

    private async Task ProcessSpecChange(string specPath)
    {
        try
        {
            _logger.Info($"Processing spec change: {specPath}");
            
            // Read the changed spec
            var specContent = await File.ReadAllTextAsync(specPath);
            
            // Extract UI requirements from spec (simplified)
            if (specContent.Contains("UI") || specContent.Contains("interface") || specContent.Contains("component"))
            {
                // Trigger AI-driven component regeneration based on spec changes
                var specName = Path.GetFileNameWithoutExtension(specPath);
                await TriggerSpecBasedRegeneration(specName, specContent);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing spec change: {ex.Message}", ex);
        }
    }

    private async Task TriggerSpecBasedRegeneration(string specName, string specContent)
    {
        try
        {
            // Call AI module to analyze spec and generate/update UI components
            var aiRequest = new
            {
                pageId = $"spec-driven-{specName}",
                uiAtom = $"Generate UI components based on this specification:\n\n{specContent.Substring(0, Math.Min(2000, specContent.Length))}",
                provider = "openai",
                model = "gpt-5-codex"
            };

            using var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(aiRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("http://localhost:5002/ai/generate-ui-page", content);
            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"AI-generated UI updates for spec: {specName}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error triggering spec-based regeneration: {ex.Message}", ex);
        }
    }

    private async Task TriggerModuleReload(string modulePath)
    {
        try
        {
            _logger.Info($"Triggering module reload: {modulePath}");
            
            // This would integrate with the existing SelfUpdateModule
            // For now, just log the event
            _logger.Info($"Module {modulePath} would be hot-reloaded here");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error triggering module reload: {ex.Message}", ex);
        }
    }

    private bool IsStableModule(string moduleName)
    {
        var stableModules = new[] 
        { 
            "Program", "ModuleLoader", "NodeRegistry", "CoreApiService", 
            "BootstrapEnvironment", "CodexBootstrapHost" 
        };
        return stableModules.Contains(moduleName);
    }
}

// Data structures for hot-reload (shared with SelfUpdateModule)

[MetaNode(Id = "codex.hot-reload.component-definition", Name = "Component Definition", Description = "Definition of a hot-reloadable component")]
public record ComponentDefinition(
    string Id,
    string Path,
    string Type,
    string LensSpec,
    DateTime LastModified,
    Dictionary<string, object> Metadata
);

[MetaNode(Id = "codex.hot-reload.event", Name = "Hot Reload Event", Description = "Hot-reload event record")]
public record HotReloadEvent
{
    public string Type { get; set; } = "";
    public string ComponentId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; } = "";
}
