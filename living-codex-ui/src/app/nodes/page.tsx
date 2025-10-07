'use client';

/**
 * Node Browser Page
 * Browse, filter, and search all nodes in the Living Codex
 */

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { getNodes, getNodeStats, getNodesByType, searchNodes, Node } from '@/lib/graph-api';

export default function NodesPage() {
  const router = useRouter();
  const [nodes, setNodes] = useState<Node[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<any>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterType, setFilterType] = useState<string>('all');
  const [filterState, setFilterState] = useState<string>('all');
  const [page, setPage] = useState(1);
  const pageSize = 50;

  // Load data
  useEffect(() => {
    loadNodes();
    loadStats();
  }, [filterType, filterState]);

  async function loadNodes() {
    try {
      setLoading(true);
      setError(null);

      let loadedNodes: Node[];
      
      if (filterType !== 'all') {
        loadedNodes = await getNodesByType(filterType);
      } else {
        loadedNodes = await getNodes(1000);
      }

      // Apply state filter
      if (filterState !== 'all') {
        loadedNodes = loadedNodes.filter(n => n.state === filterState);
      }

      setNodes(loadedNodes);
      setPage(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load nodes');
    } finally {
      setLoading(false);
    }
  }

  async function loadStats() {
    try {
      const statsData = await getNodeStats();
      setStats(statsData);
    } catch (err) {
      console.error('Failed to load stats:', err);
    }
  }

  async function handleSearch() {
    if (!searchQuery.trim()) {
      loadNodes();
      return;
    }

    try {
      setLoading(true);
      const results = await searchNodes(searchQuery, 200);
      setNodes(results);
      setPage(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
    } finally {
      setLoading(false);
    }
  }

  const paginatedNodes = nodes.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(nodes.length / pageSize);

  const topTypes = stats?.byType 
    ? Object.entries(stats.byType)
        .sort(([,a], [,b]) => (b as number) - (a as number))
        .slice(0, 20)
    : [];

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 text-white p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-4xl font-bold mb-2">Node Browser</h1>
          <p className="text-gray-300">
            Explore all nodes in the Living Codex knowledge graph
          </p>
        </div>

        {/* Stats */}
        {stats && (
          <div className="mb-6 grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Total Nodes</div>
              <div className="text-2xl font-bold">{stats.totalNodes.toLocaleString()}</div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Ice (Persistent)</div>
              <div className="text-2xl font-bold text-blue-400">
                {(stats.byState.ice || 0).toLocaleString()}
              </div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Water (Semi-persistent)</div>
              <div className="text-2xl font-bold text-green-400">
                {(stats.byState.water || 0).toLocaleString()}
              </div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Gas (Transient)</div>
              <div className="text-2xl font-bold text-red-400">
                {(stats.byState.gas || 0).toLocaleString()}
              </div>
            </div>
          </div>
        )}

        {/* Search and Filters */}
        <div className="mb-6 space-y-4">
          {/* Search */}
          <div className="flex gap-2">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              placeholder="Search nodes..."
              className="flex-1 px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
            />
            <button
              onClick={handleSearch}
              className="px-6 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg font-semibold transition-colors"
            >
              Search
            </button>
            <button
              onClick={() => { setSearchQuery(''); loadNodes(); }}
              className="px-6 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
            >
              Reset
            </button>
          </div>

          {/* Filters */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm mb-1">Filter by Type</label>
              <select
                value={filterType}
                onChange={(e) => setFilterType(e.target.value)}
                className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
              >
                <option value="all">All Types</option>
                {topTypes.map(([type, count]) => (
                  <option key={type} value={type}>
                    {type} ({count})
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm mb-1">Filter by State</label>
              <select
                value={filterState}
                onChange={(e) => setFilterState(e.target.value)}
                className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
              >
                <option value="all">All States</option>
                <option value="ice">Ice (Persistent)</option>
                <option value="water">Water (Semi-persistent)</option>
                <option value="gas">Gas (Transient)</option>
              </select>
            </div>
          </div>
        </div>

        {/* Results Info */}
        <div className="mb-4 text-sm text-gray-400">
          Showing {nodes.length > 0 ? (page - 1) * pageSize + 1 : 0} - {Math.min(page * pageSize, nodes.length)} of {nodes.length} nodes
        </div>

        {/* Error */}
        {error && (
          <div className="mb-6 bg-red-900/50 border border-red-700 rounded-lg p-4">
            <div className="font-semibold">Error loading nodes</div>
            <div className="text-sm text-red-300">{error}</div>
          </div>
        )}

        {/* Loading */}
        {loading && (
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
            <div className="mt-4 text-gray-300">Loading nodes...</div>
          </div>
        )}

        {/* Node List */}
        {!loading && paginatedNodes.length > 0 && (
          <div className="space-y-3">
            {paginatedNodes.map(node => (
              <div
                key={node.id}
                onClick={() => router.push(`/node/${encodeURIComponent(node.id)}`)}
                className="bg-gray-800 rounded-lg p-4 hover:bg-gray-700 cursor-pointer transition-colors"
              >
                <div className="flex justify-between items-start mb-2">
                  <h3 className="font-semibold text-lg">{node.title}</h3>
                  <span className={`px-2 py-1 rounded text-xs font-semibold ${
                    node.state === 'ice' ? 'bg-blue-900 text-blue-300' :
                    node.state === 'water' ? 'bg-green-900 text-green-300' :
                    'bg-red-900 text-red-300'
                  }`}>
                    {node.state}
                  </span>
                </div>
                
                <p className="text-sm text-gray-300 mb-2">{node.description}</p>
                
                <div className="flex gap-4 text-xs text-gray-400">
                  <span>Type: <span className="font-mono">{node.typeId}</span></span>
                  <span>ID: <span className="font-mono">{node.id}</span></span>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* No results */}
        {!loading && nodes.length === 0 && (
          <div className="text-center py-12">
            <div className="text-gray-400 text-lg">No nodes found</div>
            <div className="text-gray-500 text-sm mt-2">Try adjusting your search or filters</div>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="mt-6 flex justify-center gap-2">
            <button
              onClick={() => setPage(Math.max(1, page - 1))}
              disabled={page === 1}
              className="px-4 py-2 bg-gray-700 hover:bg-gray-600 disabled:bg-gray-800 disabled:text-gray-500 rounded-lg transition-colors"
            >
              ← Previous
            </button>
            
            <span className="px-4 py-2 bg-gray-800 rounded-lg">
              Page {page} of {totalPages}
            </span>
            
            <button
              onClick={() => setPage(Math.min(totalPages, page + 1))}
              disabled={page === totalPages}
              className="px-4 py-2 bg-gray-700 hover:bg-gray-600 disabled:bg-gray-800 disabled:text-gray-500 rounded-lg transition-colors"
            >
              Next →
            </button>
          </div>
        )}

        {/* Quick Links */}
        <div className="mt-8 flex gap-4 justify-center">
          <button
            onClick={() => router.push('/graph')}
            className="px-6 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
          >
            View Graph Visualization
          </button>
          <button
            onClick={() => router.push('/')}
            className="px-6 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
          >
            ← Back to Home
          </button>
        </div>
      </div>
    </div>
  );
}
