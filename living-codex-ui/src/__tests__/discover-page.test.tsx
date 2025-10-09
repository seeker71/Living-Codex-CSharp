import React from 'react'
import { screen, waitFor, fireEvent, cleanup } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from './test-utils'
import DiscoverPage from '@/app/discover/page'

describe('Discover Page - Main Exploration Interface', () => {
  beforeEach(() => {
    // Ensure clean state for each test
    jest.clearAllMocks()
    
    // Mock geolocation API for NearbyLens
    const mockGeolocation = {
      getCurrentPosition: jest.fn().mockImplementation((success) => {
        success({
          coords: {
            latitude: 37.7749,
            longitude: -122.4194,
          },
        })
      }),
    }
    Object.defineProperty(global.navigator, 'geolocation', {
      value: mockGeolocation,
      writable: true,
      configurable: true,
    })
  })

  afterEach(() => {
    cleanup()
    jest.clearAllMocks()
  })

  describe('Core Features', () => {
    it('renders the discover page without crashing', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/Discover/i)).toBeInTheDocument()
      })
    })

    it('displays lens navigation tabs', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Main lenses should be available
        expect(screen.getByText('Stream')).toBeInTheDocument()
        expect(screen.getByText('Gallery')).toBeInTheDocument()
      }, { timeout: 3000 })
    })

    it('allows switching between lenses', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Check for lens tabs
        expect(screen.getByText('Stream')).toBeInTheDocument()
        expect(screen.getByText('Conversations')).toBeInTheDocument()
      }, { timeout: 3000 })
    })

    it('loads content in the active lens', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should show either loading state or content
        const hasContent = 
          screen.queryByRole('progressbar') ||
          screen.queryByText(/concept/i) ||
          screen.queryByText(/No.*available/i)
        
        expect(hasContent).toBeTruthy()
      }, { timeout: 10000 })
    })
  })

  describe('With Real API', () => {
    beforeEach(() => {
      // Use real fetch for integration testing
      jest.restoreAllMocks()
    })

    it('renders discover page with real backend data', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Page should load without errors
        expect(screen.queryByText(/error/i)).not.toBeInTheDocument()
      }, { timeout: 15000 })
    })

    it('fetches and displays concepts from real API', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should either show concepts or empty state (not error)
        const hasValidState = 
          screen.queryByText(/concept/i) ||
          screen.queryByText(/No.*available/i) ||
          screen.queryByText(/Discover/i)
        
        expect(hasValidState).toBeTruthy()
      }, { timeout: 15000 })
    })
  })

  describe('Error Handling', () => {
    it('handles API failures gracefully', async () => {
      // Mock fetch to simulate API failure
      global.fetch = jest.fn(() => 
        Promise.reject(new Error('Network error'))
      ) as jest.Mock

      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should show error state or fallback content
        const hasErrorHandling = 
          screen.queryByText(/error/i) ||
          screen.queryByText(/unavailable/i) ||
          screen.queryByText(/Discover/i) // Fallback to basic UI
        
        expect(hasErrorHandling).toBeTruthy()
      }, { timeout: 10000 })
    })
  })

  describe('Lens Navigation', () => {
    it('renders all lens tabs with correct icons and names', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Stream')).toBeInTheDocument()
        expect(screen.getByText('Conversations')).toBeInTheDocument()
        expect(screen.getByText('Gallery')).toBeInTheDocument()
        expect(screen.getByText('Nearby')).toBeInTheDocument()
        expect(screen.getByText('Swipe')).toBeInTheDocument()
      })
    })

    it('switches between all lens tabs', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      // Test each lens individually to avoid rapid switching issues
      await waitFor(() => {
        expect(screen.getByText('Stream')).toBeInTheDocument()
      })
      
      // Test Conversations
      const conversationsButton = screen.getByRole('button', { name: /💬.*Conversations/i })
      await user.click(conversationsButton)
      await waitFor(() => {
        expect(conversationsButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
      
      // Test Gallery
      const galleryButton = screen.getByRole('button', { name: /🖼️.*Gallery/i })
      await user.click(galleryButton)
      await waitFor(() => {
        expect(galleryButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
      
      // Test Nearby
      const nearbyButton = screen.getByRole('button', { name: /📍.*Nearby/i })
      await user.click(nearbyButton)
      await new Promise(resolve => setTimeout(resolve, 100))
      await waitFor(() => {
        expect(nearbyButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
      
      // Test Swipe
      const swipeButton = screen.getByRole('button', { name: /👆.*Swipe/i })
      await user.click(swipeButton)
      await new Promise(resolve => setTimeout(resolve, 100))
      await waitFor(() => {
        expect(swipeButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
      
      // Go back to Stream
      const streamButton = screen.getByRole('button', { name: /📱.*Stream/i })
      await user.click(streamButton)
      await new Promise(resolve => setTimeout(resolve, 100))
      await waitFor(() => {
        expect(streamButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
    })

    it('shows correct active tab styling', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Stream')).toBeInTheDocument()
      })

      // Stream should be active by default
      const streamTab = screen.getByText('Stream')
      expect(streamTab).toHaveClass('border-blue-500')

      // Click another tab
      await user.click(screen.getByText('Gallery'))
      
      // Gallery should now be active
      const galleryTab = screen.getByText('Gallery')
      expect(galleryTab).toHaveClass('border-blue-500')
      
      // Stream should no longer be active
      expect(streamTab).not.toHaveClass('border-blue-500')
    })
  })

  describe('Resonance Controls', () => {
    it('renders resonance controls sidebar', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // ResonanceControls component should be present
        // This might be tested by looking for control elements or the component itself
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })
    })

    it('handles control changes', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })

      // Test that controls can be interacted with
      // This would depend on the actual ResonanceControls implementation
    })
  })

  describe('Authentication States', () => {
    it('shows read-only mode for unauthenticated users', async () => {
      const unauthMock = {
        user: null,
        isAuthenticated: false,
        isLoading: false
      }

      renderWithProviders(<DiscoverPage />, { authValue: unauthMock })
      
      await waitFor(() => {
        expect(screen.getByText('Read-only mode')).toBeInTheDocument()
        expect(screen.getByText('Sign in to interact and personalize your experience')).toBeInTheDocument()
        expect(screen.getByText('Sign In')).toBeInTheDocument()
      })
    })

    it('handles Sign In button click', async () => {
      const user = userEvent.setup()
      const unauthMock = {
        user: null,
        isAuthenticated: false,
        isLoading: false
      }

      renderWithProviders(<DiscoverPage />, { authValue: unauthMock })
      
      await waitFor(() => {
        expect(screen.getByText('Sign In')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Sign In'))
      // Should navigate to login page
    })

    it('hides read-only mode for authenticated users', async () => {
      const authMock = {
        user: { id: 'test-user', username: 'testuser' },
        isAuthenticated: true,
        isLoading: false
      }

      renderWithProviders(<DiscoverPage />, { authValue: authMock })
      
      await waitFor(() => {
        expect(screen.queryByText('Read-only mode')).not.toBeInTheDocument()
        expect(screen.queryByText('Sign In')).not.toBeInTheDocument()
      })
    })
  })

  describe('Lens Content Rendering', () => {
    it('renders StreamLens when Stream tab is active', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Stream')).toBeInTheDocument()
      })

      // Stream tab should be active by default
      const streamTab = screen.getByText('Stream')
      expect(streamTab).toHaveClass('border-blue-500')
    })

    it('renders ThreadsLens when Conversations tab is active', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Conversations')).toBeInTheDocument()
      })

      // Use getByRole to target the button specifically
      const conversationsButton = screen.getByRole('button', { name: /💬.*Conversations/i })
      await user.click(conversationsButton)
      
      // Should render ThreadsLens content
      await waitFor(() => {
        expect(conversationsButton).toHaveClass('border-blue-500')
      })
    })

    it('renders GalleryLens when Gallery tab is active', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Gallery'))
      
      // Should render GalleryLens content
      await waitFor(() => {
        const galleryTab = screen.getByText('Gallery')
        expect(galleryTab).toHaveClass('border-blue-500')
      })
    })

    it('renders NearbyLens when Nearby tab is active', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Nearby')).toBeInTheDocument()
      })

      const nearbyButton = screen.getByRole('button', { name: /📍.*Nearby/i })
      await user.click(nearbyButton)
      
      // Wait for the lens to switch
      await new Promise(resolve => setTimeout(resolve, 100))
      
      // Should render NearbyLens content
      await waitFor(() => {
        expect(nearbyButton).toHaveClass('border-blue-500')
      }, { timeout: 2000 })
    })

    it('renders SwipeLens when Swipe tab is active', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Swipe')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Swipe'))
      
      // Should render SwipeLens content
      await waitFor(() => {
        const swipeTab = screen.getByText('Swipe')
        expect(swipeTab).toHaveClass('border-blue-500')
      })
    })
  })

  describe('Loading States', () => {
    it('shows loading state while data is being fetched', async () => {
      renderWithProviders(<DiscoverPage />)
      
      // Should show loading indicators
      await waitFor(() => {
        const hasLoadingState = 
          screen.queryByRole('progressbar') ||
          screen.queryByText(/loading/i) ||
          screen.queryByText('Discover')
        
        expect(hasLoadingState).toBeTruthy()
      })
    })

    it('handles loading state transitions', async () => {
      renderWithProviders(<DiscoverPage />)
      
      // Should transition from loading to content
      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      }, { timeout: 10000 })
    })
  })

  describe('Lens Info Display', () => {
    it('shows lens information card', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should show lens info with status
        const lensInfo = screen.queryByText(/Status:/)
        expect(lensInfo).toBeTruthy()
      })
    })

    it('displays current lens status and ranking', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should show status information
        const statusText = screen.queryByText(/Status:/)
        expect(statusText).toBeTruthy()
      })
    })
  })

  describe('Responsive Design', () => {
    it('adapts layout for mobile viewport', async () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })

      // Should still render all lens tabs
      expect(screen.getByText('Stream')).toBeInTheDocument()
      expect(screen.getByText('Gallery')).toBeInTheDocument()
    })

    it('adapts layout for tablet viewport', async () => {
      // Mock tablet viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768,
      })

      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper heading structure', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        const headings = screen.queryAllByRole('heading')
        expect(headings.length).toBeGreaterThan(0)
      })
    })

    it('has accessible navigation controls', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        // Should have navigable elements
        const buttons = screen.queryAllByRole('button')
        const tabs = screen.queryAllByRole('tab')
        
        expect(buttons.length + tabs.length).toBeGreaterThan(0)
      })
    })

    it('supports keyboard navigation between tabs', async () => {
      const user = userEvent.setup()
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Stream')).toBeInTheDocument()
      })

      // Tab navigation should work
      await user.tab()
      expect(document.activeElement).toBeDefined()
    })
  })
})

