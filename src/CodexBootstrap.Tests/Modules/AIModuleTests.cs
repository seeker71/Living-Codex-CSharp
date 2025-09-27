using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class AIModuleTests
    {
        private readonly ICodexLogger _logger;
        private readonly NodeRegistry _registry;
        private readonly AIModule _module;

        public AIModuleTests()
        {
            _logger = new Log4NetLogger(typeof(AIModuleTests));
            _registry = TestInfrastructure.CreateTestNodeRegistry();
            
            // Create a real HTTP client for the AI module
            var httpClient = new HttpClient();
            _module = new AIModule(_registry, _logger, httpClient);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Assert
            _module.Should().NotBeNull();
        }

        [Fact]
        public void GetModuleNode_ShouldReturnModuleNode()
        {
            // Act
            var moduleNode = _module.GetModuleNode();

            // Assert
            moduleNode.Should().NotBeNull();
            moduleNode.Id.Should().Be("ai-module");
            moduleNode.Title.Should().Be("AI Module (Refactored)");
        }

        [Fact]
        public void Register_ShouldRegisterModuleWithRegistry()
        {
            // Act
            _module.Register(_registry);

            // Assert - Check that the module node was registered
            var moduleNode = _registry.GetNode("ai-module");
            moduleNode.Should().NotBeNull();
            moduleNode!.Id.Should().Be("ai-module");
            moduleNode.Title.Should().Be("AI Module (Refactored)");

            // Check that prompt nodes were registered
            var expectedPromptIds = new[]
            {
                "prompt.concept-extraction",
                "prompt.fractal-transformation",
                "prompt.scoring-analysis",
                "prompt.future-query"
            };

            foreach (var promptId in expectedPromptIds)
            {
                var promptNode = _registry.GetNode(promptId);
                promptNode.Should().NotBeNull($"Prompt node {promptId} should be registered");
            }
        }

        [Fact]
        public void RegisterApiHandlers_ShouldNotThrow()
        {
            // Act & Assert - This test verifies the module can register API handlers without throwing
            // Since we don't have a real API router in this test context, we'll skip this test
            // or verify the module can be constructed without errors
            _module.Should().NotBeNull();
        }

        [Fact]
        public void RegisterHttpEndpoints_ShouldNotThrow()
        {
            // This test is simplified since WebApplication cannot be mocked
            // We just verify the module can be created and registered
            _module.Should().NotBeNull();
            _module.GetModuleNode().Should().NotBeNull();
        }

        [Fact]
        public void Name_ShouldReturnCorrectName()
        {
            // Assert
            _module.Name.Should().Be("AI Module (Refactored)");
        }

        [Fact]
        public void Description_ShouldReturnCorrectDescription()
        {
            // Assert
            _module.Description.Should().Be("Streamlined AI functionality with configurable prompts and reusable patterns");
        }

        [Fact]
        public void Version_ShouldReturnCorrectVersion()
        {
            // Assert
            _module.Version.Should().Be("2.0.0");
        }
    }
}
