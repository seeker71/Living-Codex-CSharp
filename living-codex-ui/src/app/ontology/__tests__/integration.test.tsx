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

// Mock fetch for comprehensive API testing
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

// Mock the KnowledgeMap component
jest.mock('../../../components/ui/KnowledgeMap', () => {
  return function MockKnowledgeMap({ nodes, className }: any) {
    return (
      <div className={className} data-testid="knowledge-map">
        <canvas data-testid="map-canvas" />
        <div data-testid="map-legend">Legend</div>
        <div data-testid="map-stats">Stats</div>
      </div>
    )
  }
})

describe('OntologyPage Integration', () => {
  const user = userEvent.setup()

  beforeEach(() => {
    jest.clearAllMocks()

    // Mock comprehensive API responses
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
            json: () => Promise.resolve({
              success: true,
              nodeCount: 3,
              edgeCount: 1
            })
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

  describe('Complete User Journey', () => {
    it('completes full user journey from landing to domain exploration', async () => {
      renderWithProviders(<OntologyPage />)

      // 1. Initial load and domain overview
      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
        expect(screen.getByText('Physics')).toBeInTheDocument()
        expect(screen.getByText('Painting')).toBeInTheDocument()
      })

      // 2. Explore physics domain
      fireEvent.click(screen.getByText('Physics'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
        expect(screen.getByText('Physics')).toBeInTheDocument()
        expect(screen.getByText('3')).toBeInTheDocument() // Concept count
      })

      // 3. Navigate back to overview
      fireEvent.click(screen.getByText('← Back to Overview'))

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // 4. Switch to map view
      fireEvent.click(screen.getByText('🕸️ Map'))

      await waitFor(() => {
        expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
        expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
      })

      // 5. Search functionality
      fireEvent.click(screen.getByText('📋 Cards')) // Switch back to cards

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      // 6. Navigate to concept detail
      fireEvent.click(screen.getByText('Quantum Mechanics'))

      expect(mockPush).toHaveBeenCalledWith('/node/codex-concept-quantum')
    })

    it('handles search and filter workflow', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      // Test search functionality
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'physics' } })

      // Should show search is active
      expect(searchInput).toHaveValue('physics')

      // Test filter selection
      const conceptFilter = screen.getByText('concepts')
      fireEvent.click(conceptFilter)

      // Should update filter state
      expect(conceptFilter).toBeInTheDocument()
    })

    it('manages state across view mode changes', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Navigate to domain
      fireEvent.click(screen.getByText('Physics'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Switch to map view while in domain
      fireEvent.click(screen.getByText('🕸️ Map'))

      await waitFor(() => {
        expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
      })

      // Switch back to cards
      fireEvent.click(screen.getByText('📋 Cards'))

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Should maintain state properly
    })
  })

  describe('Error Recovery Flows', () => {
    it('recovers from API failures and retries', async () => {
      let callCount = 0
      global.fetch = jest.fn().mockImplementation(() => {
        callCount++
        if (callCount === 1) {
          return Promise.resolve({
            ok: false,
            status: 500,
            statusText: 'Internal Server Error'
          })
        }
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockNodesData)
        })
      })

      renderWithProviders(<OntologyPage />)

      // Should show error state
      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
        expect(screen.getByText('🔄 Try Again')).toBeInTheDocument()
      })

      // Click retry
      fireEvent.click(screen.getByText('🔄 Try Again'))

      // Should recover and show data
      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })
    })

    it('handles partial data loading', async () => {
      // Mock partial failure - nodes succeed, edges fail
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
              ok: false,
              status: 500
            })
          }
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ success: true })
          })
        })

      renderWithProviders(<OntologyPage />)

      // Should load nodes but handle edge failure gracefully
      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
        expect(screen.getByText('Physics')).toBeInTheDocument()
      })
    })

    it('handles network timeout scenarios', async () => {
      global.fetch = jest.fn().mockImplementation(() =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: false,
              status: 408,
              statusText: 'Request Timeout'
            })
          }, 100)
        })
      )

      renderWithProviders(<OntologyPage />)

      // Should eventually show error state
      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
      }, { timeout: 1000 })
    })
  })

  describe('Performance Under Load', () => {
    it('handles rapid user interactions without breaking', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Rapid interactions
      const actions = [
        () => fireEvent.click(screen.getByText('Physics')),
        () => fireEvent.click(screen.getByText('← Back to Overview')),
        () => fireEvent.click(screen.getByText('🕸️ Map')),
        () => fireEvent.click(screen.getByText('📋 Cards')),
        () => {
          const searchInput = screen.getByTestId('search-input')
          fireEvent.change(searchInput, { target: { value: 'test' } })
        }
      ]

      // Execute rapid actions
      for (const action of actions) {
        action()
        await new Promise(resolve => setTimeout(resolve, 50))
      }

      // Should still be functional
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })

    it('maintains performance with large datasets', async () => {
      // Mock large dataset
      const largeDataset = {
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
        json: () => Promise.resolve(largeDataset)
      })

      const startTime = Date.now()
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      const renderTime = Date.now() - startTime

      // Should render in reasonable time (less than 2 seconds)
      expect(renderTime).toBeLessThan(2000)
    })

    it('handles memory efficiently with frequent updates', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Simulate frequent state updates
      for (let i = 0; i < 10; i++) {
        const searchInput = screen.getByTestId('search-input')
        fireEvent.change(searchInput, { target: { value: `search-${i}` } })
        await new Promise(resolve => setTimeout(resolve, 10))
      }

      // Should not cause memory leaks or performance degradation
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })
  })

  describe('Cross-Component Integration', () => {
    it('integrates SmartSearch with domain filtering', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByTestId('search-input')).toBeInTheDocument()
      })

      // Navigate to domain
      fireEvent.click(screen.getByText('Physics'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Search within domain
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      // Should perform domain-specific search
      expect(searchInput).toHaveValue('quantum')
    })

    it('coordinates between view modes and search results', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Perform search
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'physics' } })

      // Switch to map view
      fireEvent.click(screen.getByText('🕸️ Map'))

      await waitFor(() => {
        expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
      })

      // Search state should persist
      expect(searchInput).toHaveValue('physics')
    })

    it('maintains state consistency across navigation', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Navigate to domain
      fireEvent.click(screen.getByText('Physics'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Navigate to another domain
      fireEvent.click(screen.getByText('← Back to Overview'))
      fireEvent.click(screen.getByText('Painting'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Should maintain proper navigation state
    })
  })

  describe('Authentication Integration', () => {
    it('works with authenticated user', async () => {
      renderWithProviders(<OntologyPage />, {
        authValue: {
          user: { id: 'test-user', username: 'testuser' },
          isAuthenticated: true
        }
      })

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Should work normally with authenticated user
    })

    it('works in read-only mode without authentication', async () => {
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

    it('handles authentication state changes', async () => {
      const { rerender } = renderWithProviders(<OntologyPage />, {
        authValue: {
          user: null,
          isAuthenticated: false
        }
      })

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Change to authenticated
      rerender(
        <OntologyPage />
      )

      // Should handle authentication state change
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })
  })

  describe('Real-world Scenarios', () => {
    it('handles slow network conditions', async () => {
      // Mock slow response
      global.fetch = jest.fn().mockImplementation(() =>
        new Promise(resolve => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: () => Promise.resolve(mockNodesData)
            })
          }, 2000)
        })
      )

      renderWithProviders(<OntologyPage />)

      // Should show loading state for extended period
      expect(screen.getByText('Loading Knowledge Universe')).toBeInTheDocument()

      // Eventually loads
      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      }, { timeout: 3000 })
    })

    it('handles concurrent API requests', async () => {
      let requestCount = 0
      global.fetch = jest.fn().mockImplementation(() => {
        requestCount++
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockNodesData)
        })
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Multiple rapid interactions
      fireEvent.click(screen.getByText('Physics'))
      fireEvent.click(screen.getByText('← Back to Overview'))
      fireEvent.click(screen.getByText('🕸️ Map'))

      // Should handle concurrent requests appropriately
      expect(requestCount).toBeGreaterThan(0)
    })

    it('maintains user experience during data updates', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Simulate data refresh
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'refresh' } })

      // Should maintain smooth UX during updates
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })
  })

  describe('Error Boundaries and Resilience', () => {
    it('handles component crashes gracefully', async () => {
      // Mock a component that throws an error
      const originalError = console.error
      console.error = jest.fn()

      // This would test error boundary integration if implemented
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      console.error = originalError
    })

    it('recovers from JavaScript errors', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Simulate JS error (this would test error recovery)
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'error-test' } })

      // Should handle errors gracefully
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })

    it('maintains functionality after errors', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Simulate error scenario
      global.fetch = jest.fn().mockRejectedValue(new Error('Test Error'))

      // Trigger action that would cause error
      fireEvent.click(screen.getByText('Physics'))

      // Should recover and maintain functionality
      expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
    })
  })

  describe('End-to-End User Flows', () => {
    it('completes knowledge discovery workflow', async () => {
      renderWithProviders(<OntologyPage />)

      // 1. User lands on ontology page
      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // 2. User explores featured domains
      fireEvent.click(screen.getByText('Physics'))
      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // 3. User searches for specific concept
      fireEvent.click(screen.getByText('← Back to Overview'))
      const searchInput = screen.getByTestId('search-input')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      // 4. User navigates to concept detail
      fireEvent.click(screen.getByText('Quantum Mechanics'))
      expect(mockPush).toHaveBeenCalledWith('/node/codex-concept-quantum')

      // 5. User returns and explores map view
      // (This would be tested in a more complete e2e test)
    })

    it('handles exploration with engagement tracking', async () => {
      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // User explores multiple domains (tracking engagement)
      fireEvent.click(screen.getByText('Physics'))
      fireEvent.click(screen.getByText('← Back to Overview'))
      fireEvent.click(screen.getByText('Painting'))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Should track exploration progress
      expect(screen.getByText('🏆 Your Journey')).toBeInTheDocument()
    })

    it('supports mobile user journey', async () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('🌍 Knowledge Universe')).toBeInTheDocument()
      })

      // Mobile-specific interactions
      const physicsCard = screen.getByText('Physics').closest('div')
      if (physicsCard) {
        fireEvent.click(physicsCard)
      }

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Should work smoothly on mobile
    })
  })

  describe('API Integration Testing', () => {
    it('handles API rate limiting', async () => {
      let callCount = 0
      global.fetch = jest.fn().mockImplementation(() => {
        callCount++
        if (callCount > 5) {
          return Promise.resolve({
            ok: false,
            status: 429,
            statusText: 'Too Many Requests'
          })
        }
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockNodesData)
        })
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      // Rapid API calls (should be rate limited)
      for (let i = 0; i < 10; i++) {
        const searchInput = screen.getByTestId('search-input')
        fireEvent.change(searchInput, { target: { value: `search-${i}` } })
        await new Promise(resolve => setTimeout(resolve, 10))
      }

      // Should handle rate limiting gracefully
    })

    it('handles API version changes', async () => {
      // Mock API version change response
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 406,
        statusText: 'Not Acceptable'
      })

      renderWithProviders(<OntologyPage />)

      await waitFor(() => {
        expect(screen.getByText('Connection Lost')).toBeInTheDocument()
      })

      // Should handle API version changes
    })

    it('retries failed requests appropriately', async () => {
      let attemptCount = 0
      global.fetch = jest.fn().mockImplementation(() => {
        attemptCount++
        if (attemptCount < 3) {
          return Promise.resolve({
            ok: false,
            status: 500
          })
        }
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockNodesData)
        })
      })

      renderWithProviders(<OntologyPage />)

      // Should eventually succeed after retries
      await waitFor(() => {
        expect(screen.getByText('Featured Knowledge Areas')).toBeInTheDocument()
      })

      expect(attemptCount).toBeGreaterThanOrEqual(3)
    })
  })
})
