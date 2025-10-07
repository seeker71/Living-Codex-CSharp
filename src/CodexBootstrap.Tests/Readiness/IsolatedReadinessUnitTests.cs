using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Completely isolated unit tests for readiness system components
    /// These tests run in complete isolation and don't share any state
    /// </summary>
    [Collection("IsolatedReadinessTests")]
    public class IsolatedReadinessUnitTests
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
            systemReadiness.TotalComponents.Should().Be(2);
            systemReadiness.ReadyComponents.Should().Be(1);
            systemReadiness.OverallState.Should().Be(ReadinessState.Initializing);
        }

        [Fact]
        public void ReadinessResult_ShouldCreateCorrectStates()
        {
            // Arrange & Act
            var notStarted = ReadinessResult.NotStarted("Not started");
            var initializing = ReadinessResult.Initializing("Initializing");
            var ready = ReadinessResult.Success("Ready");
            var degraded = ReadinessResult.Degraded("Degraded");
            var failed = ReadinessResult.Failed("Failed");

            // Assert
            notStarted.State.Should().Be(ReadinessState.NotStarted);
            initializing.State.Should().Be(ReadinessState.Initializing);
            ready.State.Should().Be(ReadinessState.Ready);
            degraded.State.Should().Be(ReadinessState.Degraded);
            failed.State.Should().Be(ReadinessState.Failed);
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
    }
}


