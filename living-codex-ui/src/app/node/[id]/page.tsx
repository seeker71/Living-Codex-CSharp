'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { ContentRenderer } from '@/components/renderers/ContentRenderer';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

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
  const [edgesTotal, setEdgesTotal] = useState(0);
  const [edgesPage, setEdgesPage] = useState(1);
  const [edgesPageSize] = useState(25);
  const [edgeRoleFilter, setEdgeRoleFilter] = useState('');
  const [edgeSearch, setEdgeSearch] = useState('');
  
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
      let resolvedNodeId: string | null = nodeId;
      let nodeResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${encodeURIComponent(nodeId)}`));

      // Fallback: resolve file:* shorthand to actual file node id by listing files
      if (!nodeResponse.ok && nodeId.startsWith('file:')) {
        const path = nodeId.slice('file:'.length);
        try {
          const filesResp = await fetch(buildApiUrl(`/filesystem/files?limit=5000`));
          if (filesResp.ok) {
            const filesData = await filesResp.json();
            const files: any[] = filesData.files || filesData.nodes || [];
            const match = files.find(f => f.meta?.relativePath === path || f.meta?.fileName === path || f.title === path);
            if (match?.id) {
              resolvedNodeId = match.id;
              nodeResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${encodeURIComponent(resolvedNodeId!)}`));
            }
          }
        } catch {
          // ignore and let the original error surface
        }
      }

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
      // Load edges involving this node (server-side pagination and filters if supported)
      const params = new URLSearchParams();
      params.set('nodeId', nodeId);
      params.set('skip', String((edgesPage - 1) * edgesPageSize));
      params.set('take', String(edgesPageSize));
      if (edgeRoleFilter) params.set('role', edgeRoleFilter);
      if (edgeSearch) params.set('searchTerm', edgeSearch);
      const edgesResponse = await fetch(buildApiUrl(`/storage-endpoints/edges?${params.toString()}`));
      if (edgesResponse.ok) {
        const edgesData = await edgesResponse.json();
        if (edgesData.edges) {
          // Ensure only edges directly connected to this node
          const onlyConnected = edgesData.edges.filter((e: EdgeData) => e.fromId === nodeId || e.toId === nodeId);
          setEdges(onlyConnected);
          if (typeof edgesData.totalCount === 'number') {
            setEdgesTotal(edgesData.totalCount);
          } else {
            setEdgesTotal(onlyConnected.length);
          }
          
          // Load related nodes
          const relatedNodeIds = new Set<string>();
          onlyConnected.forEach((edge: EdgeData) => {
            if (edge.fromId === nodeId) relatedNodeIds.add(edge.toId);
            if (edge.toId === nodeId) relatedNodeIds.add(edge.fromId);
          });

          const related: RelatedNode[] = [];
          for (const relatedId of relatedNodeIds) {
            try {
              const relatedResponse = await fetch(buildApiUrl(`/storage-endpoints/nodes/${encodeURIComponent(relatedId)}`));
              if (relatedResponse.ok) {
                const relatedData = await relatedResponse.json();
                if (relatedData.node) {
                  const edge = edgesData.edges.find((e: any) => 
                    (e.fromId === nodeId && e.toId === relatedId) || 
                    (e.fromId === relatedId && e.toId === nodeId)
                  );
                  
                  related.push({
                    node: relatedData.node,
                    relationship: (edge?.meta?.relationship) || edge?.role || 'related',
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

  // Note: Edge creation is enforced server-side on node creation; no UI fallback here

  const saveNodeChanges = async () => {
    if (!node || !editedNode) return;

    try {
      const response = await fetch(buildApiUrl(`/storage-endpoints/nodes/${node.id}`), {
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
      'ontology.axis': '🌟',
      'concept': '💡',
      'user': '👤',
      'news': '📰',
      'contribution': '🎯',
      'module': '📦'
    };
    return iconMap[typeId] || '🔵';
  };

  const openTypeMetaNode = (typeId: string) => {
    // Many typeIds are logical like codex.meta/module; attempt resolution:
    const tryIds = [
      typeId,
      typeId.replace('.', '/'),
      typeId.replace(':', '/'),
      typeId.includes('/') ? typeId : typeId.replace('.', '/'),
    ].filter((v, i, a) => !!v && a.indexOf(v) === i);

    // Try each candidate until one loads
    (async () => {
      for (const candidate of tryIds) {
        const resp = await fetch(buildApiUrl(`/storage-endpoints/nodes/${encodeURIComponent(candidate)}`));
        if (resp.ok) {
          router.push(`/node/${encodeURIComponent(candidate)}`);
          return;
        }
      }
      // Fallback: open raw
      router.push(`/node/${encodeURIComponent(typeId)}`);
    })();
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
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
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
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
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
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
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
                  <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
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
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Title</label>
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
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Description</label>
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
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Type</label>
                        <button
                          onClick={() => openTypeMetaNode(node.typeId)}
                          className="mt-1 text-blue-600 hover:text-blue-800 hover:underline"
                          title="Open type meta-node"
                        >
                          {node.typeId}
                        </button>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">State</label>
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
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Node ID</label>
                        <p className="mt-1 text-gray-900 font-mono text-sm">{node.id}</p>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Locale</label>
                        <p className="mt-1 text-gray-900">{node.locale}</p>
                      </div>
                      
                      {node.createdAt && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Created</label>
                          <p className="mt-1 text-gray-600 text-sm">
                            {new Date(node.createdAt).toLocaleString()}
                          </p>
                        </div>
                      )}
                      
                      {node.updatedAt && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Updated</label>
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
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Content</h3>
                
                {node.content ? (
                  <ContentRenderer content={node.content} nodeId={node.id} />
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
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                    Relationships ({relatedNodes.length})
                  </h3>
                </div>

                {/* Edge Filters */}
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                    <div>
                      <label className="block text-sm text-gray-600 dark:text-gray-300 mb-1">Role</label>
                      <input
                        type="text"
                        value={edgeRoleFilter}
                        onChange={(e) => setEdgeRoleFilter(e.target.value)}
                        placeholder="e.g. defines, references"
                        className="input-standard w-full"
                      />
                    </div>
                    <div>
                      <label className="block text-sm text-gray-600 dark:text-gray-300 mb-1">Search</label>
                      <input
                        type="text"
                        value={edgeSearch}
                        onChange={(e) => setEdgeSearch(e.target.value)}
                        placeholder="Search by related node id"
                        className="input-standard w-full"
                      />
                    </div>
                    <div className="flex items-end">
                      <button
                        onClick={() => loadNodeRelationships(node.id)}
                        className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                      >
                        Apply
                      </button>
                      <button
                        onClick={() => { setEdgeRoleFilter(''); setEdgeSearch(''); setEdgesPage(1); loadNodeRelationships(node.id); }}
                        className="ml-2 px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-100 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600"
                      >
                        Clear
                      </button>
                    </div>
                  </div>
                </div>
                
                {relatedNodes.length > 0 ? (
                  <div className="space-y-4">
                    {/* U-CORE Axes linked to this node */}
                    {(() => {
                      const ucoreAxes = relatedNodes.filter(r => (r.node.typeId || '').includes('codex.ontology.axis'));
                      if (ucoreAxes.length === 0) return null;
                      return (
                        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
                          <div className="flex items-center justify-between mb-3">
                            <h4 className="text-md font-semibold text-gray-900 dark:text-gray-100">U-CORE Axes</h4>
                            <button
                              onClick={() => router.push('/ontology')}
                              className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                            >
                              Open Ontology →
                            </button>
                          </div>
                          <div className="flex flex-wrap gap-2">
                            {ucoreAxes.map((rel, idx) => (
                              <button
                                key={`${rel.node.id}-${idx}`}
                                onClick={() => router.push(`/node/${rel.node.id}`)}
                                className="px-2 py-1 rounded-md text-xs bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-300 border border-purple-200 dark:border-purple-800 hover:bg-purple-200 dark:hover:bg-purple-900/50"
                              >
                                {rel.node.title || rel.node.id}
                              </button>
                            ))}
                          </div>
                        </div>
                      );
                    })()}
                    {relatedNodes.map((related, index) => (
                      <Card key={index} className="hover:shadow-md transition-shadow">
                        <CardContent className="p-4">
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
                                <button
                                  onClick={() => {
                                    const edgePath = related.direction === 'outgoing' 
                                      ? `/edge/${node.id}/${related.node.id}`
                                      : `/edge/${related.node.id}/${node.id}`;
                                    window.open(edgePath, '_blank');
                                  }}
                                  className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                                >
                                  View Edge
                                </button>
                              </div>
                              {related.weight !== undefined && (
                                <div className="text-xs text-gray-500">
                                  Weight: {related.weight}
                                </div>
                              )}
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    <div className="text-4xl mb-2">🔗</div>
                    <p>No relationships found for this node</p>
                  </div>
                )}

                {/* All Edges (incoming and outgoing) */}
                <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                  <h4 className="text-md font-semibold text-gray-900 dark:text-gray-100 mb-3">All Edges ({edges.length})</h4>
                  {edges.length > 0 ? (
                    <div className="overflow-x-auto">
                      <table className="min-w-full text-sm">
                        <thead className="bg-gray-50 dark:bg-gray-800/60">
                          <tr className="text-left text-gray-700 dark:text-gray-300">
                            <th className="px-3 py-2 font-medium">Direction</th>
                            <th className="px-3 py-2 font-medium">From</th>
                            <th className="px-3 py-2 font-medium">To</th>
                            <th className="px-3 py-2 font-medium">Role</th>
                            <th className="px-3 py-2 font-medium">Weight</th>
                            <th className="px-3 py-2 font-medium">Relationship</th>
                            <th className="px-3 py-2 font-medium">Actions</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                          {edges.map((e: any, idx) => {
                            const direction = e.fromId === node.id ? 'Outgoing' : (e.toId === node.id ? 'Incoming' : 'Connected');
                            return (
                              <tr key={idx} className="text-gray-900 dark:text-gray-100">
                                <td className="px-3 py-2 text-xs">
                                  <span className={`px-2 py-1 rounded ${direction === 'Outgoing' ? 'bg-blue-100 text-blue-800' : direction === 'Incoming' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                                    {direction}
                                  </span>
                                </td>
                                <td className="px-3 py-2 font-mono">
                                  <button onClick={() => router.push(`/node/${e.fromId}`)} className="text-blue-600 hover:underline">
                                    {e.fromId}
                                  </button>
                                </td>
                                <td className="px-3 py-2 font-mono">
                                  <button onClick={() => router.push(`/node/${e.toId}`)} className="text-blue-600 hover:underline">
                                    {e.toId}
                                  </button>
                                </td>
                                <td className="px-3 py-2">
                                  <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
                                    {e.role || e.meta?.relationship || 'related'}
                                  </span>
                                </td>
                                <td className="px-3 py-2 text-xs">{e.weight ?? '—'}</td>
                                <td className="px-3 py-2 text-xs">{e.meta?.relationship ?? '—'}</td>
                                <td className="px-3 py-2 text-xs">
                                  <button
                                    onClick={() => window.open(`/edge/${e.fromId}/${e.toId}`, '_blank')}
                                    className="text-blue-600 hover:text-blue-800"
                                  >
                                    View Edge →
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                      {/* Pagination */}
                      {edgesTotal > edgesPageSize && (
                        <div className="flex items-center justify-between py-3">
                          <div className="text-xs text-gray-500">Page {edgesPage} of {Math.ceil(edgesTotal / edgesPageSize)}</div>
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => { const p = Math.max(1, edgesPage - 1); setEdgesPage(p); loadNodeRelationships(node.id); }}
                              disabled={edgesPage === 1}
                              className="px-3 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50"
                            >
                              Previous
                            </button>
                            <button
                              onClick={() => { const p = Math.min(Math.ceil(edgesTotal / edgesPageSize), edgesPage + 1); setEdgesPage(p); loadNodeRelationships(node.id); }}
                              disabled={edgesPage >= Math.ceil(edgesTotal / edgesPageSize)}
                              className="px-3 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50"
                            >
                              Next
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                  ) : (
                    <div className="text-sm text-gray-500">No edges found.</div>
                  )}
                </div>
              </div>
            )}

            {/* Metadata Tab */}
            {activeTab === 'metadata' && (
              <div className="space-y-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Metadata</h3>
                
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
                            <p className="text-sm">
                              {renderMetaValueAsLinkIfNodeId(String(value), router)}
                            </p>
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
        <Card>
          <CardHeader>
            <CardTitle>Actions</CardTitle>
          </CardHeader>
          <CardContent>
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
                <>
                  <button
                    onClick={() => {
                      const randomRelated = relatedNodes[Math.floor(Math.random() * relatedNodes.length)];
                      router.push(`/node/${randomRelated.node.id}`);
                    }}
                    className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors"
                  >
                    🎲 Random Related
                  </button>
                  
                  <button
                    onClick={() => {
                      // Find nodes of the same type
                      const sameTypeNodes = relatedNodes.filter(r => r.node.typeId === node.typeId);
                      if (sameTypeNodes.length > 0) {
                        const randomSameType = sameTypeNodes[Math.floor(Math.random() * sameTypeNodes.length)];
                        router.push(`/node/${randomSameType.node.id}`);
                      }
                    }}
                    className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
                  >
                    🔗 Random Same Type
                  </button>
                </>
              )}
              
              {/* Meta-node navigation */}
              {node.typeId && node.typeId.startsWith('codex.meta/') && (
                <button
                  onClick={() => {
                    // Look for nodes that reference this meta-node
                    const metaEdges = edges.filter(e => e.toId === node.id && e.relationship === 'defines');
                    if (metaEdges.length > 0) {
                      const randomEdge = metaEdges[Math.floor(Math.random() * metaEdges.length)];
                      router.push(`/node/${randomEdge.fromId}`);
                    }
                  }}
                  className="px-4 py-2 bg-yellow-600 text-white rounded-md hover:bg-yellow-700 transition-colors"
                >
                  🔍 Find Defined Nodes
                </button>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function renderMetaValueAsLinkIfNodeId(value: string, router: any) {
  // Heuristic: treat values that look like node-ids as links
  const looksLikeNodeId = value.startsWith('codex.') || value.startsWith('u-core') || value.startsWith('codex.meta/') || value.includes('.')
  if (!looksLikeNodeId) {
    return value
  }
  return (
    <button
      onClick={() => router.push(`/node/${encodeURIComponent(value)}`)}
      className="text-blue-600 hover:text-blue-800 hover:underline"
    >
      {value}
    </button>
  )
}
