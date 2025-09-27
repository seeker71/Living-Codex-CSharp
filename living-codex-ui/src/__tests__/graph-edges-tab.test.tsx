import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import GraphPage from '@/app/graph/page';
import '@testing-library/jest-dom';

// Mock the hooks
const mockUseAdvancedEdgeSearch = jest.fn();
const mockUseAdvancedNodeSearch = jest.fn();
const mockUseEdgeMetadata = jest.fn();
const mockUseNodeTypes = jest.fn();
const mockUseStorageStats = jest.fn();
const mockUseHealthStatus = jest.fn();

jest.mock('@/lib/hooks', () => ({
  useStorageStats: () => mockUseStorageStats(),
  useHealthStatus: () => mockUseHealthStatus(),
  useNodeTypes: () => mockUseNodeTypes(),
  useEdgeMetadata: () => mockUseEdgeMetadata(),
  useAdvancedNodeSearch: () => mockUseAdvancedNodeSearch(),
  useAdvancedEdgeSearch: () => mockUseAdvancedEdgeSearch()
}));

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(),
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn()
  })
}));

// Mock window.open
Object.defineProperty(window, 'open', {
  value: jest.fn(),
  writable: true
});

const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false }
  }
});

const renderWithProviders = (component: React.ReactElement) => {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {component}
    </QueryClientProvider>
  );
};

describe('Graph Edges Tab', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    
    // Set up default mock values
    mockUseStorageStats.mockReturnValue({
      data: { success: true, data: { stats: { nodeCount: 100, edgeCount: 50 } } },
      isLoading: false
    });
    mockUseHealthStatus.mockReturnValue({
      data: { success: true, data: { nodeCount: 100 } }
    });
    mockUseNodeTypes.mockReturnValue({
      data: { success: true, data: { nodeTypes: [{ typeId: 'concept' }, { typeId: 'user' }] } },
      isLoading: false
    });
    mockUseEdgeMetadata.mockReturnValue({
      data: { success: true, data: { roles: ['parent', 'child', 'related'], relationshipTypes: ['hierarchical', 'semantic'] } },
      isLoading: false
    });
    mockUseAdvancedNodeSearch.mockReturnValue({
      data: { success: true, data: { nodes: [], totalCount: 0 } },
      isLoading: false,
      refetch: jest.fn()
    });
    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { success: true, data: { edges: [], totalCount: 0 } },
      isLoading: false,
      refetch: jest.fn()
    });
  });

  it('renders edges tab when selected', () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Check if edges tab content is visible
    expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    expect(screen.getByText('Browse and filter edge relationships in the knowledge graph')).toBeInTheDocument();
  });

  it('displays edge filters correctly', () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Check filter controls
    expect(screen.getByLabelText(/role filter/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/relationship type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/from node id/i)).toBeInTheDocument();
    
    // Check filter options
    const roleSelect = screen.getByLabelText(/Role Filter/i);
    expect(roleSelect).toHaveValue('');
    
    const relationshipSelect = screen.getByLabelText(/Relationship Type/i);
    expect(relationshipSelect).toHaveValue('');
  });

  it('allows filtering by edge role', async () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Select a role filter
    const roleSelect = screen.getByLabelText(/Role Filter/i);
    fireEvent.change(roleSelect, { target: { value: 'parent' } });
    
    expect(roleSelect).toHaveValue('parent');
  });

  it('allows filtering by relationship type', async () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Select a relationship type filter
    const relationshipSelect = screen.getByLabelText(/Relationship Type/i);
    fireEvent.change(relationshipSelect, { target: { value: 'hierarchical' } });
    
    expect(relationshipSelect).toHaveValue('hierarchical');
  });

  it('allows searching by from node ID', async () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Enter search query
    const searchInput = screen.getByLabelText(/From Node ID/i);
    fireEvent.change(searchInput, { target: { value: 'node-123' } });
    
    expect(searchInput).toHaveValue('node-123');
  });

  it('shows active filters and clear button when filters are applied', async () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Apply filters
    const roleSelect = screen.getByLabelText(/Role Filter/i);
    fireEvent.change(roleSelect, { target: { value: 'parent' } });
    
    const searchInput = screen.getByLabelText(/From Node ID/i);
    fireEvent.change(searchInput, { target: { value: 'node-123' } });
    
    // Check if active filters are shown
    expect(screen.getByText(/active filters:/i)).toBeInTheDocument();
    expect(screen.getByText(/parent, node-123/i)).toBeInTheDocument();
    
    // Check if clear button is present
    expect(screen.getByText(/clear all filters/i)).toBeInTheDocument();
  });

  it('clears all filters when clear button is clicked', async () => {
    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Apply filters
    const roleSelect = screen.getByLabelText(/Role Filter/i);
    fireEvent.change(roleSelect, { target: { value: 'parent' } });
    
    const searchInput = screen.getByLabelText(/From Node ID/i);
    fireEvent.change(searchInput, { target: { value: 'node-123' } });
    
    // Clear filters
    const clearButton = screen.getByText(/clear all filters/i);
    fireEvent.click(clearButton);
    
    // Check if filters are cleared
    expect(roleSelect).toHaveValue('');
    expect(searchInput).toHaveValue('');
  });

  it('displays pagination controls with page size selector', async () => {
    // Mock edges data with enough items to trigger pagination
    const mockEdges = Array.from({ length: 30 }, (_, i) => ({
      fromId: `node-${i}`,
      toId: `node-${i + 1}`,
      role: 'related',
      weight: 0.5
    }));

    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: mockEdges,
          totalCount: 30
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Wait for edges to load and pagination to appear
    await waitFor(() => {
      expect(screen.getByText(/Showing 1 to 25 of 30/i)).toBeInTheDocument();
    });
    
    // Check if pagination controls are present
    expect(screen.getByText(/per page/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Previous/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Next/i })).toBeInTheDocument();
  });

  it('allows changing page size', async () => {
    // Mock edges data to show pagination
    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: Array.from({ length: 100 }, (_, i) => ({
            fromId: `node-${i}`,
            toId: `node-${i + 1}`,
            role: 'related',
            weight: 0.5
          })),
          totalCount: 100
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Check if page size selector is present
    const pageSizeSelect = screen.getByDisplayValue('25');
    expect(pageSizeSelect).toBeInTheDocument();
    
    // Change page size
    fireEvent.change(pageSizeSelect, { target: { value: '50' } });
    expect(pageSizeSelect).toHaveValue('50');
  });

  it('displays edge cards with proper information', async () => {
    // Mock edges data with actual edge objects
    const mockEdges = [
      {
        fromId: 'node-1',
        toId: 'node-2',
        role: 'parent',
        weight: 0.8,
        meta: { relationship: 'hierarchical' }
      },
      {
        fromId: 'node-3',
        toId: 'node-4',
        role: 'related',
        weight: 0.6,
        meta: { relationship: 'semantic' }
      }
    ];

    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: mockEdges,
          totalCount: 2
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Wait for edge cards to load
    await waitFor(() => {
      expect(screen.getByText('node-1')).toBeInTheDocument();
    });
    
    // Check if edge information is displayed in the edge cards (not in dropdowns)
    expect(screen.getAllByText('parent')).toHaveLength(2); // One in dropdown, one in edge card
    expect(screen.getAllByText('related')).toHaveLength(2); // One in dropdown, one in edge card
    expect(screen.getByText(/Weight: 0.8/i)).toBeInTheDocument();
    expect(screen.getByText(/Weight: 0.6/i)).toBeInTheDocument();
    expect(screen.getAllByText('hierarchical')).toHaveLength(2); // One in dropdown, one in edge card
    expect(screen.getAllByText('semantic')).toHaveLength(2); // One in dropdown, one in edge card
  });

  it('makes node IDs clickable and opens in new tab', async () => {
    const mockEdges = [
      {
        fromId: 'node-1',
        toId: 'node-2',
        role: 'parent',
        weight: 0.8
      }
    ];

    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: mockEdges,
          totalCount: 1
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Wait for edge cards to load
    await waitFor(() => {
      expect(screen.getByText('node-1')).toBeInTheDocument();
    });
    
    // Find and click on a node ID
    const nodeButton = screen.getByText('node-1');
    fireEvent.click(nodeButton);
    
    // Check if window.open was called with correct URL
    expect(window.open).toHaveBeenCalledWith('/node/node-1', '_blank');
  });

  it('handles URL encoding for node IDs with special characters', async () => {
    const mockEdges = [
      {
        fromId: 'node/with/slashes',
        toId: 'node?with=query&params',
        role: 'parent',
        weight: 0.8
      }
    ];

    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: mockEdges,
          totalCount: 1
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Wait for edge cards to load
    await waitFor(() => {
      expect(screen.getByText('node/with/slashes')).toBeInTheDocument();
    });
    
    // Find and click on a node ID with special characters
    const nodeButton = screen.getByText('node/with/slashes');
    fireEvent.click(nodeButton);
    
    // Check if window.open was called with URL-encoded ID
    expect(window.open).toHaveBeenCalledWith('/node/node%2Fwith%2Fslashes', '_blank');
  });

  it('displays empty state when no edges are found', async () => {
    const mockUseAdvancedEdgeSearch = jest.fn(() => ({
      data: { 
        success: true, 
        data: { 
          edges: [],
          totalCount: 0
        }
      },
      isLoading: false,
      refetch: jest.fn()
    }));

    jest.doMock('@/lib/hooks', () => ({
      ...jest.requireActual('@/lib/hooks'),
      useAdvancedEdgeSearch: mockUseAdvancedEdgeSearch
    }));

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Check if empty state is displayed
    expect(screen.getByText('No Edges Found')).toBeInTheDocument();
    expect(screen.getByText(/no edge relationships found in the system/i)).toBeInTheDocument();
  });

  it('displays loading state while fetching edges', async () => {
    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: null,
      isLoading: true,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Check if loading state is displayed (there should be multiple "Loading..." texts)
    expect(screen.getAllByText(/Loading.../i)).toHaveLength(2); // One in description, one in button
  });

  it('allows refreshing edge data', async () => {
    const mockRefetch = jest.fn();
    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: [],
          totalCount: 0
        }
      },
      isLoading: false,
      refetch: mockRefetch
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Click refresh button
    const refreshButton = screen.getByRole('button', { name: /Refresh/i });
    fireEvent.click(refreshButton);
    
    // Check if refetch was called
    expect(mockRefetch).toHaveBeenCalled();
  });

  it('handles pagination correctly', async () => {
    const mockEdges = Array.from({ length: 30 }, (_, i) => ({
      fromId: `node-${i}`,
      toId: `node-${i + 1}`,
      role: 'related',
      weight: 0.5
    }));

    mockUseAdvancedEdgeSearch.mockReturnValue({
      data: { 
        success: true, 
        data: { 
          edges: mockEdges.slice(0, 25), // First page
          totalCount: 30
        }
      },
      isLoading: false,
      refetch: jest.fn()
    });

    renderWithProviders(<GraphPage />);
    
    // Click on edges tab
    const edgesTab = screen.getByRole('button', { name: /Edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText(/Edge Explorer/i)).toBeInTheDocument();
    });
    
    // Check pagination info
    expect(screen.getByText(/Showing 1 to 25 of 30/i)).toBeInTheDocument();
    
    // Check if next button is enabled
    const nextButton = screen.getByRole('button', { name: /Next/i });
    expect(nextButton).not.toBeDisabled();
  });
});
