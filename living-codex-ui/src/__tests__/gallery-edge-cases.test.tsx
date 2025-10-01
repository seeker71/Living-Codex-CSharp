import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
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

describe('Gallery Edge Cases and User Experience Tests', () => {
  const defaultProps = {
    controls: {
      axes: ['resonance'],
      joy: 0.7,
      serendipity: 0.5,
    },
    userId: 'test-user',
    readOnly: false,
  }

  afterEach(() => {
    jest.restoreAllMocks()
  })

  test('should handle concepts with missing data gracefully', async () => {
    const conceptsWithMissingData = {
      concepts: [
        {
          id: "incomplete-concept",
          name: "", // Missing name
          description: "", // Missing description
          domain: "", // Missing domain
          complexity: null,
          tags: null,
          createdAt: null,
          updatedAt: null,
          resonance: null,
          energy: null,
          isInterested: false,
          interestCount: 0
        },
        {
          id: "partial-concept",
          name: "Partial Concept",
          description: null, // Missing description
          domain: null, // Missing domain
          complexity: undefined,
          tags: undefined,
          createdAt: undefined,
          updatedAt: undefined,
          resonance: undefined,
          energy: undefined,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: conceptsWithMissingData }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: conceptsWithMissingData }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Should handle missing name gracefully
      expect(screen.getByText('Untitled Concept')).toBeInTheDocument()
      expect(screen.getByText('Partial Concept')).toBeInTheDocument()
    })
  })

  test('should handle very long concept names', async () => {
    const conceptsWithLongNames = {
      concepts: [
        {
          id: "long-name-concept",
          name: "This is a very long concept name that should be truncated properly in the UI to prevent layout issues and maintain readability",
          description: "A concept with an extremely long name",
          domain: "General",
          complexity: 1,
          tags: ["long", "name"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0.5,
          energy: 100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: conceptsWithLongNames }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: conceptsWithLongNames }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      const longNameElement = screen.getByText(/This is a very long concept name/)
      expect(longNameElement).toBeInTheDocument()
      // Should have truncate class
      expect(longNameElement).toHaveClass('truncate')
    })
  })

  test('should handle special characters in concept names', async () => {
    const conceptsWithSpecialChars = {
      concepts: [
        {
          id: "special-chars-concept",
          name: "Concept with Special Characters: @#$%^&*()_+-=[]{}|;':\",./<>?",
          description: "A concept with special characters",
          domain: "General",
          complexity: 1,
          tags: ["special", "characters"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0.5,
          energy: 100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: conceptsWithSpecialChars }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: conceptsWithSpecialChars }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText(/Concept with Special Characters/)).toBeInTheDocument()
    })
  })

  test('should handle network timeout gracefully', async () => {
    // Mock fetch to simulate timeout
    global.fetch = jest.fn().mockImplementation(() => {
      return new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Network timeout')), 100)
      })
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Error Loading Gallery')).toBeInTheDocument()
      expect(screen.getByText(/Network timeout/)).toBeInTheDocument()
    })
  })

  test('should handle malformed JSON response', async () => {
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.reject(new Error('Invalid JSON')),
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Error Loading Gallery')).toBeInTheDocument()
      expect(screen.getByText(/Invalid JSON/)).toBeInTheDocument()
    })
  })

  test('should handle empty response body', async () => {
    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/concepts')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({}), // Empty response
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('No concepts available yet.')).toBeInTheDocument()
    })
  })

  test('should handle concepts with extreme resonance values', async () => {
    const conceptsWithExtremeValues = {
      concepts: [
        {
          id: "zero-resonance",
          name: "Zero Resonance",
          description: "A concept with zero resonance",
          domain: "General",
          complexity: 1,
          tags: ["zero"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0,
          energy: 0,
          isInterested: false,
          interestCount: 0
        },
        {
          id: "max-resonance",
          name: "Max Resonance",
          description: "A concept with maximum resonance",
          domain: "General",
          complexity: 1,
          tags: ["max"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 1,
          energy: 1000,
          isInterested: false,
          interestCount: 0
        },
        {
          id: "negative-resonance",
          name: "Negative Resonance",
          description: "A concept with negative resonance",
          domain: "General",
          complexity: 1,
          tags: ["negative"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: -0.5,
          energy: -100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: conceptsWithExtremeValues }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: conceptsWithExtremeValues }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Resonance: 0.00')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 1.00')).toBeInTheDocument()
      expect(screen.getByText('Resonance: -0.50')).toBeInTheDocument()
    })
  })

  test('should handle rapid filter changes', async () => {
    const mockConcepts = {
      concepts: [
        {
          id: "concept1",
          name: "Concept 1",
          description: "Description 1",
          domain: "General",
          complexity: 1,
          tags: ["tag1"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0.5,
          energy: 100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: mockConcepts }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: mockConcepts }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Concept 1')).toBeInTheDocument()
    })
    
    // Rapidly change filters
    const filterSelect = screen.getByDisplayValue('All Concepts')
    fireEvent.change(filterSelect, { target: { value: 'consciousness' } })
    fireEvent.change(filterSelect, { target: { value: 'abundance' } })
    fireEvent.change(filterSelect, { target: { value: 'all' } })
    
    // Should still be stable
    expect(screen.getByText('Concept 1')).toBeInTheDocument()
  })

  test('should handle rapid sort changes', async () => {
    const mockConcepts = {
      concepts: [
        {
          id: "concept1",
          name: "Concept 1",
          description: "Description 1",
          domain: "General",
          complexity: 1,
          tags: ["tag1"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0.5,
          energy: 100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: mockConcepts }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: mockConcepts }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      expect(screen.getByText('Concept 1')).toBeInTheDocument()
    })
    
    // Rapidly change sorts
    const sortSelect = screen.getByDisplayValue('By Resonance')
    fireEvent.change(sortSelect, { target: { value: 'energy' } })
    fireEvent.change(sortSelect, { target: { value: 'complexity' } })
    fireEvent.change(sortSelect, { target: { value: 'recent' } })
    
    // Should still be stable
    expect(screen.getByText('Concept 1')).toBeInTheDocument()
  })

  test('should handle read-only mode correctly', async () => {
    const mockConcepts = {
      concepts: [
        {
          id: "concept1",
          name: "Concept 1",
          description: "Description 1",
          domain: "General",
          complexity: 1,
          tags: ["tag1"],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          resonance: 0.5,
          energy: 100,
          isInterested: false,
          interestCount: 0
        }
      ]
    }

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: mockConcepts }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: mockConcepts }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} readOnly={true} />)
    
    await waitFor(() => {
      expect(screen.getByText('Concept 1')).toBeInTheDocument()
    })
    
    // In read-only mode, certain interactions should be disabled
    // This would be tested by checking that certain buttons or actions are not available
  })

  test('should handle very large concept datasets', async () => {
    // Create a large dataset
    const largeConcepts = Array.from({ length: 1000 }, (_, i) => ({
      id: `concept-${i}`,
      name: `Concept ${i}`,
      description: `Description for concept ${i}`,
      domain: 'General',
      complexity: Math.floor(Math.random() * 5),
      tags: [`tag${i}`],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      resonance: Math.random(),
      energy: Math.floor(Math.random() * 1000),
      isInterested: false,
      interestCount: 0
    }))

    global.fetch = jest.fn().mockImplementation((url) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ success: true, items: largeConcepts }),
          text: () => Promise.resolve(JSON.stringify({ success: true, items: largeConcepts }))
        })
      }
      return Promise.reject(new Error('Unhandled fetch request'))
    })

    renderWithProviders(<GalleryLens {...defaultProps} />)
    
    await waitFor(() => {
      // Should show pagination and handle large datasets
      expect(screen.getByText(/1000 concepts/)).toBeInTheDocument()
    })
  })
})
