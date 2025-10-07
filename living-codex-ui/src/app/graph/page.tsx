'use client';

/**
 * Graph Visualization Page
 * Interactive exploration of the Living Codex knowledge graph
 * Demonstrates "Everything is a Node" principle
 */

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import GraphVisualization from '@/components/graph/GraphVisualization';
import { getNodes, getEdgesFrom, Node, Edge, getNodeStats, searchNodes } from '@/lib/graph-api';

export default function GraphPage() {
  const router = useRouter();
  const [nodes, setNodes] = useState<Node[]>([]);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<any>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedNode, setSelectedNode] = useState<Node | null>(null);
  const [nodeLimit, setNodeLimit] = useState(100);

  // Load initial data
  useEffect(() => {
    loadGraph();
    loadStats();
  }, [nodeLimit]);

  async function loadGraph() {
    try {
      setLoading(true);
      setError(null);

      // Load nodes
      const loadedNodes = await getNodes(nodeLimit);
      setNodes(loadedNodes);

      // Load edges for first 50 nodes (to keep visualization manageable)
      const edgePromises = loadedNodes
        .slice(0, 50)
        .map(node => getEdgesFrom(node.id));
      
      const edgeArrays = await Promise.all(edgePromises);
      const allEdges = edgeArrays.flat();
      
      // Filter edges to only include nodes in our set
      const nodeIds = new Set(loadedNodes.map(n => n.id));
      const filteredEdges = allEdges.filter(
        edge => nodeIds.has(edge.fromId) && nodeIds.has(edge.toId)
      );
      
      setEdges(filteredEdges);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load graph');
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
      loadGraph();
      return;
    }

    try {
      setLoading(true);
      const results = await searchNodes(searchQuery);
      setNodes(results);
      
      // Load edges for search results
      const edgePromises = results
        .slice(0, 20)
        .map(node => getEdgesFrom(node.id));
      const edgeArrays = await Promise.all(edgePromises);
      const allEdges = edgeArrays.flat();
      
      const nodeIds = new Set(results.map(n => n.id));
      const filteredEdges = allEdges.filter(
        edge => nodeIds.has(edge.fromId) && nodeIds.has(edge.toId)
      );
      
      setEdges(filteredEdges);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
    } finally {
      setLoading(false);
    }
  }

  function handleNodeClick(node: Node) {
    setSelectedNode(node);
  }

  function viewNodeDetail() {
    if (selectedNode) {
      router.push(`/node/${encodeURIComponent(selectedNode.id)}`);
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 text-white p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-4xl font-bold mb-2">Living Codex Graph</h1>
          <p className="text-gray-300">
            Interactive visualization of the knowledge graph • Everything is a Node
          </p>
        </div>

        {/* Controls */}
        <div className="mb-6 grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Search */}
          <div className="col-span-2">
            <div className="flex gap-2">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                placeholder="Search nodes by title or description..."
                className="flex-1 px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
              />
              <button
                onClick={handleSearch}
                className="px-6 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg font-semibold transition-colors"
              >
                Search
              </button>
              <button
                onClick={loadGraph}
                className="px-6 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
              >
                Reset
              </button>
            </div>
          </div>

          {/* Node Limit */}
          <div>
            <label className="block text-sm mb-1">Nodes to display</label>
            <select
              value={nodeLimit}
              onChange={(e) => setNodeLimit(Number(e.target.value))}
              className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:outline-none"
            >
              <option value={50}>50 nodes</option>
              <option value={100}>100 nodes</option>
              <option value={200}>200 nodes</option>
              <option value={500}>500 nodes</option>
            </select>
          </div>
        </div>

        {/* Stats */}
        {stats && (
          <div className="mb-6 grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Total Nodes</div>
              <div className="text-2xl font-bold">{stats.totalNodes.toLocaleString()}</div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Displayed</div>
              <div className="text-2xl font-bold">{nodes.length}</div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Edges Shown</div>
              <div className="text-2xl font-bold">{edges.length}</div>
            </div>
            <div className="bg-gray-800 rounded-lg p-4">
              <div className="text-sm text-gray-400">Node Types</div>
              <div className="text-2xl font-bold">{Object.keys(stats.byType).length}</div>
            </div>
          </div>
        )}

        {/* Error */}
        {error && (
          <div className="mb-6 bg-red-900/50 border border-red-700 rounded-lg p-4">
            <div className="font-semibold">Error loading graph</div>
            <div className="text-sm text-red-300">{error}</div>
          </div>
        )}

        {/* Loading */}
        {loading && (
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
            <div className="mt-4 text-gray-300">Loading graph data...</div>
          </div>
        )}

        {/* Graph Visualization */}
        {!loading && nodes.length > 0 && (
          <div className="bg-gray-800 rounded-lg p-6">
            <GraphVisualization
              nodes={nodes}
              edges={edges}
              onNodeClick={handleNodeClick}
              width={1000}
              height={700}
            />
          </div>
        )}

        {/* Selected Node Info */}
        {selectedNode && (
          <div className="mt-6 bg-gray-800 rounded-lg p-6">
            <div className="flex justify-between items-start mb-4">
              <h2 className="text-2xl font-bold">{selectedNode.title}</h2>
              <button
                onClick={viewNodeDetail}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg text-sm font-semibold transition-colors"
              >
                View Details →
              </button>
            </div>
            
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <div className="text-sm text-gray-400">Node ID</div>
                <div className="font-mono text-sm">{selectedNode.id}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Type</div>
                <div className="font-mono text-sm">{selectedNode.typeId}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">State</div>
                <div className="font-mono text-sm">{selectedNode.state}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Locale</div>
                <div className="font-mono text-sm">{selectedNode.locale}</div>
              </div>
            </div>
            
            <div>
              <div className="text-sm text-gray-400 mb-2">Description</div>
              <div className="text-sm">{selectedNode.description}</div>
            </div>
          </div>
        )}

        {/* Quick Links */}
        <div className="mt-8 flex gap-4 justify-center">
          <button
            onClick={() => router.push('/nodes')}
            className="px-6 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
          >
            Browse All Nodes →
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
