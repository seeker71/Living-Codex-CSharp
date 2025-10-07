using System;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Integration tests for the readiness system
    /// </summary>
    public class ReadinessIntegrationTests : ReadinessTestBase, IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _fixture;

        public ReadinessIntegrationTests(TestServerFixture fixture) : base(fixture.BaseUrl)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SystemReadiness_ShouldBeAvailable()
        {
            // Act
            var readiness = await GetSystemReadinessAsync();

            // Assert
            readiness.Should().NotBeNull();
            readiness.TotalComponents.Should().BeGreaterThan(0);
            readiness.Components.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SystemReadiness_ShouldTrackAllModules()
        {
            // Act
            var readiness = await GetSystemReadinessAsync();

            // Assert
            readiness.TotalComponents.Should().BeGreaterThan(50); // Should have many modules
            readiness.Components.Should().AllSatisfy(component =>
            {
                component.ComponentType.Should().Be("Module");
                component.ComponentId.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public async Task SystemReadiness_ShouldEventuallyBecomeReady()
        {
            // Act & Assert
            await WaitForSystemReadyAsync(TimeSpan.FromMinutes(3));
            
            var readiness = await GetSystemReadinessAsync();
            readiness.IsFullyReady.Should().BeTrue();
            readiness.FailedComponents.Should().Be(0);
        }

        [Fact]
        public async Task ComponentReadiness_ShouldBeTracked()
        {
            // Act
            var readiness = await GetSystemReadinessAsync();
            var firstComponent = readiness.Components.First();

            var componentReadiness = await GetComponentReadinessAsync(firstComponent.ComponentId);

            // Assert
            componentReadiness.Should().NotBeNull();
            componentReadiness.ComponentId.Should().Be(firstComponent.ComponentId);
            componentReadiness.ComponentType.Should().Be("Module");
        }

        [Fact]
        public async Task ReadinessEvents_ShouldBeStreamed()
        {
            // Act
            var events = await CollectReadinessEventsAsync(TimeSpan.FromSeconds(10));

            // Assert
            events.Should().NotBeEmpty();
            events.Should().Contain(e => e.ToString().Contains("system-state"));
        }

        [Fact]
        public async Task ModulesEndpoint_ShouldReturnAllModules()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness/modules");
            response.IsSuccessStatusCode.Should().BeTrue();

            var content = await response.Content.ReadAsStringAsync();
            var modules = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            modules.Should().NotBeNull();
            modules.Should().NotBeEmpty();
            modules.Should().AllSatisfy(module =>
            {
                module.ComponentType.Should().Be("Module");
                module.ComponentId.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public async Task ReadyEndpoints_ShouldReturnOnlyReadyComponents()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness/ready");
            response.IsSuccessStatusCode.Should().BeTrue();

            var content = await response.Content.ReadAsStringAsync();
            var readyComponents = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            readyComponents.Should().NotBeNull();
            readyComponents.Should().AllSatisfy(component =>
            {
                component.State.Should().Be(ReadinessState.Ready);
            });
        }

        [Fact]
        public async Task NotReadyEndpoints_ShouldReturnNotReadyComponents()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness/not-ready");
            response.IsSuccessStatusCode.Should().BeTrue();

            var content = await response.Content.ReadAsStringAsync();
            var notReadyComponents = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            notReadyComponents.Should().NotBeNull();
            notReadyComponents.Should().AllSatisfy(component =>
            {
                component.State.Should().NotBe(ReadinessState.Ready);
            });
        }

        [Fact]
        public async Task WaitForComponent_ShouldTimeoutForNonExistentComponent()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await WaitForComponentReadyAsync("NonExistentModule", TimeSpan.FromSeconds(2));
            });

            exception.Message.Should().Contain("NonExistentModule");
        }

        [Fact]
        public async Task HealthEndpoint_ShouldAlwaysBeAvailable()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue("Health endpoint should always be available");
        }

        [Fact]
        public async Task ReadinessEndpoints_ShouldAlwaysBeAvailable()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue("Readiness endpoints should always be available");
        }
    }
}


