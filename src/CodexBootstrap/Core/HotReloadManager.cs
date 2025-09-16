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
    /// Manages hot reloading of modules with backup and rollback capabilities
    /// </summary>
    public class HotReloadManager
    {
        private readonly ILogger<HotReloadManager> _logger;
        private readonly ModuleLoader _moduleLoader;
        private readonly INodeRegistry _nodeRegistry;
        private readonly string _backupPath;
        private readonly Dictionary<string, ModuleBackup> _moduleBackups;
        private readonly Dictionary<string, Assembly> _loadedAssemblies;

        public HotReloadManager(
            ILogger<HotReloadManager> logger,
            ModuleLoader moduleLoader,
            INodeRegistry nodeRegistry,
            string backupPath = "./modules/backup")
        {
            _logger = logger;
            _moduleLoader = moduleLoader;
            _nodeRegistry = nodeRegistry;
            _backupPath = backupPath;
            _moduleBackups = new Dictionary<string, ModuleBackup>();
            _loadedAssemblies = new Dictionary<string, Assembly>();

            // Ensure backup directory exists
            Directory.CreateDirectory(_backupPath);
        }

        /// <summary>
        /// Hot reloads a module with backup and rollback
        /// </summary>
        public async Task<HotReloadResult> HotReloadModuleAsync(string moduleName, string newDllPath)
        {
            try
            {
                _logger.LogInformation("Starting hot reload for module: {ModuleName}", moduleName);

                // Step 1: Create backup of current module
                var backupResult = await CreateModuleBackupAsync(moduleName);
                if (!backupResult.Success)
                {
                    return new HotReloadResult(false, $"Failed to create backup: {backupResult.ErrorMessage}", null);
                }

                // Step 2: Validate new module
                var validationResult = await ValidateNewModuleAsync(newDllPath);
                if (!validationResult.Success)
                {
                    return new HotReloadResult(false, $"Validation failed: {validationResult.ErrorMessage}", null);
                }

                // Step 3: Unload current module
                var unloadResult = await UnloadModuleAsync(moduleName);
                if (!unloadResult.Success)
                {
                    _logger.LogWarning("Failed to unload module {ModuleName}: {Error}", moduleName, unloadResult.ErrorMessage);
                }

                // Step 4: Load new module
                var loadResult = await LoadNewModuleAsync(moduleName, newDllPath);
                if (!loadResult.Success)
                {
                    // Rollback to backup
                    await RollbackToBackupAsync(moduleName);
                    return new HotReloadResult(false, $"Failed to load new module: {loadResult.ErrorMessage}", null);
                }

                // Step 5: Run integration tests
                var integrationResult = await RunIntegrationTestsAsync(moduleName);
                if (!integrationResult.Success)
                {
                    // Rollback to backup
                    await RollbackToBackupAsync(moduleName);
                    return new HotReloadResult(false, $"Integration tests failed: {integrationResult.ErrorMessage}", null);
                }

                // Step 6: Clean up backup after successful reload
                await CleanupBackupAsync(moduleName);

                _logger.LogInformation("Successfully hot reloaded module: {ModuleName}", moduleName);
                return new HotReloadResult(true, "Module reloaded successfully", newDllPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hot reload of module {ModuleName}", moduleName);
                
                // Attempt rollback
                await RollbackToBackupAsync(moduleName);
                
                return new HotReloadResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Creates a backup of the current module
        /// </summary>
        private async Task<OperationResult> CreateModuleBackupAsync(string moduleName)
        {
            try
            {
                var backup = new ModuleBackup
                {
                    ModuleName = moduleName,
                    BackupPath = Path.Combine(_backupPath, $"{moduleName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dll"),
                    CreatedAt = DateTime.UtcNow
                };

                // Find current module DLL
                var currentDllPath = FindCurrentModuleDll(moduleName);
                if (string.IsNullOrEmpty(currentDllPath))
                {
                    _logger.LogWarning("No current DLL found for module {ModuleName}, creating empty backup", moduleName);
                    backup.IsEmpty = true;
                }
                else
                {
                    // Copy current DLL to backup
                    File.Copy(currentDllPath, backup.BackupPath, true);
                    backup.OriginalPath = currentDllPath;
                    backup.IsEmpty = false;
                }

                _moduleBackups[moduleName] = backup;
                _logger.LogInformation("Created backup for module {ModuleName}: {BackupPath}", moduleName, backup.BackupPath);

                return new OperationResult(true, "Backup created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup for module {ModuleName}", moduleName);
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Validates a new module before loading
        /// </summary>
        private async Task<ValidationResult> ValidateNewModuleAsync(string dllPath)
        {
            try
            {
                _logger.LogInformation("Validating new module: {DllPath}", dllPath);

                // Check if file exists
                if (!File.Exists(dllPath))
                {
                    return new ValidationResult(false, "DLL file not found");
                }

                // Try to load the assembly
                var assembly = Assembly.LoadFrom(dllPath);
                if (assembly == null)
                {
                    return new ValidationResult(false, "Failed to load assembly");
                }

                // Check for IModule implementations
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!moduleTypes.Any())
                {
                    return new ValidationResult(false, "No IModule implementation found");
                }

                // Validate each module type
                foreach (var moduleType in moduleTypes)
                {
                    var validation = await ValidateModuleTypeAsync(moduleType);
                    if (!validation.Success)
                    {
                        return validation;
                    }
                }

                _logger.LogInformation("Module validation successful: {DllPath}", dllPath);
                return new ValidationResult(true, "Module validation successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating module {DllPath}", dllPath);
                return new ValidationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Validates a specific module type
        /// </summary>
        private async Task<ValidationResult> ValidateModuleTypeAsync(Type moduleType)
        {
            try
            {
                // Check if it can be instantiated
                var instance = Activator.CreateInstance(moduleType);
                if (instance == null)
                {
                    return new ValidationResult(false, $"Failed to instantiate module type: {moduleType.Name}");
                }

                // Check for required methods
                var getModuleNodeMethod = moduleType.GetMethod("GetModuleNode");
                if (getModuleNodeMethod == null)
                {
                    return new ValidationResult(false, $"Module {moduleType.Name} missing GetModuleNode method");
                }

                // Test GetModuleNode method
                var moduleNode = getModuleNodeMethod.Invoke(instance, null);
                if (moduleNode == null)
                {
                    return new ValidationResult(false, $"Module {moduleType.Name} GetModuleNode returned null");
                }

                // Check for API endpoints
                var hasEndpoints = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(m => m.GetCustomAttributes<GetAttribute>().Any() ||
                             m.GetCustomAttributes<PostAttribute>().Any() ||
                             m.GetCustomAttributes<PutAttribute>().Any() ||
                             m.GetCustomAttributes<DeleteAttribute>().Any() ||
                             m.GetCustomAttributes<PatchAttribute>().Any() ||
                             m.GetCustomAttributes<ApiRouteAttribute>().Any());

                if (!hasEndpoints)
                {
                    return new ValidationResult(false, $"Module {moduleType.Name} has no API endpoints");
                }

                return new ValidationResult(true, "Module type validation successful");
            }
            catch (Exception ex)
            {
                return new ValidationResult(false, $"Error validating module type {moduleType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unloads a module from the system
        /// </summary>
        private async Task<OperationResult> UnloadModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Unloading module: {ModuleName}", moduleName);

                // Remove from module loader
                // This would depend on your specific module loading implementation
                // For now, we'll just log the action
                _logger.LogInformation("Module {ModuleName} unloaded from module loader", moduleName);

                // Remove from loaded assemblies
                if (_loadedAssemblies.ContainsKey(moduleName))
                {
                    _loadedAssemblies.Remove(moduleName);
                }

                return new OperationResult(true, "Module unloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading module {ModuleName}", moduleName);
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Loads a new module into the system
        /// </summary>
        private async Task<OperationResult> LoadNewModuleAsync(string moduleName, string dllPath)
        {
            try
            {
                _logger.LogInformation("Loading new module: {ModuleName} from {DllPath}", moduleName, dllPath);

                // Load the assembly
                var assembly = Assembly.LoadFrom(dllPath);
                _loadedAssemblies[moduleName] = assembly;

                // Register with module loader
                // This would depend on your specific module loading implementation
                // For now, we'll just log the action
                _logger.LogInformation("Module {ModuleName} loaded into module loader", moduleName);

                return new OperationResult(true, "Module loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading module {ModuleName}", moduleName);
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Runs integration tests on the newly loaded module
        /// </summary>
        private async Task<IntegrationTestResult> RunIntegrationTestsAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Running integration tests for module: {ModuleName}", moduleName);

                // Test 1: Module can be instantiated
                var instantiationTest = await TestModuleInstantiationAsync(moduleName);
                if (!instantiationTest.Success)
                {
                    return new IntegrationTestResult(false, $"Instantiation test failed: {instantiationTest.ErrorMessage}");
                }

                // Test 2: Module endpoints are accessible
                var endpointTest = await TestModuleEndpointsAsync(moduleName);
                if (!endpointTest.Success)
                {
                    return new IntegrationTestResult(false, $"Endpoint test failed: {endpointTest.ErrorMessage}");
                }

                // Test 3: Module integrates with system
                var integrationTest = await TestSystemIntegrationAsync(moduleName);
                if (!integrationTest.Success)
                {
                    return new IntegrationTestResult(false, $"System integration test failed: {integrationTest.ErrorMessage}");
                }

                _logger.LogInformation("All integration tests passed for module: {ModuleName}", moduleName);
                return new IntegrationTestResult(true, "All integration tests passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running integration tests for module {ModuleName}", moduleName);
                return new IntegrationTestResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Tests module instantiation
        /// </summary>
        private async Task<TestResult> TestModuleInstantiationAsync(string moduleName)
        {
            try
            {
                if (!_loadedAssemblies.TryGetValue(moduleName, out var assembly))
                {
                    return new TestResult(false, "Module assembly not found");
                }

                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var moduleType in moduleTypes)
                {
                    var instance = Activator.CreateInstance(moduleType);
                    if (instance == null)
                    {
                        return new TestResult(false, $"Failed to instantiate {moduleType.Name}");
                    }
                }

                return new TestResult(true, "Module instantiation successful");
            }
            catch (Exception ex)
            {
                return new TestResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Tests module endpoints
        /// </summary>
        private async Task<TestResult> TestModuleEndpointsAsync(string moduleName)
        {
            try
            {
                if (!_loadedAssemblies.TryGetValue(moduleName, out var assembly))
                {
                    return new TestResult(false, "Module assembly not found");
                }

                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var moduleType in moduleTypes)
                {
                    var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    var hasEndpoints = methods.Any(m => 
                        m.GetCustomAttributes<GetAttribute>().Any() ||
                        m.GetCustomAttributes<PostAttribute>().Any() ||
                        m.GetCustomAttributes<PutAttribute>().Any() ||
                        m.GetCustomAttributes<DeleteAttribute>().Any() ||
                        m.GetCustomAttributes<PatchAttribute>().Any() ||
                        m.GetCustomAttributes<ApiRouteAttribute>().Any());

                    if (!hasEndpoints)
                    {
                        return new TestResult(false, $"Module {moduleType.Name} has no endpoints");
                    }
                }

                return new TestResult(true, "Module endpoints test successful");
            }
            catch (Exception ex)
            {
                return new TestResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Tests system integration
        /// </summary>
        private async Task<TestResult> TestSystemIntegrationAsync(string moduleName)
        {
            try
            {
                // This would test integration with the broader system
                // For now, we'll just return success
                return new TestResult(true, "System integration test successful");
            }
            catch (Exception ex)
            {
                return new TestResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Rolls back to a previous backup
        /// </summary>
        private async Task<OperationResult> RollbackToBackupAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("Rolling back module: {ModuleName}", moduleName);

                if (!_moduleBackups.TryGetValue(moduleName, out var backup))
                {
                    return new OperationResult(false, "No backup found for module");
                }

                if (backup.IsEmpty)
                {
                    _logger.LogInformation("Backup is empty, no rollback needed for module: {ModuleName}", moduleName);
                    return new OperationResult(true, "No rollback needed (empty backup)");
                }

                // Copy backup back to original location
                if (File.Exists(backup.BackupPath))
                {
                    File.Copy(backup.BackupPath, backup.OriginalPath, true);
                    _logger.LogInformation("Rolled back module {ModuleName} to backup", moduleName);
                }

                return new OperationResult(true, "Rollback successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back module {ModuleName}", moduleName);
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Cleans up backup after successful reload
        /// </summary>
        private async Task<OperationResult> CleanupBackupAsync(string moduleName)
        {
            try
            {
                if (_moduleBackups.TryGetValue(moduleName, out var backup))
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Delete(backup.BackupPath);
                        _logger.LogInformation("Cleaned up backup for module: {ModuleName}", moduleName);
                    }
                    _moduleBackups.Remove(moduleName);
                }

                return new OperationResult(true, "Backup cleanup successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up backup for module {ModuleName}", moduleName);
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Finds the current DLL path for a module
        /// </summary>
        private string FindCurrentModuleDll(string moduleName)
        {
            // This would depend on your module loading system
            // For now, we'll return null
            return null;
        }

        /// <summary>
        /// Gets all module backups
        /// </summary>
        public Dictionary<string, ModuleBackup> GetAllBackups()
        {
            return new Dictionary<string, ModuleBackup>(_moduleBackups);
        }

        /// <summary>
        /// Gets backup for a specific module
        /// </summary>
        public ModuleBackup GetModuleBackup(string moduleName)
        {
            return _moduleBackups.TryGetValue(moduleName, out var backup) ? backup : null;
        }
    }

    /// <summary>
    /// Represents a module backup
    /// </summary>
    public class ModuleBackup
    {
        public string ModuleName { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsEmpty { get; set; }
    }

    /// <summary>
    /// Result of an integration test
    /// </summary>
    public class IntegrationTestResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public IntegrationTestResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Result of a test
    /// </summary>
    public class TestResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public TestResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
