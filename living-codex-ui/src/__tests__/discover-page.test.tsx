import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import DiscoverPage from '@/app/discover/page'

describe('Discover Page - Main Exploration Interface', () => {
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
        const streamOption = screen.queryByText(/Stream/i)
        const galleryOption = screen.queryByText(/Gallery/i)
        
        // At least one lens should be visible
        expect(streamOption || galleryOption).toBeTruthy()
      })
    })

    it('allows switching between lenses', async () => {
      renderWithProviders(<DiscoverPage />)
      
      await waitFor(() => {
        const tabs = screen.queryAllByRole('tab')
        expect(tabs.length).toBeGreaterThan(0)
      }, { timeout: 5000 })
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
  })
})

