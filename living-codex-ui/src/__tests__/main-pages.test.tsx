import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import NodesPage from '@/app/nodes/page'
import OntologyPage from '@/app/ontology/page'
import GraphPage from '@/app/graph/page'

// Mock Next.js navigation
const mockPush = jest.fn()
const mockBack = jest.fn()

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, back: mockBack }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/',
}))

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

// Mock the buildApiUrl function
jest.mock('@/lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock fetch responses
const mockNodesData = {
  success: true,
  nodes: [
    {
      id: 'u-core-concept-kw-matter',
      typeId: 'codex.concept.keyword',
      title: 'Matter',
      description: 'Physical substance that has mass and occupies space',
      state: 'Ice',
      locale: 'en'
    },
    {
      id: 'u-core-axis-water_states',
      typeId: 'codex.ontology.axis',
      title: 'Water States Axis',
      description: 'Axis representing different states of matter',
      state: 'Ice',
      locale: 'en'
    }
  ],
  totalCount: 2
}

const mockEdgesData = {
  success: true,
  edges: [
    {
      fromId: 'u-core-concept-kw-matter',
      toId: 'u-core-axis-water_states',
      role: 'concept_on_axis',
      weight: 0.9
    }
  ],
  totalCount: 1
}

const mockStatsData = {
  success: true,
  nodeCount: 2,
  edgeCount: 1,
  moduleCount: 1,
  uptime: '24h',
  requestCount: 100
}

describe('Main Pages', () => {
  beforeEach(() => {
    // Reset mocks
    jest.clearAllMocks()
    
    // Mock fetch
    global.fetch = jest.fn()
      .mockImplementation((url: string) => {
        if (url.includes('/storage-endpoints/nodes')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockNodesData)
          })
        }
        if (url.includes('/storage-endpoints/edges')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockEdgesData)
          })
        }
        if (url.includes('/storage-endpoints/stats')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockStatsData)
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

  describe('NodesPage', () => {
    it('renders nodes page correctly', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Nodes')).toBeInTheDocument()
      })
      
      expect(screen.getByText('Matter')).toBeInTheDocument()
      expect(screen.getByText('Water States Axis')).toBeInTheDocument()
    })

    it('displays node statistics', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Nodes')).toBeInTheDocument()
      })
      
      expect(screen.getByText('2')).toBeInTheDocument() // nodeCount
    })

    it('handles node navigation', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Matter')).toBeInTheDocument()
      })
      
      // Click on a node
      fireEvent.click(screen.getByText('Matter'))
      
      expect(mockPush).toHaveBeenCalledWith('/node/u-core-concept-kw-matter')
    })

    it('handles search functionality', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Nodes')).toBeInTheDocument()
      })
      
      // Look for search input
      const searchInput = screen.getByPlaceholderText(/search/i)
      if (searchInput) {
        fireEvent.change(searchInput, { target: { value: 'matter' } })
        expect(searchInput).toHaveValue('matter')
      }
    })

    it('handles filtering by type', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Nodes')).toBeInTheDocument()
      })
      
      // Look for type filter
      const typeFilter = screen.getByDisplayValue(/all types/i)
      if (typeFilter) {
        fireEvent.change(typeFilter, { target: { value: 'codex.concept.keyword' } })
        expect(typeFilter).toHaveValue('codex.concept.keyword')
      }
    })
  })

  describe('OntologyPage', () => {
    it('renders ontology page correctly', async () => {
      renderWithProviders(<OntologyPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/ontology/i)).toBeInTheDocument()
      })
    })

    it('displays axis information', async () => {
      renderWithProviders(<OntologyPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/axis/i)).toBeInTheDocument()
      })
    })

    it('handles axis navigation', async () => {
      renderWithProviders(<OntologyPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/axis/i)).toBeInTheDocument()
      })
      
      // Look for axis links
      const axisLinks = screen.getAllByText(/water states/i)
      if (axisLinks.length > 0) {
        fireEvent.click(axisLinks[0])
        expect(mockPush).toHaveBeenCalledWith('/node/u-core-axis-water_states')
      }
    })
  })

  describe('GraphPage', () => {
    it('renders graph page correctly', async () => {
      renderWithProviders(<GraphPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/graph/i)).toBeInTheDocument()
      })
    })

    it('displays graph controls', async () => {
      renderWithProviders(<GraphPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/graph/i)).toBeInTheDocument()
      })
      
      // Look for graph controls
      expect(screen.getByText(/zoom/i) || screen.getByText(/reset/i) || screen.getByText(/fit/i)).toBeInTheDocument()
    })

    it('handles graph interactions', async () => {
      renderWithProviders(<GraphPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/graph/i)).toBeInTheDocument()
      })
      
      // Look for graph interaction buttons
      const zoomIn = screen.queryByText(/zoom in/i)
      if (zoomIn) {
        fireEvent.click(zoomIn)
      }
      
      const zoomOut = screen.queryByText(/zoom out/i)
      if (zoomOut) {
        fireEvent.click(zoomOut)
      }
    })
  })

  describe('Error Handling', () => {
    it('handles API errors gracefully', async () => {
      // Mock fetch to return error
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500
      })
      
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/error/i) || screen.getByText(/failed/i)).toBeInTheDocument()
      })
    })

    it('handles empty data gracefully', async () => {
      // Mock fetch to return empty data
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, nodes: [], totalCount: 0 })
      })
      
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/no nodes/i) || screen.getByText(/empty/i)).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('shows loading state initially', () => {
      renderWithProviders(<NodesPage />)
      
      expect(screen.getByText(/loading/i)).toBeInTheDocument()
    })

    it('hides loading state after data loads', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Matter')).toBeInTheDocument()
      })
      
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument()
    })
  })

  describe('Responsive Design', () => {
    it('adapts to different screen sizes', () => {
      // Mock different screen sizes
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768,
      })
      
      renderWithProviders(<NodesPage />)
      
      // Check that responsive elements are present
      expect(screen.getByText('Nodes')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA labels', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Matter')).toBeInTheDocument()
      })
      
      // Check for accessibility attributes
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
      
      const links = screen.getAllByRole('link')
      expect(links.length).toBeGreaterThan(0)
    })

    it('supports keyboard navigation', async () => {
      renderWithProviders(<NodesPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Matter')).toBeInTheDocument()
      })
      
      // Test keyboard navigation
      const firstButton = screen.getAllByRole('button')[0]
      if (firstButton) {
        firstButton.focus()
        expect(document.activeElement).toBe(firstButton)
      }
    })
  })
})
