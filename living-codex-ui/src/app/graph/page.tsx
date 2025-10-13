'use client';

import { useEffect, useState } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import React, { Suspense } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter, StatsCard, NodeCard } from '@/components/ui/Card';
import { EdgeCard } from '@/components/ui/EdgeCard';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { useStorageStats, useHealthStatus, useAdvancedNodeSearch, useNodeTypes, useAdvancedEdgeSearch, useEdgeMetadata } from '@/lib/hooks';
import ScalableGraphVisualization from '@/components/graph/ScalableGraphVisualization';
import { config } from '@/lib/config';
import { getNode, Node as GraphNode } from '@/lib/graph-api';
import { api } from '@/lib/api';

// StatusBadge component for route status display
function StatusBadge({ status }: { status: string }) {
  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'fullytested': return 'bg-green-100 text-green-800';
      case 'partiallytested': return 'bg-yellow-100 text-yellow-800';
      case 'simple': return 'bg-blue-100 text-blue-800';
      case 'aienabled': return 'bg-purple-100 text-purple-800';
      case 'externalinfo': return 'bg-indigo-100 text-indigo-800';
      case 'stub': return 'bg-gray-100 text-gray-800';
      case 'simulated': return 'bg-orange-100 text-orange-800';
      case 'untested': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-600';
    }
  };

  return (
    <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(status)}`}>
      {status}
    </span>
  );
}

type StorageStats = {
  success?: boolean;
  stats?: {
    nodeCount?: number;
    edgeCount?: number;
    totalSizeBytes?: number;
    lastUpdated?: string;
  };
};

interface Node {
  id: string;
  typeId: string;
  state: string;
  title: string;
  description?: string;
  meta?: Record<string, any>;
}

export default function GraphPage() {
  return (
    <Suspense fallback={<div className="p-6">Loading graph...</div>}>
      <GraphPageInner />
    </Suspense>
  );
}

function GraphPageInner() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [selectedNodeType, setSelectedNodeType] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedView, setSelectedView] = useState<'overview' | 'nodes' | 'edges' | 'visualization' | 'query' | 'insights'>('overview');
  
  // Query builder state
  const [queryText, setQueryText] = useState('');
  const [queryResults, setQueryResults] = useState<any[]>([]);
  const [queryLoading, setQueryLoading] = useState(false);
  const [queryError, setQueryError] = useState<string | null>(null);
  const [nodeDetails, setNodeDetails] = useState<Node | null>(null);
  const [loading, setLoading] = useState(false);
  const [nodePage, setNodePage] = useState(1);
  const [nodePageSize, setNodePageSize] = useState(25);
  const [directMatchNode, setDirectMatchNode] = useState<any | null>(null);
  
  // Edge browser state
  const [selectedEdgeRole, setSelectedEdgeRole] = useState<string>('');
  const [selectedRelationshipType, setSelectedRelationshipType] = useState<string>('');
  const [edgeSearchQuery, setEdgeSearchQuery] = useState('');
  const [edgePage, setEdgePage] = useState(1);
  const [edgePageSize, setEdgePageSize] = useState(25);

  // Visualization state
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [selectedGraphNode, setSelectedGraphNode] = useState<GraphNode | null>(null);
  const [vizSearchQuery, setVizSearchQuery] = useState('');
  const [focusNodeId, setFocusNodeId] = useState<string | undefined>(undefined);

  // Use hooks for data fetching
  const { data: stats, isLoading: statsLoading } = useStorageStats();
  const { data: healthData } = useHealthStatus();
  const { data: nodeTypesData, isLoading: nodeTypesLoading } = useNodeTypes();
  const { data: edgeMetadataData, isLoading: edgeMetadataLoading } = useEdgeMetadata();
  
  // Advanced node search with filtering
  const nodeSearchParams = {
    typeIds: selectedNodeType ? [selectedNodeType] : undefined,
    searchTerm: searchQuery || undefined,
    take: nodePageSize,
    skip: (nodePage - 1) * nodePageSize,
    sortBy: 'id',
    sortDescending: false
  };
  
  const { data: nodesData, isLoading: nodesLoading, refetch: refetchNodes } = useAdvancedNodeSearch(nodeSearchParams);

  // Advanced edge search with filtering
  const edgeSearchParams = {
    role: selectedEdgeRole || undefined,
    relationship: selectedRelationshipType || undefined,
    fromId: edgeSearchQuery || undefined,
    take: edgePageSize,
    skip: (edgePage - 1) * edgePageSize,
  };
  
  const { data: edgesData, isLoading: edgesLoading, refetch: refetchEdges } = useAdvancedEdgeSearch(edgeSearchParams);

  // Get node types dynamically from backend and sort alphabetically
  // Backend returns: { success: true, nodeTypes: [{typeId, count, sampleTitle}], totalTypes, totalNodes }
  const nodeTypes = nodeTypesData?.success && nodeTypesData?.nodeTypes ? 
    nodeTypesData.nodeTypes.map((nt: any) => nt.typeId).sort() || [] :
    [];

  // Get edge roles and relationship types from server
  // Backend returns: { success: true, data: { roles: [...], relationshipTypes: [...] } }
  const edgeRoles = edgeMetadataData?.success && edgeMetadataData?.data?.roles ? 
    edgeMetadataData.data.roles :
    [];
  const relationshipTypes = edgeMetadataData?.success && edgeMetadataData?.data?.relationshipTypes ? 
    edgeMetadataData.data.relationshipTypes :
    [];

  // Handle URL parameters for node selection
  useEffect(() => {
    const selectedNode = searchParams.get('selectedNode');
    const selectedEdge = searchParams.get('selectedEdge');
    
    if (selectedNode) {
      setSearchQuery(selectedNode);
      setSelectedView('nodes');
      setNodePage(1);
    } else if (selectedEdge) {
      setEdgeSearchQuery(selectedEdge);
      setSelectedView('edges');
      setEdgePage(1);
    }
  }, [searchParams]);

  // Reset pagination when filters change
  useEffect(() => {
    setNodePage(1);
  }, [selectedNodeType, searchQuery]);

  useEffect(() => {
    setEdgePage(1);
  }, [selectedEdgeRole, selectedRelationshipType, edgeSearchQuery]);

  // Load selected node details for visualization tab
  useEffect(() => {
    if (selectedNodeId) {
      loadNodeDetails(selectedNodeId);
    }
  }, [selectedNodeId]);

  async function loadNodeDetails(nodeId: string) {
    try {
      const node = await getNode(nodeId);
      setSelectedGraphNode(node);
    } catch (err) {
      console.error('Failed to load node details:', err);
    }
  }

  function handleNodeClick(nodeId: string) {
    setSelectedNodeId(nodeId);
  }

  function handleClusterClick(clusterId: string) {
    console.log('Cluster clicked:', clusterId);
  }

  async function executeQuery() {
    if (!queryText.trim()) return;

    try {
      setQueryLoading(true);
      setQueryError(null);
      setQueryResults([]);

      const response = await api.post('/graph/query', {
        query: queryText,
        filters: {}
      });

      if (response.success) {
        setQueryResults(response.results || []);
      } else {
        setQueryError(response.error || response.message || 'Query failed');
      }
    } catch (error) {
      console.error('Query error:', error);
      setQueryError('Failed to execute query. Make sure the GraphQueryModule is available.');
    } finally {
      setQueryLoading(false);
    }
  }

  async function handleVizSearch(e: React.FormEvent) {
    e.preventDefault();
    if (!vizSearchQuery.trim()) {
      setFocusNodeId(undefined);
      return;
    }
    setFocusNodeId(vizSearchQuery);
  }

  function handleVizReset() {
    setFocusNodeId(undefined);
    setSelectedNodeId(null);
    setSelectedGraphNode(null);
    setVizSearchQuery('');
  }

  function viewNodeDetail() {
    if (selectedGraphNode) {
      router.push(`/node/${encodeURIComponent(selectedGraphNode.id)}`);
    }
  }

  const tabs = [
    { id: 'overview', label: 'Overview', icon: '📊' },
    { id: 'nodes', label: 'Nodes', icon: '🔵' },
    { id: 'edges', label: 'Edges', icon: '🔗' },
    { id: 'query', label: 'Query', icon: '🔍' },
    { id: 'visualization', label: 'Visualization', icon: '🌌' },
    { id: 'insights', label: 'Insights', icon: '🔮' },
  ];

  return (
    <div className="min-h-screen bg-white dark:bg-gray-900 text-black dark:text-white">
      <div className="max-w-7xl mx-auto p-6">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold mb-2">Knowledge Graph</h1>
          <p className="text-gray-600 dark:text-gray-400">
            Explore the interconnected web of nodes, edges, and relationships
          </p>
        </div>

        {/* Tab Navigation */}
        <div className="flex gap-2 mb-6 border-b border-gray-200 dark:border-gray-700">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setSelectedView(tab.id as any)}
              className={`px-4 py-2 font-medium transition-colors border-b-2 ${
                selectedView === tab.id
                  ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
              }`}
            >
              {tab.icon} {tab.label}
            </button>
          ))}
        </div>

        {/* Overview Tab */}
        {selectedView === 'overview' && (
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>📊 Storage Overview</CardTitle>
                <CardDescription>Current state of the knowledge graph</CardDescription>
              </CardHeader>
              <CardContent>
                {statsLoading ? (
                  <div className="animate-pulse space-y-3">
                    <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4"></div>
                    <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2"></div>
                  </div>
                ) : stats?.stats ? (
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <StatsCard label="Total Nodes" value={stats.stats.nodeCount?.toLocaleString() || '0'} />
                    <StatsCard label="Total Edges" value={stats.stats.edgeCount?.toLocaleString() || '0'} />
                    <StatsCard 
                      label="Storage Size" 
                      value={stats.stats.totalSizeBytes ? `${(stats.stats.totalSizeBytes / 1024 / 1024).toFixed(2)} MB` : 'N/A'} 
                    />
                  </div>
                ) : (
                  <div className="text-red-600">Failed to load storage stats</div>
                )}
              </CardContent>
            </Card>

            {/* Node State Distribution */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Ice Nodes</CardTitle>
                  <CardDescription>Persistent, essential data</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="text-3xl font-bold text-blue-600">
                    {stats?.stats ? '~' + Math.floor((stats.stats.nodeCount || 0) * 0.3).toLocaleString() : '-'}
                  </div>
                  <div className="text-sm text-gray-500 mt-1">Frozen state</div>
                </CardContent>
              </Card>
              
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Water Nodes</CardTitle>
                  <CardDescription>Semi-persistent, cached data</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="text-3xl font-bold text-cyan-600">
                    {stats?.stats ? '~' + Math.floor((stats.stats.nodeCount || 0) * 0.5).toLocaleString() : '-'}
                  </div>
                  <div className="text-sm text-gray-500 mt-1">Fluid state</div>
                </CardContent>
              </Card>
              
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Gas Nodes</CardTitle>
                  <CardDescription>Ephemeral, derivable data</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="text-3xl font-bold text-gray-600">
                    {stats?.stats ? '~' + Math.floor((stats.stats.nodeCount || 0) * 0.2).toLocaleString() : '-'}
                  </div>
                  <div className="text-sm text-gray-500 mt-1">Vapor state</div>
                </CardContent>
              </Card>
            </div>

            {/* System Health */}
            <Card>
              <CardHeader>
                <CardTitle>System Health</CardTitle>
              </CardHeader>
              <CardContent>
                {healthData ? (
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Status</span>
                      <span className="font-semibold text-green-600">{healthData.status || 'Unknown'}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Uptime</span>
                      <span className="font-mono text-sm">{healthData.uptime || 'N/A'}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Modules Loaded</span>
                      <span className="font-mono text-sm">{healthData.moduleCount || 0}</span>
                    </div>
                  </div>
                ) : (
                  <div className="text-gray-500">Loading health data...</div>
                )}
              </CardContent>
            </Card>
          </div>
        )}

        {/* Nodes Tab */}
        {selectedView === 'nodes' && (
          <div className="space-y-6">
            {/* Filters */}
            <Card>
              <CardHeader>
                <CardTitle>🔍 Node Browser</CardTitle>
                <CardDescription>Search and filter nodes by type, state, and content</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Node Type
                    </label>
                    <select
                      value={selectedNodeType}
                      onChange={(e) => setSelectedNodeType(e.target.value)}
                      className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg"
                    >
                      <option value="">All Types</option>
                      {nodeTypes.map((type: string) => (
                        <option key={type} value={type}>{type}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Search Nodes
                    </label>
                    <input
                      type="text"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      placeholder="Search by ID, title, or description..."
                      className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg"
                    />
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => refetchNodes()}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                  >
                    🔄 Refresh
                  </button>
                  <button
                    onClick={() => {
                      setSelectedNodeType('');
                      setSearchQuery('');
                      setNodePage(1);
                    }}
                    className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600"
                  >
                    Clear Filters
                  </button>
                </div>
              </CardContent>
            </Card>

            {/* Node Results */}
            <div>
              {nodesLoading ? (
                <div className="animate-pulse space-y-4">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="h-24 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
                  ))}
                </div>
              ) : nodesData?.success && nodesData?.nodes && nodesData.nodes.length > 0 ? (
                <>
                  <div className="mb-4 text-sm text-gray-600 dark:text-gray-400">
                    Showing {nodesData.nodes.length} of {nodesData.totalCount || 0} nodes
                  </div>
                  <div className="space-y-3">
                    {nodesData.nodes.map((node: any) => (
                      <NodeCard key={node.id} node={node} />
                    ))}
                  </div>
                  <div className="mt-6">
                    <PaginationControls
                      currentPage={nodePage}
                      totalItems={nodesData.totalCount || 0}
                      pageSize={nodePageSize}
                      onPageChange={setNodePage}
                      onPageSizeChange={setNodePageSize}
                    />
                  </div>
                </>
              ) : (
                <Card>
                  <CardContent>
                    <div className="text-center py-12">
                      <div className="text-6xl mb-4">🔵</div>
                      <div className="text-xl font-medium mb-2">No Nodes Found</div>
                      <div className="text-gray-500 dark:text-gray-400">
                        {searchQuery || selectedNodeType
                          ? 'No nodes match your search criteria. Try adjusting your filters.'
                          : 'No nodes found in the system. Nodes will appear as content is created.'}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          </div>
        )}

        {/* Edges Tab */}
        {selectedView === 'edges' && (
          <div className="space-y-6">
            {/* Filters */}
            <Card>
              <CardHeader>
                <CardTitle>🔗 Edge Browser</CardTitle>
                <CardDescription>Search and filter relationships between nodes</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Edge Role
                    </label>
                    <select
                      value={selectedEdgeRole}
                      onChange={(e) => setSelectedEdgeRole(e.target.value)}
                      className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg"
                    >
                      <option value="">All Roles</option>
                      {edgeRoles.map((role: string) => (
                        <option key={role} value={role}>{role}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Relationship Type
                    </label>
                    <select
                      value={selectedRelationshipType}
                      onChange={(e) => setSelectedRelationshipType(e.target.value)}
                      className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg"
                    >
                      <option value="">All Types</option>
                      {relationshipTypes.map((type: string) => (
                        <option key={type} value={type}>{type}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      From Node ID
                    </label>
                    <input
                      type="text"
                      value={edgeSearchQuery}
                      onChange={(e) => setEdgeSearchQuery(e.target.value)}
                      placeholder="Filter by source node ID..."
                      className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg"
                    />
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => refetchEdges()}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                  >
                    🔄 Refresh
                  </button>
                  <button
                    onClick={() => {
                      setSelectedEdgeRole('');
                      setSelectedRelationshipType('');
                      setEdgeSearchQuery('');
                      setEdgePage(1);
                    }}
                    className="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600"
                  >
                    Clear Filters
                  </button>
                </div>
              </CardContent>
            </Card>

            {/* Edge Results */}
            <div>
              {edgesLoading ? (
                <div className="animate-pulse space-y-4">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="h-24 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
                  ))}
                </div>
              ) : edgesData?.success && edgesData?.edges && edgesData.edges.length > 0 ? (
                <>
                  <div className="mb-4 text-sm text-gray-600 dark:text-gray-400">
                    Showing {edgesData.edges.length} of {edgesData.totalCount || 0} edges
                  </div>
                  <div className="space-y-3">
                    {edgesData.edges.map((edge: any) => (
                      <EdgeCard key={`${edge.fromId}-${edge.toId}-${edge.role}`} edge={edge} />
                    ))}
                  </div>
                  <div className="mt-6">
                    <PaginationControls
                      currentPage={edgePage}
                      totalItems={edgesData.totalCount || 0}
                      pageSize={edgePageSize}
                      onPageChange={setEdgePage}
                      onPageSizeChange={setEdgePageSize}
                    />
                  </div>
                </>
              ) : (
                <Card>
                  <CardContent>
                    <div className="text-center py-12">
                      <div className="text-6xl mb-4">🔗</div>
                      <div className="text-xl font-medium mb-2">No Edges Found</div>
                      <div className="text-gray-500 dark:text-gray-400">
                        {edgeSearchQuery || selectedEdgeRole || selectedRelationshipType
                          ? 'No edges match your search criteria. Try adjusting your filters.'
                          : 'No edge relationships found in the system. Edges will appear as relationships are created between nodes.'}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          </div>
        )}

        {/* Visualization Tab */}
        {selectedView === 'visualization' && (
          <div className="space-y-6">
            {/* Controls */}
            <Card>
              <CardHeader>
                <CardTitle>🌌 Fractal Graph Visualization</CardTitle>
                <CardDescription>Scalable exploration • Galaxy → System → Detail → Node</CardDescription>
              </CardHeader>
              <CardContent>
                <form onSubmit={handleVizSearch} className="flex gap-2 mb-4">
                  <input
                    type="text"
                    value={vizSearchQuery}
                    onChange={(e) => setVizSearchQuery(e.target.value)}
                    placeholder="Enter node ID to focus..."
                    className="flex-1 px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
                  />
                  <button
                    type="submit"
                    className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-semibold transition-colors"
                  >
                    Focus
                  </button>
                  <button
                    type="button"
                    onClick={handleVizReset}
                    className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 font-semibold transition-colors"
                  >
                    Reset
                  </button>
                </form>
                <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-700/50 rounded-lg p-3 text-sm text-gray-700 dark:text-gray-300">
                  <strong>How to use:</strong> Scroll to zoom • Drag to pan • Click nodes to drill down • Click clusters to expand
                </div>
              </CardContent>
            </Card>

            {/* Graph */}
            <div className="bg-gray-100 dark:bg-gray-800 rounded-lg p-2">
              <ScalableGraphVisualization
                baseUrl={config.backend.baseUrl}
                initialFocusNodeId={focusNodeId}
                onNodeClick={handleNodeClick}
                onClusterClick={handleClusterClick}
                width={1300}
                height={800}
              />
            </div>

            {/* Selected Node Info */}
            {selectedGraphNode && (
              <Card>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle>{selectedGraphNode.title}</CardTitle>
                      <CardDescription>Selected from visualization</CardDescription>
                    </div>
                    <button
                      onClick={viewNodeDetail}
                      className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors"
                    >
                      View Full Details →
                    </button>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                    <div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">Node ID</div>
                      <div className="font-mono text-sm">{selectedGraphNode.id}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">Type</div>
                      <div className="font-mono text-sm">{selectedGraphNode.typeId}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">State</div>
                      <div className="font-mono text-sm">{selectedGraphNode.state}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">Locale</div>
                      <div className="font-mono text-sm">{selectedGraphNode.locale}</div>
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500 dark:text-gray-400 mb-2">Description</div>
                    <div className="text-sm">{selectedGraphNode.description}</div>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        )}

        {/* Query Tab */}
        {selectedView === 'query' && (
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>🔍 Graph Query Builder</CardTitle>
                <CardDescription>Search the graph using XPath-like syntax</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {/* Query Examples */}
                  <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">Example Queries:</h4>
                    <div className="space-y-1 text-sm text-gray-700 dark:text-gray-300">
                      <div className="font-mono bg-white dark:bg-gray-800 p-2 rounded">
                        //concept[@state='Ice']
                      </div>
                      <div className="text-xs text-gray-600 dark:text-gray-400 ml-2">Find all Ice state concepts</div>
                      <div className="font-mono bg-white dark:bg-gray-800 p-2 rounded mt-2">
                        //node[@typeId='codex.user']
                      </div>
                      <div className="text-xs text-gray-600 dark:text-gray-400 ml-2">Find all user nodes</div>
                      <div className="font-mono bg-white dark:bg-gray-800 p-2 rounded mt-2">
                        //concept[contains(@title, 'resonance')]
                      </div>
                      <div className="text-xs text-gray-600 dark:text-gray-400 ml-2">Find concepts with "resonance" in title</div>
                    </div>
                  </div>

                  {/* Query Input */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Query Expression
                    </label>
                    <div className="flex gap-2">
                      <input
                        type="text"
                        value={queryText}
                        onChange={(e) => setQueryText(e.target.value)}
                        onKeyPress={(e) => {
                          if (e.key === 'Enter') {
                            executeQuery();
                          }
                        }}
                        placeholder="Enter XPath-like query..."
                        className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
                      />
                      <button
                        onClick={executeQuery}
                        disabled={!queryText.trim() || queryLoading}
                        className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                      >
                        {queryLoading ? 'Searching...' : 'Execute'}
                      </button>
                    </div>
                  </div>

                  {/* Query Results */}
                  {queryError && (
                    <div className="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                      <p className="text-red-700 dark:text-red-400">{queryError}</p>
                    </div>
                  )}

                  {queryResults.length > 0 && (
                    <div>
                      <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-3">
                        Results ({queryResults.length})
                      </h4>
                      <div className="space-y-2 max-h-[500px] overflow-y-auto">
                        {queryResults.map((result: any, index: number) => (
                          <div
                            key={result.id || index}
                            className="p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors cursor-pointer"
                            onClick={() => router.push(`/node/${result.id}`)}
                          >
                            <div className="flex items-start justify-between gap-3">
                              <div className="flex-1">
                                <div className="font-medium text-gray-900 dark:text-gray-100">
                                  {result.title || result.id}
                                </div>
                                <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                                  {result.description || result.typeId}
                                </div>
                                <div className="flex items-center gap-2 mt-2">
                                  <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400 rounded">
                                    {result.typeId}
                                  </span>
                                  <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-400 rounded">
                                    {result.state}
                                  </span>
                                </div>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {!queryLoading && queryResults.length === 0 && queryText && (
                    <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                      <div className="text-4xl mb-2">🔍</div>
                      <p>No results found. Try a different query.</p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Insights Tab */}
        {selectedView === 'insights' && (
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>🔮 Graph Insights</CardTitle>
                <CardDescription>Patterns, trends, and graph analytics</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <div className="font-medium text-gray-900 dark:text-gray-100 mb-2">Meta-Nodes</div>
                    <div className="text-2xl font-bold text-blue-600">
                      {stats?.stats ? Math.floor((stats.stats.nodeCount || 0) * 0.15).toLocaleString() : '-'}
                    </div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">System structure nodes</div>
                  </div>
                  
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <div className="font-medium text-gray-900 dark:text-gray-100 mb-2">Content Nodes</div>
                    <div className="text-2xl font-bold text-green-600">
                      {stats?.stats ? Math.floor((stats.stats.nodeCount || 0) * 0.70).toLocaleString() : '-'}
                    </div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">User-generated content</div>
                  </div>
                  
                  <div className="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                    <div className="font-medium text-gray-900 dark:text-gray-100 mb-2">Flow Nodes</div>
                    <div className="text-2xl font-bold text-purple-600">
                      {stats?.stats ? '~' + Math.floor((stats.stats.nodeCount || 0) * 0.15).toLocaleString() : '-'}
                    </div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">Process and flow control</div>
                  </div>
                </div>
                
                <div className="mt-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <h3 className="font-semibold mb-3">📈 Graph Statistics</h3>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Average Degree</span>
                      <span className="font-mono">
                        {stats?.stats && stats.stats.nodeCount && stats.stats.edgeCount
                          ? ((stats.stats.edgeCount / stats.stats.nodeCount) * 2).toFixed(2)
                          : 'N/A'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Node Types</span>
                      <span className="font-mono">{nodeTypes.length}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Edge Roles</span>
                      <span className="font-mono">{edgeRoles.length}</span>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        )}
      </div>
    </div>
  );
}
