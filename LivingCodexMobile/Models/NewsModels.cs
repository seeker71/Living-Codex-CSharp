using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public class NewsItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
    public bool IsRead { get; set; }
    public double RelevanceScore { get; set; }
    public double Resonance { get; set; }
    public double Energy { get; set; }
    public List<Concept> RelatedConcepts { get; set; } = new();
}


public class NewsSearchRequest
{
    public string? Query { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class NewsReadRequest
{
    public string UserId { get; set; } = string.Empty;
    public string NewsId { get; set; } = string.Empty;
}

// Response models
public class NewsFeedResponse
{
    public List<NewsItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
}

public class NewsItemResponse
{
    public NewsItem? Item { get; set; }
}

public class TrendingTopic
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MentionCount { get; set; }
    public double TrendScore { get; set; }
    public List<string> RelatedTags { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public string Domain { get; set; } = string.Empty;
    public int Complexity { get; set; }
    public string[] Tags { get; set; } = new string[0];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double Resonance { get; set; }
    public double Energy { get; set; }
    public string Type { get; set; } = "concept"; // "concept", "news", etc.
}

public class TrendingTopicsResponse
{
    public bool Success { get; set; }
    public List<TrendingTopic> Topics { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

