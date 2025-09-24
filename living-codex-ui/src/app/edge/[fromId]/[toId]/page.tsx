'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Navigation } from '@/components/ui/Navigation';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface EdgeData {
  fromId: string;
  toId: string;
  role: string;
  weight: number;
  meta?: Record<string, any>;
}

interface NodeData {
  id: string;
  typeId: string;
  title: string;
  description: string;
  state: string;
  locale: string;
  content?: {
    mediaType: string;
    inlineJson?: string;
    inlineBytes?: string;
    externalUri?: string;
  };
  meta?: Record<string, any>;
  createdAt?: string;
  updatedAt?: string;
}

export default function EdgeDetailPage() {
  const { fromId, toId } = useParams();
  const router = useRouter();
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Data state
  const [edge, setEdge] = useState<EdgeData | null>(null);
  const [fromNode, setFromNode] = useState<NodeData | null>(null);
  const [toNode, setToNode] = useState<NodeData | null>(null);
  
  // UI state
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  // Track page visit
  useEffect(() => {
    if (user?.id && fromId && toId) {
      trackInteraction(`${fromId}-${toId}`, 'view-edge', { 
        description: `User viewed edge detail page: ${fromId} -> ${toId}`,
        fromId: fromId as string,
        toId: toId as string
      });
    }
  }, [user?.id, fromId, toId, trackInteraction]);

  // Load edge data
  useEffect(() => {
    if (fromId && toId) {
      loadEdgeData(fromId as string, toId as string);
    }
  }, [fromId, toId]);

  const loadEdgeData = async (fromNodeId: string, toNodeId: string) => {
    setLoading(true);
    setError('');
    
    try {
      // Load edge details
      const edgeResponse = await fetch(buildApiUrl(`/storage-endpoints/edges/${fromNodeId}/${toNodeId}`));
      if (edgeResponse.ok) {
        const edgeData = await edgeResponse.json();
        if (edgeData.edge) {
          setEdge(edgeData.edge);
        } else {
          setError('Edge not found');
          return;
        }
      } else {
        setError('Failed to load edge');
        return;
      }

      // Load from node
      const fromResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${fromNodeId}`));
      if (fromResponse.ok) {
        const fromData = await fromResponse.json();
        if (fromData.node) {
          setFromNode(fromData.node);
        }
      }

      // Load to node
      const toResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${toNodeId}`));
      if (toResponse.ok) {
        const toData = await toResponse.json();
        if (toData.node) {
          setToNode(toData.node);
        }
      }

    } catch (err) {
      console.error('Error loading edge data:', err);
      setError('Error loading edge data');
    } finally {
      setLoading(false);
    }
  };

  const getNodeTypeIcon = (typeId: string): string => {
    const iconMap: Record<string, string> = {
      'codex.meta/node': '🧩',
      'codex.meta/type': '🏷️',
      'codex.meta/route': '🛤️',
      'codex.meta/method': '⚙️',
      'codex.meta/spec': '📋',
      'codex.ontology.axis': '🌟',
      'concept': '💡',
      'user': '👤',
      'news': '📰',
      'contribution': '🎯',
      'module': '📦'
    };
    return iconMap[typeId] || '🔵';
  };

  const getStateColor = (state: string): string => {
    const colorMap: Record<string, string> = {
      'Ice': 'blue',
      'Water': 'cyan',
      'Gas': 'purple',
      'Plasma': 'red'
    };
    return colorMap[state] || 'gray';
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <Navigation />
        <div className="max-w-6xl mx-auto px-4 py-8">
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-500">Loading edge...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error || !edge) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <Navigation />
        <div className="max-w-6xl mx-auto px-4 py-8">
          <div className="text-center py-12">
            <div className="text-6xl mb-4">❌</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Edge Not Found</h2>
            <p className="text-gray-600 mb-4">{error || 'The requested edge could not be found.'}</p>
            <button
              onClick={() => router.back()}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
            >
              Go Back
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <Navigation />
      
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <button
                onClick={() => router.back()}
                className="text-gray-600 hover:text-gray-800 text-2xl"
              >
                ←
              </button>
              <div>
                <div className="flex items-center space-x-3 mb-2">
                  <span className="text-3xl">🔗</span>
                  <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
                    Edge Relationship
                  </h1>
                </div>
                <div className="flex items-center space-x-4 text-sm text-gray-600">
                  <span className="px-2 py-1 bg-blue-100 text-blue-800 rounded-md font-medium">
                    {edge.role}
                  </span>
                  <span>Weight: {edge.weight}</span>
                  <span>From: {edge.fromId}</span>
                  <span>To: {edge.toId}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Edge Details */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {/* From Node */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <span className="text-lg">📤</span>
                <span>From Node</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {fromNode ? (
                <div className="space-y-3">
                  <div className="flex items-center space-x-3">
                    <span className="text-2xl">{getNodeTypeIcon(fromNode.typeId)}</span>
                    <div>
                      <h3 className="font-medium text-gray-900 dark:text-gray-100">
                        <button
                          onClick={() => router.push(`/node/${fromNode.id}`)}
                          className="hover:text-blue-600 hover:underline"
                        >
                          {fromNode.title}
                        </button>
                      </h3>
                      <p className="text-sm text-gray-600">{fromNode.typeId}</p>
                    </div>
                  </div>
                  <div className="text-sm text-gray-600">
                    {fromNode.description}
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 bg-${getStateColor(fromNode.state)}-100 text-${getStateColor(fromNode.state)}-800 rounded-md text-xs`}>
                      {fromNode.state}
                    </span>
                    <span className="text-xs text-gray-500">ID: {fromNode.id}</span>
                  </div>
                </div>
              ) : (
                <div className="text-center py-4 text-gray-500">
                  <div className="text-2xl mb-2">❓</div>
                  <p>From node not found</p>
                  <p className="text-sm">ID: {edge.fromId}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* To Node */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <span className="text-lg">📥</span>
                <span>To Node</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {toNode ? (
                <div className="space-y-3">
                  <div className="flex items-center space-x-3">
                    <span className="text-2xl">{getNodeTypeIcon(toNode.typeId)}</span>
                    <div>
                      <h3 className="font-medium text-gray-900 dark:text-gray-100">
                        <button
                          onClick={() => router.push(`/node/${toNode.id}`)}
                          className="hover:text-blue-600 hover:underline"
                        >
                          {toNode.title}
                        </button>
                      </h3>
                      <p className="text-sm text-gray-600">{toNode.typeId}</p>
                    </div>
                  </div>
                  <div className="text-sm text-gray-600">
                    {toNode.description}
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 bg-${getStateColor(toNode.state)}-100 text-${getStateColor(toNode.state)}-800 rounded-md text-xs`}>
                      {toNode.state}
                    </span>
                    <span className="text-xs text-gray-500">ID: {toNode.id}</span>
                  </div>
                </div>
              ) : (
                <div className="text-center py-4 text-gray-500">
                  <div className="text-2xl mb-2">❓</div>
                  <p>To node not found</p>
                  <p className="text-sm">ID: {edge.toId}</p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Edge Metadata */}
        {edge.meta && Object.keys(edge.meta).length > 0 && (
          <Card className="mb-8">
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <span className="text-lg">🏷️</span>
                <span>Edge Metadata</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {Object.entries(edge.meta).map(([key, value]) => (
                  <div key={key} className="border-b border-gray-200 pb-3 last:border-b-0">
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {key}
                    </label>
                    <div className="text-gray-900 dark:text-gray-100">
                      {typeof value === 'object' ? (
                        <pre className="bg-gray-100 dark:bg-gray-700 p-3 rounded-md text-sm font-mono overflow-x-auto">
                          {JSON.stringify(value, null, 2)}
                        </pre>
                      ) : (
                        <p className="text-sm">{String(value)}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Actions */}
        <Card>
          <CardHeader>
            <CardTitle>Actions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-3">
              <button
                onClick={() => router.push(`/graph?selectedEdge=${edge.fromId}-${edge.toId}`)}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
              >
                🕸️ View in Graph
              </button>
              
              <button
                onClick={() => {
                  navigator.clipboard.writeText(`${edge.fromId} -> ${edge.toId}`);
                }}
                className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors"
              >
                📋 Copy Edge ID
              </button>
              
              <button
                onClick={() => {
                  const edgeJson = JSON.stringify(edge, null, 2);
                  const blob = new Blob([edgeJson], { type: 'application/json' });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = `edge-${edge.fromId}-${edge.toId}.json`;
                  a.click();
                  URL.revokeObjectURL(url);
                }}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors"
              >
                💾 Export JSON
              </button>
              
              {fromNode && (
                <button
                  onClick={() => router.push(`/node/${fromNode.id}`)}
                  className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors"
                >
                  📤 View From Node
                </button>
              )}
              
              {toNode && (
                <button
                  onClick={() => router.push(`/node/${toNode.id}`)}
                  className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors"
                >
                  📥 View To Node
                </button>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
