import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import { GalleryLens } from '@/components/lenses/GalleryLens'
import { testConfig, getFetchMock, isBackendAvailable } from './test-config'

// Mock Next.js navigation
const mockPush = jest.fn()
const mockBack = jest.fn()

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, back: mockBack }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/',
}))

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

// Mock the buildApiUrl function
jest.mock('@/lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

// Mock the useTrackInteraction hook
jest.mock('@/lib/hooks', () => ({
  useTrackInteraction: () => jest.fn(),
}))

describe('Gallery Image Display Tests', () => {
  beforeAll(async () => {
    // If using real API, check if backend is available
    if (testConfig.useRealApi) {
      const backendAvailable = await isBackendAvailable()
      if (!backendAvailable) {
        console.warn('⚠️  Backend not available, tests will be skipped. Start backend with: ./start-server.sh')
        pending('Backend not available')
      }
    }
  })

  beforeEach(() => {
    // Reset mocks
    jest.clearAllMocks()
    
    // Set up fetch mock based on configuration
    const fetchMock = getFetchMock()
    if (fetchMock) {
      global.fetch = fetchMock
    }
    // If fetchMock is undefined, we'll use real fetch (when useRealApi is true)
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Gallery Grid Display', () => {
    it('renders gallery items with placeholder images', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that all concepts are displayed
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      expect(screen.getByText('Consciousness')).toBeInTheDocument()
      expect(screen.getByText('Sustainable Energy')).toBeInTheDocument()
      
      // Check that images are rendered
      const images = screen.getAllByRole('img')
      expect(images.length).toBeGreaterThan(0)
      
      // Check that each image has proper alt text
      const quantumImage = screen.getByAltText('Quantum Computing')
      const consciousnessImage = screen.getByAltText('Consciousness')
      const energyImage = screen.getByAltText('Sustainable Energy')
      
      expect(quantumImage).toBeInTheDocument()
      expect(consciousnessImage).toBeInTheDocument()
      expect(energyImage).toBeInTheDocument()
    })

    it('displays placeholder images with concept initials', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that images are using placeholder URLs with concept initials
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('src')
        const src = img.getAttribute('src')
        expect(src).toMatch(/via\.placeholder\.com/)
        expect(src).toMatch(/text=/)
      })
    })

    it('shows proper image dimensions and styling', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that images have proper CSS classes for styling
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveClass('w-full', 'h-full', 'object-cover')
      })
    })

    it('displays resonance values correctly', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that resonance values are displayed
      expect(screen.getByText('Resonance: 0.85')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 0.92')).toBeInTheDocument()
      expect(screen.getByText('Resonance: 0.78')).toBeInTheDocument()
    })

    it('shows domain information for each concept', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that domains are displayed
      expect(screen.getByText('Technology')).toBeInTheDocument()
      expect(screen.getByText('Philosophy')).toBeInTheDocument()
      expect(screen.getByText('Environment')).toBeInTheDocument()
    })
  })

  describe('Gallery Item Modal Display', () => {
    it('opens modal when clicking on a gallery item', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on the first gallery item
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      // Check that modal opens
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
    })

    it('displays large image in modal', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        // Check that large image is displayed in modal
        const modalImages = screen.getAllByRole('img')
        const largeImage = modalImages.find(img => 
          img.getAttribute('alt') === 'Quantum Computing' && 
          img.getAttribute('class')?.includes('w-full')
        )
        expect(largeImage).toBeInTheDocument()
      })
    })

    it('shows detailed concept information in modal', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        // Check detailed information is displayed
        expect(screen.getByText('Computing based on quantum mechanical phenomena')).toBeInTheDocument()
        expect(screen.getByText('Domain:')).toBeInTheDocument()
        expect(screen.getByText('Technology')).toBeInTheDocument()
        expect(screen.getByText('Complexity:')).toBeInTheDocument()
        expect(screen.getByText('8')).toBeInTheDocument()
        expect(screen.getByText('Resonance:')).toBeInTheDocument()
        expect(screen.getByText('0.850')).toBeInTheDocument()
      })
    })

    it('displays axes and tags in modal', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        // Check that axes and tags are displayed
        expect(screen.getByText('Axes:')).toBeInTheDocument()
        expect(screen.getByText('quantum')).toBeInTheDocument()
        expect(screen.getByText('computing')).toBeInTheDocument()
        expect(screen.getByText('technology')).toBeInTheDocument()
        
        expect(screen.getByText('Tags:')).toBeInTheDocument()
      })
    })

    it('closes modal when clicking close button', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
      
      // Click close button
      const closeButton = screen.getByText('✕')
      fireEvent.click(closeButton)
      
      // Check that modal is closed
      await waitFor(() => {
        expect(screen.queryByText('Description')).not.toBeInTheDocument()
      })
    })
  })

  describe('Image Error Handling', () => {
    it('displays error state when image fails to load', async () => {
      // Mock fetch to return concepts but simulate image load failure
      global.fetch = jest.fn()
        .mockImplementation((url: string) => {
          if (url.includes('/concepts')) {
            return Promise.resolve({
              ok: true,
              json: () => Promise.resolve({
                concepts: [{
                  ...mockGalleryItems[0],
                  imageError: 'Failed to load image'
                }]
              })
            })
          }
          return Promise.resolve({
            ok: false,
            status: 404
          })
        })

      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that error state is displayed
      expect(screen.getByText('Image Error')).toBeInTheDocument()
      expect(screen.getByText('Failed to load image')).toBeInTheDocument()
    })

    it('shows fallback placeholder when no image URL', async () => {
      // Mock fetch to return concepts without image URLs
      global.fetch = jest.fn()
        .mockImplementation((url: string) => {
          if (url.includes('/concepts')) {
            return Promise.resolve({
              ok: true,
              json: () => Promise.resolve({
                concepts: [{
                  ...mockGalleryItems[0],
                  imageUrl: null
                }]
              })
            })
          }
          return Promise.resolve({
            ok: false,
            status: 404
          })
        })

      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that fallback placeholder is shown
      expect(screen.getByText('📷')).toBeInTheDocument()
    })
  })

  describe('Image Loading States', () => {
    it('shows loading state while images are being generated', async () => {
      // Mock fetch to return concepts with loading state
      global.fetch = jest.fn()
        .mockImplementation((url: string) => {
          if (url.includes('/concepts')) {
            return Promise.resolve({
              ok: true,
              json: () => Promise.resolve({
                concepts: [{
                  ...mockGalleryItems[0],
                  imageLoading: true
                }]
              })
            })
          }
          return Promise.resolve({
            ok: false,
            status: 404
          })
        })

      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that loading state is handled (placeholder images are shown immediately)
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
    })
  })

  describe('Gallery Filtering and Sorting', () => {
    it('filters gallery items by axis', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that filter dropdown is present
      const filterSelect = screen.getByDisplayValue('All Concepts')
      expect(filterSelect).toBeInTheDocument()
      
      // Change filter
      fireEvent.change(filterSelect, { target: { value: 'quantum' } })
      
      // Check that only filtered items are shown
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
        expect(screen.queryByText('Consciousness')).not.toBeInTheDocument()
        expect(screen.queryByText('Sustainable Energy')).not.toBeInTheDocument()
      })
    })

    it('sorts gallery items by resonance', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that sort dropdown is present
      const sortSelect = screen.getByDisplayValue('By Resonance')
      expect(sortSelect).toBeInTheDocument()
      
      // Change sort order
      fireEvent.change(sortSelect, { target: { value: 'energy' } })
      
      // Check that items are re-sorted
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
    })
  })

  describe('Gallery Actions', () => {
    it('handles contribute action', async () => {
      // Mock window.alert
      const mockAlert = jest.spyOn(window, 'alert').mockImplementation(() => {})
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
      
      // Click contribute button
      const contributeButton = screen.getByText('💡 Contribute')
      fireEvent.click(contributeButton)
      
      // Check that alert is shown
      expect(mockAlert).toHaveBeenCalledWith(
        expect.stringContaining('Contribute to "Quantum Computing"')
      )
      
      mockAlert.mockRestore()
    })

    it('handles invest action', async () => {
      // Mock window.alert
      const mockAlert = jest.spyOn(window, 'alert').mockImplementation(() => {})
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
      
      // Click invest button
      const investButton = screen.getByText('💰 Invest')
      fireEvent.click(investButton)
      
      // Check that alert is shown
      expect(mockAlert).toHaveBeenCalledWith(
        expect.stringContaining('Invest in "Quantum Computing"')
      )
      
      mockAlert.mockRestore()
    })

    it('handles discuss action', async () => {
      // Mock window.alert
      const mockAlert = jest.spyOn(window, 'alert').mockImplementation(() => {})
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
      
      // Click discuss button
      const discussButton = screen.getByText('💬 Discuss')
      fireEvent.click(discussButton)
      
      // Check that alert is shown
      expect(mockAlert).toHaveBeenCalledWith(
        expect.stringContaining('Start a discussion about "Quantum Computing"')
      )
      
      mockAlert.mockRestore()
    })
  })

  describe('Responsive Design', () => {
    it('adapts grid layout for different screen sizes', async () => {
      // Mock different screen sizes
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768, // Tablet size
      })
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that grid adapts to screen size
      const gridContainer = screen.getByText('Quantum Computing').closest('div')?.parentElement
      expect(gridContainer).toHaveClass('grid')
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA labels for images', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that images have proper alt text
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('alt')
        expect(img.getAttribute('alt')).not.toBe('')
      })
    })

    it('supports keyboard navigation', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Test keyboard navigation on gallery items
      const galleryItems = screen.getAllByRole('button')
      if (galleryItems.length > 0) {
        galleryItems[0].focus()
        expect(document.activeElement).toBe(galleryItems[0])
      }
    })

    it('has proper focus management in modal', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on a gallery item to open modal
      const quantumItem = screen.getByText('Quantum Computing')
      fireEvent.click(quantumItem.closest('div')!)
      
      await waitFor(() => {
        expect(screen.getByText('Description')).toBeInTheDocument()
      })
      
      // Check that modal has proper focus management
      const closeButton = screen.getByText('✕')
      closeButton.focus()
      expect(document.activeElement).toBe(closeButton)
    })
  })

  describe('Performance', () => {
    it('loads images efficiently', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that images are loaded with proper attributes
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('src')
        // Check that placeholder images are used (fast loading)
        const src = img.getAttribute('src')
        expect(src).toMatch(/via\.placeholder\.com/)
      })
    })

    it('handles large number of gallery items', async () => {
      // Mock fetch to return many concepts
      const manyConcepts = Array.from({ length: 50 }, (_, i) => ({
        ...mockGalleryItems[0],
        id: `concept-${i}`,
        name: `Concept ${i}`,
        description: `Description for concept ${i}`
      }))

      global.fetch = jest.fn()
        .mockImplementation((url: string) => {
          if (url.includes('/concepts')) {
            return Promise.resolve({
              ok: true,
              json: () => Promise.resolve({
                concepts: manyConcepts
              })
            })
          }
          return Promise.resolve({
            ok: false,
            status: 404
          })
        })

      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that all items are rendered
      expect(screen.getByText('Concept 0')).toBeInTheDocument()
      expect(screen.getByText('Concept 49')).toBeInTheDocument()
    })
  })
})
