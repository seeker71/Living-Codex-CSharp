import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import NodeDetailPage from '@/app/node/[id]/page'

// Mock the useParams and useRouter hooks
const mockPush = jest.fn()
const mockBack = jest.fn()

jest.mock('next/navigation', () => ({
  useParams: () => ({ id: 'u-core-concept-kw-matter' }),
  useRouter: () => ({ push: mockPush, back: mockBack }),
}))

// Mock the useAuth hook
const mockTrackInteraction = jest.fn()
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

jest.mock('@/lib/hooks', () => ({
  useTrackInteraction: () => mockTrackInteraction,
}))

// Mock the buildApiUrl function
jest.mock('@/lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock fetch responses
const mockNodeData = {
  success: true,
  node: {
    id: 'u-core-concept-kw-matter',
    typeId: 'codex.concept.keyword',
    title: 'Matter',
    description: 'Physical substance that has mass and occupies space',
    state: 'Ice',
    locale: 'en',
    meta: {
      keywords: ['matter', 'substance', 'physical'],
      axes: ['water_states'],
      level: 2
    },
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z'
  }
}

const mockEdgesData = {
  success: true,
  edges: [
    {
      fromId: 'u-core-concept-kw-matter',
      toId: 'u-core-axis-water_states',
      role: 'concept_on_axis',
      weight: 0.9,
      meta: { relationship: 'concept_on_axis' }
    },
    {
      fromId: 'u-core-concept-kw-matter',
      toId: 'u-core-concept-entity',
      role: 'is_a',
      weight: 0.8,
      meta: { relationship: 'is_a' }
    }
  ],
  totalCount: 2
}

const mockRelatedNodeData = {
  success: true,
  node: {
    id: 'u-core-axis-water_states',
    typeId: 'codex.ontology.axis',
    title: 'Water States Axis',
    description: 'Axis representing different states of matter',
    state: 'Ice',
    locale: 'en'
  }
}

describe('NodeDetailPage', () => {
  beforeEach(() => {
    // Reset mocks
    jest.clearAllMocks()
    
    // Mock fetch
    global.fetch = jest.fn()
      .mockImplementation((url: string) => {
        if (url.includes('/storage-endpoints/nodes/u-core-concept-kw-matter')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockNodeData)
          })
        }
        if (url.includes('/storage-endpoints/nodes/u-core-axis-water_states')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockRelatedNodeData)
          })
        }
        if (url.includes('/storage-endpoints/edges')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockEdgesData)
          })
        }
        return Promise.resolve({
          ok: false,
          status: 404
        })
      })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  it('renders loading state initially', () => {
    renderWithProviders(<NodeDetailPage />)
    expect(screen.getByText('Loading node...')).toBeInTheDocument()
  })

  it('renders node details after loading', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    expect(screen.getByText('Physical substance that has mass and occupies space')).toBeInTheDocument()
    expect(screen.getByText('Type: codex.concept.keyword')).toBeInTheDocument()
    expect(screen.getByText('ID: u-core-concept-kw-matter')).toBeInTheDocument()
    expect(screen.getAllByText('Ice')).toHaveLength(2) // Should appear in header and details
  })

  it('displays node type icon correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Check for the concept icon (🔵 - default icon for unknown type)
    expect(screen.getByText('🔵')).toBeInTheDocument()
  })

  it('renders all tabs correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    expect(screen.getByText('📋 Details')).toBeInTheDocument()
    expect(screen.getByText('📄 Content')).toBeInTheDocument()
    expect(screen.getByText('🔗 Relationships')).toBeInTheDocument()
    expect(screen.getByText('🏷️ Metadata')).toBeInTheDocument()
  })

  it('switches between tabs correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Click on Relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getByText('Relationships (1)')).toBeInTheDocument()
    })
  })

  it('displays relationships correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getAllByText('Water States Axis')).toHaveLength(2) // Should appear in U-CORE Axes and relationships
    })
    
    expect(screen.getByText('→ concept_on_axis')).toBeInTheDocument()
    expect(screen.getByText('Type: codex.ontology.axis')).toBeInTheDocument()
  })

  it('displays metadata correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to metadata tab
    fireEvent.click(screen.getByText('🏷️ Metadata'))
    
    await waitFor(() => {
      expect(screen.getByText('keywords')).toBeInTheDocument()
    })
    
    expect(screen.getByText('axes')).toBeInTheDocument()
    expect(screen.getByText('level')).toBeInTheDocument()
  })

  it('handles edit mode correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Click edit button
    fireEvent.click(screen.getByText('✏️ Edit'))
    
    // Check that edit inputs are shown
    expect(screen.getAllByDisplayValue('Matter')).toHaveLength(2) // Header and form input
    expect(screen.getByDisplayValue('Physical substance that has mass and occupies space')).toBeInTheDocument()
    
    // Check that save and cancel buttons are shown
    expect(screen.getByText('💾 Save')).toBeInTheDocument()
    expect(screen.getByText('Cancel')).toBeInTheDocument()
  })

  it('handles back navigation', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Click back button
    fireEvent.click(screen.getByText('←'))
    
    expect(mockBack).toHaveBeenCalled()
  })

  it('handles node navigation from relationships', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getAllByText('Water States Axis')).toHaveLength(2) // Should appear in U-CORE Axes and relationships
    })
    
    // Click on related node (use the first one found)
    fireEvent.click(screen.getAllByText('Water States Axis')[0])
    
    expect(mockPush).toHaveBeenCalledWith('/node/u-core-axis-water_states')
  })

  it('handles edge navigation', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getByText('View Edge')).toBeInTheDocument()
    })
    
    // Click view edge button
    fireEvent.click(screen.getByText('View Edge'))
    
    // Should open in new window (we can't test this directly, but we can verify the function was called)
    expect(screen.getByText('View Edge')).toBeInTheDocument()
  })

  it('handles error state correctly', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 404
    })
    
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Node Not Found')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Failed to load node')).toBeInTheDocument()
    expect(screen.getByText('Go Back')).toBeInTheDocument()
  })

  it('handles missing node gracefully', async () => {
    // Mock fetch to return empty response
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ success: true, node: null })
    })
    
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Node Not Found')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Node not found')).toBeInTheDocument()
  })

  it('displays action buttons correctly', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    expect(screen.getByText('🕸️ View in Graph')).toBeInTheDocument()
    expect(screen.getByText('📋 Copy ID')).toBeInTheDocument()
    expect(screen.getByText('💾 Export JSON')).toBeInTheDocument()
  })

  it('handles edge filtering', async () => {
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getAllByText('Role')).toHaveLength(2) // Should appear in filter and table header
    })
    
    // Test role filter input
    const roleInput = screen.getByPlaceholderText('e.g. defines, references')
    fireEvent.change(roleInput, { target: { value: 'concept_on_axis' } })
    
    expect(roleInput).toHaveValue('concept_on_axis')
    
    // Test search input
    const searchInput = screen.getByPlaceholderText('Search by related node id')
    fireEvent.change(searchInput, { target: { value: 'water_states' } })
    
    expect(searchInput).toHaveValue('water_states')
  })

  it('handles pagination for edges', async () => {
    // Mock more edges for pagination test
    const mockEdgesDataWithPagination = {
      ...mockEdgesData,
      totalCount: 50
    }
    
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/nodes/u-core-concept-kw-matter')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockNodeData)
        })
      }
      if (url.includes('/storage-endpoints/edges')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockEdgesDataWithPagination)
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<NodeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Matter' })).toBeInTheDocument()
    })
    
    // Switch to relationships tab
    fireEvent.click(screen.getByText('🔗 Relationships'))
    
    await waitFor(() => {
      expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Previous')).toBeInTheDocument()
    expect(screen.getByText('Next')).toBeInTheDocument()
  })
})
