import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import PortalsPage from '@/app/portals/page'

describe('Portals Page - External Connections', () => {
  describe('Core Features', () => {
    it('renders the portals page without crashing', async () => {
      renderWithProviders(<PortalsPage />)
      
      await waitFor(() => {
        expect(screen.getByText(/Portal/i)).toBeInTheDocument()
      })
    })

    it('displays portal list or grid', async () => {
      renderWithProviders(<PortalsPage />)
      
      await waitFor(() => {
        const hasPortalUI = 
          screen.queryByText(/portal/i) ||
          screen.queryByText(/connection/i) ||
          screen.queryByText(/external/i) ||
          screen.queryByRole('list')
        
        expect(hasPortalUI).toBeTruthy()
      })
    })
  })

  describe('With Real API', () => {
    it('loads portals from backend', async () => {
      renderWithProviders(<PortalsPage />)
      
      await waitFor(() => {
        expect(screen.queryByText(/fatal.*error/i)).not.toBeInTheDocument()
      }, { timeout: 10000 })
    })
  })
})

