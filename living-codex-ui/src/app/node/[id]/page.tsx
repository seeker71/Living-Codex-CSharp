'use client';

/**
 * Node Detail Page
 * Detailed view of a single node with its edges and content
 */

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { getNode, getEdgesFrom, getEdgesTo, Node, Edge } from '@/lib/graph-api';

export default function NodeDetailPage() {
  const params = useParams();
  const router = useRouter();
  const nodeId = decodeURIComponent(params.id as string);
  
  const [node, setNode] = useState<Node | null>(null);
  const [edgesFrom, setEdgesFrom] = useState<Edge[]>([]);
  const [edgesTo, setEdgesTo] = useState<Edge[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'content' | 'edges' | 'meta'>('content');

  useEffect(() => {
    loadNodeData();
  }, [nodeId]);

  async function loadNodeData() {
    try {
      setLoading(true);
      setError(null);

      const [nodeData, fromEdges, toEdges] = await Promise.all([
        getNode(nodeId),
        getEdgesFrom(nodeId),
        getEdgesTo(nodeId),
      ]);

      setNode(nodeData);
      setEdgesFrom(fromEdges);
      setEdgesTo(toEdges);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load node');
    } finally {
      setLoading(false);
    }
  }

  function parseContent(node: Node) {
    if (node.content.inlineJson) {
      try {
        return JSON.parse(node.content.inlineJson);
      } catch {
        return node.content.inlineJson;
      }
    }
    return null;
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 text-white flex items-center justify-center">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
          <div className="mt-4 text-gray-300">Loading node...</div>
        </div>
      </div>
    );
  }

  if (error || !node) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 text-white p-6">
        <div className="max-w-4xl mx-auto">
          <div className="bg-red-900/50 border border-red-700 rounded-lg p-6">
            <h2 className="text-2xl font-bold mb-2">Error</h2>
            <p className="text-red-300">{error || 'Node not found'}</p>
            <button
              onClick={() => router.back()}
              className="mt-4 px-6 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg transition-colors"
            >
              ← Go Back
            </button>
          </div>
        </div>
      </div>
    );
  }

  const parsedContent = parseContent(node);

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 text-white p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <button
            onClick={() => router.back()}
            className="text-gray-400 hover:text-white mb-4 transition-colors"
          >
            ← Back
          </button>
          
          <div className="flex justify-between items-start">
            <div>
              <h1 className="text-4xl font-bold mb-2">{node.title}</h1>
              <p className="text-gray-300">{node.description}</p>
            </div>
            <span className={`px-3 py-1 rounded-lg text-sm font-semibold ${
              node.state === 'ice' ? 'bg-blue-900 text-blue-300' :
              node.state === 'water' ? 'bg-green-900 text-green-300' :
              'bg-red-900 text-red-300'
            }`}>
              {node.state}
            </span>
          </div>
        </div>

        {/* Quick Info */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <div className="bg-gray-800 rounded-lg p-4">
            <div className="text-sm text-gray-400">Type</div>
            <div className="font-mono text-sm mt-1">{node.typeId}</div>
          </div>
          <div className="bg-gray-800 rounded-lg p-4">
            <div className="text-sm text-gray-400">Locale</div>
            <div className="font-mono text-sm mt-1">{node.locale}</div>
          </div>
          <div className="bg-gray-800 rounded-lg p-4">
            <div className="text-sm text-gray-400">Connections</div>
            <div className="text-2xl font-bold">
              {edgesFrom.length} out • {edgesTo.length} in
            </div>
          </div>
        </div>

        {/* Node ID */}
        <div className="mb-6 bg-gray-800 rounded-lg p-4">
          <div className="text-sm text-gray-400 mb-1">Node ID</div>
          <div className="font-mono text-xs break-all">{node.id}</div>
        </div>

        {/* Tabs */}
        <div className="mb-6">
          <div className="flex gap-2 border-b border-gray-700">
            <button
              onClick={() => setActiveTab('content')}
              className={`px-6 py-3 font-semibold transition-colors ${
                activeTab === 'content'
                  ? 'border-b-2 border-blue-500 text-white'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              Content
            </button>
            <button
              onClick={() => setActiveTab('edges')}
              className={`px-6 py-3 font-semibold transition-colors ${
                activeTab === 'edges'
                  ? 'border-b-2 border-blue-500 text-white'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              Edges ({edgesFrom.length + edgesTo.length})
            </button>
            <button
              onClick={() => setActiveTab('meta')}
              className={`px-6 py-3 font-semibold transition-colors ${
                activeTab === 'meta'
                  ? 'border-b-2 border-blue-500 text-white'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              Metadata
            </button>
          </div>
        </div>

        {/* Content Tab */}
        {activeTab === 'content' && (
          <div className="bg-gray-800 rounded-lg p-6">
            <div className="mb-4">
              <div className="text-sm text-gray-400">Media Type</div>
              <div className="font-mono text-sm">{node.content.mediaType}</div>
            </div>
            
            {parsedContent && (
              <div>
                <div className="text-sm text-gray-400 mb-2">Parsed Content</div>
                <pre className="bg-gray-900 p-4 rounded-lg overflow-auto text-xs">
                  {JSON.stringify(parsedContent, null, 2)}
                </pre>
              </div>
            )}

            {node.content.url && (
              <div className="mt-4">
                <div className="text-sm text-gray-400 mb-2">External URL</div>
                <a
                  href={node.content.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-400 hover:text-blue-300 underline"
                >
                  {node.content.url}
                </a>
              </div>
            )}
          </div>
        )}

        {/* Edges Tab */}
        {activeTab === 'edges' && (
          <div className="space-y-6">
            {/* Outgoing Edges */}
            <div>
              <h3 className="text-xl font-bold mb-4">Outgoing Edges ({edgesFrom.length})</h3>
              <div className="space-y-3">
                {edgesFrom.length === 0 ? (
                  <div className="text-gray-400 text-sm">No outgoing edges</div>
                ) : (
                  edgesFrom.map(edge => (
                    <div
                      key={edge.id}
                      onClick={() => router.push(`/node/${encodeURIComponent(edge.toId)}`)}
                      className="bg-gray-800 rounded-lg p-4 hover:bg-gray-700 cursor-pointer transition-colors"
                    >
                      <div className="flex justify-between items-start mb-2">
                        <span className="text-sm font-semibold text-green-400">→ {edge.toId}</span>
                        <span className="text-xs text-gray-400 font-mono">{edge.role}</span>
                      </div>
                      <div className="text-xs text-gray-400">Type: {edge.typeId}</div>
                    </div>
                  ))
                )}
              </div>
            </div>

            {/* Incoming Edges */}
            <div>
              <h3 className="text-xl font-bold mb-4">Incoming Edges ({edgesTo.length})</h3>
              <div className="space-y-3">
                {edgesTo.length === 0 ? (
                  <div className="text-gray-400 text-sm">No incoming edges</div>
                ) : (
                  edgesTo.map(edge => (
                    <div
                      key={edge.id}
                      onClick={() => router.push(`/node/${encodeURIComponent(edge.fromId)}`)}
                      className="bg-gray-800 rounded-lg p-4 hover:bg-gray-700 cursor-pointer transition-colors"
                    >
                      <div className="flex justify-between items-start mb-2">
                        <span className="text-sm font-semibold text-blue-400">← {edge.fromId}</span>
                        <span className="text-xs text-gray-400 font-mono">{edge.role}</span>
                      </div>
                      <div className="text-xs text-gray-400">Type: {edge.typeId}</div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}

        {/* Metadata Tab */}
        {activeTab === 'meta' && (
          <div className="bg-gray-800 rounded-lg p-6">
            <pre className="bg-gray-900 p-4 rounded-lg overflow-auto text-xs">
              {JSON.stringify(node.meta, null, 2)}
            </pre>
          </div>
        )}

        {/* Actions */}
        <div className="mt-8 flex gap-4 justify-center">
          <button
            onClick={() => router.push('/graph')}
            className="px-6 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
          >
            View in Graph
          </button>
          <button
            onClick={() => router.push('/nodes')}
            className="px-6 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-semibold transition-colors"
          >
            Browse All Nodes
          </button>
        </div>
      </div>
    </div>
  );
}
