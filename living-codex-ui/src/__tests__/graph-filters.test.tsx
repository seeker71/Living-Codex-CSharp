import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import GraphPage from '@/app/graph/page';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Mock the API endpoints
const mockEdgeMetadata = {
  success: true,
  data: {
    roles: [
      'analyzed-as',
      'axis_child_of',
      'axis_has_dimension',
      'axis_has_keyword',
      'axis_parent_of',
      'belongs_to',
      'concept_on_axis',
      'contains',
      'contains-content',
      'defines',
      'exposes',
      'has-content',
      'has-summary',
      'has_child',
      'has_content_type',
      'identity',
      'instance-of',
      'is_a',
      'summarized-as'
    ],
    relationshipTypes: [
      'identity-self',
      'module-contains-file',
      'module-defines-record-type',
      'module-exposes-api',
      'node-has-content-type',
      'node-instance-of-type',
      'node-to-ontology',
      'ontology-contains-concept'
    ],
    totalRoles: 19,
    totalRelationshipTypes: 8
  }
};

const mockEdgesData = {
  success: true,
  data: {
    edges: [
      {
        fromId: 'node1',
        toId: 'node2',
        role: 'contains',
        weight: 0.8,
        meta: {
          relationship: 'module-contains-file'
        }
      },
      {
        fromId: 'node2',
        toId: 'node3',
        role: 'defines',
        weight: 0.9,
        meta: {
          relationship: 'module-defines-record-type'
        }
      }
    ],
    totalCount: 2
  }
};

// Mock fetch globally
global.fetch = jest.fn();

// Mock the hooks
jest.mock('@/lib/hooks', () => ({
  useStorageStats: () => ({
    data: { success: true, data: { stats: { nodeCount: 100, edgeCount: 50 } } },
    isLoading: false
  }),
  useHealthStatus: () => ({
    data: { success: true, data: { nodeCount: 100 } }
  }),
  useNodeTypes: () => ({
    data: { success: true, data: { nodeTypes: [{ typeId: 'codex.concept' }, { typeId: 'codex.module' }] } },
    isLoading: false
  }),
  useEdgeMetadata: () => ({
    data: mockEdgeMetadata,
    isLoading: false
  }),
  useAdvancedNodeSearch: () => ({
    data: { success: true, data: { nodes: [], totalCount: 0 } },
    isLoading: false,
    refetch: jest.fn()
  }),
  useAdvancedEdgeSearch: () => ({
    data: mockEdgesData,
    isLoading: false,
    refetch: jest.fn()
  })
}));

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(),
}));

describe('Graph Filters', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
      },
    });
    (global.fetch as jest.Mock).mockClear();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  const renderGraphPage = () => {
    return render(
      <QueryClientProvider client={queryClient}>
        <GraphPage />
      </QueryClientProvider>
    );
  };

  test('renders graph page with overview tab by default', () => {
    renderGraphPage();
    
    expect(screen.getByText('📊 Storage Overview')).toBeInTheDocument();
    expect(screen.getByText('🌊 Node State Distribution')).toBeInTheDocument();
  });

  test('switches to edges tab and shows filter controls', async () => {
    renderGraphPage();
    
    // Click on the Edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    // Wait for the edges tab content to load
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Check that filter controls are present
    expect(screen.getByText('Role')).toBeInTheDocument();
    expect(screen.getByText('Relationship Type')).toBeInTheDocument();
    expect(screen.getByText('Search From Node ID')).toBeInTheDocument();
  });

  test('populates role dropdown with data from server', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Check that role dropdown has options
    const roleSelect = screen.getByDisplayValue('All Roles');
    expect(roleSelect).toBeInTheDocument();
    
    // Click to open dropdown
    fireEvent.click(roleSelect);
    
    // Check that some expected roles are present in the dropdown
    await waitFor(() => {
      expect(screen.getByDisplayValue('All Roles')).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'analyzed-as' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'contains' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'defines' })).toBeInTheDocument();
    });
  });

  test('populates relationship type dropdown with data from server', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Check that relationship type dropdown has options
    const relationshipSelect = screen.getByDisplayValue('All Relationships');
    expect(relationshipSelect).toBeInTheDocument();
    
    // Click to open dropdown
    fireEvent.click(relationshipSelect);
    
    // Check that some expected relationship types are present in the dropdown
    await waitFor(() => {
      expect(screen.getByDisplayValue('All Relationships')).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'module-contains-file' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'module-defines-record-type' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'node-has-content-type' })).toBeInTheDocument();
    });
  });

  test('filters edges by role when role is selected', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Select a role
    const roleSelect = screen.getByDisplayValue('All Roles');
    fireEvent.change(roleSelect, { target: { value: 'contains' } });
    
    // Check that the filter was applied
    expect(roleSelect).toHaveValue('contains');
  });

  test('filters edges by relationship type when relationship is selected', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Select a relationship type
    const relationshipSelect = screen.getByDisplayValue('All Relationships');
    fireEvent.change(relationshipSelect, { target: { value: 'module-contains-file' } });
    
    // Check that the filter was applied
    expect(relationshipSelect).toHaveValue('module-contains-file');
  });

  test('shows clear filters button when filters are applied', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Initially, clear filters button should not be visible
    expect(screen.queryByText('Clear all filters')).not.toBeInTheDocument();
    
    // Select a role
    const roleSelect = screen.getByDisplayValue('All Roles');
    fireEvent.change(roleSelect, { target: { value: 'contains' } });
    
    // Now clear filters button should be visible
    await waitFor(() => {
      expect(screen.getByText('Clear all filters')).toBeInTheDocument();
    });
  });

  test('clears all filters when clear button is clicked', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Apply some filters
    const roleSelect = screen.getByDisplayValue('All Roles');
    const relationshipSelect = screen.getByDisplayValue('All Relationships');
    
    fireEvent.change(roleSelect, { target: { value: 'contains' } });
    fireEvent.change(relationshipSelect, { target: { value: 'module-contains-file' } });
    
    // Click clear filters
    const clearButton = screen.getByText('Clear all filters');
    fireEvent.click(clearButton);
    
    // Check that filters are cleared
    expect(roleSelect).toHaveValue('');
    expect(relationshipSelect).toHaveValue('');
  });

  test('displays edge data when available', async () => {
    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationships')).toBeInTheDocument();
    });
    
    // Check that edge data is displayed
    expect(screen.getByText('From:')).toBeInTheDocument();
    expect(screen.getByText('To:')).toBeInTheDocument();
    expect(screen.getByText('node1')).toBeInTheDocument();
    expect(screen.getByText('node2')).toBeInTheDocument();
  });

  test('shows loading state while fetching data', async () => {
    // Mock loading state
    jest.doMock('@/lib/hooks', () => ({
      useStorageStats: () => ({
        data: null,
        isLoading: true
      }),
      useHealthStatus: () => ({
        data: null,
        isLoading: true
      }),
      useNodeTypes: () => ({
        data: null,
        isLoading: true
      }),
      useEdgeMetadata: () => ({
        data: null,
        isLoading: true
      }),
      useAdvancedNodeSearch: () => ({
        data: null,
        isLoading: true,
        refetch: jest.fn()
      }),
      useAdvancedEdgeSearch: () => ({
        data: null,
        isLoading: true,
        refetch: jest.fn()
      })
    }));

    renderGraphPage();
    
    // Check for loading indicators
    expect(screen.getAllByText('Loading...')).toHaveLength(2); // One for stats, one for edges
  });

  test('handles empty edge metadata gracefully', async () => {
    // Mock empty edge metadata
    jest.doMock('@/lib/hooks', () => ({
      useStorageStats: () => ({
        data: { success: true, data: { stats: { nodeCount: 100, edgeCount: 50 } } },
        isLoading: false
      }),
      useHealthStatus: () => ({
        data: { success: true, data: { nodeCount: 100 } }
      }),
      useNodeTypes: () => ({
        data: { success: true, data: { nodeTypes: [{ typeId: 'codex.concept' }, { typeId: 'codex.module' }] } },
        isLoading: false
      }),
      useEdgeMetadata: () => ({
        data: { success: true, data: { roles: [], relationshipTypes: [] } },
        isLoading: false
      }),
      useAdvancedNodeSearch: () => ({
        data: { success: true, data: { nodes: [], totalCount: 0 } },
        isLoading: false,
        refetch: jest.fn()
      }),
      useAdvancedEdgeSearch: () => ({
        data: { success: true, data: { edges: [], totalCount: 0 } },
        isLoading: false,
        refetch: jest.fn()
      })
    }));

    renderGraphPage();
    
    // Switch to edges tab - text is split across spans
    const edgesTab = screen.getByRole('button', { name: /edges/i });
    fireEvent.click(edgesTab);
    
    await waitFor(() => {
      expect(screen.getByText('Edge Explorer')).toBeInTheDocument();
    });
    
    // Check that dropdowns are still present but empty
    const roleSelect = screen.getByDisplayValue('All Roles');
    const relationshipSelect = screen.getByDisplayValue('All Relationships');
    
    expect(roleSelect).toBeInTheDocument();
    expect(relationshipSelect).toBeInTheDocument();
  });
});
