using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Base class for readiness-related tests with common utilities
    /// </summary>
    public abstract class ReadinessTestBase : TestBase
    {
        protected readonly string BaseUrl;

        protected ReadinessTestBase()
        {
            BaseUrl = "http://localhost:5002"; // Default test server URL
        }

        protected ReadinessTestBase(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        /// <summary>
        /// Wait for system to be fully ready
        /// </summary>
        protected async Task WaitForSystemReadyAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromMinutes(2);
            var endTime = DateTime.UtcNow.Add(timeout.Value);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var response = await HttpClient.GetAsync($"{BaseUrl}/readiness");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var readiness = JsonSerializer.Deserialize<SystemReadiness>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (readiness?.IsFullyReady == true)
                        {
                            Logger.Info($"System is fully ready: {readiness.ReadyComponents}/{readiness.TotalComponents} components ready");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Readiness check failed: {ex.Message}");
                }

                await Task.Delay(1000);
            }

            throw new TimeoutException($"System did not become ready within {timeout.Value.TotalSeconds} seconds");
        }

        /// <summary>
        /// Wait for a specific component to be ready
        /// </summary>
        protected async Task WaitForComponentReadyAsync(string componentId, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            var endTime = DateTime.UtcNow.Add(timeout.Value);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var response = await HttpClient.GetAsync($"{BaseUrl}/readiness/modules/{componentId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var component = JsonSerializer.Deserialize<ComponentReadiness>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (component?.State == ReadinessState.Ready)
                        {
                            Logger.Info($"Component {componentId} is ready");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Component readiness check failed: {ex.Message}");
                }

                await Task.Delay(1000);
            }

            throw new TimeoutException($"Component {componentId} did not become ready within {timeout.Value.TotalSeconds} seconds");
        }

        /// <summary>
        /// Get current system readiness status
        /// </summary>
        protected async Task<SystemReadiness> GetSystemReadinessAsync()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness");
            response.IsSuccessStatusCode.Should().BeTrue($"Readiness endpoint should be available. Status: {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SystemReadiness>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to deserialize system readiness");
        }

        /// <summary>
        /// Get readiness status for a specific component
        /// </summary>
        protected async Task<ComponentReadiness> GetComponentReadinessAsync(string componentId)
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness/modules/{componentId}");
            response.IsSuccessStatusCode.Should().BeTrue($"Component {componentId} should be tracked. Status: {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ComponentReadiness>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException($"Failed to deserialize component readiness for {componentId}");
        }

        /// <summary>
        /// Assert that a component is in a specific state
        /// </summary>
        protected async Task AssertComponentStateAsync(string componentId, ReadinessState expectedState)
        {
            var component = await GetComponentReadinessAsync(componentId);
            component.State.Should().Be(expectedState, 
                $"Component {componentId} should be in {expectedState} state, but was {component.State}");
        }

        /// <summary>
        /// Assert that the system is fully ready
        /// </summary>
        protected async Task AssertSystemReadyAsync()
        {
            var readiness = await GetSystemReadinessAsync();
            readiness.IsFullyReady.Should().BeTrue(
                $"System should be fully ready. Ready: {readiness.ReadyComponents}/{readiness.TotalComponents}, " +
                $"Failed: {readiness.FailedComponents}, Initializing: {readiness.InitializingComponents}");
        }

        /// <summary>
        /// Test that an endpoint returns 503 when its module is not ready
        /// </summary>
        protected async Task AssertEndpointNotReadyAsync(string endpoint, int expectedStatusCode = 503)
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}{endpoint}");
            response.StatusCode.Should().Be((System.Net.HttpStatusCode)expectedStatusCode,
                $"Endpoint {endpoint} should return {expectedStatusCode} when not ready");
        }

        /// <summary>
        /// Test that an endpoint works when its module is ready
        /// </summary>
        protected async Task AssertEndpointReadyAsync(string endpoint, int expectedStatusCode = 200)
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}{endpoint}");
            response.IsSuccessStatusCode.Should().BeTrue(
                $"Endpoint {endpoint} should work when ready. Status: {response.StatusCode}");
        }

        /// <summary>
        /// Subscribe to readiness events and collect them
        /// </summary>
        protected async Task<System.Collections.Generic.List<object>> CollectReadinessEventsAsync(TimeSpan duration)
        {
            var events = new System.Collections.Generic.List<object>();
            var endTime = DateTime.UtcNow.Add(duration);

            try
            {
                using var eventSource = new System.Net.Http.HttpClient();
                var response = await eventSource.GetAsync($"{BaseUrl}/readiness/events", HttpCompletionOption.ResponseHeadersRead);
                
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new System.IO.StreamReader(stream);
                    
                    while (DateTime.UtcNow < endTime)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        
                        if (line.StartsWith("data: "))
                        {
                            var json = line.Substring(6);
                            try
                            {
                                var eventData = JsonSerializer.Deserialize<object>(json);
                                events.Add(eventData);
                            }
                            catch (JsonException)
                            {
                                // Skip malformed JSON
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to collect readiness events: {ex.Message}");
            }

            return events;
        }
    }
}


