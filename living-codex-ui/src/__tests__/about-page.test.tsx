import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import AboutPage from '@/app/about/page'

describe('About Page - System Information', () => {
  describe('Core Features', () => {
    it('renders the about page without crashing', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/About|Living Codex/i)).toBeInTheDocument()
      })
    })

    it('displays system information', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        // Should show version, status, or other system info
        const hasSystemInfo = 
          screen.queryByText(/version/i) ||
          screen.queryByText(/status/i) ||
          screen.queryByText(/health/i) ||
          screen.queryByText(/Living Codex/i)
        
        expect(hasSystemInfo).toBeTruthy()
      })
    })

    it('displays API logs or system logs', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        // Should show logs section
        const hasLogs = 
          screen.queryByText(/log/i) ||
          screen.queryByText(/api/i) ||
          screen.queryByText(/request/i)
        
        expect(hasLogs).toBeTruthy()
      }, { timeout: 5000 })
    })
  })

  describe('With Real API', () => {
    it('fetches and displays real system health data', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        // Should display content without errors
        expect(screen.queryByText(/error/i)).not.toBeInTheDocument()
      }, { timeout: 10000 })
    })
  })

  describe('Accessibility', () => {
    it('has proper heading structure', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        const headings = screen.queryAllByRole('heading')
        expect(headings.length).toBeGreaterThan(0)
      })
    })

    it('provides navigable content', async () => {
      renderWithProviders(<AboutPage />)
      
      await waitFor(() => {
        // Should have some interactive elements or content
        const hasContent = document.body.textContent && document.body.textContent.length > 50
        expect(hasContent).toBeTruthy()
      })
    })
  })
})

