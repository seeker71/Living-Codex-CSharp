using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime
{
    /// <summary>
    /// Centralized service for tracking component readiness states
    /// </summary>
    public class ReadinessTracker : IDisposable
    {
        private readonly ConcurrentDictionary<string, ComponentReadiness> _components = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ReadinessResult>> _waiters = new();
        private readonly ICodexLogger _logger;
        private readonly object _lock = new();
        private bool _disposed = false;

        public event EventHandler<ReadinessChangedEventArgs>? ReadinessChanged;

        public ReadinessTracker(ICodexLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register a component for readiness tracking
        /// </summary>
        public void RegisterComponent(string componentId, string componentType, List<string>? dependencies = null)
        {
            var component = new ComponentReadiness
            {
                ComponentId = componentId,
                ComponentType = componentType,
                State = ReadinessState.NotStarted,
                Dependencies = dependencies ?? new List<string>(),
                LastUpdated = DateTime.UtcNow
            };

            _components[componentId] = component;
            _logger.Info($"Registered component for readiness tracking: {componentId} ({componentType})");
        }

        /// <summary>
        /// Update a component's readiness state
        /// </summary>
        public void UpdateReadiness(string componentId, ReadinessResult result)
        {
            if (!_components.TryGetValue(componentId, out var component))
            {
                _logger.Warn($"Attempted to update readiness for unregistered component: {componentId}");
                return;
            }

            var previousState = component.State;
            component.State = result.State;
            component.LastResult = result;
            component.LastUpdated = DateTime.UtcNow;

            _logger.Info($"Component {componentId} state changed: {previousState} -> {result.State} ({result.Message})");

            // Notify waiters
            if (_waiters.TryRemove(componentId, out var waiter))
            {
                if (result.State == ReadinessState.Ready)
                {
                    waiter.SetResult(result);
                }
                else if (result.State == ReadinessState.Failed)
                {
                    waiter.SetException(result.Exception ?? new InvalidOperationException(result.Message));
                }
            }

            // Raise event
            ReadinessChanged?.Invoke(this, new ReadinessChangedEventArgs
            {
                ComponentId = componentId,
                PreviousState = previousState,
                CurrentState = result.State,
                Result = result
            });
        }

        /// <summary>
        /// Get current readiness state for a component
        /// </summary>
        public ReadinessState GetComponentState(string componentId)
        {
            return _components.TryGetValue(componentId, out var component) ? component.State : ReadinessState.NotStarted;
        }

        /// <summary>
        /// Get detailed readiness information for a component
        /// </summary>
        public ComponentReadiness? GetComponentReadiness(string componentId)
        {
            return _components.TryGetValue(componentId, out var component) ? component : null;
        }

        /// <summary>
        /// Get overall system readiness
        /// </summary>
        public SystemReadiness GetSystemReadiness()
        {
            var components = _components.Values.ToList();
            var readyCount = components.Count(c => c.State == ReadinessState.Ready);
            var initializingCount = components.Count(c => c.State == ReadinessState.Initializing);
            var failedCount = components.Count(c => c.State == ReadinessState.Failed);
            var degradedCount = components.Count(c => c.State == ReadinessState.Degraded);

            var overallState = components.Count == 0 ? ReadinessState.NotStarted :
                             failedCount > 0 ? ReadinessState.Failed :
                             initializingCount > 0 ? ReadinessState.Initializing :
                             degradedCount > 0 ? ReadinessState.Degraded :
                             readyCount == components.Count ? ReadinessState.Ready :
                             ReadinessState.NotStarted;

            return new SystemReadiness
            {
                OverallState = overallState,
                TotalComponents = components.Count,
                ReadyComponents = readyCount,
                InitializingComponents = initializingCount,
                FailedComponents = failedCount,
                DegradedComponents = degradedCount,
                LastUpdated = DateTime.UtcNow,
                Components = components.OrderBy(c => c.ComponentId).ToList()
            };
        }

        /// <summary>
        /// Wait for a component to become ready
        /// </summary>
        public async Task<ReadinessResult> WaitForComponentAsync(string componentId, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (!_components.TryGetValue(componentId, out var component))
            {
                throw new ArgumentException($"Component not registered: {componentId}");
            }

            if (component.State == ReadinessState.Ready)
            {
                return component.LastResult;
            }

            if (component.State == ReadinessState.Failed)
            {
                throw new InvalidOperationException($"Component {componentId} is in failed state: {component.LastResult.Message}");
            }

            var waiter = _waiters.GetOrAdd(componentId, _ => new TaskCompletionSource<ReadinessResult>());
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                cts.Token.Register(() => waiter.TrySetCanceled());
                return await waiter.Task;
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Component {componentId} did not become ready within {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Wait for all components to become ready
        /// </summary>
        public async Task<SystemReadiness> WaitForSystemReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var endTime = DateTime.UtcNow.Add(timeout);
            
            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                var systemReadiness = GetSystemReadiness();
                if (systemReadiness.IsFullyReady)
                {
                    return systemReadiness;
                }

                await Task.Delay(100, cancellationToken);
            }

            throw new TimeoutException($"System did not become fully ready within {timeout.TotalSeconds} seconds");
        }

        /// <summary>
        /// Get all components of a specific type
        /// </summary>
        public IEnumerable<ComponentReadiness> GetComponentsByType(string componentType)
        {
            return _components.Values.Where(c => c.ComponentType.Equals(componentType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get components that are ready
        /// </summary>
        public IEnumerable<ComponentReadiness> GetReadyComponents()
        {
            return _components.Values.Where(c => c.State == ReadinessState.Ready);
        }

        /// <summary>
        /// Get components that are not ready
        /// </summary>
        public IEnumerable<ComponentReadiness> GetNotReadyComponents()
        {
            return _components.Values.Where(c => c.State != ReadinessState.Ready);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Cancel all waiters
            foreach (var waiter in _waiters.Values)
            {
                waiter.TrySetCanceled();
            }
            _waiters.Clear();
        }
    }
}
