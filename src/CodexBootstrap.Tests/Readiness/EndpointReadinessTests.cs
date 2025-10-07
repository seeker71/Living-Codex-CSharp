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
    /// Tests for endpoint readiness tracking
    /// </summary>
    public class EndpointReadinessTests : TestBase
    {
        private readonly ReadinessTracker _readinessTracker;
        private readonly EndpointReadinessTracker _endpointTracker;

        public EndpointReadinessTests()
        {
            _readinessTracker = new ReadinessTracker(Logger);
            _endpointTracker = new EndpointReadinessTracker(_readinessTracker, Logger);
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldStartEmpty()
        {
            // Act
            var endpoints = _endpointTracker.GetAllEndpoints();

            // Assert
            endpoints.Should().BeEmpty();
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldRegisterEndpoints()
        {
            // Act
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/news/sources", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/ai/generate", "AIModule");

            var endpoints = _endpointTracker.GetAllEndpoints();

            // Assert
            endpoints.Should().HaveCount(3);
            endpoints.Should().Contain(e => e.ComponentId == "/api/news/latest");
            endpoints.Should().Contain(e => e.ComponentId == "/api/news/sources");
            endpoints.Should().Contain(e => e.ComponentId == "/api/ai/generate");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldTrackModuleDependencies()
        {
            // Act
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            var endpoint = _endpointTracker.GetEndpointReadiness("/api/news/latest");

            // Assert
            endpoint.Should().NotBeNull();
            endpoint.ComponentType.Should().Be("Endpoint");
            endpoint.Dependencies.Should().Contain("NewsModule");
            endpoint.Metadata.Should().ContainKey("module");
            endpoint.Metadata["module"].Should().Be("NewsModule");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldUpdateBasedOnModuleState()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _readinessTracker.RegisterComponent("NewsModule", "Module");

            // Act
            _readinessTracker.UpdateReadiness("NewsModule", ReadinessResult.Initializing("Starting"));
            _endpointTracker.UpdateModuleEndpoints("NewsModule", ReadinessState.Initializing, "Module starting");

            var endpoint = _endpointTracker.GetEndpointReadiness("/api/news/latest");

            // Assert
            endpoint.State.Should().Be(ReadinessState.Initializing);
            endpoint.LastResult.Message.Should().Contain("Module starting");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldReflectModuleReadiness()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _readinessTracker.RegisterComponent("NewsModule", "Module");

            // Act
            _readinessTracker.UpdateReadiness("NewsModule", ReadinessResult.Success("Ready"));
            _endpointTracker.UpdateModuleEndpoints("NewsModule", ReadinessState.Ready, "Module ready");

            var endpoint = _endpointTracker.GetEndpointReadiness("/api/news/latest");

            // Assert
            endpoint.State.Should().Be(ReadinessState.Ready);
            endpoint.LastResult.Message.Should().Contain("Module ready");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldHandleModuleFailure()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _readinessTracker.RegisterComponent("NewsModule", "Module");

            // Act
            _readinessTracker.UpdateReadiness("NewsModule", ReadinessResult.Failed("Configuration error"));
            _endpointTracker.UpdateModuleEndpoints("NewsModule", ReadinessState.Failed, "Module failed");

            var endpoint = _endpointTracker.GetEndpointReadiness("/api/news/latest");

            // Assert
            endpoint.State.Should().Be(ReadinessState.Failed);
            endpoint.LastResult.Message.Should().Contain("Module failed");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldFilterByModule()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/news/sources", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/ai/generate", "AIModule");

            // Act
            var newsEndpoints = _endpointTracker.GetEndpointsByModule("NewsModule");
            var aiEndpoints = _endpointTracker.GetEndpointsByModule("AIModule");

            // Assert
            newsEndpoints.Should().HaveCount(2);
            newsEndpoints.Should().AllSatisfy(e => e.Metadata["module"].Should().Be("NewsModule"));
            
            aiEndpoints.Should().HaveCount(1);
            aiEndpoints.Should().AllSatisfy(e => e.Metadata["module"].Should().Be("AIModule"));
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldFilterByState()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/ai/generate", "AIModule");
            _readinessTracker.RegisterComponent("NewsModule", "Module");
            _readinessTracker.RegisterComponent("AIModule", "Module");

            // Act
            _readinessTracker.UpdateReadiness("NewsModule", ReadinessResult.Success("Ready"));
            _readinessTracker.UpdateReadiness("AIModule", ReadinessResult.Initializing("Starting"));
            
            _endpointTracker.UpdateModuleEndpoints("NewsModule", ReadinessState.Ready, "Ready");
            _endpointTracker.UpdateModuleEndpoints("AIModule", ReadinessState.Initializing, "Starting");

            var readyEndpoints = _endpointTracker.GetReadyEndpoints();
            var notReadyEndpoints = _endpointTracker.GetNotReadyEndpoints();

            // Assert
            readyEndpoints.Should().HaveCount(1);
            readyEndpoints.Should().AllSatisfy(e => e.State.Should().Be(ReadinessState.Ready));
            
            notReadyEndpoints.Should().HaveCount(1);
            notReadyEndpoints.Should().AllSatisfy(e => e.State.Should().NotBe(ReadinessState.Ready));
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldAutoRegisterModuleEndpoints()
        {
            // Arrange
            var endpoints = new List<string>
            {
                "/api/news/latest",
                "/api/news/sources",
                "/api/news/health"
            };

            // Act
            _endpointTracker.AutoRegisterModuleEndpoints("NewsModule", endpoints);
            var registeredEndpoints = _endpointTracker.GetEndpointsByModule("NewsModule");

            // Assert
            registeredEndpoints.Should().HaveCount(3);
            registeredEndpoints.Should().AllSatisfy(e => e.Metadata["module"].Should().Be("NewsModule"));
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldHandleUnknownEndpoint()
        {
            // Act
            var endpoint = _endpointTracker.GetEndpointReadiness("/api/unknown");

            // Assert
            endpoint.Should().BeNull();
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldHandleUnknownModule()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/test", "TestModule");

            // Act
            _endpointTracker.UpdateModuleEndpoints("UnknownModule", ReadinessState.Ready, "Ready");

            var endpoint = _endpointTracker.GetEndpointReadiness("/api/test");

            // Assert
            endpoint.State.Should().Be(ReadinessState.NotStarted); // Should remain unchanged
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldMaintainEndpointMetadata()
        {
            // Act
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            var endpoint = _endpointTracker.GetEndpointReadiness("/api/news/latest");

            // Assert
            endpoint.Metadata.Should().ContainKey("module");
            endpoint.Metadata.Should().ContainKey("path");
            endpoint.Metadata["module"].Should().Be("NewsModule");
            endpoint.Metadata["path"].Should().Be("/api/news/latest");
        }

        [Fact]
        public void EndpointReadinessTracker_ShouldUpdateMultipleEndpointsForModule()
        {
            // Arrange
            _endpointTracker.RegisterEndpoint("/api/news/latest", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/news/sources", "NewsModule");
            _endpointTracker.RegisterEndpoint("/api/news/health", "NewsModule");
            _readinessTracker.RegisterComponent("NewsModule", "Module");

            // Act
            _readinessTracker.UpdateReadiness("NewsModule", ReadinessResult.Success("Ready"));
            _endpointTracker.UpdateModuleEndpoints("NewsModule", ReadinessState.Ready, "All news endpoints ready");

            var newsEndpoints = _endpointTracker.GetEndpointsByModule("NewsModule");

            // Assert
            newsEndpoints.Should().HaveCount(3);
            newsEndpoints.Should().AllSatisfy(e => e.State.Should().Be(ReadinessState.Ready));
            newsEndpoints.Should().AllSatisfy(e => e.LastResult.Message.Should().Contain("All news endpoints ready"));
        }
    }
}


