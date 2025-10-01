using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using CodexBootstrap.Core;
using Xunit;
using System.Net.Http;
using System.Text;

namespace CodexBootstrap.Tests;

public class OntologyDomainTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OntologyDomainTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Ensure we're using in-memory storage for tests
                services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
                {
                    options.BackgroundServiceExceptionBehavior = Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior.Ignore;
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetNodes_WithScienceKeywords_ReturnsScienceConcepts()
    {
        // Act - Search for science-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=science&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
        
        // Verify science-related concepts are found
        var foundScienceConcept = false;
        foreach (var node in nodes)
        {
            var title = node.GetProperty("title").GetString() ?? "";
            var description = node.GetProperty("description").GetString() ?? "";
            if (title.ToLower().Contains("science") || description.ToLower().Contains("science"))
            {
                foundScienceConcept = true;
                break;
            }
        }
        Assert.True(foundScienceConcept);
    }

    [Fact]
    public async Task GetNodes_WithTechnologyKeywords_ReturnsTechnologyConcepts()
    {
        // Act - Search for technology-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=technology&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
    }

    [Fact]
    public async Task GetNodes_WithArtKeywords_ReturnsArtConcepts()
    {
        // Act - Search for art-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=art&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // May be empty if no art concepts exist, but should not error
    }

    [Fact]
    public async Task GetNodes_WithHealthKeywords_ReturnsHealthConcepts()
    {
        // Act - Search for health-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=health&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // May be empty if no health concepts exist, but should not error
    }

    [Fact]
    public async Task GetNodes_WithBusinessKeywords_ReturnsBusinessConcepts()
    {
        // Act - Search for business-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=business&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // May be empty if no business concepts exist, but should not error
    }

    [Fact]
    public async Task GetNodes_WithNatureKeywords_ReturnsNatureConcepts()
    {
        // Act - Search for nature-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=nature&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // May be empty if no nature concepts exist, but should not error
    }

    [Fact]
    public async Task GetNodes_WithSocietyKeywords_ReturnsSocietyConcepts()
    {
        // Act - Search for society-related concepts
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=society&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // May be empty if no society concepts exist, but should not error
    }

    [Fact]
    public async Task GetNodes_WithUcoreConcepts_ReturnsSpiritualConcepts()
    {
        // Act - Get U-CORE concepts which are often spiritual/metaphysical
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ucore.base&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.ucore.base", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithAxisType_ReturnsOntologyAxes()
    {
        // Act - Get ontology axis nodes
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.ontology.axis", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithFrequencyType_ReturnsFrequencyNodes()
    {
        // Act - Get frequency nodes
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.frequency&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.frequency", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithConceptKeywordType_ReturnsConceptKeywords()
    {
        // Act - Get concept keyword nodes
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept.keyword&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.concept.keyword", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithMultipleDomainKeywords_ReturnsMixedResults()
    {
        // Act - Search for concepts that might span multiple domains
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=research&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
    }

    [Fact]
    public async Task GetNodes_WithEmptyDomainSearch_ReturnsAllConcepts()
    {
        // Act - Search with empty domain term
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
    }

    [Fact]
    public async Task GetNodes_WithSpecialCharacters_HandlesCorrectly()
    {
        // Act - Search with special characters
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=@#$%&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should not crash, may return empty results
    }

    [Fact]
    public async Task GetNodes_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Act - Search with unicode characters
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=测试&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should not crash, may return empty results
    }
}
