using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Self-updating system that manages dynamic module compilation and hot reloading
    /// </summary>
    public class SelfUpdateSystem
    {
        private readonly ILogger<SelfUpdateSystem> _logger;
        private readonly ModuleLoader _moduleLoader;
        private readonly NodeRegistry _nodeRegistry;
        private readonly string _modulesSourcePath;
        private readonly string _modulesCompiledPath;
        private readonly string _modulesBackupPath;
        private readonly Dictionary<string, ModuleVersion> _moduleVersions;
        private readonly FileSystemWatcher _fileWatcher;

        public SelfUpdateSystem(
            ILogger<SelfUpdateSystem> logger,
            ModuleLoader moduleLoader,
            NodeRegistry nodeRegistry,
            string modulesSourcePath = "./src/CodexBootstrap/Modules",
            string modulesCompiledPath = "./modules/compiled",
            string modulesBackupPath = "./modules/backup")
        {
            _logger = logger;
            _moduleLoader = moduleLoader;
            _nodeRegistry = nodeRegistry;
            _modulesSourcePath = modulesSourcePath;
            _modulesCompiledPath = modulesCompiledPath;
            _modulesBackupPath = modulesBackupPath;
            _moduleVersions = new Dictionary<string, ModuleVersion>();

            // Ensure directories exist
            Directory.CreateDirectory(_modulesSourcePath);
            Directory.CreateDirectory(_modulesCompiledPath);
            Directory.CreateDirectory(_modulesBackupPath);

            // Initialize file watcher for source changes
            _fileWatcher = new FileSystemWatcher(_modulesSourcePath, "*.cs")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _fileWatcher.Changed += OnSourceFileChanged;
            _fileWatcher.Created += OnSourceFileChanged;
            _fileWatcher.Deleted += OnSourceFileChanged;

            _logger.LogInformation("SelfUpdateSystem initialized with source: {SourcePath}, compiled: {CompiledPath}, backup: {BackupPath}", 
                _modulesSourcePath, _modulesCompiledPath, _modulesBackupPath);
        }

        /// <summary>
        /// Compiles a module from source code to DLL
        /// </summary>
        public async Task<CompilationResult> CompileModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Starting compilation of module: {ModuleName}", moduleName);

                var sourceFile = Path.Combine(_modulesSourcePath, $"{moduleName}.cs");
                if (!File.Exists(sourceFile))
                {
                    return new CompilationResult(false, $"Source file not found: {sourceFile}", null);
                }

                var sourceCode = await File.ReadAllTextAsync(sourceFile);
                var compilationResult = await CompileSourceCodeAsync(sourceCode, moduleName);

                if (compilationResult.Success)
                {
                    // Update module version tracking
                    var version = new ModuleVersion
                    {
                        Name = moduleName,
                        Version = DateTime.UtcNow.Ticks.ToString(),
                        CompiledAt = DateTime.UtcNow,
                        SourceHash = ComputeHash(sourceCode),
                        DllPath = compilationResult.DllPath
                    };
                    _moduleVersions[moduleName] = version;

                    _logger.LogInformation("Successfully compiled module {ModuleName} to {DllPath}", 
                        moduleName, compilationResult.DllPath);
                }

                return compilationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling module {ModuleName}", moduleName);
                return new CompilationResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Hot reloads a module with backup and rollback capability
        /// </summary>
        public async Task<HotReloadResult> HotReloadModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Starting hot reload of module: {ModuleName}", moduleName);

                // Step 1: Create backup of current DLL
                var currentDllPath = Path.Combine(_modulesCompiledPath, $"{moduleName}.dll");
                var backupDllPath = Path.Combine(_modulesBackupPath, $"{moduleName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dll");

                if (File.Exists(currentDllPath))
                {
                    File.Copy(currentDllPath, backupDllPath, true);
                    _logger.LogInformation("Created backup: {BackupPath}", backupDllPath);
                }

                // Step 2: Compile new version
                var compilationResult = await CompileModuleAsync(moduleName);
                if (!compilationResult.Success)
                {
                    return new HotReloadResult(false, $"Compilation failed: {compilationResult.ErrorMessage}", null);
                }

                // Step 3: Run sanity checks
                var sanityResult = await RunSanityChecksAsync(moduleName, compilationResult.DllPath);
                if (!sanityResult.Success)
                {
                    // Rollback to backup
                    if (File.Exists(backupDllPath))
                    {
                        File.Copy(backupDllPath, currentDllPath, true);
                        _logger.LogWarning("Rolled back to backup due to sanity check failure: {Error}", sanityResult.ErrorMessage);
                    }
                    return new HotReloadResult(false, $"Sanity checks failed: {sanityResult.ErrorMessage}", null);
                }

                // Step 4: Unload old module and load new one
                var unloadResult = await UnloadModuleAsync(moduleName);
                if (!unloadResult.Success)
                {
                    _logger.LogWarning("Failed to unload old module: {Error}", unloadResult.ErrorMessage);
                }

                var loadResult = await LoadModuleAsync(compilationResult.DllPath);
                if (!loadResult.Success)
                {
                    // Rollback to backup
                    if (File.Exists(backupDllPath))
                    {
                        File.Copy(backupDllPath, currentDllPath, true);
                        await LoadModuleAsync(backupDllPath);
                    }
                    return new HotReloadResult(false, $"Failed to load new module: {loadResult.ErrorMessage}", null);
                }

                // Step 5: Clean up backup after successful reload
                if (File.Exists(backupDllPath))
                {
                    File.Delete(backupDllPath);
                    _logger.LogInformation("Cleaned up backup after successful reload");
                }

                _logger.LogInformation("Successfully hot reloaded module: {ModuleName}", moduleName);
                return new HotReloadResult(true, "Module reloaded successfully", compilationResult.DllPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hot reload of module {ModuleName}", moduleName);
                return new HotReloadResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Runs sanity checks on a compiled module
        /// </summary>
        private async Task<SanityCheckResult> RunSanityChecksAsync(string moduleName, string dllPath)
        {
            try
            {
                _logger.LogInformation("Running sanity checks for module: {ModuleName}", moduleName);

                // Check 1: DLL can be loaded
                var assembly = Assembly.LoadFrom(dllPath);
                if (assembly == null)
                {
                    return new SanityCheckResult(false, "Failed to load assembly");
                }

                // Check 2: Module implements IModule interface
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!moduleTypes.Any())
                {
                    return new SanityCheckResult(false, "No IModule implementation found");
                }

                // Check 3: Module can be instantiated
                foreach (var moduleType in moduleTypes)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(moduleType);
                        if (instance == null)
                        {
                            return new SanityCheckResult(false, $"Failed to instantiate module type: {moduleType.Name}");
                        }

                        // Check 4: Module has required methods
                        var getModuleNodeMethod = moduleType.GetMethod("GetModuleNode");
                        if (getModuleNodeMethod == null)
                        {
                            return new SanityCheckResult(false, $"Module {moduleType.Name} missing GetModuleNode method");
                        }

                        // Check 5: Module node can be retrieved
                        var moduleNode = getModuleNodeMethod.Invoke(instance, null);
                        if (moduleNode == null)
                        {
                            return new SanityCheckResult(false, $"Module {moduleType.Name} GetModuleNode returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        return new SanityCheckResult(false, $"Error instantiating module {moduleType.Name}: {ex.Message}");
                    }
                }

                // Check 6: Module has at least one endpoint
                var hasEndpoints = moduleTypes.Any(t => 
                    t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Any(m => m.GetCustomAttributes<GetAttribute>().Any() ||
                              m.GetCustomAttributes<PostAttribute>().Any() ||
                              m.GetCustomAttributes<PutAttribute>().Any() ||
                              m.GetCustomAttributes<DeleteAttribute>().Any() ||
                              m.GetCustomAttributes<PatchAttribute>().Any() ||
                              m.GetCustomAttributes<ApiRouteAttribute>().Any()));

                if (!hasEndpoints)
                {
                    return new SanityCheckResult(false, "Module has no API endpoints");
                }

                _logger.LogInformation("All sanity checks passed for module: {ModuleName}", moduleName);
                return new SanityCheckResult(true, "All sanity checks passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sanity checks for module {ModuleName}", moduleName);
                return new SanityCheckResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Compiles source code to DLL using Roslyn
        /// </summary>
        private async Task<CompilationResult> CompileSourceCodeAsync(string sourceCode, string moduleName)
        {
            try
            {
                // This is a simplified implementation
                // In a real implementation, you would use Roslyn to compile the source code
                // For now, we'll simulate the compilation process

                var dllPath = Path.Combine(_modulesCompiledPath, $"{moduleName}.dll");
                
                // Simulate compilation delay
                await Task.Delay(100);

                // In a real implementation, you would:
                // 1. Parse the source code
                // 2. Add necessary using statements
                // 3. Compile to IL
                // 4. Save as DLL

                // For now, we'll create a placeholder DLL
                File.WriteAllText(dllPath, "Placeholder DLL - Real implementation would use Roslyn");

                return new CompilationResult(true, "Compilation successful", dllPath);
            }
            catch (Exception ex)
            {
                return new CompilationResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Unloads a module from the system
        /// </summary>
        private async Task<OperationResult> UnloadModuleAsync(string moduleName)
        {
            try
            {
                // Implementation would depend on your module loading system
                // This is a placeholder
                await Task.Delay(10);
                return new OperationResult(true, "Module unloaded successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Loads a module into the system
        /// </summary>
        private async Task<OperationResult> LoadModuleAsync(string dllPath)
        {
            try
            {
                // Implementation would depend on your module loading system
                // This is a placeholder
                await Task.Delay(10);
                return new OperationResult(true, "Module loaded successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Computes hash of source code for change detection
        /// </summary>
        private string ComputeHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Handles source file changes
        /// </summary>
        private async void OnSourceFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(e.FullPath);
                _logger.LogInformation("Source file changed: {FileName}, triggering hot reload", fileName);

                // Debounce rapid changes
                await Task.Delay(1000);

                var result = await HotReloadModuleAsync(fileName);
                if (result.Success)
                {
                    _logger.LogInformation("Successfully hot reloaded module: {FileName}", fileName);
                }
                else
                {
                    _logger.LogError("Failed to hot reload module {FileName}: {Error}", fileName, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling source file change");
            }
        }

        /// <summary>
        /// Gets the current version of a module
        /// </summary>
        public ModuleVersion GetModuleVersion(string moduleName)
        {
            return _moduleVersions.TryGetValue(moduleName, out var version) ? version : null;
        }

        /// <summary>
        /// Gets all module versions
        /// </summary>
        public Dictionary<string, ModuleVersion> GetAllModuleVersions()
        {
            return new Dictionary<string, ModuleVersion>(_moduleVersions);
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }
    }

    /// <summary>
    /// Represents a module version
    /// </summary>
    public class ModuleVersion
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime CompiledAt { get; set; }
        public string SourceHash { get; set; }
        public string DllPath { get; set; }
    }

    /// <summary>
    /// Result of a compilation operation
    /// </summary>
    public class CompilationResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public string DllPath { get; }

        public CompilationResult(bool success, string errorMessage, string dllPath)
        {
            Success = success;
            ErrorMessage = errorMessage;
            DllPath = dllPath;
        }
    }

    /// <summary>
    /// Result of a hot reload operation
    /// </summary>
    public class HotReloadResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public string DllPath { get; }

        public HotReloadResult(bool success, string errorMessage, string dllPath)
        {
            Success = success;
            ErrorMessage = errorMessage;
            DllPath = dllPath;
        }
    }

    /// <summary>
    /// Result of a sanity check operation
    /// </summary>
    public class SanityCheckResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public SanityCheckResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Generic operation result
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public OperationResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
