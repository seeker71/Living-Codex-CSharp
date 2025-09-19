'use client';

import { useEffect, useState } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { useAuth } from '@/contexts/AuthContext';
import { endpoints } from '@/lib/api';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface NewsItem {
  title: string;
  description: string;
  url: string;
  publishedAt: string;
  source: string;
  author?: string;
  imageUrl?: string;
  content?: string;
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
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState<string>('personalized');
  const [searchQuery, setSearchQuery] = useState('');
  const [timeRange, setTimeRange] = useState(24);

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('news-page', 'page-visit', { description: 'User visited news page' });
    }
  }, [user?.id, trackInteraction]);

  // Load news data
  useEffect(() => {
    loadNewsData();
  }, [selectedCategory, timeRange, user?.id]); // loadNewsData is stable due to useState setters

  const loadNewsData = async () => {
    setLoading(true);
    try {
      if (selectedCategory === 'personalized' && user?.id) {
        // Load personalized news feed
        const response = await endpoints.getNewsFeed(user.id, 20, timeRange);
        if (response.success && response.data) {
          setNewsItems((response.data as any).items || []);
        }
      } else if (selectedCategory === 'trending') {
        // Load trending news
        const response = await fetch(buildApiUrl(`/news/trending?limit=20&hoursBack=${timeRange}`));
        const data = await response.json();
        setTrendingTopics(data.topics || []);
      } else {
        // Load general news by search
        const searchRequest = {
          interests: selectedCategory === 'all' ? [] : [selectedCategory],
          limit: 20,
          hoursBack: timeRange
        };
        const response = await fetch(buildApiUrl('/news/search'), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(searchRequest)
        });
        const data = await response.json();
        setNewsItems(data.items || []);
      }
    } catch (error) {
      console.error('Error loading news:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    
    setLoading(true);
    try {
      const searchRequest = {
        interests: [searchQuery],
        limit: 20,
        hoursBack: timeRange
      };
      const response = await fetch(buildApiUrl('/news/search'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(searchRequest)
      });
      const data = await response.json();
      setNewsItems(data.items || []);
      
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
      await fetch(buildApiUrl('/news/read'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: user.id,
          newsId: newsItem.url // Using URL as unique identifier
        })
      });
      
      // Track read interaction
      trackInteraction(newsItem.url, 'view', { description: `User read: ${newsItem.title}`, title: newsItem.title, source: newsItem.source });
    } catch (error) {
      console.error('Error marking news as read:', error);
    }
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
    <div className="min-h-screen bg-gray-50">
      <Navigation />
      
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">📰 News Feed</h1>
          <p className="text-gray-600">
            Personalized news based on your concept interactions and interests
          </p>
        </div>

        {/* Controls */}
        <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Category Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Category
              </label>
              <select
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Time Range
              </label>
              <select
                value={timeRange}
                onChange={(e) => setTimeRange(parseInt(e.target.value))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
              <label className="block text-sm font-medium text-gray-700 mb-2">
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
            <div className="bg-white rounded-lg border border-gray-200">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-xl font-semibold text-gray-900">
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
                      <div key={index} className="p-6 hover:bg-gray-50">
                        <div className="flex items-center justify-between">
                          <div>
                            <h3 className="text-lg font-medium text-gray-900">
                              #{index + 1} {topic.topic}
                            </h3>
                            <p className="text-sm text-gray-500">
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
                    newsItems.map((item, index) => (
                      <article key={index} className="p-6 hover:bg-gray-50">
                        <div className="flex items-start space-x-4">
                          {item.imageUrl && (
                            <img
                              src={item.imageUrl}
                              alt={item.title}
                              className="w-20 h-20 object-cover rounded-lg flex-shrink-0"
                            />
                          )}
                          <div className="flex-1 min-w-0">
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                              <a
                                href={item.url}
                                target="_blank"
                                rel="noopener noreferrer"
                                onClick={() => markAsRead(item)}
                                className="hover:text-blue-600 transition-colors"
                              >
                                {item.title}
                              </a>
                            </h3>
                            <p className="text-gray-600 mb-3 line-clamp-2">
                              {item.description}
                            </p>
                            <div className="flex items-center justify-between text-sm text-gray-500">
                              <div className="flex items-center space-x-4">
                                <span>📰 {item.source}</span>
                                {item.author && <span>✍️ {item.author}</span>}
                                <span>🕒 {formatTimeAgo(item.publishedAt)}</span>
                              </div>
                              <button
                                onClick={() => markAsRead(item)}
                                className="text-blue-600 hover:text-blue-800 transition-colors"
                              >
                                Mark as Read
                              </button>
                            </div>
                          </div>
                        </div>
                      </article>
                    ))
                  ) : (
                    <div className="p-8 text-center text-gray-500">
                      {selectedCategory === 'personalized' 
                        ? "No personalized news found. Try interacting with more concepts to improve your feed!"
                        : "No news items found for the selected criteria."}
                    </div>
                  )
                )}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Quick Stats */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">📊 News Stats</h3>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-600">Articles Today</span>
                  <span className="font-medium">{newsItems.length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Time Range</span>
                  <span className="font-medium">{timeRange}h</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Category</span>
                  <span className="font-medium capitalize">{selectedCategory}</span>
                </div>
              </div>
            </div>

            {/* News Sources */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">📡 News Sources</h3>
              <div className="space-y-2">
                {Array.from(new Set(newsItems.map(item => item.source))).slice(0, 5).map((source, index) => (
                  <div key={index} className="flex items-center justify-between">
                    <span className="text-gray-600">{source}</span>
                    <span className="text-sm text-gray-500">
                      {newsItems.filter(item => item.source === source).length}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {/* Quick Actions */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">⚡ Quick Actions</h3>
              <div className="space-y-3">
                <button
                  onClick={() => setSelectedCategory('personalized')}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  🎯 My Personalized Feed
                </button>
                <button
                  onClick={() => setSelectedCategory('trending')}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  📈 Trending Topics
                </button>
                <button
                  onClick={() => {
                    setTimeRange(1);
                    setSelectedCategory('all');
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  ⚡ Breaking News
                </button>
                <button
                  onClick={loadNewsData}
                  className="w-full text-left px-3 py-2 text-sm text-blue-600 hover:bg-blue-50 rounded-md transition-colors"
                >
                  🔄 Refresh Feed
                </button>
              </div>
            </div>

            {/* User Interests */}
            {user && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">🎨 Your Interests</h3>
                <div className="text-sm text-gray-600">
                  <p className="mb-2">News is personalized based on your concept interactions:</p>
                  <div className="flex flex-wrap gap-2">
                    {['technology', 'science', 'business', 'health'].map((interest) => (
                      <span
                        key={interest}
                        className="px-2 py-1 bg-blue-100 text-blue-800 rounded-md text-xs"
                      >
                        {interest}
                      </span>
                    ))}
                  </div>
                  <p className="mt-3 text-xs">
                    💡 Tip: Attune to more concepts to improve your news feed!
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
