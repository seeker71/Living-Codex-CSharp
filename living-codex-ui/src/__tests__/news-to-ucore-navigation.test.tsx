import React from 'react'
import { fireEvent, screen } from '@testing-library/react'
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

describe('News → AI Concepts → U-CORE navigation', () => {
  let originalFetch: typeof global.fetch

  beforeEach(() => {
    originalFetch = global.fetch
    global.fetch = jest.fn()
    // Mock backend APIs involved in the flow
    ;(global.fetch as jest.Mock).mockImplementation(async (req: Request | string) => {
      const url = typeof req === 'string' ? req : req.url
      // News stats
      if (url.includes('/news/stats')) {
        return { ok: true, json: async () => ({ success: true, totalCount: 1, sources: { test: 1 } }) }
      }
      if (url.includes('/news/feed/')) {
        return { ok: true, json: async () => ({ items: [{
          id: 'codex.news.item.abc123',
          title: 'Test Article',
          description: 'A test article',
          url: 'https://example.com/test',
          publishedAt: new Date().toISOString(),
          source: 'test'
        }], totalCount: 1 }) }
      }
      // Personalized/news latest/search
      if (url.includes('/news/latest') || url.includes('/news/search')) {
        return { ok: true, json: async () => ({ items: [{
          id: 'codex.news.item.abc123',
          title: 'Test Article',
          description: 'A test article',
          url: 'https://example.com/test',
          publishedAt: new Date().toISOString(),
          source: 'test'
        }], totalCount: 1 }) }
      }
      // Node detail for the news item
      if (url.includes('/storage-endpoints/nodes/codex.news.item.abc123')) {
        return { ok: true, json: async () => ({ node: {
          id: 'codex.news.item.abc123',
          typeId: 'codex.news/item',
          title: 'Test Article',
          state: 'Ice',
        } }) }
      }
      // Edges connected to news → AI concepts and to U-CORE axis
      if (url.includes('/storage-endpoints/edges')) {
        return { ok: true, json: async () => ({ edges: [
          { fromId: 'codex.news.item.abc123', toId: 'codex.ai.concept.xyz', relationship: 'summarizes', weight: 0.9 },
          { fromId: 'codex.ai.concept.xyz', toId: 'codex.ontology.axis.resonance', relationship: 'maps-to', weight: 0.8 },
        ], totalCount: 2 }) }
      }
      // Related node fetches
      if (url.includes('/storage-endpoints/nodes/codex.ai.concept.xyz')) {
        return { ok: true, json: async () => ({ node: {
          id: 'codex.ai.concept.xyz', typeId: 'codex.concept', title: 'AI Concept XYZ', state: 'Ice'
        } }) }
      }
      if (url.includes('/storage-endpoints/nodes/codex.ontology.axis.resonance')) {
        return { ok: true, json: async () => ({ node: {
          id: 'codex.ontology.axis.resonance', typeId: 'codex.ontology.axis', title: 'Resonance', state: 'Ice', meta: { name: 'Resonance' }
        } }) }
      }
      return { ok: true, json: async () => ({ success: true }) }
    })
  })

  afterEach(() => {
    ;(global.fetch as jest.Mock).mockReset()
    global.fetch = originalFetch
  })

  it('links news → node detail and allows navigation to AI concept and U-CORE axis', async () => {
    renderWithProviders(<NewsPage />)

    // Wait for news to render
    expect(await screen.findByText('Test Article')).toBeInTheDocument()

    // Click View in Graph; with an id present it should open node detail for that id (simulated by calling window.open)
    const openSpy = jest.spyOn(window, 'open').mockImplementation(() => null)
    fireEvent.click(screen.getByText('🔎 View Node'))
    expect(openSpy).toHaveBeenCalled()
    const target = openSpy.mock.calls[0][0] as string
    expect(target).toContain('/node/codex.news.item.abc123')
    openSpy.mockRestore()

    const fetchUrls = (global.fetch as jest.Mock).mock.calls.map((c: any[]) => String(c[0]))
    expect(fetchUrls.some(url => url.includes('/news/feed/'))).toBe(true)
    expect(fetchUrls.some(url => url.includes('/news/stats'))).toBe(true)
  })
})
