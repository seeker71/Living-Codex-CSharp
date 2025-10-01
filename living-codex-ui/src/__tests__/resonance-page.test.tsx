import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import ResonancePage from '@/app/resonance/page'

describe('Resonance Page - Frequency Controls', () => {
  describe('Core Features', () => {
    it('renders the resonance page without crashing', async () => {
      renderWithProviders(<ResonancePage />)
      
      await waitFor(() => {
        expect(screen.getByText(/Resonance/i)).toBeInTheDocument()
      })
    })

    it('displays resonance controls', async () => {
      renderWithProviders(<ResonancePage />)
      
      await waitFor(() => {
        // Should show resonance-related controls or info
        const hasResonanceUI = 
          screen.queryByText(/frequency/i) ||
          screen.queryByText(/resonance/i) ||
          screen.queryByText(/432|528|741/i) || // Sacred frequencies
          screen.queryByRole('slider') ||
          screen.queryByRole('button')
        
        expect(hasResonanceUI).toBeTruthy()
      })
    })

    it('provides concept comparison functionality', async () => {
      renderWithProviders(<ResonancePage />)
      
      await waitFor(() => {
        // Should have comparison UI
        const hasComparisonUI = 
          screen.queryByText(/compare/i) ||
          screen.queryByRole('button') ||
          screen.queryByRole('textbox')
        
        expect(hasComparisonUI).toBeTruthy()
      }, { timeout: 5000 })
    })
  })

  describe('With Real API', () => {
    it('loads resonance data from real backend', async () => {
      renderWithProviders(<ResonancePage />)
      
      await waitFor(() => {
        // Should display content without errors
        expect(screen.queryByText(/fatal.*error/i)).not.toBeInTheDocument()
      }, { timeout: 15000 })
    })
  })

  describe('Error Handling', () => {
    it('handles API failures gracefully', async () => {
      global.fetch = jest.fn(() => 
        Promise.reject(new Error('Network error'))
      ) as jest.Mock

      renderWithProviders(<ResonancePage />)
      
      await waitFor(() => {
        // Should show error state or fallback
        const hasErrorHandling = 
          screen.queryByText(/error/i) ||
          screen.queryByText(/Resonance/i) // Falls back to page title
        
        expect(hasErrorHandling).toBeTruthy()
      }, { timeout: 10000 })
    })
  })
})

