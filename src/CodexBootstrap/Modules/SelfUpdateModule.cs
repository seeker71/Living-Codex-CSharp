using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Module that provides self-updating functionality via API endpoints
    /// </summary>
    [ApiModule(Name = "SelfUpdateModule", Version = "1.0.0", Description = "Provides self-updating functionality for dynamic modules", Tags = new[] { "self-update", "hot-reload", "modules" })]
    public class SelfUpdateModule : ModuleBase
    {
        private readonly StableCore? _stableCore;
#pragma warning disable CS0414 // Field assigned but never used - temporary until SelfUpdateSystem is implemented
        private readonly SelfUpdateSystem? _selfUpdateSystem;
#pragma warning restore CS0414
        private readonly ModuleCompiler? _moduleCompiler;
        private readonly HotReloadManager? _hotReloadManager;

        public override string Name => "Self Update Module";
        public override string Description => "Provides self-updating functionality for dynamic modules";
        public override string Version => "1.0.0";

        public SelfUpdateModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
            // Initialize to null - these complex dependencies would need proper DI setup
            _moduleCompiler = null;
            _hotReloadManager = null;
            _selfUpdateSystem = null;
            _stableCore = null;
        }

        /// <summary>
        /// Gets the module node for this module
        /// </summary>
        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "self-update-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "self-update", "hot-reload", "modules" },
                capabilities: new[] { "Hot Reload", "Module Compilation", "Dynamic Loading", "Rollback" },
                spec: "codex.spec.self-update"
            );
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            // API handlers are registered via attributes, no additional registration needed
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // HTTP endpoints are registered via attributes, no additional registration needed
        }

        /// <summary>
        /// Gets the system status
        /// </summary>
        [Get("/self-update/status", "Get System Status", "Get the current status of the self-updating system", "self-update")]
        public async Task<object> GetSystemStatusAsync()
        {
            try
            {
                if (_stableCore == null || _logger == null)
                {
                    return new { success = false, error = "Self-update system not initialized - dependencies not injected. Please check dependency injection configuration." };
                }

                var moduleStatus = _stableCore.GetModuleStatus();
                var systemHealth = _stableCore.GetSystemHealth();

                return new
                {
                    success = true,
                    message = "System status retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    core = new
                    {
                        version = moduleStatus.CoreVersion,
                        totalModules = moduleStatus.TotalModules,
                        lastUpdated = moduleStatus.LastUpdated
                    },
                    health = new
                    {
                        isHealthy = systemHealth.IsHealthy,
                        coreModules = systemHealth.CoreModules,
                        dynamicModules = systemHealth.DynamicModules,
                        issues = systemHealth.Issues,
                        lastChecked = systemHealth.LastChecked
                    },
                    modules = new
                    {
                        core = moduleStatus.CoreModules,
                        dynamic = moduleStatus.DynamicModules
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting system status", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Updates a module from source code
        /// </summary>
        [Post("/self-update/update-module", "Update Module", "Update a module from source code", "self-update")]
        public async Task<object> UpdateModuleAsync([ApiParameter("body", "Update module request")] UpdateModuleRequest request)
        {
            try
            {
                if (_stableCore == null || _logger == null)
                {
                    return new { success = false, error = "Self-update system not initialized - dependencies not injected" };
                }

                if (string.IsNullOrEmpty(request.ModuleName))
                {
                    return new { success = false, error = "Module name is required" };
                }

                if (string.IsNullOrEmpty(request.SourceCode))
                {
                    return new { success = false, error = "Source code is required" };
                }

                // Check if module is stable (core module)
                if (_stableCore.IsModuleStable(request.ModuleName))
                {
                    return new { success = false, error = "Cannot update stable core modules" };
                }

                _logger.Info($"Updating module {request.ModuleName} from source code");

                var updateResult = await _stableCore.UpdateModuleAsync(request.ModuleName, request.SourceCode);
                
                if (updateResult.Success)
                {
                    return new
                    {
                        success = true,
                        message = "Module updated successfully",
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName,
                        dllPath = updateResult.DllPath
                    };
                }
                else
                {
                    return new
                    {
                        success = false,
                        error = updateResult.ErrorMessage,
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating module {request.ModuleName}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Compiles a module from source code
        /// </summary>
        [Post("/self-update/compile-module", "Compile Module", "Compile a module from source code", "self-update")]
        public async Task<object> CompileModuleAsync([ApiParameter("body", "Compile module request")] CompileModuleRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ModuleName))
                {
                    return new { success = false, error = "Module name is required" };
                }

                if (string.IsNullOrEmpty(request.SourceCode))
                {
                    return new { success = false, error = "Source code is required" };
                }

                _logger.Info($"Compiling module {request.ModuleName}");

                var compilationResult = await _moduleCompiler.CompileModuleAsync(request.ModuleName, request.SourceCode);
                
                if (compilationResult.Success)
                {
                    return new
                    {
                        success = true,
                        message = "Module compiled successfully",
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName,
                        dllPath = compilationResult.DllPath
                    };
                }
                else
                {
                    return new
                    {
                        success = false,
                        error = compilationResult.ErrorMessage,
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error compiling module {request.ModuleName}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Validates a compiled module
        /// </summary>
        [Post("/self-update/validate-module", "Validate Module", "Validate a compiled module", "self-update")]
        public async Task<object> ValidateModuleAsync([ApiParameter("body", "Validate module request")] ValidateModuleRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DllPath))
                {
                    return new { success = false, error = "DLL path is required" };
                }

                if (!File.Exists(request.DllPath))
                {
                    return new { success = false, error = "DLL file not found" };
                }

                _logger.Info($"Validating module {request.DllPath}");

                var validationResult = await _moduleCompiler.ValidateCompiledModuleAsync(request.DllPath);
                
                if (validationResult.Success)
                {
                    return new
                    {
                        success = true,
                        message = "Module validation successful",
                        timestamp = DateTime.UtcNow,
                        dllPath = request.DllPath
                    };
                }
                else
                {
                    return new
                    {
                        success = false,
                        error = validationResult.ErrorMessage,
                        timestamp = DateTime.UtcNow,
                        dllPath = request.DllPath
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error validating module {request.DllPath}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        // File watching and hot-reload endpoints moved from HotReloadModule
        
        /// <summary>
        /// Start hot-reload file watching
        /// </summary>
        [ApiRoute("POST", "/self-update/start-watching", "self-update-start-watching", "Start hot-reload file watching", "self-update")]
        public async Task<object> StartWatching([ApiParameter("body", "Watch configuration", Required = true, Location = "body")] WatchConfig config)
        {
            try
            {
                // Use _apiRouter for cross-module communication with HotReloadModule
                if (_apiRouter != null)
                {
                    var result = await CallModuleMethod("hot-reload", "start-watching", System.Text.Json.JsonSerializer.SerializeToElement(config));
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error starting file watching: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Stop hot-reload file watching
        /// </summary>
        [ApiRoute("POST", "/self-update/stop-watching", "self-update-stop-watching", "Stop hot-reload file watching", "self-update")]
        public async Task<object> StopWatching()
        {
            try
            {
                if (_apiRouter != null)
                {
                    var result = await CallModuleMethod("hot-reload", "stop-watching", null);
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error stopping file watching: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Get hot-reload status
        /// </summary>
        [ApiRoute("GET", "/self-update/hot-reload-status", "self-update-get-hot-reload-status", "Get hot-reload status", "self-update")]
        public async Task<object> GetHotReloadStatus()
        {
            try
            {
                if (_apiRouter != null)
                {
                    var result = await CallModuleMethod("hot-reload", "get-status", null);
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting hot-reload status: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// AI-regenerate component
        /// </summary>
        [ApiRoute("POST", "/self-update/regenerate-component", "self-update-regenerate-component", "AI-regenerate component from spec", "self-update")]
        public async Task<object> RegenerateComponent([ApiParameter("body", "Regeneration request", Required = true, Location = "body")] RegenerationRequest request)
        {
            try
            {
                if (_apiRouter != null)
                {
                    var result = await CallModuleMethod("hot-reload", "regenerate-component", System.Text.Json.JsonSerializer.SerializeToElement(request));
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error regenerating component: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Hot-swap component code
        /// </summary>
        [ApiRoute("POST", "/self-update/hot-swap", "self-update-hot-swap-component", "Hot-swap component code", "self-update")]
        public async Task<object> HotSwapComponent([ApiParameter("body", "Hot-swap request", Required = true, Location = "body")] HotSwapRequest request)
        {
            try
            {
                if (_apiRouter != null)
                {
                    var result = await CallModuleMethod("hot-reload", "hot-swap-component", System.Text.Json.JsonSerializer.SerializeToElement(request));
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error hot-swapping component: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Get hot-reload history
        /// </summary>
        [ApiRoute("GET", "/self-update/hot-reload-history", "self-update-get-hot-reload-history", "Get hot-reload event history", "self-update")]
        public async Task<object> GetHotReloadHistory([ApiParameter("limit", "Number of events to return", Required = false, Location = "query")] int limit = 50)
        {
            try
            {
                if (_apiRouter != null)
                {
                    var parameters = new { limit };
                    var result = await CallModuleMethod("hot-reload", "get-history", System.Text.Json.JsonSerializer.SerializeToElement(parameters));
                    return result ?? new { success = false, error = "No response from hot-reload module" };
                }
                return new { success = false, error = "API router not available" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting hot-reload history: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Hot reloads a module
        /// </summary>
        [ApiRoute("POST", "/self-update/hot-reload", "hot-reload-module", "Hot reload a module with backup and rollback", "self-update")]
        public async Task<IResult> HotReloadModuleAsync([ApiParameter("body", "Hot reload request")] HotReloadRequest request)
        {
            try
            {
                if (_stableCore == null || _logger == null || _hotReloadManager == null)
                {
                    return Results.Json(new { success = false, error = "Self-update system not initialized - dependencies not injected" }, statusCode: 503);
                }

                if (string.IsNullOrEmpty(request.ModuleName))
                {
                    return Results.BadRequest(new { success = false, error = "Module name is required" });
                }

                if (string.IsNullOrEmpty(request.DllPath))
                {
                    return Results.BadRequest(new { success = false, error = "DLL path is required" });
                }

                if (!File.Exists(request.DllPath))
                {
                    return Results.NotFound(new { success = false, error = "DLL file not found" });
                }

                // Check if module is stable (core module)
                if (_stableCore.IsModuleStable(request.ModuleName))
                {
                    return Results.Json(new { success = false, error = "Cannot hot reload stable core modules" }, statusCode: 403);
                }

                _logger.Info($"Hot reloading module {request.ModuleName} from {request.DllPath}");

                var hotReloadResult = await _hotReloadManager.HotReloadModuleAsync(request.ModuleName, request.DllPath);
                
                if (hotReloadResult.Success)
                {
                    return Results.Ok(new
                    {
                        success = true,
                        message = "Module hot reloaded successfully",
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName,
                        dllPath = hotReloadResult.DllPath
                    });
                }
                else
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = hotReloadResult.ErrorMessage,
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName
                    }, statusCode: 500);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error hot reloading module {request.ModuleName}", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Gets module backups
        /// </summary>
        [Get("/self-update/backups", "Get Module Backups", "Get all module backups", "self-update")]
        public async Task<IResult> GetModuleBackupsAsync()
        {
            try
            {
                if (_hotReloadManager == null)
                {
                    return Results.Json(new { success = false, error = "Self-update system not initialized - dependencies not injected" }, statusCode: 503);
                }

                var backups = _hotReloadManager.GetAllBackups();
                var backupList = backups.Values.Select(b => new
                {
                    moduleName = b.ModuleName,
                    backupPath = b.BackupPath,
                    originalPath = b.OriginalPath,
                    createdAt = b.CreatedAt,
                    isEmpty = b.IsEmpty
                }).ToList();

                return Results.Ok(new
                {
                    success = true,
                    message = "Module backups retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    backups = backupList,
                    totalCount = backupList.Count
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting module backups", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Gets core modules
        /// </summary>
        [Get("/self-update/core-modules", "Get Core Modules", "Get all core modules", "self-update")]
        public async Task<object> GetCoreModulesAsync()
        {
            try
            {
                if (_stableCore == null)
                {
                    return new { success = false, error = "Self-update system not initialized - dependencies not injected" };
                }

                var coreModules = _stableCore.GetCoreModules();
                var moduleList = coreModules.Values.Select(m => new
                {
                    name = m.Name,
                    version = m.Version,
                    isStable = m.IsStable,
                    loadedAt = m.LoadedAt,
                    type = m.Type?.Name
                }).ToList();

                return new
                {
                    success = true,
                    message = "Core modules retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    modules = moduleList,
                    totalCount = moduleList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting core modules", ex);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets dynamic modules
        /// </summary>
        [Get("/self-update/dynamic-modules", "Get Dynamic Modules", "Get all dynamic modules", "self-update")]
        public async Task<object> GetDynamicModulesAsync()
        {
            try
            {
                var dynamicModules = _stableCore.GetDynamicModules();
                var moduleList = dynamicModules.Values.Select(m => new
                {
                    name = m.Name,
                    version = m.Version,
                    dllPath = m.DllPath,
                    isStable = m.IsStable,
                    loadedAt = m.LoadedAt
                }).ToList();

                return new
                {
                    success = true,
                    message = "Dynamic modules retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    modules = moduleList,
                    totalCount = moduleList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting dynamic modules", ex);
                return new { success = false, error = ex.Message };
            }
        }

        // Helper method for cross-module communication
        private async Task<object?> CallModuleMethod(string moduleId, string method, System.Text.Json.JsonElement? parameters = null)
        {
            if (_apiRouter != null && _apiRouter.TryGetHandler(moduleId, method, out var handler))
            {
                return await handler(parameters);
            }
            return null;
        }
    }

    /// <summary>
    /// Request to update a module
    /// </summary>
    public class UpdateModuleRequest
    {
        public string ModuleName { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to compile a module
    /// </summary>
    public class CompileModuleRequest
    {
        public string ModuleName { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to validate a module
    /// </summary>
    public class ValidateModuleRequest
    {
        public string DllPath { get; set; } = string.Empty;
    }

/// <summary>
/// Request to hot reload a module
/// </summary>
public class HotReloadRequest
{
    public string ModuleName { get; set; } = string.Empty;
    public string DllPath { get; set; } = string.Empty;
}

// Hot-reload data structures
[MetaNode(Id = "codex.self-update.watch-config", Name = "Watch Config", Description = "Configuration for file watching")]
public record WatchConfig(
    List<string>? Paths = null,
    List<string>? Extensions = null,
    bool AutoRegenerate = true,
    string? Provider = "openai",
    string? Model = "gpt-5-codex"
);

[MetaNode(Id = "codex.self-update.regeneration-request", Name = "Regeneration Request", Description = "Request to regenerate component")]
public record RegenerationRequest(
    string ComponentId,
    string? LensSpec = null,
    string? ComponentType = null,
    string? Requirements = null,
    string? Provider = "openai",
    string? Model = "gpt-5-codex"
);

[MetaNode(Id = "codex.self-update.hot-swap-request", Name = "Hot Swap Request", Description = "Request to hot-swap component")]
public record HotSwapRequest(
    string ComponentPath,
    string NewCode,
    bool CreateBackup = true
);
}
