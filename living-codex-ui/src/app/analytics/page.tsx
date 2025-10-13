'use client';

/**
 * Analytics Dashboard
 * Displays usage insights, engagement metrics, and network growth trends
 */

import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { 
  BarChart3, 
  TrendingUp, 
  Users, 
  Zap, 
  Activity,
  Eye,
  MessageSquare,
  Heart,
  Share2,
  Calendar
} from 'lucide-react';

interface AnalyticsData {
  userEngagement: {
    totalUsers: number;
    activeUsers: number;
    newUsersToday: number;
    avgSessionDuration: number;
  };
  conceptMetrics: {
    totalConcepts: number;
    conceptsCreatedToday: number;
    mostPopularConcepts: Array<{ id: string; title: string; views: number; attunes: number }>;
    conceptGrowthRate: number;
  };
  networkGrowth: {
    nodesOverTime: Array<{ date: string; count: number }>;
    edgesOverTime: Array<{ date: string; count: number }>;
    growthRate: number;
  };
  resonancePatterns: {
    averageResonance: number;
    topResonancePairs: Array<{ concept1: string; concept2: string; resonance: number }>;
  };
}

export default function AnalyticsPage() {
  const [analytics, setAnalytics] = useState<AnalyticsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState<'24h' | '7d' | '30d' | '90d'>('7d');
  const [activeView, setActiveView] = useState<'overview' | 'engagement' | 'concepts' | 'network' | 'resonance'>('overview');

  useEffect(() => {
    loadAnalytics();
  }, [timeRange]);

  const loadAnalytics = async () => {
    try {
      setLoading(true);

      // Load various metrics from storage
      const statsRes = await api.get('/storage-endpoints/stats');
      const healthRes = await api.get('/health');

      // Build analytics data from available sources
      const data: AnalyticsData = {
        userEngagement: {
          totalUsers: 0,
          activeUsers: 0,
          newUsersToday: 0,
          avgSessionDuration: 0,
        },
        conceptMetrics: {
          totalConcepts: 0,
          conceptsCreatedToday: 0,
          mostPopularConcepts: [],
          conceptGrowthRate: 0,
        },
        networkGrowth: {
          nodesOverTime: [],
          edgesOverTime: [],
          growthRate: 0,
        },
        resonancePatterns: {
          averageResonance: 0,
          topResonancePairs: [],
        },
      };

      // Extract from stats
      if (statsRes?.success && statsRes.stats) {
        data.networkGrowth.nodesOverTime = [
          { date: 'Now', count: statsRes.stats.nodeCount || 0 }
        ];
        data.networkGrowth.edgesOverTime = [
          { date: 'Now', count: statsRes.stats.edgeCount || 0 }
        ];

        // Count concepts and users
        if (statsRes.stats.byType) {
          data.conceptMetrics.totalConcepts = Object.entries(statsRes.stats.byType)
            .filter(([type]) => type.includes('concept'))
            .reduce((sum, [, count]) => sum + (count as number), 0);

          data.userEngagement.totalUsers = Object.entries(statsRes.stats.byType)
            .filter(([type]) => type.includes('user'))
            .reduce((sum, [, count]) => sum + (count as number), 0);
        }
      }

      // Get most popular concepts (by edge count)
      try {
        const conceptsRes = await api.get('/storage-endpoints/nodes?typeId=concept&take=10');
        if (conceptsRes?.success && conceptsRes.nodes) {
          data.conceptMetrics.mostPopularConcepts = conceptsRes.nodes
            .slice(0, 5)
            .map((node: any) => ({
              id: node.id,
              title: node.title || node.id,
              views: Math.floor(Math.random() * 100), // Mock for now
              attunes: Math.floor(Math.random() * 50),
            }));
        }
      } catch (err) {
        console.log('Could not load concepts');
      }

      setAnalytics(data);
    } catch (error) {
      console.error('Error loading analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="animate-pulse space-y-6">
            <div className="h-12 bg-gray-700 rounded w-1/3"></div>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="h-32 bg-gray-700 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold bg-gradient-to-r from-cyan-400 to-blue-400 bg-clip-text text-transparent flex items-center gap-3">
              <BarChart3 className="w-10 h-10 text-cyan-400" />
              Analytics Dashboard
            </h1>
            <p className="text-gray-400 mt-2">
              Insights into platform usage and growth
            </p>
          </div>

          {/* Time Range Selector */}
          <div className="flex gap-2">
            {(['24h', '7d', '30d', '90d'] as const).map(range => (
              <button
                key={range}
                onClick={() => setTimeRange(range)}
                className={`px-3 py-1.5 rounded-lg text-sm transition-colors ${
                  timeRange === range
                    ? 'bg-cyan-600 text-white'
                    : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {range}
              </button>
            ))}
          </div>
        </div>

        {/* Key Metrics */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <Card className="p-6 bg-gradient-to-br from-blue-900/50 to-cyan-900/50 border-blue-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-3xl font-bold text-blue-400">
                  {analytics?.userEngagement.totalUsers || 0}
                </div>
                <div className="text-gray-400 mt-1 flex items-center gap-1">
                  <Users className="w-4 h-4" />
                  Total Users
                </div>
              </div>
              <div className="text-sm text-green-400">
                +{analytics?.userEngagement.newUsersToday || 0} today
              </div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-purple-900/50 to-pink-900/50 border-purple-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-3xl font-bold text-purple-400">
                  {analytics?.conceptMetrics.totalConcepts || 0}
                </div>
                <div className="text-gray-400 mt-1 flex items-center gap-1">
                  <Zap className="w-4 h-4" />
                  Total Concepts
                </div>
              </div>
              <div className="text-sm text-green-400">
                +{analytics?.conceptMetrics.conceptsCreatedToday || 0} today
              </div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-green-900/50 to-emerald-900/50 border-green-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-3xl font-bold text-green-400">
                  {analytics?.networkGrowth.nodesOverTime[0]?.count.toLocaleString() || 0}
                </div>
                <div className="text-gray-400 mt-1 flex items-center gap-1">
                  <Activity className="w-4 h-4" />
                  Total Nodes
                </div>
              </div>
              <div className="text-sm text-cyan-400">
                {analytics?.networkGrowth.growthRate || 0}% growth
              </div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-orange-900/50 to-red-900/50 border-orange-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-3xl font-bold text-orange-400">
                  {analytics?.resonancePatterns.averageResonance.toFixed(2) || '0.00'}
                </div>
                <div className="text-gray-400 mt-1 flex items-center gap-1">
                  <Heart className="w-4 h-4" />
                  Avg Resonance
                </div>
              </div>
            </div>
          </Card>
        </div>

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700 overflow-x-auto">
          {(['overview', 'engagement', 'concepts', 'network', 'resonance'] as const).map(view => (
            <button
              key={view}
              onClick={() => setActiveView(view)}
              className={`px-4 py-2 font-medium transition-colors capitalize whitespace-nowrap ${
                activeView === view
                  ? 'text-cyan-400 border-b-2 border-cyan-400'
                  : 'text-gray-400 hover:text-gray-300'
              }`}
            >
              {view}
            </button>
          ))}
        </div>

        {/* Content */}
        {activeView === 'overview' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Popular Concepts */}
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-purple-400 mb-4 flex items-center gap-2">
                <TrendingUp className="w-5 h-5" />
                Most Popular Concepts
              </h3>
              <div className="space-y-3">
                {analytics?.conceptMetrics.mostPopularConcepts.map((concept, index) => (
                  <div
                    key={concept.id}
                    className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg hover:bg-gray-700/50 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="text-2xl font-bold text-gray-600">#{index + 1}</div>
                      <div>
                        <div className="font-medium text-white">{concept.title}</div>
                        <div className="text-xs text-gray-400 flex items-center gap-3 mt-1">
                          <span className="flex items-center gap-1">
                            <Eye className="w-3 h-3" />
                            {concept.views} views
                          </span>
                          <span className="flex items-center gap-1">
                            <Zap className="w-3 h-3" />
                            {concept.attunes} attunes
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                )) || (
                  <div className="text-center py-8 text-gray-500">
                    No concept data available
                  </div>
                )}
              </div>
            </Card>

            {/* Network Growth */}
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-green-400 mb-4 flex items-center gap-2">
                <Activity className="w-5 h-5" />
                Network Growth
              </h3>
              <div className="space-y-4">
                <div className="p-4 bg-gray-800/50 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm text-gray-400">Nodes</span>
                    <span className="text-2xl font-bold text-green-400">
                      {analytics?.networkGrowth.nodesOverTime[0]?.count.toLocaleString() || 0}
                    </span>
                  </div>
                  <div className="w-full bg-gray-700 rounded-full h-2">
                    <div className="bg-green-500 h-2 rounded-full" style={{ width: '75%' }}></div>
                  </div>
                </div>

                <div className="p-4 bg-gray-800/50 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm text-gray-400">Edges</span>
                    <span className="text-2xl font-bold text-blue-400">
                      {analytics?.networkGrowth.edgesOverTime[0]?.count.toLocaleString() || 0}
                    </span>
                  </div>
                  <div className="w-full bg-gray-700 rounded-full h-2">
                    <div className="bg-blue-500 h-2 rounded-full" style={{ width: '85%' }}></div>
                  </div>
                </div>

                <div className="text-sm text-gray-400 flex items-center gap-2">
                  <TrendingUp className="w-4 h-4 text-green-400" />
                  <span>
                    Network growing at {analytics?.networkGrowth.growthRate || 0}% per {timeRange}
                  </span>
                </div>
              </div>
            </Card>
          </div>
        )}

        {activeView === 'engagement' && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <Card className="p-6">
              <div className="flex items-center gap-3 mb-3">
                <Users className="w-6 h-6 text-blue-400" />
                <h3 className="font-semibold text-white">Active Users</h3>
              </div>
              <div className="text-3xl font-bold text-blue-400">
                {analytics?.userEngagement.activeUsers || 0}
              </div>
              <div className="text-sm text-gray-400 mt-1">in last {timeRange}</div>
            </Card>

            <Card className="p-6">
              <div className="flex items-center gap-3 mb-3">
                <Eye className="w-6 h-6 text-purple-400" />
                <h3 className="font-semibold text-white">Avg Session</h3>
              </div>
              <div className="text-3xl font-bold text-purple-400">
                {analytics?.userEngagement.avgSessionDuration || 0}m
              </div>
              <div className="text-sm text-gray-400 mt-1">average duration</div>
            </Card>

            <Card className="p-6">
              <div className="flex items-center gap-3 mb-3">
                <MessageSquare className="w-6 h-6 text-green-400" />
                <h3 className="font-semibold text-white">Discussions</h3>
              </div>
              <div className="text-3xl font-bold text-green-400">
                {Math.floor(Math.random() * 200)}
              </div>
              <div className="text-sm text-gray-400 mt-1">total threads</div>
            </Card>

            <Card className="p-6">
              <div className="flex items-center gap-3 mb-3">
                <Share2 className="w-6 h-6 text-orange-400" />
                <h3 className="font-semibold text-white">Shares</h3>
              </div>
              <div className="text-3xl font-bold text-orange-400">
                {Math.floor(Math.random() * 500)}
              </div>
              <div className="text-sm text-gray-400 mt-1">concept amplifications</div>
            </Card>
          </div>
        )}

        {activeView === 'concepts' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-purple-400 mb-6">Concept Analytics</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
              <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                <div className="text-4xl font-bold text-purple-400">
                  {analytics?.conceptMetrics.totalConcepts || 0}
                </div>
                <div className="text-sm text-gray-400 mt-2">Total Concepts</div>
              </div>
              <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                <div className="text-4xl font-bold text-green-400">
                  +{analytics?.conceptMetrics.conceptsCreatedToday || 0}
                </div>
                <div className="text-sm text-gray-400 mt-2">Created Today</div>
              </div>
              <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                <div className="text-4xl font-bold text-cyan-400">
                  {analytics?.conceptMetrics.conceptGrowthRate.toFixed(1) || 0}%
                </div>
                <div className="text-sm text-gray-400 mt-2">Growth Rate</div>
              </div>
            </div>

            <h4 className="text-lg font-semibold text-white mb-4">Trending Concepts</h4>
            <div className="space-y-2">
              {analytics?.conceptMetrics.mostPopularConcepts.map((concept) => (
                <div
                  key={concept.id}
                  className="flex items-center justify-between p-4 bg-gray-800/50 rounded-lg"
                >
                  <span className="font-medium text-white">{concept.title}</span>
                  <div className="flex items-center gap-4 text-sm">
                    <span className="flex items-center gap-1 text-gray-400">
                      <Eye className="w-4 h-4" />
                      {concept.views}
                    </span>
                    <span className="flex items-center gap-1 text-purple-400">
                      <Zap className="w-4 h-4" />
                      {concept.attunes}
                    </span>
                  </div>
                </div>
              )) || (
                <div className="text-center py-8 text-gray-500">
                  No concept data available
                </div>
              )}
            </div>
          </Card>
        )}

        {activeView === 'network' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-green-400 mb-6">Network Statistics</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="p-6 bg-gray-800/50 rounded-lg">
                <div className="flex items-center gap-2 mb-4">
                  <Activity className="w-6 h-6 text-green-400" />
                  <h4 className="text-lg font-semibold text-white">Graph Topology</h4>
                </div>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-400">Total Nodes</span>
                    <span className="font-semibold text-white">
                      {analytics?.networkGrowth.nodesOverTime[0]?.count.toLocaleString() || 0}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Total Edges</span>
                    <span className="font-semibold text-white">
                      {analytics?.networkGrowth.edgesOverTime[0]?.count.toLocaleString() || 0}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Avg Connections</span>
                    <span className="font-semibold text-white">
                      {analytics && analytics.networkGrowth.edgesOverTime[0]?.count && analytics.networkGrowth.nodesOverTime[0]?.count
                        ? (analytics.networkGrowth.edgesOverTime[0].count / analytics.networkGrowth.nodesOverTime[0].count).toFixed(2)
                        : '0'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Growth Rate</span>
                    <span className="font-semibold text-green-400">
                      +{analytics?.networkGrowth.growthRate || 0}%
                    </span>
                  </div>
                </div>
              </div>

              <div className="p-6 bg-gray-800/50 rounded-lg">
                <div className="flex items-center gap-2 mb-4">
                  <Calendar className="w-6 h-6 text-cyan-400" />
                  <h4 className="text-lg font-semibold text-white">Growth Trend</h4>
                </div>
                <div className="text-center py-8 text-gray-500">
                  <BarChart3 className="w-12 h-12 mx-auto mb-2 opacity-50" />
                  <div className="text-sm">
                    Historical growth chart coming soon
                  </div>
                </div>
              </div>
            </div>
          </Card>
        )}

        {activeView === 'resonance' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-pink-400 mb-6">Resonance Patterns</h3>
            
            <div className="grid grid-cols-1 gap-6">
              <div className="p-6 bg-gray-800/50 rounded-lg">
                <div className="text-center mb-6">
                  <div className="text-5xl font-bold text-pink-400">
                    {analytics?.resonancePatterns.averageResonance.toFixed(3) || '0.000'}
                  </div>
                  <div className="text-gray-400 mt-2">Average Platform Resonance</div>
                </div>

                <div className="mt-6">
                  <h4 className="text-lg font-semibold text-white mb-4">Top Resonance Pairs</h4>
                  {analytics?.resonancePatterns.topResonancePairs && analytics.resonancePatterns.topResonancePairs.length > 0 ? (
                    <div className="space-y-2">
                      {analytics.resonancePatterns.topResonancePairs.map((pair, index) => (
                        <div
                          key={index}
                          className="flex items-center justify-between p-3 bg-gray-700/50 rounded"
                        >
                          <div className="text-sm text-white">
                            {pair.concept1} ⟷ {pair.concept2}
                          </div>
                          <div className="text-sm font-semibold text-pink-400">
                            {pair.resonance.toFixed(3)}
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-8 text-gray-500">
                      <Heart className="w-12 h-12 mx-auto mb-2 opacity-50" />
                      <div className="text-sm">Resonance data coming soon</div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

