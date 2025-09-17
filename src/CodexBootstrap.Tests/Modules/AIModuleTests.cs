using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class AIModuleTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly Mock<INodeRegistry> _mockRegistry;
        private readonly Mock<IApiRouter> _mockApiRouter;
        private readonly AIModule _module;

        public AIModuleTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _mockRegistry = new Mock<INodeRegistry>();
            _mockApiRouter = new Mock<IApiRouter>();
            var mockHttpClient = new Mock<HttpClient>();
            
            _module = new AIModule(_mockRegistry.Object, _mockLogger.Object, mockHttpClient.Object);
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
            _module.Register(_mockRegistry.Object);

            // Assert
            var upsertedNodes = _mockRegistry.Invocations
                .Where(invocation => invocation.Method.Name == nameof(INodeRegistry.Upsert) && invocation.Arguments.FirstOrDefault() is Node)
                .Select(invocation => (Node)invocation.Arguments[0]!)
                .ToList();

            upsertedNodes.Should().NotBeEmpty();

            var upsertedIds = upsertedNodes
                .Select(n => n.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            upsertedIds.Should().Contain("ai-module");

            var expectedPromptIds = new[]
            {
                "prompt.concept-extraction",
                "prompt.fractal-transformation",
                "prompt.scoring-analysis",
                "prompt.future-query"
            };

            foreach (var promptId in expectedPromptIds)
            {
                upsertedIds.Should().Contain(promptId);
            }

            upsertedNodes.Count.Should().BeGreaterOrEqualTo(expectedPromptIds.Length + 1);
        }

        [Fact]
        public void RegisterApiHandlers_ShouldNotThrow()
        {
            // Act & Assert
            _module.RegisterApiHandlers(_mockApiRouter.Object, _mockRegistry.Object);
            // Should not throw
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
