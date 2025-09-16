using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Stable core system that manages self-updating modules
    /// </summary>
    public class StableCore
    {
        private readonly ILogger<StableCore> _logger;
        private readonly SelfUpdateSystem _selfUpdateSystem;
        private readonly ModuleCompiler _moduleCompiler;
        private readonly HotReloadManager _hotReloadManager;
        private readonly INodeRegistry _nodeRegistry;
        private readonly ModuleLoader _moduleLoader;
        private readonly string _coreVersion;
        private readonly Dictionary<string, CoreModule> _coreModules;
        private readonly Dictionary<string, DynamicModule> _dynamicModules;

        public StableCore(
            ILogger<StableCore> logger,
            SelfUpdateSystem selfUpdateSystem,
            ModuleCompiler moduleCompiler,
            HotReloadManager hotReloadManager,
            INodeRegistry nodeRegistry,
            ModuleLoader moduleLoader)
        {
            _logger = logger;
            _selfUpdateSystem = selfUpdateSystem;
            _moduleCompiler = moduleCompiler;
            _hotReloadManager = hotReloadManager;
            _nodeRegistry = nodeRegistry;
            _moduleLoader = moduleLoader;
            _coreVersion = GetCoreVersion();
            _coreModules = new Dictionary<string, CoreModule>();
            _dynamicModules = new Dictionary<string, DynamicModule>();

            InitializeCore();
        }

        /// <summary>
        /// Initializes the stable core system
        /// </summary>
        private void InitializeCore()
        {
            try
            {
                _logger.LogInformation("Initializing Stable Core v{Version}", _coreVersion);

                // Register core modules (these are never hot-reloaded)
                RegisterCoreModules();

                // Load existing dynamic modules
                LoadExistingDynamicModules();

                _logger.LogInformation("Stable Core initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Stable Core");
                throw;
            }
        }

        /// <summary>
        /// Registers core modules that are part of the stable core
        /// </summary>
        private void RegisterCoreModules()
        {
            var coreModuleTypes = new[]
            {
                typeof(NodeRegistry),
                typeof(ModuleLoader),
                typeof(SelfUpdateSystem),
                typeof(ModuleCompiler),
                typeof(HotReloadManager),
                typeof(StableCore)
            };

            foreach (var moduleType in coreModuleTypes)
            {
                var coreModule = new CoreModule
                {
                    Name = moduleType.Name,
                    Type = moduleType,
                    Version = _coreVersion,
                    IsStable = true,
                    LoadedAt = DateTime.UtcNow
                };

                _coreModules[coreModule.Name] = coreModule;
                _logger.LogDebug("Registered core module: {ModuleName}", coreModule.Name);
            }

            _logger.LogInformation("Registered {Count} core modules", _coreModules.Count);
        }

        /// <summary>
        /// Loads existing dynamic modules
        /// </summary>
        private void LoadExistingDynamicModules()
        {
            try
            {
                var compiledModulesPath = "./modules/compiled";
                if (!Directory.Exists(compiledModulesPath))
                {
                    _logger.LogInformation("No compiled modules directory found, skipping dynamic module loading");
                    return;
                }

                var dllFiles = Directory.GetFiles(compiledModulesPath, "*.dll");
                foreach (var dllFile in dllFiles)
                {
                    var moduleName = Path.GetFileNameWithoutExtension(dllFile);
                    LoadDynamicModule(moduleName, dllFile);
                }

                _logger.LogInformation("Loaded {Count} existing dynamic modules", _dynamicModules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading existing dynamic modules");
            }
        }

        /// <summary>
        /// Loads a dynamic module
        /// </summary>
        private void LoadDynamicModule(string moduleName, string dllPath)
        {
            try
            {
                var dynamicModule = new DynamicModule
                {
                    Name = moduleName,
                    DllPath = dllPath,
                    Version = GetModuleVersion(dllPath),
                    LoadedAt = DateTime.UtcNow,
                    IsStable = false
                };

                _dynamicModules[moduleName] = dynamicModule;
                _logger.LogDebug("Loaded dynamic module: {ModuleName} v{Version}", moduleName, dynamicModule.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dynamic module {ModuleName}", moduleName);
            }
        }

        /// <summary>
        /// Updates a module from source code
        /// </summary>
        public async Task<UpdateResult> UpdateModuleAsync(string moduleName, string sourceCode)
        {
            try
            {
                _logger.LogInformation("Updating module: {ModuleName}", moduleName);

                // Step 1: Compile the module
                var compilationResult = await _moduleCompiler.CompileModuleAsync(moduleName, sourceCode);
                if (!compilationResult.Success)
                {
                    return new UpdateResult(false, $"Compilation failed: {compilationResult.ErrorMessage}", null);
                }

                // Step 2: Validate the compiled module
                var validationResult = await _moduleCompiler.ValidateCompiledModuleAsync(compilationResult.DllPath);
                if (!validationResult.Success)
                {
                    return new UpdateResult(false, $"Validation failed: {validationResult.ErrorMessage}", null);
                }

                // Step 3: Hot reload the module
                var hotReloadResult = await _hotReloadManager.HotReloadModuleAsync(moduleName, compilationResult.DllPath);
                if (!hotReloadResult.Success)
                {
                    return new UpdateResult(false, $"Hot reload failed: {hotReloadResult.ErrorMessage}", null);
                }

                // Step 4: Update dynamic module registry
                UpdateDynamicModuleRegistry(moduleName, compilationResult.DllPath);

                _logger.LogInformation("Successfully updated module: {ModuleName}", moduleName);
                return new UpdateResult(true, "Module updated successfully", compilationResult.DllPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating module {ModuleName}", moduleName);
                return new UpdateResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Updates the dynamic module registry
        /// </summary>
        private void UpdateDynamicModuleRegistry(string moduleName, string dllPath)
        {
            var dynamicModule = new DynamicModule
            {
                Name = moduleName,
                DllPath = dllPath,
                Version = GetModuleVersion(dllPath),
                LoadedAt = DateTime.UtcNow,
                IsStable = false
            };

            _dynamicModules[moduleName] = dynamicModule;
        }

        /// <summary>
        /// Gets the status of all modules
        /// </summary>
        public ModuleStatus GetModuleStatus()
        {
            var coreModules = _coreModules.Values.Select(m => new ModuleInfo
            {
                Name = m.Name,
                Version = m.Version,
                Type = "Core",
                IsStable = m.IsStable,
                LoadedAt = m.LoadedAt,
                Status = "Active"
            }).ToList();

            var dynamicModules = _dynamicModules.Values.Select(m => new ModuleInfo
            {
                Name = m.Name,
                Version = m.Version,
                Type = "Dynamic",
                IsStable = m.IsStable,
                LoadedAt = m.LoadedAt,
                Status = "Active"
            }).ToList();

            return new ModuleStatus
            {
                CoreVersion = _coreVersion,
                TotalModules = coreModules.Count + dynamicModules.Count,
                CoreModules = coreModules,
                DynamicModules = dynamicModules,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets the core version
        /// </summary>
        private string GetCoreVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Gets the module version from DLL
        /// </summary>
        private string GetModuleVersion(string dllPath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Gets all core modules
        /// </summary>
        public Dictionary<string, CoreModule> GetCoreModules()
        {
            return new Dictionary<string, CoreModule>(_coreModules);
        }

        /// <summary>
        /// Gets all dynamic modules
        /// </summary>
        public Dictionary<string, DynamicModule> GetDynamicModules()
        {
            return new Dictionary<string, DynamicModule>(_dynamicModules);
        }

        /// <summary>
        /// Gets a specific core module
        /// </summary>
        public CoreModule GetCoreModule(string moduleName)
        {
            return _coreModules.TryGetValue(moduleName, out var module) ? module : null;
        }

        /// <summary>
        /// Gets a specific dynamic module
        /// </summary>
        public DynamicModule GetDynamicModule(string moduleName)
        {
            return _dynamicModules.TryGetValue(moduleName, out var module) ? module : null;
        }

        /// <summary>
        /// Checks if a module is stable (core module)
        /// </summary>
        public bool IsModuleStable(string moduleName)
        {
            return _coreModules.ContainsKey(moduleName);
        }

        /// <summary>
        /// Gets the system health status
        /// </summary>
        public SystemHealth GetSystemHealth()
        {
            var coreModuleCount = _coreModules.Count;
            var dynamicModuleCount = _dynamicModules.Count;
            var totalModules = coreModuleCount + dynamicModuleCount;

            var health = new SystemHealth
            {
                CoreVersion = _coreVersion,
                TotalModules = totalModules,
                CoreModules = coreModuleCount,
                DynamicModules = dynamicModuleCount,
                IsHealthy = true,
                LastChecked = DateTime.UtcNow,
                Issues = new List<string>()
            };

            // Check for issues
            if (coreModuleCount == 0)
            {
                health.IsHealthy = false;
                health.Issues.Add("No core modules loaded");
            }

            if (dynamicModuleCount == 0)
            {
                health.Issues.Add("No dynamic modules loaded");
            }

            return health;
        }
    }

    /// <summary>
    /// Represents a core module
    /// </summary>
    public class CoreModule
    {
        public string Name { get; set; } = string.Empty;
        public Type Type { get; set; } = typeof(object);
        public string Version { get; set; } = string.Empty;
        public bool IsStable { get; set; }
        public DateTime LoadedAt { get; set; }
    }

    /// <summary>
    /// Represents a dynamic module
    /// </summary>
    public class DynamicModule
    {
        public string Name { get; set; } = string.Empty;
        public string DllPath { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
        public bool IsStable { get; set; }
    }

    /// <summary>
    /// Represents module status information
    /// </summary>
    public class ModuleStatus
    {
        public string CoreVersion { get; set; } = string.Empty;
        public int TotalModules { get; set; }
        public List<ModuleInfo> CoreModules { get; set; } = new();
        public List<ModuleInfo> DynamicModules { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents module information
    /// </summary>
    public class ModuleInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsStable { get; set; }
        public DateTime LoadedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents system health
    /// </summary>
    public class SystemHealth
    {
        public string CoreVersion { get; set; } = string.Empty;
        public int TotalModules { get; set; }
        public int CoreModules { get; set; }
        public int DynamicModules { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime LastChecked { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// Result of an update operation
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public string DllPath { get; }

        public UpdateResult(bool success, string errorMessage, string dllPath)
        {
            Success = success;
            ErrorMessage = errorMessage;
            DllPath = dllPath;
        }
    }
}
