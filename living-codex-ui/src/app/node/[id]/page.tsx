'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Navigation } from '@/components/ui/Navigation';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';

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

interface EdgeData {
  fromId: string;
  toId: string;
  relationship: string;
  weight?: number;
  metadata?: Record<string, any>;
}

interface RelatedNode {
  node: NodeData;
  relationship: string;
  direction: 'incoming' | 'outgoing';
  weight?: number;
}

export default function NodeDetailPage() {
  const { id } = useParams();
  const router = useRouter();
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Data state
  const [node, setNode] = useState<NodeData | null>(null);
  const [relatedNodes, setRelatedNodes] = useState<RelatedNode[]>([]);
  const [edges, setEdges] = useState<EdgeData[]>([]);
  
  // UI state
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [activeTab, setActiveTab] = useState<'details' | 'content' | 'relationships' | 'metadata'>('details');
  const [editMode, setEditMode] = useState(false);
  const [editedNode, setEditedNode] = useState<Partial<NodeData>>({});

  // Track page visit
  useEffect(() => {
    if (user?.id && id) {
      trackInteraction(id as string, 'view-node', { 
        description: `User viewed node detail page: ${id}`,
        nodeId: id 
      });
    }
  }, [user?.id, id, trackInteraction]);

  // Load node data
  useEffect(() => {
    if (id) {
      loadNodeData(id as string);
    }
  }, [id]);

  const loadNodeData = async (nodeId: string) => {
    setLoading(true);
    setError('');
    
    try {
      // Load node details
      const nodeResponse = await fetch(`http://localhost:5002/storage-endpoints/nodes/${nodeId}`);
      if (nodeResponse.ok) {
        const nodeData = await nodeResponse.json();
        if (nodeData.node) {
          setNode(nodeData.node);
          setEditedNode(nodeData.node);
        } else {
          setError('Node not found');
          return;
        }
      } else {
        setError('Failed to load node');
        return;
      }

      // Load relationships
      await loadNodeRelationships(nodeId);

    } catch (err) {
      console.error('Error loading node data:', err);
      setError('Error loading node data');
    } finally {
      setLoading(false);
    }
  };

  const loadNodeRelationships = async (nodeId: string) => {
    try {
      // Load edges involving this node
      const edgesResponse = await fetch(`http://localhost:5002/storage-endpoints/edges?nodeId=${nodeId}`);
      if (edgesResponse.ok) {
        const edgesData = await edgesResponse.json();
        if (edgesData.edges) {
          setEdges(edgesData.edges);
          
          // Load related nodes
          const relatedNodeIds = new Set<string>();
          edgesData.edges.forEach((edge: EdgeData) => {
            if (edge.fromId === nodeId) relatedNodeIds.add(edge.toId);
            if (edge.toId === nodeId) relatedNodeIds.add(edge.fromId);
          });

          const related: RelatedNode[] = [];
          for (const relatedId of relatedNodeIds) {
            try {
              const relatedResponse = await fetch(`http://localhost:5002/storage-endpoints/nodes/${relatedId}`);
              if (relatedResponse.ok) {
                const relatedData = await relatedResponse.json();
                if (relatedData.node) {
                  const edge = edgesData.edges.find((e: EdgeData) => 
                    (e.fromId === nodeId && e.toId === relatedId) || 
                    (e.fromId === relatedId && e.toId === nodeId)
                  );
                  
                  related.push({
                    node: relatedData.node,
                    relationship: edge?.relationship || 'related',
                    direction: edge?.fromId === nodeId ? 'outgoing' : 'incoming',
                    weight: edge?.weight
                  });
                }
              }
            } catch (err) {
              console.error(`Error loading related node ${relatedId}:`, err);
            }
          }
          
          setRelatedNodes(related);
        }
      }
    } catch (err) {
      console.error('Error loading relationships:', err);
    }
  };

  const saveNodeChanges = async () => {
    if (!node || !editedNode) return;

    try {
      const response = await fetch(`http://localhost:5002/storage-endpoints/nodes/${node.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(editedNode)
      });

      if (response.ok) {
        const updatedData = await response.json();
        if (updatedData.node) {
          setNode(updatedData.node);
          setEditMode(false);
          
          // Track edit
          if (user?.id) {
            trackInteraction(node.id, 'edit-node', {
              description: `User edited node: ${node.title}`,
              changes: Object.keys(editedNode).length
            });
          }
        }
      }
    } catch (err) {
      console.error('Error saving node changes:', err);
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

  const formatJsonContent = (jsonStr: string): string => {
    try {
      return JSON.stringify(JSON.parse(jsonStr), null, 2);
    } catch {
      return jsonStr;
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="max-w-6xl mx-auto px-4 py-8">
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-500">Loading node...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error || !node) {
    return (
      <div className="min-h-screen bg-gray-50">
        <Navigation />
        <div className="max-w-6xl mx-auto px-4 py-8">
          <div className="text-center py-12">
            <div className="text-6xl mb-4">❌</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Node Not Found</h2>
            <p className="text-gray-600 mb-4">{error || 'The requested node could not be found.'}</p>
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
    <div className="min-h-screen bg-gray-50">
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
                  <span className="text-3xl">{getNodeTypeIcon(node.typeId)}</span>
                  <h1 className="text-3xl font-bold text-gray-900">
                    {editMode ? (
                      <input
                        type="text"
                        value={editedNode.title || ''}
                        onChange={(e) => setEditedNode({...editedNode, title: e.target.value})}
                        className="bg-white border border-gray-300 rounded px-3 py-1 text-3xl font-bold"
                      />
                    ) : (
                      node.title
                    )}
                  </h1>
                </div>
                <div className="flex items-center space-x-4 text-sm text-gray-600">
                  <span className={`px-2 py-1 bg-${getStateColor(node.state)}-100 text-${getStateColor(node.state)}-800 rounded-md font-medium`}>
                    {node.state}
                  </span>
                  <span>Type: {node.typeId}</span>
                  <span>ID: {node.id}</span>
                </div>
              </div>
            </div>
            
            <div className="flex items-center space-x-3">
              {editMode ? (
                <>
                  <button
                    onClick={saveNodeChanges}
                    className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
                  >
                    💾 Save
                  </button>
                  <button
                    onClick={() => {
                      setEditMode(false);
                      setEditedNode(node);
                    }}
                    className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
                  >
                    Cancel
                  </button>
                </>
              ) : (
                <button
                  onClick={() => setEditMode(true)}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                >
                  ✏️ Edit
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="bg-white rounded-lg border border-gray-200 mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex space-x-8 px-6">
              {[
                { id: 'details', label: 'Details', icon: '📋' },
                { id: 'content', label: 'Content', icon: '📄' },
                { id: 'relationships', label: 'Relationships', icon: '🔗' },
                { id: 'metadata', label: 'Metadata', icon: '🏷️' }
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  {tab.icon} {tab.label}
                </button>
              ))}
            </nav>
          </div>

          <div className="p-6">
            {/* Details Tab */}
            {activeTab === 'details' && (
              <div className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-4">Basic Information</h3>
                    <div className="space-y-3">
                      <div>
                        <label className="block text-sm font-medium text-gray-700">Title</label>
                        {editMode ? (
                          <input
                            type="text"
                            value={editedNode.title || ''}
                            onChange={(e) => setEditedNode({...editedNode, title: e.target.value})}
                            className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                          />
                        ) : (
                          <p className="mt-1 text-gray-900">{node.title}</p>
                        )}
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700">Description</label>
                        {editMode ? (
                          <textarea
                            value={editedNode.description || ''}
                            onChange={(e) => setEditedNode({...editedNode, description: e.target.value})}
                            rows={3}
                            className="mt-1 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                          />
                        ) : (
                          <p className="mt-1 text-gray-600">{node.description || 'No description available'}</p>
                        )}
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700">Type</label>
                        <p className="mt-1 text-gray-900">{node.typeId}</p>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700">State</label>
                        <span className={`inline-block mt-1 px-3 py-1 bg-${getStateColor(node.state)}-100 text-${getStateColor(node.state)}-800 rounded-md text-sm font-medium`}>
                          {node.state}
                        </span>
                      </div>
                    </div>
                  </div>
                  
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-4">System Information</h3>
                    <div className="space-y-3">
                      <div>
                        <label className="block text-sm font-medium text-gray-700">Node ID</label>
                        <p className="mt-1 text-gray-900 font-mono text-sm">{node.id}</p>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700">Locale</label>
                        <p className="mt-1 text-gray-900">{node.locale}</p>
                      </div>
                      
                      {node.createdAt && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700">Created</label>
                          <p className="mt-1 text-gray-600 text-sm">
                            {new Date(node.createdAt).toLocaleString()}
                          </p>
                        </div>
                      )}
                      
                      {node.updatedAt && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700">Updated</label>
                          <p className="mt-1 text-gray-600 text-sm">
                            {new Date(node.updatedAt).toLocaleString()}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Content Tab */}
            {activeTab === 'content' && (
              <div className="space-y-6">
                <h3 className="text-lg font-semibold text-gray-900">Content</h3>
                
                {node.content ? (
                  <div className="space-y-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Media Type</label>
                      <span className="px-3 py-1 bg-gray-100 text-gray-800 rounded-md text-sm font-mono">
                        {node.content.mediaType}
                      </span>
                    </div>
                    
                    {node.content.inlineJson && (
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">JSON Content</label>
                        <pre className="bg-gray-100 p-4 rounded-md overflow-x-auto text-sm font-mono">
                          {formatJsonContent(node.content.inlineJson)}
                        </pre>
                      </div>
                    )}
                    
                    {node.content.externalUri && (
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">External URI</label>
                        <a
                          href={node.content.externalUri}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:text-blue-800 underline"
                        >
                          {node.content.externalUri}
                        </a>
                      </div>
                    )}
                    
                    {node.content.inlineBytes && (
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Binary Content</label>
                        <p className="text-gray-600 text-sm">
                          Binary content available ({node.content.inlineBytes.length} bytes)
                        </p>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    <div className="text-4xl mb-2">📄</div>
                    <p>No content available for this node</p>
                  </div>
                )}
              </div>
            )}

            {/* Relationships Tab */}
            {activeTab === 'relationships' && (
              <div className="space-y-6">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-semibold text-gray-900">
                    Relationships ({relatedNodes.length})
                  </h3>
                </div>
                
                {relatedNodes.length > 0 ? (
                  <div className="space-y-4">
                    {relatedNodes.map((related, index) => (
                      <div key={index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50">
                        <div className="flex items-start justify-between">
                          <div className="flex items-start space-x-3">
                            <span className="text-2xl">{getNodeTypeIcon(related.node.typeId)}</span>
                            <div>
                              <h4 className="font-medium text-gray-900 mb-1">
                                <button
                                  onClick={() => router.push(`/node/${related.node.id}`)}
                                  className="hover:text-blue-600 transition-colors"
                                >
                                  {related.node.title}
                                </button>
                              </h4>
                              <p className="text-sm text-gray-600 mb-2">
                                {related.node.description}
                              </p>
                              <div className="flex items-center space-x-3 text-xs text-gray-500">
                                <span>Type: {related.node.typeId}</span>
                                <span>State: {related.node.state}</span>
                              </div>
                            </div>
                          </div>
                          
                          <div className="text-right">
                            <div className="flex items-center space-x-2 mb-1">
                              <span className={`px-2 py-1 rounded-md text-xs font-medium ${
                                related.direction === 'outgoing' 
                                  ? 'bg-blue-100 text-blue-800' 
                                  : 'bg-green-100 text-green-800'
                              }`}>
                                {related.direction === 'outgoing' ? '→' : '←'} {related.relationship}
                              </span>
                            </div>
                            {related.weight !== undefined && (
                              <div className="text-xs text-gray-500">
                                Weight: {related.weight}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    <div className="text-4xl mb-2">🔗</div>
                    <p>No relationships found for this node</p>
                  </div>
                )}
              </div>
            )}

            {/* Metadata Tab */}
            {activeTab === 'metadata' && (
              <div className="space-y-6">
                <h3 className="text-lg font-semibold text-gray-900">Metadata</h3>
                
                {node.meta && Object.keys(node.meta).length > 0 ? (
                  <div className="space-y-4">
                    {Object.entries(node.meta).map(([key, value]) => (
                      <div key={key} className="border-b border-gray-200 pb-3">
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          {key}
                        </label>
                        <div className="text-gray-900">
                          {typeof value === 'object' ? (
                            <pre className="bg-gray-100 p-3 rounded-md text-sm font-mono overflow-x-auto">
                              {JSON.stringify(value, null, 2)}
                            </pre>
                          ) : (
                            <p className="text-sm">{String(value)}</p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    <div className="text-4xl mb-2">🏷️</div>
                    <p>No metadata available for this node</p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Actions</h3>
          <div className="flex flex-wrap gap-3">
            <button
              onClick={() => router.push(`/graph?selectedNode=${node.id}`)}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              🕸️ View in Graph
            </button>
            
            <button
              onClick={() => {
                navigator.clipboard.writeText(node.id);
              }}
              className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors"
            >
              📋 Copy ID
            </button>
            
            <button
              onClick={() => {
                const nodeJson = JSON.stringify(node, null, 2);
                const blob = new Blob([nodeJson], { type: 'application/json' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `node-${node.id}.json`;
                a.click();
                URL.revokeObjectURL(url);
              }}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors"
            >
              💾 Export JSON
            </button>
            
            {relatedNodes.length > 0 && (
              <button
                onClick={() => {
                  const randomRelated = relatedNodes[Math.floor(Math.random() * relatedNodes.length)];
                  router.push(`/node/${randomRelated.node.id}`);
                }}
                className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors"
              >
                🎲 Random Related
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
