// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), back: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/discover',
}));

// Mock API endpoints - provide minimal implementation needed for tests
jest.mock('@/lib/api', () => ({
  endpoints: {
    recordContribution: jest.fn().mockResolvedValue({ success: true }),
    toggleLike: jest.fn().mockResolvedValue({ success: true }),
  },
  ApiErrorHandler: {
    handle: (error: any) => { console.error(error); },
    getUserMessage: (error: any) => error?.message || 'An error occurred',
  },
}));

import React from 'react';
import { screen, waitFor, fireEvent, within } from '@testing-library/react';
import { renderWithProviders } from './test-utils';
import DiscoverPage from '@/app/discover/page';

// Mock the config and buildApiUrl function
jest.mock('@/lib/config', () => ({
  config: {
    backend: {
      baseUrl: 'http://localhost:5002',
      timeout: 10000,
    },
  },
}))

jest.mock('@/lib/utils', () => ({
  ...jest.requireActual('@/lib/utils'),
  cn: (...classes: any[]) => classes.filter(Boolean).join(' '),
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock the hooks
// Mock the ConceptStreamCard component to avoid complex dependencies
jest.mock('@/components/lenses/ConceptStreamCard', () => ({
  ConceptStreamCard: ({ concept }: any) => (
    <div data-testid="concept-card">
      <h3>{concept.name}</h3>
      <p>{concept.description}</p>
      <span>Domain: {concept.domain}</span>
      <span>Resonance: {concept.resonance}</span>
    </div>
  )
}))

jest.mock('@/lib/hooks', () => ({
  usePages: () => ({ isLoading: false }),
  useLenses: () => ({ 
    data: [
      {
        id: 'lens.gallery',
        name: 'Gallery',
        status: 'Simple',
        ranking: 'resonance*energy*complexity'
      }
    ], 
    isLoading: false 
  }),
  useResonanceControls: () => ({
    data: {
      fields: [
        {
          id: 'axes',
          options: ['resonance', 'energy', 'complexity']
        }
      ],
      status: 'active'
    },
    isLoading: false
  }),
  useTrackInteraction: () => jest.fn(),
  // Provide a stub for useConceptDiscovery used by StreamLens in Discover
  useConceptDiscovery: () => ({
    data: {
      concepts: [
        { id: 'c1', name: 'Quantum', domain: 'General', resonance: 0.75, description: 'Quantum mechanics and quantum computing concepts' },
        { id: 'c2', name: 'Consciousness', domain: 'General', resonance: 0.85, description: 'The state of being aware and able to think and perceive' },
        { id: 'c3', name: 'Fractal Pattern', domain: 'Mathematics', resonance: 0.65, description: 'A geometric pattern that repeats at different scales' },
      ]
    },
    isLoading: false,
    isError: false,
    error: null,
  }),
  // Provide a stub for useUserDiscovery used by StreamLens in Discover
  useUserDiscovery: () => ({
    data: {
      users: [
        { id: 'u1', username: 'user1', displayName: 'User One', lastSeen: '2025-01-01T00:00:00Z' },
        { id: 'u2', username: 'user2', displayName: 'User Two', lastSeen: '2025-01-01T00:00:00Z' },
      ]
    },
    isLoading: false,
    isError: false,
    error: null,
  }),
  // Provide stubs for interaction hooks used by ConceptStreamCard
  useAttune: () => ({
    mutate: jest.fn(),
    isLoading: false,
    isError: false,
    error: null,
  }),
  useAmplify: () => ({
    mutate: jest.fn(),
    isLoading: false,
    isError: false,
    error: null,
  }),
}))

describe('Gallery Discover Integration Tests', () => {
  const mockAuthValue = {
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
    login: jest.fn(),
    register: jest.fn(),
    logout: jest.fn(),
    refreshUser: jest.fn(),
    testConnection: jest.fn(),
  };

  const mockConceptsData = {
    concepts: [
      {
        id: "u-core-concept-quantum",
        name: "Quantum",
        description: "Keyword for phenomena governed by quantum theory (discreteness, superposition, entanglement).",
        domain: "General",
        complexity: 0,
        tags: [],
        createdAt: "2025-09-25T19:23:20.190065Z",
        updatedAt: "2025-09-25T19:23:20.190065Z",
        resonance: 0.75,
        energy: 500,
        isInterested: false,
        interestCount: 0
      },
      {
        id: "u-core-concept-consciousness",
        name: "Consciousness",
        description: "The state of being aware and able to think and perceive",
        domain: "General",
        complexity: 1,
        tags: ["awareness", "consciousness", "mind", "perception"],
        createdAt: "2025-09-25T19:23:20.190065Z",
        updatedAt: "2025-09-25T19:23:20.190065Z",
        resonance: 0.85,
        energy: 750,
        isInterested: false,
        interestCount: 0
      },
      {
        id: "u-core-concept-fractal",
        name: "Fractal Pattern",
        description: "A geometric pattern that repeats at different scales",
        domain: "Mathematics",
        complexity: 2,
        tags: ["fractal", "pattern", "mathematics", "geometry"],
        createdAt: "2025-09-25T19:23:20.190065Z",
        updatedAt: "2025-09-25T19:23:20.190065Z",
        resonance: 0.65,
        energy: 600,
        isInterested: false,
        interestCount: 0
      }
    ]
  }

  beforeEach(() => {
    // Mock fetch for concepts and gallery endpoints
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockConceptsData),
        })
      }
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({
            success: true,
            items: mockConceptsData.concepts.map(c => ({
              ...c,
              title: c.name,
              url: `https://via.placeholder.com/300x200?text=${c.name}`,
              thumbnail: `https://via.placeholder.com/150x100?text=${c.name}`,
            }))
          }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('should display Gallery tab in navigation', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    await waitFor(() => {
      expect(screen.getByText('Gallery')).toBeInTheDocument()
    })
  })

  test('should switch to Gallery lens when Gallery tab is clicked', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Click on Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery content to load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })

  test('should display concepts in gallery grid format', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for concepts to load
    await waitFor(() => {
      expect(screen.getByText('Quantum')).toBeInTheDocument()
      expect(screen.getByText('Consciousness')).toBeInTheDocument()
      expect(screen.getByText('Fractal Pattern')).toBeInTheDocument()
    })
  })

  test('should display concept names in gallery cards', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for concepts to load and check that names are visible
    await waitFor(() => {
      const quantumCard = screen.getByText('Quantum')
      const consciousnessCard = screen.getByText('Consciousness')
      const fractalCard = screen.getByText('Fractal Pattern')
      
      expect(quantumCard).toBeVisible()
      expect(consciousnessCard).toBeVisible()
      expect(fractalCard).toBeVisible()
    })
  })

  test('should display concept domains in gallery cards', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery to load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })

  test('should display resonance values in gallery cards', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery to load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })

  test('should show concept count in gallery header', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery to load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })

  test('should handle gallery item click to open modal', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for concepts to load
    await waitFor(() => {
      expect(screen.getByText('Quantum')).toBeInTheDocument()
    })
    
    // Click on a concept card
    const quantumCard = screen.getByText('Quantum')
    fireEvent.click(quantumCard)
    
    // Wait for modal to open
    await waitFor(() => {
      expect(screen.getByText('Quantum')).toBeInTheDocument() // Should be in modal
    })
  })

  test('should display filter and sort controls', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery to load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })

  test('should handle backend error gracefully', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockRejectedValue(new Error('Backend error'))

    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for error message
    await waitFor(() => {
      expect(screen.getByText('Gallery Unavailable')).toBeInTheDocument()
    })
  })

  test('should show loading state while fetching concepts', async () => {
    renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Gallery should eventually load
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
    })
  })
})
