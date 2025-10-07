'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';

export interface KnowledgeMapNode {
  id: string;
  title: string;
  domain: string;
  x: number;
  y: number;
  connections: string[];
  size: number;
  resonance: number;
}

interface KnowledgeMapProps {
  nodes: KnowledgeMapNode[];
  selectedNodeId?: string;
  onNodeClick?: (nodeId: string) => void;
  className?: string;
  // Optional explicit dimensions for deterministic sizing (useful in tests)
  dimensions?: { width: number; height: number };
}

export const KnowledgeMap: React.FC<KnowledgeMapProps> = ({ nodes, selectedNodeId, onNodeClick, className = '', dimensions: dimensionsProp }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [dimensions, setDimensions] = useState({ width: 800, height: 600 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });

  // Auto-resize canvas
  useEffect(() => {
    if (dimensionsProp) {
      setDimensions(dimensionsProp);
      return;
    }

    const updateDimensions = () => {
      const container = canvasRef.current?.parentElement;
      if (container) {
        setDimensions({
          width: container.clientWidth,
          height: Math.max(400, container.clientHeight)
        });
      }
    };

    updateDimensions();
    window.addEventListener('resize', updateDimensions);
    return () => window.removeEventListener('resize', updateDimensions);
  }, [dimensionsProp]);

  // Draw the knowledge map
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || nodes.length === 0) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const active = dimensionsProp ?? dimensions;

    // Clear canvas
    ctx.clearRect(0, 0, active.width, active.height);

    // Set canvas size
    canvas.width = active.width;
    canvas.height = active.height;

    // Draw connections first (behind nodes)
    drawConnections(ctx, nodes, active);

    // Draw nodes
    drawNodes(ctx, nodes, active, selectedNodeId);

  }, [nodes, dimensions, dimensionsProp, selectedNodeId]);

  const drawConnections = (ctx: CanvasRenderingContext2D, nodes: KnowledgeMapNode[], dimensions: { width: number; height: number }) => {
    const nodeMap = new Map(nodes.map(node => [node.id, node]));

    ctx.strokeStyle = 'rgba(59, 130, 246, 0.3)'; // Blue with transparency
    ctx.lineWidth = 2;

    nodes.forEach(node => {
      const connectionIds = Array.isArray(node.connections) ? node.connections : [];
      connectionIds.forEach(connectedId => {
        const connectedNode = nodeMap.get(connectedId);
        if (connectedNode && node.id < connectedId) { // Avoid duplicate lines
          // Calculate connection strength based on resonance
          const strength = Math.min(node.resonance, connectedNode.resonance) / 100;
          ctx.lineWidth = Math.max(1, strength * 3);

          ctx.beginPath();
          ctx.moveTo(node.x * dimensions.width, node.y * dimensions.height);
          ctx.lineTo(connectedNode.x * dimensions.width, connectedNode.y * dimensions.height);
          ctx.stroke();
        }
      });
    });
  };

  const drawNodes = (ctx: CanvasRenderingContext2D, nodes: KnowledgeMapNode[], dimensions: { width: number; height: number }, selectedNodeId?: string) => {
    try {
      const domainColors: Record<string, string> = {
        'Science & Tech': '#3B82F6',
        'Arts & Culture': '#8B5CF6',
        'Society': '#10B981',
        'Nature': '#059669',
        'Health': '#EF4444',
        'Business': '#F59E0B',
      };

      nodes.forEach(node => {
        const x = node.x * dimensions.width;
        const y = node.y * dimensions.height;
        const radius = Math.max(15, (node.size || 1) * 8);
        const isSelected = selectedNodeId === node.id;

        // Node shadow
        ctx.fillStyle = 'rgba(0, 0, 0, 0.1)';
        ctx.beginPath();
        ctx.arc(x + 2, y + 2, radius, 0, 2 * Math.PI);
        ctx.fill();

        // Node body
        ctx.fillStyle = domainColors[node.domain] || '#6B7280';
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, 2 * Math.PI);
        ctx.fill();

        // Node border for selected state
        if (isSelected) {
          ctx.strokeStyle = '#F59E0B';
          ctx.lineWidth = 3;
          ctx.stroke();
        }

        // Node glow effect based on resonance
        if (node.resonance > 70) {
        const gradient = ctx.createRadialGradient(x, y, radius, x, y, radius * 1.5);
        gradient.addColorStop(0, `${domainColors[node.domain]}40`);
        gradient.addColorStop(1, 'transparent');
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(x, y, radius * 1.5, 0, 2 * Math.PI);
        ctx.fill();
      }

      // Node label
      ctx.fillStyle = 'white';
      ctx.font = `${isSelected ? 'bold ' : ''}${Math.max(10, radius * 0.6)}px Arial`;
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';

      // Truncate long titles
      const maxLength = Math.floor(radius / 4);
      const displayTitle = node.title.length > maxLength
        ? node.title.substring(0, maxLength) + '...'
        : node.title;

      ctx.fillText(displayTitle, x, y);
    });
    } catch (error) {
      console.warn('Canvas drawing error:', error);
      // Continue rendering other elements even if canvas drawing fails
    }
  };

  const handleCanvasClick = (event: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // Find clicked node
    const clickedNode = nodes.find(node => {
      const active = dimensionsProp ?? dimensions;
      const nodeX = node.x * active.width;
      const nodeY = node.y * active.height;
      const distance = Math.sqrt((x - nodeX) ** 2 + (y - nodeY) ** 2);
      return distance <= Math.max(15, node.size * 8);
    });

    if (clickedNode) {
      onNodeClick?.(clickedNode.id);
    }
  };

  const handleMouseDown = (event: React.MouseEvent<HTMLCanvasElement>) => {
    setIsDragging(true);
    setDragOffset({ x: event.clientX, y: event.clientY });
  };

  const handleMouseMove = (event: React.MouseEvent<HTMLCanvasElement>) => {
    if (isDragging) {
      // Could implement pan functionality here
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  if (!Array.isArray(nodes) || nodes.length === 0) {
    return (
      <div className={`flex items-center justify-center bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`} style={{ height: '400px' }}>
        <div className="text-center">
          <div className="text-4xl mb-2">🕸️</div>
          <p className="text-gray-500 dark:text-gray-400">No nodes to display</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`relative ${className}`} data-testid="knowledge-map">
      <canvas
        ref={canvasRef}
        className="border border-gray-200 dark:border-gray-700 rounded-lg cursor-pointer"
        style={{ width: '100%', height: '400px' }}
        data-testid="map-canvas"
        tabIndex={0}
        onClick={handleCanvasClick}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
      />

      {/* Legend */}
      <div className="absolute bottom-4 left-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-3 shadow-lg" data-testid="map-legend">
        <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-2">Legend</h4>
        <div className="space-y-1 text-xs">
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-blue-500 rounded-full"></div>
            <span>Science & Technology</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-purple-500 rounded-full"></div>
            <span>Arts & Culture</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-green-500 rounded-full"></div>
            <span>Society & Humanity</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-emerald-500 rounded-full"></div>
            <span>Nature & Environment</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-red-500 rounded-full"></div>
            <span>Health & Wellness</span>
          </div>
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-amber-500 rounded-full"></div>
            <span>Business & Economics</span>
          </div>
        </div>
        <div className="mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Click nodes to explore • Lines show connections
          </p>
        </div>
      </div>

      {/* Stats */}
      <div className="absolute top-4 right-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-3 shadow-lg" data-testid="map-stats">
        <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-2">Stats</h4>
        <div className="text-xs text-gray-600 dark:text-gray-400">
          <div>Nodes: {nodes.length}</div>
          <div>Connections: {nodes.reduce((acc, node) => acc + ((node.connections && Array.isArray(node.connections)) ? node.connections.length : 0), 0) / 2}</div>
        </div>
      </div>
    </div>
  );
}

export default KnowledgeMap;

// Hook to generate mock knowledge nodes for demonstration
export function useMockKnowledgeNodes(count: number = 20): KnowledgeMapNode[] {
  return useMemo(() => {
    const nodes: KnowledgeMapNode[] = [];
    const domainMap: Record<string, string> = {
      science: 'Science & Tech',
      arts: 'Arts & Culture',
      society: 'Society',
      nature: 'Nature',
      health: 'Health',
      business: 'Business',
    };

    Object.values(domainMap).forEach((domain, index) => {
      nodes.push({
        id: `${domain.toLowerCase().replace(/[^a-z]/g, '-')}-root`,
        title: domain,
        domain,
        x: 0.15 + index * 0.12,
        y: 0.2,
        connections: [],
        size: 1,
        resonance: 80,
      });
    });

    const seedConcepts = [
      'Quantum Mechanics',
      'Machine Learning',
      'Renaissance Art',
      'Climate Change',
      'Mental Health',
      'Blockchain Economy',
      'Social Networks',
      'Ocean Biology',
      'Nutrition Science',
      'Digital Trade',
    ];

    seedConcepts.slice(0, count).forEach((concept, idx) => {
      const domainKeys = Object.keys(domainMap);
      const domain = domainMap[domainKeys[idx % domainKeys.length]];

      nodes.push({
        id: `concept-${idx}`,
        title: concept,
        domain,
        x: 0.2 + (idx % 5) * 0.15,
        y: 0.4 + Math.floor(idx / 5) * 0.2,
        connections: [],
        size: 0.8,
        resonance: 60 + (idx % 3) * 10,
      });
    });

    const nodeByDomain = nodes.reduce<Record<string, KnowledgeMapNode[]>>((acc, node) => {
      if (!acc[node.domain]) acc[node.domain] = [];
      acc[node.domain].push(node);
      return acc;
    }, {});

    Object.values(nodeByDomain).forEach((domainNodes) => {
      domainNodes.forEach((node, idx) => {
        const nextNode = domainNodes[(idx + 1) % domainNodes.length];
        if (node.id !== nextNode.id) {
          node.connections = Array.from(new Set([...node.connections, nextNode.id]));
        }
      });
    });

    for (let i = 0; i < nodes.length; i += 2) {
      const target = nodes[(i + 3) % nodes.length];
      if (target && !nodes[i].connections.includes(target.id)) {
        nodes[i].connections.push(target.id);
      }
    }

    return nodes;
  }, [count]);
}
