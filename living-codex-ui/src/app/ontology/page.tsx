'use client';

import { useState, useEffect, useMemo } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

// Core interfaces that reflect the fractal nature
interface Node {
  id: string;
  typeId: string;
  meta: any;
  description?: string;
  children?: Node[];
  parent?: Node;
  level: number;
  resonance?: number;
}

interface FractalView {
  node: Node;
  depth: number;
  maxDepth: number;
  connections: Node[];
  frequencies: any[];
}

// Main component
export default function OntologyPage() {
  console.log('OntologyPage component rendering');
  
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Core state - everything derives from the data
  const [nodes, setNodes] = useState<Node[]>([]);
  const [currentView, setCurrentView] = useState<FractalView | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Navigation state
  const [navigationStack, setNavigationStack] = useState<FractalView[]>([]);
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);

  // Find connections for a node - this is the fractal principle
  const findConnections = (node: Node, allNodes: Node[]): Node[] => {
    const connections: Node[] = [];
    
    // Find children
    const children = allNodes.filter(n => 
      n.meta?.parentAxes?.includes(node.id) || 
      n.meta?.axis === node.id
    );
    connections.push(...children);
    
    // Find siblings
    const siblings = allNodes.filter(n => 
      n.id !== node.id && 
      n.level === node.level &&
      n.typeId === node.typeId
    );
    connections.push(...siblings.slice(0, 3)); // Limit to avoid clutter
    
    // Find related by keywords
    const related = allNodes.filter(n => 
      n.id !== node.id &&
      n.meta?.keywords?.some((keyword: string) => 
        node.meta?.keywords?.includes(keyword)
      )
    );
    connections.push(...related.slice(0, 2));
    
    return connections;
  };

  // Load all nodes from the system
  const loadNodes = async () => {
    console.log('loadNodes called');
    setLoading(true);
    setError(null);
    
    try {
      // Load all node types in parallel - no hardcoding
      const nodeTypes = ['codex.ontology.axis', 'codex.ucore.base', 'codex.relationship', 'codex.frequency'];
      console.log('Loading node types:', nodeTypes);
      
      const allNodes = await Promise.all(
        nodeTypes.map(async (typeId) => {
          const url = buildApiUrl(`/storage-endpoints/nodes?typeId=${typeId}&take=1000`);
          console.log(`Fetching ${typeId} from:`, url);
          const response = await fetch(url);
          console.log(`${typeId} response status:`, response.status);
          if (response.ok) {
            const data = await response.json();
            console.log(`${typeId} data:`, data);
            return data.nodes?.map((node: any) => ({
              id: node.id,
              typeId: node.typeId,
              meta: node.meta || {},
              description: node.description,
              level: node.meta?.level || 0,
              resonance: Math.random() * 100 // This would come from actual resonance calculation
            })) || [];
          }
          console.log(`${typeId} failed with status:`, response.status);
          return [];
        })
      );
      
      const flatNodes = allNodes.flat();
      console.log('All nodes loaded:', flatNodes.length);
      console.log('Sample nodes:', flatNodes.slice(0, 3));
      setNodes(flatNodes);
      
      // Initialize with root nodes (level 0)
      const rootNodes = flatNodes.filter(node => node.level === 0);
      console.log('Root nodes found:', rootNodes.length);
      console.log('Root nodes:', rootNodes);
      if (rootNodes.length > 0) {
        const initialView: FractalView = {
          node: rootNodes[0],
          depth: 0,
          maxDepth: 3,
          connections: findConnections(rootNodes[0], flatNodes),
          frequencies: []
        };
        console.log('Setting initial view:', initialView);
        setCurrentView(initialView);
        setNavigationStack([initialView]);
      } else {
        console.log('No root nodes found, currentView will remain null');
      }
      
    } catch (err) {
      console.error('Error in loadNodes:', err);
      setError(`Failed to load nodes: ${err instanceof Error ? err.message : 'Unknown error'}`);
    } finally {
      setLoading(false);
    }
  };

  // Navigate to a node - this is the core interaction
  const navigateToNode = (node: Node) => {
    if (!currentView) return;
    
    const newView: FractalView = {
      node,
      depth: currentView.depth + 1,
      maxDepth: currentView.maxDepth,
      connections: findConnections(node, nodes),
      frequencies: [] // This would be calculated from actual data
    };
    
    setCurrentView(newView);
    setNavigationStack(prev => [...prev, newView]);
    setSelectedNodeId(node.id);
    
    if (user?.id) {
      trackInteraction('ontology-navigation', 'node-selected', {
        nodeId: node.id,
        nodeType: node.typeId,
        depth: newView.depth
      });
    }
  };

  // Navigate back in the stack
  const navigateBack = () => {
    if (navigationStack.length > 1) {
      const newStack = navigationStack.slice(0, -1);
      setNavigationStack(newStack);
      setCurrentView(newStack[newStack.length - 1]);
      setSelectedNodeId(newStack[newStack.length - 1].node.id);
    }
  };

  // Load data on mount
  useEffect(() => {
    console.log('useEffect called, loading nodes...');
    loadNodes();
  }, []);

  // Render a node as a fractal element
  const renderNode = (node: Node, isMain: boolean = false) => {
    const isSelected = selectedNodeId === node.id;
    const hasChildren = node.children && node.children.length > 0;
    const resonance = node.resonance || 0;
    
    return (
      <div
        key={node.id}
        className={`
          relative p-4 rounded-lg border-2 transition-all duration-300 cursor-pointer
          ${isMain ? 'bg-gradient-to-br from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20' : 'bg-white dark:bg-gray-800'}
          ${isSelected ? 'border-blue-500 shadow-lg scale-105' : 'border-gray-200 dark:border-gray-700 hover:border-blue-300'}
          ${isMain ? 'min-h-[200px]' : 'min-h-[120px]'}
        `}
        onClick={() => navigateToNode(node)}
      >
        {/* Resonance indicator */}
        <div className="absolute top-2 right-2">
          <div className={`w-3 h-3 rounded-full ${
            resonance > 80 ? 'bg-green-500' : 
            resonance > 60 ? 'bg-yellow-500' : 
            resonance > 40 ? 'bg-orange-500' : 'bg-gray-400'
          }`} />
        </div>
        
        {/* Node content */}
        <div className="space-y-2">
          <h3 className={`font-semibold ${isMain ? 'text-xl' : 'text-lg'} text-gray-900 dark:text-gray-100`}>
            {node.meta?.name || node.id}
          </h3>
          
          <p className="text-sm text-gray-600 dark:text-gray-300 line-clamp-2">
            {node.description || node.meta?.description || 'No description available'}
          </p>
          
          {/* Keywords as clickable elements */}
          {node.meta?.keywords && (
            <div className="flex flex-wrap gap-1">
              {node.meta.keywords.slice(0, 4).map((keyword: string) => (
                <span
                  key={keyword}
                  className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded-full text-xs hover:bg-blue-200 dark:hover:bg-blue-800/50 transition-colors"
                >
                  {keyword}
                </span>
              ))}
              {node.meta.keywords.length > 4 && (
                <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-full text-xs">
                  +{node.meta.keywords.length - 4}
                </span>
              )}
            </div>
          )}
          
          {/* Dimensions as clickable elements */}
          {node.meta?.dimensions && (
            <div className="flex flex-wrap gap-1">
              {node.meta.dimensions.slice(0, 3).map((dimension: string) => (
                <span
                  key={dimension}
                  className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 rounded-full text-xs hover:bg-green-200 dark:hover:bg-green-800/50 transition-colors"
                >
                  {dimension}
                </span>
              ))}
              {node.meta.dimensions.length > 3 && (
                <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-full text-xs">
                  +{node.meta.dimensions.length - 3}
                </span>
              )}
            </div>
          )}
          
          {/* Type indicator */}
          <div className="text-xs text-gray-500 dark:text-gray-400">
            {node.typeId.replace('codex.', '')} • Level {node.level}
          </div>
        </div>
        
        {/* Navigation hint */}
        {hasChildren && (
          <div className="absolute bottom-2 right-2 text-gray-400">
            ↓
          </div>
        )}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">Loading knowledge universe...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-500 text-6xl mb-4">⚠️</div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">Connection Lost</h2>
          <p className="text-gray-600 dark:text-gray-300 mb-4">{error}</p>
          <button
            onClick={loadNodes}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  if (!currentView) {
    console.log('Rendering no knowledge found state');
    console.log('Loading:', loading);
    console.log('Error:', error);
    console.log('Nodes length:', nodes.length);
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="text-gray-400 text-6xl mb-4">🧠</div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">No Knowledge Found</h2>
          <p className="text-gray-600 dark:text-gray-300">The knowledge universe appears to be empty.</p>
          <div className="mt-4 text-sm text-gray-500">
            <p>Loading: {loading ? 'true' : 'false'}</p>
            <p>Error: {error || 'none'}</p>
            <p>Nodes: {nodes.length}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                🧠 Knowledge Explorer
              </h1>
              <p className="text-sm text-gray-600 dark:text-gray-300">
                Navigate the fractal structure of knowledge • Everything is a Node
              </p>
            </div>
            
            {/* Navigation breadcrumb */}
            <div className="flex items-center space-x-2">
              {navigationStack.length > 1 && (
                <button
                  onClick={navigateBack}
                  className="px-3 py-1 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
                >
                  ← Back
                </button>
              )}
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Depth: {currentView.depth}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main content */}
      <div className="max-w-7xl mx-auto px-4 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Current node - the center of the fractal */}
          <div className="lg:col-span-2">
            <div className="mb-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                Current Focus
              </h2>
              {renderNode(currentView.node, true)}
            </div>
            
            {/* Connections - the fractal expansion */}
            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                Connected Knowledge ({currentView.connections.length})
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {currentView.connections.map(connection => renderNode(connection))}
              </div>
            </div>
          </div>
          
          {/* Sidebar - system information */}
          <div className="space-y-6">
            {/* System stats */}
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                📊 System State
              </h3>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Total Nodes</span>
                  <span className="font-medium">{nodes.length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Current Depth</span>
                  <span className="font-medium">{currentView.depth}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Connections</span>
                  <span className="font-medium">{currentView.connections.length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Resonance</span>
                  <span className="font-medium">{Math.round(currentView.node.resonance || 0)}%</span>
                </div>
              </div>
            </div>
            
            {/* Navigation help */}
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                🧭 Navigation
              </h3>
              <div className="text-sm text-gray-600 dark:text-gray-300 space-y-2">
                <p><strong>Click any node</strong> to explore deeper into the knowledge fractal</p>
                <p><strong>Keywords & dimensions</strong> are clickable and show connections</p>
                <p><strong>Resonance indicators</strong> show the strength of connections</p>
                <p><strong>Everything is a Node</strong> - every element can be explored further</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
