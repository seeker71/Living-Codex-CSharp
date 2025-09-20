using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Tests.Modules;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Tests for GalleryModule functionality after removing mock data
/// </summary>
public class GalleryModuleTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;

    public GalleryModuleTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListGalleryItems_WithoutMockData_ReturnsEmptyList()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        // Act
        var result = await module.ListGalleryItems(10, null, "resonance");

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        var items = resultObj?.items;
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateGalleryItem_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        var request = new GalleryItemCreateRequest
        {
            Title = "Test Gallery Item",
            Description = "This is a test gallery item",
            ImageUrl = "https://example.com/image.jpg", 
            AuthorId = "test-user-1",
            AuthorName = "Test User",
            Axes = new[] { "consciousness", "unity" },
            MediaType = "image"
        };

        // Act
        var result = await module.CreateGalleryItem(request);

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        Assert.NotNull(resultObj?.itemId);
        Assert.Equal("Gallery item created successfully", resultObj?.message);
    }

    [Fact]
    public async Task CreateGalleryItem_WithoutTitle_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        var request = new GalleryItemCreateRequest
        {
            Title = "",
            ImageUrl = "https://example.com/image.jpg",
            AuthorId = "test-user-1"
        };

        // Act
        var result = await module.CreateGalleryItem(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Title and image URL are required", errorResponse.Error);
    }

    [Fact]
    public async Task CreateGalleryItem_WithoutImageUrl_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        var request = new GalleryItemCreateRequest
        {
            Title = "Test Gallery Item",
            ImageUrl = "",
            AuthorId = "test-user-1"
        };

        // Act
        var result = await module.CreateGalleryItem(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Title and image URL are required", errorResponse.Error);
    }

    [Fact]
    public async Task GetGalleryItem_WithNonExistentId_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        // Act
        var result = await module.GetGalleryItem("non-existent-item");

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Gallery item not found", errorResponse.Error);
    }

    [Fact]
    public async Task GenerateAIImage_WithoutPrompt_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        var request = new AIImageGenerateRequest
        {
            Prompt = "",
            Title = "Test AI Image"
        };

        // Act
        var result = await module.GenerateAIImage(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Prompt is required for AI image generation", errorResponse.Error);
    }

    [Fact]
    public void ModuleNode_HasCorrectProperties()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new GalleryModule(registry, logger, httpClient);

        // Act
        var moduleNode = module.GetModuleNode();

        // Assert
        Assert.Equal("codex.gallery", moduleNode.Id);
        Assert.Equal("Gallery Module", moduleNode.Title);
        Assert.Equal("Visual expressions of consciousness and creativity", moduleNode.Description);
        Assert.Contains("gallery", moduleNode.Meta?["tags"] as string[] ?? Array.Empty<string>());
        Assert.Contains("media-upload", moduleNode.Meta?["capabilities"] as string[] ?? Array.Empty<string>());
    }
}
