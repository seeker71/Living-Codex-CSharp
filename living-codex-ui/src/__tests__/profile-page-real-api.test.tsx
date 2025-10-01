import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import { renderWithProviders } from './test-utils'
import ProfilePage from '../app/profile/page'

// Mock the auth context for real API testing
const mockAuthValue = {
  user: {
    id: 'test-user-123',
    username: 'testuser',
    email: 'test@example.com',
    displayName: 'Test User',
    createdAt: '2024-01-01T00:00:00Z',
    isActive: true
  },
  isAuthenticated: true,
  isLoading: false
}

describe('ProfilePage Component with Real API', () => {
  beforeEach(() => {
    // Use real API calls - we want to test the real system
    // The server should be running for these tests to work
  })

  afterEach(() => {
    // Clean up any test data if needed
  })

  describe('Component Rendering with Real API', () => {
    it('renders loading state initially', () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
      // The loading spinner is a div, not a progressbar role
      expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
    })

    it('renders profile page when user is authenticated', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      // Wait for the profile to load
      await waitFor(() => {
        expect(screen.getByText('testuser')).toBeInTheDocument()
      }, { timeout: 10000 })

      // Should show profile content - the actual username from the API
      expect(screen.getByText('testuser')).toBeInTheDocument()
    })

    it('handles API errors gracefully', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      // Wait for either success or error state
      await waitFor(() => {
        const hasContent = screen.queryByText('testuser') ||
                          screen.queryByText('Error loading profile') ||
                          screen.queryByText('Please log in to view your profile')
        expect(hasContent).toBeTruthy()
      }, { timeout: 15000 })
    })
  })

  describe('Authentication States', () => {
    it('shows login prompt when user is not authenticated', () => {
      const unauthenticatedAuthValue = {
        user: null,
        isAuthenticated: false,
        isLoading: false
      }

      renderWithProviders(<ProfilePage />, { authValue: unauthenticatedAuthValue })

      expect(screen.getByText('Please log in to view your profile.')).toBeInTheDocument()
    })

    it('shows loading state when authentication is in progress', () => {
      const loadingAuthValue = {
        user: null,
        isAuthenticated: false,
        isLoading: true
      }

      renderWithProviders(<ProfilePage />, { authValue: loadingAuthValue })

      expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
    })
  })
})
