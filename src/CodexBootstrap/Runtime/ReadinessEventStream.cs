using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime
{
    /// <summary>
    /// Manages Server-Sent Events for readiness notifications
    /// </summary>
    public class ReadinessEventStream : IDisposable
    {
        private readonly ConcurrentDictionary<string, StreamWriter> _clients = new();
        private readonly ICodexLogger _logger;
        private readonly ReadinessTracker _readinessTracker;
        private bool _disposed = false;

        public ReadinessEventStream(ReadinessTracker readinessTracker, ICodexLogger logger)
        {
            _readinessTracker = readinessTracker;
            _logger = logger;
            
            // Subscribe to readiness changes
            _readinessTracker.ReadinessChanged += OnReadinessChanged;
        }

        /// <summary>
        /// Add a client to the event stream
        /// </summary>
        public void AddClient(string clientId, StreamWriter writer)
        {
            _clients[clientId] = writer;
            _logger.Info($"Added client to readiness event stream: {clientId}");
            
            // Send initial state
            _ = Task.Run(async () => await SendInitialStateAsync(clientId, writer));
        }

        /// <summary>
        /// Remove a client from the event stream
        /// </summary>
        public void RemoveClient(string clientId)
        {
            if (_clients.TryRemove(clientId, out var writer))
            {
                _logger.Info($"Removed client from readiness event stream: {clientId}");
                try
                {
                    writer.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error disposing writer for client {clientId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Send a message to all connected clients
        /// </summary>
        public async Task SendToAllAsync(string eventType, object data)
        {
            var message = CreateSSEMessage(eventType, data);
            var tasks = new List<Task>();

            foreach (var client in _clients)
            {
                tasks.Add(SendToClientAsync(client.Key, client.Value, message));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Send a message to a specific client
        /// </summary>
        public async Task SendToClientAsync(string clientId, string eventType, object data)
        {
            if (_clients.TryGetValue(clientId, out var writer))
            {
                var message = CreateSSEMessage(eventType, data);
                await SendToClientAsync(clientId, writer, message);
            }
        }

        private async Task SendToClientAsync(string clientId, StreamWriter writer, string message)
        {
            try
            {
                await writer.WriteAsync(message);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error sending message to client {clientId}: {ex.Message}");
                RemoveClient(clientId);
            }
        }

        private async Task SendInitialStateAsync(string clientId, StreamWriter writer)
        {
            try
            {
                var systemReadiness = _readinessTracker.GetSystemReadiness();
                var message = CreateSSEMessage("system-state", systemReadiness);
                await writer.WriteAsync(message);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error sending initial state to client {clientId}: {ex.Message}");
            }
        }

        private string CreateSSEMessage(string eventType, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return $"event: {eventType}\ndata: {json}\n\n";
        }

        private void OnReadinessChanged(object? sender, ReadinessChangedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                // Send component-specific event
                await SendToAllAsync("component-changed", new
                {
                    componentId = e.ComponentId,
                    previousState = e.PreviousState.ToString(),
                    currentState = e.CurrentState.ToString(),
                    message = e.Result.Message,
                    timestamp = e.Timestamp
                });

                // Send updated system state
                var systemReadiness = _readinessTracker.GetSystemReadiness();
                await SendToAllAsync("system-state", systemReadiness);

                // Send system ready event if all components are ready
                if (systemReadiness.IsFullyReady)
                {
                    await SendToAllAsync("system-ready", new
                    {
                        message = "All components are ready",
                        timestamp = DateTime.UtcNow,
                        totalComponents = systemReadiness.TotalComponents,
                        readyComponents = systemReadiness.ReadyComponents
                    });
                }
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _readinessTracker.ReadinessChanged -= OnReadinessChanged;

            foreach (var client in _clients.Values)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error disposing client writer: {ex.Message}");
                }
            }
            _clients.Clear();
        }
    }
}


