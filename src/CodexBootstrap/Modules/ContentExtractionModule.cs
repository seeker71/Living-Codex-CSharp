using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using System.Collections.Generic;

namespace CodexBootstrap.Modules;

/// <summary>
/// Content Extraction Module - Advanced content extraction for news articles and web content
/// Uses hybrid strategy with RSS parsing, HTML content extraction, and headless browser fallback
/// </summary>
[MetaNode(Id = "codex.content-extraction", Name = "Content Extraction Module", Description = "Advanced content extraction system for news and web content")]
public sealed class ContentExtractionModule : ModuleBase
{
    private readonly HttpClient _httpClient;
    private new readonly ICodexLogger _logger;
    private new readonly INodeRegistry _registry;

    public override string Name => "Content Extraction Module";
    public override string Description => "Advanced content extraction system for news and web content";
    public override string Version => "1.0.0";

    public ContentExtractionModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _registry = registry;
        _logger = logger;
        _httpClient = httpClient;
        _logger.Info("ContentExtractionModule constructor called");
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.content-extraction",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "content", "extraction", "news", "html", "rss", "parsing", "headless" },
            capabilities: new[] {
                "rss-parsing", "html-content-extraction", "article-extraction",
                "headless-browser", "content-cleaning", "text-processing"
            },
            spec: "codex.spec.content-extraction"
        );
    }

    // Main content extraction method following the hybrid strategy
    [ApiRoute("POST", "/content/extract", "Extract Content", "Extract content from URL using hybrid strategy", "codex.content-extraction")]
    public async Task<object> ExtractContent([ApiParameter("request", "Content extraction request", Required = true, Location = "body")] ContentExtractionRequest request)
    {
        try
        {
            var result = await ExtractContentFromUrl(request.Url, request.UseHeadlessBrowser);
            return new ContentExtractionResponse(result.Content, result.ContentType, result.Success, result.MethodUsed, result.Metadata);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error extracting content from {request.Url}: {ex.Message}", ex);
            return new ErrorResponse($"Error extracting content: {ex.Message}");
        }
    }

    // RSS Feed parsing with improved error handling
    [ApiRoute("POST", "/content/parse-rss", "Parse RSS Feed", "Parse RSS/Atom feed from URL", "codex.content-extraction")]
    public async Task<object> ParseRssFeed([ApiParameter("request", "RSS feed request", Required = true, Location = "body")] RssFeedRequest request)
    {
        try
        {
            var feedItems = await ParseRssFeedAsync(request.FeedUrl, request.MaxItems);
            return new RssFeedResponse(feedItems, feedItems.Count, "RSS feed parsed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error parsing RSS feed {request.FeedUrl}: {ex.Message}", ex);
            return new ErrorResponse($"Error parsing RSS feed: {ex.Message}");
        }
    }

    // Advanced content extraction implementation
    public async Task<ContentExtractionResult> ExtractContentFromUrl(string url, bool useHeadlessBrowser = false)
    {
        _logger.Info($"Extracting content from URL: {url} (headless: {useHeadlessBrowser})");

        try
        {
            // Step 1: Try RSS/Atom feed parsing first
            var (rssSuccess, rssResult) = await TryExtractFromRssFeed(url);
            if (rssSuccess)
            {
                _logger.Info($"Content extracted from RSS feed for: {url}");
                return rssResult;
            }

            // Step 2: Fetch HTML content
            var html = await FetchHtmlContent(url);
            if (string.IsNullOrEmpty(html))
            {
                return new ContentExtractionResult("", "text/plain", false, "fetch_failed", new Dictionary<string, object> { ["error"] = "Failed to fetch HTML content" });
            }

            // Step 3: Try advanced HTML content extraction
            var htmlResult = ExtractContentFromHtml(html, url);
            if (htmlResult.Success && !string.IsNullOrEmpty(htmlResult.Content))
            {
                _logger.Info($"Content extracted from HTML for: {url} (method: {htmlResult.MethodUsed})");
                return htmlResult;
            }

            // Step 4: Fallback to headless browser if requested
            if (useHeadlessBrowser)
            {
                var headlessResult = await ExtractContentWithHeadlessBrowser(url);
                if (headlessResult.Success)
                {
                    _logger.Info($"Content extracted with headless browser for: {url}");
                    return headlessResult;
                }
            }

            // Final fallback: return basic text extraction
            var fallbackResult = ExtractBasicText(html, url);
            _logger.Warn($"Using fallback text extraction for: {url}");
            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in content extraction for {url}: {ex.Message}", ex);
            return new ContentExtractionResult("", "text/plain", false, "error", new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["stackTrace"] = ex.StackTrace ?? ""
            });
        }
    }

    private async Task<(bool success, ContentExtractionResult result)> TryExtractFromRssFeed(string url)
    {
        var result = new ContentExtractionResult("", "text/plain", false, "none", new Dictionary<string, object>());

        try
        {
            var feedItems = await ParseRssFeedAsync(url, 1);
            if (feedItems.Any())
            {
                var item = feedItems.First();
                result = new ContentExtractionResult(
                    $"{item.Title}\n\n{item.Description}\n\n{item.Content}",
                    "text/plain",
                    true,
                    "rss_feed",
                    new Dictionary<string, object>
                    {
                        ["feedTitle"] = item.Title,
                        ["publishedAt"] = item.PublishedAt,
                        ["author"] = item.Author ?? "",
                        ["source"] = item.Source
                    }
                );
                return (true, result);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"RSS extraction failed for {url}: {ex.Message}");
        }

        return (false, result);
    }

    private async Task<string> FetchHtmlContent(string url)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LivingCodex/1.0 (+https://livingcodex.org)");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.Warn($"Failed to fetch HTML from {url}: {response.StatusCode}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching HTML from {url}: {ex.Message}", ex);
            return string.Empty;
        }
    }

    private ContentExtractionResult ExtractContentFromHtml(string html, string url)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script and style elements
            RemoveNoiseElements(doc);

            // Try multiple content extraction strategies
            var strategies = new[]
            {
                () => ExtractWithReadability(doc, url),
                () => ExtractWithHeuristics(doc, url),
                () => ExtractWithSelectors(doc, url)
            };

            foreach (var strategy in strategies)
            {
                var result = strategy();
                if (result.Success && !string.IsNullOrEmpty(result.Content))
                {
                    return result;
                }
            }

            return new ContentExtractionResult("", "text/plain", false, "no_content_found", new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.Error($"Error extracting HTML content from {url}: {ex.Message}", ex);
            return new ContentExtractionResult("", "text/plain", false, "error", new Dictionary<string, object> { ["error"] = ex.Message });
        }
    }

    private async Task<ContentExtractionResult> ExtractContentWithHeadlessBrowser(string url)
    {
        try
        {
            // Check if we need to download browser
            await new BrowserFetcher().DownloadAsync();

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            });

            var page = await browser.NewPageAsync();
            await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);

            // Wait a bit for dynamic content to load
            await Task.Delay(2000);

            var content = await page.GetContentAsync();
            await browser.CloseAsync();

            return ExtractContentFromHtml(content, url);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error with headless browser extraction for {url}: {ex.Message}", ex);
            return new ContentExtractionResult("", "text/plain", false, "headless_error", new Dictionary<string, object> { ["error"] = ex.Message });
        }
    }

    private ContentExtractionResult ExtractBasicText(string html, string url)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script and style elements
            RemoveNoiseElements(doc);

            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                var text = CleanText(bodyNode.InnerText);
                return new ContentExtractionResult(text, "text/plain", true, "basic_text", new Dictionary<string, object>());
            }

            return new ContentExtractionResult(CleanText(doc.DocumentNode.InnerText), "text/plain", true, "basic_text", new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in basic text extraction for {url}: {ex.Message}", ex);
            return new ContentExtractionResult("", "text/plain", false, "error", new Dictionary<string, object> { ["error"] = ex.Message });
        }
    }

    // Advanced content extraction methods
    private ContentExtractionResult ExtractWithReadability(HtmlDocument doc, string url)
    {
        // This is a simplified readability-like algorithm
        // In a production system, you might want to use a dedicated library

        var article = doc.DocumentNode.SelectSingleNode("//article");
        if (article != null)
        {
            var content = ExtractReadableContent(article);
            if (!string.IsNullOrEmpty(content))
            {
                return new ContentExtractionResult(content, "text/html", true, "readability_article", new Dictionary<string, object>());
            }
        }

        return new ContentExtractionResult("", "text/plain", false, "no_readability_content", new Dictionary<string, object>());
    }

    private ContentExtractionResult ExtractWithHeuristics(HtmlDocument doc, string url)
    {
        // Find the largest content block with good text density
        var contentNodes = doc.DocumentNode.DescendantsAndSelf()
            .Where(n => n.Name == "div" || n.Name == "p" || n.Name == "article" || n.Name == "section")
            .Where(n => !string.IsNullOrEmpty(n.InnerText?.Trim()))
            .ToList();

        if (!contentNodes.Any())
        {
            return new ContentExtractionResult("", "text/plain", false, "no_content_nodes", new Dictionary<string, object>());
        }

        // Score nodes by text density and size
        var scoredNodes = contentNodes
            .Select(node => new
            {
                Node = node,
                Score = CalculateContentScore(node),
                Text = CleanText(node.InnerText)
            })
            .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.Length > 200)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scoredNodes.Any())
        {
            var bestNode = scoredNodes.First();
            return new ContentExtractionResult(bestNode.Text, "text/plain", true, "heuristics", new Dictionary<string, object>
            {
                ["score"] = bestNode.Score,
                ["nodeType"] = bestNode.Node.Name
            });
        }

        return new ContentExtractionResult("", "text/plain", false, "no_good_content", new Dictionary<string, object>());
    }

    private ContentExtractionResult ExtractWithSelectors(HtmlDocument doc, string url)
    {
        var contentSelectors = new[]
        {
            "article",
            "main",
            ".content",
            ".post-content",
            ".entry-content",
            ".article-content",
            ".story-content",
            "[role='main']",
            ".article-body",
            ".post-body",
            ".entry-body",
            "#content",
            "#main",
            ".main-content",
            ".post",
            ".entry"
        };

        foreach (var selector in contentSelectors)
        {
            var contentNode = doc.DocumentNode.SelectSingleNode($"//{selector}");
            if (contentNode != null)
            {
                var text = CleanText(contentNode.InnerText);
                if (!string.IsNullOrEmpty(text) && text.Length > 100)
                {
                    return new ContentExtractionResult(text, "text/plain", true, $"selector_{selector}", new Dictionary<string, object>());
                }
            }
        }

        return new ContentExtractionResult("", "text/plain", false, "no_selectors_found", new Dictionary<string, object>());
    }

    private string ExtractReadableContent(HtmlNode node)
    {
        // Extract readable content from a node
        var readableNodes = node.DescendantsAndSelf()
            .Where(n => n.Name == "p" || n.Name == "h1" || n.Name == "h2" || n.Name == "h3" ||
                       n.Name == "h4" || n.Name == "h5" || n.Name == "h6" ||
                       (n.Name == "div" && !HasOnlyInlineElements(n)))
            .Where(n => !string.IsNullOrEmpty(n.InnerText?.Trim()) && n.InnerText.Trim().Length > 20)
            .ToList();

        return string.Join("\n\n", readableNodes.Select(n => CleanText(n.InnerText)));
    }

    private bool HasOnlyInlineElements(HtmlNode node)
    {
        var inlineElements = new[] { "a", "span", "em", "strong", "b", "i", "u", "small", "mark", "abbr", "cite" };
        var childElements = node.Descendants().Where(n => !string.IsNullOrEmpty(n.Name)).Select(n => n.Name).ToList();
        return childElements.All(e => inlineElements.Contains(e));
    }

    private double CalculateContentScore(HtmlNode node)
    {
        var text = node.InnerText ?? "";
        var textLength = text.Length;
        var linkTextLength = node.Descendants("a").Sum(a => (a.InnerText ?? "").Length);
        var tagCount = node.Descendants().Count();

        // Score based on text density (prefer content over navigation/links)
        var textDensity = textLength > 0 ? (double)(textLength - linkTextLength) / textLength : 0;
        var sizeScore = Math.Min(textLength / 1000.0, 2.0); // Cap at 2.0
        var complexityPenalty = Math.Min(tagCount / 50.0, 1.0); // Penalize complex markup

        return (textDensity * 0.6) + (sizeScore * 0.3) - (complexityPenalty * 0.1);
    }

    private void RemoveNoiseElements(HtmlDocument doc)
    {
        var noiseSelectors = new[]
        {
            "script", "style", "nav", "header", "footer", "aside", "noscript",
            ".nav", ".navigation", ".header", ".footer", ".sidebar", ".aside",
            ".advertisement", ".ads", ".social", ".share", ".comment", ".comments",
            ".related", ".recommended", ".sponsored", ".promo", ".newsletter"
        };

        foreach (var selector in noiseSelectors)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{selector}") ?? new HtmlNodeCollection(doc.DocumentNode);
            foreach (var node in nodes.ToList())
            {
                node.Remove();
            }
        }
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        // Remove common noise patterns
        var noisePatterns = new[]
        {
            @"Advertisement\s*",
            @"Subscribe\s*",
            @"Follow us\s*",
            @"Share this\s*",
            @"Read more\s*",
            @"Continue reading\s*",
            @"Also read\s*",
            @"Related\s*",
            @"Sponsored\s*",
            @"Loading\s*",
            @"\[\s*\d+\s*\]"
        };

        foreach (var pattern in noisePatterns)
        {
            text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
        }

        // Remove excessive punctuation
        text = Regex.Replace(text, @"[.,!?;:]{3,}", "...");

        return text.Trim();
    }

    // RSS Feed parsing implementation
    public async Task<List<RssFeedItem>> ParseRssFeedAsync(string feedUrl, int maxItems = 50)
    {
        var feedItems = new List<RssFeedItem>();

        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LivingCodex/1.0 (+https://livingcodex.org)");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Handle redirects and HTML landing pages
            var finalUrl = await ResolveFeedUrl(feedUrl);
            if (string.IsNullOrEmpty(finalUrl))
            {
                _logger.Warn($"Could not resolve feed URL: {feedUrl}");
                return feedItems;
            }

            var response = await _httpClient.GetAsync(finalUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn($"Feed request failed: {response.StatusCode} for {finalUrl}");
                return feedItems;
            }

            var content = await response.Content.ReadAsStringAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            // If we got HTML, try to discover RSS link
            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                var discoveredFeedUrl = ExtractRssLinkFromHtml(content, finalUrl);
                if (!string.IsNullOrWhiteSpace(discoveredFeedUrl))
                {
                    _logger.Info($"Discovered RSS feed: {discoveredFeedUrl} from HTML page");
                    finalUrl = discoveredFeedUrl;
                    response = await _httpClient.GetAsync(finalUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Warn($"Discovered feed request failed: {response.StatusCode}");
                        return feedItems;
                    }
                    content = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _logger.Warn($"HTML page doesn't contain RSS feed link: {feedUrl}");
                    return feedItems;
                }
            }

            // Parse the RSS/Atom feed
            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null
            });

            var feed = SyndicationFeed.Load(xmlReader);
            if (feed == null)
            {
                _logger.Warn($"Failed to parse feed: {finalUrl}");
                return feedItems;
            }

            _logger.Info($"Successfully parsed RSS feed: {feed.Title?.Text ?? "Unknown"} with {feed.Items?.Count() ?? 0} items");

            foreach (var item in feed.Items?.Take(maxItems) ?? Enumerable.Empty<SyndicationItem>())
            {
                feedItems.Add(new RssFeedItem(
                    Title: item.Title?.Text ?? "",
                    Description: item.Summary?.Text ?? "",
                    Content: ExtractContentFromSyndicationItem(item),
                    Url: item.Links?.FirstOrDefault()?.Uri?.ToString() ?? "",
                    PublishedAt: item.PublishDate,
                    Author: item.Authors?.FirstOrDefault()?.Name ?? "",
                    Source: feed.Title?.Text ?? "Unknown"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error parsing RSS feed {feedUrl}: {ex.Message}", ex);
        }

        return feedItems;
    }

    private async Task<string> ResolveFeedUrl(string feedUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode ? feedUrl : null;
        }
        catch
        {
            return null;
        }
    }

    private string ExtractRssLinkFromHtml(string html, string baseUrl)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Look for RSS/Atom link elements in head
            var linkNodes = doc.DocumentNode.SelectNodes("//head//link");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes)
                {
                    var rel = link.GetAttributeValue("rel", "");
                    var type = link.GetAttributeValue("type", "");
                    var href = link.GetAttributeValue("href", "");

                    if ((rel.Contains("alternate") || rel.Contains("feed")) &&
                        (type.Contains("rss") || type.Contains("atom") || type.Contains("xml")) &&
                        !string.IsNullOrEmpty(href))
                    {
                        // Convert relative URLs to absolute
                        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
                            Uri.TryCreate(href, UriKind.Relative, out var relativeUri))
                        {
                            return new Uri(baseUri, relativeUri).ToString();
                        }
                        return href;
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractContentFromSyndicationItem(SyndicationItem item)
    {
        // Prefer content:encoded, then summary, then description
        if (item.ElementExtensions.Any(e => e.OuterName == "encoded" && e.OuterNamespace == "http://purl.org/rss/1.0/modules/content/"))
        {
            var encodedExtension = item.ElementExtensions
                .First(e => e.OuterName == "encoded" && e.OuterNamespace == "http://purl.org/rss/1.0/modules/content/");
            if (encodedExtension.GetObject<XElement>()?.Value != null)
            {
                return CleanText(encodedExtension.GetObject<XElement>().Value);
            }
        }

        return item.Summary?.Text ?? item.Content?.ToString() ?? "";
    }
}

// Request/Response types
[MetaNode(Id = "codex.content-extraction.request", Name = "Content Extraction Request", Description = "Request for content extraction")]
public record ContentExtractionRequest(
    string Url,
    bool UseHeadlessBrowser = false
);

[MetaNode(Id = "codex.content-extraction.response", Name = "Content Extraction Response", Description = "Response from content extraction")]
public record ContentExtractionResponse(
    string Content,
    string ContentType,
    bool Success,
    string MethodUsed,
    Dictionary<string, object> Metadata
);

[MetaNode(Id = "codex.content-extraction.result", Name = "Content Extraction Result", Description = "Result of content extraction operation")]
public record ContentExtractionResult(
    string Content,
    string ContentType,
    bool Success,
    string MethodUsed,
    Dictionary<string, object> Metadata
);

[MetaNode(Id = "codex.content-extraction.rss-request", Name = "RSS Feed Request", Description = "Request for RSS feed parsing")]
public record RssFeedRequest(
    string FeedUrl,
    int MaxItems = 50
);

[MetaNode(Id = "codex.content-extraction.rss-response", Name = "RSS Feed Response", Description = "Response from RSS feed parsing")]
public record RssFeedResponse(
    List<RssFeedItem> Items,
    int TotalCount,
    string Message
);

[MetaNode(Id = "codex.content-extraction.rss-item", Name = "RSS Feed Item", Description = "Item from RSS feed")]
public record RssFeedItem(
    string Title,
    string Description,
    string Content,
    string Url,
    DateTimeOffset PublishedAt,
    string Author,
    string Source
);
