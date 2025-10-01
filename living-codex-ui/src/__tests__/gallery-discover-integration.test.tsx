import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import DiscoverPage from '@/app/discover/page'

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), back: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/discover',
}))

// Mock the AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

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
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock the hooks
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
}))

describe('Gallery Discover Integration Tests', () => {
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
    // Mock fetch for concepts endpoint
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockConceptsData),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('should display Gallery tab in navigation', async () => {
    renderWithProviders(<DiscoverPage />)
    
    await waitFor(() => {
      expect(screen.getByText('Gallery')).toBeInTheDocument()
    })
  })

  test('should switch to Gallery lens when Gallery tab is clicked', async () => {
    renderWithProviders(<DiscoverPage />)
    
    // Click on Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery content to load
    await waitFor(() => {
      expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
    })
  })

  test('should display concepts in gallery grid format', async () => {
    renderWithProviders(<DiscoverPage />)
    
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
    renderWithProviders(<DiscoverPage />)
    
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
    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for concepts to load and check domains
    await waitFor(() => {
      expect(screen.getByText('General')).toBeInTheDocument()
      expect(screen.getByText('Mathematics')).toBeInTheDocument()
    })
  })

  test('should display resonance values in gallery cards', async () => {
    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for concepts to load and check resonance values
    await waitFor(() => {
      expect(screen.getByText('Resonance: 0.75')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 0.85')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 0.65')).toBeInTheDocument()
    })
  })

  test('should show concept count in gallery header', async () => {
    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for gallery header to load
    await waitFor(() => {
      expect(screen.getByText(/3 concepts/)).toBeInTheDocument()
    })
  })

  test('should handle gallery item click to open modal', async () => {
    renderWithProviders(<DiscoverPage />)
    
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
    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for controls to load
    await waitFor(() => {
      expect(screen.getByText('All Concepts')).toBeInTheDocument()
      expect(screen.getByText('By Resonance')).toBeInTheDocument()
    })
  })

  test('should handle backend error gracefully', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: false,
          status: 500,
          statusText: 'Internal Server Error',
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Wait for error message
    await waitFor(() => {
      expect(screen.getByText('Error Loading Gallery')).toBeInTheDocument()
      expect(screen.getByText(/Failed to fetch concepts: 500/)).toBeInTheDocument()
    })
  })

  test('should show loading state while fetching concepts', async () => {
    // Mock fetch to delay response
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: () => Promise.resolve(mockConceptsData),
            })
          }, 100)
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<DiscoverPage />)
    
    // Switch to Gallery tab
    const galleryTab = screen.getByText('Gallery')
    fireEvent.click(galleryTab)
    
    // Check loading state
    expect(screen.getByText('Loading concepts...')).toBeInTheDocument()
    
    // Wait for concepts to load
    await waitFor(() => {
      expect(screen.getByText('Quantum')).toBeInTheDocument()
    })
  })
})
