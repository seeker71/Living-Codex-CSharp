using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace CodexBootstrap.Tests.Modules
{
    public class AIModuleTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<CodexBootstrap.Core.LLMOrchestrator> _mockOrchestrator;
        private readonly AIModule _aiModule;

        public AIModuleTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockOrchestrator = new Mock<CodexBootstrap.Core.LLMOrchestrator>();
            
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
            
            _aiModule = new AIModule(_mockOrchestrator.Object, _mockLoggerFactory.Object);
        }

        [Fact]
        public async Task ExtractConceptsAsync_WithValidInput_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new ConceptExtractionRequest
            {
                Text = "Artificial intelligence is transforming the world",
                MaxConcepts = 5
            };

            var expectedConcepts = new List<ConceptScore>
            {
                new() { Name = "Artificial Intelligence", Score = 0.9, Category = "Technology" },
                new() { Name = "Transformation", Score = 0.7, Category = "Process" }
            };

            var mockResponse = new LLMResponse<ConceptExtractionResult>
            {
                Success = true,
                Data = new ConceptExtractionResult
                {
                    Concepts = expectedConcepts,
                    ProcessingTime = 1.5,
                    Confidence = 0.85
                }
            };

            _mockOrchestrator.Setup(x => x.ExecuteAsync<ConceptExtractionResult>(
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiModule.ExtractConceptsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Concepts.Should().HaveCount(2);
            result.Data.Concepts.Should().Contain(expectedConcepts[0]);
            result.Data.Concepts.Should().Contain(expectedConcepts[1]);
        }

        [Fact]
        public async Task ExtractConceptsAsync_WithEmptyText_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ConceptExtractionRequest
            {
                Text = "",
                MaxConcepts = 5
            };

            // Act
            var result = await _aiModule.ExtractConceptsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Text cannot be empty");
        }

        [Fact]
        public async Task ExtractConceptsAsync_WithNullText_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ConceptExtractionRequest
            {
                Text = null,
                MaxConcepts = 5
            };

            // Act
            var result = await _aiModule.ExtractConceptsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Text cannot be null");
        }

        [Fact]
        public async Task ExtractConceptsAsync_WithOrchestratorException_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ConceptExtractionRequest
            {
                Text = "Test text",
                MaxConcepts = 5
            };

            _mockOrchestrator.Setup(x => x.ExecuteAsync<ConceptExtractionResult>(
                It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("LLM service unavailable"));

            // Act
            var result = await _aiModule.ExtractConceptsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("LLM service unavailable");
        }

        [Fact]
        public async Task ScoreAnalysisAsync_WithValidInput_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new ScoringAnalysisRequest
            {
                Concepts = new List<ConceptScore>
                {
                    new() { Name = "AI", Score = 0.9, Category = "Technology" },
                    new() { Name = "Innovation", Score = 0.8, Category = "Process" }
                }
            };

            var expectedResult = new ScoringAnalysisResult
            {
                AbundanceScore = 0.85,
                ConsciousnessScore = 0.75,
                UnityScore = 0.80,
                OverallScore = 0.80,
                Analysis = "High potential for positive impact"
            };

            var mockResponse = new LLMResponse<ScoringAnalysisResult>
            {
                Success = true,
                Data = expectedResult
            };

            _mockOrchestrator.Setup(x => x.ExecuteAsync<ScoringAnalysisResult>(
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiModule.ScoreAnalysisAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.OverallScore.Should().Be(0.80);
            result.Data.Analysis.Should().Be("High potential for positive impact");
        }

        [Fact]
        public async Task ScoreAnalysisAsync_WithEmptyConcepts_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ScoringAnalysisRequest
            {
                Concepts = new List<ConceptScore>()
            };

            // Act
            var result = await _aiModule.ScoreAnalysisAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Concepts cannot be empty");
        }

        [Fact]
        public async Task ScoreAnalysisAsync_WithNullConcepts_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ScoringAnalysisRequest
            {
                Concepts = null
            };

            // Act
            var result = await _aiModule.ScoreAnalysisAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Concepts cannot be null");
        }

        [Fact]
        public async Task FractalTransformAsync_WithValidInput_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new FractalTransformationRequest
            {
                Content = "Test content for transformation",
                TransformationType = "abundance",
                Depth = 3
            };

            var expectedResult = new FractalTransformationResult
            {
                TransformedContent = "Transformed test content",
                TransformationDepth = 3,
                Confidence = 0.85,
                Metadata = new Dictionary<string, object>
                {
                    { "transformation_type", "abundance" },
                    { "original_length", 28 }
                }
            };

            var mockResponse = new LLMResponse<FractalTransformationResult>
            {
                Success = true,
                Data = expectedResult
            };

            _mockOrchestrator.Setup(x => x.ExecuteAsync<FractalTransformationResult>(
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiModule.FractalTransformAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TransformedContent.Should().Be("Transformed test content");
            result.Data.Confidence.Should().Be(0.85);
        }

        [Fact]
        public async Task FractalTransformAsync_WithEmptyContent_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new FractalTransformationRequest
            {
                Content = "",
                TransformationType = "abundance",
                Depth = 3
            };

            // Act
            var result = await _aiModule.FractalTransformAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Content cannot be empty");
        }

        [Fact]
        public async Task FractalTransformAsync_WithInvalidDepth_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new FractalTransformationRequest
            {
                Content = "Test content",
                TransformationType = "abundance",
                Depth = 0
            };

            // Act
            var result = await _aiModule.FractalTransformAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Depth must be greater than 0");
        }

        [Fact]
        public async Task FractalTransformAsync_WithNegativeDepth_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new FractalTransformationRequest
            {
                Content = "Test content",
                TransformationType = "abundance",
                Depth = -1
            };

            // Act
            var result = await _aiModule.FractalTransformAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Depth must be greater than 0");
        }

        [Fact]
        public async Task FractalTransformAsync_WithOrchestratorException_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new FractalTransformationRequest
            {
                Content = "Test content",
                TransformationType = "abundance",
                Depth = 3
            };

            _mockOrchestrator.Setup(x => x.ExecuteAsync<FractalTransformationResult>(
                It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Transformation service unavailable"));

            // Act
            var result = await _aiModule.FractalTransformAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Transformation service unavailable");
        }

        [Fact]
        public void GetModuleInfo_ShouldReturnCorrectInfo()
        {
            // Act
            var info = _aiModule.GetModuleInfo();

            // Assert
            info.Should().NotBeNull();
            info.Name.Should().Be("AI Module");
            info.Version.Should().Be("2.0.0");
            info.Description.Should().Contain("AI processing");
        }

        [Fact]
        public void GetApiEndpoints_ShouldReturnAllEndpoints()
        {
            // Act
            var endpoints = _aiModule.GetApiEndpoints();

            // Assert
            endpoints.Should().NotBeNull();
            endpoints.Should().HaveCountGreaterThan(0);
            endpoints.Should().Contain(e => e.Path.Contains("extract-concepts"));
            endpoints.Should().Contain(e => e.Path.Contains("score-analysis"));
            endpoints.Should().Contain(e => e.Path.Contains("fractal-transform"));
        }
    }
}
