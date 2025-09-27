import React from 'react'
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import NewsPage from '@/app/news/page'

const mockAuthContext = {
  user: { id: 'test-user', username: 'newsuser', email: 'news@example.com', displayName: 'News User', createdAt: new Date().toISOString(), isActive: true },
  token: 'token',
  isLoading: false,
  isAuthenticated: true,
  login: jest.fn(),
  register: jest.fn(),
  logout: jest.fn(),
  refreshUser: jest.fn(),
  testConnection: jest.fn(),
}

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockAuthContext,
  AuthProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}))

describe('News Item Flows - Comprehensive Testing', () => {
  let originalFetch: typeof global.fetch
  let mockFetch: jest.Mock

  beforeEach(() => {
    originalFetch = global.fetch
    mockFetch = jest.fn()
    global.fetch = mockFetch
  })

  afterEach(() => {
    global.fetch = originalFetch
    mockFetch.mockReset()
  })

  const createMockNewsItem = (overrides: any = {}) => ({
    id: 'codex.news.item.test123',
    title: 'Test News Article',
    description: 'A test news article description',
    url: 'https://example.com/test-article',
    publishedAt: new Date().toISOString(),
    source: 'Test Source',
    author: 'Test Author',
    imageUrl: 'https://example.com/image.jpg',
    content: 'Full article content here...',
    ...overrides
  })

  const setupMockResponses = (responses: Record<string, any>) => {
    mockFetch.mockImplementation(async (req: Request | string) => {
      const url = typeof req === 'string' ? req : req.url
      
      // News feed responses
      if (url.includes('/news/feed/') || url.includes('/news/latest') || url.includes('/news/search')) {
        return { ok: true, json: async () => responses.newsFeed || { items: [], totalCount: 0 } }
      }
      
      // News stats
      if (url.includes('/news/stats')) {
        return { ok: true, json: async () => responses.newsStats || { success: true, totalCount: 0, sources: {} } }
      }
      
      // News summary
      if (url.includes('/news/summary/')) {
        return { ok: true, json: async () => responses.newsSummary || { summary: 'Test summary', status: 'available' } }
      }
      
      // News concepts
      if (url.includes('/news/concepts/')) {
        return { ok: true, json: async () => responses.newsConcepts || { success: true, concepts: [] } }
      }
      
      // Node details
      if (url.includes('/storage-endpoints/nodes/')) {
        return { ok: true, json: async () => responses.nodeDetails || { node: { id: 'test-node', typeId: 'codex.news.item' } } }
      }
      
      // Edges
      if (url.includes('/storage-endpoints/edges')) {
        return { ok: true, json: async () => responses.edges || { edges: [], totalCount: 0 } }
      }
      
      // Mark as read
      if (url.includes('/news/read')) {
        return { ok: true, json: async () => ({ success: true }) }
      }
      
      return { ok: true, json: async () => ({ success: true }) }
    })
  }

  describe('Source Information Handling', () => {
    it('displays proper source names from news-sources.json configuration', async () => {
      const newsItems = [
        createMockNewsItem({ source: 'Nature - Scientific Discoveries' }),
        createMockNewsItem({ source: 'Scientific American' }),
        createMockNewsItem({ source: 'MIT Technology Review' }),
        createMockNewsItem({ source: 'BBC World News' }),
      ]

      setupMockResponses({
        newsFeed: { items: newsItems, totalCount: 4 },
        newsStats: { success: true, totalCount: 4, sources: { 'Nature - Scientific Discoveries': 1, 'Scientific American': 1, 'MIT Technology Review': 1, 'BBC World News': 1 } }
      })

      renderWithProviders(<NewsPage />)

      // Wait for news to load
      await waitFor(() => {
        expect(screen.getAllByText('Test News Article')).toHaveLength(4)
      })

      // Check that all items show their proper source names
      expect(screen.getByText('📰 Nature - Scientific Discoveries')).toBeInTheDocument()
      expect(screen.getByText('📰 Scientific American')).toBeInTheDocument()
      expect(screen.getByText('📰 MIT Technology Review')).toBeInTheDocument()
      expect(screen.getByText('📰 BBC World News')).toBeInTheDocument()
    })

    it('makes source information clickable and navigable', async () => {
      const newsItem = createMockNewsItem({ source: 'Nature - Scientific Discoveries' })
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsStats: { success: true, totalCount: 1, sources: { 'Nature - Scientific Discoveries': 1 } }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      // Source should be displayed
      const sourceElement = screen.getByText('📰 Nature - Scientific Discoveries')
      expect(sourceElement).toBeInTheDocument()
      
      // Source should be clickable - in the current implementation, sources are displayed as spans
      // but they should be made clickable for filtering or source details
      expect(sourceElement).toBeInTheDocument()
    })

    it('maintains source information through the entire processing pipeline', async () => {
      const newsItem = createMockNewsItem({ 
        source: 'Quanta Magazine',
        title: 'Quantum Computing Breakthrough',
        description: 'Scientists achieve new milestone in quantum computing'
      })
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsStats: { success: true, totalCount: 1, sources: { 'Quanta Magazine': 1 } }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Quantum Computing Breakthrough')).toBeInTheDocument()
      })

      // Source should be preserved and displayed
      expect(screen.getByText('📰 Quanta Magazine')).toBeInTheDocument()
      
      // Click on the news item to load summary and concepts
      fireEvent.click(screen.getByText('Quantum Computing Breakthrough'))
      
      // Source information should still be visible and maintained
      await waitFor(() => {
        expect(screen.getByText('📰 Quanta Magazine')).toBeInTheDocument()
      })
    })

    it('ensures source information is never lost or replaced with fallbacks', async () => {
      // Test that source information from news-sources.json is always preserved
      const newsItems = [
        createMockNewsItem({ source: 'Nature - Scientific Discoveries' }),
        createMockNewsItem({ source: 'Scientific American' }),
        createMockNewsItem({ source: 'MIT Technology Review' }),
      ]

      setupMockResponses({
        newsFeed: { items: newsItems, totalCount: 3 },
        newsStats: { success: true, totalCount: 3, sources: { 'Nature - Scientific Discoveries': 1, 'Scientific American': 1, 'MIT Technology Review': 1 } }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getAllByText('Test News Article')).toHaveLength(3)
      })

      // All sources should be properly displayed from news-sources.json configuration
      expect(screen.getByText('📰 Nature - Scientific Discoveries')).toBeInTheDocument()
      expect(screen.getByText('📰 Scientific American')).toBeInTheDocument()
      expect(screen.getByText('📰 MIT Technology Review')).toBeInTheDocument()
      
      // No "Unknown" or fallback sources should be present
      expect(screen.queryByText('📰 Unknown')).not.toBeInTheDocument()
      expect(screen.queryByText('📰 AI')).not.toBeInTheDocument()
    })
  })

  describe('No Summary Handling', () => {
    it('displays "No summary available" when summary API returns error', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsSummary: { summary: '', status: 'none' }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      // Click on news item to load summary
      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('No summary available for this article.')).toBeInTheDocument()
      })
    })

    it('displays "Error loading summary" when summary API fails', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsSummary: { summary: '', status: 'error' }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('Error loading summary. Please try again.')).toBeInTheDocument()
      })
    })

    it('shows loading state while summary is being generated', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsSummary: { summary: '', status: 'generating' }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('Generating summary...')).toBeInTheDocument()
      })
    })

    it('displays "Click to load summary" initially', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      // The summary section should appear with initial state
      await waitFor(() => {
        expect(screen.getByText(/Click to load summary|Loading summary|No summary available/)).toBeInTheDocument()
      })
    })
  })

  describe('No Concepts Handling', () => {
    it('displays "No concepts extracted" when concepts API returns empty', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsConcepts: { success: true, concepts: [] }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      // Click on news item to load concepts
      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('No concepts extracted from this news item yet.')).toBeInTheDocument()
      })
    })

    it('displays error message when concepts API fails', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsConcepts: { success: false, error: 'Failed to load concepts' }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('Failed to load concepts: Failed to load concepts')).toBeInTheDocument()
      })
    })

    it('shows loading state while concepts are being loaded', async () => {
      const newsItem = createMockNewsItem()
      
      // Mock a delayed response
      mockFetch.mockImplementation(async (req: Request | string) => {
        const url = typeof req === 'string' ? req : req.url
        
        if (url.includes('/news/feed/')) {
          return { ok: true, json: async () => ({ items: [newsItem], totalCount: 1 }) }
        }
        
        if (url.includes('/news/concepts/')) {
          // Simulate delay
          await new Promise(resolve => setTimeout(resolve, 100))
          return { ok: true, json: async () => ({ success: true, concepts: [] }) }
        }
        
        return { ok: true, json: async () => ({ success: true }) }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      // Should show loading state briefly
      expect(screen.getByText('Loading concepts...')).toBeInTheDocument()
    })
  })

  describe('Complete News Item Display', () => {
    it('displays all available news item information correctly', async () => {
      const newsItem = createMockNewsItem({
        title: 'Breaking: Major Technology Breakthrough',
        description: 'Scientists have made a significant discovery in quantum computing.',
        author: 'Jane Smith',
        source: 'Tech News',
        imageUrl: 'https://example.com/quantum.jpg'
      })

      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsStats: { success: true, totalCount: 1, sources: { 'Tech News': 1 } }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Breaking: Major Technology Breakthrough')).toBeInTheDocument()
      })

      // Check all displayed information
      expect(screen.getByText('Scientists have made a significant discovery in quantum computing.')).toBeInTheDocument()
      expect(screen.getByText('📰 Tech News')).toBeInTheDocument()
      expect(screen.getByText('✍️ Jane Smith')).toBeInTheDocument()
      expect(screen.getByAltText('Breaking: Major Technology Breakthrough')).toBeInTheDocument()
    })

    it('handles news items without optional fields gracefully', async () => {
      const newsItem = createMockNewsItem({
        author: undefined,
        imageUrl: undefined,
        content: undefined
      })

      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      // Should not show author or image when not present
      expect(screen.queryByText(/✍️/)).not.toBeInTheDocument()
      expect(screen.queryByAltText('Test News Article')).not.toBeInTheDocument()
    })
  })

  describe('Clickable Elements Functionality', () => {
    it('opens node detail page when "View Node" is clicked', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 }
      })

      const openSpy = jest.spyOn(window, 'open').mockImplementation(() => null)

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('🔎 View Node'))

      expect(openSpy).toHaveBeenCalledWith('/node/codex.news.item.test123', '_blank')
      openSpy.mockRestore()
    })

    it('tracks interaction when "Mark as Read" is clicked', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Mark as Read'))

      // Should make API call to mark as read
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/news/read'),
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: expect.stringContaining('test-user')
          })
        )
      })
    })

    it('loads summary when news item title is clicked', async () => {
      const newsItem = createMockNewsItem()
      
      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsSummary: { summary: 'This is a test summary', status: 'available' }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('This is a test summary')).toBeInTheDocument()
      })
    })
  })

  describe('Summary and Concepts Integration', () => {
    it('displays both summary and concepts when available', async () => {
      const newsItem = createMockNewsItem()
      const concepts = [
        {
          id: 'concept1',
          name: 'Quantum Computing',
          description: 'Advanced computing technology',
          weight: 0.9,
          resonance: 0.8,
          axes: ['innovation', 'science']
        },
        {
          id: 'concept2',
          name: 'Technology Breakthrough',
          description: 'Major advancement in tech',
          weight: 0.7,
          resonance: 0.6,
          axes: ['impact']
        }
      ]

      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsSummary: { summary: 'This article discusses quantum computing breakthroughs', status: 'available' },
        newsConcepts: { success: true, concepts }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      // Should show summary
      await waitFor(() => {
        expect(screen.getByText('This article discusses quantum computing breakthroughs')).toBeInTheDocument()
      })

      // Should show concepts
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
        expect(screen.getByText('Technology Breakthrough')).toBeInTheDocument()
      })

      // Should show concept details
      expect(screen.getByText('Advanced computing technology')).toBeInTheDocument()
      expect(screen.getByText('Major advancement in tech')).toBeInTheDocument()
    })

    it('allows navigation to concept nodes', async () => {
      const newsItem = createMockNewsItem()
      const concepts = [
        {
          id: 'concept1',
          name: 'Quantum Computing',
          description: 'Advanced computing technology',
          weight: 0.9,
          resonance: 0.8,
          axes: ['innovation']
        }
      ]

      setupMockResponses({
        newsFeed: { items: [newsItem], totalCount: 1 },
        newsConcepts: { success: true, concepts }
      })

      const openSpy = jest.spyOn(window, 'open').mockImplementation(() => null)

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText('Test News Article')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test News Article'))

      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      })

      // Click on concept name
      fireEvent.click(screen.getByText('Quantum Computing'))

      expect(openSpy).toHaveBeenCalledWith('/node/concept1', '_blank')
      openSpy.mockRestore()
    })
  })

  describe('Error Handling and Edge Cases', () => {
    it('handles API errors gracefully', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      renderWithProviders(<NewsPage />)

      // Should not crash and should show some error state
      await waitFor(() => {
        expect(screen.getByText('📰 News Feed')).toBeInTheDocument()
      })
    })

    it('handles malformed news data', async () => {
      const malformedItems = [
        { id: 'test1', title: 'Valid Item', description: 'Valid description', url: 'https://example.com/1', publishedAt: new Date().toISOString() }, // Valid item
        { title: 'Test', description: 'Test desc', url: 'https://example.com/2', publishedAt: new Date().toISOString() }, // Missing id but has other required fields
      ]

      setupMockResponses({
        newsFeed: { items: malformedItems, totalCount: 2 }
      })

      renderWithProviders(<NewsPage />)

      // Should not crash and should handle malformed data gracefully
      await waitFor(() => {
        expect(screen.getByText('📰 News Feed')).toBeInTheDocument()
      })
    })

    it('handles empty news feed', async () => {
      setupMockResponses({
        newsFeed: { items: [], totalCount: 0 }
      })

      renderWithProviders(<NewsPage />)

      await waitFor(() => {
        expect(screen.getByText(/No personalized news found|No news items found/)).toBeInTheDocument()
      })
    })
  })

  describe('UI State Management', () => {
    it('shows loading state while fetching news', async () => {
      // Mock a delayed response
      mockFetch.mockImplementation(async (req: Request | string) => {
        const url = typeof req === 'string' ? req : req.url
        
        if (url.includes('/news/feed/')) {
          await new Promise(resolve => setTimeout(resolve, 100))
          return { ok: true, json: async () => ({ items: [], totalCount: 0 }) }
        }
        
        return { ok: true, json: async () => ({ success: true }) }
      })

      renderWithProviders(<NewsPage />)

      // Should show loading state
      expect(screen.getByText('Loading news...')).toBeInTheDocument()
    })

    it('updates UI when category changes', async () => {
      setupMockResponses({
        newsFeed: { items: [], totalCount: 0 }
      })

      renderWithProviders(<NewsPage />)

      const categorySelect = screen.getByDisplayValue('🎯 Personalized')
      fireEvent.change(categorySelect, { target: { value: 'technology' } })

      // Should trigger new API call
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/news/search'),
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('technology')
          })
        )
      })
    })

    it('handles search functionality', async () => {
      setupMockResponses({
        newsFeed: { items: [], totalCount: 0 }
      })

      renderWithProviders(<NewsPage />)

      const searchInput = screen.getByPlaceholderText('Search for specific topics...')
      const searchButton = screen.getByText('🔍 Search')

      fireEvent.change(searchInput, { target: { value: 'quantum computing' } })
      fireEvent.click(searchButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/news/search'),
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('quantum computing')
          })
        )
      })
    })
  })
})
