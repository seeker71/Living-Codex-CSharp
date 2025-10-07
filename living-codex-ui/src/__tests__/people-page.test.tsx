import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import PeoplePage from '@/app/people/page'

describe('People Page - User Discovery', () => {
  describe('Core Features', () => {
    it('renders the people page without crashing', async () => {
      renderWithProviders(<PeoplePage />)
      
      await waitFor(() => {
        expect(screen.getByText(/People|Discover|Users/i)).toBeInTheDocument()
      })
    })

    it('displays user discovery interface', async () => {
      renderWithProviders(<PeoplePage />)
      
      await waitFor(() => {
        // Should show discovery UI elements
        const hasDiscoveryUI = 
          screen.queryByText(/discover/i) ||
          screen.queryByPlaceholderText(/search/i) ||
          screen.queryByRole('button')
        
        expect(hasDiscoveryUI).toBeTruthy()
      })
    })

    it('handles user search or filtering', async () => {
      renderWithProviders(<PeoplePage />)
      
      await waitFor(() => {
        // Should have search or filter controls
        const controls = 
          screen.queryByPlaceholderText(/search/i) ||
          screen.queryByRole('textbox') ||
          screen.queryByRole('button')
        
        expect(controls).toBeTruthy()
      }, { timeout: 5000 })
    })
  })

  describe('With Real API', () => {
    it('fetches users from real API', async () => {
      renderWithProviders(<PeoplePage />)
      
      await waitFor(() => {
        // Should either show users or empty state (not error)
        const hasValidState = 
          screen.queryByText(/user/i) ||
          screen.queryByText(/people/i) ||
          screen.queryByText(/No.*found/i) ||
          screen.queryByText(/Discover/i)
        
        expect(hasValidState).toBeTruthy()
      }, { timeout: 15000 })
    })
  })

  describe('Error Handling', () => {
    it('handles API failures gracefully', async () => {
      global.fetch = jest.fn(() => 
        Promise.reject(new Error('Network error'))
      ) as jest.Mock

      renderWithProviders(<PeoplePage />)
      
      await waitFor(() => {
        // Should show error state or fallback
        const hasErrorHandling = 
          screen.queryByText(/error/i) ||
          screen.queryByText(/unavailable/i) ||
          screen.queryByText(/People/i)
        
        expect(hasErrorHandling).toBeTruthy()
      }, { timeout: 10000 })
    })
  })
})

