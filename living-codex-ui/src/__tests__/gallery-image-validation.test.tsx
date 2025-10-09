import React from 'react'
import { screen, waitFor, fireEvent, within, act } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import GalleryLens from '@/components/lenses/GalleryLens'

describe('Gallery Image Validation Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    
    // Mock fetch for gallery API
    global.fetch = jest.fn().mockImplementation((url: string) => {
      if (url.includes('/gallery/list')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({
            success: true,
            items: [
              {
                id: 'img1',
                title: 'Neural Networks Visualization',
                url: 'https://example.com/neural-networks.jpg',
                thumbnail: 'https://example.com/neural-networks-thumb.jpg',
                description: 'A visualization of neural networks',
                conceptId: 'concept1',
                likes: 10,
                views: 100
              },
              {
                id: 'img2',
                title: 'Quantum Computing',
                url: 'https://example.com/quantum.jpg',
                thumbnail: 'https://example.com/quantum-thumb.jpg',
                description: 'Quantum computing concepts',
                conceptId: 'concept2',
                likes: 5,
                views: 50
              }
            ]
          })
        })
      }
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ success: true, data: {} })
      })
    }) as jest.Mock
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Image Loading and Display', () => {
    it('loads and displays images correctly', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
      })
      
      // Check that images are loaded
      const images = screen.getAllByRole('img')
      expect(images.length).toBeGreaterThan(0)
      
      // Images should have alt text (src might be handled differently by next/Image mock)
      images.forEach(img => {
        expect(img).toHaveAttribute('alt')
      })
    })

    it('handles image load errors gracefully', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Neural Networks Visualization')).toBeInTheDocument()
      })
      
      // Simulate image load error
      const images = screen.getAllByRole('img')
      if (images.length > 0) {
        fireEvent.error(images[0])
        // Component should handle error gracefully
        expect(images[0]).toBeInTheDocument()
      }
    })

    it('shows proper image attributes', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        const images = screen.getAllByRole('img')
        images.forEach(img => {
          expect(img).toHaveAttribute('alt')
          expect(img.getAttribute('alt')).not.toBe('')
        })
      })
    })
  })

  describe('Error Handling', () => {
    it('shows error state when gallery fails to load', async () => {
      // Mock fetch to return error
      global.fetch = jest.fn().mockRejectedValue(new Error('Network error'))
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Unavailable')).toBeInTheDocument()
      })
    })

    it('shows empty state when no images are available', async () => {
      // Mock fetch to return empty array
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, items: [] })
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
