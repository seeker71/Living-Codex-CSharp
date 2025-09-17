using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Security;
using CodexBootstrap.Core.Storage;
using FluentAssertions;
using Moq;
using Xunit;
using SecurityUser = CodexBootstrap.Core.Security.User;

namespace CodexBootstrap.Tests.Core;

public sealed class NodeRegistryUserRepositoryTests
{
    private readonly NodeRegistry _registry;
    private readonly NodeRegistryUserRepository _repository;
    private readonly Mock<ICodexLogger> _loggerMock = new();

    public NodeRegistryUserRepositoryTests()
    {
        var iceStorage = new InMemoryIceStorageBackend();
        var waterStorage = new InMemoryWaterStorageBackend();
        _registry = new NodeRegistry(iceStorage, waterStorage, _loggerMock.Object);
        _registry.InitializeAsync().GetAwaiter().GetResult();
        _repository = new NodeRegistryUserRepository(_registry, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistUserAsWaterNode()
    {
        var user = CreateUser();

        await _repository.CreateAsync(user);

        var storedNode = _registry.GetNode($"user.{user.Id}");
        storedNode.Should().NotBeNull();
        storedNode!.State.Should().Be(ContentState.Water);
        storedNode.Meta.Should().ContainKey("emailNormalized");
        storedNode.Meta!["emailNormalized"].Should().Be(user.Email.ToLowerInvariant());
        storedNode.Meta.Should().ContainKey("passwordHash");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldMatchRegardlessOfCase()
    {
        var user = CreateUser(email: "CaseUser@example.com");
        await _repository.CreateAsync(user);

        // Wait a bit for async storage to complete
        await Task.Delay(100);

        var result = await _repository.GetByEmailAsync("caseuser@EXAMPLE.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldOverwriteMetadata()
    {
        var user = CreateUser(displayName: "Original");
        await _repository.CreateAsync(user);

        user.DisplayName = "Updated Name";
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow.AddMinutes(1);
        user.LastLoginAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        var reloaded = await _repository.GetByIdAsync(user.Id);
        reloaded.Should().NotBeNull();
        reloaded!.DisplayName.Should().Be("Updated Name");
        reloaded.IsActive.Should().BeFalse();
        reloaded.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveNode()
    {
        var user = CreateUser();
        await _repository.CreateAsync(user);

        await _repository.DeleteAsync(user.Id);

        await WaitUntilAsync(async () => await _registry.GetNodeAsync($"user.{user.Id}") == null, TimeSpan.FromMilliseconds(500));

        var result = await _repository.GetByIdAsync(user.Id);
        result.Should().BeNull();
    }

    private static SecurityUser CreateUser(
        string? id = null,
        string? email = null,
        string? displayName = null,
        string? passwordHash = null)
    {
        return new SecurityUser
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Email = email ?? "user@example.com",
            DisplayName = displayName ?? "Test User",
            PasswordHash = passwordHash ?? "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var start = Stopwatch.StartNew();
        while (start.Elapsed < timeout)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Condition not met within timeout");
    }
}
