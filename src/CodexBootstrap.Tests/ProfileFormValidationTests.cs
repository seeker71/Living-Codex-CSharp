using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;
using Xunit;
using System.Net.Http;
using System.Text;

namespace CodexBootstrap.Tests;

/// <summary>
/// Tests for profile form validation and UI integration
/// </summary>
public class ProfileFormValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProfileFormValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                Environment.SetEnvironmentVariable("ICE_STORAGE_TYPE", "sqlite");
                Environment.SetEnvironmentVariable("ICE_CONNECTION_STRING", "Data Source=data/codex.db");
            });
        });
        _client = _factory.CreateClient();
    }

    #region Display Name Validation

    [Fact]
    public async Task UpdateProfile_WithEmptyDisplayName_HandlesGracefully()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "",
            bio = "Valid bio"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task UpdateProfile_WithVeryLongDisplayName_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var longDisplayName = new string('A', 1000); // Very long display name
        var updateData = new
        {
            displayName = longDisplayName,
            bio = "Valid bio"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Bio Validation

    [Fact]
    public async Task UpdateProfile_WithShortBio_CalculatesCompletionCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Short" // Less than 10 characters
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should not count as complete due to short bio
        Assert.True(completion < 20); // Basic section not complete
    }

    [Fact]
    public async Task UpdateProfile_WithLongBio_IncreasesResonanceLevel()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var longBio = "This is a very detailed bio with more than 200 characters to test the resonance level calculation and ensure it properly accounts for bio quality and length in the overall scoring algorithm. This should significantly increase the resonance level.";
        var updateData = new
        {
            displayName = "Test User",
            bio = longBio
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        // Should be higher than base level due to long bio
        Assert.True(resonanceLevel > 50);
    }

    #endregion

    #region Interests Validation

    [Fact]
    public async Task UpdateProfile_WithManyInterests_IncreasesResonanceLevel()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var manyInterests = new[] { "tech", "AI", "programming", "science", "innovation", "research", "development", "design", "art", "music" };
        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio with enough characters",
            interests = manyInterests
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        // Should be higher due to many interests
        Assert.True(resonanceLevel > 50);
    }

    [Fact]
    public async Task UpdateProfile_WithDuplicateInterests_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var duplicateInterests = new[] { "technology", "AI", "technology", "programming", "AI" };
        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio",
            interests = duplicateInterests
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var interests = profile.GetProperty("interests").EnumerateArray().Select(i => i.GetString()).ToArray();
        
        // Should preserve duplicates as they are (frontend should handle deduplication)
        Assert.Equal(5, interests.Length);
    }

    #endregion

    #region Location Validation

    [Fact]
    public async Task UpdateProfile_WithLocationData_CompletesLocationSection()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio with enough characters",
            location = "San Francisco, CA",
            latitude = 37.7749,
            longitude = -122.4194
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, location)
        Assert.Equal(40, completion);
        
        // Verify location data is stored
        Assert.Equal("San Francisco, CA", profile.GetProperty("location").GetString());
        Assert.Equal(37.7749, profile.GetProperty("latitude").GetDouble());
        Assert.Equal(-122.4194, profile.GetProperty("longitude").GetDouble());
    }

    #endregion

    #region Avatar and Cover Image Validation

    [Fact]
    public async Task UpdateProfile_WithImageUrls_CompletesAvatarSection()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio with enough characters",
            avatarUrl = "https://example.com/avatar.jpg",
            coverImageUrl = "https://example.com/cover.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, avatar)
        Assert.Equal(40, completion);
        
        // Verify image URLs are stored
        Assert.Equal("https://example.com/avatar.jpg", profile.GetProperty("avatarUrl").GetString());
        Assert.Equal("https://example.com/cover.jpg", profile.GetProperty("coverImageUrl").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidImageUrls_HandlesGracefully()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio",
            avatarUrl = "not-a-valid-url",
            coverImageUrl = "also-not-valid"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should still store the URLs as provided (validation should be on frontend)
    }

    #endregion

    #region Belief System Integration

    [Fact]
    public async Task UpdateProfile_WithBeliefSystem_CompletesBeliefsSection()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);
        await RegisterBeliefSystem(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio with enough characters"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, beliefs)
        Assert.Equal(40, completion);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UpdateProfile_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User",
            bio = "Valid bio",
            location = (string)null,
            interests = (string[])null,
            avatarUrl = (string)null,
            coverImageUrl = (string)null
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task UpdateProfile_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Test User with émojis 🚀 and spëcial chars",
            bio = "Bio with unicode: 你好世界 🌍 and symbols: @#$%^&*()",
            location = "São Paulo, Brasil 🇧🇷",
            interests = new[] { "AI/ML", "C++", "JavaScript", "Python 🐍" }
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        
        // Verify special characters are preserved
        Assert.Equal("Test User with émojis 🚀 and spëcial chars", profile.GetProperty("displayName").GetString());
        Assert.Equal("Bio with unicode: 你好世界 🌍 and symbols: @#$%^&*()", profile.GetProperty("bio").GetString());
        Assert.Equal("São Paulo, Brasil 🇧🇷", profile.GetProperty("location").GetString());
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestUser(string userId)
    {
        var userData = new
        {
            id = userId,
            typeId = "codex.user",
            state = "ice",
            locale = "en",
            title = "Test User",
            description = "Test user for profile testing",
            content = new
            {
                mediaType = "application/json",
                inlineJson = "{\"username\": \"testuser\", \"email\": \"test@example.com\"}"
            },
            meta = new
            {
                username = "testuser",
                email = "test@example.com",
                displayName = "Test User",
                createdAt = "2025-09-30T00:44:00.000Z",
                lastLoginAt = "2025-09-30T00:44:00.000Z",
                isActive = true,
                status = "active",
                bio = "",
                location = "",
                interests = new string[0],
                avatarUrl = "",
                coverImageUrl = ""
            }
        };

        var json = JsonSerializer.Serialize(userData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task RegisterBeliefSystem(string userId)
    {
        var beliefData = new
        {
            userId = userId,
            framework = "scientific",
            principles = new[] { "evidence-based thinking", "empirical validation" },
            values = new[] { "truth", "clarity", "precision" },
            language = "en",
            culturalContext = "western",
            spiritualTradition = "secular",
            scientificBackground = "computer science",
            resonanceThreshold = 0.8,
            consciousnessLevel = "analytical"
        };

        var json = JsonSerializer.Serialize(beliefData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/userconcept/belief-system/register", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion
}
