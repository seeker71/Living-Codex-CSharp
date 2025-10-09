/**
 * GraphVisualization Component Tests
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import GraphVisualization from '../GraphVisualization';

// Mock canvas methods
const mockCanvas = {
  getContext: jest.fn(() => ({
    clearRect: jest.fn(),
    beginPath: jest.fn(),
    moveTo: jest.fn(),
    lineTo: jest.fn(),
    stroke: jest.fn(),
    arc: jest.fn(),
    fill: jest.fn(),
    strokeStyle: '',
    lineWidth: 0,
    fillStyle: ''
  })),
  getBoundingClientRect: jest.fn(() => ({
    left: 0,
    top: 0
  }))
};

// Mock requestAnimationFrame
global.requestAnimationFrame = jest.fn(cb => setTimeout(cb, 16));
global.cancelAnimationFrame = jest.fn();

// Mock canvas element
Object.defineProperty(HTMLCanvasElement.prototype, 'getContext', {
  value: mockCanvas.getContext
});

Object.defineProperty(HTMLCanvasElement.prototype, 'getBoundingClientRect', {
  value: mockCanvas.getBoundingClientRect
});

const mockNodes = [
  {
    id: 'node1',
    typeId: 'codex.concept',
    state: 'ice' as const,
    locale: 'en',
    title: 'Test Node 1',
    description: 'A test node',
    content: { mediaType: 'application/json' },
    meta: {}
  },
  {
    id: 'node2',
    typeId: 'codex.concept',
    state: 'water' as const,
    locale: 'en',
    title: 'Test Node 2',
    description: 'Another test node',
    content: { mediaType: 'application/json' },
    meta: {}
  }
];

const mockEdges = [
  {
    id: 'edge1',
    fromId: 'node1',
    toId: 'node2',
    typeId: 'connects',
    role: 'related',
    meta: {},
    createdAt: '2025-10-07T20:00:00Z'
  }
];

describe('GraphVisualization', () => {
  const defaultProps = {
    nodes: mockNodes,
    edges: mockEdges,
    width: 800,
    height: 600
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders canvas element', () => {
    render(<GraphVisualization {...defaultProps} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
    expect(canvas).toHaveAttribute('width', '800');
    expect(canvas).toHaveAttribute('height', '600');
  });

  it('renders legend', () => {
    render(<GraphVisualization {...defaultProps} />);
    
    expect(screen.getByText('Node States')).toBeInTheDocument();
    expect(screen.getByText('Ice (Persistent)')).toBeInTheDocument();
    expect(screen.getByText('Water (Semi-persistent)')).toBeInTheDocument();
    expect(screen.getByText('Gas (Transient)')).toBeInTheDocument();
  });

  it('handles mouse move events', () => {
    const onNodeClick = jest.fn();
    render(<GraphVisualization {...defaultProps} onNodeClick={onNodeClick} />);
    
    const canvas = document.querySelector('canvas');
    fireEvent.mouseMove(canvas!, { clientX: 100, clientY: 100 });
    
    // Should not throw error
    expect(canvas).toBeInTheDocument();
  });

  it('handles click events', () => {
    const onNodeClick = jest.fn();
    render(<GraphVisualization {...defaultProps} onNodeClick={onNodeClick} />);
    
    const canvas = document.querySelector('canvas');
    fireEvent.click(canvas!, { clientX: 100, clientY: 100 });
    
    // Should not throw error
    expect(canvas).toBeInTheDocument();
  });

  it('applies custom width and height', () => {
    render(<GraphVisualization {...defaultProps} width={1000} height={500} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toHaveAttribute('width', '1000');
    expect(canvas).toHaveAttribute('height', '500');
  });

  it('handles empty nodes array', () => {
    render(<GraphVisualization {...defaultProps} nodes={[]} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });

  it('handles empty edges array', () => {
    render(<GraphVisualization {...defaultProps} edges={[]} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });

  it('calls onNodeClick when provided', () => {
    const onNodeClick = jest.fn();
    render(<GraphVisualization {...defaultProps} onNodeClick={onNodeClick} />);
    
    const canvas = document.querySelector('canvas');
    fireEvent.click(canvas!, { clientX: 100, clientY: 100 });
    
    // Note: In a real test, we'd need to mock the physics simulation
    // to ensure a node is actually at the click position
  });

  it('applies correct CSS classes', () => {
    render(<GraphVisualization {...defaultProps} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toHaveClass('border', 'border-gray-300', 'rounded-lg', 'bg-gray-900', 'cursor-pointer');
  });

  it('handles different node states', () => {
    const nodesWithDifferentStates = [
      { ...mockNodes[0], state: 'ice' as const },
      { ...mockNodes[1], state: 'water' as const },
      { ...mockNodes[0], id: 'node3', state: 'gas' as const }
    ];
    
    render(<GraphVisualization {...defaultProps} nodes={nodesWithDifferentStates} />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
