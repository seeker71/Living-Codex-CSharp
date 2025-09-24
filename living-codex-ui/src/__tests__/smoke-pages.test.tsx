import React from 'react'
import { act } from '@testing-library/react'
import { renderWithProviders } from './test-utils'

let originalFetch: typeof global.fetch

const mockAuthContext = {
  user: null,
  token: null,
  isLoading: false,
  isAuthenticated: false,
  login: jest.fn().mockResolvedValue({ success: true }),
  register: jest.fn().mockResolvedValue({ success: true }),
  logout: jest.fn(),
  refreshUser: jest.fn().mockResolvedValue(undefined),
  testConnection: jest.fn().mockResolvedValue(true),
}

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => mockAuthContext,
  AuthProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}))

// Mock Next.js app router navigation hooks used by pages
jest.mock('next/navigation', () => {
  return {
    useRouter: () => ({ push: jest.fn(), replace: jest.fn(), back: jest.fn() }),
    useParams: () => ({}),
    useSearchParams: () => ({ get: () => null }),
  }
})

// Mock next/link to simple anchor for tests
jest.mock('next/link', () => ({
  __esModule: true,
  default: ({ href, children, ...props }: any) => <a href={typeof href === 'string' ? href : '#'} {...props}>{children}</a>,
}))

function createMockResponse(url: string) {
  if (url.includes('/contributions/abundance/collective-energy')) {
    return {
      collectiveResonance: 72.5,
      totalContributors: 128,
      totalAbundanceEvents: 42,
      recentAbundanceEvents: 5,
      averageAbundanceMultiplier: 1.18,
      totalCollectiveValue: 5600,
      timestamp: new Date().toISOString(),
    }
  }

  if (url.includes('/contributions/abundance/contributor-energy')) {
    return {
      userId: 'demo-user',
      energyLevel: 64.2,
      baseEnergy: 40,
      amplifiedEnergy: 92,
      resonanceLevel: 73.4,
      totalContributions: 21,
      totalValue: 1200,
      totalCollectiveValue: 5600,
      averageAbundanceMultiplier: 1.18,
      lastUpdated: new Date().toISOString(),
    }
  }

  if (url.includes('/storage-endpoints/nodes')) {
    return { nodes: [] }
  }

  if (url.includes('/storage-endpoints/edges')) {
    return { edges: [] }
  }

  if (url.includes('/storage-endpoints/types')) {
    return { types: [] }
  }

  if (url.includes('/storage-endpoints/stats')) {
    return {
      nodeCount: 0,
      edgeCount: 0,
      moduleCount: 0,
      uptime: '24h',
      requestCount: 0,
    }
  }

  if (url.includes('/filesystem/files')) {
    return { files: [] }
  }

  if (url.includes('/news/stats')) {
    return {
      totalArticles: 0,
      trendingTopics: [],
      success: true,
    }
  }

  if (url.includes('/ai/extract-concepts')) {
    return { concepts: [] }
  }

  if (url.includes('/concept/create')) {
    return { success: true, conceptId: 'mock-concept' }
  }

  if (url.includes('/graph/overview')) {
    return { nodes: [], edges: [] }
  }

  if (url.includes('/health')) {
    return {
      status: 'ok',
      nodeCount: 0,
      edgeCount: 0,
      moduleCount: 0,
      uptime: '24h',
      requestCount: 0,
    }
  }

  return { success: true }
}

// Very permissive global fetch mock to avoid network calls in smoke tests
beforeAll(() => {
  originalFetch = global.fetch

  // @ts-ignore
  global.fetch = jest.fn(async (input: RequestInfo | URL) => {
    const url = typeof input === 'string'
      ? input
      : input instanceof URL
        ? input.toString()
        : input?.url ?? ''

    const payload = createMockResponse(url)
    return {
      ok: true,
      status: 200,
      json: async () => payload,
    } as any
  })
})

afterAll(() => {
  if (global.fetch && typeof (global.fetch as any).mockReset === 'function') {
    ;(global.fetch as jest.Mock).mockReset()
  }
  global.fetch = originalFetch
})

// Import pages (only static routes for smoke coverage)
import AboutPage from '@/app/about/page'
import CodePage from '@/app/code/page'
import CreatePage from '@/app/create/page'
import DevPage from '@/app/dev/page'
import DiscoverPage from '@/app/discover/page'
import GraphPage from '@/app/graph/page'
import NewsPage from '@/app/news/page'
import NodesPage from '@/app/nodes/page'
import OntologyPage from '@/app/ontology/page'
import PeoplePage from '@/app/people/page'
import PortalsPage from '@/app/portals/page'
import ProfilePage from '@/app/profile/page'
import ResonancePage from '@/app/resonance/page'

describe('Smoke test: pages render', () => {
  const cases: Array<[string, React.ComponentType<any>]> = [
    ['about', AboutPage],
    ['code', CodePage],
    ['create', CreatePage],
    ['dev', DevPage],
    ['discover', DiscoverPage],
    ['graph', GraphPage],
    ['news', NewsPage],
    ['nodes', NodesPage],
    ['ontology', OntologyPage],
    ['people', PeoplePage],
    ['portals', PortalsPage],
    ['profile', ProfilePage],
    ['resonance', ResonancePage],
  ]

  it.each(cases)('renders /%s without crashing', async (_name, Page) => {
    await act(async () => {
      renderWithProviders(<Page />)
      await Promise.resolve()
    })
  })
})
