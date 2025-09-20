using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Tests.Modules;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Tests for ThreadsModule functionality after removing mock data
/// </summary>
public class ThreadsModuleTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;

    public ThreadsModuleTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListThreads_WithoutMockData_ReturnsEmptyList()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        // Act
        var result = await module.ListThreads(10);

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        var threads = resultObj?.threads;
        Assert.NotNull(threads);
        Assert.Empty(threads);
    }

    [Fact]
    public async Task CreateThread_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        var request = new ThreadCreateRequest
        {
            Title = "Test Thread",
            Content = "This is a test thread content",
            AuthorId = "test-user-1",
            AuthorName = "Test User",
            Axes = new[] { "consciousness", "unity" }
        };

        // Act
        var result = await module.CreateThread(request);

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        Assert.NotNull(resultObj?.threadId);
        Assert.Equal("Thread created successfully", resultObj?.message);
    }

    [Fact]
    public async Task CreateThread_WithoutTitle_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        var request = new ThreadCreateRequest
        {
            Title = "",
            Content = "This is a test thread content",
            AuthorId = "test-user-1"
        };

        // Act
        var result = await module.CreateThread(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Title and content are required", errorResponse.Error);
    }

    [Fact]
    public async Task GetThread_WithNonExistentId_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        // Act
        var result = await module.GetThread("non-existent-thread");

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Thread not found", errorResponse.Error);
    }

    [Fact]
    public async Task AddReply_WithoutThread_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        var replyRequest = new ThreadReplyRequest
        {
            Content = "This is a reply",
            AuthorId = "test-user-1",
            AuthorName = "Test User"
        };

        // Act
        var result = await module.AddReply("non-existent-thread", replyRequest);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Thread not found", errorResponse.Error);
    }

    [Fact]
    public void ModuleNode_HasCorrectProperties()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new ThreadsModule(registry, logger, httpClient);

        // Act
        var moduleNode = module.GetModuleNode();

        // Assert
        Assert.Equal("codex.threads", moduleNode.Id);
        Assert.Equal("Threads Module", moduleNode.Title);
        Assert.Equal("Deep conversations and collaborative exploration", moduleNode.Description);
        Assert.Contains("threads", moduleNode.Meta?["tags"] as string[] ?? Array.Empty<string>());
        Assert.Contains("thread-creation", moduleNode.Meta?["capabilities"] as string[] ?? Array.Empty<string>());
    }
}

