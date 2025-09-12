using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Module that provides spec-driven architecture functionality
    /// </summary>
    [ApiModule(Name = "SpecDrivenModule", Version = "1.0.0", Description = "Provides spec-driven architecture with ice/water/gas states", Tags = new[] { "spec-driven", "architecture", "ice-water-gas" })]
    public class SpecDrivenModule : IModule
    {
        private readonly ILogger<SpecDrivenModule> _logger;
        private readonly SpecDrivenArchitecture _specDrivenArchitecture;

        public SpecDrivenModule()
        {
            // Parameterless constructor for module loading
            _logger = null!;
            _specDrivenArchitecture = null!;
        }

        public SpecDrivenModule(ILogger<SpecDrivenModule> logger, SpecDrivenArchitecture specDrivenArchitecture)
        {
            _logger = logger;
            _specDrivenArchitecture = specDrivenArchitecture;
        }

        public Node GetModuleNode()
        {
            return new Node(
                Id: "spec-driven-module",
                TypeId: "module",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Spec-Driven Architecture Module",
                Description: "Provides spec-driven architecture with ice/water/gas states",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new { 
                        id = "spec-driven-module", 
                        name = "SpecDrivenModule", 
                        version = "1.0.0", 
                        description = "Provides spec-driven architecture with ice/water/gas states",
                        architecture = "ice-water-gas"
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = "SpecDrivenModule",
                    ["version"] = "1.0.0",
                    ["description"] = "Provides spec-driven architecture with ice/water/gas states",
                    ["tags"] = new[] { "spec-driven", "architecture", "ice-water-gas" },
                    ["architecture"] = "ice-water-gas"
                }
            );
        }

        public void Register(NodeRegistry registry)
        {
            var moduleNode = GetModuleNode();
            registry.Upsert(moduleNode);
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via attributes, no additional registration needed
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // HTTP endpoints are registered via attributes, no additional registration needed
        }

        /// <summary>
        /// Gets the current architecture state (ice/water/gas)
        /// </summary>
        [Get("/spec-driven/architecture-state", "Get Architecture State", "Get the current state of the ice/water/gas architecture", "spec-driven")]
        public async Task<object> GetArchitectureStateAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return new { success = false, error = "Spec-driven architecture not initialized" };
                }

                var state = await _specDrivenArchitecture.GetArchitectureStateAsync();
                
                return new
                {
                    success = true,
                    message = "Architecture state retrieved successfully",
                    timestamp = DateTime.UtcNow,
                    architecture = new
                    {
                        ice = new
                        {
                            count = state.Ice.SpecCount,
                            specs = state.Ice.Specs.Select(s => new
                            {
                                name = s.Name,
                                path = s.Path,
                                lastModified = s.LastModified,
                                size = s.Size
                            })
                        },
                        water = new
                        {
                            count = state.Water.GeneratedCodeCount,
                            files = state.Water.GeneratedFiles.Select(f => new
                            {
                                name = f.Name,
                                path = f.Path,
                                lastGenerated = f.LastGenerated,
                                size = f.Size
                            })
                        },
                        gas = new
                        {
                            count = state.Gas.CompiledDllCount,
                            dlls = state.Gas.CompiledDlls.Select(d => new
                            {
                                name = d.Name,
                                path = d.Path,
                                lastCompiled = d.LastCompiled,
                                size = d.Size
                            })
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting architecture state");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Regenerates everything from specs (ice) - the ultimate rebuild
        /// </summary>
        [Post("/spec-driven/regenerate", "Regenerate from Specs", "Regenerate everything from specs (ice) - the ultimate rebuild", "spec-driven")]
        public async Task<object> RegenerateFromSpecsAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return new { success = false, error = "Spec-driven architecture not initialized" };
                }

                _logger?.LogInformation("Starting complete regeneration from specs");
                
                var result = await _specDrivenArchitecture.RegenerateFromSpecsAsync();
                
                return new
                {
                    success = result.Success,
                    message = result.Success ? "Complete regeneration successful" : "Regeneration failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.TotalDuration.TotalMilliseconds,
                    steps = result.Steps.Select(s => new
                    {
                        step = s.Step,
                        status = s.Status,
                        duration = s.Duration.TotalMilliseconds,
                        details = s.Details
                    }),
                    error = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during regeneration from specs");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Generates code from specs (ice -> water)
        /// </summary>
        [Post("/spec-driven/generate-code", "Generate Code from Specs", "Generate code from specs (ice -> water)", "spec-driven")]
        public async Task<object> GenerateCodeFromSpecsAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return new { success = false, error = "Spec-driven architecture not initialized" };
                }

                _logger?.LogInformation("Starting code generation from specs");
                
                var result = await _specDrivenArchitecture.GenerateCodeFromSpecsAsync();
                
                return new
                {
                    success = result.Success,
                    message = result.Success ? "Code generation successful" : "Code generation failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    generatedFiles = result.GeneratedFiles,
                    error = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during code generation from specs");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Compiles generated code to DLLs (water -> gas)
        /// </summary>
        [Post("/spec-driven/compile-code", "Compile Code to DLLs", "Compile generated code to DLLs (water -> gas)", "spec-driven")]
        public async Task<object> CompileGeneratedCodeAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return new { success = false, error = "Spec-driven architecture not initialized" };
                }

                _logger?.LogInformation("Starting code compilation to DLLs");
                
                var result = await _specDrivenArchitecture.CompileGeneratedCodeAsync();
                
                return new
                {
                    success = result.Success,
                    message = result.Success ? "Code compilation successful" : "Code compilation failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    compiledDlls = result.CompiledDlls,
                    error = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during code compilation");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Loads compiled modules into runtime
        /// </summary>
        [Post("/spec-driven/load-modules", "Load Compiled Modules", "Load compiled modules into runtime", "spec-driven")]
        public async Task<object> LoadCompiledModulesAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return new { success = false, error = "Spec-driven architecture not initialized" };
                }

                _logger?.LogInformation("Starting module loading");
                
                var result = await _specDrivenArchitecture.LoadCompiledModulesAsync();
                
                return new
                {
                    success = result.Success,
                    message = result.Success ? "Module loading successful" : "Module loading failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    loadedModules = result.LoadedModules,
                    error = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during module loading");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets information about the spec-driven architecture
        /// </summary>
        [Get("/spec-driven/info", "Get Architecture Info", "Get information about the spec-driven architecture", "spec-driven")]
        public async Task<object> GetArchitectureInfoAsync()
        {
            return new
            {
                success = true,
                message = "Spec-driven architecture information",
                timestamp = DateTime.UtcNow,
                architecture = new
                {
                    name = "Ice-Water-Gas Architecture",
                    description = "Three-state architecture where only specs (ice) are persistent",
                    states = new
                    {
                        ice = new
                        {
                            name = "Specs (Ice)",
                            description = "Persistent, immutable source of truth",
                            persistence = "Permanent",
                            examples = new[] { "Module specifications", "API contracts", "Data schemas" }
                        },
                        water = new
                        {
                            name = "Code (Water)",
                            description = "Generated from specs, can be regenerated",
                            persistence = "Temporary",
                            examples = new[] { "Generated C# modules", "API implementations", "Data access layers" }
                        },
                        gas = new
                        {
                            name = "DLLs (Gas)",
                            description = "Compiled from code, ephemeral runtime artifacts",
                            persistence = "Ephemeral",
                            examples = new[] { "Compiled assemblies", "Runtime modules", "Executable artifacts" }
                        }
                    },
                    benefits = new[]
                    {
                        "Only specs need to be persistent",
                        "Everything can be regenerated from specs",
                        "Code and DLLs are just cached artifacts",
                        "True source of truth is the spec",
                        "Enables complete system regeneration"
                    },
                    workflow = new[]
                    {
                        "1. Write/modify specs (ice)",
                        "2. Generate code from specs (ice -> water)",
                        "3. Compile code to DLLs (water -> gas)",
                        "4. Load modules into runtime",
                        "5. Repeat as needed - everything regeneratable"
                    }
                }
            };
        }
    }
}
