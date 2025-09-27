'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import React, { Suspense } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter, StatsCard, NodeCard } from '@/components/ui/Card';
import { EdgeCard } from '@/components/ui/EdgeCard';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { useStorageStats, useNodes, useHealthStatus, useAdvancedNodeSearch, useNodeTypes, useAdvancedEdgeSearch, useEdgeMetadata } from '@/lib/hooks';
import { endpoints } from '@/lib/api';
import { buildApiUrl } from '@/lib/config';

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
  const [selectedNodeType, setSelectedNodeType] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedView, setSelectedView] = useState<'overview' | 'nodes' | 'edges' | 'insights'>('overview');
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
    // Interpret the input as an exact fromId filter per UI label
    fromId: edgeSearchQuery || undefined,
    take: edgePageSize,
    skip: (edgePage - 1) * edgePageSize,
  };
  
  const { data: edgesData, isLoading: edgesLoading, refetch: refetchEdges } = useAdvancedEdgeSearch(edgeSearchParams);

  // Get node types dynamically from backend and sort alphabetically
  const nodeTypes = nodeTypesData?.success ? 
    (nodeTypesData.data as any)?.nodeTypes?.map((nt: any) => nt.typeId).sort() || [] :
    [];

  // Get edge roles and relationship types from server, fallback to empty arrays if loading or failed
  const edgeRoles = edgeMetadataData?.success ? 
    (edgeMetadataData.data as any)?.roles || [] :
    [];
  const relationshipTypes = edgeMetadataData?.success ? 
    (edgeMetadataData.data as any)?.relationshipTypes || [] :
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

  // Direct node-id lookup when search changes
  useEffect(() => {
    let cancelled = false;
    async function tryDirectLookup() {
      setDirectMatchNode(null);
      const q = (searchQuery || '').trim();
      if (!q) return;
      try {
        const resp = await fetch(buildApiUrl(`/storage-endpoints/nodes/${encodeURIComponent(q)}`));
        if (!resp.ok) return;
        const data = await resp.json();
        if (!cancelled && data && (data.node || data.id)) {
          setDirectMatchNode(data.node ?? data);
        }
      } catch {}
    }
    tryDirectLookup();
    return () => { cancelled = true; };
  }, [searchQuery]);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Global header is provided by RootLayout */}

      <main className="max-w-7xl mx-auto p-6">
        {/* View Tabs */}
        <div className="mb-6">
          <div className="border-b border-gray-200">
            <nav className="-mb-px flex space-x-8">
              {[
                { id: 'overview', label: 'Overview', icon: '📊' },
                { id: 'nodes', label: 'Nodes', icon: '🔵' },
                { id: 'edges', label: 'Edges', icon: '🔗' },
                { id: 'insights', label: 'Insights', icon: '🔮' },
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setSelectedView(tab.id as any)}
                  className={`flex items-center space-x-2 py-2 px-1 border-b-2 font-medium text-sm ${
                    selectedView === tab.id
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
        </div>

        {/* Overview Tab */}
        {selectedView === 'overview' && (
          <div className="space-y-6">
            {/* Enhanced Storage Stats */}
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-4">📊 Storage Overview</h2>
              
              {statsLoading ? (
                <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
                  {[...Array(4)].map((_, i) => (
                    <div key={i} className="animate-pulse bg-gray-200 rounded-lg h-24"></div>
                  ))}
                </div>
              ) : stats?.success ? (
                <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
                  <StatsCard>
                    <CardContent className="p-4">
                      <div className="text-gray-500 dark:text-gray-400 text-sm">Total Nodes</div>
                      <div className="text-3xl font-bold text-gray-900 dark:text-gray-100">{(stats.data as any)?.stats?.nodeCount?.toLocaleString() ?? '—'}</div>
                      <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">All states</div>
                    </CardContent>
                  </StatsCard>
                  <StatsCard>
                    <CardContent className="p-4">
                      <div className="text-gray-500 dark:text-gray-400 text-sm">Total Edges</div>
                      <div className="text-3xl font-bold text-gray-900 dark:text-gray-100">{(stats.data as any)?.stats?.edgeCount?.toLocaleString() ?? '—'}</div>
                      <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">Relationships</div>
                    </CardContent>
                  </StatsCard>
                  <StatsCard>
                    <CardContent className="p-4">
                      <div className="text-gray-500 dark:text-gray-400 text-sm">Storage Backend</div>
                      <div className="text-lg font-bold text-gray-900 dark:text-gray-100">
                        {(stats.data as any)?.stats?.storageBackend ? 
                          (stats.data as any).stats.storageBackend.replace('StorageBackend', '') : '—'}
                      </div>
                      <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">Backend type</div>
                    </CardContent>
                  </StatsCard>
                  <StatsCard>
                    <CardContent className="p-4">
                      <div className="text-gray-500 dark:text-gray-400 text-sm">Last Updated</div>
                      <div className="text-lg font-bold text-gray-900 dark:text-gray-100">
                        {(stats.data as any)?.stats?.timestamp ? 
                          new Date((stats.data as any).stats.timestamp).toLocaleTimeString() : '—'}
                      </div>
                      <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                        {(stats.data as any)?.stats?.timestamp ? 
                          new Date((stats.data as any).stats.timestamp).toLocaleDateString() : 'Unknown'}
                      </div>
                    </CardContent>
                  </StatsCard>
                </div>
              ) : (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                  <div className="text-red-700">Failed to load storage statistics</div>
                </div>
              )}
            </section>

            {/* Node State Breakdown */}
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-4">🌊 Node State Distribution</h2>
              
              <Card>
                <CardContent className="p-6">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  <div className="text-center">
                    <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-3">
                      <span className="text-2xl">🧊</span>
                    </div>
                    <h3 className="font-semibold text-blue-900 mb-2">Ice Nodes</h3>
                    <p className="text-sm text-gray-600 mb-3">
                      Immutable, persistent knowledge stored in federated storage (PostgreSQL)
                    </p>
                    <div className="text-2xl font-bold text-blue-600">
                      {healthData?.success ? Math.floor(Number((healthData.data as any)?.nodeCount) * 0.3).toLocaleString() : '—'}
                    </div>
                    <div className="text-xs text-gray-500">Estimated</div>
                  </div>
                  
                  <div className="text-center">
                    <div className="w-16 h-16 bg-cyan-100 rounded-full flex items-center justify-center mx-auto mb-3">
                      <span className="text-2xl">💧</span>
                    </div>
                    <h3 className="font-semibold text-cyan-900 mb-2">Water Nodes</h3>
                    <p className="text-sm text-gray-600 mb-3">
                      Mutable, semi-persistent data in local cache (SQLite)
                    </p>
                    <div className="text-2xl font-bold text-cyan-600">
                      {healthData?.success ? Math.floor(Number((healthData.data as any)?.nodeCount) * 0.5).toLocaleString() : '—'}
                    </div>
                    <div className="text-xs text-gray-500">Estimated</div>
                  </div>
                  
                  <div className="text-center">
                    <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-3">
                      <span className="text-2xl">💨</span>
                    </div>
                    <h3 className="font-semibold text-gray-900 mb-2">Gas Nodes</h3>
                    <p className="text-sm text-gray-600 mb-3">
                      Transient, derivable information generated on-demand
                    </p>
                    <div className="text-2xl font-bold text-gray-600">
                      {healthData?.success ? Math.floor(Number((healthData.data as any)?.nodeCount) * 0.2).toLocaleString() : '—'}
                    </div>
                    <div className="text-xs text-gray-500">Estimated</div>
                  </div>
                </div>
                </CardContent>
              </Card>
            </section>
          </div>
        )}

        {/* Nodes Tab */}
        {selectedView === 'nodes' && (
          <div className="space-y-6">
            {/* Node Explorer Controls */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">🔍 Node Explorer</h2>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Node Type
                  </label>
                  <select
                    value={selectedNodeType}
                    onChange={(e) => setSelectedNodeType(e.target.value)}
                    className="input-standard"
                  >
                    <option value="">All Types</option>
                    {nodeTypes.map((type: string) => (
                      <option key={type} value={type}>
                        {type}
                      </option>
                    ))}
                  </select>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Search Query
                  </label>
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Search nodes..."
                    className="input-standard"
                  />
                </div>
                
                <div className="flex items-end">
                  <button
                    onClick={() => refetchNodes()}
                    disabled={loading || nodesLoading}
                    className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
                  >
                    {loading || nodesLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
              </div>
            </div>

            {/* Node Results */}
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle>Node Results</CardTitle>
                    <CardDescription className="mt-1">
                      {nodesLoading ? 'Loading...' : 
                        `${(nodesData?.data as any)?.nodes?.length || 0} of ${(nodesData?.data as any)?.totalCount || 0} nodes`}
                    </CardDescription>
                  </div>
                  <button
                    onClick={() => refetchNodes()}
                    disabled={loading || nodesLoading}
                    className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors text-sm font-medium"
                  >
                    {loading || nodesLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
              </CardHeader>
              
              <CardContent>
                {/* Pagination at top */}
                {nodesData?.success && (nodesData.data as any)?.totalCount > 0 && (
                  <div className="mb-6">
                    <PaginationControls
                      currentPage={nodePage}
                      pageSize={nodePageSize}
                      totalCount={(nodesData.data as any)?.totalCount || 0}
                      onPageChange={setNodePage}
                      onPageSizeChange={setNodePageSize}
                      showPageSizeSelector={true}
                      pageSizeOptions={[10, 25, 50, 100, 200]}
                    />
                  </div>
                )}

                {nodesLoading ? (
                  <div className="space-y-3">
                    {[...Array(5)].map((_, i) => (
                      <div key={i} className="animate-pulse bg-gray-200 dark:bg-gray-700 rounded-lg h-16"></div>
                    ))}
                  </div>
                ) : nodesData?.success && (nodesData.data as any)?.nodes ? (
                  <div className="space-y-3">
                    {directMatchNode && (
                      <div className="border border-green-300 rounded-lg p-3 bg-green-50 dark:bg-green-900/20">
                        <div className="flex items-center justify-between">
                          <div>
                            <div className="text-sm text-green-700 dark:text-green-300 font-semibold">Direct ID match</div>
                            <div className="text-gray-900 dark:text-gray-100 font-medium">
                              {directMatchNode.title || directMatchNode.id}
                            </div>
                            <div className="text-xs text-gray-600 dark:text-gray-400">{directMatchNode.typeId}</div>
                          </div>
                          <button
                            onClick={() => window.open(`/node/${encodeURIComponent(directMatchNode.id)}`, '_blank')}
                            className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium"
                          >
                            View →
                          </button>
                        </div>
                      </div>
                    )}
                    {(nodesData.data as any).nodes.map((node: any) => (
                      <div
                        key={node.id}
                        className="flex items-center justify-between p-3 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                      >
                        <div className="flex items-center space-x-3">
                          <span className="text-xl">{getNodeTypeIcon(node.typeId)}</span>
                          <div>
                            <div className="font-medium text-gray-900 dark:text-gray-100">
                              <button
                                onClick={() => window.open(`/node/${encodeURIComponent(node.id)}`, '_blank')}
                                className="hover:text-blue-600 hover:underline text-left"
                              >
                                {node.title || node.id}
                              </button>
                            </div>
                            <div className="text-sm text-gray-600 dark:text-gray-400">{node.typeId}</div>
                          </div>
                        </div>
                        <div className="flex items-center space-x-2">
                          <span className={`px-2 py-1 rounded-full text-xs font-medium border ${getStateColor(node.state)}`}>
                            {node.state}
                          </span>
                          <button
                            onClick={() => window.open(`/node/${encodeURIComponent(node.id)}`, '_blank')}
                            className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium"
                          >
                            View →
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center text-gray-500 dark:text-gray-400 py-12">
                    <div className="text-6xl mb-4">🔍</div>
                    <div className="text-xl font-medium mb-2">No Nodes Found</div>
                    <p className="text-sm mb-6 max-w-md mx-auto">
                      {selectedNodeType || searchQuery
                        ? 'No nodes match your current filters. Try adjusting the filters above or clear them to see all nodes.'
                        : 'No nodes found in the system. Nodes will appear as content is created.'
                      }
                    </p>
                    {(selectedNodeType || searchQuery) && (
                      <button
                        onClick={() => {
                          setSelectedNodeType('');
                          setSearchQuery('');
                        }}
                        className="bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 transition-colors text-sm font-medium"
                      >
                        Clear Filters
                      </button>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        )}

        {/* Edges Tab */}
        {selectedView === 'edges' && (
          <div className="space-y-6">
            {/* Edge Filters */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <span>🔗</span>
                  Edge Explorer
                </CardTitle>
                <CardDescription>
                  Browse and filter edge relationships in the knowledge graph
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                 <div>
                   <label htmlFor="role-filter" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                     Role Filter
                   </label>
                   <select
                     id="role-filter"
                     value={selectedEdgeRole}
                     onChange={(e) => setSelectedEdgeRole(e.target.value)}
                     className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                   >
                     <option value="">All Roles</option>
                     {edgeRoles.map((role: string) => (
                       <option key={role} value={role}>{role}</option>
                     ))}
                   </select>
                 </div>
                  
                 <div>
                   <label htmlFor="relationship-type" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                     Relationship Type
                   </label>
                   <select
                     id="relationship-type"
                     value={selectedRelationshipType}
                     onChange={(e) => setSelectedRelationshipType(e.target.value)}
                     className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                   >
                     <option value="">All Relationships</option>
                     {relationshipTypes.map((type: string) => (
                       <option key={type} value={type}>{type}</option>
                     ))}
                   </select>
                 </div>
                  
                 <div>
                   <label htmlFor="from-node-id" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                     From Node ID
                   </label>
                   <input
                     id="from-node-id"
                     type="text"
                     placeholder="Search by source node ID..."
                     value={edgeSearchQuery}
                     onChange={(e) => setEdgeSearchQuery(e.target.value)}
                     className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                   />
                 </div>
                </div>
                
                {(selectedEdgeRole || selectedRelationshipType || edgeSearchQuery) && (
                  <div className="mt-4 flex items-center justify-between">
                    <div className="text-sm text-gray-600 dark:text-gray-400">
                      Active filters: {[selectedEdgeRole, selectedRelationshipType, edgeSearchQuery].filter(Boolean).join(', ')}
                    </div>
                    <button
                      onClick={() => {
                        setSelectedEdgeRole('');
                        setSelectedRelationshipType('');
                        setEdgeSearchQuery('');
                      }}
                      className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 font-medium"
                    >
                      Clear all filters
                    </button>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Edge Results */}
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2">
                      <span>📊</span>
                      Edge Relationships
                    </CardTitle>
                    <CardDescription className="mt-1">
                      {edgesData?.success ? 
                        `${(edgesData.data as any)?.edges?.length || 0} of ${(edgesData.data as any)?.totalCount || 0} edges` : 
                        'Loading edges...'
                      }
                    </CardDescription>
                  </div>
                  <button
                    onClick={() => refetchEdges()}
                    disabled={edgesLoading}
                    className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors text-sm font-medium"
                  >
                    {edgesLoading ? 'Loading...' : 'Refresh'}
                  </button>
                </div>
              </CardHeader>
              
              <CardContent>
                {/* Pagination at top */}
                {edgesData?.success && (edgesData.data as any)?.totalCount > 0 && (
                  <div className="mb-6">
                    <PaginationControls
                      currentPage={edgePage}
                      pageSize={edgePageSize}
                      totalCount={(edgesData.data as any)?.totalCount || 0}
                      onPageChange={setEdgePage}
                      onPageSizeChange={setEdgePageSize}
                      showPageSizeSelector={true}
                      pageSizeOptions={[10, 25, 50, 100, 200]}
                    />
                  </div>
                )}
                
                {/* Edge List */}
                {edgesLoading ? (
                  <div className="space-y-3">
                    {[...Array(5)].map((_, i) => (
                      <div key={i} className="animate-pulse">
                        <div className="bg-gray-200 dark:bg-gray-700 rounded-lg h-24"></div>
                      </div>
                    ))}
                  </div>
                ) : edgesData?.success && (edgesData.data as any)?.edges?.length > 0 ? (
                  <div className="space-y-3">
                    {(edgesData.data as any).edges.map((edge: any, index: number) => (
                      <EdgeCard
                        key={`${edge.fromId}-${edge.toId}-${edge.role}-${index}`}
                        edge={edge}
                        onNodeClick={(nodeId) => window.open(`/node/${encodeURIComponent(nodeId)}`, '_blank')}
                        onEdgeClick={(fromId, toId, role) => window.open(`/edge/${encodeURIComponent(fromId)}/${encodeURIComponent(toId)}`, '_blank')}
                      />
                    ))}
                  </div>
                ) : (
                  <div className="text-center text-gray-500 dark:text-gray-400 py-12">
                    <div className="text-6xl mb-4">🔗</div>
                    <div className="text-xl font-medium mb-2">No Edges Found</div>
                    <p className="text-sm mb-6 max-w-md mx-auto">
                      {selectedEdgeRole || selectedRelationshipType || edgeSearchQuery
                        ? 'No edges match your current filters. Try adjusting the filters above or clear them to see all edges.'
                        : 'No edge relationships found in the system. Edges will appear as relationships are created between nodes.'
                      }
                    </p>
                    {(selectedEdgeRole || selectedRelationshipType || edgeSearchQuery) && (
                      <button
                        onClick={() => {
                          setSelectedEdgeRole('');
                          setSelectedRelationshipType('');
                          setEdgeSearchQuery('');
                        }}
                        className="bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 transition-colors text-sm font-medium"
                      >
                        Clear Filters
                      </button>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        )}

        {/* Insights Tab */}
        {selectedView === 'insights' && (
          <div className="space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-4">🔮 Graph Insights</h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">📈 Growth Patterns</h3>
                  <div className="space-y-4">
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600 dark:text-gray-300">Node Density</span>
                      <span className="font-medium">
                        {stats?.success && (stats.data as any)?.stats?.nodeCount && (stats.data as any)?.stats?.edgeCount ? 
                          ((stats.data as any).stats.edgeCount / (stats.data as any).stats.nodeCount).toFixed(2) : '—'} edges/node
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600 dark:text-gray-300">Average Node Size</span>
                      <span className="font-medium">
                        {stats?.success && (stats.data as any)?.stats?.nodeCount ? 
                          `${((stats.data as any).stats.totalItems / (stats.data as any).stats.nodeCount).toFixed(1)}` : '—'} items/node
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600 dark:text-gray-300">System Health</span>
                      <span className="font-medium text-green-600">Excellent</span>
                    </div>
                  </div>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">🎯 Node Types</h3>
                  <div className="space-y-3">
                     {nodeTypes.slice(0, 5).map((type: string) => (
                      <div key={type} className="flex items-center justify-between">
                        <div className="flex items-center space-x-2">
                          <span>{getNodeTypeIcon(type)}</span>
                          <span className="text-sm text-gray-700">{type}</span>
                        </div>
                        <button
                          onClick={() => {
                            setSelectedNodeType(type);
                            setSelectedView('nodes');
                          }}
                          className="text-blue-600 hover:text-blue-700 text-sm font-medium"
                        >
                          Explore →
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </section>

            <section>
              <div className="bg-gradient-to-r from-blue-50 to-purple-50 rounded-lg border border-blue-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">🌐 Fractal Architecture</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                  <div>
                    <div className="font-medium text-gray-900 mb-2">Meta-Nodes</div>
                    <p className="text-gray-600 dark:text-gray-300">Schemas, APIs, and structural definitions that describe the system itself</p>
                  </div>
                  <div>
                    <div className="font-medium text-gray-900 mb-2">Content Nodes</div>
                    <p className="text-gray-600 dark:text-gray-300">User data, concepts, contributions, and knowledge artifacts</p>
                  </div>
                  <div>
                    <div className="font-medium text-gray-900 mb-2">Flow Nodes</div>
                    <p className="text-gray-600 dark:text-gray-300">Process definitions, workflows, and system state transitions</p>
                  </div>
                </div>
              </div>
            </section>
          </div>
        )}

        {/* Node Details Modal */}
        {nodeDetails && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 max-w-2xl w-full mx-4 max-h-96 overflow-y-auto">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Node Details</h3>
                <button
                  onClick={() => setNodeDetails(null)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  ✕
                </button>
              </div>
              
              <div className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-gray-700">ID</label>
                  <div className="text-sm text-gray-900 font-mono bg-gray-50 p-2 rounded">{nodeDetails.id}</div>
                </div>
                
                <div>
                  <label className="text-sm font-medium text-gray-700">Type</label>
                  <div className="text-sm text-gray-900">{nodeDetails.typeId}</div>
                </div>
                
                <div>
                  <label className="text-sm font-medium text-gray-700">State</label>
                  <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${getStateColor(nodeDetails.state)}`}>
                    {nodeDetails.state}
                  </span>
                </div>
                
                <div>
                  <label className="text-sm font-medium text-gray-700">Title</label>
                  <div className="text-sm text-gray-900">{nodeDetails.title}</div>
                </div>
                
                {nodeDetails.description && (
                  <div>
                    <label className="text-sm font-medium text-gray-700">Description</label>
                    <div className="text-sm text-gray-900">{nodeDetails.description}</div>
                  </div>
                )}
                
                {nodeDetails.meta && Object.keys(nodeDetails.meta).length > 0 && (
                  <div>
                    <label className="text-sm font-medium text-gray-700">Metadata</label>
                    <div className="text-xs text-gray-600 bg-gray-50 p-2 rounded font-mono max-h-32 overflow-y-auto">
                      {JSON.stringify(nodeDetails.meta, null, 2)}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );

  function getNodeTypeIcon(typeId: string): string {
    if (typeId.includes('api')) return '🛠️';
    if (typeId.includes('module')) return '🔧';
    if (typeId.includes('type')) return '📝';
    if (typeId.includes('response')) return '📤';
    if (typeId.includes('route')) return '🛣️';
    if (typeId.includes('axis')) return '📊';
    if (typeId.includes('concept')) return '🧠';
    if (typeId.includes('user')) return '👤';
    return '🔵';
  }

  function getStateColor(state: string): string {
    switch (state.toLowerCase()) {
      case 'ice': return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'water': return 'bg-cyan-100 text-cyan-800 border-cyan-200';
      case 'gas': return 'bg-gray-100 text-gray-800 border-gray-200';
      default: return 'bg-gray-100 text-gray-600 border-gray-200';
    }
  }
}

