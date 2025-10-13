'use client';

/**
 * Future Knowledge Predictions Dashboard
 * Displays AI-powered predictions, confidence scores, timelines, and accuracy tracking
 * Connects to: FutureKnowledgeModule, LLMFutureKnowledgeModule
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { TrendingUp, Brain, LineChart, Target, Clock, Zap, AlertTriangle, CheckCircle } from 'lucide-react';

interface Prediction {
  predictionId: string;
  pattern: string;
  prediction: string;
  confidence: number;
  timeframe: string;
  createdAt: string;
  status: 'pending' | 'validating' | 'confirmed' | 'invalidated';
  accuracy?: number;
}

interface Pattern {
  patternId: string;
  name: string;
  description: string;
  frequency: number;
  trendScore: number;
  lastSeen: string;
}

interface FutureKnowledge {
  knowledgeId: string;
  content: string;
  confidence: number;
  timeframe: string;
  source: string;
  timestamp: string;
}

export default function PredictionsPage() {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'dashboard' | 'predictions' | 'patterns' | 'accuracy'>('dashboard');
  const [loading, setLoading] = useState(false);
  
  // Predictions state
  const [predictions, setPredictions] = useState<Prediction[]>([]);
  const [patterns, setPatterns] = useState<Pattern[]>([]);
  const [futureKnowledge, setFutureKnowledge] = useState<FutureKnowledge[]>([]);
  
  // Filters
  const [confidenceFilter, setConfidenceFilter] = useState<number>(0);
  const [timeframeFilter, setTimeframeFilter] = useState<string>('all');
  
  // Stats
  const [stats, setStats] = useState({
    totalPredictions: 0,
    avgConfidence: 0,
    confirmedCount: 0,
    accuracy: 0
  });

  useEffect(() => {
    if (user?.id) {
      loadPredictionsData();
    }
  }, [user?.id]);

  const loadPredictionsData = async () => {
    setLoading(true);
    try {
      await Promise.all([
        loadPredictions(),
        loadPatterns(),
        loadFutureKnowledge()
      ]);
    } finally {
      setLoading(false);
    }
  };

  const loadPredictions = async () => {
    try {
      const response = await api.post('/future-knowledge/predict', {
        context: 'general',
        timeframe: '30d',
        limit: 50
      });

      if (response?.success && response?.predictions) {
        setPredictions(response.predictions);
        
        // Calculate stats
        const confirmed = response.predictions.filter((p: Prediction) => p.status === 'confirmed');
        setStats({
          totalPredictions: response.predictions.length,
          avgConfidence: response.predictions.reduce((sum: number, p: Prediction) => sum + p.confidence, 0) / response.predictions.length,
          confirmedCount: confirmed.length,
          accuracy: confirmed.length > 0 ? confirmed.reduce((sum: number, p: Prediction) => sum + (p.accuracy || 0), 0) / confirmed.length : 0
        });
      }
    } catch (error) {
      console.error('Error loading predictions:', error);
    }
  };

  const loadPatterns = async () => {
    try {
      const response = await api.get('/future-knowledge/trending');
      if (response?.success && response?.patterns) {
        setPatterns(response.patterns);
      }
    } catch (error) {
      console.error('Error loading patterns:', error);
    }
  };

  const loadFutureKnowledge = async () => {
    try {
      const response = await api.post('/future-knowledge/retrieve', {
        targetState: 'optimal',
        depth: 3,
        confidence: 0.7
      });

      if (response?.success && response?.knowledge) {
        setFutureKnowledge(Array.isArray(response.knowledge) ? response.knowledge : [response.knowledge]);
      }
    } catch (error) {
      console.error('Error loading future knowledge:', error);
    }
  };

  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 0.8) return 'text-green-400';
    if (confidence >= 0.6) return 'text-yellow-400';
    if (confidence >= 0.4) return 'text-orange-400';
    return 'text-red-400';
  };

  const getConfidenceBg = (confidence: number) => {
    if (confidence >= 0.8) return 'bg-green-900/20';
    if (confidence >= 0.6) return 'bg-yellow-900/20';
    if (confidence >= 0.4) return 'bg-orange-900/20';
    return 'bg-red-900/20';
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'confirmed':
        return <CheckCircle className="w-4 h-4 text-green-400" />;
      case 'invalidated':
        return <AlertTriangle className="w-4 h-4 text-red-400" />;
      case 'validating':
        return <Clock className="w-4 h-4 text-yellow-400" />;
      default:
        return <Clock className="w-4 h-4 text-gray-400" />;
    }
  };

  const filteredPredictions = predictions.filter(p => {
    if (p.confidence < confidenceFilter / 100) return false;
    if (timeframeFilter !== 'all' && p.timeframe !== timeframeFilter) return false;
    return true;
  });

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-cyan-400 to-blue-400 bg-clip-text text-transparent flex items-center gap-3">
            <Brain className="w-10 h-10 text-cyan-400" />
            Future Knowledge Predictions
          </h1>
          <p className="text-gray-400 mt-2">
            AI-powered pattern analysis and knowledge predictions
          </p>
        </div>

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700">
          {[
            { id: 'dashboard', label: 'Dashboard', icon: TrendingUp },
            { id: 'predictions', label: 'Predictions', icon: Brain },
            { id: 'patterns', label: 'Patterns', icon: Zap },
            { id: 'accuracy', label: 'Accuracy', icon: Target }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`flex items-center gap-2 px-4 py-2 border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-cyan-400 text-cyan-400'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              }`}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
            </button>
          ))}
        </div>

        {/* Dashboard Tab */}
        {activeTab === 'dashboard' && (
          <div className="space-y-6">
            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <Card className="p-4">
                <div className="text-gray-400 text-sm mb-1">Total Predictions</div>
                <div className="text-3xl font-bold text-cyan-400">{stats.totalPredictions}</div>
              </Card>
              <Card className="p-4">
                <div className="text-gray-400 text-sm mb-1">Avg Confidence</div>
                <div className="text-3xl font-bold text-blue-400">{(stats.avgConfidence * 100).toFixed(0)}%</div>
              </Card>
              <Card className="p-4">
                <div className="text-gray-400 text-sm mb-1">Confirmed</div>
                <div className="text-3xl font-bold text-green-400">{stats.confirmedCount}</div>
              </Card>
              <Card className="p-4">
                <div className="text-gray-400 text-sm mb-1">Accuracy</div>
                <div className="text-3xl font-bold text-purple-400">{(stats.accuracy * 100).toFixed(0)}%</div>
              </Card>
            </div>

            {/* Recent Predictions */}
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-gray-200 mb-4">🔮 Recent Predictions</h3>
              <div className="space-y-3">
                {predictions.slice(0, 5).map((pred) => (
                  <div key={pred.predictionId} className={`p-4 ${getConfidenceBg(pred.confidence)} rounded-lg border border-gray-700`}>
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          {getStatusIcon(pred.status)}
                          <span className="font-medium text-gray-200">{pred.prediction}</span>
                        </div>
                        <p className="text-sm text-gray-400 mb-2">Pattern: {pred.pattern}</p>
                        <div className="flex items-center gap-4 text-xs text-gray-500">
                          <span>🎯 Confidence: <span className={getConfidenceColor(pred.confidence)}>{(pred.confidence * 100).toFixed(0)}%</span></span>
                          <span>⏱️ Timeframe: {pred.timeframe}</span>
                          <span>📅 {new Date(pred.createdAt).toLocaleDateString()}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </Card>

            {/* Trending Patterns */}
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-gray-200 mb-4">⚡ Trending Patterns</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {patterns.slice(0, 6).map((pattern) => (
                  <div key={pattern.patternId} className="p-3 bg-gray-800/50 rounded-lg border border-gray-700">
                    <div className="font-medium text-gray-200 mb-1">{pattern.name}</div>
                    <p className="text-xs text-gray-400 mb-2">{pattern.description}</p>
                    <div className="flex items-center justify-between text-xs text-gray-500">
                      <span>Frequency: {pattern.frequency}</span>
                      <span className="text-cyan-400">Trend: {pattern.trendScore.toFixed(1)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        )}

        {/* Predictions Tab */}
        {activeTab === 'predictions' && (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-200">
                🔮 All Predictions ({filteredPredictions.length})
              </h3>
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-2">
                  <label className="text-sm text-gray-400">Min Confidence:</label>
                  <input
                    type="range"
                    min="0"
                    max="100"
                    value={confidenceFilter}
                    onChange={(e) => setConfidenceFilter(parseInt(e.target.value))}
                    className="w-32"
                  />
                  <span className="text-sm text-gray-300">{confidenceFilter}%</span>
                </div>
                <select
                  value={timeframeFilter}
                  onChange={(e) => setTimeframeFilter(e.target.value)}
                  className="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-gray-200 text-sm"
                >
                  <option value="all">All Timeframes</option>
                  <option value="7d">7 Days</option>
                  <option value="30d">30 Days</option>
                  <option value="90d">90 Days</option>
                  <option value="1y">1 Year</option>
                </select>
              </div>
            </div>

            <div className="space-y-3 max-h-[600px] overflow-y-auto">
              {filteredPredictions.map((pred) => (
                <div key={pred.predictionId} className={`p-4 ${getConfidenceBg(pred.confidence)} rounded-lg border border-gray-700`}>
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(pred.status)}
                      <span className="text-sm px-2 py-1 bg-gray-800 rounded text-gray-300">{pred.status}</span>
                    </div>
                    <div className={`text-xl font-bold ${getConfidenceColor(pred.confidence)}`}>
                      {(pred.confidence * 100).toFixed(0)}%
                    </div>
                  </div>
                  <p className="text-gray-200 font-medium mb-2">{pred.prediction}</p>
                  <div className="flex items-center gap-4 text-xs text-gray-400">
                    <span>📊 Pattern: {pred.pattern}</span>
                    <span>⏱️ Timeframe: {pred.timeframe}</span>
                    <span>📅 {new Date(pred.createdAt).toLocaleString()}</span>
                    {pred.accuracy !== undefined && (
                      <span className="text-green-400">✓ Accuracy: {(pred.accuracy * 100).toFixed(0)}%</span>
                    )}
                  </div>
                </div>
              ))}

              {filteredPredictions.length === 0 && (
                <div className="text-center py-12 text-gray-500">
                  <Brain className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No predictions match your filters</p>
                  <p className="text-sm mt-2">Adjust filters or wait for new predictions</p>
                </div>
              )}
            </div>
          </Card>
        )}

        {/* Patterns Tab */}
        {activeTab === 'patterns' && (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-200">
                ⚡ Discovered Patterns ({patterns.length})
              </h3>
              <button
                onClick={loadPatterns}
                className="px-4 py-2 bg-cyan-600 hover:bg-cyan-700 rounded-lg transition-colors text-sm"
              >
                🔄 Refresh
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {patterns.map((pattern) => (
                <div key={pattern.patternId} className="p-4 bg-gray-800/50 rounded-lg border border-gray-700">
                  <div className="flex items-center gap-2 mb-2">
                    <Zap className="w-5 h-5 text-cyan-400" />
                    <h4 className="font-semibold text-gray-200">{pattern.name}</h4>
                  </div>
                  <p className="text-sm text-gray-400 mb-3">{pattern.description}</p>
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-gray-500">
                      Frequency: <span className="text-cyan-400">{pattern.frequency}</span>
                    </span>
                    <span className="text-gray-500">
                      Trend: <span className="text-cyan-400">{pattern.trendScore.toFixed(1)}</span>
                    </span>
                  </div>
                  <div className="text-xs text-gray-500 mt-2">
                    Last seen: {new Date(pattern.lastSeen).toLocaleDateString()}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* Accuracy Tab */}
        {activeTab === 'accuracy' && (
          <Card className="p-6">
            <h3 className="text-xl font-semibold text-gray-200 mb-6">🎯 Prediction Accuracy Tracking</h3>

            {/* Accuracy Overview */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
              <div className="p-4 bg-green-900/20 rounded-lg border border-green-700">
                <div className="text-sm text-gray-400 mb-1">Confirmed Predictions</div>
                <div className="text-2xl font-bold text-green-400">{stats.confirmedCount}</div>
                <div className="text-xs text-gray-500 mt-1">
                  {stats.totalPredictions > 0 ? ((stats.confirmedCount / stats.totalPredictions) * 100).toFixed(1) : 0}% of total
                </div>
              </div>
              <div className="p-4 bg-blue-900/20 rounded-lg border border-blue-700">
                <div className="text-sm text-gray-400 mb-1">Overall Accuracy</div>
                <div className="text-2xl font-bold text-blue-400">{(stats.accuracy * 100).toFixed(1)}%</div>
                <div className="text-xs text-gray-500 mt-1">
                  Across confirmed predictions
                </div>
              </div>
              <div className="p-4 bg-purple-900/20 rounded-lg border border-purple-700">
                <div className="text-sm text-gray-400 mb-1">Avg Confidence</div>
                <div className="text-2xl font-bold text-purple-400">{(stats.avgConfidence * 100).toFixed(1)}%</div>
                <div className="text-xs text-gray-500 mt-1">
                  Average prediction confidence
                </div>
              </div>
            </div>

            {/* Timeline Visualization */}
            <div className="mb-6">
              <h4 className="text-lg font-semibold text-gray-200 mb-4">📈 Prediction Timeline</h4>
              <div className="space-y-2">
                {predictions
                  .filter(p => p.status === 'confirmed' || p.status === 'invalidated')
                  .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
                  .map((pred) => (
                    <div key={pred.predictionId} className="flex items-center gap-3">
                      <div className="text-xs text-gray-500 w-32">
                        {new Date(pred.createdAt).toLocaleDateString()}
                      </div>
                      <div className="flex-1 h-8 bg-gray-800 rounded-lg overflow-hidden flex items-center">
                        <div 
                          className={`h-full ${pred.status === 'confirmed' ? 'bg-green-600' : 'bg-red-600'}`}
                          style={{ width: `${pred.confidence * 100}%` }}
                        ></div>
                        <span className="px-3 text-sm text-gray-300">{pred.prediction.substring(0, 50)}...</span>
                      </div>
                      <div className="text-xs text-gray-500 w-24 text-right">
                        {pred.accuracy !== undefined ? `${(pred.accuracy * 100).toFixed(0)}%` : '-'}
                      </div>
                    </div>
                  ))}
              </div>
            </div>

            {/* Confidence Distribution */}
            <div>
              <h4 className="text-lg font-semibold text-gray-200 mb-4">📊 Confidence Distribution</h4>
              <div className="grid grid-cols-5 gap-2">
                {[
                  { range: '0-20%', color: 'bg-red-600', count: predictions.filter(p => p.confidence < 0.2).length },
                  { range: '20-40%', color: 'bg-orange-600', count: predictions.filter(p => p.confidence >= 0.2 && p.confidence < 0.4).length },
                  { range: '40-60%', color: 'bg-yellow-600', count: predictions.filter(p => p.confidence >= 0.4 && p.confidence < 0.6).length },
                  { range: '60-80%', color: 'bg-blue-600', count: predictions.filter(p => p.confidence >= 0.6 && p.confidence < 0.8).length },
                  { range: '80-100%', color: 'bg-green-600', count: predictions.filter(p => p.confidence >= 0.8).length }
                ].map((bucket) => (
                  <div key={bucket.range} className="text-center">
                    <div className={`h-24 ${bucket.color} rounded-lg mb-2 flex items-end justify-center pb-2 text-white font-bold`}>
                      {bucket.count}
                    </div>
                    <div className="text-xs text-gray-400">{bucket.range}</div>
                  </div>
                ))}
              </div>
            </div>
          </Card>
        )}

        {/* Future Knowledge Tab (already showing in predictions, can add more detail) */}
        {futureKnowledge.length > 0 && activeTab === 'dashboard' && (
          <Card className="p-6">
            <h3 className="text-xl font-semibold text-gray-200 mb-4">🌟 Future Knowledge Insights</h3>
            <div className="space-y-3">
              {futureKnowledge.map((knowledge, index) => (
                <div key={knowledge.knowledgeId || index} className="p-4 bg-indigo-900/20 rounded-lg border border-indigo-700">
                  <p className="text-gray-200 mb-2">{knowledge.content}</p>
                  <div className="flex items-center gap-4 text-xs text-gray-400">
                    <span className={getConfidenceColor(knowledge.confidence)}>
                      Confidence: {(knowledge.confidence * 100).toFixed(0)}%
                    </span>
                    <span>⏱️ {knowledge.timeframe}</span>
                    <span>📍 Source: {knowledge.source}</span>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

