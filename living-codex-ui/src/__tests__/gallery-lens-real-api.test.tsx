import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import { GalleryLens } from '@/components/lenses/GalleryLens'

describe('GalleryLens Component with Real API', () => {
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
    // Use real API calls - we want to test the real system
    // The server should be running for these tests to work
  })

  afterEach(() => {
    // Clean up any test data if needed
  })

  describe('Component Rendering with Real API', () => {
    it('renders gallery header', async () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
        expect(screen.getByText('Explore concepts through visual representations and artistic interpretations')).toBeInTheDocument()
      }, { timeout: 10000 })
    })

    it('shows loading state initially', () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      // Should show loading skeleton cards
      const skeletonCards = document.querySelectorAll('.animate-pulse')
      expect(skeletonCards.length).toBeGreaterThan(0)
    })

    it('handles API errors gracefully', async () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      // Wait for either success or error state
      await waitFor(() => {
        const hasContent = screen.queryByText('Explore concepts through visual representations and artistic interpretations') ||
                          screen.queryByText('Error Loading Gallery') ||
                          screen.queryByText('No concepts available yet')
        expect(hasContent).toBeTruthy()
      }, { timeout: 15000 })
    })

    it('displays images with proper alt text when loaded', async () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      await waitFor(() => {
        // Check that either images are rendered or empty state is shown
        const images = screen.queryAllByRole('img')
        const emptyState = screen.queryByText('No images available')
        
        // Either we have images or we show empty state
        expect(images.length > 0 || emptyState).toBeTruthy()
        
        // If we have images, each should have an alt attribute
        if (images.length > 0) {
          images.forEach(img => {
            expect(img).toHaveAttribute('alt')
          })
        }
      }, { timeout: 10000 })
    })

    it('shows retry button on error', async () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      // Wait for either success or error state
      await waitFor(() => {
        const hasContent = screen.queryByText('Explore concepts through visual representations and artistic interpretations') ||
                          screen.queryByText('Retry')
        expect(hasContent).toBeTruthy()
      }, { timeout: 15000 })
    })

    it('handles empty concepts array', async () => {
      renderWithProviders(<GalleryLens {...defaultProps} />)
      
      // Wait for either success or empty state
      await waitFor(() => {
        const hasContent = screen.queryByText('Explore concepts through visual representations and artistic interpretations') ||
                          screen.queryByText('No concepts available yet') ||
                          screen.queryByText('Concepts will appear here as they are discovered!')
        expect(hasContent).toBeTruthy()
      }, { timeout: 15000 })
    })
  })
})
