using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Tests;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Tests for UXPrimitivesModule functionality after removing mock/fallback data
/// </summary>
public class UXPrimitivesModuleTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;

    public UXPrimitivesModuleTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateWeave_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        // First create some test nodes to weave between
        var sourceNode = new Node(
            Id: "test-source",
            TypeId: "test.concept",
            State: ContentState.Water,
            Locale: "en-US",
            Title: "Source Concept",
            Description: "A test source concept",
            Content: new ContentRef(MediaType: "text/plain", InlineJson: null, InlineBytes: null, ExternalUri: null)
        );
        
        var targetNode = new Node(
            Id: "test-target",
            TypeId: "test.concept",
            State: ContentState.Water,
            Locale: "en-US",
            Title: "Target Concept",
            Description: "A test target concept",
            Content: new ContentRef(MediaType: "text/plain", InlineJson: null, InlineBytes: null, ExternalUri: null)
        );

        registry.Upsert(sourceNode);
        registry.Upsert(targetNode);

        var request = new WeaveRequest
        {
            SourceId = "test-source",
            TargetId = "test-target",
            Relationship = "related",
            Strength = 0.8,
            UserId = "test-user-1"
        };

        // Act
        var result = await module.CreateWeave(request);

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        Assert.NotNull(resultObj?.weaveId);
        Assert.Equal("Weave connection created successfully", resultObj?.message);
        Assert.Equal("related", resultObj?.relationship);
        Assert.Equal(0.8, resultObj?.strength);
    }

    [Fact]
    public async Task CreateWeave_WithoutSourceId_ReturnsError()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        var request = new WeaveRequest
        {
            SourceId = "",
            TargetId = "test-target",
            Relationship = "related",
            Strength = 0.8
        };

        // Act
        var result = await module.CreateWeave(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Source ID and Target ID are required", errorResponse.Message);
    }

    [Fact]
    public async Task GenerateReflection_WithNonExistentContent_ReturnsError()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        var request = new ReflectRequest
        {
            ContentId = "non-existent-content",
            ReflectionType = "insight",
            Depth = 3,
            UserId = "test-user-1"
        };

        // Act
        var result = await module.GenerateReflection(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Content not found", errorResponse.Message);
    }

    [Fact]
    public async Task GenerateReflection_WithoutContentId_ReturnsError()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        var request = new ReflectRequest
        {
            ContentId = "",
            ReflectionType = "insight",
            Depth = 3
        };

        // Act
        var result = await module.GenerateReflection(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Content ID is required", errorResponse.Message);
    }

    [Fact]
    public async Task SendInvite_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        var request = new InviteRequest
        {
            ContentId = "test-content",
            InviteType = "collaboration",
            Message = "Let's collaborate on this!",
            TargetUserId = "test-user-2",
            FromUserId = "test-user-1"
        };

        // Act
        var result = await module.SendInvite(request);

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        Assert.NotNull(resultObj?.inviteId);
        Assert.Equal("Invitation sent successfully", resultObj?.message);
        Assert.Equal("collaboration", resultObj?.inviteType);
    }

    [Fact]
    public async Task SendInvite_WithoutMessage_ReturnsError()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        var request = new InviteRequest
        {
            ContentId = "test-content",
            InviteType = "collaboration",
            Message = "",
            FromUserId = "test-user-1"
        };

        // Act
        var result = await module.SendInvite(request);

        // Assert
        Assert.NotNull(result);
        var errorResponse = result as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Content ID and message are required", errorResponse.Message);
    }

    [Fact]
    public async Task GetReceivedInvites_WithNoInvites_ReturnsEmptyList()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        // Act
        var result = await module.GetReceivedInvites("test-user-1");

        // Assert
        Assert.NotNull(result);
        var resultObj = result as dynamic;
        Assert.True(resultObj?.success);
        var invites = resultObj?.invites;
        Assert.NotNull(invites);
        Assert.Empty(invites);
    }

    [Fact]
    public void ModuleNode_HasCorrectProperties()
    {
        // Arrange
        var registry = new NodeRegistry();
        var logger = new ConsoleLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        // Act
        var moduleNode = module.GetModuleNode();

        // Assert
        Assert.Equal("codex.ux-primitives", moduleNode.Id);
        Assert.Equal("UX Primitives Module", moduleNode.Title);
        Assert.Equal("Core interaction patterns: Weave, Reflect, Invite", moduleNode.Description);
        Assert.Contains("weave", moduleNode.Meta?["tags"] as string[] ?? Array.Empty<string>());
        Assert.Contains("weave-connections", moduleNode.Meta?["capabilities"] as string[] ?? Array.Empty<string>());
    }
}
