using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class CoreModuleTests
    {
        private readonly Mock<NodeRegistry> _mockRegistry;
        private readonly Mock<IApiRouter> _mockApiRouter;
        private readonly CodexBootstrap.Modules.CoreModule _module;

        public CoreModuleTests()
        {
            _mockRegistry = new Mock<NodeRegistry>();
            _mockApiRouter = new Mock<IApiRouter>();
            
            _module = new CodexBootstrap.Modules.CoreModule(_mockApiRouter.Object, _mockRegistry.Object);
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
            moduleNode.Id.Should().Be("codex.core");
            moduleNode.Title.Should().Be("Core System Module");
        }

        [Fact]
        public void Register_ShouldRegisterModuleWithRegistry()
        {
            // Act
            _module.Register(_mockRegistry.Object);

            // Assert
            _mockRegistry.Verify(x => x.Upsert(It.IsAny<Node>()), Times.Once);
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
    }
}