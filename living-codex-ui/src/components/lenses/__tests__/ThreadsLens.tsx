import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import '@testing-library/jest-dom'
import { ThreadsLens } from '../ThreadsLens'

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
const mockFetch = fetch as jest.MockedFunction<typeof fetch>
global.fetch = mockFetch

describe('ThreadsLens Component', () => {
  const mockConversations = [
    {
      id: 'conv-1',
      title: 'Test Conversation 1',
      content: 'This is a test conversation',
      author: { id: 'user-1', name: 'User One' },
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T01:00:00Z',
      lastActivity: '2024-01-01T01:00:00Z',
      replies: [
        {
          id: 'reply-1',
          content: 'Test reply',
          author: { id: 'user-2', name: 'User Two' },
          createdAt: '2024-01-01T01:00:00Z',
          resonance: 0.5,
          isAccepted: false,
          reactions: { '👍': ['user-2'] }
        }
      ],
      resonance: 0.5,
      axes: ['consciousness', 'unity'],
      isResolved: false,
      primaryGroupId: 'group-1',
      groupIds: ['group-1'],
      replyCount: 1,
      hasUnread: true
    },
    {
      id: 'conv-2',
      title: 'Test Conversation 2',
      content: 'Another test conversation',
      author: { id: 'user-3', name: 'User Three' },
      createdAt: '2024-01-02T00:00:00Z',
      updatedAt: '2024-01-02T01:00:00Z',
      lastActivity: '2024-01-02T01:00:00Z',
      replies: [],
      resonance: 0.7,
      axes: ['innovation', 'science'],
      isResolved: true,
      primaryGroupId: 'group-2',
      groupIds: ['group-2'],
      replyCount: 0,
      hasUnread: false
    }
  ]

  const mockGroups = [
    {
      id: 'group-1',
      name: 'General',
      description: 'General discussion group',
      threadCount: 10,
      color: '#3B82F6',
      isDefault: true
    },
    {
      id: 'group-2',
      name: 'Tech',
      description: 'Technology discussions',
      threadCount: 5,
      color: '#10B981',
      isDefault: false
    }
  ]

  beforeEach(() => {
    jest.clearAllMocks()
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ success: true, threads: mockConversations, groups: mockGroups })
    } as Response)
  })

  describe('Component Rendering', () => {
    it('renders loading state initially', async () => {
      mockFetch.mockImplementation(() =>
        new Promise(resolve => setTimeout(() => resolve({
          ok: true,
          json: async () => ({ success: true, threads: [], groups: [] })
        } as Response), 100))
      )

      render(<ThreadsLens />)

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument()
      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('renders conversations list after loading', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
        expect(screen.getByText('Test Conversation 2')).toBeInTheDocument()
      })
    })

    it('renders header with correct title and description', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Conversations')).toBeInTheDocument()
        expect(screen.getByText('Meaningful discussions and collaborative exploration')).toBeInTheDocument()
      })
    })

    it('renders action buttons for authenticated users', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('New Group')).toBeInTheDocument()
        expect(screen.getByText('Start Conversation')).toBeInTheDocument()
      })
    })

    it('renders read-only state for non-authenticated users', async () => {
      jest.doMock('../../../contexts/AuthContext', () => ({
        useAuth: () => ({
          user: null,
          isAuthenticated: false,
          loading: false
        })
      }))

      render(<ThreadsLens readOnly={true} />)

      await waitFor(() => {
        expect(screen.getByText('Sign in to participate in conversations')).toBeInTheDocument()
        expect(screen.queryByText('New Group')).not.toBeInTheDocument()
      })
    })

    it('renders groups navigation with correct styling', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Groups')).toBeInTheDocument()
        expect(screen.getByText('General')).toBeInTheDocument()
        expect(screen.getByText('Tech')).toBeInTheDocument()
        expect(screen.getByText('10 threads')).toBeInTheDocument()
        expect(screen.getByText('5 threads')).toBeInTheDocument()
      })
    })

    it('renders search and filter controls', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByPlaceholderText('🔍 Search conversations...')).toBeInTheDocument()
        expect(screen.getByDisplayValue('📂 All Topics')).toBeInTheDocument()
      })
    })

    it('renders chat pane with selected conversation', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })
    })
  })

  describe('API Integration', () => {
    it('loads conversations on mount', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/threads/list')
      })
    })

    it('loads groups on mount', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('http://localhost:5002/threads/groups')
      })
    })

    it('handles API errors gracefully', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        json: async () => ({ message: 'Server error' })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Server error')).toBeInTheDocument()
      })
    })

    it('handles network errors gracefully', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Network error loading conversations')).toBeInTheDocument()
      })
    })

    it('creates new conversation successfully', async () => {
      mockFetch.mockImplementation((url) => {
        if (url.includes('/threads/create')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, threadId: 'new-thread-123' })
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations, groups: mockGroups })
        } as Response)
      })

      render(<ThreadsLens />)

      await waitFor(() => {
        fireEvent.click(screen.getByText('Start Conversation'))
      })

      // Fill out the form
      await waitFor(() => {
        const titleInput = screen.getByPlaceholderText('What would you like to explore?')
        const contentInput = screen.getByPlaceholderText('Share your thoughts and invite discussion...')

        fireEvent.change(titleInput, { target: { value: 'New Test Conversation' } })
        fireEvent.change(contentInput, { target: { value: 'This is a new test conversation content' } })

        fireEvent.click(screen.getByText('Start Conversation'))
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/create',
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
          })
        )
      })
    })

    it('creates new reply successfully', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      await waitFor(() => {
        const replyInput = screen.getByPlaceholderText('Write a message...')
        fireEvent.change(replyInput, { target: { value: 'Test reply message' } })
        fireEvent.keyPress(replyInput, { key: 'Enter', code: 'Enter' })
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/reply',
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('Test reply message')
          })
        )
      })
    })

    it('adds emoji reaction successfully', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Wait for the message to appear
      await waitFor(() => {
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })

      // Click on the emoji button
      const emojiButton = screen.getAllByTitle('Add Reaction')[0]
      fireEvent.click(emojiButton)

      // Select an emoji from the picker
      await waitFor(() => {
        const emojiPicker = screen.getByText('👍')
        fireEvent.click(emojiPicker)
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/conv-1/reactions',
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('👍')
          })
        )
      })
    })

    it('removes emoji reaction successfully', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Wait for the message to appear
      await waitFor(() => {
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })

      // Click on existing reaction to remove it
      const existingReaction = screen.getByText('👍')
      fireEvent.click(existingReaction)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/conv-1/reactions/%F0%9F%91%8D',
          expect.objectContaining({
            method: 'DELETE'
          })
        )
      })
    })
  })

  describe('User Interactions', () => {
    it('handles conversation selection', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByText('Test Conversation 1'))

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      })
    })

    it('handles search functionality', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const searchInput = screen.getByPlaceholderText('🔍 Search conversations...')
        fireEvent.change(searchInput, { target: { value: 'test' } })
      })

      await waitFor(() => {
        expect(screen.getByDisplayValue('test')).toBeInTheDocument()
      })
    })

    it('handles group filtering', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const groupButton = screen.getByText('Tech')
        fireEvent.click(groupButton)
      })

      await waitFor(() => {
        expect(screen.getByText('Tech')).toBeInTheDocument()
      })
    })

    it('handles axis filtering', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const filterSelect = screen.getByDisplayValue('📂 All Topics')
        fireEvent.change(filterSelect, { target: { value: 'consciousness' } })
      })

      await waitFor(() => {
        expect(screen.getByDisplayValue('Consciousness')).toBeInTheDocument()
      })
    })

    it('handles keyboard navigation', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      })

      // Test j/k navigation
      fireEvent.keyDown(document, { key: 'j', code: 'KeyJ' })
      fireEvent.keyDown(document, { key: 'k', code: 'KeyK' })
      fireEvent.keyDown(document, { key: 'Enter', code: 'Enter' })

      // Test n/g shortcuts
      fireEvent.keyDown(document, { key: 'n', code: 'KeyN' })
      fireEvent.keyDown(document, { key: 'g', code: 'KeyG' })

      // Test escape
      fireEvent.keyDown(document, { key: 'Escape', code: 'Escape' })
    })

    it('handles message editing', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Wait for the message to appear
      await waitFor(() => {
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })

      // Click the more actions button
      const moreActionsButton = screen.getAllByTitle('More Actions')[0]
      fireEvent.click(moreActionsButton)

      // Click edit
      await waitFor(() => {
        const editButton = screen.getByText('Edit')
        fireEvent.click(editButton)
      })

      // Edit the message
      await waitFor(() => {
        const editInput = screen.getByDisplayValue('This is a test conversation')
        fireEvent.change(editInput, { target: { value: 'Edited conversation content' } })

        const saveButton = screen.getByText('Save')
        fireEvent.click(saveButton)
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/conv-1/edit',
          expect.objectContaining({
            method: 'PUT',
            body: expect.stringContaining('Edited conversation content')
          })
        )
      })
    })

    it('handles message deletion with confirmation', async () => {
      // Mock window.confirm
      const mockConfirm = jest.fn().mockReturnValue(true)
      window.confirm = mockConfirm

      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Wait for the message to appear
      await waitFor(() => {
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })

      // Click the more actions button
      const moreActionsButton = screen.getAllByTitle('More Actions')[0]
      fireEvent.click(moreActionsButton)

      // Click delete
      await waitFor(() => {
        const deleteButton = screen.getByText('Delete')
        fireEvent.click(deleteButton)
      })

      expect(mockConfirm).toHaveBeenCalledWith('Are you sure you want to delete this message?')

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          'http://localhost:5002/threads/conv-1',
          expect.objectContaining({
            method: 'DELETE'
          })
        )
      })
    })

    it('handles message copying', async () => {
      // Mock clipboard API
      const mockClipboard = {
        writeText: jest.fn().mockResolvedValue(undefined)
      }
      Object.assign(navigator, { clipboard: mockClipboard })

      render(<ThreadsLens />)

      await waitFor(() => {
        // Select a conversation first
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Wait for the message to appear
      await waitFor(() => {
        expect(screen.getByText('User One: This is a test conversation')).toBeInTheDocument()
      })

      // Click the more actions button
      const moreActionsButton = screen.getAllByTitle('More Actions')[0]
      fireEvent.click(moreActionsButton)

      // Click copy
      await waitFor(() => {
        const copyButton = screen.getByText('Copy')
        fireEvent.click(copyButton)
      })

      expect(mockClipboard.writeText).toHaveBeenCalledWith('This is a test conversation')
    })
  })

  describe('Edge Cases', () => {
    it('handles empty conversations list', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({ success: true, threads: [], groups: [] })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('No conversations available yet.')).toBeInTheDocument()
      })
    })

    it('handles conversations with no replies', async () => {
      const conversationWithoutReplies = {
        ...mockConversations[0],
        replies: [],
        replyCount: 0
      }

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          threads: [conversationWithoutReplies],
          groups: mockGroups
        })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
        expect(screen.getByText('0 messages')).toBeInTheDocument()
      })
    })

    it('handles invalid API responses', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({ success: false, message: 'Invalid data' })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Invalid data')).toBeInTheDocument()
      })
    })

    it('handles malformed conversation data', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          threads: [
            {
              id: 'invalid-conv',
              title: null, // Invalid data
              content: 'Test content',
              author: { id: 'user-1', name: 'User One' },
              createdAt: 'invalid-date',
              replies: [],
              hasUnread: true
            }
          ],
          groups: mockGroups
        })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test content')).toBeInTheDocument()
      })
    })

    it('handles network timeouts', async () => {
      mockFetch.mockImplementation(() =>
        new Promise((_, reject) =>
          setTimeout(() => reject(new Error('Request timeout')), 100)
        )
      )

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Network error loading conversations')).toBeInTheDocument()
      }, { timeout: 5000 })
    })

    it('handles large conversation lists', async () => {
      const largeConversationList = Array.from({ length: 1000 }, (_, i) => ({
        ...mockConversations[0],
        id: `conv-${i}`,
        title: `Conversation ${i}`,
        hasUnread: i < 10 // First 10 are unread
      }))

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          threads: largeConversationList,
          groups: mockGroups
        })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Conversation 0')).toBeInTheDocument()
        expect(screen.getByText('Conversation 49')).toBeInTheDocument() // Should show first 50
      })

      // Test load more functionality
      const loadMoreButton = screen.getByText('Load More Conversations')
      fireEvent.click(loadMoreButton)

      await waitFor(() => {
        expect(screen.getByText('Conversation 99')).toBeInTheDocument()
      })
    })

    it('handles authentication errors', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ message: 'Unauthorized' })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Unauthorized')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('provides proper ARIA labels for conversations', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const conversationButton = screen.getByLabelText(/Select conversation: Test Conversation 1 by User One \(unread\)/)
        expect(conversationButton).toBeInTheDocument()
        expect(conversationButton).toHaveAttribute('role', 'option')
        expect(conversationButton).toHaveAttribute('aria-pressed', 'false')
      })
    })

    it('supports keyboard navigation', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const firstConversation = screen.getByLabelText(/Select conversation: Test Conversation 1/)
        expect(firstConversation).toHaveAttribute('tabIndex', '0')
      })

      // Test focus management with keyboard
      fireEvent.keyDown(document, { key: 'j', code: 'KeyJ' })
      fireEvent.keyDown(document, { key: 'Enter', code: 'Enter' })
    })

    it('provides proper focus management', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const conversationButton = screen.getByLabelText(/Select conversation: Test Conversation 1/)
        conversationButton.focus()
        expect(document.activeElement).toBe(conversationButton)
      })
    })

    it('supports screen reader announcements', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('2 active conversations')).toBeInTheDocument()
        expect(screen.getByLabelText('System online')).toBeInTheDocument()
      })
    })

    it('provides keyboard shortcuts help', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const helpButton = screen.getByText('⌨️ Help')
        expect(helpButton).toBeInTheDocument()
        expect(helpButton).toHaveAttribute('title', expect.stringContaining('j/k: navigate'))
      })
    })

    it('handles focus trap in modals', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        fireEvent.click(screen.getByText('Start Conversation'))
      })

      await waitFor(() => {
        const modal = screen.getByRole('dialog')
        expect(modal).toBeInTheDocument()

        // Test focus trap
        const firstFocusable = modal.querySelector('input, button, [tabindex]:not([tabindex="-1"])')
        if (firstFocusable) {
          ;(firstFocusable as HTMLElement).focus()
          expect(document.activeElement).toBe(firstFocusable)
        }
      })
    })
  })

  describe('Performance', () => {
    it('handles virtual scrolling efficiently', async () => {
      const largeConversationList = Array.from({ length: 1000 }, (_, i) => ({
        ...mockConversations[0],
        id: `conv-${i}`,
        title: `Conversation ${i}`,
        hasUnread: i < 10
      }))

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          threads: largeConversationList,
          groups: mockGroups
        })
      } as Response)

      const startTime = performance.now()
      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Conversation 0')).toBeInTheDocument()
      })

      const renderTime = performance.now() - startTime
      expect(renderTime).toBeLessThan(1000) // Should render quickly
    })

    it('limits initial render to prevent performance issues', async () => {
      const largeConversationList = Array.from({ length: 1000 }, (_, i) => ({
        ...mockConversations[0],
        id: `conv-${i}`,
        title: `Conversation ${i}`,
        hasUnread: i < 10
      }))

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          threads: largeConversationList,
          groups: mockGroups
        })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        // Should only render first 50 conversations initially
        expect(screen.getByText('Conversation 0')).toBeInTheDocument()
        expect(screen.getByText('Conversation 49')).toBeInTheDocument()
        expect(screen.queryByText('Conversation 50')).not.toBeInTheDocument()
      })
    })

    it('handles memory cleanup on unmount', async () => {
      const { unmount } = render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      })

      // Unmount should not cause memory leaks
      expect(() => unmount()).not.toThrow()
    })
  })

  describe('Error Handling', () => {
    it('displays error messages for failed operations', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({ message: 'Validation error' })
      } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Validation error')).toBeInTheDocument()
      })
    })

    it('handles network failures gracefully', async () => {
      mockFetch.mockRejectedValue(new Error('Connection failed'))

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Network error loading conversations')).toBeInTheDocument()
      })
    })

    it('recovers from temporary errors', async () => {
      // First call fails, second succeeds
      mockFetch
        .mockRejectedValueOnce(new Error('Temporary error'))
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, threads: mockConversations, groups: mockGroups })
        } as Response)

      render(<ThreadsLens />)

      await waitFor(() => {
        expect(screen.getByText('Network error loading conversations')).toBeInTheDocument()
      })

      // Trigger retry by clicking refresh or similar action
      // In this case, we expect the component to eventually load
      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      }, { timeout: 5000 })
    })
  })

  describe('State Management', () => {
    it('maintains conversation selection across re-renders', async () => {
      const { rerender } = render(<ThreadsLens />)

      await waitFor(() => {
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      await waitFor(() => {
        expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
      })

      rerender(<ThreadsLens />)

      // Selection should be maintained
      expect(screen.getByText('Test Conversation 1')).toBeInTheDocument()
    })

    it('handles filter state changes correctly', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        const searchInput = screen.getByPlaceholderText('🔍 Search conversations...')
        fireEvent.change(searchInput, { target: { value: 'test' } })
      })

      await waitFor(() => {
        expect(screen.getByDisplayValue('test')).toBeInTheDocument()
      })

      // Clear search
      const clearButton = screen.getByLabelText('Clear search')
      fireEvent.click(clearButton)

      await waitFor(() => {
        expect(screen.getByDisplayValue('')).toBeInTheDocument()
      })
    })

    it('handles editing state correctly', async () => {
      render(<ThreadsLens />)

      await waitFor(() => {
        fireEvent.click(screen.getByText('Test Conversation 1'))
      })

      // Enter edit mode
      await waitFor(() => {
        const moreActionsButton = screen.getAllByTitle('More Actions')[0]
        fireEvent.click(moreActionsButton)
      })

      await waitFor(() => {
        const editButton = screen.getByText('Edit')
        fireEvent.click(editButton)
      })

      // Should show edit form
      await waitFor(() => {
        expect(screen.getByPlaceholderText('Edit your message...')).toBeInTheDocument()
        expect(screen.getByText('Save')).toBeInTheDocument()
        expect(screen.getByText('Cancel')).toBeInTheDocument()
      })

      // Cancel editing
      const cancelButton = screen.getByText('Cancel')
      fireEvent.click(cancelButton)

      // Should return to normal view
      await waitFor(() => {
        expect(screen.queryByPlaceholderText('Edit your message...')).not.toBeInTheDocument()
      })
    })
  })
})
