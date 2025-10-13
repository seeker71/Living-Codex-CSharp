'use client';

import { useEffect, useState, useCallback } from 'react';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import { endpoints } from '@/lib/api';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';
import { htmlEncode } from '@/lib/utils';
import { NewsConceptsList } from '@/components/lenses/NewsConceptsList';

interface NewsItem {
  id?: string;
  nodeId?: string;
  title: string;
  description: string;
  url: string;
  publishedAt: string;
  source: string;
  author?: string;
  imageUrl?: string;
  content?: string;
  meta?: {
    nodeId?: string;
    newsId?: string;
    originalNewsId?: string;
    [key: string]: unknown;
  };
}

interface TrendingTopic {
  topic: string;
  mentionCount: number;
  trendScore: number;
}

export default function NewsPage() {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [newsItems, setNewsItems] = useState<NewsItem[]>([]);
  const [trendingTopics, setTrendingTopics] = useState<TrendingTopic[]>([]);
  const [sourceStats, setSourceStats] = useState<Record<string, number>>({});
  const [statsTotalCount, setStatsTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<string>('personalized');
  const [searchQuery, setSearchQuery] = useState('');
  const [timeRange, setTimeRange] = useState(24);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(20);
  const [selectedNewsId, setSelectedNewsId] = useState<string | null>(null);
  const [summaries, setSummaries] = useState<Record<string, { text: string; status: string }>>({});
  const [summaryLoading, setSummaryLoading] = useState(false);
  
  // Source preferences state
  const [showSourcePrefs, setShowSourcePrefs] = useState(false);
  const [preferredSources, setPreferredSources] = useState<string[]>([]);
  const [blockedSources, setBlockedSources] = useState<string[]>([]);
  
  // Belief translation state
  const [showTranslations, setShowTranslations] = useState(false);
  const [selectedArticleForTranslation, setSelectedArticleForTranslation] = useState<NewsItem | null>(null);
  const [translations, setTranslations] = useState<any[]>([]);
  
  // Saved articles state
  const [savedArticles, setSavedArticles] = useState<string[]>([]);
  const [showSaved, setShowSaved] = useState(false);
  
  // Source reliability state
  const [sourceReliability, setSourceReliability] = useState<Record<string, number>>({});

  const getNewsNodeId = (item: NewsItem): string | undefined => {
    const candidates = [
      item.id,
      item.nodeId,
      typeof item.meta?.nodeId === 'string' ? item.meta.nodeId : undefined,
      typeof item.meta?.newsId === 'string' ? item.meta.newsId : undefined,
      typeof item.meta?.originalNewsId === 'string' ? item.meta.originalNewsId : undefined,
      typeof (item as any).newsId === 'string' ? (item as any).newsId : undefined,
      typeof (item as any).node?.id === 'string' ? (item as any).node.id : undefined,
    ];

    return candidates
      .map((value) => (typeof value === 'string' ? value.trim() : undefined))
      .find((value) => !!value);
  };

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('news-page', 'page-visit', { description: 'User visited news page' });
      loadUserPreferences();
      loadSourceReliability();
    }
  }, [user?.id, trackInteraction]);

  const loadUserPreferences = () => {
    const prefs = localStorage.getItem('news-preferences');
    if (prefs) {
      const data = JSON.parse(prefs);
      setPreferredSources(data.preferred || []);
      setBlockedSources(data.blocked || []);
    }
    const saved = localStorage.getItem('saved-articles');
    if (saved) {
      setSavedArticles(JSON.parse(saved));
    }
  };

  const savePreferences = () => {
    localStorage.setItem('news-preferences', JSON.stringify({
      preferred: preferredSources,
      blocked: blockedSources
    }));
  };

  const togglePreferredSource = (source: string) => {
    if (preferredSources.includes(source)) {
      setPreferredSources(preferredSources.filter(s => s !== source));
    } else {
      setPreferredSources([...preferredSources, source]);
      setBlockedSources(blockedSources.filter(s => s !== source));
    }
  };

  const toggleBlockedSource = (source: string) => {
    if (blockedSources.includes(source)) {
      setBlockedSources(blockedSources.filter(s => s !== source));
    } else {
      setBlockedSources([...blockedSources, source]);
      setPreferredSources(preferredSources.filter(s => s !== source));
    }
  };

  const loadSourceReliability = async () => {
    try {
      const response = await fetch(buildApiUrl('/news/source-reliability'));
      if (response.ok) {
        const data = await response.json();
        setSourceReliability(data.sources || {});
      }
    } catch (error) {
      console.error('Error loading source reliability:', error);
    }
  };

  const getReliabilityScore = (source: string): number => {
    return sourceReliability[source] || 0.5;
  };

  const getReliabilityColor = (score: number): string => {
    if (score >= 0.8) return 'text-green-600 dark:text-green-400';
    if (score >= 0.6) return 'text-yellow-600 dark:text-yellow-400';
    if (score >= 0.4) return 'text-orange-600 dark:text-orange-400';
    return 'text-red-600 dark:text-red-400';
  };

  const loadBeliefTranslations = async (article: NewsItem) => {
    try {
      const response = await fetch(buildApiUrl('/news/belief-translations'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: article.title,
          content: article.description,
          url: article.url
        })
      });
      if (response.ok) {
        const data = await response.json();
        setTranslations(data.translations || []);
      }
    } catch (error) {
      console.error('Error loading translations:', error);
    }
  };

  const saveArticle = (newsId: string) => {
    const updated = savedArticles.includes(newsId)
      ? savedArticles.filter(id => id !== newsId)
      : [...savedArticles, newsId];
    setSavedArticles(updated);
    localStorage.setItem('saved-articles', JSON.stringify(updated));
  };

  const shareToConceptNetwork = async (article: NewsItem) => {
    try {
      const response = await fetch(buildApiUrl('/news/share-to-network'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: user?.id,
          newsUrl: article.url,
          title: article.title,
          description: article.description
        })
      });
      if (response.ok) {
        alert('Article shared to your concept network!');
      }
    } catch (error) {
      console.error('Error sharing article:', error);
    }
  };

  const loadNewsData = useCallback(async () => {
    setLoading(true);
    try {
      const skip = (currentPage - 1) * pageSize;
      
      if (selectedCategory === 'personalized' && user?.id) {
        const response = await endpoints.getNewsFeed(user.id, pageSize, timeRange, skip);
        if (response.success && response.data) {
          const data = response.data as any;
          setNewsItems(data.items || []);
          setTotalCount(data.totalCount || 0);
        }
      } else if (selectedCategory === 'trending') {
        const response = await fetch(buildApiUrl(`/news/trending?limit=${pageSize}&hoursBack=${timeRange}`));
        const data = await response.json();
        setTrendingTopics(data.topics || []);
        setTotalCount(data.topics?.length || 0);
      } else if (selectedCategory === 'personalized' && !user?.id) {
        const response = await fetch(buildApiUrl(`/news/latest?limit=${pageSize}&skip=${skip}`));
        const data = await response.json();
        setNewsItems(data.items || []);
        setTotalCount(data.totalCount || 0);
      } else {
        const searchRequest = {
          interests: selectedCategory === 'all' ? [] : [selectedCategory],
          limit: pageSize,
          skip: skip,
          hoursBack: timeRange
        };
        const response = await fetch(buildApiUrl('/news/search'), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(searchRequest)
        });
        const data = await response.json();
        setNewsItems(data.items || []);
        setTotalCount(data.totalCount || 0);
      }
    } catch (error) {
      console.error('Error loading news:', error);
    } finally {
      setLoading(false);
    }
  }, [currentPage, endpoints, selectedCategory, timeRange, user?.id, pageSize]);

  const loadStats = useCallback(async () => {
    try {
      const search = selectedCategory === 'all' || selectedCategory === 'personalized' || selectedCategory === 'trending'
        ? undefined
        : selectedCategory;
      const res = await endpoints.getNewsStats(timeRange, search);
      if (res.success && res.data) {
        const data = res.data as any;
        setStatsTotalCount(data.totalCount || 0);
        setSourceStats(data.sources || {});
      }
    } catch (e) {
      // ignore stats errors
    }
  }, [endpoints, selectedCategory, timeRange]);

  // Load news data
  useEffect(() => {
    setCurrentPage(1); // Reset to first page when filters change
    loadNewsData();
    loadStats();
  }, [selectedCategory, timeRange, user?.id, loadNewsData, loadStats]);

  // Load news data when page changes
  useEffect(() => {
    if (currentPage > 1) {
      loadNewsData();
    }
  }, [currentPage, loadNewsData]);


  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    
    setLoading(true);
    setCurrentPage(1); // Reset to first page for search
    try {
      const skip = (currentPage - 1) * pageSize;
      const searchRequest = {
        interests: [searchQuery],
        limit: pageSize,
        skip: 0,
        hoursBack: timeRange
      };
      const response = await fetch(buildApiUrl('/news/search'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(searchRequest)
      });
      const data = await response.json();
      setNewsItems(data.items || []);
      setTotalCount(data.totalCount || 0);
      
      // Track search interaction
      if (user?.id) {
        trackInteraction('news-search', 'search', { description: `User searched for: ${searchQuery}`, query: searchQuery });
      }
    } catch (error) {
      console.error('Error searching news:', error);
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (newsItem: NewsItem) => {
    if (!user?.id) return;

    try {
      const nodeId = getNewsNodeId(newsItem);
      const newsIdentifier = nodeId ?? newsItem.url;

      await fetch(buildApiUrl('/news/read'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: user.id,
          newsId: newsIdentifier,
          nodeId,
        })
      });

      // Track read interaction - only if we have a valid node ID
      if (nodeId) {
        trackInteraction(nodeId, 'view', {
          description: `User read: ${newsItem.title}`,
          title: newsItem.title,
          source: newsItem.source,
          url: newsItem.url, // Include URL in metadata for reference
        });
      } else {
        // If no node ID, don't track contribution to avoid URL-based contributions
        console.warn('Cannot track contribution for news item without node ID:', newsItem.title);
      }
    } catch (error) {
      console.error('Error marking news as read:', error);
    }
  };

  const resolveAndOpenNode = async (newsItem: NewsItem) => {
    // Prefer explicit id if present
    const candidateId = getNewsNodeId(newsItem);
    if (candidateId) {
      window.open(`/node/${encodeURIComponent(candidateId)}`, '_blank');
      return;
    }
    // Try to resolve by URL via searchTerm
    try {
      const qs = new URLSearchParams();
      qs.set('searchTerm', newsItem.url || newsItem.title);
      qs.set('take', '1');
      const resp = await fetch(buildApiUrl(`/storage-endpoints/nodes?${qs.toString()}`));
      if (resp.ok) {
        const data = await resp.json();
        const node = (data.nodes || [])[0];
        if (node?.id) {
          window.open(`/node/${encodeURIComponent(node.id)}`, '_blank');
          return;
        }
      }
    } catch {}
    // Fallback to graph preselect
    const q = encodeURIComponent(newsItem.url || newsItem.title);
    window.open(`/graph?selectedNode=${q}`, '_blank');
  };

  const loadSummary = async (newsId: string) => {
    if (!newsId) return;
    setSummaryLoading(true);
    try {
      const resp = await fetch(buildApiUrl(`/news/summary/${encodeURIComponent(newsId)}`));
      if (resp.ok) {
        const data = await resp.json();
        setSummaries(prev => ({ 
          ...prev, 
          [newsId]: { 
            text: data?.summary || '', 
            status: data?.status || 'error' 
          } 
        }));
      } else {
        setSummaries(prev => ({ 
          ...prev, 
          [newsId]: { 
            text: '', 
            status: 'error' 
          } 
        }));
      }
    } catch (error) {
      setSummaries(prev => ({ 
        ...prev, 
        [newsId]: { 
          text: '', 
          status: 'error' 
        } 
      }));
    }
    setSummaryLoading(false);
  };

  const formatTimeAgo = (publishedAt: string) => {
    const now = new Date();
    const published = new Date(publishedAt);
    const diffInHours = Math.floor((now.getTime() - published.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) return 'Just now';
    if (diffInHours < 24) return `${diffInHours}h ago`;
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays}d ago`;
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">📰 News Feed</h1>
              <p className="text-gray-600 dark:text-gray-300">
                Personalized news based on your concept interactions and interests
              </p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowSourcePrefs(true)}
                className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm"
              >
                ⚙️ Sources
              </button>
              <button
                onClick={() => setShowSaved(true)}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 text-sm"
              >
                💾 Saved ({savedArticles.length})
              </button>
            </div>
          </div>
        </div>

        {/* Controls */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Category Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                Category
              </label>
              <select
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
                className="input-standard"
              >
                <option value="personalized">🎯 Personalized</option>
                <option value="trending">📈 Trending</option>
                <option value="technology">💻 Technology</option>
                <option value="science">🔬 Science</option>
                <option value="business">💼 Business</option>
                <option value="health">🏥 Health</option>
                <option value="politics">🏛️ Politics</option>
                <option value="all">🌍 All News</option>
              </select>
            </div>

            {/* Time Range */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                Time Range
              </label>
              <select
                value={timeRange}
                onChange={(e) => setTimeRange(parseInt(e.target.value))}
                className="input-standard"
              >
                <option value={1}>Last Hour</option>
                <option value={6}>Last 6 Hours</option>
                <option value={24}>Last 24 Hours</option>
                <option value={72}>Last 3 Days</option>
                <option value={168}>Last Week</option>
              </select>
            </div>

            {/* Search */}
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                Search Topics
              </label>
              <div className="flex space-x-2">
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Search for specific topics..."
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                />
                <button
                  onClick={handleSearch}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  🔍 Search
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main News Feed */}
          <div className="lg:col-span-2">
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                  {selectedCategory === 'personalized' ? '🎯 Your Personalized Feed' :
                   selectedCategory === 'trending' ? '📈 Trending Topics' :
                   `📰 ${selectedCategory.charAt(0).toUpperCase() + selectedCategory.slice(1)} News`}
                </h2>
              </div>

              <div className="divide-y divide-gray-200">
                {loading ? (
                  <div className="p-8 text-center">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-2 text-gray-500">Loading news...</p>
                  </div>
                ) : selectedCategory === 'trending' ? (
                  // Trending Topics View
                  trendingTopics.length > 0 ? (
                    trendingTopics.map((topic, index) => (
                      <div key={index} className="p-6 hover:bg-gray-50 dark:hover:bg-gray-700">
                        <div className="flex items-center justify-between">
                          <div>
                            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                              #{index + 1} {topic.topic}
                            </h3>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                              {topic.mentionCount} mentions • Trend Score: {topic.trendScore.toFixed(2)}
                            </p>
                          </div>
                          <div className="text-right">
                            <div className="text-2xl">📈</div>
                          </div>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="p-8 text-center text-gray-500">
                      No trending topics found for the selected time range.
                    </div>
                  )
                ) : (
                  // News Items View
                  newsItems.length > 0 ? (
                    newsItems.map((item, index) => {
                      const nodeId = getNewsNodeId(item);
                      const summaryKey = nodeId ?? item.url;
                      const summaryData = summaries[summaryKey];
                      const isSelected = selectedNewsId === summaryKey;

                      return (
                        <article key={index} className="p-6 hover:bg-gray-50 dark:hover:bg-gray-700">
                        <div className="flex items-start space-x-4">
                          {item.imageUrl && (
                            <Image
                              src={item.imageUrl}
                              alt={item.title}
                              width={80}
                              height={80}
                              className="w-20 h-20 object-cover rounded-lg flex-shrink-0"
                            />
                          )}
                          <div className="flex-1 min-w-0">
                            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
                              <button
                                onClick={() => {
                                  setSelectedNewsId(summaryKey);
                                  loadSummary(summaryKey);
                                  markAsRead(item);
                                }}
                                className="hover:text-blue-600 transition-colors text-left"
                                title="Select to load summary"
                                dangerouslySetInnerHTML={{ __html: htmlEncode(item.title) }}
                              />
                            </h3>
                            <p 
                              className="text-gray-600 dark:text-gray-300 mb-3 line-clamp-2"
                              dangerouslySetInnerHTML={{ __html: htmlEncode(item.description) }}
                            />
                            <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
                              <div className="flex items-center space-x-4">
                                <span className="flex items-center gap-1">
                                  📰 {item.source}
                                  <span className={`ml-1 ${getReliabilityColor(getReliabilityScore(item.source))}`}>
                                    ⭐ {(getReliabilityScore(item.source) * 100).toFixed(0)}%
                                  </span>
                                </span>
                                {item.author && <span>✍️ {item.author}</span>}
                                <span>🕒 {formatTimeAgo(item.publishedAt)}</span>
                              </div>
                              <div className="flex items-center space-x-2">
                                <button
                                  onClick={() => {
                                    setSelectedArticleForTranslation(item);
                                    loadBeliefTranslations(item);
                                    setShowTranslations(true);
                                  }}
                                  className="text-purple-600 dark:text-purple-400 hover:text-purple-800 dark:hover:text-purple-300 transition-colors"
                                  title="View belief translations"
                                >
                                  🔄
                                </button>
                                <button
                                  onClick={() => saveArticle(summaryKey)}
                                  className={`${savedArticles.includes(summaryKey) ? 'text-yellow-600' : 'text-gray-600'} hover:text-yellow-800 transition-colors`}
                                  title={savedArticles.includes(summaryKey) ? 'Unsave article' : 'Save article'}
                                >
                                  {savedArticles.includes(summaryKey) ? '⭐' : '☆'}
                                </button>
                                <button
                                  onClick={() => shareToConceptNetwork(item)}
                                  className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 transition-colors"
                                  title="Share to concept network"
                                >
                                  🔗
                                </button>
                                <button
                                  onClick={() => resolveAndOpenNode(item)}
                                  className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors"
                                >
                                  🔎
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>
                        {/* Inline summary when selected */}
                        {isSelected && (
                          <div className="mt-3 p-3 bg-gray-50 dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 text-sm text-gray-700 dark:text-gray-200">
                            {summaryLoading && <span>Loading summary...</span>}
                            {!summaryLoading && summaryData && (
                              summaryData.status === 'available' ? (
                                <div dangerouslySetInnerHTML={{ __html: htmlEncode(summaryData.text) }} />
                              ) : summaryData.status === 'generating' ? (
                                <div className="flex items-center space-x-2">
                                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                                  <span>Generating summary...</span>
                                </div>
                              ) : summaryData.status === 'none' ? (
                                <div className="text-gray-500 italic">No summary available for this article.</div>
                              ) : (
                                <div className="text-red-500">Error loading summary. Please try again.</div>
                              )
                            )}
                            {!summaryLoading && !summaryData && (
                              <div className="text-gray-500 italic">Click to load summary</div>
                            )}
                          </div>
                        )}
                        </article>
                      );
                    })
                  ) : (
                    <div className="p-8 text-center text-gray-500">
                      {selectedCategory === 'personalized' 
                        ? "No personalized news found. Try interacting with more concepts to improve your feed!"
                        : "No news items found for the selected criteria."}
                    </div>
                  )
                )}
              </div>
              
              {/* Pagination Controls */}
              {selectedCategory !== 'trending' && totalCount > pageSize && (
                <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700">
                  <div className="flex items-center justify-between">
                    <div className="text-sm text-gray-700 dark:text-gray-300">
                      Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} articles
                    </div>
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                        disabled={currentPage === 1}
                        className="px-3 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Previous
                      </button>
                      
                      <div className="flex items-center space-x-1">
                        {Array.from({ length: Math.min(5, Math.ceil(totalCount / pageSize)) }, (_, i) => {
                          const page = i + 1;
                          const maxPages = Math.ceil(totalCount / pageSize);
                          const startPage = Math.max(1, Math.min(currentPage - 2, maxPages - 4));
                          const displayPage = startPage + i;
                          
                          if (displayPage > maxPages) return null;
                          
                          return (
                            <button
                              key={displayPage}
                              onClick={() => setCurrentPage(displayPage)}
                              className={`px-3 py-1 text-sm border rounded-md ${
                                currentPage === displayPage
                                  ? 'bg-blue-600 text-white border-blue-600'
                                  : 'border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'
                              }`}
                            >
                              {displayPage}
                            </button>
                          );
                        })}
                      </div>
                      
                      <button
                        onClick={() => setCurrentPage(prev => Math.min(Math.ceil(totalCount / pageSize), prev + 1))}
                        disabled={currentPage >= Math.ceil(totalCount / pageSize)}
                        className="px-3 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Next
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Extracted Concepts - Show when a news item is selected */}
            {selectedNewsId && (
              <NewsConceptsList
                newsItemId={selectedNewsId}
                newsTitle={newsItems.find(item => {
                  const nodeId = getNewsNodeId(item);
                  const summaryKey = nodeId ?? item.url;
                  return summaryKey === selectedNewsId;
                })?.title || 'Selected News Item'}
                className="mb-6"
              />
            )}

            {/* Quick Stats (server-side totals) */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">📊 News Stats</h3>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Total Articles</span>
                  <span className="font-medium">{statsTotalCount}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Showing</span>
                  <span className="font-medium">{newsItems.length} on this page</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Time Range</span>
                  <span className="font-medium">{timeRange}h</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Category</span>
                  <span className="font-medium capitalize">{selectedCategory}</span>
                </div>
              </div>
            </div>

            {/* News Sources (server-side) */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">📡 News Sources</h3>
              <div className="space-y-2">
                {Object.entries(sourceStats).slice(0, 5).map(([source, count]) => (
                  <div key={source} className="flex items-center justify-between">
                    <span className="text-gray-600 dark:text-gray-300">{source}</span>
                    <span className="text-sm text-gray-500">{count}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Quick Actions */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">⚡ Quick Actions</h3>
              <div className="space-y-3">
                <button
                  onClick={() => setSelectedCategory('personalized')}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
                >
                  🎯 My Personalized Feed
                </button>
                <button
                  onClick={() => setSelectedCategory('trending')}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
                >
                  📈 Trending Topics
                </button>
                <button
                  onClick={() => {
                    setTimeRange(1);
                    setSelectedCategory('all');
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
                >
                  ⚡ Breaking News
                </button>
                <button
                  onClick={loadNewsData}
                  className="w-full text-left px-3 py-2 text-sm text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-md transition-colors"
                >
                  🔄 Refresh Feed
                </button>
              </div>
            </div>

            {/* User Interests */}
            {user && (
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">🎨 Your Interests</h3>
                <div className="text-sm text-gray-600 dark:text-gray-300">
                  <p className="mb-2">News is personalized based on your concept interactions:</p>
                  <div className="flex flex-wrap gap-2">
                    {['technology', 'science', 'business', 'health'].map((interest) => (
                      <span
                        key={interest}
                        className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded-md text-xs"
                      >
                        {interest}
                      </span>
                    ))}
                  </div>
                  <p className="mt-3 text-xs text-gray-500 dark:text-gray-400">
                    💡 Tip: Attune to more concepts to improve your news feed!
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Source Preferences Modal */}
        {showSourcePrefs && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">⚙️ News Source Preferences</h3>
                <button onClick={() => {setShowSourcePrefs(false); savePreferences();}} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6">
                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">Available Sources</h4>
                    <div className="space-y-2">
                      {Object.keys(sourceStats).map((source) => (
                        <div key={source} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 rounded-lg">
                          <div className="flex items-center gap-3">
                            <span className="text-gray-900 dark:text-gray-100">{source}</span>
                            <span className={`text-sm ${getReliabilityColor(getReliabilityScore(source))}`}>
                              ⭐ {(getReliabilityScore(source) * 100).toFixed(0)}% reliability
                            </span>
                          </div>
                          <div className="flex gap-2">
                            <button
                              onClick={() => togglePreferredSource(source)}
                              className={`px-3 py-1 text-sm rounded-md ${
                                preferredSources.includes(source)
                                  ? 'bg-green-600 text-white'
                                  : 'bg-gray-300 text-gray-700'
                              }`}
                            >
                              {preferredSources.includes(source) ? '✓ Preferred' : 'Prefer'}
                            </button>
                            <button
                              onClick={() => toggleBlockedSource(source)}
                              className={`px-3 py-1 text-sm rounded-md ${
                                blockedSources.includes(source)
                                  ? 'bg-red-600 text-white'
                                  : 'bg-gray-300 text-gray-700'
                              }`}
                            >
                              {blockedSources.includes(source) ? '✓ Blocked' : 'Block'}
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Belief Translations Modal */}
        {showTranslations && selectedArticleForTranslation && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-3xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">🔄 Belief System Translations</h3>
                <button onClick={() => setShowTranslations(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6">
                <div className="mb-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                  <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">Original Article</h4>
                  <p className="text-sm text-gray-700 dark:text-gray-300">{selectedArticleForTranslation.title}</p>
                </div>
                <div className="space-y-4">
                  {translations.length > 0 ? (
                    translations.map((trans, index) => (
                      <div key={index} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                        <div className="flex items-center gap-2 mb-2">
                          <span className="px-2 py-1 bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-400 text-xs rounded">
                            {trans.perspective || trans.beliefSystem}
                          </span>
                          {trans.confidence && (
                            <span className="text-xs text-gray-500">
                              {(trans.confidence * 100).toFixed(0)}% confidence
                            </span>
                          )}
                        </div>
                        <p className="text-gray-700 dark:text-gray-300 text-sm">{trans.translation || trans.interpretation}</p>
                      </div>
                    ))
                  ) : (
                    <p className="text-center text-gray-500 py-4">Loading translations...</p>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Saved Articles Modal */}
        {showSaved && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-3xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">💾 Saved Articles ({savedArticles.length})</h3>
                <button onClick={() => setShowSaved(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6">
                {savedArticles.length > 0 ? (
                  <div className="space-y-3">
                    {newsItems
                      .filter(item => savedArticles.includes(getNewsNodeId(item) ?? item.url))
                      .map((item, index) => (
                        <div key={index} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-600">
                          <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-1">{item.title}</h4>
                          <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">{item.description?.substring(0, 120)}...</p>
                          <div className="flex items-center justify-between text-xs text-gray-500">
                            <span>📰 {item.source}</span>
                            <div className="flex gap-2">
                              <button
                                onClick={() => resolveAndOpenNode(item)}
                                className="text-blue-600 hover:text-blue-800"
                              >
                                View
                              </button>
                              <button
                                onClick={() => saveArticle(getNewsNodeId(item) ?? item.url)}
                                className="text-red-600 hover:text-red-800"
                              >
                                Remove
                              </button>
                            </div>
                          </div>
                        </div>
                      ))}
                  </div>
                ) : (
                  <p className="text-center text-gray-500 py-8">No saved articles yet. Click the ☆ icon on any article to save it.</p>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
