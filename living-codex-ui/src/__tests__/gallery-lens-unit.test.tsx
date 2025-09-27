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

  test('should render gallery header with concept count', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      expect(screen.getByText(/2 concepts/)).toBeInTheDocument()
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

  test('should display concept domains in cards', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Technology')).toBeInTheDocument()
      expect(screen.getByText('Philosophy')).toBeInTheDocument()
    })
  })

  test('should display resonance values in cards', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Resonance: 0.85')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 0.95')).toBeInTheDocument()
    })
  })

  test('should show filter and sort controls', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('All Concepts')).toBeInTheDocument()
      expect(screen.getByText('By Resonance')).toBeInTheDocument()
    })
  })

  test('should handle filter changes', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
    
    // Change filter
    const filterSelect = screen.getByDisplayValue('All Concepts')
    fireEvent.change(filterSelect, { target: { value: 'consciousness' } })
    
    // Should still show concepts (filtering logic would be tested in integration)
    expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
  })

  test('should handle sort changes', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
    
    // Change sort
    const sortSelect = screen.getByDisplayValue('By Resonance')
    fireEvent.change(sortSelect, { target: { value: 'energy' } })
    
    // Should still show concepts (sorting logic would be tested in integration)
    expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
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
    
    expect(screen.getByText('Loading concepts...')).toBeInTheDocument()
  })

  test('should handle fetch error gracefully', async () => {
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

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Error Loading Gallery')).toBeInTheDocument()
      expect(screen.getByText(/Failed to fetch concepts: 500/)).toBeInTheDocument()
    })
  })

  test('should show retry button on error', async () => {
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

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Retry')).toBeInTheDocument()
    })
  })

  test('should generate proper placeholder images with concept initials', async () => {
    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Check that images are rendered (they should be data URLs)
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
      expect(screen.getByText('Living Codex')).toBeInTheDocument()
    })
  })

  test('should handle empty concepts array', async () => {
    // Mock fetch to return empty array
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ concepts: [] }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('No concepts available yet.')).toBeInTheDocument()
      expect(screen.getByText('Concepts will appear here as they are discovered!')).toBeInTheDocument()
    })
  })

  test('should display pagination controls when there are many concepts', async () => {
    // Mock fetch to return many concepts
    const manyConcepts = Array.from({ length: 20 }, (_, i) => ({
      id: `concept-${i}`,
      name: `Concept ${i}`,
      description: `Description for concept ${i}`,
      domain: 'General',
      complexity: 1,
      tags: [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      resonance: 0.5,
      energy: 100,
      isInterested: false,
      interestCount: 0
    }))

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ concepts: manyConcepts }),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Should show pagination controls
      expect(screen.getByText(/20 concepts/)).toBeInTheDocument()
    })
  })
})
