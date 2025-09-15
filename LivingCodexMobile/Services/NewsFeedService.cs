using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface INewsFeedService
{
    Task<List<NewsItem>> GetNewsFeedAsync(string userId);
    Task<List<NewsItem>> SearchNewsAsync(string query);
}

public class NewsFeedService : INewsFeedService
{
    private readonly IApiService _apiService;

    public NewsFeedService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<NewsItem>> GetNewsFeedAsync(string userId)
    {
        try
        {
            var response = await _apiService.GetAsync<NewsResponse>($"/news/feed/{Uri.EscapeDataString(userId)}");
            return response?.Items ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news feed error: {ex.Message}");
            return GetDemoNewsItems();
        }
    }

    public async Task<List<NewsItem>> SearchNewsAsync(string query)
    {
        try
        {
            var payload = new { query = query };
            var response = await _apiService.PostAsync<object, NewsResponse>("/news/search", payload);
            return response?.Items ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search news error: {ex.Message}");
            return new List<NewsItem>();
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

public class NewsResponse
{
    public List<NewsItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

