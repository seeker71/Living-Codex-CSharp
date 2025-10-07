using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Unit tests for readiness system components
    /// These tests are completely isolated and don't share any state
    /// </summary>
    [Collection("IsolatedReadinessTests")]
    public class ReadinessUnitTests
    {
        [Fact]
        public void ReadinessTracker_ShouldStartEmpty()
        {
            // Arrange - Create completely isolated instances with mock logger
            var mockLogger = new MockLogger();
            var tracker = new ReadinessTracker(mockLogger);

            // Act
            var systemReadiness = tracker.GetSystemReadiness();

            // Assert
            systemReadiness.TotalComponents.Should().Be(0);
            systemReadiness.ReadyComponents.Should().Be(0);
            systemReadiness.OverallState.Should().Be(ReadinessState.NotStarted);
        }

        // Mock logger for complete isolation
        private class MockLogger : ICodexLogger
        {
            public void Debug(string message) { }
            public void Debug(string message, Exception exception) { }
            public void Info(string message) { }
            public void Info(string message, Exception exception) { }
            public void Warn(string message) { }
            public void Warn(string message, Exception exception) { }
            public void Error(string message) { }
            public void Error(string message, Exception exception) { }
            public void Fatal(string message) { }
            public void Fatal(string message, Exception exception) { }
        }

        [Fact]
        public void ReadinessTracker_ShouldRegisterComponents()
        {
            // Arrange
            var mockLogger = new MockLogger();
            var tracker = new ReadinessTracker(mockLogger);

            // Act
            tracker.RegisterComponent("TestModule", "Module", new List<string> { "Dependency" });
            var systemReadiness = tracker.GetSystemReadiness();

            // Assert
            systemReadiness.TotalComponents.Should().Be(1);
            systemReadiness.Components.Should().HaveCount(1);
            systemReadiness.Components.First().ComponentId.Should().Be("TestModule");
            systemReadiness.Components.First().ComponentType.Should().Be("Module");
        }

        [Fact]
        public void ReadinessTracker_ShouldUpdateComponentState()
        {
            // Arrange
            var mockLogger = new MockLogger();
            var tracker = new ReadinessTracker(mockLogger);
            tracker.RegisterComponent("TestModule", "Module");

            // Act
            tracker.UpdateReadiness("TestModule", ReadinessResult.Initializing("Starting up"));
            var component = tracker.GetComponentReadiness("TestModule");

            // Assert
            component.Should().NotBeNull();
            component.State.Should().Be(ReadinessState.Initializing);
            component.LastResult.Message.Should().Be("Starting up");
        }

        [Fact]
        public void ReadinessTracker_ShouldCalculateOverallState()
        {
            // Arrange
            var mockLogger = new MockLogger();
            var tracker = new ReadinessTracker(mockLogger);
            tracker.RegisterComponent("Module1", "Module");
            tracker.RegisterComponent("Module2", "Module");

            // Act
            tracker.UpdateReadiness("Module1", ReadinessResult.Success("Ready"));
            tracker.UpdateReadiness("Module2", ReadinessResult.Initializing("Starting"));

            var systemReadiness = tracker.GetSystemReadiness();

            // Assert
            systemReadiness.OverallState.Should().Be(ReadinessState.Initializing);
            systemReadiness.ReadyComponents.Should().Be(1);
            systemReadiness.InitializingComponents.Should().Be(1);
            systemReadiness.IsFullyReady.Should().BeFalse();
        }

        [Fact]
        public void ReadinessTracker_ShouldDetectFullyReady()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(ReadinessTracker));
            var tracker = new ReadinessTracker(logger);
            tracker.RegisterComponent("Module1", "Module");
            tracker.RegisterComponent("Module2", "Module");

            // Act
            tracker.UpdateReadiness("Module1", ReadinessResult.Success("Ready"));
            tracker.UpdateReadiness("Module2", ReadinessResult.Success("Ready"));

            var systemReadiness = tracker.GetSystemReadiness();

            // Assert
            systemReadiness.OverallState.Should().Be(ReadinessState.Ready);
            systemReadiness.ReadyComponents.Should().Be(2);
            systemReadiness.IsFullyReady.Should().BeTrue();
        }

        [Fact]
        public void ReadinessTracker_ShouldDetectFailedState()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(ReadinessTracker));
            var tracker = new ReadinessTracker(logger);
            tracker.RegisterComponent("Module1", "Module");
            tracker.RegisterComponent("Module2", "Module");

            // Act
            tracker.UpdateReadiness("Module1", ReadinessResult.Success("Ready"));
            tracker.UpdateReadiness("Module2", ReadinessResult.Failed("Error"));

            var systemReadiness = tracker.GetSystemReadiness();

            // Assert
            systemReadiness.OverallState.Should().Be(ReadinessState.Failed);
            systemReadiness.ReadyComponents.Should().Be(1);
            systemReadiness.FailedComponents.Should().Be(1);
            systemReadiness.IsFullyReady.Should().BeFalse();
        }

        [Fact]
        public async Task ReadinessTracker_ShouldWaitForComponent()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(ReadinessTracker));
            var tracker = new ReadinessTracker(logger);
            tracker.RegisterComponent("TestModule", "Module");
            var waitTask = tracker.WaitForComponentAsync("TestModule", TimeSpan.FromSeconds(5));

            // Act
            await Task.Delay(100); // Small delay to ensure wait task is started
            tracker.UpdateReadiness("TestModule", ReadinessResult.Success("Ready"));
            var result = await waitTask;

            // Assert
            result.State.Should().Be(ReadinessState.Ready);
            result.Message.Should().Be("Ready");
        }

        [Fact]
        public async Task ReadinessTracker_ShouldTimeoutWaitingForComponent()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(ReadinessTracker));
            var tracker = new ReadinessTracker(logger);
            tracker.RegisterComponent("TestModule", "Module");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await tracker.WaitForComponentAsync("TestModule", TimeSpan.FromMilliseconds(100));
            });

            exception.Message.Should().Contain("TestModule");
        }

        [Fact]
        public async Task ReadinessTracker_ShouldWaitForSystemReady()
        {
            // Arrange
            var logger = new Log4NetLogger(typeof(ReadinessTracker));
            var tracker = new ReadinessTracker(logger);
            tracker.RegisterComponent("Module1", "Module");
            tracker.RegisterComponent("Module2", "Module");
            var waitTask = tracker.WaitForSystemReadyAsync(TimeSpan.FromSeconds(5));

            // Act
            await Task.Delay(100); // Small delay to ensure wait task is started
            tracker.UpdateReadiness("Module1", ReadinessResult.Success("Ready"));
            tracker.UpdateReadiness("Module2", ReadinessResult.Success("Ready"));
            var result = await waitTask;

            // Assert
            result.IsFullyReady.Should().BeTrue();
            result.ReadyComponents.Should().Be(2);
        }

        [Fact]
        public void ReadinessResult_ShouldCreateSuccessResult()
        {
            // Act
            var result = ReadinessResult.Success("Test message");

            // Assert
            result.State.Should().Be(ReadinessState.Ready);
            result.Message.Should().Be("Test message");
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void ReadinessResult_ShouldCreateFailedResult()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");

            // Act
            var result = ReadinessResult.Failed("Test message", exception);

            // Assert
            result.State.Should().Be(ReadinessState.Failed);
            result.Message.Should().Be("Test message");
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void ReadinessResult_ShouldCreateInitializingResult()
        {
            // Act
            var result = ReadinessResult.Initializing("Test message");

            // Assert
            result.State.Should().Be(ReadinessState.Initializing);
            result.Message.Should().Be("Test message");
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void ReadinessResult_ShouldCreateDegradedResult()
        {
            // Act
            var result = ReadinessResult.Degraded("Test message");

            // Assert
            result.State.Should().Be(ReadinessState.Degraded);
            result.Message.Should().Be("Test message");
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void ComponentReadiness_ShouldInitializeCorrectly()
        {
            // Act
            var component = new ComponentReadiness
            {
                ComponentId = "TestComponent",
                ComponentType = "Module",
                State = ReadinessState.Ready,
                Dependencies = new List<string> { "Dependency1" },
                Metadata = new Dictionary<string, object> { ["key"] = "value" }
            };

            // Assert
            component.ComponentId.Should().Be("TestComponent");
            component.ComponentType.Should().Be("Module");
            component.State.Should().Be(ReadinessState.Ready);
            component.Dependencies.Should().Contain("Dependency1");
            component.Metadata.Should().ContainKey("key");
        }

        [Fact]
        public void SystemReadiness_ShouldCalculateCorrectly()
        {
            // Arrange
            var components = new List<ComponentReadiness>
            {
                new() { ComponentId = "Module1", State = ReadinessState.Ready },
                new() { ComponentId = "Module2", State = ReadinessState.Initializing },
                new() { ComponentId = "Module3", State = ReadinessState.Failed },
                new() { ComponentId = "Module4", State = ReadinessState.Degraded }
            };

            // Act
            var systemReadiness = new SystemReadiness
            {
                Components = components,
                TotalComponents = components.Count,
                ReadyComponents = components.Count(c => c.State == ReadinessState.Ready),
                InitializingComponents = components.Count(c => c.State == ReadinessState.Initializing),
                FailedComponents = components.Count(c => c.State == ReadinessState.Failed),
                DegradedComponents = components.Count(c => c.State == ReadinessState.Degraded),
                OverallState = ReadinessState.Failed // Because there are failed components
            };

            // Assert
            systemReadiness.TotalComponents.Should().Be(4);
            systemReadiness.ReadyComponents.Should().Be(1);
            systemReadiness.InitializingComponents.Should().Be(1);
            systemReadiness.FailedComponents.Should().Be(1);
            systemReadiness.DegradedComponents.Should().Be(1);
            systemReadiness.OverallState.Should().Be(ReadinessState.Failed);
            systemReadiness.IsFullyReady.Should().BeFalse();
        }
    }
}
