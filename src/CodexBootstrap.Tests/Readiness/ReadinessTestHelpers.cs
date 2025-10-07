using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Helper methods for testing readiness scenarios
    /// </summary>
    public static class ReadinessTestHelpers
    {
        /// <summary>
        /// Create a test scenario with multiple modules in different states
        /// </summary>
        public static ReadinessTracker CreateTestScenario()
        {
            var logger = new Log4NetLogger(typeof(ReadinessTestHelpers));
            var tracker = new ReadinessTracker(logger);

            // Register test modules
            tracker.RegisterComponent("CoreModule", "Module");
            tracker.RegisterComponent("NewsModule", "Module", new List<string> { "CoreModule" });
            tracker.RegisterComponent("AIModule", "Module", new List<string> { "CoreModule" });
            tracker.RegisterComponent("FileModule", "Module");
            tracker.RegisterComponent("FailedModule", "Module");

            // Set up different states
            tracker.UpdateReadiness("CoreModule", ReadinessResult.Success("Core ready"));
            tracker.UpdateReadiness("NewsModule", ReadinessResult.Initializing("Loading news sources"));
            tracker.UpdateReadiness("AIModule", ReadinessResult.Success("AI ready"));
            tracker.UpdateReadiness("FileModule", ReadinessResult.Degraded("Limited functionality"));
            tracker.UpdateReadiness("FailedModule", ReadinessResult.Failed("Configuration error"));

            return tracker;
        }

        /// <summary>
        /// Simulate a module initialization sequence
        /// </summary>
        public static async Task SimulateModuleInitialization(ReadinessTracker tracker, string moduleName, TimeSpan duration)
        {
            tracker.UpdateReadiness(moduleName, ReadinessResult.Initializing("Starting initialization"));
            
            await Task.Delay(duration);
            
            tracker.UpdateReadiness(moduleName, ReadinessResult.Success("Initialization complete"));
        }

        /// <summary>
        /// Simulate a module failure
        /// </summary>
        public static void SimulateModuleFailure(ReadinessTracker tracker, string moduleName, string errorMessage)
        {
            var exception = new InvalidOperationException(errorMessage);
            tracker.UpdateReadiness(moduleName, ReadinessResult.Failed(errorMessage, exception));
        }

        /// <summary>
        /// Assert that a system readiness state matches expected values
        /// </summary>
        public static void AssertSystemReadiness(SystemReadiness readiness, int expectedTotal, int expectedReady, int expectedFailed = 0, int expectedInitializing = 0, int expectedDegraded = 0)
        {
            readiness.TotalComponents.Should().Be(expectedTotal, "total components should match");
            readiness.ReadyComponents.Should().Be(expectedReady, "ready components should match");
            readiness.FailedComponents.Should().Be(expectedFailed, "failed components should match");
            readiness.InitializingComponents.Should().Be(expectedInitializing, "initializing components should match");
            readiness.DegradedComponents.Should().Be(expectedDegraded, "degraded components should match");
        }

        /// <summary>
        /// Create a test endpoint readiness tracker
        /// </summary>
        public static (ReadinessTracker, EndpointReadinessTracker) CreateTestEndpointScenario()
        {
            var logger = new Log4NetLogger(typeof(ReadinessTestHelpers));
            var tracker = new ReadinessTracker(logger);
            var endpointTracker = new EndpointReadinessTracker(tracker, logger);

            // Register modules
            tracker.RegisterComponent("NewsModule", "Module");
            tracker.RegisterComponent("AIModule", "Module");

            // Register endpoints
            endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            endpointTracker.RegisterEndpoint("/api/news/sources", "NewsModule");
            endpointTracker.RegisterEndpoint("/api/ai/generate", "AIModule");
            endpointTracker.RegisterEndpoint("/api/ai/health", "AIModule");

            return (tracker, endpointTracker);
        }

        /// <summary>
        /// Simulate endpoint readiness based on module state
        /// </summary>
        public static void UpdateEndpointReadiness(ReadinessTracker tracker, EndpointReadinessTracker endpointTracker, string moduleName, ReadinessState moduleState, string message = "")
        {
            tracker.UpdateReadiness(moduleName, new ReadinessResult { State = moduleState, Message = message });
            endpointTracker.UpdateModuleEndpoints(moduleName, moduleState, message);
        }

        /// <summary>
        /// Create a test scenario for middleware testing
        /// </summary>
        public static (ReadinessTracker, Dictionary<string, ReadinessState>) CreateMiddlewareTestScenario()
        {
            var logger = new Log4NetLogger(typeof(ReadinessTestHelpers));
            var tracker = new ReadinessTracker(logger);

            // Register modules with different states
            var moduleStates = new Dictionary<string, ReadinessState>
            {
                ["RealtimeNewsStreamModule"] = ReadinessState.Ready,
                ["FileSystemModule"] = ReadinessState.Initializing,
                ["AIModule"] = ReadinessState.Failed,
                ["CoreModule"] = ReadinessState.Ready
            };

            foreach (var (moduleName, state) in moduleStates)
            {
                tracker.RegisterComponent(moduleName, "Module");
                tracker.UpdateReadiness(moduleName, new ReadinessResult { State = state, Message = $"{moduleName} is {state}" });
            }

            return (tracker, moduleStates);
        }

        /// <summary>
        /// Assert that an endpoint should be available based on module state
        /// </summary>
        public static void AssertEndpointAvailability(string endpoint, string expectedModule, ReadinessState moduleState, bool shouldBeAvailable)
        {
            var isAvailable = moduleState == ReadinessState.Ready;
            isAvailable.Should().Be(shouldBeAvailable, 
                $"Endpoint {endpoint} (module: {expectedModule}, state: {moduleState}) should {(shouldBeAvailable ? "be" : "not be")} available");
        }

        /// <summary>
        /// Create a test scenario for SSE events
        /// </summary>
        public static async Task<List<object>> SimulateReadinessEvents(ReadinessTracker tracker, TimeSpan duration)
        {
            var events = new List<object>();
            var endTime = DateTime.UtcNow.Add(duration);

            // Subscribe to events
            tracker.ReadinessChanged += (sender, e) =>
            {
                events.Add(new
                {
                    type = "component-changed",
                    componentId = e.ComponentId,
                    previousState = e.PreviousState.ToString(),
                    currentState = e.CurrentState.ToString(),
                    message = e.Result.Message,
                    timestamp = e.Timestamp
                });
            };

            // Simulate state changes
            var tasks = new List<Task>
            {
                SimulateModuleInitialization(tracker, "Module1", TimeSpan.FromMilliseconds(100)),
                SimulateModuleInitialization(tracker, "Module2", TimeSpan.FromMilliseconds(200)),
                SimulateModuleInitialization(tracker, "Module3", TimeSpan.FromMilliseconds(150))
            };

            await Task.WhenAll(tasks);

            // Add system state event
            var systemReadiness = tracker.GetSystemReadiness();
            events.Add(new
            {
                type = "system-state",
                overallState = systemReadiness.OverallState.ToString(),
                readyComponents = systemReadiness.ReadyComponents,
                totalComponents = systemReadiness.TotalComponents,
                isFullyReady = systemReadiness.IsFullyReady
            });

            return events;
        }

        /// <summary>
        /// Create a test scenario for concurrent readiness updates
        /// </summary>
        public static async Task TestConcurrentReadinessUpdates(ReadinessTracker tracker, int moduleCount, int updateCount)
        {
            // Register modules
            for (int i = 0; i < moduleCount; i++)
            {
                tracker.RegisterComponent($"Module{i}", "Module");
            }

            // Concurrent updates
            var tasks = new List<Task>();
            var random = new Random();

            for (int i = 0; i < updateCount; i++)
            {
                var moduleIndex = random.Next(moduleCount);
                var moduleName = $"Module{moduleIndex}";
                var state = (ReadinessState)random.Next(0, 5);
                var message = $"Update {i} for {moduleName}";

                tasks.Add(Task.Run(() =>
                {
                    tracker.UpdateReadiness(moduleName, new ReadinessResult
                    {
                        State = state,
                        Message = message
                    });
                }));
            }

            await Task.WhenAll(tasks);

            // Verify final state
            var systemReadiness = tracker.GetSystemReadiness();
            systemReadiness.TotalComponents.Should().Be(moduleCount);
        }

        /// <summary>
        /// Create a test scenario for timeout handling
        /// </summary>
        public static async Task TestReadinessTimeouts(ReadinessTracker tracker)
        {
            tracker.RegisterComponent("SlowModule", "Module");

            // Test component timeout
            var componentTimeout = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await tracker.WaitForComponentAsync("SlowModule", TimeSpan.FromMilliseconds(100));
            });

            componentTimeout.Message.Should().Contain("SlowModule");

            // Test system timeout
            var systemTimeout = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await tracker.WaitForSystemReadyAsync(TimeSpan.FromMilliseconds(100));
            });

            systemTimeout.Message.Should().Contain("System did not become fully ready");
        }
    }
}
