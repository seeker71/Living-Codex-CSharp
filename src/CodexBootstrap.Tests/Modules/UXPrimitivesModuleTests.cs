using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Tests.Modules;

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
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
            Content: new ContentRef(MediaType: "text/plain", InlineJson: null, InlineBytes: null, ExternalUri: null),
            Meta: new Dictionary<string, object>()
        );
        
        var targetNode = new Node(
            Id: "test-target",
            TypeId: "test.concept",
            State: ContentState.Water,
            Locale: "en-US",
            Title: "Target Concept",
            Description: "A test target concept",
            Content: new ContentRef(MediaType: "text/plain", InlineJson: null, InlineBytes: null, ExternalUri: null),
            Meta: new Dictionary<string, object>()
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
        Console.WriteLine($"Result type: {result.GetType().Name}");
        Console.WriteLine($"Result: {result}");
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.Fail($"Expected success but got error: {errorResponse.Error}");
        }
        
        // Use reflection to access anonymous type properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var weaveIdProperty = resultType.GetProperty("weaveId");
        var messageProperty = resultType.GetProperty("message");
        var relationshipProperty = resultType.GetProperty("relationship");
        var strengthProperty = resultType.GetProperty("strength");
        
        Assert.True((bool)successProperty.GetValue(result)!);
        Assert.NotNull(weaveIdProperty.GetValue(result));
        Assert.Equal("Weave connection created successfully", messageProperty.GetValue(result));
        Assert.Equal("related", relationshipProperty.GetValue(result));
        Assert.Equal(0.8, strengthProperty.GetValue(result));
    }

    [Fact]
    public async Task CreateWeave_WithoutSourceId_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
        Console.WriteLine($"Result type: {result.GetType().Name}");
        Console.WriteLine($"Result: {result}");
        Console.WriteLine($"Result is ErrorResponse: {result is ErrorResponse}");
        Console.WriteLine($"Result is object: {result is object}");
        Console.WriteLine($"Result is string: {result is string}");
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.Equal("Source ID and Target ID are required", errorResponse.Error);
        }
        else
        {
            Assert.Fail($"Expected ErrorResponse but got {result.GetType().Name}: {result}");
        }
    }

    [Fact]
    public async Task GenerateReflection_WithNonExistentContent_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Contains("Content not found", errorResponse.Error);
    }

    [Fact]
    public async Task GenerateReflection_WithoutContentId_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Content ID is required", errorResponse.Error);
    }

    [Fact]
    public async Task SendInvite_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.Fail($"Expected success but got error: {errorResponse.Error}");
        }
        
        // Use reflection to access anonymous type properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var inviteIdProperty = resultType.GetProperty("inviteId");
        var messageProperty = resultType.GetProperty("message");
        var inviteTypeProperty = resultType.GetProperty("inviteType");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(inviteIdProperty);
        Assert.NotNull(messageProperty);
        Assert.NotNull(inviteTypeProperty);
        
        Assert.True((bool)successProperty.GetValue(result)!);
        Assert.NotNull(inviteIdProperty.GetValue(result));
        Assert.Equal("Invitation sent successfully", messageProperty.GetValue(result));
        Assert.Equal("collaboration", inviteTypeProperty.GetValue(result));
    }

    [Fact]
    public async Task SendInvite_WithoutMessage_ReturnsError()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
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
        var errorResponse = result as CodexBootstrap.Core.ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Content ID and message are required", errorResponse.Error);
    }

    [Fact]
    public async Task GetReceivedInvites_WithNoInvites_ReturnsEmptyList()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        await registry.InitializeAsync();
        var logger = TestInfrastructure.CreateTestLogger();
        var httpClient = new HttpClient();
        var module = new UXPrimitivesModule(registry, logger, httpClient);

        // Act
        var result = await module.GetReceivedInvites("test-user-1");

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.Fail($"Expected success but got error: {errorResponse.Error}");
        }
        
        // Use reflection to access anonymous type properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var invitesProperty = resultType.GetProperty("invites");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(invitesProperty);
        
        Assert.True((bool)successProperty.GetValue(result)!);
        var invites = invitesProperty.GetValue(result);
        Assert.NotNull(invites);
        Assert.Empty((IEnumerable<object>)invites!);
    }

    [Fact]
    public void ModuleNode_HasCorrectProperties()
    {
        // Arrange
        var registry = TestInfrastructure.CreateTestNodeRegistry();
        registry.InitializeAsync().Wait();
        var logger = TestInfrastructure.CreateTestLogger();
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
