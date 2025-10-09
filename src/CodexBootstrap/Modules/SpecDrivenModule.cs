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
    public class SpecDrivenModule : ModuleBase
    {
        private readonly SpecDrivenArchitecture _specDrivenArchitecture;

        public override string Name => "Spec Driven Module";
        public override string Description => "Provides spec-driven architecture with ice/water/gas states";
        public override string Version => "1.0.0";

        public SpecDrivenModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
            _specDrivenArchitecture = new SpecDrivenArchitecture(logger, registry, null, null);
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "spec-driven-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "spec-driven", "architecture", "ice-water-gas" },
                capabilities: new[] { "Spec-Driven Architecture", "Ice/Water/Gas States", "Dynamic Generation" },
                spec: "codex.spec.spec-driven"
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
        /// Gets the current architecture state (ice/water/gas)
        /// </summary>
        [Get("/spec-driven/architecture-state", "Get Architecture State", "Get the current state of the ice/water/gas architecture", "spec-driven")]
        public async Task<IResult> GetArchitectureStateAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return Results.Json(new { success = false, error = "Spec-driven architecture not initialized" }, statusCode: 503);
                }

                var state = await _specDrivenArchitecture.GetArchitectureStateAsync();
                
                return Results.Ok(new
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
                });
            }
            catch (Exception ex)
            {
                _logger?.Error("Error getting architecture state", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Regenerates everything from specs (ice) - the ultimate rebuild
        /// </summary>
        [Post("/spec-driven/regenerate", "Regenerate from Specs", "Regenerate everything from specs (ice) - the ultimate rebuild", "spec-driven")]
        public async Task<IResult> RegenerateFromSpecsAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return Results.Json(new { success = false, error = "Spec-driven architecture not initialized" }, statusCode: 503);
                }

                _logger?.Info("Starting complete regeneration from specs");
                
                var result = await _specDrivenArchitecture.RegenerateFromSpecsAsync();
                
                return Results.Ok(new
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
                });
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during regeneration from specs", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Generates code from specs (ice -> water)
        /// </summary>
        [Post("/spec-driven/generate-code", "Generate Code from Specs", "Generate code from specs (ice -> water)", "spec-driven")]
        public async Task<IResult> GenerateCodeFromSpecsAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return Results.Json(new { success = false, error = "Spec-driven architecture not initialized" }, statusCode: 503);
                }

                _logger?.Info("Starting code generation from specs");
                
                var result = await _specDrivenArchitecture.GenerateCodeFromSpecsAsync();
                
                return Results.Ok(new
                {
                    success = result.Success,
                    message = result.Success ? "Code generation successful" : "Code generation failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    generatedFiles = result.GeneratedFiles,
                    error = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during code generation from specs", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Compiles generated code to DLLs (water -> gas)
        /// </summary>
        [Post("/spec-driven/compile-code", "Compile Code to DLLs", "Compile generated code to DLLs (water -> gas)", "spec-driven")]
        public async Task<IResult> CompileGeneratedCodeAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return Results.Json(new { success = false, error = "Spec-driven architecture not initialized" }, statusCode: 503);
                }

                _logger?.Info("Starting code compilation to DLLs");
                
                var result = await _specDrivenArchitecture.CompileGeneratedCodeAsync();
                
                return Results.Ok(new
                {
                    success = result.Success,
                    message = result.Success ? "Code compilation successful" : "Code compilation failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    compiledDlls = result.CompiledDlls,
                    error = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during code compilation", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Loads compiled modules into runtime
        /// </summary>
        [Post("/spec-driven/load-modules", "Load Compiled Modules", "Load compiled modules into runtime", "spec-driven")]
        public async Task<IResult> LoadCompiledModulesAsync()
        {
            try
            {
                if (_specDrivenArchitecture == null)
                {
                    return Results.Json(new { success = false, error = "Spec-driven architecture not initialized" }, statusCode: 503);
                }

                _logger?.Info("Starting module loading");
                
                var result = await _specDrivenArchitecture.LoadCompiledModulesAsync();
                
                return Results.Ok(new
                {
                    success = result.Success,
                    message = result.Success ? "Module loading successful" : "Module loading failed",
                    timestamp = DateTime.UtcNow,
                    duration = result.Duration.TotalMilliseconds,
                    loadedModules = result.LoadedModules,
                    error = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during module loading", ex);
                return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
            }
        }

        /// <summary>
        /// Gets information about the spec-driven architecture
        /// </summary>
        [Get("/spec-driven/info", "Get Architecture Info", "Get information about the spec-driven architecture", "spec-driven")]
        public async Task<IResult> GetArchitectureInfoAsync()
        {
            return Results.Ok(new
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
            });
        }
    }
}
