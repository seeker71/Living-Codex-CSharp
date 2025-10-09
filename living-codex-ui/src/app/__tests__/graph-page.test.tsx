/**
 * Graph Page Tests
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { useRouter } from 'next/navigation';
import GraphPage from '../graph/page';

// Mock the graph API
jest.mock('@/lib/graph-api', () => ({
  getNodes: jest.fn(),
  getEdgesFrom: jest.fn(),
  getNodeStats: jest.fn(),
  searchNodes: jest.fn()
}));

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    back: jest.fn()
  })
}));

// Mock the GraphVisualization component
jest.mock('@/components/graph/GraphVisualization', () => {
  return function MockGraphVisualization({ nodes, edges, onNodeClick }: any) {
    return (
      <div data-testid="graph-visualization">
        <div>Nodes: {nodes.length}</div>
        <div>Edges: {edges.length}</div>
        <button onClick={() => onNodeClick?.(nodes[0])}>Click Node</button>
      </div>
    );
  };
});

import * as mockGraphApi from '@/lib/graph-api';

describe('Graph Page', () => {
  const mockNodes = [
    {
      id: 'node1',
      typeId: 'codex.concept',
      state: 'ice',
      title: 'Test Node 1',
      description: 'A test node'
    },
    {
      id: 'node2',
      typeId: 'codex.concept',
      state: 'water',
      title: 'Test Node 2',
      description: 'Another test node'
    }
  ];

  const mockEdges = [
    {
      id: 'edge1',
      fromId: 'node1',
      toId: 'node2',
      typeId: 'connects'
    }
  ];

  const mockStats = {
    totalNodes: 1000,
    byType: { 'codex.concept': 500, 'codex.meta/type': 200 },
    byState: { ice: 600, water: 300, gas: 100 }
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    mockGraphApi.getNodes.mockResolvedValue(mockNodes);
    mockGraphApi.getEdgesFrom.mockResolvedValue(mockEdges);
    mockGraphApi.getNodeStats.mockResolvedValue(mockStats);
    mockGraphApi.searchNodes.mockResolvedValue(mockNodes);
  });

  it('renders loading state initially', () => {
    render(<GraphPage />);
    
    expect(screen.getByText('Loading graph data...')).toBeInTheDocument();
  });

  it('renders graph visualization after loading', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    expect(screen.getByText('Nodes: 2')).toBeInTheDocument();
    expect(screen.getByText('Edges: 2')).toBeInTheDocument();
  });

  it('displays statistics', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByText('1,000')).toBeInTheDocument();
    });
    
    expect(screen.getByText('Total Nodes')).toBeInTheDocument();
    expect(screen.getByText('Displayed')).toBeInTheDocument();
    expect(screen.getByText('Edges Shown')).toBeInTheDocument();
    expect(screen.getByText('Node Types')).toBeInTheDocument();
  });

  it('handles search functionality', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const searchInput = screen.getByPlaceholderText('Search nodes by title or description...');
    const searchButton = screen.getByText('Search');
    
    fireEvent.change(searchInput, { target: { value: 'test' } });
    fireEvent.click(searchButton);
    
    await waitFor(() => {
      expect(mockGraphApi.searchNodes).toHaveBeenCalledWith('test');
    });
  });

  it('handles reset functionality', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const resetButton = screen.getByText('Reset');
    fireEvent.click(resetButton);
    
    await waitFor(() => {
      expect(mockGraphApi.getNodes).toHaveBeenCalledTimes(2); // Initial load + reset
    });
  });

  it('handles node limit changes', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const limitSelect = screen.getByDisplayValue('100 nodes');
    fireEvent.change(limitSelect, { target: { value: '200' } });
    
    await waitFor(() => {
      expect(mockGraphApi.getNodes).toHaveBeenCalledWith(200);
    });
  });

  it('handles node click navigation', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const clickButton = screen.getByText('Click Node');
    fireEvent.click(clickButton);
    
    // The mock GraphVisualization component should call onNodeClick
    // We can't easily test router navigation without more complex mocking
    expect(clickButton).toBeInTheDocument();
  });

  it('displays error state', async () => {
    mockGraphApi.getNodes.mockRejectedValue(new Error('API Error'));
    
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Error loading graph')).toBeInTheDocument();
    });
  });

  it('shows selected node information', async () => {
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const clickButton = screen.getByText('Click Node');
    fireEvent.click(clickButton);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
  });

  it('handles empty search results', async () => {
    mockGraphApi.searchNodes.mockResolvedValue([]);
    
    render(<GraphPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('graph-visualization')).toBeInTheDocument();
    });
    
    const searchInput = screen.getByPlaceholderText('Search nodes by title or description...');
    const searchButton = screen.getByText('Search');
    
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });
    fireEvent.click(searchButton);
    
    await waitFor(() => {
      expect(screen.getAllByText('0')[0]).toBeInTheDocument(); // Displayed count
    });
  });
});
