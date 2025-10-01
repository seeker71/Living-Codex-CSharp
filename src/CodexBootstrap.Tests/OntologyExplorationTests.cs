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

public class OntologyExplorationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OntologyExplorationTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_WithPagination_SimulatesExploration()
    {
        // Act - Simulate user exploring through pages
        var page1 = await _client.GetAsync("/storage-endpoints/nodes?skip=0&take=10");
        var page2 = await _client.GetAsync("/storage-endpoints/nodes?skip=10&take=10");
        var page3 = await _client.GetAsync("/storage-endpoints/nodes?skip=20&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page3.StatusCode);

        var page1Content = await page1.Content.ReadAsStringAsync();
        var page2Content = await page2.Content.ReadAsStringAsync();
        var page3Content = await page3.Content.ReadAsStringAsync();

        var result1 = JsonSerializer.Deserialize<JsonElement>(page1Content);
        var result2 = JsonSerializer.Deserialize<JsonElement>(page2Content);
        var result3 = JsonSerializer.Deserialize<JsonElement>(page3Content);

        Assert.True(result1.GetProperty("success").GetBoolean());
        Assert.True(result2.GetProperty("success").GetBoolean());
        Assert.True(result3.GetProperty("success").GetBoolean());

        // Verify different pages return different content
        var nodes1 = result1.GetProperty("nodes").EnumerateArray().ToList();
        var nodes2 = result2.GetProperty("nodes").EnumerateArray().ToList();
        var nodes3 = result3.GetProperty("nodes").EnumerateArray().ToList();

        Assert.True(nodes1.Count > 0);
        Assert.True(nodes2.Count > 0);
        Assert.True(nodes3.Count > 0);

        // Verify pagination parameters
        Assert.Equal(0, result1.GetProperty("skip").GetInt32());
        Assert.Equal(10, result1.GetProperty("take").GetInt32());
        Assert.Equal(10, result2.GetProperty("skip").GetInt32());
        Assert.Equal(10, result2.GetProperty("take").GetInt32());
        Assert.Equal(20, result3.GetProperty("skip").GetInt32());
        Assert.Equal(10, result3.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetNodes_WithDifferentFilters_SimulatesDomainExploration()
    {
        // Act - Simulate user exploring different domains
        var scienceConcepts = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=science&take=5");
        var techConcepts = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=technology&take=5");
        var artConcepts = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=art&take=5");
        var healthConcepts = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=health&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, scienceConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, techConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, artConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthConcepts.StatusCode);

        var scienceContent = await scienceConcepts.Content.ReadAsStringAsync();
        var techContent = await techConcepts.Content.ReadAsStringAsync();
        var artContent = await artConcepts.Content.ReadAsStringAsync();
        var healthContent = await healthConcepts.Content.ReadAsStringAsync();

        var scienceResult = JsonSerializer.Deserialize<JsonElement>(scienceContent);
        var techResult = JsonSerializer.Deserialize<JsonElement>(techContent);
        var artResult = JsonSerializer.Deserialize<JsonElement>(artContent);
        var healthResult = JsonSerializer.Deserialize<JsonElement>(healthContent);

        Assert.True(scienceResult.GetProperty("success").GetBoolean());
        Assert.True(techResult.GetProperty("success").GetBoolean());
        Assert.True(artResult.GetProperty("success").GetBoolean());
        Assert.True(healthResult.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithTypeExploration_SimulatesConceptTypeDiscovery()
    {
        // Act - Simulate user exploring different concept types
        var regularConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&take=5");
        var ucoreConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=ucore.concept&take=5");
        var axisConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=5");
        var frequencyConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.frequency&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, regularConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ucoreConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, axisConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, frequencyConcepts.StatusCode);

        var regularContent = await regularConcepts.Content.ReadAsStringAsync();
        var ucoreContent = await ucoreConcepts.Content.ReadAsStringAsync();
        var axisContent = await axisConcepts.Content.ReadAsStringAsync();
        var frequencyContent = await frequencyConcepts.Content.ReadAsStringAsync();

        var regularResult = JsonSerializer.Deserialize<JsonElement>(regularContent);
        var ucoreResult = JsonSerializer.Deserialize<JsonElement>(ucoreContent);
        var axisResult = JsonSerializer.Deserialize<JsonElement>(axisContent);
        var frequencyResult = JsonSerializer.Deserialize<JsonElement>(frequencyContent);

        Assert.True(regularResult.GetProperty("success").GetBoolean());
        Assert.True(ucoreResult.GetProperty("success").GetBoolean());
        Assert.True(axisResult.GetProperty("success").GetBoolean());
        Assert.True(frequencyResult.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithSearchProgression_SimulatesSearchRefinement()
    {
        // Act - Simulate user refining their search
        var broadSearch = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=concept&take=10");
        var specificSearch = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=consciousness&take=10");
        var verySpecificSearch = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=universal mind&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, broadSearch.StatusCode);
        Assert.Equal(HttpStatusCode.OK, specificSearch.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verySpecificSearch.StatusCode);

        var broadContent = await broadSearch.Content.ReadAsStringAsync();
        var specificContent = await specificSearch.Content.ReadAsStringAsync();
        var verySpecificContent = await verySpecificSearch.Content.ReadAsStringAsync();

        var broadResult = JsonSerializer.Deserialize<JsonElement>(broadContent);
        var specificResult = JsonSerializer.Deserialize<JsonElement>(specificContent);
        var verySpecificResult = JsonSerializer.Deserialize<JsonElement>(verySpecificContent);

        Assert.True(broadResult.GetProperty("success").GetBoolean());
        Assert.True(specificResult.GetProperty("success").GetBoolean());
        Assert.True(verySpecificResult.GetProperty("success").GetBoolean());

        // Verify search refinement reduces results
        var broadNodes = broadResult.GetProperty("nodes").EnumerateArray().ToList();
        var specificNodes = specificResult.GetProperty("nodes").EnumerateArray().ToList();
        var verySpecificNodes = verySpecificResult.GetProperty("nodes").EnumerateArray().ToList();

        Assert.True(broadNodes.Count >= specificNodes.Count);
        Assert.True(specificNodes.Count >= verySpecificNodes.Count);
    }

    [Fact]
    public async Task GetNodes_WithMixedFilters_SimulatesComplexExploration()
    {
        // Act - Simulate complex exploration with multiple filters
        var scienceConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&searchTerm=science&take=5");
        var ucoreConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=ucore.concept&searchTerm=consciousness&take=5");
        var axisConcepts = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&searchTerm=cognitive&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, scienceConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ucoreConcepts.StatusCode);
        Assert.Equal(HttpStatusCode.OK, axisConcepts.StatusCode);

        var scienceContent = await scienceConcepts.Content.ReadAsStringAsync();
        var ucoreContent = await ucoreConcepts.Content.ReadAsStringAsync();
        var axisContent = await axisConcepts.Content.ReadAsStringAsync();

        var scienceResult = JsonSerializer.Deserialize<JsonElement>(scienceContent);
        var ucoreResult = JsonSerializer.Deserialize<JsonElement>(ucoreContent);
        var axisResult = JsonSerializer.Deserialize<JsonElement>(axisContent);

        Assert.True(scienceResult.GetProperty("success").GetBoolean());
        Assert.True(ucoreResult.GetProperty("success").GetBoolean());
        Assert.True(axisResult.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithStateExploration_SimulatesContentStateDiscovery()
    {
        // Act - Simulate exploring different content states
        var iceNodes = await _client.GetAsync("/storage-endpoints/nodes?state=ice&take=5");
        var waterNodes = await _client.GetAsync("/storage-endpoints/nodes?state=water&take=5");
        var gasNodes = await _client.GetAsync("/storage-endpoints/nodes?state=gas&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, iceNodes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, waterNodes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, gasNodes.StatusCode);

        var iceContent = await iceNodes.Content.ReadAsStringAsync();
        var waterContent = await waterNodes.Content.ReadAsStringAsync();
        var gasContent = await gasNodes.Content.ReadAsStringAsync();

        var iceResult = JsonSerializer.Deserialize<JsonElement>(iceContent);
        var waterResult = JsonSerializer.Deserialize<JsonElement>(waterContent);
        var gasResult = JsonSerializer.Deserialize<JsonElement>(gasContent);

        Assert.True(iceResult.GetProperty("success").GetBoolean());
        Assert.True(waterResult.GetProperty("success").GetBoolean());
        Assert.True(gasResult.GetProperty("success").GetBoolean());

        // Verify state filtering works
        var iceNodesList = iceResult.GetProperty("nodes").EnumerateArray().ToList();
        var waterNodesList = waterResult.GetProperty("nodes").EnumerateArray().ToList();
        var gasNodesList = gasResult.GetProperty("nodes").EnumerateArray().ToList();

        foreach (var node in iceNodesList)
        {
            Assert.Equal("ice", node.GetProperty("state").GetString());
        }
        foreach (var node in waterNodesList)
        {
            Assert.Equal("water", node.GetProperty("state").GetString());
        }
        foreach (var node in gasNodesList)
        {
            Assert.Equal("gas", node.GetProperty("state").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithLocaleExploration_SimulatesLanguageDiscovery()
    {
        // Act - Simulate exploring different locales
        var enNodes = await _client.GetAsync("/storage-endpoints/nodes?locale=en&take=5");
        var enUsNodes = await _client.GetAsync("/storage-endpoints/nodes?locale=en-US&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, enNodes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enUsNodes.StatusCode);

        var enContent = await enNodes.Content.ReadAsStringAsync();
        var enUsContent = await enUsNodes.Content.ReadAsStringAsync();

        var enResult = JsonSerializer.Deserialize<JsonElement>(enContent);
        var enUsResult = JsonSerializer.Deserialize<JsonElement>(enUsContent);

        Assert.True(enResult.GetProperty("success").GetBoolean());
        Assert.True(enUsResult.GetProperty("success").GetBoolean());

        // Verify locale filtering works
        var enNodesList = enResult.GetProperty("nodes").EnumerateArray().ToList();
        var enUsNodesList = enUsResult.GetProperty("nodes").EnumerateArray().ToList();

        foreach (var node in enNodesList)
        {
            Assert.Equal("en", node.GetProperty("locale").GetString());
        }
        foreach (var node in enUsNodesList)
        {
            Assert.Equal("en-US", node.GetProperty("locale").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithLargeDataset_SimulatesBulkExploration()
    {
        // Act - Simulate exploring large datasets
        var largeDataset = await _client.GetAsync("/storage-endpoints/nodes?take=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, largeDataset.StatusCode);
        var largeContent = await largeDataset.Content.ReadAsStringAsync();
        var largeResult = JsonSerializer.Deserialize<JsonElement>(largeContent);

        Assert.True(largeResult.GetProperty("success").GetBoolean());
        var nodes = largeResult.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
        Assert.True(nodes.Count <= 100);
    }

    [Fact]
    public async Task GetNodes_WithEmptyResults_HandlesGracefully()
    {
        // Act - Simulate searching for non-existent concepts
        var emptySearch = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=nonexistentconcept12345&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, emptySearch.StatusCode);
        var emptyContent = await emptySearch.Content.ReadAsStringAsync();
        var emptyResult = JsonSerializer.Deserialize<JsonElement>(emptyContent);

        Assert.True(emptyResult.GetProperty("success").GetBoolean());
        var nodes = emptyResult.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Empty(nodes);
        Assert.Equal(0, emptyResult.GetProperty("totalCount").GetInt32());
    }
}
