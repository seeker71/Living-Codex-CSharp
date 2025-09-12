using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Compiles C# source code to DLL using Roslyn
    /// </summary>
    public class ModuleCompiler
    {
        private readonly ILogger<ModuleCompiler> _logger;
        private readonly string _outputPath;
        private readonly List<MetadataReference> _references;

        public ModuleCompiler(ILogger<ModuleCompiler> logger, string outputPath = "./modules/compiled")
        {
            _logger = logger;
            _outputPath = outputPath;
            _references = new List<MetadataReference>();

            // Ensure output directory exists
            Directory.CreateDirectory(_outputPath);

            // Load common references
            LoadCommonReferences();
        }

        /// <summary>
        /// Compiles a single module from source code
        /// </summary>
        public async Task<CompilationResult> CompileModuleAsync(string moduleName, string sourceCode)
        {
            try
            {
                _logger.LogInformation("Compiling module: {ModuleName}", moduleName);

                // Parse the source code
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                // Create compilation
                var compilation = CSharpCompilation.Create(
                    moduleName,
                    new[] { syntaxTree },
                    _references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                // Emit to memory stream first
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    var errors = string.Join("\n", emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString()));
                    
                    _logger.LogError("Compilation failed for module {ModuleName}: {Errors}", moduleName, errors);
                    return new CompilationResult(false, errors, null);
                }

                // Write to file
                var dllPath = Path.Combine(_outputPath, $"{moduleName}.dll");
                ms.Position = 0;
                await File.WriteAllBytesAsync(dllPath, ms.ToArray());

                _logger.LogInformation("Successfully compiled module {ModuleName} to {DllPath}", moduleName, dllPath);
                return new CompilationResult(true, "Compilation successful", dllPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling module {ModuleName}", moduleName);
                return new CompilationResult(false, ex.Message, null);
            }
        }

        /// <summary>
        /// Compiles multiple modules in parallel
        /// </summary>
        public async Task<Dictionary<string, CompilationResult>> CompileModulesAsync(Dictionary<string, string> modules)
        {
            var tasks = modules.Select(async kvp =>
            {
                var result = await CompileModuleAsync(kvp.Key, kvp.Value);
                return new { ModuleName = kvp.Key, Result = result };
            });

            var results = await Task.WhenAll(tasks);
            return results.ToDictionary(r => r.ModuleName, r => r.Result);
        }

        /// <summary>
        /// Validates that a compiled DLL can be loaded and contains valid module
        /// </summary>
        public async Task<ValidationResult> ValidateCompiledModuleAsync(string dllPath)
        {
            try
            {
                _logger.LogInformation("Validating compiled module: {DllPath}", dllPath);

                // Load the assembly
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
        /// Loads common assembly references needed for compilation
        /// </summary>
        private void LoadCommonReferences()
        {
            try
            {
                // Get the current assembly location
                var currentAssembly = Assembly.GetExecutingAssembly();
                var currentAssemblyPath = currentAssembly.Location;

                // Add common references
                var referencePaths = new[]
                {
                    typeof(object).Assembly.Location, // System.Private.CoreLib
                    typeof(Console).Assembly.Location, // System.Console
                    typeof(Task).Assembly.Location, // System.Threading.Tasks
                    typeof(System.Collections.Generic.List<>).Assembly.Location, // System.Collections
                    typeof(System.Linq.Enumerable).Assembly.Location, // System.Linq
                    typeof(System.Text.Json.JsonSerializer).Assembly.Location, // System.Text.Json
                    typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location, // Microsoft.AspNetCore.Http
                    typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location, // Microsoft.Extensions.Logging
                    currentAssemblyPath // Current assembly
                };

                foreach (var path in referencePaths)
                {
                    if (File.Exists(path))
                    {
                        _references.Add(MetadataReference.CreateFromFile(path));
                        _logger.LogDebug("Added reference: {Path}", path);
                    }
                    else
                    {
                        _logger.LogWarning("Reference not found: {Path}", path);
                    }
                }

                _logger.LogInformation("Loaded {Count} assembly references", _references.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assembly references");
            }
        }

        /// <summary>
        /// Adds a custom reference to the compiler
        /// </summary>
        public void AddReference(string assemblyPath)
        {
            if (File.Exists(assemblyPath))
            {
                _references.Add(MetadataReference.CreateFromFile(assemblyPath));
                _logger.LogInformation("Added custom reference: {Path}", assemblyPath);
            }
            else
            {
                _logger.LogWarning("Reference not found: {Path}", assemblyPath);
            }
        }

        /// <summary>
        /// Gets the current references
        /// </summary>
        public IReadOnlyList<MetadataReference> GetReferences()
        {
            return _references.AsReadOnly();
        }
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
