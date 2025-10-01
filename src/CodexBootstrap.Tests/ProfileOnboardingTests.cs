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
/// Tests for profile onboarding flow and completion tracking
/// </summary>
public class ProfileOnboardingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProfileOnboardingTests(WebApplicationFactory<Program> factory)
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

    #region Onboarding Flow Tests

    [Fact]
    public async Task NewUser_StartsWithEmptyProfile_ZeroCompletion()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        Assert.Equal(0, completion);
        Assert.Equal(50, resonanceLevel); // Base level
    }

    [Fact]
    public async Task OnboardingStep1_BasicInfo_CompletesBasicSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        var basicInfo = new
        {
            displayName = "New User",
            bio = "This is a complete bio with more than 10 characters"
        };

        // Act
        var json = JsonSerializer.Serialize(basicInfo);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 20% (1 out of 5 sections: basic)
        Assert.Equal(20, completion);
    }

    [Fact]
    public async Task OnboardingStep2_AddAvatar_CompletesAvatarSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);
        await CompleteBasicInfo(userId);

        var avatarData = new
        {
            avatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var json = JsonSerializer.Serialize(avatarData);
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
    }

    [Fact]
    public async Task OnboardingStep3_AddInterests_CompletesInterestsSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);
        await CompleteBasicInfo(userId);

        var interestsData = new
        {
            interests = new[] { "technology", "AI", "programming" }
        };

        // Act
        var json = JsonSerializer.Serialize(interestsData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, interests)
        Assert.Equal(40, completion);
    }

    [Fact]
    public async Task OnboardingStep4_AddLocation_CompletesLocationSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);
        await CompleteBasicInfo(userId);

        var locationData = new
        {
            location = "San Francisco, CA",
            latitude = 37.7749,
            longitude = -122.4194
        };

        // Act
        var json = JsonSerializer.Serialize(locationData);
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
    }

    [Fact]
    public async Task OnboardingStep5_AddBeliefSystem_CompletesBeliefsSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);
        await CompleteBasicInfo(userId);

        var beliefData = new
        {
            userId = userId,
            framework = "scientific",
            principles = new[] { "evidence-based thinking" },
            values = new[] { "truth", "clarity" },
            language = "en",
            culturalContext = "western",
            resonanceThreshold = 0.8
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
        
        // Verify profile completion increased
        var profileResponse = await _client.GetAsync($"/auth/profile/{userId}");
        var profileContent = await profileResponse.Content.ReadAsStringAsync();
        var profileResult = JsonSerializer.Deserialize<JsonElement>(profileContent);
        
        var profile = profileResult.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, beliefs)
        Assert.Equal(40, completion);
    }

    [Fact]
    public async Task CompleteOnboarding_AllSections_100PercentCompletion()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        // Act - Complete all sections
        await CompleteBasicInfo(userId);
        await AddAvatar(userId);
        await AddInterests(userId);
        await AddLocation(userId);
        await AddBeliefSystem(userId);

        // Assert
        var response = await _client.GetAsync($"/auth/profile/{userId}");
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        var resonanceLevel = profile.GetProperty("resonanceLevel").GetDouble();
        
        Assert.Equal(100, completion);
        Assert.True(resonanceLevel > 50); // Should be higher due to complete profile
    }

    #endregion

    #region Completion Tracking Tests

    [Fact]
    public async Task ProfileCompletion_CalculatesCorrectly_ForPartialCompletion()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        // Complete basic info and interests
        await CompleteBasicInfo(userId);
        await AddInterests(userId);

        // Act
        var response = await _client.GetAsync($"/auth/profile/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 40% (2 out of 5 sections: basic, interests)
        Assert.Equal(40, completion);
    }

    [Fact]
    public async Task ResonanceLevel_IncreasesWithCompletion()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        // Get initial resonance level
        var initialResponse = await _client.GetAsync($"/auth/profile/{userId}");
        var initialContent = await initialResponse.Content.ReadAsStringAsync();
        var initialResult = JsonSerializer.Deserialize<JsonElement>(initialContent);
        var initialResonance = initialResult.GetProperty("profile").GetProperty("resonanceLevel").GetDouble();

        // Act - Add rich profile data
        await CompleteBasicInfo(userId);
        await AddInterests(userId);
        await AddBeliefSystem(userId);

        // Assert
        var response = await _client.GetAsync($"/auth/profile/{userId}");
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var finalResonance = profile.GetProperty("resonanceLevel").GetDouble();
        
        Assert.True(finalResonance > initialResonance);
    }

    #endregion

    #region Onboarding Edge Cases

    [Fact]
    public async Task Onboarding_WithMinimalData_StillCalculatesCompletion()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        var minimalData = new
        {
            displayName = "Minimal User",
            bio = "Short bio", // Less than 10 characters
            interests = new[] { "tech" }
        };

        // Act
        var json = JsonSerializer.Serialize(minimalData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 20% (1 out of 5 sections: interests, basic not complete due to short bio)
        Assert.Equal(20, completion);
    }

    [Fact]
    public async Task Onboarding_WithIncompleteBasicInfo_DoesNotCompleteBasicSection()
    {
        // Arrange
        var userId = "user.newuser";
        await CreateEmptyUser(userId);

        var incompleteBasic = new
        {
            displayName = "Incomplete User"
            // Missing bio
        };

        // Act
        var json = JsonSerializer.Serialize(incompleteBasic);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var profile = result.GetProperty("profile");
        var completion = profile.GetProperty("profileCompletion").GetDouble();
        
        // Should be 0% (basic section not complete due to missing bio)
        Assert.Equal(0, completion);
    }

    #endregion

    #region Helper Methods

    private async Task CreateEmptyUser(string userId)
    {
        var userData = new
        {
            id = userId,
            typeId = "codex.user",
            state = "ice",
            locale = "en",
            title = "New User",
            description = "New user for onboarding testing",
            content = new
            {
                mediaType = "application/json",
                inlineJson = "{\"username\": \"newuser\", \"email\": \"new@example.com\"}"
            },
            meta = new
            {
                username = "newuser",
                email = "new@example.com",
                displayName = "",
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

    private async Task CompleteBasicInfo(string userId)
    {
        var basicInfo = new
        {
            displayName = "New User",
            bio = "This is a complete bio with more than 10 characters"
        };

        var json = JsonSerializer.Serialize(basicInfo);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task AddAvatar(string userId)
    {
        var avatarData = new
        {
            avatarUrl = "https://example.com/avatar.jpg"
        };

        var json = JsonSerializer.Serialize(avatarData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task AddInterests(string userId)
    {
        var interestsData = new
        {
            interests = new[] { "technology", "AI", "programming" }
        };

        var json = JsonSerializer.Serialize(interestsData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task AddLocation(string userId)
    {
        var locationData = new
        {
            location = "San Francisco, CA",
            latitude = 37.7749,
            longitude = -122.4194
        };

        var json = JsonSerializer.Serialize(locationData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/auth/profile/{userId}", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task AddBeliefSystem(string userId)
    {
        var beliefData = new
        {
            userId = userId,
            framework = "scientific",
            principles = new[] { "evidence-based thinking" },
            values = new[] { "truth", "clarity" },
            language = "en",
            culturalContext = "western",
            resonanceThreshold = 0.8
        };

        var json = JsonSerializer.Serialize(beliefData);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/userconcept/belief-system/register", requestContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion
}
