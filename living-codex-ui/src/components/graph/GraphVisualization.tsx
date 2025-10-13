'use client';

/**
 * Graph Visualization Component
 * Interactive force-directed graph showing nodes and edges
 * Exposes the "Everything is a Node" architecture
 */

import React, { useEffect, useRef, useState } from 'react';
import { Node, Edge } from '@/lib/graph-api';

interface GraphVisualizationProps {
  nodes: Node[];
  edges: Edge[];
  onNodeClick?: (node: Node) => void;
  width?: number;
  height?: number;
}

interface GraphNode extends Node {
  x: number;
  y: number;
  vx: number;
  vy: number;
}

export default function GraphVisualization({
  nodes,
  edges,
  onNodeClick,
  width = 800,
  height = 600,
}: GraphVisualizationProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const graphNodesRef = useRef<GraphNode[]>([]);
  const [hoveredNode, setHoveredNode] = useState<GraphNode | null>(null);
  const [selectedNode, setSelectedNode] = useState<GraphNode | null>(null);
  const animationRef = useRef<number | null>(null);
  const edgeMapRef = useRef<Map<string, string[]>>(new Map());

  // Initialize graph nodes with physics properties
  useEffect(() => {
    const initialized = nodes.map(node => ({
      ...node,
      x: Math.random() * width,
      y: Math.random() * height,
      vx: 0,
      vy: 0,
    }));
    graphNodesRef.current = initialized;
    
    // Build edge map once
    const edgeMap = new Map<string, string[]>();
    edges.forEach(edge => {
      if (!edgeMap.has(edge.fromId)) {
        edgeMap.set(edge.fromId, []);
      }
      edgeMap.get(edge.fromId)!.push(edge.toId);
    });
    edgeMapRef.current = edgeMap;
  }, [nodes, edges, width, height]);

  // Physics simulation
  useEffect(() => {
    if (graphNodesRef.current.length === 0) return;

    const animate = () => {
      const canvas = canvasRef.current;
      if (!canvas) return;

      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      const graphNodes = graphNodesRef.current;
      const edgeMap = edgeMapRef.current;

      // Clear canvas
      ctx.clearRect(0, 0, width, height);

      // Apply forces
      const alpha = 0.3;

      // Update node positions with simple physics
      graphNodes.forEach((node, i) => {
        // Center gravity
        const centerX = width / 2;
        const centerY = height / 2;
        const dx = centerX - node.x;
        const dy = centerY - node.y;
        const distToCenter = Math.sqrt(dx * dx + dy * dy);
        
        if (distToCenter > 0) {
          node.vx += (dx / distToCenter) * 0.1;
          node.vy += (dy / distToCenter) * 0.1;
        }

        // Repulsion from other nodes
        graphNodes.forEach((other, j) => {
          if (i === j) return;
          
          const dx = other.x - node.x;
          const dy = other.y - node.y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          
          if (dist < 100 && dist > 0) {
            const force = 100 / (dist * dist);
            node.vx -= (dx / dist) * force;
            node.vy -= (dy / dist) * force;
          }
        });

        // Attraction to connected nodes
        const connectedIds = edgeMap.get(node.id) || [];
        connectedIds.forEach(toId => {
          const other = graphNodes.find(n => n.id === toId);
          if (!other) return;
          
          const dx = other.x - node.x;
          const dy = other.y - node.y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          
          if (dist > 0) {
            const force = 0.01;
            node.vx += (dx / dist) * force;
            node.vy += (dy / dist) * force;
          }
        });

        // Apply velocity with damping
        node.x += node.vx * alpha;
        node.y += node.vy * alpha;
        node.vx *= 0.9;
        node.vy *= 0.9;

        // Keep in bounds
        node.x = Math.max(20, Math.min(width - 20, node.x));
        node.y = Math.max(20, Math.min(height - 20, node.y));
      });

      // Draw edges
      ctx.strokeStyle = 'rgba(100, 100, 100, 0.2)';
      ctx.lineWidth = 1;
      
      edges.forEach(edge => {
        const from = graphNodes.find(n => n.id === edge.fromId);
        const to = graphNodes.find(n => n.id === edge.toId);
        
        if (from && to) {
          ctx.beginPath();
          ctx.moveTo(from.x, from.y);
          ctx.lineTo(to.x, to.y);
          ctx.stroke();
        }
      });

      // Draw nodes
      graphNodes.forEach(node => {
        const isHovered = hoveredNode?.id === node.id;
        const isSelected = selectedNode?.id === node.id;
        
        // Node color by state
        const colors = {
          ice: '#60a5fa', // blue
          water: '#34d399', // green
          gas: '#f87171', // red
        };
        
        const radius = isSelected ? 8 : isHovered ? 6 : 4;
        
        ctx.beginPath();
        ctx.arc(node.x, node.y, radius, 0, 2 * Math.PI);
        ctx.fillStyle = colors[node.state] || '#gray';
        ctx.fill();
        
        if (isHovered || isSelected) {
          ctx.strokeStyle = '#fff';
          ctx.lineWidth = 2;
          ctx.stroke();
        }
      });

      animationRef.current = requestAnimationFrame(animate);
    };

    animationRef.current = requestAnimationFrame(animate);

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [edges, hoveredNode, selectedNode, width, height]);

  // Mouse interaction
  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    const found = graphNodesRef.current.find(node => {
      const dx = node.x - x;
      const dy = node.y - y;
      return Math.sqrt(dx * dx + dy * dy) < 10;
    });

    setHoveredNode(found || null);
  };

  const handleClick = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (hoveredNode) {
      setSelectedNode(hoveredNode);
      onNodeClick?.(hoveredNode);
    }
  };

  return (
    <div className="relative">
      <canvas
        ref={canvasRef}
        width={width}
        height={height}
        className="border border-gray-300 rounded-lg bg-gray-900 cursor-pointer"
        onMouseMove={handleMouseMove}
        onClick={handleClick}
      />
      
      {/* Node tooltip */}
      {hoveredNode && (
        <div className="absolute top-2 left-2 bg-black/80 text-white p-3 rounded-lg max-w-sm">
          <div className="font-semibold text-sm">{hoveredNode.title}</div>
          <div className="text-xs text-gray-300 mt-1">{hoveredNode.typeId}</div>
          <div className="text-xs text-gray-400 mt-1">
            State: <span className="font-mono">{hoveredNode.state}</span>
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="absolute bottom-2 right-2 bg-black/80 text-white p-3 rounded-lg text-xs">
        <div className="font-semibold mb-2">Node States</div>
        <div className="flex items-center gap-2 mb-1">
          <div className="w-3 h-3 rounded-full bg-blue-400"></div>
          <span>Ice (Persistent)</span>
        </div>
        <div className="flex items-center gap-2 mb-1">
          <div className="w-3 h-3 rounded-full bg-green-400"></div>
          <span>Water (Semi-persistent)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-red-400"></div>
          <span>Gas (Transient)</span>
        </div>
      </div>
    </div>
  );
}

