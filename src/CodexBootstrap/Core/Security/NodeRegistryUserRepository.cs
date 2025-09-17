using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core.Security;

/// <summary>
/// NodeRegistry-backed user repository that persists identities as Water-state nodes.
/// </summary>
public class NodeRegistryUserRepository : IUserRepository
{
    private const string UserTypeId = "codex.user";
    private const string UserNodePrefix = "user.";

    private readonly INodeRegistry _nodeRegistry;
    private readonly ICodexLogger _logger;

    public NodeRegistryUserRepository(INodeRegistry nodeRegistry, ICodexLogger logger)
    {
        _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<User?> GetByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<User?>(null);
        }

        var nodeId = BuildNodeId(userId);
        var node = _nodeRegistry.GetNode(nodeId) ?? _nodeRegistry.GetNode(userId);
        if (node == null)
        {
            return Task.FromResult<User?>(null);
        }

        return Task.FromResult(MapNodeToUser(node));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        var candidates = await _nodeRegistry.GetNodesByTypeAsync(UserTypeId);

        foreach (var node in candidates)
        {
            var meta = node.Meta ?? new Dictionary<string, object>();
            var emailValue = ReadString(meta, "email")?.ToLowerInvariant();
            var normalizedValue = ReadString(meta, "emailNormalized")?.ToLowerInvariant();

            if (emailValue == normalized || normalizedValue == normalized)
            {
                return MapNodeToUser(node);
            }
        }

        return null;
    }

    public Task CreateAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var node = BuildNode(user);
        _nodeRegistry.Upsert(node);
        _logger.Info($"User {user.Email} persisted to NodeRegistry as Water node");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var node = BuildNode(user);
        _nodeRegistry.Upsert(node);
        _logger.Info($"User {user.Email} updated in NodeRegistry");
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.CompletedTask;
        }

        var nodeId = BuildNodeId(userId);
        var node = _nodeRegistry.GetNode(nodeId) ?? _nodeRegistry.GetNode(userId);
        if (node != null)
        {
            _nodeRegistry.RemoveNode(node.Id);
            _logger.Info($"User node {node.Id} removed from NodeRegistry");
        }

        return Task.CompletedTask;
    }

    private static string BuildNodeId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        return userId.StartsWith(UserNodePrefix, StringComparison.OrdinalIgnoreCase)
            ? userId
            : $"{UserNodePrefix}{userId}";
    }

    private static string StripNodePrefix(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return nodeId;
        }

        return nodeId.StartsWith(UserNodePrefix, StringComparison.OrdinalIgnoreCase)
            ? nodeId.Substring(UserNodePrefix.Length)
            : nodeId;
    }

    private Node BuildNode(User user)
    {
        var nodeId = BuildNodeId(user.Id);
        var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.Id : user.DisplayName;
        var createdAt = user.CreatedAt == default ? DateTime.UtcNow : EnsureUtc(user.CreatedAt);
        var updatedAt = user.UpdatedAt == default ? DateTime.UtcNow : EnsureUtc(user.UpdatedAt);
        var lastLogin = user.LastLoginAt.HasValue ? EnsureUtc(user.LastLoginAt.Value) : (DateTime?)null;

        var meta = new Dictionary<string, object>
        {
            ["userId"] = user.Id,
            ["email"] = user.Email ?? string.Empty,
            ["emailNormalized"] = (user.Email ?? string.Empty).Trim().ToLowerInvariant(),
            ["displayName"] = displayName,
            ["passwordHash"] = user.PasswordHash ?? string.Empty,
            ["isActive"] = user.IsActive,
            ["createdAt"] = createdAt.ToString("o", CultureInfo.InvariantCulture),
            ["updatedAt"] = updatedAt.ToString("o", CultureInfo.InvariantCulture)
        };

        if (lastLogin.HasValue)
        {
            meta["lastLoginAt"] = lastLogin.Value.ToString("o", CultureInfo.InvariantCulture);
        }

        var payload = new
        {
            user.Id,
            user.Email,
            DisplayName = displayName,
            user.IsActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastLoginAt = lastLogin
        };

        var content = new ContentRef(
            MediaType: "application/json",
            InlineJson: JsonSerializer.Serialize(payload),
            InlineBytes: null,
            ExternalUri: null
        );

        return new Node(
            Id: nodeId,
            TypeId: UserTypeId,
            State: ContentState.Water,
            Locale: "en-US",
            Title: displayName,
            Description: $"User profile for {displayName}",
            Content: content,
            Meta: meta
        );
    }

    private static User? MapNodeToUser(Node node)
    {
        if (node.Meta == null)
        {
            return null;
        }

        var meta = node.Meta;

        var id = ReadString(meta, "userId") ?? StripNodePrefix(node.Id);
        var email = ReadString(meta, "email") ?? string.Empty;
        var displayName = ReadString(meta, "displayName") ?? email;
        var passwordHash = ReadString(meta, "passwordHash") ?? string.Empty;
        var isActive = ReadBool(meta, "isActive", defaultValue: true);
        var createdAt = ReadDateTime(meta, "createdAt") ?? DateTime.UtcNow;
        var updatedAt = ReadDateTime(meta, "updatedAt") ?? createdAt;
        var lastLoginAt = ReadDateTime(meta, "lastLoginAt");

        return new User
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastLoginAt = lastLoginAt
        };
    }

    private static string? ReadString(IDictionary<string, object> meta, string key)
    {
        if (!meta.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
            JsonElement json when json.ValueKind == JsonValueKind.Number => json.GetRawText(),
            JsonElement json when json.ValueKind == JsonValueKind.True => "true",
            JsonElement json when json.ValueKind == JsonValueKind.False => "false",
            _ => value.ToString()
        };
    }

    private static bool ReadBool(IDictionary<string, object> meta, string key, bool defaultValue)
    {
        if (!meta.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            JsonElement json when json.ValueKind == JsonValueKind.True => true,
            JsonElement json when json.ValueKind == JsonValueKind.False => false,
            _ => defaultValue
        };
    }

    private static DateTime? ReadDateTime(IDictionary<string, object> meta, string key)
    {
        if (!meta.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        if (value is DateTime dt)
        {
            return EnsureUtc(dt);
        }

        if (value is string s && DateTime.TryParse(s, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return EnsureUtc(parsed);
        }

        if (value is JsonElement json && json.ValueKind == JsonValueKind.String && DateTime.TryParse(json.GetString(), null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedFromJson))
        {
            return EnsureUtc(parsedFromJson);
        }

        return null;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }
}
