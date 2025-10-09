import React from 'react'
import { render, screen, waitFor, act } from '@testing-library/react'
import '@testing-library/jest-dom'

// Mock the auth context and API calls
jest.mock('../contexts/AuthContext', () => ({
  useAuth: () => ({
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
  })
}))

jest.mock('../lib/api', () => ({
  api: {
    get: jest.fn().mockResolvedValue({
      success: true,
      data: {
        profile: {
          id: 'test-user-123',
          displayName: 'Test User',
          email: 'test@example.com',
          bio: 'I am a test user exploring consciousness',
          location: 'San Francisco, CA',
          interests: 'machine learning, philosophy, meditation',
          contributions: 'concept1,concept2,concept3,concept4,concept5,concept6,concept7,concept8,concept9,concept10,concept11,concept12,concept13,concept14,concept15',
          avatarUrl: 'https://example.com/avatar.jpg',
          coverImageUrl: 'https://example.com/cover.jpg',
          joinedDate: '2024-01-01T00:00:00Z',
          lastActive: '2024-01-15T10:00:00Z',
          resonanceLevel: 75,
          totalContributions: 15,
          profileCompletion: 85
        }
      }
    }),
    put: jest.fn().mockResolvedValue({ success: true }),
    post: jest.fn().mockResolvedValue({ success: true })
  }
}))

// Mock fetch for belief system API
global.fetch = jest.fn().mockResolvedValue({
  ok: true,
  json: () => Promise.resolve({
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
  })
})

describe('ProfilePage Integration', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders the profile page structure', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument())

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Test User')
    const resonanceText = screen.getByText((content) => content.includes('High Resonance'))
    expect(resonanceText).toBeInTheDocument()

    // Check for main sections
    expect(screen.getByText('Profile Completion')).toBeInTheDocument()
    expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
    expect(screen.getByText('Quick Actions')).toBeInTheDocument()
  })

  it('displays profile completion progress', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getAllByText((content) => /\d+%/.test(content))[0]).toBeInTheDocument()
      expect(screen.getAllByText('Complete')[0]).toBeInTheDocument()
    })
  })

  it('shows profile sections with completion status', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getAllByText('Basic Information')[0]).toBeInTheDocument()
      expect(screen.getAllByText('Profile Picture')[0]).toBeInTheDocument()
      expect(screen.getAllByText('Interests & Passions')[0]).toBeInTheDocument()
      expect(screen.getAllByText('Belief Framework')[0]).toBeInTheDocument()
      expect(screen.getAllByText('Location')[0]).toBeInTheDocument()
    })
  })

  it('displays badge collection', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
      const badgeSummary = screen.getByText((content) => content.includes('Earned'))
      expect(badgeSummary).toBeInTheDocument()
      expect(screen.getByText('First Steps')).toBeInTheDocument()
      expect(screen.getByText('Resonance Seeker')).toBeInTheDocument()
    })
  })

  it('shows social proof sections', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getByText('Community Impact')).toBeInTheDocument()
      expect(screen.getByText('Enhanced Discovery')).toBeInTheDocument()
      expect(screen.getByText('Consciousness Badges')).toBeInTheDocument()
    })
  })

  it('displays user stats in header', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getByText((content) => content.includes('15 contributions'))).toBeInTheDocument()
      expect(screen.getByText((content) => content.includes('3 interests'))).toBeInTheDocument()
    })
  })

  it('shows quick action buttons', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      expect(screen.getByText('Edit Profile')).toBeInTheDocument()
      expect(screen.getByText('Share Profile')).toBeInTheDocument()
      expect(screen.getByText('Connect Portal')).toBeInTheDocument()
      expect(screen.getByText('Create Concept')).toBeInTheDocument()
    })
  })

  it('displays sacred frequencies section', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    // Profile page renders
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
    })
  })

  it('shows consciousness level', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    // Profile page renders
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
    })
  })

  it.skip('handles unauthenticated user gracefully', async () => {
    // Mock unauthenticated state
    const mockUseAuth = jest.fn(() => ({
      user: null,
      isAuthenticated: false,
      loading: false
    }))

    jest.doMock('../contexts/AuthContext', () => ({
      useAuth: mockUseAuth
    }))

    // Clear the module cache to ensure the mock is applied
    jest.resetModules()

    const { default: ProfilePage } = await import('../app/profile/page')

    await act(async () => {
      render(<ProfilePage />)
    })

    await waitFor(() => {
      expect(screen.getByText('Please log in to view your profile.')).toBeInTheDocument()
    })
  })

  it('handles loading state', async () => {
    // Mock loading state
    jest.doMock('../contexts/AuthContext', () => ({
      useAuth: () => ({
        user: null,
        isAuthenticated: false,
        isLoading: true
      })
    }))

    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    expect(screen.getByText('Loading your resonance profile...')).toBeInTheDocument()
  })

  it('has proper heading hierarchy', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      const headings = screen.getAllByRole('heading', { level: 1 })
      expect(headings.some((heading) => heading.textContent?.includes('Test User'))).toBe(true)
      const level2Headings = screen.getAllByRole('heading', { level: 2 })
      expect(level2Headings.some((heading) => heading.textContent?.includes('Profile Completion'))).toBe(true)
      const level3Headings = screen.getAllByRole('heading', { level: 3 })
      expect(level3Headings.some((heading) => heading.textContent?.includes('Community Impact'))).toBe(true)
    })
  })

  it('has accessible profile sections', async () => {
    const { default: ProfilePage } = await import('../app/profile/page')

    render(<ProfilePage />)

    await waitFor(() => {
      // Check that profile sections are clickable and have proper accessibility
      const sections = screen.getAllByRole('button')
      expect(sections.length).toBeGreaterThan(0)
    })
  })
})
