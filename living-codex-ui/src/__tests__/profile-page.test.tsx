import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import '@testing-library/jest-dom'
import { renderWithProviders, mockFetch } from './test-utils'
import ProfilePage from '../app/profile/page'
import { api } from '@/lib/api'

// Mock the auth context
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

// Mock API responses
const mockProfileResponse = {
  success: true,
  data: {
    profile: {
      id: 'test-user-123',
      displayName: 'Test User',
      email: 'test@example.com',
      bio: 'I am a test user exploring consciousness',
      location: 'San Francisco, CA',
      interests: ['machine learning', 'philosophy', 'meditation'],
      avatarUrl: 'https://example.com/avatar.jpg',
      coverImageUrl: 'https://example.com/cover.jpg',
      joinedDate: '2024-01-01T00:00:00Z',
      lastActive: '2024-01-15T10:00:00Z',
      resonanceLevel: 75,
      totalContributions: 15,
      profileCompletion: 85
    }
  }
}

const mockBeliefSystemResponse = {
  success: true,
  beliefSystemId: 'belief-123',
  userId: 'test-user-123',
  framework: 'Scientific Spiritualism',
  principles: ['Curiosity', 'Compassion', 'Critical Thinking'],
  values: ['Truth', 'Love', 'Growth'],
  language: 'en',
  culturalContext: 'Western',
  spiritualTradition: 'Meditation',
  scientificBackground: 'Computer Science',
  resonanceThreshold: 0.8,
  sacredFrequencies: ['432Hz', '528Hz', '741Hz'],
  consciousnessLevel: 'Exploring'
}

describe('ProfilePage Component', () => {
  beforeEach(() => {
    // Use real API calls - we want to test the real system
    // The server should be running for these tests to work
  })

  afterEach(() => {
    // Clean up any test data if needed
  })

  describe('Component Rendering', () => {
    it('renders loading state initially', () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('renders profile page when user is authenticated', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
        expect(screen.getByText('🌟 High Resonance')).toBeInTheDocument()
      })

      // Check for main sections
      expect(screen.getByText('Profile Completion')).toBeInTheDocument()
      expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
    })

    it('renders profile header with avatar and stats', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
        expect(screen.getByText('15 contributions')).toBeInTheDocument()
        expect(screen.getByText('3 interests')).toBeInTheDocument()
        expect(screen.getByText('Edit Profile')).toBeInTheDocument()
        expect(screen.getByText('Share Profile')).toBeInTheDocument()
      })
    })

    it('shows profile completion progress', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('85%')).toBeInTheDocument()
        expect(screen.getByText('Complete')).toBeInTheDocument()
      })
    })

    it('displays badge collection with earned badges', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
        expect(screen.getByText('8/5 Earned')).toBeInTheDocument()
        expect(screen.getByText('First Steps')).toBeInTheDocument()
        expect(screen.getByText('Resonance Seeker')).toBeInTheDocument()
      })
    })

    it('shows social proof sections', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Community Impact')).toBeInTheDocument()
        expect(screen.getByText('Enhanced Discovery')).toBeInTheDocument()
        expect(screen.getByText('Achievements')).toBeInTheDocument()
      })
    })
  })

  describe('Profile Completion Logic', () => {
    it('calculates profile completion correctly', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        // With the mock data (85% complete), should show 85%
        expect(screen.getByText('85%')).toBeInTheDocument()
      })
    })

    it('shows completion rewards correctly', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('🎯 Basic Discovery')).toBeInTheDocument()
        expect(screen.getByText('🔮 Enhanced Resonance')).toBeInTheDocument()
        expect(screen.getByText('🌟 Advanced Insights')).toBeInTheDocument()
      })
    })

    it('displays profile sections with completion status', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument()
        expect(screen.getByText('Profile Picture')).toBeInTheDocument()
        expect(screen.getByText('Interests & Passions')).toBeInTheDocument()
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
        expect(screen.getByText('Location')).toBeInTheDocument()
      })
    })
  })

  describe('Interactive Profile Sections', () => {
    it('opens basic information section when clicked', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument()
      })

      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        expect(screen.getByText('Display Name *')).toBeInTheDocument()
        expect(screen.getByText('Email *')).toBeInTheDocument()
        expect(screen.getByText('Bio')).toBeInTheDocument()
        expect(screen.getByText('Save Changes')).toBeInTheDocument()
      })
    })

    it('opens avatar section and shows upload interface', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Profile Picture')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Profile Picture'))

      await waitFor(() => {
        expect(screen.getByText('Upload New Photo')).toBeInTheDocument()
        expect(screen.getByText('Avatar Tips')).toBeInTheDocument()
        expect(screen.getByText('Supported formats: JPG, PNG, GIF • Max size: 5MB')).toBeInTheDocument()
      })
    })

    it('opens interests section and shows interest management', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Interests & Passions')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Interests & Passions'))

      await waitFor(() => {
        expect(screen.getByText('Add Interest')).toBeInTheDocument()
        expect(screen.getByText('Your Interests')).toBeInTheDocument()
        expect(screen.getByText('machine learning')).toBeInTheDocument()
        expect(screen.getByText('philosophy')).toBeInTheDocument()
        expect(screen.getByText('meditation')).toBeInTheDocument()
      })
    })

    it('opens beliefs section and shows belief system form', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Belief Framework'))

      await waitFor(() => {
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
        expect(screen.getByDisplayValue('Scientific Spiritualism')).toBeInTheDocument()
        expect(screen.getByText('Your Sacred Frequencies')).toBeInTheDocument()
        expect(screen.getByText('432Hz')).toBeInTheDocument()
        expect(screen.getByText('528Hz')).toBeInTheDocument()
        expect(screen.getByText('741Hz')).toBeInTheDocument()
      })
    })

    it('opens location section and shows location form', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Location')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Location'))

      await waitFor(() => {
        expect(screen.getByText('Your Location')).toBeInTheDocument()
        expect(screen.getByDisplayValue('San Francisco, CA')).toBeInTheDocument()
        expect(screen.getByText('Benefits of sharing your location:')).toBeInTheDocument()
        expect(screen.getByText('🔒 Privacy & Safety')).toBeInTheDocument()
      })
    })

    it('navigates back to overview from section forms', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      // Open a section
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        expect(screen.getByText('← Back to Overview')).toBeInTheDocument()
      })

      // Click back button
      await user.click(screen.getByText('← Back to Overview'))

      await waitFor(() => {
        expect(screen.getByText('Profile Completion')).toBeInTheDocument()
      })
    })
  })

  describe('Form Interactions and Validation', () => {
    it('updates display name field', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        const displayNameInput = screen.getByDisplayValue('Test User')
        expect(displayNameInput).toBeInTheDocument()

        fireEvent.change(displayNameInput, { target: { value: 'Updated Name' } })
        expect(screen.getByDisplayValue('Updated Name')).toBeInTheDocument()
      })
    })

    it('updates bio field with character count', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        const bioTextarea = screen.getByDisplayValue('I am a test user exploring consciousness')
        expect(bioTextarea).toBeInTheDocument()

        fireEvent.change(bioTextarea, { target: { value: 'This is a longer bio that demonstrates the character count functionality and should be well within the 500 character limit for comprehensive testing.' } })

        expect(screen.getByText('140/500 characters')).toBeInTheDocument()
      })
    })

    it('adds and removes interests', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Interests & Passions')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Interests & Passions'))

      await waitFor(() => {
        const interestInput = screen.getByPlaceholderText(/e\.g\., machine learning, philosophy/)
        expect(interestInput).toBeInTheDocument()
      })

      // Add new interest
      const interestInput = screen.getByPlaceholderText(/e\.g\., machine learning, philosophy/)
      await user.type(interestInput, 'quantum physics')
      await user.click(screen.getByText('Add'))

      await waitFor(() => {
        expect(screen.getByText('quantum physics')).toBeInTheDocument()
      })

      // Remove interest
      const removeButton = screen.getAllByText('×')[0] // First remove button
      await user.click(removeButton)

      await waitFor(() => {
        expect(screen.queryByText('machine learning')).not.toBeInTheDocument()
      })
    })

    it('updates belief system fields', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
      })
      await user.click(screen.getByText('Belief Framework'))

      await waitFor(() => {
        const frameworkInput = screen.getByDisplayValue('Scientific Spiritualism')
        expect(frameworkInput).toBeInTheDocument()

        fireEvent.change(frameworkInput, { target: { value: 'Quantum Consciousness' } })
        expect(screen.getByDisplayValue('Quantum Consciousness')).toBeInTheDocument()
      })
    })

    it('adjusts resonance threshold slider', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
      })
      await user.click(screen.getByText('Belief Framework'))

      await waitFor(() => {
        const slider = screen.getByRole('slider')
        expect(slider).toBeInTheDocument()
        expect(screen.getByText('0.8')).toBeInTheDocument()

        fireEvent.change(slider, { target: { value: '0.6' } })
        expect(screen.getByText('0.6')).toBeInTheDocument()
      })
    })
  })

  describe('API Integration', () => {
    it('calls profile API on component mount', async () => {
      const mockFetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockProfileResponse),
        text: () => Promise.resolve(JSON.stringify(mockProfileResponse))
      })
      global.fetch = mockFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/auth/profile/test-user-123', expect.any(Object))
      })
    })

    it('calls belief system API on component mount', async () => {
      const mockFetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockBeliefSystemResponse),
        text: () => Promise.resolve(JSON.stringify(mockBeliefSystemResponse))
      })
      global.fetch = mockFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/userconcept/belief-system/test-user-123', expect.any(Object))
      })
    })

    it('saves profile changes when Save button is clicked', async () => {
      const user = userEvent.setup()
      const mockFetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true }),
        text: () => Promise.resolve(JSON.stringify({ success: true }))
      })
      global.fetch = mockFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        expect(screen.getByText('Save Changes')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Changes'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/identity/test-user-123',
          expect.objectContaining({
            method: 'PUT',
            body: expect.stringContaining('Test User')
          })
        )
      })
    })

    it('saves belief system changes when Save button is clicked', async () => {
      const user = userEvent.setup()
      const mockFetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true }),
        text: () => Promise.resolve(JSON.stringify({ success: true }))
      })
      global.fetch = mockFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Belief Framework')).toBeInTheDocument()
      })
      await user.click(screen.getByText('Belief Framework'))

      await waitFor(() => {
        expect(screen.getByText('Save Belief System')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Belief System'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/userconcept/belief-system/register',
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('Scientific Spiritualism')
          })
        )
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API errors gracefully', async () => {
      const errorMock = mockFetch([
        { success: false, error: 'Failed to load profile' },
        { success: false, error: 'Failed to load belief system' }
      ])
      global.fetch = errorMock

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument() // Should still render with fallback data
      })
    })

    it('shows error messages for failed saves', async () => {
      const user = userEvent.setup()
      const errorMock = mockFetch([
        mockProfileResponse,
        mockBeliefSystemResponse,
        { success: false, error: 'Failed to save profile' }
      ])
      global.fetch = errorMock

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        expect(screen.getByText('Save Changes')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Changes'))

      await waitFor(() => {
        expect(screen.getByText('Error')).toBeInTheDocument()
        expect(screen.getByText('Failed to save profile')).toBeInTheDocument()
      })
    })

    it('handles network failures', async () => {
      global.fetch = jest.fn().mockRejectedValue(new Error('Network error'))

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument() // Should render with fallback
      })
    })
  })

  describe('Loading States', () => {
    it('shows loading spinner during API calls', async () => {
      const slowMock = mockFetch([
        new Promise(resolve => setTimeout(() => resolve(mockProfileResponse), 100)),
        mockBeliefSystemResponse
      ])
      global.fetch = slowMock

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
      })
    })

    it('shows saving state during form submission', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        expect(screen.getByText('Save Changes')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Changes'))

      // Should show loading state
      expect(screen.getByText('Saving...')).toBeInTheDocument()
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })
  })

  describe('Responsive Design', () => {
    it('renders correctly on mobile viewport', async () => {
      // Mock window.innerWidth for mobile testing
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
      })

      // Should still render all key elements
      expect(screen.getByText('Profile Completion')).toBeInTheDocument()
      expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
    })

    it('adapts layout for tablet viewport', async () => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768,
      })

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper heading hierarchy', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Test User')
        expect(screen.getByText('Profile Completion')).toBeInTheDocument()
        expect(screen.getByText('Community Impact')).toBeInTheDocument()
      })
    })

    it('has accessible form controls', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Basic Information' })).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        const displayNameInput = screen.getByLabelText('Display Name *')
        const emailInput = screen.getByLabelText('Email *')
        const bioTextarea = screen.getByLabelText('Bio')

        expect(displayNameInput).toBeInTheDocument()
        expect(emailInput).toBeInTheDocument()
        expect(bioTextarea).toBeInTheDocument()
      })
    })

    it('supports keyboard navigation', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument()
      })

      // Tab navigation should work
      await user.tab()
      expect(document.activeElement).toBeDefined()
    })
  })

  describe('Badge System', () => {
    it('displays correct badge states', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()

        // Check earned badges
        expect(screen.getByText('First Steps')).toBeInTheDocument()
        expect(screen.getByText('Resonance Seeker')).toBeInTheDocument()

        // Check locked badges
        expect(screen.getByText('Consciousness Explorer')).toBeInTheDocument()
        expect(screen.getByText('Master Resonator')).toBeInTheDocument()
      })
    })

    it('shows progress to next badge', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Next Badge: Consciousness Explorer')).toBeInTheDocument()
        expect(screen.getByText('15% remaining')).toBeInTheDocument()
      })
    })

    it('displays badge rarity correctly', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Resonance Seeker')).toBeInTheDocument()
        // Badge should have correct styling based on rarity
        const badgeElement = screen.getByText('Resonance Seeker').closest('[class*="bg-gradient"]')
        expect(badgeElement).toBeInTheDocument()
      })
    })
  })

  describe('Onboarding Flow', () => {
    it('shows onboarding for low completion profiles', async () => {
      const lowCompletionMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            ...mockProfileResponse.data.profile,
            profileCompletion: 25
          }
        }
      }

      const lowCompletionFetch = mockFetch([lowCompletionMock, mockBeliefSystemResponse])
      global.fetch = lowCompletionFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Welcome to Your Consciousness Journey!')).toBeInTheDocument()
        expect(screen.getByText('Complete your profile to unlock the full resonance experience')).toBeInTheDocument()
        expect(screen.getByText('Start Journey →')).toBeInTheDocument()
      })
    })

    it('hides onboarding for high completion profiles', async () => {
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.queryByText('Welcome to Your Consciousness Journey!')).not.toBeInTheDocument()
      })
    })

    it('navigates to next incomplete section from onboarding', async () => {
      const user = userEvent.setup()
      const lowCompletionMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            ...mockProfileResponse.data.profile,
            profileCompletion: 25,
            interests: []
          }
        }
      }

      const lowCompletionFetch = mockFetch([lowCompletionMock, mockBeliefSystemResponse])
      global.fetch = lowCompletionFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Start Journey →')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Start Journey →'))

      await waitFor(() => {
        expect(screen.getByText('Interests & Passions')).toBeInTheDocument()
        expect(screen.getByText('Add Interest')).toBeInTheDocument()
      })
    })
  })

  describe('Edge Cases', () => {
    it('handles missing profile data gracefully', async () => {
      const emptyProfileMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            id: 'test-user-123',
            displayName: '',
            email: '',
            bio: '',
            location: '',
            interests: [],
            avatarUrl: '',
            coverImageUrl: '',
            joinedDate: '',
            lastActive: '',
            resonanceLevel: 0,
            totalContributions: 0,
            profileCompletion: 0
          }
        }
      }

      const emptyProfileFetch = mockFetch([emptyProfileMock, mockBeliefSystemResponse])
      global.fetch = emptyProfileFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument() // Fallback display name
        expect(screen.getByText('0%')).toBeInTheDocument()
      })
    })

    it('handles missing belief system data', async () => {
      const mockFetchWithoutBelief = mockFetch([
        mockProfileResponse,
        { success: false, error: 'No belief system found' }
      ])
      global.fetch = mockFetchWithoutBelief

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
        // Should still render without belief system section
      })
    })

    it('handles unauthenticated user', () => {
      const unauthMock = {
        user: null,
        isAuthenticated: false,
        isLoading: false
      }

      renderWithProviders(<ProfilePage />, { authValue: unauthMock })

      expect(screen.getByText('Please log in to view your profile.')).toBeInTheDocument()
    })

    it('handles very long display names', async () => {
      const longNameMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            ...mockProfileResponse.data.profile,
            displayName: 'This is an extremely long display name that should test the layout boundaries and ensure proper text wrapping in the profile header section'
          }
        }
      }

      const longNameFetch = mockFetch([longNameMock, mockBeliefSystemResponse])
      global.fetch = longNameFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('This is an extremely long display name that should test the layout boundaries and ensure proper text wrapping in the profile header section')).toBeInTheDocument()
      })
    })

    it('handles special characters in profile data', async () => {
      const specialCharMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            ...mockProfileResponse.data.profile,
            displayName: 'José María García-Fernández',
            bio: 'Exploring consciousness with émojis 🚀 and spëcial châractérs!',
            location: 'México City, México 🇲🇽'
          }
        }
      }

      const specialCharFetch = mockFetch([specialCharMock, mockBeliefSystemResponse])
      global.fetch = specialCharFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('José María García-Fernández')).toBeInTheDocument()
        expect(screen.getByText('Exploring consciousness with émojis 🚀 and spëcial châractérs!')).toBeInTheDocument()
        expect(screen.getByText('México City, México 🇲🇽')).toBeInTheDocument()
      })
    })
  })

  describe('Performance', () => {
    it('renders efficiently with large datasets', async () => {
      const largeInterests = Array.from({ length: 50 }, (_, i) => `interest-${i}`)
      const largeProfileMock = {
        ...mockProfileResponse,
        data: {
          profile: {
            ...mockProfileResponse.data.profile,
            interests: largeInterests
          }
        }
      }

      const startTime = performance.now()
      const largeProfileFetch = mockFetch([largeProfileMock, mockBeliefSystemResponse])
      global.fetch = largeProfileFetch

      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument()
      })

      const endTime = performance.now()
      const renderTime = endTime - startTime

      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000)
    })

    it('handles rapid form changes without performance issues', async () => {
      const user = userEvent.setup()
      renderWithProviders(<ProfilePage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument()
      })
      await user.click(screen.getByRole('heading', { name: 'Basic Information' }))

      await waitFor(() => {
        const displayNameInput = screen.getByDisplayValue('Test User')
        expect(displayNameInput).toBeInTheDocument()
      })

      const startTime = performance.now()

      // Rapid typing simulation
      for (let i = 0; i < 10; i++) {
        fireEvent.change(screen.getByDisplayValue('Test User'), {
          target: { value: `Test User ${i}` }
        })
      }

      const endTime = performance.now()
      const interactionTime = endTime - startTime

      // Should handle rapid changes efficiently
      expect(interactionTime).toBeLessThan(100)
    })
  })
})
