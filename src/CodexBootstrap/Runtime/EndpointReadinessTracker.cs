using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime
{
    /// <summary>
    /// Tracks readiness of individual API endpoints
    /// </summary>
    public class EndpointReadinessTracker
    {
        private readonly ConcurrentDictionary<string, ComponentReadiness> _endpoints = new();
        private readonly ReadinessTracker _readinessTracker;
        private readonly ICodexLogger _logger;

        public EndpointReadinessTracker(ReadinessTracker readinessTracker, ICodexLogger logger)
        {
            _readinessTracker = readinessTracker;
            _logger = logger;
        }

        /// <summary>
        /// Register an endpoint for readiness tracking
        /// </summary>
        public void RegisterEndpoint(string endpoint, string moduleName, List<string>? dependencies = null)
        {
            var endpointComponent = new ComponentReadiness
            {
                ComponentId = endpoint,
                ComponentType = "Endpoint",
                State = ReadinessState.NotStarted,
                Dependencies = dependencies ?? new List<string> { moduleName },
                LastUpdated = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["module"] = moduleName,
                    ["path"] = endpoint
                }
            };

            _endpoints[endpoint] = endpointComponent;
            _logger.Info($"Registered endpoint for readiness tracking: {endpoint} (module: {moduleName})");
        }

        /// <summary>
        /// Update endpoint readiness based on module readiness
        /// </summary>
        public void UpdateEndpointReadiness(string endpoint, ReadinessState moduleState, string message = "")
        {
            if (!_endpoints.TryGetValue(endpoint, out var endpointComponent))
            {
                _logger.Warn($"Attempted to update readiness for unregistered endpoint: {endpoint}");
                return;
            }

            var previousState = endpointComponent.State;
            
            // Endpoint readiness depends on module readiness
            var endpointState = moduleState switch
            {
                ReadinessState.Ready => ReadinessState.Ready,
                ReadinessState.Failed => ReadinessState.Failed,
                ReadinessState.Degraded => ReadinessState.Degraded,
                ReadinessState.Initializing => ReadinessState.Initializing,
                _ => ReadinessState.NotStarted
            };

            endpointComponent.State = endpointState;
            endpointComponent.LastResult = new ReadinessResult
            {
                State = endpointState,
                Message = string.IsNullOrEmpty(message) ? $"Module {endpointComponent.Metadata["module"]} is {moduleState}" : message,
                Timestamp = DateTime.UtcNow
            };
            endpointComponent.LastUpdated = DateTime.UtcNow;

            _logger.Info($"Endpoint {endpoint} state changed: {previousState} -> {endpointState} (module: {moduleState})");
        }

        /// <summary>
        /// Get readiness for a specific endpoint
        /// </summary>
        public ComponentReadiness? GetEndpointReadiness(string endpoint)
        {
            return _endpoints.TryGetValue(endpoint, out var component) ? component : null;
        }

        /// <summary>
        /// Get all endpoints
        /// </summary>
        public IEnumerable<ComponentReadiness> GetAllEndpoints()
        {
            return _endpoints.Values.OrderBy(e => e.ComponentId);
        }

        /// <summary>
        /// Get endpoints by module
        /// </summary>
        public IEnumerable<ComponentReadiness> GetEndpointsByModule(string moduleName)
        {
            return _endpoints.Values.Where(e => 
                e.Metadata.TryGetValue("module", out var module) && 
                module?.ToString() == moduleName);
        }

        /// <summary>
        /// Get ready endpoints
        /// </summary>
        public IEnumerable<ComponentReadiness> GetReadyEndpoints()
        {
            return _endpoints.Values.Where(e => e.State == ReadinessState.Ready);
        }

        /// <summary>
        /// Get not-ready endpoints
        /// </summary>
        public IEnumerable<ComponentReadiness> GetNotReadyEndpoints()
        {
            return _endpoints.Values.Where(e => e.State != ReadinessState.Ready);
        }

        /// <summary>
        /// Auto-register endpoints from a module's provided endpoints
        /// </summary>
        public void AutoRegisterModuleEndpoints(string moduleName, IEnumerable<string> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                RegisterEndpoint(endpoint, moduleName);
            }
        }

        /// <summary>
        /// Update all endpoints for a module when module state changes
        /// </summary>
        public void UpdateModuleEndpoints(string moduleName, ReadinessState moduleState, string message = "")
        {
            var moduleEndpoints = GetEndpointsByModule(moduleName);
            foreach (var endpoint in moduleEndpoints)
            {
                UpdateEndpointReadiness(endpoint.ComponentId, moduleState, message);
            }
        }
    }
}


