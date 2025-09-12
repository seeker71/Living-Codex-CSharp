using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace DynamicTestModule
{
    /// <summary>
    /// A simple dynamic module for testing hot-reload functionality
    /// </summary>
    public class DynamicTestModule : IModule
    {
        private int _version = 1;
        private int _callCount = 0;

        public Node GetModuleNode()
        {
            return new Node(
                Id: "dynamic-test-module",
                TypeId: "module",
                State: ContentState.Ice,
                Locale: "en",
                Title: $"Dynamic Test Module v{_version}",
                Description: $"A dynamic module for testing hot-reload functionality - Version {_version}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new { 
                        id = "dynamic-test-module", 
                        name = "DynamicTestModule", 
                        version = _version.ToString(), 
                        callCount = _callCount
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = "DynamicTestModule",
                    ["version"] = _version.ToString(),
                    ["description"] = $"A dynamic module for testing hot-reload functionality - Version {_version}",
                    ["tags"] = new[] { "dynamic", "test", "hot-reload" },
                    ["callCount"] = _callCount
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

        public void RegisterHttpEndpoints(object app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // HTTP endpoints are registered via attributes, no additional registration needed
        }

        /// <summary>
        /// Gets the current status of the dynamic test module
        /// </summary>
        public async Task<object> GetStatusAsync()
        {
            _callCount++;
            
            return new
            {
                success = true,
                message = $"Dynamic test module v{_version} is running",
                timestamp = DateTime.UtcNow,
                version = _version.ToString(),
                callCount = _callCount,
                moduleId = "dynamic-test-module"
            };
        }

        /// <summary>
        /// Increments the call counter
        /// </summary>
        public async Task<object> IncrementCounterAsync()
        {
            _callCount++;
            
            return new
            {
                success = true,
                message = "Counter incremented",
                timestamp = DateTime.UtcNow,
                newCount = _callCount,
                version = _version.ToString()
            };
        }
    }
}
