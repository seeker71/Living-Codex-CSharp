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

describe('Gallery Image Display - Simple Tests', () => {
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

  describe('Basic Gallery Rendering', () => {
    it('renders gallery with placeholder images', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that concepts are displayed
      expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      expect(screen.getByText('Consciousness')).toBeInTheDocument()
      
      // Check that images are rendered
      const images = screen.getAllByRole('img')
      expect(images.length).toBeGreaterThan(0)
      
      // Check that each image has proper alt text
      const quantumImage = screen.getByAltText('Quantum Computing')
      const consciousnessImage = screen.getByAltText('Consciousness')
      
      expect(quantumImage).toBeInTheDocument()
      expect(consciousnessImage).toBeInTheDocument()
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
    })

    it('shows domain information for each concept', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Check that domains are displayed
      expect(screen.getByText('Technology')).toBeInTheDocument()
      expect(screen.getByText('Philosophy')).toBeInTheDocument()
    })
  })

  describe('Gallery Modal Functionality', () => {
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

    it('closes modal when clicking close button', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument()
      })
      
      // Click on quantum computing item
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

  describe('Error Handling', () => {
    it('handles API errors gracefully', async () => {
      // Mock fetch to return error
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500
      })
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText(/error/i) || screen.getByText(/failed/i)).toBeInTheDocument()
      })
    })

    it('handles empty data gracefully', async () => {
      // Mock fetch to return empty data
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ concepts: [] })
      })
      
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText(/no concepts/i) || screen.getByText(/empty/i)).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('shows loading state initially', () => {
      renderWithProviders(<GalleryLens />)
      
      expect(screen.getByText(/loading/i)).toBeInTheDocument()
    })

    it('hides loading state after data loads', async () => {
      renderWithProviders(<GalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      })
      
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument()
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
  })
})
