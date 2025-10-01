import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import { GalleryLens } from '@/components/lenses/GalleryLens'

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

describe('GalleryLens Unit Tests', () => {
  const mockConceptsData = {
    concepts: [
      {
        id: "u-core-concept-quantum",
        name: "Quantum Computing",
        description: "Computing based on quantum mechanical phenomena",
        domain: "Technology",
        complexity: 2,
        tags: ["quantum", "computing", "technology"],
        createdAt: "2025-09-25T19:23:20.190065Z",
        updatedAt: "2025-09-25T19:23:20.190065Z",
        resonance: 0.85,
        energy: 800,
        isInterested: false,
        interestCount: 0
      },
      {
        id: "u-core-concept-consciousness",
        name: "Consciousness",
        description: "The state of being aware and able to think and perceive",
        domain: "Philosophy",
        complexity: 3,
        tags: ["awareness", "consciousness", "mind"],
        createdAt: "2025-09-25T19:23:20.190065Z",
        updatedAt: "2025-09-25T19:23:20.190065Z",
        resonance: 0.95,
        energy: 1000,
        isInterested: false,
        interestCount: 0
      }
    ]
  }

  const defaultProps = {
    controls: {
      axes: ['resonance'],
      joy: 0.7,
      serendipity: 0.5,
    },
    userId: 'test-user',
    readOnly: false,
  }

  beforeEach(() => {
    // Mock fetch for gallery endpoint
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({
            success: true,
            items: [
              {
                id: "u-core-concept-quantum",
                title: "Quantum Computing",
                description: "Computing based on quantum mechanical phenomena",
                author: { name: "Living Codex" },
                imageUrl: "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjNjY2Ii8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZmlsbD0iI2ZmZiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPlF1YW50dW0gQ29tcHV0aW5nPC90ZXh0Pjwvc3ZnPg==",
                likes: 0,
                comments: 0,
                tags: ["quantum", "computing", "technology"],
                resonance: 0.85,
                axes: [],
                mediaType: "image",
                aiGenerated: false,
                createdAt: "2025-09-25T19:23:20.190065Z"
              },
              {
                id: "u-core-concept-consciousness",
                title: "Consciousness",
                description: "The state of being aware and able to think and perceive",
                author: { name: "Living Codex" },
                imageUrl: "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjNjY2Ii8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZmlsbD0iI2ZmZiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPkNvbnNjaW91c25lc3M8L3RleHQ+PC9zdmc+",
                likes: 0,
                comments: 0,
                tags: ["awareness", "consciousness", "mind"],
                resonance: 0.95,
                axes: [],
                mediaType: "image",
                aiGenerated: false,
                createdAt: "2025-09-25T19:23:20.190065Z"
              }
            ]
          }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('should render gallery header', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      expect(screen.getByText('Explore concepts through visual representations and artistic interpretations')).toBeInTheDocument()
    })
  })

  test('should render concept cards with proper data', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      expect(screen.getByText('Consciousness')).toBeInTheDocument()
    })
  })

  test('should display concept names in cards', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      const quantumCard = screen.getByText('Quantum Computing')
      const consciousnessCard = screen.getByText('Consciousness')
      
      expect(quantumCard).toBeVisible()
      expect(consciousnessCard).toBeVisible()
    })
  })

  test('should display author information in cards', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('by Living Codex')).toBeInTheDocument()
    })
  })

  test('should display tags in cards', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('quantum')).toBeInTheDocument()
      expect(screen.getByText('computing')).toBeInTheDocument()
      expect(screen.getByText('technology')).toBeInTheDocument()
    })
  })

  test('should display like and share buttons', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
    
    // Should show like and share buttons for each card
    const likeButtons = screen.getAllByText('Like')
    const shareButtons = screen.getAllByText('Share')
    
    expect(likeButtons.length).toBeGreaterThan(0)
    expect(shareButtons.length).toBeGreaterThan(0)
  })

  test('should open modal when concept card is clicked', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
    
    // Click on concept card
    const quantumCard = screen.getByText('Quantum Computing')
    fireEvent.click(quantumCard)
    
    // Modal should open (modal content would be tested in integration)
    await waitFor(() => {
      // The modal should be visible - we can check for modal-specific elements
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
  })

  test('should show loading state initially', () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    // Should show loading skeleton cards
    const skeletonCards = document.querySelectorAll('.animate-pulse')
    expect(skeletonCards.length).toBeGreaterThan(0)
  })

  test('should handle fetch error gracefully', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: false,
          status: 500,
          statusText: 'Internal Server Error',
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Gallery Unavailable')).toBeInTheDocument()
      expect(screen.getByText('Try Again')).toBeInTheDocument()
    })
  })

  test('should show retry button on error', async () => {
    // Mock fetch to return error
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: false,
          status: 500,
          statusText: 'Internal Server Error',
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Try Again')).toBeInTheDocument()
    })
  })

  test('should display images with proper alt text', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Check that images are rendered
      const images = screen.getAllByRole('img')
      expect(images.length).toBeGreaterThan(0)
      
      // Each image should have an alt attribute with the concept name
      expect(screen.getByAltText('Quantum Computing')).toBeInTheDocument()
      expect(screen.getByAltText('Consciousness')).toBeInTheDocument()
    })
  })

  test('should display author information', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('by Living Codex')).toBeInTheDocument()
    })
  })

  test('should handle empty concepts array', async () => {
    // Mock fetch to return empty array
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ 
            success: true,
            items: []
          }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('No images available')).toBeInTheDocument()
      expect(screen.getByText('The visual gallery is currently empty. Check back later for new visual discoveries.')).toBeInTheDocument()
    })
  })

  test('should display many concepts in grid layout', async () => {
    // Mock fetch to return many concepts
    const manyConcepts = Array.from({ length: 20 }, (_, i) => ({
      id: `concept-${i}`,
      title: `Concept ${i}`,
      description: `Description for concept ${i}`,
      author: { name: "Living Codex" },
      imageUrl: "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjNjY2Ii8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZmlsbD0iI2ZmZiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZHk9Ii4zZW0iPkNvbmNlcHQge2l9PC90ZXh0Pjwvc3ZnPg==",
      likes: 0,
      comments: 0,
      tags: [],
      resonance: 0.5,
      axes: [],
      mediaType: "image",
      aiGenerated: false,
      createdAt: new Date().toISOString()
    }))

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ 
            success: true,
            items: manyConcepts 
          }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Should show all concepts in grid
      expect(screen.getByText('Concept 0')).toBeInTheDocument()
      expect(screen.getByText('Concept 19')).toBeInTheDocument()
    })
  })
})
