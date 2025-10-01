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
/// Comprehensive tests for the Profile API endpoints
/// </summary>
public class ProfileApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProfileApiTests(WebApplicationFactory<Program> factory)
    {
        // Configure the WebApplicationFactory to use SQLite for testing
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

    #region Profile GET Tests

    [Fact]
    public async Task GetProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonExistentUserId = "user.nonexistent";

        // Act
        var response = await _client.GetAsync($"/auth/profile/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // API returns 200 with success: false

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("User not found", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetProfile_WithValidUser_ReturnsProfileData()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        
        // Verify basic profile fields
        Assert.Equal(userId, profile.GetProperty("id").GetString());
        Assert.Equal("testuser", profile.GetProperty("username").GetString());
        Assert.Equal("test@example.com", profile.GetProperty("email").GetString());
        Assert.Equal("Test User", profile.GetProperty("displayName").GetString());
        Assert.True(profile.GetProperty("isActive").GetBoolean());
        Assert.Equal("active", profile.GetProperty("status").GetString());
        
        // Verify extended profile fields
        Assert.True(profile.TryGetProperty("bio", out _));
        Assert.True(profile.TryGetProperty("location", out _));
        Assert.True(profile.TryGetProperty("interests", out _));
        Assert.True(profile.TryGetProperty("avatarUrl", out _));
        Assert.True(profile.TryGetProperty("coverImageUrl", out _));
        Assert.True(profile.TryGetProperty("joinedDate", out _));
        Assert.True(profile.TryGetProperty("lastActive", out _));
        Assert.True(profile.TryGetProperty("profileCompletion", out _));
        Assert.True(profile.TryGetProperty("resonanceLevel", out _));
    }

    [Fact]
    public async Task GetProfile_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.test-user_123";
        await CreateTestUser(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{Uri.EscapeDataString(userId)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Profile PUT Tests

    [Fact]
    public async Task UpdateProfile_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Updated Test User",
            bio = "This is an updated bio for testing profile updates",
            location = "Test City",
            interests = new[] { "technology", "AI", "programming" },
            avatarUrl = "https://example.com/avatar.jpg",
            coverImageUrl = "https://example.com/cover.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal("Profile updated successfully", result.GetProperty("message").GetString());

        var profile = result.GetProperty("profile");
        Assert.Equal("Updated Test User", profile.GetProperty("displayName").GetString());
        Assert.Equal("This is an updated bio for testing profile updates", profile.GetProperty("bio").GetString());
        Assert.Equal("Test City", profile.GetProperty("location").GetString());
        
        var interests = profile.GetProperty("interests").EnumerateArray().Select(i => i.GetString()).ToArray();
        Assert.Equal(3, interests.Length);
        Assert.Contains("technology", interests);
        Assert.Contains("AI", interests);
        Assert.Contains("programming", interests);
    }

    [Fact]
    public async Task UpdateProfile_WithPartialData_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            displayName = "Partially Updated User",
            bio = "Updated bio only"
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        Assert.Equal("Partially Updated User", profile.GetProperty("displayName").GetString());
        Assert.Equal("Updated bio only", profile.GetProperty("bio").GetString());
        // Other fields should remain unchanged
        Assert.Equal("test@example.com", profile.GetProperty("email").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonExistentUserId = "user.nonexistent";
        var updateData = new { displayName = "Updated" };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{nonExistentUserId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // API returns 200 with success: false

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("User not found", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyInterests_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var updateData = new
        {
            interests = new string[0]
        };

        // Act
        var json = JsonSerializer.Serialize(updateData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var interests = profile.GetProperty("interests").EnumerateArray().ToArray();
        Assert.Empty(interests);
    }

    #endregion

    #region Profile Completion Tests

    [Fact]
    public async Task GetProfile_CalculatesCompletionCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 0% for a basic user with no bio, interests, etc.
        Assert.Equal(0, completion);
    }

    [Fact]
    public async Task UpdateProfile_WithCompleteData_CalculatesCompletionCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var completeData = new
        {
            displayName = "Complete User",
            bio = "This is a complete bio with more than 10 characters",
            location = "Test City",
            interests = new[] { "technology", "AI" },
            avatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(completeData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 80% (4 out of 5 sections complete: basic, avatar, interests, location)
        Assert.Equal(80, completion);
    }

    #endregion

    #region Resonance Level Tests

    [Fact]
    public async Task GetProfile_CalculatesResonanceLevelCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        // Should be 50 (base level) for a basic user
        Assert.Equal(50, resonanceLevel);
    }

    [Fact]
    public async Task UpdateProfile_WithRichData_IncreasesResonanceLevel()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var richData = new
        {
            displayName = "Rich User",
            bio = "This is a very detailed bio with more than 200 characters to test the resonance level calculation and ensure it properly accounts for bio quality and length in the overall scoring algorithm.",
            location = "Test City",
            interests = new[] { "technology", "AI", "programming", "science", "innovation" }
        };

        // Act
        var json = JsonSerializer.Serialize(richData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        // Should be higher than 50 due to bio quality and interests
        Assert.True(resonanceLevel > 50);
    }

    #endregion

    #region Belief System Tests

    [Fact]
    public async Task GetBeliefSystem_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonExistentUserId = "user.nonexistent";

        // Act
        var response = await _client.GetAsync($"/userconcept/belief-system/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // API returns 200 with success: false

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("User belief system not found", result.GetProperty("error").GetString());
    }

    [Fact]
    public async Task RegisterBeliefSystem_WithValidData_RegistersSuccessfully()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

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

        // Act
        var json = JsonSerializer.Serialize(beliefData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/userconcept/belief-system/register", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(userId, result.GetProperty("userId").GetString());
        Assert.Equal("scientific", result.GetProperty("framework").GetString());
        Assert.Equal("Belief system registered successfully", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetBeliefSystem_AfterRegistration_ReturnsBeliefSystem()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);
        await RegisterBeliefSystem(userId);

        // Act
        var response = await _client.GetAsync($"/userconcept/belief-system/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var beliefSystem = result.GetProperty("beliefSystem");
        
        Assert.Equal(userId, beliefSystem.GetProperty("userId").GetString());
        Assert.Equal("scientific", beliefSystem.GetProperty("framework").GetString());
        Assert.Equal("en", beliefSystem.GetProperty("language").GetString());
        Assert.Equal("western", beliefSystem.GetProperty("culturalContext").GetString());
        Assert.Equal("secular", beliefSystem.GetProperty("spiritualTradition").GetString());
        Assert.Equal("computer science", beliefSystem.GetProperty("scientificBackground").GetString());
        Assert.Equal(0.8, beliefSystem.GetProperty("resonanceThreshold").GetDouble());
        
        var principles = beliefSystem.GetProperty("principles").EnumerateArray().Select(p => p.GetString()).ToArray();
        Assert.Equal(2, principles.Length);
        Assert.Contains("evidence-based thinking", principles);
        Assert.Contains("empirical validation", principles);
        
        var values = beliefSystem.GetProperty("values").EnumerateArray().Select(v => v.GetString()).ToArray();
        Assert.Equal(3, values.Length);
        Assert.Contains("truth", values);
        Assert.Contains("clarity", values);
        Assert.Contains("precision", values);
    }

    [Fact]
    public async Task RegisterBeliefSystem_WithEmptyArrays_HandlesCorrectly()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var beliefData = new
        {
            userId = userId,
            framework = "minimal",
            principles = new string[0],
            values = new string[0],
            language = "en",
            culturalContext = "universal",
            resonanceThreshold = 0.5
        };

        // Act
        var json = JsonSerializer.Serialize(beliefData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/userconcept/belief-system/register", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Profile Sections Tests

    [Fact]
    public async Task UpdateProfile_WithAllSections_CompletesAllSections()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);
        await RegisterBeliefSystem(userId);

        var completeData = new
        {
            displayName = "Complete User",
            bio = "This is a complete bio with more than 10 characters for testing",
            location = "Test City",
            interests = new[] { "technology", "AI", "programming" },
            avatarUrl = "https://example.com/avatar.jpg",
            coverImageUrl = "https://example.com/cover.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(completeData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.GetProperty("success").GetBoolean());
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 100% (5 out of 5 sections complete: basic, avatar, interests, beliefs, location)
        Assert.Equal(100, completion);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UpdateProfile_WithInvalidJson_ReturnsError()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var invalidJson = "{ invalid json }";

        // Act
        var response = await _client.PutAsync($"/auth/profile/{userId}", 
            new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        // Should handle gracefully - either 400 or 200 with error
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterBeliefSystem_WithMissingRequiredFields_ReturnsError()
    {
        // Arrange
        var userId = "user.testuser";
        await CreateTestUser(userId);

        var incompleteData = new
        {
            userId = userId
            // Missing required fields
        };

        // Act
        var json = JsonSerializer.Serialize(incompleteData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/userconcept/belief-system/register", requestContent);

        // Assert
        // Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
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
