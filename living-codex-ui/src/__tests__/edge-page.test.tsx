import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import EdgeDetailPage from '@/app/edge/[fromId]/[toId]/page'

// Mock the useParams and useRouter hooks
const mockPush = jest.fn()
const mockBack = jest.fn()

jest.mock('next/navigation', () => ({
  useParams: () => ({ fromId: 'u-core-concept-kw-matter', toId: 'u-core-axis-water_states' }),
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

// Mock the Navigation component
jest.mock('@/components/ui/Navigation', () => ({
  Navigation: () => <div data-testid="navigation">Navigation</div>
}))

// Mock fetch responses
const mockEdgeData = {
  success: true,
  edge: {
    fromId: 'u-core-concept-kw-matter',
    toId: 'u-core-axis-water_states',
    role: 'concept_on_axis',
    weight: 0.9,
    meta: {
      relationship: 'concept_on_axis',
      createdBy: 'system',
      confidence: 0.95
    }
  }
}

const mockFromNodeData = {
  success: true,
  node: {
    id: 'u-core-concept-kw-matter',
    typeId: 'codex.concept.keyword',
    title: 'Matter',
    description: 'Physical substance that has mass and occupies space',
    state: 'Ice',
    locale: 'en'
  }
}

const mockToNodeData = {
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

describe('EdgeDetailPage', () => {
  beforeEach(() => {
    // Reset mocks
    jest.clearAllMocks()
    
    // Mock fetch
    global.fetch = jest.fn()
      .mockImplementation((url: string) => {
        if (url.includes('/storage-endpoints/edges/u-core-concept-kw-matter/u-core-axis-water_states')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockEdgeData)
          })
        }
        if (url.includes('/storage-endpoints/nodes/u-core-concept-kw-matter')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockFromNodeData)
          })
        }
        if (url.includes('/storage-endpoints/nodes/u-core-axis-water_states')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockToNodeData)
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
    renderWithProviders(<EdgeDetailPage />)
    expect(screen.getByText('Loading edge...')).toBeInTheDocument()
  })

  it('renders edge details after loading', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getAllByText('concept_on_axis')).toHaveLength(2) // Badge and metadata
    expect(screen.getByText('Weight: 0.9')).toBeInTheDocument()
    expect(screen.getByText('From: u-core-concept-kw-matter')).toBeInTheDocument()
    expect(screen.getByText('To: u-core-axis-water_states')).toBeInTheDocument()
  })

  it('displays from and to nodes correctly', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    // Check from node
    expect(screen.getByText('From Node')).toBeInTheDocument()
    expect(screen.getByText('Matter')).toBeInTheDocument()
    expect(screen.getByText('codex.concept.keyword')).toBeInTheDocument()
    expect(screen.getByText('Physical substance that has mass and occupies space')).toBeInTheDocument()
    
    // Check to node
    expect(screen.getByText('To Node')).toBeInTheDocument()
    expect(screen.getByText('Water States Axis')).toBeInTheDocument()
    expect(screen.getByText('codex.ontology.axis')).toBeInTheDocument()
    expect(screen.getByText('Axis representing different states of matter')).toBeInTheDocument()
  })

  it('displays node type icons correctly', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    // Check for concept icon (🔵) and axis icon (🌟)
    expect(screen.getByText('🔵')).toBeInTheDocument()
    expect(screen.getByText('🌟')).toBeInTheDocument()
  })

  it('displays edge metadata correctly', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Edge Metadata')).toBeInTheDocument()
    expect(screen.getByText('relationship')).toBeInTheDocument()
    expect(screen.getByText('createdBy')).toBeInTheDocument()
    expect(screen.getByText('confidence')).toBeInTheDocument()
  })

  it('handles back navigation', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    // Click back button
    fireEvent.click(screen.getByText('←'))
    
    expect(mockBack).toHaveBeenCalled()
  })

  it('handles node navigation from from/to nodes', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    // Click on from node
    fireEvent.click(screen.getByText('Matter'))
    
    expect(mockPush).toHaveBeenCalledWith('/node/u-core-concept-kw-matter')
    
    // Click on to node
    fireEvent.click(screen.getByText('Water States Axis'))
    
    expect(mockPush).toHaveBeenCalledWith('/node/u-core-axis-water_states')
  })

  it('handles error state correctly', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 404
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Not Found')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Failed to load edge')).toBeInTheDocument()
    expect(screen.getByText('Go Back')).toBeInTheDocument()
  })

  it('handles missing edge gracefully', async () => {
    // Mock fetch to return empty response
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/edges/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, edge: null })
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Not Found')).toBeInTheDocument()
    })
    
    expect(screen.getByText('Edge not found')).toBeInTheDocument()
  })

  it('displays action buttons correctly', async () => {
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getByText('🕸️ View in Graph')).toBeInTheDocument()
    expect(screen.getByText('📋 Copy Edge ID')).toBeInTheDocument()
    expect(screen.getByText('💾 Export JSON')).toBeInTheDocument()
    expect(screen.getByText('📤 View From Node')).toBeInTheDocument()
    expect(screen.getByText('📥 View To Node')).toBeInTheDocument()
  })

  it('handles missing from node gracefully', async () => {
    // Mock fetch to return error for from node
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/edges/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockEdgeData)
        })
      }
      if (url.includes('/storage-endpoints/nodes/u-core-concept-kw-matter')) {
        return Promise.resolve({
          ok: false,
          status: 404
        })
      }
      if (url.includes('/storage-endpoints/nodes/u-core-axis-water_states')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockToNodeData)
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getByText('From node not found')).toBeInTheDocument()
    expect(screen.getByText('ID: u-core-concept-kw-matter')).toBeInTheDocument()
  })

  it('handles missing to node gracefully', async () => {
    // Mock fetch to return error for to node
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/edges/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockEdgeData)
        })
      }
      if (url.includes('/storage-endpoints/nodes/u-core-concept-kw-matter')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockFromNodeData)
        })
      }
      if (url.includes('/storage-endpoints/nodes/u-core-axis-water_states')) {
        return Promise.resolve({
          ok: false,
          status: 404
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getByText('To node not found')).toBeInTheDocument()
    expect(screen.getByText('ID: u-core-axis-water_states')).toBeInTheDocument()
  })

  it('handles edge without metadata', async () => {
    // Mock edge without metadata
    const mockEdgeDataNoMeta = {
      ...mockEdgeData,
      edge: {
        ...mockEdgeData.edge,
        meta: {}
      }
    }
    
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/edges/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockEdgeDataNoMeta)
        })
      }
      if (url.includes('/storage-endpoints/nodes/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockFromNodeData)
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    // Should not show metadata section
    expect(screen.queryByText('Edge Metadata')).not.toBeInTheDocument()
  })

  it('handles complex metadata objects', async () => {
    // Mock edge with complex metadata
    const mockEdgeDataComplexMeta = {
      ...mockEdgeData,
      edge: {
        ...mockEdgeData.edge,
        meta: {
          relationship: 'concept_on_axis',
          createdBy: 'system',
          confidence: 0.95,
          complexData: {
            nested: {
              value: 'test',
              array: [1, 2, 3]
            }
          }
        }
      }
    }
    
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/storage-endpoints/edges/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockEdgeDataComplexMeta)
        })
      }
      if (url.includes('/storage-endpoints/nodes/')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockFromNodeData)
        })
      }
      return Promise.resolve({ ok: false, status: 404 })
    })
    
    renderWithProviders(<EdgeDetailPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Edge Relationship')).toBeInTheDocument()
    })
    
    expect(screen.getByText('complexData')).toBeInTheDocument()
    // Complex data is displayed in a pre-formatted block, just verify the section exists
  })
})
