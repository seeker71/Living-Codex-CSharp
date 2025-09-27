'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { buildApiUrl } from '@/lib/config';

interface GraphNavigationProps {
  currentNodeId: string;
  onNavigate: (nodeId: string) => void;
  className?: string;
}

interface NodeConnection {
  nodeId: string;
  title: string;
  typeId: string;
  relationship: string;
  direction: 'incoming' | 'outgoing';
  weight?: number;
}

export function GraphNavigation({ currentNodeId, onNavigate, className = '' }: GraphNavigationProps) {
  const router = useRouter();
  const [connections, setConnections] = useState<NodeConnection[]>([]);
  const [loading, setLoading] = useState(false);
  const [visitedNodes, setVisitedNodes] = useState<string[]>([currentNodeId]);
  const [navigationHistory, setNavigationHistory] = useState<string[]>([currentNodeId]);

  useEffect(() => {
    loadConnections(currentNodeId);
  }, [currentNodeId]);

  const loadConnections = async (nodeId: string) => {
    setLoading(true);
    try {
      // Load edges for this node
      const edgesResponse = await fetch(buildApiUrl(`/storage-endpoints/edges?nodeId=${nodeId}`));
      if (edgesResponse.ok) {
        const edgesData = await edgesResponse.json();
        if (edgesData.edges) {
          const connectionsList: NodeConnection[] = [];
          
          // Process each edge
          for (const edge of edgesData.edges) {
            const relatedNodeId = edge.fromId === nodeId ? edge.toId : edge.fromId;
            
            // Skip if we've already visited this node recently
            if (visitedNodes.includes(relatedNodeId)) continue;
            
            try {
              // Load the related node
              const nodeResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${relatedNodeId}`));
              if (nodeResponse.ok) {
                const nodeData = await nodeResponse.json();
                if (nodeData.node) {
                  connectionsList.push({
                    nodeId: relatedNodeId,
                    title: nodeData.node.title || relatedNodeId,
                    typeId: nodeData.node.typeId,
                    relationship: edge.role,
                    direction: edge.fromId === nodeId ? 'outgoing' : 'incoming',
                    weight: edge.weight
                  });
                }
              }
            } catch (err) {
              console.error(`Error loading node ${relatedNodeId}:`, err);
            }
          }
          
          setConnections(connectionsList);
        }
      }
    } catch (err) {
      console.error('Error loading connections:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleNavigate = (nodeId: string) => {
    // Add to visited nodes
    setVisitedNodes(prev => [...prev, nodeId]);
    
    // Add to navigation history
    setNavigationHistory(prev => [...prev, nodeId]);
    
    // Navigate
    onNavigate(nodeId);
  };

  const handleGoBack = () => {
    if (navigationHistory.length > 1) {
      const newHistory = [...navigationHistory];
      newHistory.pop(); // Remove current
      const previousNode = newHistory[newHistory.length - 1];
      setNavigationHistory(newHistory);
      onNavigate(previousNode);
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

  const getRelationshipColor = (relationship: string, direction: 'incoming' | 'outgoing'): string => {
    const colors = {
      'defines': direction === 'outgoing' ? 'bg-blue-100 text-blue-800' : 'bg-blue-100 text-blue-800',
      'implements': direction === 'outgoing' ? 'bg-green-100 text-green-800' : 'bg-green-100 text-green-800',
      'contains': direction === 'outgoing' ? 'bg-purple-100 text-purple-800' : 'bg-purple-100 text-purple-800',
      'uses': direction === 'outgoing' ? 'bg-orange-100 text-orange-800' : 'bg-orange-100 text-orange-800',
      'extends': direction === 'outgoing' ? 'bg-indigo-100 text-indigo-800' : 'bg-indigo-100 text-indigo-800',
      'references': direction === 'outgoing' ? 'bg-pink-100 text-pink-800' : 'bg-pink-100 text-pink-800',
    };
    return colors[relationship as keyof typeof colors] || 'bg-gray-100 text-gray-800';
  };

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span className="flex items-center space-x-2">
            <span>🧭</span>
            <span>Graph Navigation</span>
          </span>
          <div className="flex items-center space-x-2">
            <span className="text-xs text-gray-500">
              Visited: {visitedNodes.length}
            </span>
            {navigationHistory.length > 1 && (
              <button
                onClick={handleGoBack}
                className="px-2 py-1 text-xs bg-gray-100 text-gray-800 rounded hover:bg-gray-200"
              >
                ← Back
              </button>
            )}
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="text-center py-4">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 mx-auto"></div>
            <p className="text-sm text-gray-500 mt-2">Loading connections...</p>
          </div>
        ) : connections.length > 0 ? (
          <div className="space-y-2">
            <div className="text-sm text-gray-600 mb-3">
              Connected nodes ({connections.length}):
            </div>
            <div className="max-h-64 overflow-y-auto space-y-2">
              {connections.map((connection, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-2 border border-gray-200 rounded hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-center space-x-2 flex-1 min-w-0">
                    <span className="text-lg">{getNodeTypeIcon(connection.typeId)}</span>
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-gray-900 truncate">
                        {connection.title}
                      </div>
                      <div className="text-xs text-gray-500 truncate">
                        {connection.typeId}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 rounded text-xs font-medium ${getRelationshipColor(connection.relationship, connection.direction)}`}>
                      {connection.direction === 'outgoing' ? '→' : '←'} {connection.relationship}
                    </span>
                    <button
                      onClick={() => handleNavigate(connection.nodeId)}
                      className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded hover:bg-blue-200 transition-colors"
                    >
                      Go
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : (
          <div className="text-center py-4 text-gray-500">
            <div className="text-2xl mb-2">🔗</div>
            <p className="text-sm">No unvisited connections found</p>
            <p className="text-xs mt-1">Try exploring different nodes to find new connections</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
