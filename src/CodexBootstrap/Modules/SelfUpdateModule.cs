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
    public class SelfUpdateModule : IModule
    {
        private readonly ILogger<SelfUpdateModule> _logger;
        private readonly StableCore _stableCore;
        private readonly SelfUpdateSystem _selfUpdateSystem;
        private readonly ModuleCompiler _moduleCompiler;
        private readonly HotReloadManager _hotReloadManager;

        public SelfUpdateModule()
        {
            // Parameterless constructor for module loading
            _logger = null!;
            _stableCore = null!;
            _selfUpdateSystem = null!;
            _moduleCompiler = null!;
            _hotReloadManager = null!;
        }

        public SelfUpdateModule(
            ILogger<SelfUpdateModule> logger,
            StableCore stableCore,
            SelfUpdateSystem selfUpdateSystem,
            ModuleCompiler moduleCompiler,
            HotReloadManager hotReloadManager)
        {
            _logger = logger;
            _stableCore = stableCore;
            _selfUpdateSystem = selfUpdateSystem;
            _moduleCompiler = moduleCompiler;
            _hotReloadManager = hotReloadManager;
        }

        /// <summary>
        /// Gets the module node for this module
        /// </summary>
        public Node GetModuleNode()
        {
            return new Node(
                Id: "self-update-module",
                TypeId: "module",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Self Update Module",
                Description: "Provides self-updating functionality for dynamic modules",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new { 
                        id = "self-update-module", 
                        name = "SelfUpdateModule", 
                        version = "1.0.0", 
                        description = "Provides self-updating functionality for dynamic modules" 
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = "SelfUpdateModule",
                    ["version"] = "1.0.0",
                    ["description"] = "Provides self-updating functionality for dynamic modules",
                    ["tags"] = new[] { "self-update", "hot-reload", "modules" }
                }
            );
        }

        /// <summary>
        /// Registers the module with the node registry
        /// </summary>
        public void Register(NodeRegistry registry)
        {
            var moduleNode = GetModuleNode();
            registry.Upsert(moduleNode);
        }

        /// <summary>
        /// Registers API handlers
        /// </summary>
        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via attributes, no additional registration needed
        }

        /// <summary>
        /// Registers HTTP endpoints
        /// </summary>
        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
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
                if (_stableCore == null)
                {
                    return new { success = false, error = "Self-update system not initialized - dependencies not injected" };
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
                _logger.LogError(ex, "Error getting system status");
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

                _logger.LogInformation("Updating module {ModuleName} from source code", request.ModuleName);

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
                _logger.LogError(ex, "Error updating module {ModuleName}", request.ModuleName);
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

                _logger.LogInformation("Compiling module {ModuleName}", request.ModuleName);

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
                _logger.LogError(ex, "Error compiling module {ModuleName}", request.ModuleName);
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

                _logger.LogInformation("Validating module {DllPath}", request.DllPath);

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
                _logger.LogError(ex, "Error validating module {DllPath}", request.DllPath);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Hot reloads a module
        /// </summary>
        [Post("/self-update/hot-reload", "Hot Reload Module", "Hot reload a module with backup and rollback", "self-update")]
        public async Task<object> HotReloadModuleAsync([ApiParameter("body", "Hot reload request")] HotReloadRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ModuleName))
                {
                    return new { success = false, error = "Module name is required" };
                }

                if (string.IsNullOrEmpty(request.DllPath))
                {
                    return new { success = false, error = "DLL path is required" };
                }

                if (!File.Exists(request.DllPath))
                {
                    return new { success = false, error = "DLL file not found" };
                }

                // Check if module is stable (core module)
                if (_stableCore.IsModuleStable(request.ModuleName))
                {
                    return new { success = false, error = "Cannot hot reload stable core modules" };
                }

                _logger.LogInformation("Hot reloading module {ModuleName} from {DllPath}", request.ModuleName, request.DllPath);

                var hotReloadResult = await _hotReloadManager.HotReloadModuleAsync(request.ModuleName, request.DllPath);
                
                if (hotReloadResult.Success)
                {
                    return new
                    {
                        success = true,
                        message = "Module hot reloaded successfully",
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName,
                        dllPath = hotReloadResult.DllPath
                    };
                }
                else
                {
                    return new
                    {
                        success = false,
                        error = hotReloadResult.ErrorMessage,
                        timestamp = DateTime.UtcNow,
                        moduleName = request.ModuleName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hot reloading module {ModuleName}", request.ModuleName);
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets module backups
        /// </summary>
        [Get("/self-update/backups", "Get Module Backups", "Get all module backups", "self-update")]
        public async Task<object> GetModuleBackupsAsync()
        {
            try
            {
                var backups = _hotReloadManager.GetAllBackups();
                var backupList = backups.Values.Select(b => new
                {
                    moduleName = b.ModuleName,
                    backupPath = b.BackupPath,
                    originalPath = b.OriginalPath,
                    createdAt = b.CreatedAt,
                    isEmpty = b.IsEmpty
                }).ToList();

                return new
                {
                    success = true,
                    message = "Module backups retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    backups = backupList,
                    totalCount = backupList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting module backups");
                return new { success = false, error = ex.Message };
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
                _logger.LogError(ex, "Error getting core modules");
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
                _logger.LogError(ex, "Error getting dynamic modules");
                return new { success = false, error = ex.Message };
            }
        }
    }

    /// <summary>
    /// Request to update a module
    /// </summary>
    public class UpdateModuleRequest
    {
        public string ModuleName { get; set; }
        public string SourceCode { get; set; }
    }

    /// <summary>
    /// Request to compile a module
    /// </summary>
    public class CompileModuleRequest
    {
        public string ModuleName { get; set; }
        public string SourceCode { get; set; }
    }

    /// <summary>
    /// Request to validate a module
    /// </summary>
    public class ValidateModuleRequest
    {
        public string DllPath { get; set; }
    }

    /// <summary>
    /// Request to hot reload a module
    /// </summary>
    public class HotReloadRequest
    {
        public string ModuleName { get; set; }
        public string DllPath { get; set; }
    }
}
