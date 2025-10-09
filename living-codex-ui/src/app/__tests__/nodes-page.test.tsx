/**
 * Nodes Page Tests
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import NodesPage from '../nodes/page';

// Mock the graph API
jest.mock('@/lib/graph-api', () => ({
  getNodes: jest.fn(),
  getNodeStats: jest.fn(),
  getNodesByType: jest.fn(),
  searchNodes: jest.fn()
}));

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    back: jest.fn()
  })
}));

import * as mockGraphApi from '@/lib/graph-api';

describe('Nodes Page', () => {
  const mockNodes = [
    {
      id: 'node1',
      typeId: 'codex.concept',
      state: 'ice',
      title: 'Test Node 1',
      description: 'A test node',
      locale: 'en'
    },
    {
      id: 'node2',
      typeId: 'codex.concept',
      state: 'water',
      title: 'Test Node 2',
      description: 'Another test node',
      locale: 'en'
    },
    {
      id: 'node3',
      typeId: 'codex.meta/type',
      state: 'gas',
      title: 'Meta Node',
      description: 'A meta node',
      locale: 'en'
    }
  ];

  const mockStats = {
    totalNodes: 1000,
    byType: { 
      'codex.concept': 500, 
      'codex.meta/type': 200,
      'codex.content': 300
    },
    byState: { 
      ice: 600, 
      water: 300, 
      gas: 100 
    }
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    mockGraphApi.getNodes.mockResolvedValue(mockNodes);
    mockGraphApi.getNodeStats.mockResolvedValue(mockStats);
    mockGraphApi.getNodesByType.mockResolvedValue(mockNodes.slice(0, 2));
    mockGraphApi.searchNodes.mockResolvedValue(mockNodes.slice(0, 1));
  });

  it('renders loading state initially', () => {
    render(<NodesPage />);
    
    expect(screen.getByText('Loading nodes...')).toBeInTheDocument();
  });

  it('renders nodes list after loading', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    expect(screen.getByText('Test Node 2')).toBeInTheDocument();
    expect(screen.getByText('Meta Node')).toBeInTheDocument();
  });

  it('displays statistics', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('1,000')).toBeInTheDocument();
    });
    
    expect(screen.getByText('Total Nodes')).toBeInTheDocument();
    expect(screen.getAllByText('Ice (Persistent)')[0]).toBeInTheDocument();
    expect(screen.getAllByText('Water (Semi-persistent)')[0]).toBeInTheDocument();
    expect(screen.getAllByText('Gas (Transient)')[0]).toBeInTheDocument();
  });

  it('handles search functionality', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    const searchInput = screen.getByPlaceholderText('Search nodes...');
    const searchButton = screen.getByText('Search');
    
    fireEvent.change(searchInput, { target: { value: 'test' } });
    fireEvent.click(searchButton);
    
    await waitFor(() => {
      expect(mockGraphApi.searchNodes).toHaveBeenCalledWith('test', 200);
    });
  });

  it('handles reset functionality', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    const resetButton = screen.getByText('Reset');
    fireEvent.click(resetButton);
    
    await waitFor(() => {
      expect(mockGraphApi.getNodes).toHaveBeenCalledTimes(2); // Initial load + reset
    });
  });

  it('handles type filtering', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    const typeSelect = screen.getByDisplayValue('All Types');
    fireEvent.change(typeSelect, { target: { value: 'codex.concept' } });
    
    await waitFor(() => {
      expect(mockGraphApi.getNodesByType).toHaveBeenCalledWith('codex.concept');
    });
  });

  it('handles state filtering', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    const stateSelect = screen.getByDisplayValue('All States');
    fireEvent.change(stateSelect, { target: { value: 'ice' } });
    
    await waitFor(() => {
      expect(mockGraphApi.getNodes).toHaveBeenCalledWith(1000);
    });
  });

  it('handles pagination', async () => {
    // Mock more nodes for pagination
    const manyNodes = Array(150).fill(null).map((_, i) => ({
      id: `node${i}`,
      typeId: 'codex.concept',
      state: 'ice',
      title: `Node ${i}`,
      description: `Description ${i}`,
      locale: 'en'
    }));
    
    mockGraphApi.getNodes.mockResolvedValue(manyNodes);
    
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Showing 1 - 50 of 150 nodes')).toBeInTheDocument();
    });
    
    const nextButton = screen.getByText('Next →');
    fireEvent.click(nextButton);
    
    await waitFor(() => {
      expect(screen.getByText('Showing 51 - 100 of 150 nodes')).toBeInTheDocument();
    });
  });

  it('handles node click navigation', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    // Find the actual clickable card element
    const nodeCard = screen.getByText('Test Node 1').closest('.cursor-pointer');
    expect(nodeCard).toBeInTheDocument();
    
    fireEvent.click(nodeCard!);
    
    // The node card should be clickable and have the correct structure
    expect(nodeCard).toHaveClass('cursor-pointer');
  });

  it('displays error state', async () => {
    mockGraphApi.getNodes.mockRejectedValue(new Error('API Error'));
    
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Error loading nodes')).toBeInTheDocument();
    });
  });

  it('shows no results message when empty', async () => {
    mockGraphApi.searchNodes.mockResolvedValue([]);
    
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Node 1')).toBeInTheDocument();
    });
    
    const searchInput = screen.getByPlaceholderText('Search nodes...');
    const searchButton = screen.getByText('Search');
    
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });
    fireEvent.click(searchButton);
    
    await waitFor(() => {
      expect(screen.getByText('No nodes found')).toBeInTheDocument();
    });
  });

  it('displays node state badges correctly', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getByText('ice')).toBeInTheDocument();
      expect(screen.getByText('water')).toBeInTheDocument();
      expect(screen.getByText('gas')).toBeInTheDocument();
    });
  });

  it('shows correct node type information', async () => {
    render(<NodesPage />);
    
    await waitFor(() => {
      expect(screen.getAllByText('codex.concept')[0]).toBeInTheDocument();
      expect(screen.getByText('codex.meta/type')).toBeInTheDocument();
    });
  });
});
