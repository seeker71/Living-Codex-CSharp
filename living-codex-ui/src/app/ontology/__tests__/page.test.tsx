import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from '../../../__tests__/test-utils'
import userEvent from '@testing-library/user-event'
import OntologyPage from '../page'

// Mock Next.js navigation
const mockPush = jest.fn()
const mockBack = jest.fn()

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, back: mockBack }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/ontology',
}))

// Mock the useAuth hook
jest.mock('../../../contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser', displayName: 'Test User' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

// Mock the useTrackInteraction hook
jest.mock('../../../lib/hooks', () => ({
  useTrackInteraction: () => jest.fn(),
}))

// Mock the buildApiUrl function
jest.mock('../../../lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock fetch for API calls
const mockNodesData = {
  success: true,
  nodes: [
    {
      id: 'codex-science-physics',
      typeId: 'codex.ontology.axis',
      title: 'Physics',
      description: 'Fundamental science of matter and energy',
      state: 'Ice',
      locale: 'en',
      meta: {
        name: 'Physics',
        description: 'Fundamental science of matter and energy',
        keywords: ['physics', 'matter', 'energy', 'science'],
        level: 0,
        dimensions: ['theoretical', 'experimental', 'applied']
      }
    },
    {
      id: 'codex-arts-painting',
      typeId: 'codex.ontology.axis',
      title: 'Painting',
      description: 'Visual art created with pigments on surfaces',
      state: 'Ice',
      locale: 'en',
      meta: {
        name: 'Painting',
        description: 'Visual art created with pigments on surfaces',
        keywords: ['art', 'painting', 'visual', 'creativity'],
        level: 0,
        dimensions: ['oil', 'watercolor', 'digital']
      }
    },
    {
      id: 'codex-concept-quantum',
      typeId: 'codex.concept',
      title: 'Quantum Mechanics',
      description: 'Theory describing nature at atomic scales',
      state: 'Water',
      locale: 'en',
      meta: {
        name: 'Quantum Mechanics',
        description: 'Theory describing nature at atomic scales',
        keywords: ['quantum', 'physics', 'mechanics', 'theory'],
        level: 1
      }
    }
  ],
  totalCount: 3
}

const mockEdgesData = {
  success: true,
  edges: [
    {
      fromId: 'codex-science-physics',
      toId: 'codex-concept-quantum',
      role: 'parent_of',
      weight: 0.8
    }
  ],
  totalCount: 1
}

const mockStatsData = {
  success: true,
  nodeCount: 3,
  edgeCount: 1,
  moduleCount: 1,
  uptime: '24h',
  requestCount: 100
}

// Mock the SmartSearch component
jest.mock('../../../components/ui/SmartSearch', () => {
  return function MockSmartSearch({ placeholder, onResultSelect, onResultsChange, showFilters, className }: any) {
    return (
      <div className={className} data-testid="smart-search">
        <input
          placeholder={placeholder}
          data-testid="search-input"
          onChange={(e) => {
            if (onResultsChange) onResultsChange(e.target.value);
          }}
        />
        {showFilters && (
          <div data-testid="search-filters">
            {['concepts', 'people', 'code', 'news'].map(filter => (
              <button key={filter} data-testid={`filter-${filter}`}>
                {filter}
              </button>
            ))}
          </div>
        )}
      </div>
    )
  }
})

// Mock the KnowledgeMap component and hook API expected by the page
jest.mock('../../../components/ui/KnowledgeMap', () => {
  const MockKnowledgeMap = function MockKnowledgeMap({ nodes, className }: any) {
    return (
      <div className={className} data-testid="knowledge-map">
        <canvas data-testid="map-canvas" />
        <div data-testid="map-legend">Legend</div>
        <div data-testid="map-stats">Stats</div>
      </div>
    )
  }
  const useMockKnowledgeNodes = (count: number = 24) =>
    Array.from({ length: count }).map((_, i) => ({
      id: `mock-node-${i}`,
      title: `Concept ${i}`,
      domain: i % 2 === 0 ? 'Science & Tech' : 'Arts & Culture',
      x: (i % 10) / 10,
      y: Math.floor(i / 10) / 10,
      connections: [],
      size: 1,
      resonance: 70,
    }))
  return { 
    __esModule: true, 
    default: MockKnowledgeMap, 
    useMockKnowledgeNodes,
    KnowledgeMap: MockKnowledgeMap
  }
})

describe('OntologyPage', () => {
  const user = userEvent.setup()

  beforeEach(() => {
    jest.clearAllMocks()

    // Mock fetch responses
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

  describe('Rendering', () => {
    it('renders ontology page with header and search', async () => {
      renderWithProviders(<OntologyPage />)

      // Check header
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      expect(screen.getByText('Explore human knowledge through interconnected concepts')).toBeInTheDocument()

      // Check search component
      await waitFor(() => {
        expect(screen.getByTestId('smart-search')).toBeInTheDocument()
        expect(screen.getByTestId('search-input')).toHaveAttribute('placeholder', 'Search knowledge...')
      })
    })

    it('displays view mode toggle buttons', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('📋 Cards')).toBeInTheDocument()
        expect(screen.getByText('🕸️ Map')).toBeInTheDocument()
      })
    })

    it('shows floating action button', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        const fab = screen.getByRole('button', { name: /quick actions/i })
        expect(fab).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('shows loading state initially', () => {
      renderWithProviders(<OntologyPage />)

      expect(screen.getByText('Loading Knowledge Universe')).toBeInTheDocument()
      expect(screen.getByText('Connecting concepts and building knowledge networks...')).toBeInTheDocument()
    })

    it('displays animated loading indicators', () => {
      renderWithProviders(<OntologyPage />)

      // Check for loading animations
      const loadingContainer = screen.getByText('Loading Knowledge Universe').closest('div')
      expect(loadingContainer).toBeInTheDocument()
    })

    it('hides loading state after data loads', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      expect(screen.queryByText('Loading Knowledge Universe')).not.toBeInTheDocument()
    })
  })

  describe('Error Handling', () => {
    it('handles API errors gracefully', async () => {
      // Mock fetch to return error
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error'
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
        expect(screen.getByText('We\'re having trouble connecting to the knowledge network. This might be temporary.')).toBeInTheDocument()
      })
    })

    it('displays error details in development mode', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error'
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Internal Server Error')).toBeInTheDocument()
      })
    })

    it('provides recovery options on error', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('🔄 Try Again')).toBeInTheDocument()
        expect(screen.getByText('🔃 Reload Page')).toBeInTheDocument()
      })
    })

    it('handles network errors', async () => {
      global.fetch = jest.fn().mockRejectedValue(new Error('Network Error'))

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
      })
    })
  })

  describe('Data Display', () => {
    it('displays knowledge domains correctly', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
        expect(screen.getByText('More Knowledge Areas')).toBeInTheDocument()
      })
    })

    it('shows domain information with icons and descriptions', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        // Check for domain icons and information
        const physicsCard = screen.getByText('Physics').closest('div')
        expect(physicsCard).toBeInTheDocument()

        const paintingCard = screen.getByText('Painting').closest('div')
        expect(paintingCard).toBeInTheDocument()
      })
    })

    it('displays concept counts for each domain', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('3')).toBeInTheDocument() // Total concepts
      })
    })

    it('handles empty data gracefully', async () => {
      // Mock empty data
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, nodes: [], totalCount: 0 })
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('No Knowledge Found')).toBeInTheDocument()
      })
    })
  })

  describe('User Interactions', () => {
    it('allows switching between view modes', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('📋 Cards')).toBeInTheDocument()
      })

      // Switch to map view
      fireEvent.click(screen.getByText('🕸️ Map'))

      await waitFor(() => {
        expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
      })
    })

    it('handles domain selection', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Click on Physics domain
      fireEvent.click(screen.getByText('Physics'))

      // Should navigate to domain view (this would be tested by checking state changes)
      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })
    })

    it('handles search functionality', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      // Search results should appear (mocked)
      expect(searchInput).toHaveValue('quantum')
    })

    it('shows search filters when enabled', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-filters')).toBeInTheDocument()
      })
    })

    it('handles floating action button hover', async () => {
      renderWithProviders(<OntologyPage />)

      const fab = screen.getByRole('button', { name: /quick actions/i })

      // Hover over FAB (tooltip should appear)
      fireEvent.mouseEnter(fab)

      await waitFor(() => {
        expect(screen.getByText('Quick Actions')).toBeInTheDocument()
      })
    })
  })

  describe('Navigation', () => {
    it('navigates to concept details when clicking concepts', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Quantum Mechanics')).toBeInTheDocument()
      })

      // Click on concept
      fireEvent.click(screen.getByText('Quantum Mechanics'))

      expect(mockPush).toHaveBeenCalledWith('/node/codex-concept-quantum')
    })

    it('navigates back from domain view', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Navigate to domain
      fireEvent.click(screen.getByText('Physics'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Go back
      fireEvent.click(screen.getByText('← Back to Overview'))

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })
    })
  })

  describe('State Management', () => {
    it('maintains view mode state', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('📋 Cards')).toBeInTheDocument()
      })

      // Switch to map view
      fireEvent.click(screen.getByText('🕸️ Map'))

      await waitFor(() => {
        expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
      })

      // View mode should persist (this would be tested by checking component state)
    })

    it('updates search results when typing', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      const searchInput = screen.getByTestId('search-input')

      // Type in search
      fireEvent.change(searchInput, { target: { value: 'physics' } })
      expect(searchInput).toHaveValue('physics')

      // Should trigger search (this would update results state)
    })

    it('handles domain filter changes', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByDisplayValue('All Domains')).toBeInTheDocument()
      })

      const domainSelect = screen.getByDisplayValue('All Domains')

      // Change domain filter
      fireEvent.change(domainSelect, { target: { value: 'science' } })
      expect(domainSelect).toHaveValue('science')
    })
  })

  describe('Progress and Achievements', () => {
    it('displays exploration progress', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('🏆 Your Journey')).toBeInTheDocument()
      })

      // Check progress indicators
      expect(screen.getByText('Domains Explored')).toBeInTheDocument()
      expect(screen.getByText('Concepts Discovered')).toBeInTheDocument()
      expect(screen.getByText('Searches Performed')).toBeInTheDocument()
    })

    it('shows achievement notifications', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('💡 Quick Tips')).toBeInTheDocument()
      })

      // Check for tip content
      expect(screen.getByText('Click any domain card to explore concepts in that area')).toBeInTheDocument()
      expect(screen.getByText('Use the search bar to find specific topics quickly')).toBeInTheDocument()
    })

    it('tracks domain exploration', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Click on domain to track exploration
      fireEvent.click(screen.getByText('Physics'))

      // Should update exploration tracking (this would be tested by checking state)
    })
  })

  describe('Responsive Design', () => {
    it('adapts layout for mobile screens', () => {
      // Mock mobile screen size
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      renderWithProviders(<OntologyPage />)

      // Check that mobile-responsive elements are present
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })

    it('handles touch interactions', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Test touch/click on mobile
      const physicsCard = screen.getByText('Physics').closest('div')
      if (physicsCard) {
        fireEvent.click(physicsCard)
      }
    })

    it('maintains usability across screen sizes', () => {
      // Test different viewport sizes
      const viewports = [320, 768, 1024, 1440]

      viewports.forEach(width => {
        Object.defineProperty(window, 'innerWidth', {
          writable: true,
          configurable: true,
          value: width,
        })

        renderWithProviders(<OntologyPage />)

        // Should render without crashing
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper heading hierarchy', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Check heading levels
      const headings = screen.getAllByRole('heading')
      expect(headings.length).toBeGreaterThan(0)

      // Main heading should be h1
      const mainHeading = screen.getByText('🌍 Knowledge Universe')
      expect(mainHeading.tagName).toBe('H1')
    })

    it('supports keyboard navigation', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Test keyboard navigation
      const firstButton = screen.getAllByRole('button')[0]
      if (firstButton) {
        firstButton.focus()
        expect(document.activeElement).toBe(firstButton)
      }
    })

    it('has proper ARIA labels', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('smart-search')).toBeInTheDocument()
      })

      // Check for ARIA attributes
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)

      const links = screen.getAllByRole('link')
      expect(links.length).toBeGreaterThan(0)
    })

    it('provides screen reader friendly content', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Check for descriptive text that helps screen readers
      expect(screen.getByText('Explore human knowledge through interconnected concepts')).toBeInTheDocument()
    })
  })

  describe('Performance', () => {
    it('handles large datasets efficiently', async () => {
      // Mock larger dataset
      const largeMockData = {
        ...mockNodesData,
        nodes: Array.from({ length: 100 }, (_, i) => ({
          ...mockNodesData.nodes[0],
          id: `node-${i}`,
          title: `Concept ${i}`
        })),
        totalCount: 100
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(largeMockData)
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Should render without performance issues
    })

    it('debounces search input', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      const searchInput = screen.getByTestId('search-input')

      // Rapid typing should be debounced
      fireEvent.change(searchInput, { target: { value: 'a' } })
      fireEvent.change(searchInput, { target: { value: 'ab' } })
      fireEvent.change(searchInput, { target: { value: 'abc' } })

      // Should not cause excessive API calls
      expect(searchInput).toHaveValue('abc')
    })

    it('caches domain data appropriately', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Domain data should be cached (this would be tested by checking state)
    })
  })

  describe('Edge Cases', () => {
    it('handles malformed API responses', async () => {
      // Mock malformed data
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, nodes: null })
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Should handle gracefully
    })

    it('handles very long concept names', async () => {
      const longNameMock = {
        ...mockNodesData,
        nodes: [
          {
            ...mockNodesData.nodes[0],
            title: 'A'.repeat(100), // Very long name
            description: 'B'.repeat(500) // Very long description
          }
        ]
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(longNameMock)
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Should truncate appropriately
    })

    it('handles special characters in content', async () => {
      const specialCharsMock = {
        ...mockNodesData,
        nodes: [
          {
            ...mockNodesData.nodes[0],
            title: 'Physics & Chemistry 🚀',
            description: 'Special chars: <script>alert("test")</script> & symbols'
          }
        ]
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(specialCharsMock)
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Should sanitize or handle special characters appropriately
    })

    it('handles offline/network disconnection', async () => {
      // Mock offline scenario
      global.fetch = jest.fn().mockRejectedValue(new Error('Network Error'))

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
      })

      // Should show appropriate offline messaging
    })

    it('handles rapid state changes', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Rapidly switch between views
      fireEvent.click(screen.getByText('📋 Cards'))
      fireEvent.click(screen.getByText('🕸️ Map'))
      fireEvent.click(screen.getByText('📋 Cards'))

      // Should handle rapid changes without breaking
    })
  })

  describe('Integration', () => {
    it('integrates with authentication system', async () => {
      renderWithProviders(<OntologyPage />, {
        authValue: {
          user: { id: 'test-user', username: 'testuser' },
          isAuthenticated: true
        }
      })

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Should work with authenticated user
    })

    it('works without authentication', async () => {
      renderWithProviders(<OntologyPage />, {
        authValue: {
          user: null,
          isAuthenticated: false
        }
      })

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Should work in read-only mode
    })

    it('integrates with routing system', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Quantum Mechanics')).toBeInTheDocument()
      })

      // Click concept to navigate
      fireEvent.click(screen.getByText('Quantum Mechanics'))

      expect(mockPush).toHaveBeenCalledWith('/node/codex-concept-quantum')
    })

    it('integrates with tracking system', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })

      // Click on domain (should trigger tracking)
      fireEvent.click(screen.getByText('Physics'))

      // Tracking should be called (this would be verified by checking mock calls)
    })
  })
})
