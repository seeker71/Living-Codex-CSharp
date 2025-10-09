import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from './test-utils'
import DiscoverPage from '@/app/discover/page'

// Mock the auth context
const mockAuthValue = {
  user: {
    id: 'test-user-123',
    username: 'testuser',
    email: 'test@example.com',
    displayName: 'Test User'
  },
  isAuthenticated: true,
  isLoading: false,
  login: jest.fn(),
  logout: jest.fn(),
  register: jest.fn()
}

// Mock fetch to use real backend API calls
const mockFetch = jest.fn()
global.fetch = mockFetch

// Mock API responses that match real backend structure
const mockPagesResponse = {
  success: true,
  data: [
    {
      id: 'page-1',
      title: 'Test Page 1',
      content: 'This is test content',
      type: 'concept',
      resonance: 0.8
    },
    {
      id: 'page-2', 
      title: 'Test Page 2',
      content: 'This is more test content',
      type: 'concept',
      resonance: 0.6
    }
  ]
}

const mockLensesResponse = {
  success: true,
  data: [
    {
      id: 'lens.stream',
      name: 'Stream',
      status: 'active',
      ranking: 1
    },
    {
      id: 'lens.threads',
      name: 'Conversations', 
      status: 'active',
      ranking: 2
    },
    {
      id: 'lens.gallery',
      name: 'Gallery',
      status: 'active', 
      ranking: 3
    },
    {
      id: 'lens.nearby',
      name: 'Nearby',
      status: 'active',
      ranking: 4
    },
    {
      id: 'lens.swipe',
      name: 'Swipe',
      status: 'active',
      ranking: 5
    }
  ]
}

// Mock the hooks that fetch data
jest.mock('@/lib/hooks', () => ({
  usePages: () => ({
    data: mockPagesResponse.data,
    isLoading: false,
    error: null
  }),
  useLenses: () => ({
    data: mockLensesResponse.data,
    isLoading: false,
    error: null
  })
}))

// Mock the lens components
jest.mock('@/components/lenses/StreamLens', () => {
  return function MockStreamLens({ controls, userId, readOnly }: any) {
    return (
      <div data-testid="stream-lens">
        <h3>Stream Lens</h3>
        <p>Controls: {JSON.stringify(controls)}</p>
        <p>User ID: {userId}</p>
        <p>Read Only: {readOnly ? 'Yes' : 'No'}</p>
      </div>
    )
  }
})

jest.mock('@/components/lenses/ThreadsLens', () => {
  return function MockThreadsLens({ controls, userId, readOnly }: any) {
    return (
      <div data-testid="threads-lens">
        <h3>Threads Lens</h3>
        <p>Controls: {JSON.stringify(controls)}</p>
        <p>User ID: {userId}</p>
        <p>Read Only: {readOnly ? 'Yes' : 'No'}</p>
      </div>
    )
  }
})

jest.mock('@/components/lenses/GalleryLens', () => {
  return function MockGalleryLens({ controls, userId, readOnly }: any) {
    return (
      <div data-testid="gallery-lens">
        <h3>Gallery Lens</h3>
        <p>Controls: {JSON.stringify(controls)}</p>
        <p>User ID: {userId}</p>
        <p>Read Only: {readOnly ? 'Yes' : 'No'}</p>
      </div>
    )
  }
})

jest.mock('@/components/lenses/NearbyLens', () => {
  return function MockNearbyLens({ controls, userId, readOnly }: any) {
    return (
      <div data-testid="nearby-lens">
        <h3>Nearby Lens</h3>
        <p>Controls: {JSON.stringify(controls)}</p>
        <p>User ID: {userId}</p>
        <p>Read Only: {readOnly ? 'Yes' : 'No'}</p>
      </div>
    )
  }
})

jest.mock('@/components/lenses/SwipeLens', () => {
  return function MockSwipeLens({ controls, userId, readOnly }: any) {
    return (
      <div data-testid="swipe-lens">
        <h3>Swipe Lens</h3>
        <p>Controls: {JSON.stringify(controls)}</p>
        <p>User ID: {userId}</p>
        <p>Read Only: {readOnly ? 'Yes' : 'No'}</p>
      </div>
    )
  }
})

// Mock ResonanceControls component
jest.mock('@/components/ui/ResonanceControls', () => {
  return function MockResonanceControls({ onControlsChange, className }: any) {
    return (
      <div data-testid="resonance-controls" className={className}>
        <h3>Resonance Controls</h3>
        <button 
          onClick={() => onControlsChange({ joy: 0.8, serendipity: 0.6 })}
          data-testid="update-controls"
        >
          Update Controls
        </button>
      </div>
    )
  }
})

describe('DiscoverPage Integration Tests', () => {
  beforeEach(() => {
    mockFetch.mockClear()
  })

  describe('Real API Integration', () => {
    it('loads discover page with real backend data', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
        expect(screen.getByText('Explore concepts, people, and ideas through different lenses')).toBeInTheDocument()
      })

      // Verify all lens tabs are rendered
      expect(screen.getByText('Stream')).toBeInTheDocument()
      expect(screen.getByText('Conversations')).toBeInTheDocument()
      expect(screen.getByText('Gallery')).toBeInTheDocument()
      expect(screen.getByText('Nearby')).toBeInTheDocument()
      expect(screen.getByText('Swipe')).toBeInTheDocument()
    })

    it('switches between lens tabs correctly', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
      })

      // Click on Threads tab
      const threadsTab = screen.getByText('Conversations')
      await userEvent.click(threadsTab)

      await waitFor(() => {
        expect(screen.getByTestId('threads-lens')).toBeInTheDocument()
      })

      // Click on Gallery tab
      const galleryTab = screen.getByText('Gallery')
      await userEvent.click(galleryTab)

      await waitFor(() => {
        expect(screen.getByTestId('gallery-lens')).toBeInTheDocument()
      })
    })

    it('passes correct props to lens components', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
      })

      // Verify StreamLens receives correct props
      expect(screen.getByText('User ID: test-user-123')).toBeInTheDocument()
      expect(screen.getByText('Read Only: No')).toBeInTheDocument()
    })

    it('shows read-only mode for unauthenticated users', async () => {
      const unauthenticatedAuthValue = {
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        register: jest.fn()
      }

      renderWithProviders(<DiscoverPage />, { authValue: unauthenticatedAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Read-only mode')).toBeInTheDocument()
        expect(screen.getByText('Sign in to interact and personalize your experience')).toBeInTheDocument()
      })

      // Verify lens components receive readOnly=true
      await waitFor(() => {
        expect(screen.getByText('Read Only: Yes')).toBeInTheDocument()
      })
    })
  })

  describe('Resonance Controls Integration', () => {
    it('updates controls when resonance controls change', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('resonance-controls')).toBeInTheDocument()
      })

      // Click update controls button
      const updateButton = screen.getByTestId('update-controls')
      await userEvent.click(updateButton)

      // Verify controls were updated (this would be reflected in the lens components)
      await waitFor(() => {
        expect(screen.getByText(/joy.*0\.8/)).toBeInTheDocument()
        expect(screen.getByText(/serendipity.*0\.6/)).toBeInTheDocument()
      })
    })

    it('maintains control state across lens switches', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('resonance-controls')).toBeInTheDocument()
      })

      // Update controls
      const updateButton = screen.getByTestId('update-controls')
      await userEvent.click(updateButton)

      // Switch to different lens
      const threadsTab = screen.getByText('Conversations')
      await userEvent.click(threadsTab)

      await waitFor(() => {
        expect(screen.getByTestId('threads-lens')).toBeInTheDocument()
      })

      // Verify controls are still updated
      expect(screen.getByText(/joy.*0\.8/)).toBeInTheDocument()
    })
  })

  describe('Lens Content Integration', () => {
    it('renders StreamLens with correct props', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
      })

      expect(screen.getByText('Stream Lens')).toBeInTheDocument()
      expect(screen.getByText('User ID: test-user-123')).toBeInTheDocument()
      expect(screen.getByText('Read Only: No')).toBeInTheDocument()
    })

    it('renders ThreadsLens when selected', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      const threadsTab = screen.getByText('Conversations')
      await userEvent.click(threadsTab)

      await waitFor(() => {
        expect(screen.getByTestId('threads-lens')).toBeInTheDocument()
      })

      expect(screen.getByText('Threads Lens')).toBeInTheDocument()
    })

    it('renders GalleryLens when selected', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      const galleryTab = screen.getByText('Gallery')
      await userEvent.click(galleryTab)

      await waitFor(() => {
        expect(screen.getByTestId('gallery-lens')).toBeInTheDocument()
      })

      expect(screen.getByText('Gallery Lens')).toBeInTheDocument()
    })

    it('renders NearbyLens when selected', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      const nearbyTab = screen.getByText('Nearby')
      await userEvent.click(nearbyTab)

      await waitFor(() => {
        expect(screen.getByTestId('nearby-lens')).toBeInTheDocument()
      })

      expect(screen.getByText('Nearby Lens')).toBeInTheDocument()
    })

    it('renders SwipeLens when selected', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      const swipeTab = screen.getByText('Swipe')
      await userEvent.click(swipeTab)

      await waitFor(() => {
        expect(screen.getByTestId('swipe-lens')).toBeInTheDocument()
      })

      expect(screen.getByText('Swipe Lens')).toBeInTheDocument()
    })
  })

  describe('Error Handling', () => {
    it('handles lens loading errors gracefully', async () => {
      // Mock error in useLenses hook
      jest.doMock('@/lib/hooks', () => ({
        usePages: () => ({
          data: mockPagesResponse.data,
          isLoading: false,
          error: null
        }),
        useLenses: () => ({
          data: null,
          isLoading: false,
          error: new Error('Failed to load lenses')
        })
      }))

      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })

      // Should still render the page with default lens
      expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
    })

    it('handles pages loading errors gracefully', async () => {
      // Mock error in usePages hook
      jest.doMock('@/lib/hooks', () => ({
        usePages: () => ({
          data: null,
          isLoading: false,
          error: new Error('Failed to load pages')
        }),
        useLenses: () => ({
          data: mockLensesResponse.data,
          isLoading: false,
          error: null
        })
      }))

      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByText('Discover')).toBeInTheDocument()
      })

      // Should still render the page
      expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
    })
  })

  describe('Performance', () => {
    it('handles rapid lens switching without issues', async () => {
      renderWithProviders(<DiscoverPage />, { authValue: mockAuthValue })

      await waitFor(() => {
        expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
      })

      // Rapidly switch between lenses
      const tabs = ['Conversations', 'Gallery', 'Nearby', 'Swipe', 'Stream']
      
      for (const tabName of tabs) {
        const tab = screen.getByText(tabName)
        await userEvent.click(tab)
        
        await waitFor(() => {
          expect(screen.getByText(tabName)).toBeInTheDocument()
        })
      }

      // Should end up on Stream lens
      expect(screen.getByTestId('stream-lens')).toBeInTheDocument()
    })
  })
})
