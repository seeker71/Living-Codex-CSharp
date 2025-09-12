using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// A test dynamic module that can be hot-reloaded
    /// </summary>
    [ApiModule(Name = "TestDynamicModule", Version = "1.0.0", Description = "A test module for demonstrating hot-reload functionality", Tags = new[] { "test", "dynamic", "hot-reload" })]
    public class TestDynamicModule : IModule
    {
        private readonly ILogger<TestDynamicModule> _logger;
        private int _callCount = 0;
        private int _version = 2; // Updated version for hot-reload demo

        public TestDynamicModule()
        {
            // Parameterless constructor for dynamic loading
            _logger = null!;
        }

        public TestDynamicModule(ILogger<TestDynamicModule> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the module node for this module
        /// </summary>
        public Node GetModuleNode()
        {
            return new Node(
                Id: "test-dynamic-module",
                TypeId: "module",
                State: ContentState.Ice,
                Locale: "en",
                Title: $"Test Dynamic Module v{_version}",
                Description: $"A test module for demonstrating hot-reload functionality - Version {_version}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new { 
                        id = "test-dynamic-module", 
                        name = "TestDynamicModule", 
                        version = _version.ToString(), 
                        description = "A test module for demonstrating hot-reload functionality",
                        callCount = _callCount
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = "TestDynamicModule",
                    ["version"] = _version.ToString(),
                    ["description"] = $"A test module for demonstrating hot-reload functionality - Version {_version}",
                    ["tags"] = new[] { "test", "dynamic", "hot-reload" },
                    ["callCount"] = _callCount
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
        /// Gets the current status of the test module
        /// </summary>
        [Get("/test-dynamic/status", "Get Test Status", "Get the current status of the test dynamic module", "test")]
        public async Task<object> GetStatusAsync()
        {
            _callCount++;
            
            return new
            {
                success = true,
                message = $"Test dynamic module v{_version} is running",
                timestamp = DateTime.UtcNow,
                version = _version.ToString(),
                callCount = _callCount,
                moduleId = "test-dynamic-module"
            };
        }

        /// <summary>
        /// Increments the call counter
        /// </summary>
        [Post("/test-dynamic/increment", "Increment Counter", "Increment the call counter", "test")]
        public async Task<object> IncrementCounterAsync()
        {
            _callCount++;
            
            return new
            {
                success = true,
                message = "Counter incremented",
                timestamp = DateTime.UtcNow,
                newCount = _callCount
            };
        }

        /// <summary>
        /// Resets the call counter
        /// </summary>
        [Post("/test-dynamic/reset", "Reset Counter", "Reset the call counter to zero", "test")]
        public async Task<object> ResetCounterAsync()
        {
            _callCount = 0;
            
            return new
            {
                success = true,
                message = "Counter reset",
                timestamp = DateTime.UtcNow,
                newCount = _callCount
            };
        }

        /// <summary>
        /// Gets module information including version
        /// </summary>
        [Get("/test-dynamic/info", "Get Module Info", "Get detailed information about the test dynamic module", "test")]
        public async Task<object> GetModuleInfoAsync()
        {
            return new
            {
                success = true,
                message = "Module information retrieved",
                timestamp = DateTime.UtcNow,
                module = new
                {
                    id = "test-dynamic-module",
                    name = "TestDynamicModule",
                    version = _version.ToString(),
                    description = $"A test module for demonstrating hot-reload functionality - Version {_version}",
                    callCount = _callCount,
                    features = new[]
                    {
                        "Status endpoint",
                        "Counter increment",
                        "Counter reset",
                        "Module info (NEW in v2!)"
                    }
                }
            };
        }
    }
}
