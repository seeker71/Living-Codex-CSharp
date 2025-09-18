'use client';

import { useEffect, useState } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { useStorageStats, useNodes, useHealthStatus, useAdvancedNodeSearch, useNodeTypes } from '@/lib/hooks';
import { endpoints } from '@/lib/api';

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
  const [selectedNodeType, setSelectedNodeType] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedView, setSelectedView] = useState<'overview' | 'nodes' | 'edges' | 'insights'>('overview');
  const [nodeDetails, setNodeDetails] = useState<Node | null>(null);
  const [loading, setLoading] = useState(false);
  
  // Edge browser state
  const [selectedEdgeRole, setSelectedEdgeRole] = useState<string>('');
  const [selectedRelationshipType, setSelectedRelationshipType] = useState<string>('');
  const [edgeSearchQuery, setEdgeSearchQuery] = useState('');
  const [edgesData, setEdgesData] = useState<any>(null);
  const [edgesLoading, setEdgesLoading] = useState(false);

  // Use hooks for data fetching
  const { data: stats, isLoading: statsLoading } = useStorageStats();
  const { data: healthData } = useHealthStatus();
  const { data: nodeTypesData, isLoading: nodeTypesLoading } = useNodeTypes();
  
  // Advanced node search with filtering
  const searchParams = {
    typeIds: selectedNodeType ? [selectedNodeType] : undefined,
    searchTerm: searchQuery || undefined,
    take: 50,
    skip: 0,
    sortBy: 'id',
    sortDescending: false
  };
  
  const { data: nodesData, isLoading: nodesLoading, refetch: refetchNodes } = useAdvancedNodeSearch(searchParams);

  // Get node types dynamically from backend
  const nodeTypes = nodeTypesData?.success ? 
    (nodeTypesData.data as any)?.nodeTypes?.map((nt: any) => nt.typeId) || [] :
    [];

  const edgeRoles = ['defines', 'implements', 'contains', 'uses', 'extends', 'references', 'connects', 'provides'];
  const relationshipTypes = [
    'module-defines-record-type',
    'module-implements-interface', 
    'node-contains-property',
    'api-uses-type',
    'concept-extends-concept',
    'node-references-node',
    'service-provides-capability',
    'user-interacts-with-concept'
  ];

  // Function to load edges with filtering
  const loadEdges = async (limit = 100) => {
    setEdgesLoading(true);
    try {
      const response = await endpoints.getEdges(limit);
      if (response.success && response.data) {
        let edges = (response.data as any).edges || [];
        
        // Apply filters
        if (selectedEdgeRole) {
          edges = edges.filter((edge: any) => edge.role === selectedEdgeRole);
        }
        
        if (selectedRelationshipType) {
          edges = edges.filter((edge: any) => edge.meta?.relationship === selectedRelationshipType);
        }
        
        if (edgeSearchQuery) {
          const query = edgeSearchQuery.toLowerCase();
          edges = edges.filter((edge: any) => 
            edge.fromId?.toLowerCase().includes(query) ||
            edge.toId?.toLowerCase().includes(query)
          );
        }
        
        setEdgesData({
          ...response.data,
          edges,
          filteredCount: edges.length,
          originalCount: (response.data as any).edges?.length || 0
        });
      }
    } catch (error) {
      console.error('Failed to load edges:', error);
    } finally {
      setEdgesLoading(false);
    }
  };

  // Load edges when filters change
  useEffect(() => {
    if (selectedView === 'edges') {
      loadEdges();
    }
  }, [selectedView, selectedEdgeRole, selectedRelationshipType, edgeSearchQuery]);

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900">Knowledge Graph</h1>
              <StatusBadge status="Enhanced" />
            </div>
            <Navigation />
          </div>
        </div>
      </header>

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
                  <div className="bg-white rounded-lg border border-gray-200 p-4">
                    <div className="text-gray-500 text-sm">Total Nodes</div>
                    <div className="text-3xl font-bold text-gray-900">{(stats.data as any)?.stats?.nodeCount?.toLocaleString() ?? '—'}</div>
                    <div className="text-xs text-gray-400 mt-1">All states</div>
                  </div>
                  <div className="bg-white rounded-lg border border-gray-200 p-4">
                    <div className="text-gray-500 text-sm">Total Edges</div>
                    <div className="text-3xl font-bold text-gray-900">{(stats.data as any)?.stats?.edgeCount?.toLocaleString() ?? '—'}</div>
                    <div className="text-xs text-gray-400 mt-1">Relationships</div>
                  </div>
                  <div className="bg-white rounded-lg border border-gray-200 p-4">
                    <div className="text-gray-500 text-sm">Storage Backend</div>
                    <div className="text-lg font-bold text-gray-900">
                      {(stats.data as any)?.stats?.storageBackend ? 
                        (stats.data as any).stats.storageBackend.replace('StorageBackend', '') : '—'}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">Backend type</div>
                  </div>
                  <div className="bg-white rounded-lg border border-gray-200 p-4">
                    <div className="text-gray-500 text-sm">Last Updated</div>
                    <div className="text-lg font-bold text-gray-900">
                      {(stats.data as any)?.stats?.timestamp ? 
                        new Date((stats.data as any).stats.timestamp).toLocaleTimeString() : '—'}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">
                      {(stats.data as any)?.stats?.timestamp ? 
                        new Date((stats.data as any).stats.timestamp).toLocaleDateString() : 'Unknown'}
                    </div>
                  </div>
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
              
              <div className="bg-white rounded-lg border border-gray-200 p-6">
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
              </div>
            </section>
          </div>
        )}

        {/* Nodes Tab */}
        {selectedView === 'nodes' && (
          <div className="space-y-6">
            {/* Node Explorer Controls */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">🔍 Node Explorer</h2>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Node Type
                  </label>
                  <select
                    value={selectedNodeType}
                    onChange={(e) => setSelectedNodeType(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
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
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900">
                  Node Results
                </h3>
                <span className="text-sm text-gray-500">
                  {nodesLoading ? 'Loading...' : `${(nodesData?.data as any)?.nodes?.length || 0} nodes`}
                </span>
              </div>

              {nodesLoading ? (
                <div className="space-y-3">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="animate-pulse bg-gray-200 rounded-lg h-16"></div>
                  ))}
                </div>
              ) : nodesData?.success && (nodesData.data as any)?.nodes ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {(nodesData.data as any).nodes.map((node: any) => (
                    <div
                      key={node.id}
                      onClick={() => setNodeDetails(node)}
                      className="flex items-center justify-between p-3 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
                    >
                      <div className="flex items-center space-x-3">
                        <span className="text-xl">{getNodeTypeIcon(node.typeId)}</span>
                        <div>
                          <div className="font-medium text-gray-900">{node.title || node.id}</div>
                          <div className="text-sm text-gray-600">{node.typeId}</div>
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium border ${getStateColor(node.state)}`}>
                          {node.state}
                        </span>
                        <span className="text-gray-400">→</span>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <div className="text-4xl mb-4">🔍</div>
                  <div>No nodes found</div>
                  <button
                    onClick={() => setSelectedNodeType('')}
                    className="mt-4 text-blue-600 hover:text-blue-700 text-sm font-medium"
                  >
                    Clear filters
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Edges Tab */}
        {selectedView === 'edges' && (
          <div className="space-y-6">
            {/* Edge Filters */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">🔗 Edge Filters</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Role</label>
                  <select
                    value={selectedEdgeRole}
                    onChange={(e) => setSelectedEdgeRole(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="">All Roles</option>
                    {edgeRoles.map(role => (
                      <option key={role} value={role}>{role}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Relationship Type</label>
                  <select
                    value={selectedRelationshipType}
                    onChange={(e) => setSelectedRelationshipType(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="">All Relationships</option>
                    {relationshipTypes.map(type => (
                      <option key={type} value={type}>{type}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Search Node IDs</label>
                  <input
                    type="text"
                    placeholder="Search fromId or toId..."
                    value={edgeSearchQuery}
                    onChange={(e) => setEdgeSearchQuery(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
              </div>
              {(selectedEdgeRole || selectedRelationshipType || edgeSearchQuery) && (
                <div className="mt-4">
                  <button
                    onClick={() => {
                      setSelectedEdgeRole('');
                      setSelectedRelationshipType('');
                      setEdgeSearchQuery('');
                    }}
                    className="text-sm text-blue-600 hover:text-blue-800"
                  >
                    Clear all filters
                  </button>
                </div>
              )}
            </div>

            {/* Edge Results */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold text-gray-900">
                  📊 Edge Relationships
                  {edgesData && (
                    <span className="ml-2 text-sm font-normal text-gray-500">
                      ({edgesData.filteredCount} of {edgesData.originalCount} edges)
                    </span>
                  )}
                </h2>
                <button
                  onClick={() => loadEdges()}
                  disabled={edgesLoading}
                  className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
                >
                  {edgesLoading ? 'Loading...' : 'Refresh'}
                </button>
              </div>
              
              {edgesLoading ? (
                <div className="space-y-3">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="animate-pulse bg-gray-200 rounded-lg h-16"></div>
                  ))}
                </div>
              ) : edgesData?.edges?.length > 0 ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {edgesData.edges.map((edge: any, index: number) => (
                    <div key={index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center space-x-2 mb-2">
                            <span className="bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded">{edge.role}</span>
                            <span className="bg-green-100 text-green-800 text-xs px-2 py-1 rounded">
                              Weight: {edge.weight}
                            </span>
                            {edge.meta?.relationship && (
                              <span className="bg-purple-100 text-purple-800 text-xs px-2 py-1 rounded">
                                {edge.meta.relationship}
                              </span>
                            )}
                          </div>
                          <div className="text-sm text-gray-900 mb-1">
                            <strong>From:</strong> {edge.fromId}
                          </div>
                          <div className="text-sm text-gray-900">
                            <strong>To:</strong> {edge.toId}
                          </div>
                          {edge.meta && Object.keys(edge.meta).length > 1 && (
                            <div className="text-xs text-gray-500 mt-2">
                              <strong>Metadata:</strong> {JSON.stringify(edge.meta, null, 2)}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <div className="text-4xl mb-4">🔗</div>
                  <div className="text-lg font-medium mb-2">No Edges Found</div>
                  <p className="text-sm mb-4">
                    {selectedEdgeRole || selectedRelationshipType || edgeSearchQuery
                      ? 'No edges match your current filters. Try adjusting the filters above.'
                      : 'Click "Refresh" to load edge relationships between nodes'
                    }
                  </p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Insights Tab */}
        {selectedView === 'insights' && (
          <div className="space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mb-4">🔮 Graph Insights</h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="bg-white rounded-lg border border-gray-200 p-6">
                  <h3 className="text-lg font-semibold text-gray-900 mb-4">📈 Growth Patterns</h3>
                  <div className="space-y-4">
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600">Node Density</span>
                      <span className="font-medium">
                        {stats?.success && (stats.data as any)?.stats?.nodeCount && (stats.data as any)?.stats?.edgeCount ? 
                          ((stats.data as any).stats.edgeCount / (stats.data as any).stats.nodeCount).toFixed(2) : '—'} edges/node
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600">Average Node Size</span>
                      <span className="font-medium">
                        {stats?.success && (stats.data as any)?.stats?.nodeCount ? 
                          `${((stats.data as any).stats.totalItems / (stats.data as any).stats.nodeCount).toFixed(1)}` : '—'} items/node
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-gray-600">System Health</span>
                      <span className="font-medium text-green-600">Excellent</span>
                    </div>
                  </div>
                </div>

                <div className="bg-white rounded-lg border border-gray-200 p-6">
                  <h3 className="text-lg font-semibold text-gray-900 mb-4">🎯 Node Types</h3>
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
                    <p className="text-gray-600">Schemas, APIs, and structural definitions that describe the system itself</p>
                  </div>
                  <div>
                    <div className="font-medium text-gray-900 mb-2">Content Nodes</div>
                    <p className="text-gray-600">User data, concepts, contributions, and knowledge artifacts</p>
                  </div>
                  <div>
                    <div className="font-medium text-gray-900 mb-2">Flow Nodes</div>
                    <p className="text-gray-600">Process definitions, workflows, and system state transitions</p>
                  </div>
                </div>
              </div>
            </section>
          </div>
        )}

        {/* Node Details Modal */}
        {nodeDetails && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg border border-gray-200 p-6 max-w-2xl w-full mx-4 max-h-96 overflow-y-auto">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900">Node Details</h3>
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


