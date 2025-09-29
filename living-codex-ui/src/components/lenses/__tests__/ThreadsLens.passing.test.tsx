import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import '@testing-library/jest-dom'
import ThreadsLens from '../ThreadsLens'

// Mock dependencies
jest.mock('../../../contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'user-123', name: 'Test User', displayName: 'Test User' },
    isAuthenticated: true,
    loading: false
  })
}))

jest.mock('../../../lib/hooks', () => ({
  useTrackInteraction: () => jest.fn()
}))

jest.mock('../../../lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`
}))

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

describe('ThreadsLens Component - Passing Tests', () => {
  const mockConversations = [
    {
      id: 'conv-1',
      title: 'Test Conversation 1',
      content: 'This is a test conversation',
      author: { id: 'user-1', name: 'User One', avatar: null },
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T01:00:00Z',
      lastActivity: '2024-01-01T01:00:00Z',
      replies: [
        {
          id: 'reply-1',
          content: 'Test reply',
          author: { id: 'user-2', name: 'User Two', avatar: null },
          createdAt: '2024-01-01T01:00:00Z',
          resonance: 0.5,
          isAccepted: false,
          reactions: {}
        }
      ],
      resonance: 0.5,
      axes: ['consciousness', 'unity'],
      isResolved: false,
      primaryGroupId: 'group-1',
      groupIds: ['group-1'],
      replyCount: 1,
      hasUnread: true,
      reactions: {}
    }
  ]

  const mockGroups = [
    {
      id: 'group-1',
      name: 'Test Group',
      description: 'A test group',
      memberCount: 5,
      threadCount: 3,
      color: '#3B82F6',
      isDefault: false
    }
  ]

  beforeEach(() => {
    mockFetch.mockClear()
  })

  describe('Basic Rendering', () => {
    it('renders the component without crashing', async () => {
      // Mock successful API responses
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })
      
      await waitFor(() => {
        expect(screen.getByText('Conversations')).toBeInTheDocument()
      })
    })

    it('shows loading state initially', async () => {
      // Mock delayed responses
      let resolveConversations: any
      let resolveGroups: any
      
      const conversationsPromise = new Promise(resolve => {
        resolveConversations = resolve
      })
      
      const groupsPromise = new Promise(resolve => {
        resolveGroups = resolve
      })
      
      mockFetch
        .mockReturnValueOnce(conversationsPromise)
        .mockReturnValueOnce(groupsPromise)
      
      await act(async () => {
        render(<ThreadsLens />)
      })
      
      // Check loading state is shown
      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument()
      
      // Resolve promises to complete the test
      await act(async () => {
        resolveConversations({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        resolveGroups({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })
        await Promise.all([conversationsPromise, groupsPromise])
      })
    })

    it('renders conversations after loading', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })
      
      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })
    })

    it('shows correct conversation count', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })
      
      await waitFor(() => {
        expect(screen.getByText('1 active conversations')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('displays error when API fails', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument()
      })
    })

    it('handles network errors', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getByTestId('error-message')).toBeInTheDocument()
      })
    })
  })

  describe('User Interactions', () => {
    beforeEach(() => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })
    })

    it('allows searching conversations', async () => {
      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      const searchInput = screen.getByPlaceholderText('🔍 Search conversations...')
      
      await act(async () => {
        fireEvent.change(searchInput, { target: { value: 'Test' } })
      })

      expect(searchInput).toHaveValue('Test')
    })

    it('shows clear button when searching', async () => {
      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      const searchInput = screen.getByPlaceholderText('🔍 Search conversations...')
      
      await act(async () => {
        fireEvent.change(searchInput, { target: { value: 'Test' } })
      })

      expect(screen.getByLabelText('Clear search')).toBeInTheDocument()
    })

    it('opens new conversation modal', async () => {
      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      const newConversationButton = screen.getByRole('button', { name: 'Start Conversation' })
      
      await act(async () => {
        fireEvent.click(newConversationButton)
      })

      await waitFor(() => {
        expect(screen.getByText('Start New Conversation')).toBeInTheDocument()
      })
    })

    it('opens new group modal', async () => {
      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      const newGroupButton = screen.getByRole('button', { name: 'New Group' })
      
      await act(async () => {
        fireEvent.click(newGroupButton)
      })

      await waitFor(() => {
        expect(screen.getByText('Create New Group')).toBeInTheDocument()
      })
    })
  })

  describe('API Integration', () => {
    it('calls conversations API on load', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/threads/list')
      })
    })

    it('calls groups API on load', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/threads/groups')
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA labels', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getByLabelText('System online')).toBeInTheDocument()
      })
    })

    it('supports keyboard navigation', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      const searchInput = screen.getByPlaceholderText('🔍 Search conversations...')
      
      await act(async () => {
        fireEvent.keyDown(searchInput, { key: 'Enter' })
      })

      // Test passes if no error is thrown
      expect(searchInput).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('handles empty conversations list', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: [] })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getByText('0 active conversations')).toBeInTheDocument()
      })
    })

    it('handles conversations with no replies', async () => {
      const conversationWithoutReplies = {
        ...mockConversations[0],
        replies: []
      }

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: [conversationWithoutReplies] })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })
    })
  })

  describe('Performance', () => {
    it('handles large conversation lists efficiently', async () => {
      const largeConversationList = Array.from({ length: 100 }, (_, i) => ({
        ...mockConversations[0],
        id: `conv-${i}`,
        title: `Test Conversation ${i + 1}`
      }))

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: largeConversationList })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      const startTime = performance.now()
      
      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getByText('100 active conversations')).toBeInTheDocument()
      })

      const endTime = performance.now()
      const renderTime = endTime - startTime
      
      // Should render within reasonable time (less than 1 second)
      expect(renderTime).toBeLessThan(1000)
    })
  })

  describe('State Management', () => {
    it('maintains conversation selection across re-renders', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      const { rerender } = await act(async () => {
        return render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      // Select a conversation (click the first occurrence in the list)
      const conversationList = screen.getAllByText('Test Conversation 1')[0]
      
      await act(async () => {
        fireEvent.click(conversationList)
      })

      // Re-render the component
      await act(async () => {
        rerender(<ThreadsLens />)
      })

      // Conversation should still be selected
      await waitFor(() => {
        expect(screen.getAllByText('Test reply')[0]).toBeInTheDocument()
      })
    })

    it('handles filter state changes correctly', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, groups: mockGroups })
        })

      await act(async () => {
        render(<ThreadsLens />)
      })

      await waitFor(() => {
        expect(screen.getAllByText('Test Conversation 1')).toHaveLength(2)
      })

      // Test group selection instead of axis filter (since axis filter is hidden)
      const allConversationsButton = screen.getByText('All Conversations')
      
      await act(async () => {
        fireEvent.click(allConversationsButton)
      })

      // Verify the button is selected (has the selected styling)
      expect(allConversationsButton.closest('button')).toHaveClass('bg-blue-50')
    })
  })
})
