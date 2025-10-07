'use client';

import React, { useState, useEffect, useRef } from 'react';

interface AIQueueMetrics {
  success: boolean;
  metrics: {
    timestamp: string;
    configuration: {
      maxConcurrentRequests: number;
      maxQueueSize: number;
    };
    current: {
      activeRequests: number;
      queuedRequests: number;
      completedRequests: number;
    };
    totals: {
      processed: number;
      rejected: number;
      timeout: number;
    };
    utilization: {
      queueUtilization: number;
      queueFullness: number;
    };
    health: {
      score: number;
      status: string;
    };
  };
}

interface SystemHealth {
  status: string;
  uptime: string;
  memoryUsageMB: number;
  nodeCount: number;
  edgeCount: number;
  moduleCount: number;
  requestCount: number;
  activeRequests: number;
}

interface NewsActivity {
  success: boolean;
  totalCount: number;
  sources: Record<string, number>;
}

interface MetricsDataPoint {
  timestamp: string;
  activeRequests: number;
  queuedRequests: number;
  memoryUsage: number;
  requestCount: number;
  processingRate: number;
}

interface AIInsight {
  type: 'performance' | 'optimization' | 'alert' | 'recommendation';
  title: string;
  description: string;
  severity: 'low' | 'medium' | 'high';
  actionable: boolean;
}

export default function AIDashboard() {
  const [aiMetrics, setAiMetrics] = useState<AIQueueMetrics | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealth | null>(null);
  const [newsActivity, setNewsActivity] = useState<NewsActivity | null>(null);
  const [metricsHistory, setMetricsHistory] = useState<MetricsDataPoint[]>([]);
  const [aiInsights, setAiInsights] = useState<AIInsight[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'queue' | 'performance' | 'news' | 'insights'>('overview');
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [refreshInterval, setRefreshInterval] = useState(5000);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchData = async () => {
    try {
      const [aiResponse, healthResponse, newsResponse] = await Promise.all([
        fetch('http://localhost:5002/ai/queue/metrics').then(res => res.json()),
        fetch('http://localhost:5002/health').then(res => res.json()),
        fetch('http://localhost:5002/news/stats').then(res => res.json())
      ]);

      if (aiResponse.success) setAiMetrics(aiResponse);
      if (healthResponse.status) setSystemHealth(healthResponse);
      if (newsResponse.success) setNewsActivity(newsResponse);

      // Add to metrics history
      const now = new Date().toISOString();
      const newDataPoint: MetricsDataPoint = {
        timestamp: now,
        activeRequests: aiResponse.success ? aiResponse.metrics.current.activeRequests : 0,
        queuedRequests: aiResponse.success ? aiResponse.metrics.current.queuedRequests : 0,
        memoryUsage: healthResponse.memoryUsageMB || 0,
        requestCount: healthResponse.requestCount || 0,
        processingRate: aiResponse.success ? aiResponse.metrics.totals.processed : 0
      };

      setMetricsHistory(prev => {
        const updated = [...prev, newDataPoint];
        // Keep only last 50 data points
        return updated.length > 50 ? updated.slice(-50) : updated;
      });

      // Generate AI insights
      generateInsights(aiResponse, healthResponse, newsResponse);

      setError(null);
    } catch (err) {
      console.error('Failed to fetch dashboard data:', err);
      setError('Failed to fetch data. Please check server connection.');
    }
  };

  const generateInsights = (aiData: any, healthData: any, newsData: any) => {
    const insights: AIInsight[] = [];

    // Performance insights
    if (aiData.success) {
      const queueUtilization = aiData.metrics.utilization.queueUtilization;
      if (queueUtilization > 80) {
        insights.push({
          type: 'alert',
          title: 'High Queue Utilization',
          description: `Queue utilization is at ${queueUtilization.toFixed(1)}%. Consider scaling resources.`,
          severity: 'high',
          actionable: true
        });
      }

      const healthScore = aiData.metrics.health.score;
      if (healthScore < 70) {
        insights.push({
          type: 'performance',
          title: 'AI Pipeline Health Degraded',
          description: `AI pipeline health score is ${healthScore}. Performance may be affected.`,
          severity: 'medium',
          actionable: true
        });
      }
    }

    // Memory insights
    if (healthData.memoryUsageMB > 1000) {
      insights.push({
        type: 'optimization',
        title: 'High Memory Usage',
        description: `Memory usage is ${healthData.memoryUsageMB}MB. Consider optimizing memory usage.`,
        severity: 'medium',
        actionable: true
      });
    }

    // News processing insights
    if (newsData.success && newsData.totalCount > 0) {
      const topSource = Object.entries(newsData.sources).reduce((a, b) => 
        (newsData.sources[a[0]] > newsData.sources[b[0]]) ? a : b
      );
      insights.push({
        type: 'recommendation',
        title: 'News Source Optimization',
        description: `${topSource[0]} is the most active source (${topSource[1]} items). Consider optimizing processing for this source.`,
        severity: 'low',
        actionable: true
      });
    }

    setAiInsights(insights);
  };

  useEffect(() => {
    if (autoRefresh) {
      fetchData();
      intervalRef.current = setInterval(fetchData, refreshInterval);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [autoRefresh, refreshInterval]);

  useEffect(() => {
    fetchData();
    setIsLoading(false);
  }, []);

  const getHealthColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy': return 'text-green-600';
      case 'degraded': return 'text-yellow-600';
      case 'unhealthy': return 'text-red-600';
      default: return 'text-gray-600';
    }
  };

  const getHealthBgColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy': return 'bg-green-100';
      case 'degraded': return 'bg-yellow-100';
      case 'unhealthy': return 'bg-red-100';
      default: return 'bg-gray-100';
    }
  };

  const formatUptime = (uptime: string) => {
    return uptime.replace(/\.\d+/, ''); // Remove milliseconds
  };

  const renderOverviewTab = () => (
    <div className="space-y-6">
      {/* System Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white p-6 rounded-lg shadow-sm border">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">System Status</p>
              <p className={`text-2xl font-bold ${getHealthColor(systemHealth?.status || 'unknown')}`}>
                {systemHealth?.status || 'Unknown'}
              </p>
            </div>
            <div className={`p-3 rounded-full ${getHealthBgColor(systemHealth?.status || 'unknown')}`}>
              <span className="text-2xl">🔧</span>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-sm border">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">AI Queue Health</p>
              <p className={`text-2xl font-bold ${getHealthColor(aiMetrics?.metrics.health.status || 'unknown')}`}>
                {aiMetrics?.metrics.health.status || 'Unknown'}
              </p>
            </div>
            <div className={`p-3 rounded-full ${getHealthBgColor(aiMetrics?.metrics.health.status || 'unknown')}`}>
              <span className="text-2xl">🧠</span>
            </div>
          </div>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-sm border">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Memory Usage</p>
              <p className="text-2xl font-bold text-blue-600">
                {systemHealth?.memoryUsageMB || 0} MB
              </p>
            </div>
            <div className="p-3 rounded-full bg-blue-100">
              <span className="text-2xl">💾</span>
            </div>
          </div>
        </div>
      </div>

      {/* Real-time Metrics Chart */}
      <div className="bg-white p-6 rounded-lg shadow-sm border">
        <h3 className="text-lg font-semibold mb-4">Real-time Metrics</h3>
        <div className="h-64 bg-gray-50 rounded-lg p-4 flex items-end justify-between">
          {metricsHistory.slice(-20).map((point, index) => (
            <div key={index} className="flex flex-col items-center space-y-2">
              <div 
                className="bg-blue-500 rounded-t w-4"
                style={{ height: `${Math.max(10, (point.activeRequests / 10) * 100)}px` }}
                title={`Active: ${point.activeRequests}, Queued: ${point.queuedRequests}`}
              />
              <div 
                className="bg-green-500 rounded-t w-4"
                style={{ height: `${Math.max(10, (point.queuedRequests / 10) * 100)}px` }}
              />
              <div className="text-xs text-gray-500">
                {new Date(point.timestamp).toLocaleTimeString().slice(-5)}
              </div>
            </div>
          ))}
        </div>
        <div className="flex justify-center space-x-6 mt-4 text-sm text-gray-600">
          <div className="flex items-center">
            <div className="w-3 h-3 bg-blue-500 rounded mr-2"></div>
            Active Requests
          </div>
          <div className="flex items-center">
            <div className="w-3 h-3 bg-green-500 rounded mr-2"></div>
            Queued Requests
          </div>
        </div>
      </div>
    </div>
  );

  const renderQueueTab = () => (
    <div className="space-y-6">
      {aiMetrics && (
        <>
          {/* Queue Status */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-3xl font-bold text-blue-600">{aiMetrics.metrics.current.activeRequests}</p>
                <p className="text-sm text-gray-600">Active Requests</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-3xl font-bold text-yellow-600">{aiMetrics.metrics.current.queuedRequests}</p>
                <p className="text-sm text-gray-600">Queued Requests</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-3xl font-bold text-green-600">{aiMetrics.metrics.current.completedRequests}</p>
                <p className="text-sm text-gray-600">Completed</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-3xl font-bold text-purple-600">{aiMetrics.metrics.totals.processed}</p>
                <p className="text-sm text-gray-600">Total Processed</p>
              </div>
            </div>
          </div>

          {/* Queue Configuration */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">Queue Configuration</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Max Concurrent Requests</label>
                <div className="text-2xl font-bold text-blue-600">{aiMetrics.metrics.configuration.maxConcurrentRequests}</div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Max Queue Size</label>
                <div className="text-2xl font-bold text-blue-600">{aiMetrics.metrics.configuration.maxQueueSize}</div>
              </div>
            </div>
          </div>

          {/* Utilization Metrics */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">Utilization Metrics</h3>
            <div className="space-y-4">
              <div>
                <div className="flex justify-between text-sm mb-1">
                  <span>Queue Utilization</span>
                  <span>{aiMetrics.metrics.utilization.queueUtilization.toFixed(1)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-600 h-2 rounded-full" 
                    style={{ width: `${aiMetrics.metrics.utilization.queueUtilization}%` }}
                  />
                </div>
              </div>
              <div>
                <div className="flex justify-between text-sm mb-1">
                  <span>Queue Fullness</span>
                  <span>{aiMetrics.metrics.utilization.queueFullness.toFixed(1)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-green-600 h-2 rounded-full" 
                    style={{ width: `${aiMetrics.metrics.utilization.queueFullness}%` }}
                  />
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );

  const renderPerformanceTab = () => (
    <div className="space-y-6">
      {systemHealth && (
        <>
          {/* System Metrics */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-600">{systemHealth.requestCount}</p>
                <p className="text-sm text-gray-600">Total Requests</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-2xl font-bold text-green-600">{systemHealth.activeRequests}</p>
                <p className="text-sm text-gray-600">Active Requests</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-2xl font-bold text-purple-600">{systemHealth.moduleCount}</p>
                <p className="text-sm text-gray-600">Modules</p>
              </div>
            </div>
            <div className="bg-white p-4 rounded-lg shadow-sm border">
              <div className="text-center">
                <p className="text-2xl font-bold text-orange-600">{systemHealth.nodeCount}</p>
                <p className="text-sm text-gray-600">Nodes</p>
              </div>
            </div>
          </div>

          {/* System Uptime */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">System Information</h3>
            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-gray-600">Uptime</span>
                <span className="font-medium">{formatUptime(systemHealth.uptime)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Memory Usage</span>
                <span className="font-medium">{systemHealth.memoryUsageMB} MB</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Edge Count</span>
                <span className="font-medium">{systemHealth.edgeCount.toLocaleString()}</span>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );

  const renderNewsTab = () => (
    <div className="space-y-6">
      {newsActivity && (
        <>
          {/* News Summary */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">News Processing Summary</h3>
            <div className="text-center">
              <p className="text-4xl font-bold text-blue-600">{newsActivity.totalCount}</p>
              <p className="text-gray-600">Total News Items Processed</p>
            </div>
          </div>

          {/* Top Sources */}
          <div className="bg-white p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">Top News Sources</h3>
            <div className="space-y-3">
              {Object.entries(newsActivity.sources)
                .sort(([,a], [,b]) => b - a)
                .slice(0, 10)
                .map(([source, count]) => (
                  <div key={source} className="flex justify-between items-center">
                    <span className="text-gray-700">{source}</span>
                    <span className="font-medium text-blue-600">{count}</span>
                  </div>
                ))}
            </div>
          </div>
        </>
      )}
    </div>
  );

  const renderInsightsTab = () => (
    <div className="space-y-6">
      <div className="bg-white p-6 rounded-lg shadow-sm border">
        <h3 className="text-lg font-semibold mb-4">AI Insights & Recommendations</h3>
        {aiInsights.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <span className="text-4xl mb-4 block">🎯</span>
            <p>No insights available at the moment.</p>
            <p className="text-sm">Insights will appear based on system performance patterns.</p>
          </div>
        ) : (
          <div className="space-y-4">
            {aiInsights.map((insight, index) => (
              <div key={index} className={`p-4 rounded-lg border-l-4 ${
                insight.severity === 'high' ? 'border-red-500 bg-red-50' :
                insight.severity === 'medium' ? 'border-yellow-500 bg-yellow-50' :
                'border-blue-500 bg-blue-50'
              }`}>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h4 className="font-medium text-gray-900">{insight.title}</h4>
                    <p className="text-sm text-gray-600 mt-1">{insight.description}</p>
                    <div className="flex items-center mt-2 space-x-4">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        insight.severity === 'high' ? 'bg-red-100 text-red-800' :
                        insight.severity === 'medium' ? 'bg-yellow-100 text-yellow-800' :
                        'bg-blue-100 text-blue-800'
                      }`}>
                        {insight.severity.toUpperCase()}
                      </span>
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        insight.actionable ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                      }`}>
                        {insight.actionable ? 'ACTIONABLE' : 'INFORMATIONAL'}
                      </span>
                    </div>
                  </div>
                  <span className="text-2xl ml-4">
                    {insight.type === 'alert' ? '⚠️' :
                     insight.type === 'performance' ? '📊' :
                     insight.type === 'optimization' ? '⚡' : '💡'}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading AI Dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">AI Processing Dashboard</h1>
              <p className="text-gray-600 mt-2">Real-time monitoring of AI pipeline and system performance</p>
            </div>
            <div className="flex items-center space-x-4">
              <div className="flex items-center space-x-2">
                <label className="text-sm text-gray-600">Auto-refresh:</label>
                <input
                  type="checkbox"
                  checked={autoRefresh}
                  onChange={(e) => setAutoRefresh(e.target.checked)}
                  className="rounded border-gray-300"
                />
              </div>
              <div className="flex items-center space-x-2">
                <label className="text-sm text-gray-600">Interval:</label>
                <select
                  value={refreshInterval}
                  onChange={(e) => setRefreshInterval(Number(e.target.value))}
                  className="border border-gray-300 rounded px-2 py-1 text-sm"
                >
                  <option value={2000}>2s</option>
                  <option value={5000}>5s</option>
                  <option value={10000}>10s</option>
                  <option value={30000}>30s</option>
                </select>
              </div>
              <button
                onClick={fetchData}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm"
              >
                Refresh Now
              </button>
            </div>
          </div>
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center">
              <span className="text-red-600 mr-2">⚠️</span>
              <span className="text-red-800">{error}</span>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div className="mb-6">
          <nav className="flex space-x-8">
            {[
              { id: 'overview', label: 'Overview', icon: '📊' },
              { id: 'queue', label: 'AI Queue', icon: '🧠' },
              { id: 'performance', label: 'Performance', icon: '⚡' },
              { id: 'news', label: 'News Processing', icon: '📰' },
              { id: 'insights', label: 'Insights', icon: '💡' }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center space-x-2 px-3 py-2 border-b-2 font-medium text-sm ${
                  activeTab === tab.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <span>{tab.icon}</span>
                <span>{tab.label}</span>
              </button>
            ))}
          </nav>
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === 'overview' && renderOverviewTab()}
          {activeTab === 'queue' && renderQueueTab()}
          {activeTab === 'performance' && renderPerformanceTab()}
          {activeTab === 'news' && renderNewsTab()}
          {activeTab === 'insights' && renderInsightsTab()}
        </div>

        {/* Footer */}
        <div className="mt-12 text-center text-gray-500 text-sm">
          <p>Last updated: {new Date().toLocaleString()}</p>
          <p className="mt-1">AI Dashboard - Embodying mindful monitoring and intelligent insights</p>
        </div>
      </div>
    </div>
  );
}