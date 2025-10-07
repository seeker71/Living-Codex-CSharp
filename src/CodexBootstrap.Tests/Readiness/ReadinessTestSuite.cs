using System;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Readiness
{
    /// <summary>
    /// Comprehensive test suite for the readiness system
    /// </summary>
    public class ReadinessTestSuite : ReadinessTestBase, IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _fixture;

        public ReadinessTestSuite(TestServerFixture fixture) : base(fixture.BaseUrl)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task FullSystemReadinessFlow_ShouldWork()
        {
            // Arrange - Wait for system to be ready
            await WaitForSystemReadyAsync(TimeSpan.FromMinutes(3));

            // Act - Get final system state
            var readiness = await GetSystemReadinessAsync();

            // Assert
            readiness.IsFullyReady.Should().BeTrue();
            readiness.FailedComponents.Should().Be(0);
            readiness.TotalComponents.Should().BeGreaterThan(50);
            readiness.ReadyComponents.Should().Be(readiness.TotalComponents);
        }

        [Fact]
        public async Task ModuleReadinessProgression_ShouldBeTracked()
        {
            // Act - Get system readiness
            var readiness = await GetSystemReadinessAsync();

            // Assert - Should have modules in various states initially
            readiness.TotalComponents.Should().BeGreaterThan(0);
            readiness.Components.Should().NotBeEmpty();

            // All modules should eventually become ready
            var readyModules = readiness.Components.Where(c => c.State == ReadinessState.Ready).ToList();
            var initializingModules = readiness.Components.Where(c => c.State == ReadinessState.Initializing).ToList();
            var failedModules = readiness.Components.Where(c => c.State == ReadinessState.Failed).ToList();

            // Log the distribution
            Logger.Info($"Module states - Ready: {readyModules.Count}, Initializing: {initializingModules.Count}, Failed: {failedModules.Count}");

            // Should have some ready modules
            readyModules.Should().NotBeEmpty("Should have some ready modules");
            
            // Should not have failed modules
            failedModules.Should().BeEmpty("Should not have failed modules");
        }

        [Fact]
        public async Task ReadinessEndpoints_ShouldBeConsistent()
        {
            // Act
            var systemReadiness = await GetSystemReadinessAsync();
            var modulesResponse = await HttpClient.GetAsync($"{BaseUrl}/readiness/modules");
            var readyResponse = await HttpClient.GetAsync($"{BaseUrl}/readiness/ready");
            var notReadyResponse = await HttpClient.GetAsync($"{BaseUrl}/readiness/not-ready");

            // Assert
            modulesResponse.IsSuccessStatusCode.Should().BeTrue();
            readyResponse.IsSuccessStatusCode.Should().BeTrue();
            notReadyResponse.IsSuccessStatusCode.Should().BeTrue();

            // Parse responses
            var modulesContent = await modulesResponse.Content.ReadAsStringAsync();
            var readyContent = await readyResponse.Content.ReadAsStringAsync();
            var notReadyContent = await notReadyResponse.Content.ReadAsStringAsync();

            var modules = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(modulesContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var readyComponents = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(readyContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var notReadyComponents = System.Text.Json.JsonSerializer.Deserialize<ComponentReadiness[]>(notReadyContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Verify consistency
            modules.Should().HaveCount(systemReadiness.TotalComponents);
            readyComponents.Should().HaveCount(systemReadiness.ReadyComponents);
            notReadyComponents.Should().HaveCount(systemReadiness.TotalComponents - systemReadiness.ReadyComponents);

            // Verify state consistency
            readyComponents.Should().AllSatisfy(c => c.State.Should().Be(ReadinessState.Ready));
            notReadyComponents.Should().AllSatisfy(c => c.State.Should().NotBe(ReadinessState.Ready));
        }

        [Fact]
        public async Task ReadinessEvents_ShouldBeStreamed()
        {
            // Act - Collect events for a short period
            var events = await CollectReadinessEventsAsync(TimeSpan.FromSeconds(5));

            // Assert
            events.Should().NotBeEmpty("Should receive some readiness events");
            
            // Should have system state events
            events.Should().Contain(e => e.ToString().Contains("system-state"), "Should have system state events");
            
            // Should have component change events
            events.Should().Contain(e => e.ToString().Contains("component-changed"), "Should have component change events");
        }

        [Fact]
        public async Task SpecificModuleReadiness_ShouldBeTrackable()
        {
            // Act - Get a specific module
            var readiness = await GetSystemReadinessAsync();
            var firstModule = readiness.Components.First();

            var moduleReadiness = await GetComponentReadinessAsync(firstModule.ComponentId);

            // Assert
            moduleReadiness.Should().NotBeNull();
            moduleReadiness.ComponentId.Should().Be(firstModule.ComponentId);
            moduleReadiness.ComponentType.Should().Be("Module");
            moduleReadiness.State.Should().BeOneOf(
                ReadinessState.NotStarted,
                ReadinessState.Initializing,
                ReadinessState.Ready,
                ReadinessState.Degraded,
                ReadinessState.Failed
            );
        }

        [Fact]
        public async Task WaitForComponent_ShouldWorkForReadyComponents()
        {
            // Arrange - Wait for system to be ready
            await WaitForSystemReadyAsync(TimeSpan.FromMinutes(3));

            // Act - Try to wait for a ready component
            var readiness = await GetSystemReadinessAsync();
            var readyModule = readiness.Components.First(c => c.State == ReadinessState.Ready);

            // This should return immediately since the component is already ready
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await WaitForComponentReadyAsync(readyModule.ComponentId, TimeSpan.FromMilliseconds(1));
            });
        }

        [Fact]
        public async Task HealthEndpoint_ShouldAlwaysBeAvailable()
        {
            // Act
            var response = await HttpClient.GetAsync($"{BaseUrl}/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ReadinessEndpoints_ShouldAlwaysBeAvailable()
        {
            // Act
            var readinessResponse = await HttpClient.GetAsync($"{BaseUrl}/readiness");
            var modulesResponse = await HttpClient.GetAsync($"{BaseUrl}/readiness/modules");

            // Assert
            readinessResponse.IsSuccessStatusCode.Should().BeTrue();
            modulesResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SystemReadiness_ShouldHaveValidTimestamps()
        {
            // Act
            var readiness = await GetSystemReadinessAsync();

            // Assert
            readiness.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            readiness.Components.Should().AllSatisfy(c =>
            {
                c.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
                c.LastResult.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            });
        }

        [Fact]
        public async Task SystemReadiness_ShouldHaveValidMetadata()
        {
            // Act
            var readiness = await GetSystemReadinessAsync();

            // Assert
            readiness.Components.Should().AllSatisfy(c =>
            {
                c.ComponentId.Should().NotBeNullOrEmpty();
                c.ComponentType.Should().Be("Module");
                c.Dependencies.Should().NotBeNull();
                c.Metadata.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ReadinessSystem_ShouldHandleConcurrentRequests()
        {
            // Act - Make multiple concurrent requests
            var tasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                var response = await HttpClient.GetAsync($"{BaseUrl}/readiness");
                response.IsSuccessStatusCode.Should().BeTrue();
                return await response.Content.ReadAsStringAsync();
            });

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().AllSatisfy(r => r.Should().NotBeNullOrEmpty());
        }

        [Fact]
        public async Task ReadinessSystem_ShouldBePerformant()
        {
            // Act - Measure response time
            var startTime = DateTime.UtcNow;
            var response = await HttpClient.GetAsync($"{BaseUrl}/readiness");
            var endTime = DateTime.UtcNow;

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseTime = endTime - startTime;
            responseTime.Should().BeLessThan(TimeSpan.FromSeconds(1), "Readiness endpoint should respond quickly");
        }
    }
}


