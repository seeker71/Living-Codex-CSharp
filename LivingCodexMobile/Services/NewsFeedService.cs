using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface INewsFeedService
{
    Task<List<NewsItem>> GetNewsFeedAsync();
    Task<List<NewsItem>> GetPersonalizedNewsAsync(string userId);
    Task<NewsAnalysis> AnalyzeNewsItemAsync(string newsId);
    Task<List<Concept>> ExtractConceptsAsync(string content);
}

public class NewsFeedService : INewsFeedService
{
    private readonly HttpClient _httpClient;
    private readonly IApiService _apiService;

    public NewsFeedService(HttpClient httpClient, IApiService apiService)
    {
        _httpClient = httpClient;
        _apiService = apiService;
    }

    public async Task<List<NewsItem>> GetNewsFeedAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:5002/news/sources");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var newsResponse = JsonSerializer.Deserialize<NewsResponse>(content);
                return newsResponse?.Items ?? new List<NewsItem>();
            }
            
            // Fallback to demo news if backend is not available
            return GetDemoNewsItems();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news feed error: {ex.Message}");
            return GetDemoNewsItems();
        }
    }

    public async Task<List<NewsItem>> GetPersonalizedNewsAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://localhost:5002/news/personalized/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var newsResponse = JsonSerializer.Deserialize<NewsResponse>(content);
                return newsResponse?.Items ?? new List<NewsItem>();
            }
            
            return await GetNewsFeedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get personalized news error: {ex.Message}");
            return await GetNewsFeedAsync();
        }
    }

    public async Task<NewsAnalysis> AnalyzeNewsItemAsync(string newsId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://localhost:5002/news/analyze/{newsId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NewsAnalysis>(content) ?? new NewsAnalysis();
            }
            
            return new NewsAnalysis();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Analyze news error: {ex.Message}");
            return new NewsAnalysis();
        }
    }

    public async Task<List<Concept>> ExtractConceptsAsync(string content)
    {
        try
        {
            var request = new { content = content };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("http://localhost:5002/ai/extract-concepts", httpContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var conceptsResponse = JsonSerializer.Deserialize<ConceptsResponse>(responseContent);
                return conceptsResponse?.Concepts ?? new List<Concept>();
            }
            
            return new List<Concept>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extract concepts error: {ex.Message}");
            return new List<Concept>();
        }
    }

    private List<NewsItem> GetDemoNewsItems()
    {
        return new List<NewsItem>
        {
            new NewsItem
            {
                Id = "1",
                Title = "AI Breakthrough in Quantum Computing",
                Content = "Scientists have made a significant breakthrough in quantum computing that could revolutionize how we process information.",
                Source = "Tech News",
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                Url = "https://example.com/quantum-ai",
                Categories = new List<string> { "Technology", "AI", "Quantum Computing" },
                Resonance = 8.5,
                Energy = 7.2
            },
            new NewsItem
            {
                Id = "2",
                Title = "Climate Change Solutions Through Innovation",
                Content = "New innovative approaches to climate change are showing promising results in reducing carbon emissions.",
                Source = "Environmental News",
                PublishedAt = DateTime.UtcNow.AddHours(-4),
                Url = "https://example.com/climate-solutions",
                Categories = new List<string> { "Environment", "Innovation", "Climate" },
                Resonance = 9.1,
                Energy = 8.7
            },
            new NewsItem
            {
                Id = "3",
                Title = "Space Exploration Reaches New Milestone",
                Content = "Humanity's exploration of space has reached a new milestone with successful missions to distant planets.",
                Source = "Space News",
                PublishedAt = DateTime.UtcNow.AddHours(-6),
                Url = "https://example.com/space-milestone",
                Categories = new List<string> { "Space", "Exploration", "Science" },
                Resonance = 7.8,
                Energy = 6.9
            },
            new NewsItem
            {
                Id = "4",
                Title = "Renewable Energy Adoption Accelerates",
                Content = "Global adoption of renewable energy sources is accelerating faster than predicted, bringing us closer to sustainability goals.",
                Source = "Energy News",
                PublishedAt = DateTime.UtcNow.AddHours(-8),
                Url = "https://example.com/renewable-energy",
                Categories = new List<string> { "Energy", "Renewable", "Sustainability" },
                Resonance = 8.9,
                Energy = 7.5
            },
            new NewsItem
            {
                Id = "5",
                Title = "Medical Breakthrough in Cancer Treatment",
                Content = "Researchers have developed a new approach to cancer treatment that shows remarkable effectiveness in early trials.",
                Source = "Medical News",
                PublishedAt = DateTime.UtcNow.AddHours(-12),
                Url = "https://example.com/cancer-treatment",
                Categories = new List<string> { "Medicine", "Cancer", "Research" },
                Resonance = 9.5,
                Energy = 8.9
            }
        };
    }
}

public class NewsItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public double Resonance { get; set; }
    public double Energy { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
}

public class NewsAnalysis
{
    public string NewsId { get; set; } = string.Empty;
    public List<string> ExtractedConcepts { get; set; } = new();
    public double SentimentScore { get; set; }
    public List<string> KeyTopics { get; set; } = new();
    public double RelevanceScore { get; set; }
    public List<string> RelatedConcepts { get; set; } = new();
}

public class NewsResponse
{
    public List<NewsItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ConceptsResponse
{
    public List<Concept> Concepts { get; set; } = new();
    public int Count { get; set; }
}

