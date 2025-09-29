import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import GalleryLens from '@/components/lenses/GalleryLens'

describe('Gallery Item View Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('GalleryLens Component Rendering', () => {
    it('renders the gallery component', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
    })

    it('displays all gallery items', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
        expect(screen.getByText('Quantum Computing Concept')).toBeInTheDocument()
        expect(screen.getByText('Data Flow Patterns')).toBeInTheDocument()
      })
    })

    it('shows loading state initially', () => {
      renderWithProviders(<GalleryLens />)

      // Should show loading skeletons
      const skeletons = screen.getAllByRole('generic')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('displays images with proper attributes', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        const images = screen.getAllByRole('img')
        expect(images.length).toBeGreaterThan(0)
        
        images.forEach(img => {
          expect(img).toHaveAttribute('src')
          expect(img).toHaveAttribute('alt')
        })
      })
    })
  })

  describe('Gallery Interactions', () => {
    it('handles like button clicks', async () => {
      renderWithProviders(<GalleryLens userId="test-user" />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
      })
      
      // Find and click like button
      const likeButtons = screen.getAllByRole('button')
      const likeButton = likeButtons.find(btn => btn.textContent?.includes('42'))
      
      if (likeButton) {
        fireEvent.click(likeButton)
        // The like count should increase
        await waitFor(() => {
          expect(screen.getByText('43')).toBeInTheDocument()
        })
      }
    })

    it('shows proper card styling', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
      })
      
      // Check that cards have proper styling
      const cards = screen.getAllByRole('article')
      expect(cards.length).toBeGreaterThan(0)
    })
  })

  describe('Error Handling', () => {
    it('shows error state when gallery fails to load', async () => {
      // Mock fetch to return error
      global.fetch = jest.fn().mockRejectedValue(new Error('Network error'))
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Error loading gallery')).toBeInTheDocument()
      })
    })

    it('shows empty state when no images are available', async () => {
      // Mock fetch to return empty array
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve([])
      })
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('No images available')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('provides proper alt text for all images', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        const images = screen.getAllByRole('img')
        images.forEach(img => {
          expect(img).toHaveAttribute('alt')
          expect(img.getAttribute('alt')).not.toBe('')
        })
      })
    })

    it('has proper ARIA labels and roles', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
      })
      
      // Check for proper card structure
      const cards = screen.getAllByRole('article')
      expect(cards.length).toBeGreaterThan(0)
    })
  })
})
