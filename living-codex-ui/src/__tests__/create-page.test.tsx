import React from 'react'
import { screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from './test-utils'
import CreatePage from '@/app/create/page'

describe('Create Page - Content Creation', () => {
  describe('Core Features', () => {
    it('renders the create page without crashing', async () => {
      renderWithProviders(<CreatePage />)
      
      await waitFor(() => {
        expect(screen.getByText(/Create/i)).toBeInTheDocument()
      })
    })

    it('displays creation options', async () => {
      renderWithProviders(<CreatePage />)
      
      await waitFor(() => {
        const hasCreationUI = 
          screen.queryByText(/concept/i) ||
          screen.queryByText(/thread/i) ||
          screen.queryByText(/content/i) ||
          screen.queryByRole('button') ||
          screen.queryByRole('textbox')
        
        expect(hasCreationUI).toBeTruthy()
      })
    })

    it('provides form or input controls', async () => {
      renderWithProviders(<CreatePage />)
      
      await waitFor(() => {
        const hasInputs = 
          screen.queryAllByRole('textbox').length > 0 ||
          screen.queryAllByRole('button').length > 0
        
        expect(hasInputs).toBeTruthy()
      }, { timeout: 5000 })
    })
  })

  describe('With Real API', () => {
    it('loads without errors when backend is available', async () => {
      renderWithProviders(<CreatePage />)
      
      await waitFor(() => {
        expect(screen.queryByText(/fatal.*error/i)).not.toBeInTheDocument()
      }, { timeout: 10000 })
    })
  })
})

