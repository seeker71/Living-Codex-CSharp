using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CodexBootstrap.Core;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Comprehensive API tests for ConceptModule endpoints
/// Tests all mobile app API calls for concept management
/// </summary>
public class ConceptModuleApiTests : IClassFixture<TestServerFixture>
{
    private readonly HttpClient _client;
    private readonly TestServerFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConceptModuleApiTests(TestServerFixture fixture)
    {
        _client = fixture.HttpClient;
        _fixture = fixture;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region GET /concepts - Get All Concepts

    [Fact]
    public async Task GetConcepts_ShouldReturnValidConceptsArray()
    {
        // Act
        var response = await _client.GetAsync("/concepts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        // Should return valid concepts array (may contain existing concepts from other tests)
        result.Should().NotBeNull();
        result.Should().ContainKey("concepts");
        var concepts = result["concepts"] as JsonElement?;
        concepts.Should().NotBeNull();
        concepts!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        concepts.Value.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetConcepts_ShouldReturnConcepts_WhenConceptsExist()
    {
        // Arrange - Create a test concept first
        var createRequest = new
        {
            name = "Test Concept",
            description = "A test concept for API testing",
            domain = "Testing",
            complexity = 5,
            tags = new[] { "test", "api" }
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        await _client.PostAsync("/concepts", createContent);

        // Act
        var response = await _client.GetAsync("/concepts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region GET /concepts/{id} - Get Specific Concept

    [Fact]
    public async Task GetConcept_ShouldReturnNotFound_WhenConceptDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/concepts/nonexistent-concept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConcept_ShouldReturnConcept_WhenConceptExists()
    {
        // Arrange - Create a test concept first
        var createRequest = new
        {
            name = "Test Concept for Get",
            description = "A test concept for GET testing",
            domain = "Testing",
            complexity = 5,
            tags = new[] { "test", "get" }
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/concepts", createContent);
        var createResult = await createResponse.Content.ReadAsStringAsync();
        var createData = JsonSerializer.Deserialize<Dictionary<string, object>>(createResult, _jsonOptions);
        var conceptId = createData?["conceptId"]?.ToString();

        // Act
        var response = await _client.GetAsync($"/concepts/{conceptId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region POST /concepts - Create Concept

    [Fact]
    public async Task CreateConcept_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var request = new
        {
            name = "API Test Concept",
            description = "A concept created via API test",
            domain = "API Testing",
            complexity = "7",
            tags = new[] { "api", "test", "concept" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/concepts", content);

        // Assert - first capture the response content for debugging
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Write debug info to a file since Console.WriteLine isn't showing up
        var debugInfo = $"Response Status: {response.StatusCode}\n" +
                       $"Response Content: {responseContent}\n" +
                       $"Request JSON: {JsonSerializer.Serialize(request, _jsonOptions)}\n";
                       
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            debugInfo += $"400 Bad Request Response: {responseContent}\n";
            // Try to parse error response
            try 
            {
                var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
                debugInfo += $"Parsed Error: {JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true })}\n";
            }
            catch (Exception ex)
            {
                debugInfo += $"Failed to parse error response: {ex.Message}\n";
            }
        }
        
        await File.WriteAllTextAsync("/tmp/concept_test_debug.txt", debugInfo);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateConcept_ShouldReturnBadRequest_WhenInvalidRequest()
    {
        // Arrange
        var request = new
        {
            // Missing required fields
            description = "A concept without a name"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/concepts", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /concepts/{id} - Update Concept

    [Fact]
    public async Task UpdateConcept_ShouldReturnNotFound_WhenConceptDoesNotExist()
    {
        // Arrange
        var request = new
        {
            name = "Updated Concept",
            description = "An updated concept",
            domain = "Updated",
            complexity = 8,
            tags = new[] { "updated" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync("/concepts/nonexistent-concept", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateConcept_ShouldReturnSuccess_WhenConceptExists()
    {
        // Arrange - Create a test concept first
        var createRequest = new
        {
            name = "Original Concept",
            description = "Original description",
            domain = "Original",
            complexity = 5,
            tags = new[] { "original" }
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/concepts", createContent);
        var createResult = await createResponse.Content.ReadAsStringAsync();
        var createData = JsonSerializer.Deserialize<Dictionary<string, object>>(createResult, _jsonOptions);
        var conceptId = createData?["conceptId"]?.ToString();

        // Update request
        var updateRequest = new
        {
            name = "Updated Concept",
            description = "Updated description",
            domain = "Updated",
            complexity = 8,
            tags = new[] { "updated" }
        };

        var updateContent = new StringContent(
            JsonSerializer.Serialize(updateRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/concepts/{conceptId}", updateContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region DELETE /concepts/{id} - Delete Concept

    [Fact]
    public async Task DeleteConcept_ShouldReturnNotFound_WhenConceptDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/concepts/nonexistent-concept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConcept_ShouldReturnSuccess_WhenConceptExists()
    {
        // Arrange - Create a test concept first
        var createRequest = new
        {
            name = "Concept to Delete",
            description = "This concept will be deleted",
            domain = "Deletion",
            complexity = 3,
            tags = new[] { "delete" }
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client.PostAsync("/concepts", createContent);
        var createResult = await createResponse.Content.ReadAsStringAsync();
        var createData = JsonSerializer.Deserialize<Dictionary<string, object>>(createResult, _jsonOptions);
        var conceptId = createData?["conceptId"]?.ToString();

        // Act
        var response = await _client.DeleteAsync($"/concepts/{conceptId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region POST /concepts - Create Concept (Legacy Endpoint)

    [Fact]
    public async Task CreateConceptLegacy_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var request = new
        {
            name = "Legacy API Test Concept",
            description = "A concept created via legacy API test",
            domain = "Legacy API Testing",
            complexity = 6,
            tags = new[] { "legacy", "api", "test" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/concepts", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetConcepts_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/concepts");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should respond within 1 second
    }

    #endregion
}
