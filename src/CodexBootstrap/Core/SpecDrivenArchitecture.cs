using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Implements the three-state architecture:
    /// - Spec (Ice): Persistent, immutable source of truth
    /// - Code (Water): Generated from spec, can be regenerated
    /// - DLL (Gas): Compiled from code, ephemeral runtime artifacts
    /// </summary>
    public class SpecDrivenArchitecture
    {
        private readonly ILogger<SpecDrivenArchitecture> _logger;
        private readonly NodeRegistry _registry;
        private readonly ModuleCompiler _moduleCompiler;
        private readonly HotReloadManager _hotReloadManager;
        private readonly string _specsDirectory;
        private readonly string _generatedCodeDirectory;
        private readonly string _compiledDllsDirectory;

        public SpecDrivenArchitecture(
            ILogger<SpecDrivenArchitecture> logger,
            NodeRegistry registry,
            ModuleCompiler moduleCompiler,
            HotReloadManager hotReloadManager)
        {
            _logger = logger;
            _registry = registry;
            _moduleCompiler = moduleCompiler;
            _hotReloadManager = hotReloadManager;
            
            // Set up directories for the three states
            _specsDirectory = Path.Combine(AppContext.BaseDirectory, "specs");
            _generatedCodeDirectory = Path.Combine(AppContext.BaseDirectory, "generated");
            _compiledDllsDirectory = Path.Combine(AppContext.BaseDirectory, "compiled");
            
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_specsDirectory);
            Directory.CreateDirectory(_generatedCodeDirectory);
            Directory.CreateDirectory(_compiledDllsDirectory);
        }

        /// <summary>
        /// Gets the current state of the three-state architecture
        /// </summary>
        public async Task<ArchitectureState> GetArchitectureStateAsync()
        {
            var specs = await GetSpecFilesAsync();
            var generatedCode = await GetGeneratedCodeFilesAsync();
            var compiledDlls = await GetCompiledDllFilesAsync();

            return new ArchitectureState
            {
                Ice = new IceState
                {
                    SpecCount = specs.Count,
                    Specs = specs.Select(s => new SpecInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(s),
                        Path = s,
                        LastModified = File.GetLastWriteTime(s),
                        Size = new FileInfo(s).Length
                    }).ToList()
                },
                Water = new WaterState
                {
                    GeneratedCodeCount = generatedCode.Count,
                    GeneratedFiles = generatedCode.Select(f => new GeneratedFileInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Path = f,
                        LastGenerated = File.GetLastWriteTime(f),
                        Size = new FileInfo(f).Length
                    }).ToList()
                },
                Gas = new GasState
                {
                    CompiledDllCount = compiledDlls.Count,
                    CompiledDlls = compiledDlls.Select(d => new CompiledDllInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(d),
                        Path = d,
                        LastCompiled = File.GetLastWriteTime(d),
                        Size = new FileInfo(d).Length
                    }).ToList()
                }
            };
        }

        /// <summary>
        /// Regenerates everything from specs (ice) - the ultimate rebuild
        /// </summary>
        public async Task<RegenerationResult> RegenerateFromSpecsAsync()
        {
            try
            {
                _logger.LogInformation("Starting complete regeneration from specs (ice)");
                
                var result = new RegenerationResult
                {
                    StartTime = DateTime.UtcNow,
                    Steps = new List<RegenerationStep>()
                };

                // Step 1: Clean generated code (water)
                await CleanGeneratedCodeAsync();
                result.Steps.Add(new RegenerationStep
                {
                    Step = "Clean Generated Code",
                    Status = "Completed",
                    Duration = TimeSpan.Zero
                });

                // Step 2: Clean compiled DLLs (gas)
                await CleanCompiledDllsAsync();
                result.Steps.Add(new RegenerationStep
                {
                    Step = "Clean Compiled DLLs",
                    Status = "Completed",
                    Duration = TimeSpan.Zero
                });

                // Step 3: Generate code from specs (ice -> water)
                var codeGenResult = await GenerateCodeFromSpecsAsync();
                result.Steps.Add(new RegenerationStep
                {
                    Step = "Generate Code from Specs",
                    Status = codeGenResult.Success ? "Completed" : "Failed",
                    Duration = codeGenResult.Duration,
                    Details = codeGenResult.ErrorMessage
                });

                if (!codeGenResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to generate code from specs";
                    return result;
                }

                // Step 4: Compile code to DLLs (water -> gas)
                var compileResult = await CompileGeneratedCodeAsync();
                result.Steps.Add(new RegenerationStep
                {
                    Step = "Compile Code to DLLs",
                    Status = compileResult.Success ? "Completed" : "Failed",
                    Duration = compileResult.Duration,
                    Details = compileResult.ErrorMessage
                });

                if (!compileResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to compile generated code";
                    return result;
                }

                // Step 5: Load compiled modules
                var loadResult = await LoadCompiledModulesAsync();
                result.Steps.Add(new RegenerationStep
                {
                    Step = "Load Compiled Modules",
                    Status = loadResult.Success ? "Completed" : "Failed",
                    Duration = loadResult.Duration,
                    Details = loadResult.ErrorMessage
                });

                result.Success = loadResult.Success;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("Complete regeneration from specs completed in {Duration}ms", 
                    result.TotalDuration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complete regeneration from specs");
                return new RegenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    EndTime = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generates code from specs (ice -> water)
        /// </summary>
        public async Task<CodeGenerationResult> GenerateCodeFromSpecsAsync()
        {
            var startTime = DateTime.UtcNow;
            var specs = await GetSpecFilesAsync();
            var generatedFiles = new List<string>();

            try
            {
                foreach (var specPath in specs)
                {
                    var specContent = await File.ReadAllTextAsync(specPath);
                    var specName = Path.GetFileNameWithoutExtension(specPath);
                    
                    // Generate C# code from spec
                    var generatedCode = await GenerateCodeFromSpecAsync(specName, specContent);
                    
                    if (!string.IsNullOrEmpty(generatedCode))
                    {
                        var outputPath = Path.Combine(_generatedCodeDirectory, $"{specName}.cs");
                        await File.WriteAllTextAsync(outputPath, generatedCode);
                        generatedFiles.Add(outputPath);
                        
                        _logger.LogInformation("Generated code for spec: {SpecName}", specName);
                    }
                }

                return new CodeGenerationResult
                {
                    Success = true,
                    GeneratedFiles = generatedFiles,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code from specs");
                return new CodeGenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Compiles generated code to DLLs (water -> gas)
        /// </summary>
        public async Task<SpecCompilationResult> CompileGeneratedCodeAsync()
        {
            var startTime = DateTime.UtcNow;
            var generatedFiles = await GetGeneratedCodeFilesAsync();
            var compiledDlls = new List<string>();

            try
            {
                foreach (var codeFile in generatedFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(codeFile);
                    var codeContent = await File.ReadAllTextAsync(codeFile);
                    
                    var compileResult = await _moduleCompiler.CompileModuleAsync(fileName, codeContent);
                    
                    if (compileResult.Success)
                    {
                        compiledDlls.Add(compileResult.DllPath);
                        _logger.LogInformation("Compiled module: {ModuleName}", fileName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to compile module {ModuleName}: {Error}", 
                            fileName, compileResult.ErrorMessage);
                    }
                }

                return new SpecCompilationResult
                {
                    Success = true,
                    CompiledDlls = compiledDlls,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling generated code");
                return new SpecCompilationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Loads compiled modules into the runtime
        /// </summary>
        public async Task<LoadResult> LoadCompiledModulesAsync()
        {
            var startTime = DateTime.UtcNow;
            var compiledDlls = await GetCompiledDllFilesAsync();
            var loadedModules = new List<string>();

            try
            {
                foreach (var dllPath in compiledDlls)
                {
                    // For now, just log that we would load the module
                    // In a full implementation, this would use the ModuleLoader
                    loadedModules.Add(Path.GetFileNameWithoutExtension(dllPath));
                    _logger.LogInformation("Would load module: {ModuleName}", 
                        Path.GetFileNameWithoutExtension(dllPath));
                }

                return new LoadResult
                {
                    Success = true,
                    LoadedModules = loadedModules,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading compiled modules");
                return new LoadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime
                };
            }
        }

        private async Task<List<string>> GetSpecFilesAsync()
        {
            return Directory.GetFiles(_specsDirectory, "*.spec.json")
                .Concat(Directory.GetFiles(_specsDirectory, "*.spec.yaml"))
                .Concat(Directory.GetFiles(_specsDirectory, "*.spec.yml"))
                .ToList();
        }

        private async Task<List<string>> GetGeneratedCodeFilesAsync()
        {
            return Directory.GetFiles(_generatedCodeDirectory, "*.cs").ToList();
        }

        private async Task<List<string>> GetCompiledDllFilesAsync()
        {
            return Directory.GetFiles(_compiledDllsDirectory, "*.dll").ToList();
        }

        private async Task CleanGeneratedCodeAsync()
        {
            if (Directory.Exists(_generatedCodeDirectory))
            {
                Directory.Delete(_generatedCodeDirectory, true);
                Directory.CreateDirectory(_generatedCodeDirectory);
            }
        }

        private async Task CleanCompiledDllsAsync()
        {
            if (Directory.Exists(_compiledDllsDirectory))
            {
                Directory.Delete(_compiledDllsDirectory, true);
                Directory.CreateDirectory(_compiledDllsDirectory);
            }
        }

        private async Task<string> GenerateCodeFromSpecAsync(string specName, string specContent)
        {
            try
            {
                // Parse spec content (assuming JSON format for now)
                var spec = JsonSerializer.Deserialize<SpecModuleDefinition>(specContent);
                
                // Generate C# module code from spec
                var generatedCode = GenerateModuleCode(spec);
                
                return generatedCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code from spec {SpecName}", specName);
                return string.Empty;
            }
        }

        private string GenerateModuleCode(SpecModuleDefinition spec)
        {
            return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Generated
{{
    /// <summary>
    /// Generated module from spec: {spec.Name}
    /// </summary>
    [ApiModule(Name = ""{spec.Name}"", Version = ""{spec.Version}"", Description = ""{spec.Description}"", Tags = new[] {{ ""generated"", ""spec-driven"" }})]
    public class {spec.ClassName} : IModule
    {{
        private readonly ILogger<{spec.ClassName}> _logger;

        public {spec.ClassName}()
        {{
            _logger = null!;
        }}

        public {spec.ClassName}(ILogger<{spec.ClassName}> logger)
        {{
            _logger = logger;
        }}

        public Node GetModuleNode()
        {{
            return new Node(
                Id: ""{spec.Id}"",
                TypeId: ""module"",
                State: ContentState.Ice,
                Locale: ""en"",
                Title: ""{spec.Title}"",
                Description: ""{spec.Description}"",
                Content: new ContentRef(
                    MediaType: ""application/json"",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new {{ 
                        id = ""{spec.Id}"", 
                        name = ""{spec.Name}"", 
                        version = ""{spec.Version}"",
                        generated = true
                    }}),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {{
                    [""name""] = ""{spec.Name}"",
                    [""version""] = ""{spec.Version}"",
                    [""description""] = ""{spec.Description}"",
                    [""generated""] = true,
                    [""spec-driven""] = true
                }}
            );
        }}

        public void Register(NodeRegistry registry)
        {{
            var moduleNode = GetModuleNode();
            registry.Upsert(moduleNode);
        }}

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {{
            // API handlers are registered via attributes
        }}

        public void RegisterHttpEndpoints(object app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {{
            // HTTP endpoints are registered via attributes
        }}

        {GenerateEndpoints(spec)}
    }}
}}";
        }

        private string GenerateEndpoints(SpecModuleDefinition spec)
        {
            var endpoints = new List<string>();
            
            foreach (var endpoint in spec.Endpoints)
            {
                var methodSignature = endpoint.HttpMethod.ToUpper() switch
                {
                    "GET" => $"[Get(\"{endpoint.Path}\", \"{endpoint.Name}\", \"{endpoint.Description}\", \"generated\")]",
                    "POST" => $"[Post(\"{endpoint.Path}\", \"{endpoint.Name}\", \"{endpoint.Description}\", \"generated\")]",
                    "PUT" => $"[Put(\"{endpoint.Path}\", \"{endpoint.Name}\", \"{endpoint.Description}\", \"generated\")]",
                    "DELETE" => $"[Delete(\"{endpoint.Path}\", \"{endpoint.Name}\", \"{endpoint.Description}\", \"generated\")]",
                    _ => $"[Get(\"{endpoint.Path}\", \"{endpoint.Name}\", \"{endpoint.Description}\", \"generated\")]"
                };

                var endpointCode = $@"
        /// <summary>
        /// {endpoint.Description}
        /// </summary>
        {methodSignature}
        public async Task<object> {endpoint.MethodName}Async()
        {{
            return new
            {{
                success = true,
                message = ""{endpoint.Name} executed"",
                timestamp = DateTime.UtcNow,
                endpoint = ""{endpoint.Path}"",
                generated = true
            }};
        }}";

                endpoints.Add(endpointCode);
            }

            return string.Join("\n", endpoints);
        }
    }

    // Data models for the three-state architecture
    public class ArchitectureState
    {
        public IceState Ice { get; set; } = new();
        public WaterState Water { get; set; } = new();
        public GasState Gas { get; set; } = new();
    }

    public class IceState
    {
        public int SpecCount { get; set; }
        public List<SpecInfo> Specs { get; set; } = new();
    }

    public class WaterState
    {
        public int GeneratedCodeCount { get; set; }
        public List<GeneratedFileInfo> GeneratedFiles { get; set; } = new();
    }

    public class GasState
    {
        public int CompiledDllCount { get; set; }
        public List<CompiledDllInfo> CompiledDlls { get; set; } = new();
    }

    public class SpecInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }

    public class GeneratedFileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastGenerated { get; set; }
        public long Size { get; set; }
    }

    public class CompiledDllInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastCompiled { get; set; }
        public long Size { get; set; }
    }

    public class RegenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;
        public List<RegenerationStep> Steps { get; set; } = new();
    }

    public class RegenerationStep
    {
        public string Step { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class CodeGenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    public class SpecCompilationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> CompiledDlls { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    public class LoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> LoadedModules { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    public class SpecModuleDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EndpointSpec> Endpoints { get; set; } = new();
    }

    public class EndpointSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
