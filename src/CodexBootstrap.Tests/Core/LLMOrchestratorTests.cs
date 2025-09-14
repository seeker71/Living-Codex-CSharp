using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace CodexBootstrap.Tests.Core
{
    public class LLMOrchestratorTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<CodexBootstrap.Core.LLMClient> _mockLLMClient;
        private readonly LLMOrchestrator _orchestrator;

        public LLMOrchestratorTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLLMClient = new Mock<CodexBootstrap.Core.LLMClient>();
            
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
            
            _orchestrator = new LLMOrchestrator(_mockLLMClient.Object, _mockLoggerFactory.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidPrompt_ShouldReturnSuccessResponse()
        {
            // Arrange
            var prompt = "Test prompt";
            var expectedResponse = "Test response";
            var mockLLMResponse = new LLMResponse
            {
                Response = expectedResponse,
                Done = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _mockLLMClient.Setup(x => x.GenerateAsync(It.IsAny<string>()))
                .ReturnsAsync(mockLLMResponse);

            // Act
            var result = await _orchestrator.ExecuteAsync(prompt);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().Be(expectedResponse);
            _mockLLMClient.Verify(x => x.GenerateAsync(prompt), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithLLMException_ShouldReturnErrorResponse()
        {
            // Arrange
            var prompt = "Test prompt";
            var exception = new Exception("LLM service unavailable");

            _mockLLMClient.Setup(x => x.GenerateAsync(It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _orchestrator.ExecuteAsync(prompt);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("LLM service unavailable");
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_ShouldReturnErrorResponse()
        {
            // Arrange
            var prompt = "Test prompt";
            var timeoutException = new TimeoutException("Request timed out");

            _mockLLMClient.Setup(x => x.GenerateAsync(It.IsAny<string>()))
                .ThrowsAsync(timeoutException);

            // Act
            var result = await _orchestrator.ExecuteAsync(prompt);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Request timed out");
        }

        [Fact]
        public async Task ParseStructuredResponse_WithValidJson_ShouldParseSuccessfully()
        {
            // Arrange
            var jsonResponse = """{"concepts": [{"name": "AI", "score": 0.9}]}""";
            var mockLLMResponse = new LLMResponse
            {
                Response = jsonResponse,
                Done = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _mockLLMClient.Setup(x => x.GenerateAsync(It.IsAny<string>()))
                .ReturnsAsync(mockLLMResponse);

            // Act
            var result = await _orchestrator.ParseStructuredResponse<ConceptExtractionResult>(jsonResponse);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task ParseStructuredResponse_WithInvalidJson_ShouldReturnError()
        {
            // Arrange
            var invalidJson = "invalid json {";

            // Act
            var result = await _orchestrator.ParseStructuredResponse<ConceptExtractionResult>(invalidJson);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("JSON parsing failed");
        }

        [Fact]
        public async Task ParseStructuredResponse_WithMarkdownWrappedJson_ShouldExtractJson()
        {
            // Arrange
            var markdownJson = """
                Here's the JSON response:
                ```json
                {"concepts": [{"name": "AI", "score": 0.9}]}
                ```
                That's the result.
                """;

            // Act
            var result = await _orchestrator.ParseStructuredResponse<ConceptExtractionResult>(markdownJson);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task ParseStructuredResponse_WithEmptyResponse_ShouldReturnError()
        {
            // Arrange
            var emptyResponse = "";

            // Act
            var result = await _orchestrator.ParseStructuredResponse<ConceptExtractionResult>(emptyResponse);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Empty response");
        }

        [Fact]
        public async Task ParseStructuredResponse_WithNullResponse_ShouldReturnError()
        {
            // Arrange
            string nullResponse = null;

            // Act
            var result = await _orchestrator.ParseStructuredResponse<ConceptExtractionResult>(nullResponse);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Null response");
        }
    }
}
