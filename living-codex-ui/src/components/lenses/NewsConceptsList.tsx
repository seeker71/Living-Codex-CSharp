'use client';

import { useState, useEffect } from 'react';
import { Network, ExternalLink, Zap, Brain, TrendingUp } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { buildApiUrl } from '@/lib/config';

interface NewsConcept {
  id: string;
  name: string;
  description?: string;
  weight?: number;
  resonance?: number;
  confidence?: number;
  extractedAt?: string;
  conceptType?: string;
  axes?: string[];
  meta?: Record<string, any>;
}

interface NewsConceptsListProps {
  newsItemId: string;
  newsTitle: string;
  className?: string;
}

export function NewsConceptsList({ newsItemId, newsTitle, className = '' }: NewsConceptsListProps) {
  const [concepts, setConcepts] = useState<NewsConcept[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadConcepts();
  }, [newsItemId]);

  const loadConcepts = async () => {
    setLoading(true);
    setError(null);
    
    try {
      // Use the new API endpoint to get concepts for the news item
      const response = await fetch(buildApiUrl(`/news/concepts/${encodeURIComponent(newsItemId)}`));
      
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error('News item not found or no concepts extracted yet');
        }
        throw new Error(`Failed to fetch concepts: ${response.statusText}`);
      }
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.error || 'Failed to load concepts');
      }
      
      // Transform the API response to NewsConcept format
      const transformedConcepts: NewsConcept[] = (data.concepts || []).map((concept: any) => ({
        id: concept.id,
        name: concept.name || 'Unknown Concept',
        description: concept.description,
        weight: concept.weight || 1.0,
        resonance: concept.resonance || 0.5,
        confidence: concept.confidence || 0.8,
        extractedAt: concept.extractedAt,
        conceptType: concept.conceptType,
        axes: concept.axes || [],
        meta: concept.meta || {}
      }));
      
      setConcepts(transformedConcepts);
    } catch (err) {
      console.error('Error loading concepts:', err);
      setError(err instanceof Error ? err.message : 'Failed to load concepts');
    } finally {
      setLoading(false);
    }
  };

  const getWeightColor = (weight: number) => {
    if (weight >= 0.8) return 'text-green-600 dark:text-green-400';
    if (weight >= 0.6) return 'text-yellow-600 dark:text-yellow-400';
    if (weight >= 0.4) return 'text-orange-600 dark:text-orange-400';
    return 'text-red-600 dark:text-red-400';
  };

  const getWeightBackground = (weight: number) => {
    if (weight >= 0.8) return 'bg-green-100 dark:bg-green-900/30';
    if (weight >= 0.6) return 'bg-yellow-100 dark:bg-yellow-900/30';
    if (weight >= 0.4) return 'bg-orange-100 dark:bg-orange-900/30';
    return 'bg-red-100 dark:bg-red-900/30';
  };

  const getAxisColor = (axis: string) => {
    const colors: Record<string, string> = {
      abundance: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
      unity: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
      resonance: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
      innovation: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300',
      science: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900/30 dark:text-cyan-300',
      consciousness: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-300',
      impact: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
    };
    return colors[axis] || 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300';
  };

  if (loading) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg flex items-center space-x-2">
            <Brain className="w-5 h-5" />
            <span>Extracted Concepts</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <span className="ml-2 text-gray-500">Loading concepts...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg flex items-center space-x-2">
            <Brain className="w-5 h-5" />
            <span>Extracted Concepts</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-red-600 dark:text-red-400">
            <p>Failed to load concepts: {error}</p>
            <button
              onClick={loadConcepts}
              className="mt-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              Retry
            </button>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (concepts.length === 0) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg flex items-center space-x-2">
            <Brain className="w-5 h-5" />
            <span>Extracted Concepts</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-gray-500 dark:text-gray-400">
            <Brain className="w-12 h-12 mx-auto mb-4 text-gray-300" />
            <p>No concepts extracted from this news item yet.</p>
            <p className="text-sm mt-1">Concepts are extracted automatically when news items are processed.</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-lg flex items-center space-x-2">
          <Brain className="w-5 h-5" />
          <span>Extracted Concepts</span>
          <span className="text-sm font-normal text-gray-500 dark:text-gray-400">
            ({concepts.length} concepts)
          </span>
        </CardTitle>
        <p className="text-sm text-gray-600 dark:text-gray-300">
          AI-extracted concepts from: <em>{newsTitle}</em>
        </p>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {concepts.map((concept, index) => (
            <div
              key={concept.id}
              className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
            >
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2 mb-1">
                  <button
                    onClick={() => window.open(`/node/${concept.id}`, '_blank')}
                    className="font-medium text-gray-900 dark:text-gray-100 hover:text-blue-600 dark:hover:text-blue-400 hover:underline text-left truncate"
                    title={`View concept node: ${concept.name}`}
                  >
                    {concept.name}
                  </button>
                  <ExternalLink className="w-3 h-3 text-gray-400 flex-shrink-0" />
                </div>
                
                {concept.description && (
                  <p className="text-sm text-gray-600 dark:text-gray-300 line-clamp-2 mb-2">
                    {concept.description}
                  </p>
                )}
                
                <div className="flex items-center space-x-2 flex-wrap">
                  {/* Weight/Confidence Score */}
                  <div className={`px-2 py-1 rounded-full text-xs font-medium ${getWeightBackground(concept.weight || 0)} ${getWeightColor(concept.weight || 0)}`}>
                    <TrendingUp className="w-3 h-3 inline mr-1" />
                    {Math.round((concept.weight || 0) * 100)}%
                  </div>
                  
                  {/* Resonance Score */}
                  {concept.resonance && (
                    <div className="px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300">
                      <Zap className="w-3 h-3 inline mr-1" />
                      {Math.round(concept.resonance * 100)}%
                    </div>
                  )}
                  
                  {/* Concept Type */}
                  {concept.conceptType && (
                    <div className="px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300">
                      {concept.conceptType.replace('codex.concept.', '')}
                    </div>
                  )}
                  
                  {/* Axes */}
                  {concept.axes && concept.axes.length > 0 && (
                    <div className="flex space-x-1">
                      {concept.axes.slice(0, 3).map((axis) => (
                        <span
                          key={axis}
                          className={`px-2 py-1 rounded-full text-xs font-medium ${getAxisColor(axis)}`}
                        >
                          {axis}
                        </span>
                      ))}
                      {concept.axes.length > 3 && (
                        <span className="px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300">
                          +{concept.axes.length - 3}
                        </span>
                      )}
                    </div>
                  )}
                </div>
              </div>
              
              <div className="flex items-center space-x-2 ml-4">
                <button
                  onClick={() => window.open(`/node/${concept.id}`, '_blank')}
                  className="p-2 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-md transition-colors"
                  title="View concept details"
                >
                  <Network className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
        
        {concepts.length > 0 && (
          <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
              <span>
                Showing {concepts.length} extracted concepts
              </span>
              <span>
                Sorted by relevance weight
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
