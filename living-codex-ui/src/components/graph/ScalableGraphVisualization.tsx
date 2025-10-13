'use client';

/**
 * Scalable Graph Visualization Component
 * Uses SpatialGraphModule for viewport-based rendering of millions of nodes
 * Implements zoom-based LOD: Galaxy (clusters) → System (major nodes) → Detail (full)
 */

import React, { useEffect, useRef, useState, useCallback } from 'react';
import { 
  useViewportGraph, 
  useDrillDown, 
  useClusterMembers,
  ViewportNode,
  NodeCluster,
  ViewportEdge,
  SpatialGraphResponse,
  calculateZoomLevel,
  getZoomLevelName
} from '@/lib/spatial-graph-api';

interface ScalableGraphVisualizationProps {
  width?: number;
  height?: number;
  baseUrl?: string;
  onNodeClick?: (nodeId: string) => void;
  onClusterClick?: (clusterId: string) => void;
  initialFocusNodeId?: string;
}

type ZoomLevel = 0 | 1 | 2; // 0=Galaxy, 1=System, 2=Detail

const ZOOM_LEVELS = {
  GALAXY: 0,
  SYSTEM: 1,
  DETAIL: 2,
} as const;

export default function ScalableGraphVisualization({
  width = 1200,
  height = 800,
  baseUrl,
  onNodeClick,
  onClusterClick,
  initialFocusNodeId,
}: ScalableGraphVisualizationProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [zoomFactor, setZoomFactor] = useState(0.5); // Start at System view
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [hoveredItem, setHoveredItem] = useState<ViewportNode | NodeCluster | null>(null);
  const [drilldownNodeId, setDrilldownNodeId] = useState<string | null>(null);
  const [expandedClusterId, setExpandedClusterId] = useState<string | null>(null);
  const animationRef = useRef<number | null>(null);

  // Use spatial graph hooks
  const { data: graphData, isLoading: loading, error: queryError } = useViewportGraph({
    zoomFactor,
    centerX: pan.x,
    centerY: pan.y,
    viewportWidth: width,
    viewportHeight: height,
    focusNodeId: initialFocusNodeId || null,
    typeFilter: null,
  });

  const { data: drilldownData } = useDrillDown(drilldownNodeId);
  const { data: clusterData } = useClusterMembers(expandedClusterId);

  const error = queryError ? 'Failed to load graph visualization' : null;
  const currentZoomLevel = calculateZoomLevel(zoomFactor);

  // Zoom controls
  const handleZoomIn = () => {
    setZoomFactor(prev => Math.min(prev * 1.5, 3));
  };

  const handleZoomOut = () => {
    setZoomFactor(prev => Math.max(prev / 1.5, 0.1));
  };

  const handleResetView = () => {
    setZoomFactor(0.5);
    setPan({ x: 0, y: 0 });
  };

  // Pan controls
  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    setIsDragging(true);
    setDragStart({ x: e.clientX - pan.x, y: e.clientY - pan.y });
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    if (isDragging) {
      setPan({
        x: e.clientX - dragStart.x,
        y: e.clientY - dragStart.y,
      });
    } else {
      // Check for hover
      if (graphData) {
        let found = null;

        // Check clusters
        for (const cluster of graphData.clusters) {
          const cx = cluster.centerX;
          const cy = cluster.centerY;
          const radius = Math.max(20, Math.sqrt(cluster.nodeCount) * 5);
          const dist = Math.sqrt((x - cx) ** 2 + (y - cy) ** 2);
          if (dist < radius) {
            found = cluster;
            break;
          }
        }

        // Check nodes
        if (!found) {
          for (const node of graphData.viewportNodes) {
            const dist = Math.sqrt((x - node.x) ** 2 + (y - node.y) ** 2);
            if (dist < node.size) {
              found = node;
              break;
            }
          }
        }

        setHoveredItem(found);
      }
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  const handleClick = async (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas || !graphData) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    // Check for cluster click (expand cluster)
    for (const cluster of graphData.clusters) {
      const cx = cluster.centerX;
      const cy = cluster.centerY;
      const radius = Math.max(20, Math.sqrt(cluster.nodeCount) * 5);
      const dist = Math.sqrt((x - cx) ** 2 + (y - cy) ** 2);
      
      if (dist < radius) {
        // Notify parent and load cluster data
        if (onClusterClick) {
          onClusterClick(cluster.id);
        }
        setExpandedClusterId(cluster.id);
        setZoomFactor(prev => Math.min(prev * 2, 3));
        return;
      }
    }

    // Check for node click (drilldown)
    for (const node of graphData.viewportNodes) {
      const dist = Math.sqrt((x - node.x) ** 2 + (y - node.y) ** 2);
      
      if (dist < node.size) {
        // Trigger drilldown and notify parent
        setDrilldownNodeId(node.id);
        if (onNodeClick) {
          onNodeClick(node.id);
        }
        return;
      }
    }
  };

  // Render loop
  useEffect(() => {
    if (!graphData) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const render = () => {
      ctx.clearRect(0, 0, width, height);

      // Draw background
      ctx.fillStyle = '#0a0a0a';
      ctx.fillRect(0, 0, width, height);

      // Draw edges
      ctx.strokeStyle = 'rgba(100, 100, 255, 0.3)';
      ctx.lineWidth = 1;
      
      graphData.edges.forEach(edge => {
        const fromNode = graphData.viewportNodes.find(n => n.id === edge.fromId);
        const toNode = graphData.viewportNodes.find(n => n.id === edge.toId);
        
        if (fromNode && toNode) {
          ctx.beginPath();
          ctx.moveTo(fromNode.x, fromNode.y);
          ctx.lineTo(toNode.x, toNode.y);
          ctx.stroke();
        }
      });

      // Draw clusters (Galaxy/System view)
      graphData.clusters.forEach(cluster => {
        const radius = Math.max(20, Math.sqrt(cluster.nodeCount) * 5);
        const isHovered = hoveredItem && 'nodeCount' in hoveredItem && hoveredItem.id === cluster.id;

        ctx.beginPath();
        ctx.arc(cluster.centerX, cluster.centerY, radius, 0, Math.PI * 2);
        
        // Gradient fill
        const gradient = ctx.createRadialGradient(
          cluster.centerX, cluster.centerY, 0,
          cluster.centerX, cluster.centerY, radius
        );
        gradient.addColorStop(0, isHovered ? 'rgba(147, 51, 234, 0.8)' : 'rgba(147, 51, 234, 0.5)');
        gradient.addColorStop(1, 'rgba(147, 51, 234, 0.1)');
        ctx.fillStyle = gradient;
        ctx.fill();
        
        ctx.strokeStyle = isHovered ? '#a855f7' : '#7c3aed';
        ctx.lineWidth = isHovered ? 3 : 2;
        ctx.stroke();

        // Draw cluster count
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 12px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(`${cluster.nodeCount}`, cluster.centerX, cluster.centerY);
      });

      // Draw nodes (System/Detail view)
      graphData.viewportNodes.forEach(node => {
        const isHovered = hoveredItem && 'connectionCount' in hoveredItem && hoveredItem.id === node.id;
        const radius = isHovered ? node.size * 1.2 : node.size;

        ctx.beginPath();
        ctx.arc(node.x, node.y, radius, 0, Math.PI * 2);
        
        // Color by state
        let color = '#60a5fa'; // default blue
        if (node.state === 'Ice') color = '#3b82f6';
        if (node.state === 'Water') color = '#06b6d4';
        if (node.state === 'Gas') color = '#8b5cf6';
        
        ctx.fillStyle = isHovered ? color : color + 'cc';
        ctx.fill();
        
        ctx.strokeStyle = isHovered ? '#ffffff' : color;
        ctx.lineWidth = isHovered ? 2 : 1;
        ctx.stroke();

        // Draw node title for larger nodes
        if (node.size > 8 || isHovered) {
          ctx.fillStyle = '#ffffff';
          ctx.font = `${isHovered ? 'bold ' : ''}10px sans-serif`;
          ctx.textAlign = 'center';
          ctx.textBaseline = 'top';
          const title = node.title.length > 20 ? node.title.substring(0, 20) + '...' : node.title;
          ctx.fillText(title, node.x, node.y + radius + 4);
        }
      });

      animationRef.current = requestAnimationFrame(render);
    };

    render();

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [graphData, hoveredItem, width, height]);

  if (loading) {
    return (
      <div className="flex items-center justify-center" style={{ width, height }}>
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-purple-500 mx-auto mb-4"></div>
          <div className="text-gray-400">Loading graph...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center" style={{ width, height }}>
          <div className="text-center text-red-400">
          <div className="text-xl mb-2">⚠️ Error</div>
          <div>{error}</div>
        </div>
      </div>
    );
  }

  const zoomLevelName = getZoomLevelName(currentZoomLevel);

  // Render drilldown panel if available
  const showDrilldownPanel = drilldownData && drilldownNodeId;

  return (
    <div className="relative" style={{ width, height }}>
      {/* Canvas */}
      <canvas
        ref={canvasRef}
        width={width}
        height={height}
        className="border border-gray-700 rounded-lg cursor-move"
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
        onClick={handleClick}
      />

      {/* Controls Overlay */}
      <div className="absolute top-4 left-4 space-y-2">
        <div className="bg-gray-900/90 backdrop-blur-sm rounded-lg p-3 border border-gray-700">
          <div className="text-xs text-gray-400 mb-2">Zoom Controls</div>
          <div className="flex flex-col space-y-1">
            <button
              onClick={handleZoomIn}
              className="px-3 py-1 bg-purple-600 hover:bg-purple-700 rounded text-sm transition-colors"
            >
              + Zoom In
            </button>
            <button
              onClick={handleZoomOut}
              className="px-3 py-1 bg-purple-600 hover:bg-purple-700 rounded text-sm transition-colors"
            >
              - Zoom Out
            </button>
            <button
              onClick={handleResetView}
              className="px-3 py-1 bg-gray-700 hover:bg-gray-600 rounded text-sm transition-colors"
            >
              Reset
            </button>
          </div>
        </div>
      </div>

      {/* Stats Overlay */}
      <div className="absolute top-4 right-4 bg-gray-900/90 backdrop-blur-sm rounded-lg p-3 border border-gray-700">
        <div className="text-xs space-y-1">
          <div className="flex items-center space-x-2">
            <span className="text-gray-400">View:</span>
            <span className="font-semibold text-purple-400">{zoomLevelName}</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-gray-400">Total Nodes:</span>
            <span className="text-white">{graphData?.totalNodesInGraph.toLocaleString()}</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-gray-400">Visible:</span>
            <span className="text-white">{graphData?.viewportNodeCount}</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-gray-400">Clusters:</span>
            <span className="text-white">{graphData?.clusterCount}</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-gray-400">Zoom:</span>
            <span className="text-white">{(zoomFactor * 100).toFixed(0)}%</span>
          </div>
        </div>
      </div>

      {/* Hover Tooltip */}
      {hoveredItem && (
        <div className="absolute bottom-4 left-4 bg-gray-900/95 backdrop-blur-sm rounded-lg p-3 border border-gray-700 max-w-xs">
          {'nodeCount' in hoveredItem ? (
            // Cluster tooltip
            <>
              <div className="text-sm font-semibold text-purple-400 mb-1">Cluster</div>
              <div className="text-xs text-gray-300">{hoveredItem.title}</div>
              <div className="text-xs text-gray-400 mt-1">{hoveredItem.nodeCount} nodes</div>
              <div className="text-xs text-gray-500 mt-1">Click to expand</div>
            </>
          ) : (
            // Node tooltip
            <>
              <div className="text-sm font-semibold text-blue-400 mb-1">Node</div>
              <div className="text-xs text-gray-300">{hoveredItem.title}</div>
              <div className="text-xs text-gray-400 mt-1">
                Type: {hoveredItem.typeId} • State: {hoveredItem.state}
              </div>
              <div className="text-xs text-gray-400">
                Connections: {hoveredItem.connectionCount}
              </div>
              <div className="text-xs text-gray-500 mt-1">Click for details</div>
            </>
          )}
        </div>
      )}

      {/* Zoom Level Indicator */}
      <div className="absolute bottom-4 right-4 bg-gray-900/90 backdrop-blur-sm rounded-lg px-3 py-2 border border-gray-700">
        <div className="flex items-center space-x-2">
          {currentZoomLevel === 0 && <span className="text-2xl">🌌</span>}
          {currentZoomLevel === 1 && <span className="text-2xl">🌍</span>}
          {currentZoomLevel === 2 && <span className="text-2xl">🔬</span>}
          <span className="text-sm text-gray-300">{zoomLevelName}</span>
        </div>
      </div>

      {/* Drilldown Panel */}
      {showDrilldownPanel && drilldownData && (
        <div className="absolute top-20 left-4 bg-gray-900/95 backdrop-blur-sm rounded-lg p-4 border border-gray-700 max-w-sm max-h-[500px] overflow-y-auto">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-semibold text-purple-400">Node Details</h3>
            <button
              onClick={() => setDrilldownNodeId(null)}
              className="text-gray-400 hover:text-white transition-colors"
            >
              ✕
            </button>
          </div>
          
          {/* Center Node */}
          <div className="bg-gray-800/50 rounded-lg p-3 mb-3 border border-blue-500">
            <div className="text-sm font-semibold text-blue-400">{drilldownData.centerNode.title}</div>
            <div className="text-xs text-gray-400 mt-1">{drilldownData.centerNode.typeId}</div>
            <div className="text-xs text-gray-500 mt-1">{drilldownData.centerNode.description}</div>
          </div>

          {/* Connection Stats */}
          <div className="grid grid-cols-2 gap-2 mb-3">
            <div className="bg-gray-800/30 rounded p-2 text-center">
              <div className="text-lg font-bold text-green-400">{drilldownData.outgoingCount}</div>
              <div className="text-xs text-gray-400">Outgoing</div>
            </div>
            <div className="bg-gray-800/30 rounded p-2 text-center">
              <div className="text-lg font-bold text-blue-400">{drilldownData.incomingCount}</div>
              <div className="text-xs text-gray-400">Incoming</div>
            </div>
          </div>

          {/* Connected Nodes */}
          <div>
            <div className="text-xs text-gray-400 mb-2">Connected Nodes ({drilldownData.nodes.length})</div>
            <div className="space-y-1">
              {drilldownData.nodes.slice(0, 10).map(node => (
                <button
                  key={node.id}
                  onClick={() => {
                    setDrilldownNodeId(node.id);
                    if (onNodeClick) onNodeClick(node.id);
                  }}
                  className="w-full text-left p-2 bg-gray-800/30 hover:bg-gray-700/50 rounded text-xs transition-colors"
                >
                  <div className="text-white truncate">{node.title}</div>
                  <div className="text-gray-500 truncate">{node.typeId}</div>
                </button>
              ))}
              {drilldownData.nodes.length > 10 && (
                <div className="text-xs text-gray-500 text-center py-1">
                  +{drilldownData.nodes.length - 10} more nodes
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Cluster Members Panel */}
      {expandedClusterId && clusterData && (
        <div className="absolute top-20 right-4 bg-gray-900/95 backdrop-blur-sm rounded-lg p-4 border border-gray-700 max-w-sm max-h-[500px] overflow-y-auto">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-semibold text-purple-400">Cluster Members</h3>
            <button
              onClick={() => setExpandedClusterId(null)}
              className="text-gray-400 hover:text-white transition-colors"
            >
              ✕
            </button>
          </div>
          
          <div className="text-xs text-gray-400 mb-3">
            {clusterData.count} nodes in cluster
          </div>

          <div className="space-y-1">
            {clusterData.members.map(member => (
              <button
                key={member.id}
                onClick={() => {
                  setDrilldownNodeId(member.id);
                  setExpandedClusterId(null);
                  if (onNodeClick) onNodeClick(member.id);
                }}
                className="w-full text-left p-2 bg-gray-800/30 hover:bg-gray-700/50 rounded text-xs transition-colors"
              >
                <div className="text-white truncate">{member.title}</div>
                <div className="text-gray-500 truncate">{member.typeId}</div>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
