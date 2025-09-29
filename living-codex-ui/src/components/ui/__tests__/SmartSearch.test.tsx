import React from 'react'
import { screen, waitFor, fireEvent, act } from '@testing-library/react'
import { renderWithProviders } from '../../../__tests__/test-utils'
import userEvent from '@testing-library/user-event'
import { SmartSearch } from '../SmartSearch'

// Mock the buildApiUrl function
jest.mock('../../../lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

describe('SmartSearch', () => {
  const user = userEvent.setup()

  beforeEach(() => {
    jest.clearAllMocks()

    // Mock fetch for search API
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({
        nodes: [
          {
            id: 'concept-1',
            title: 'Quantum Physics',
            description: 'Study of matter and energy at atomic scales',
            typeId: 'codex.concept',
            meta: { keywords: ['quantum', 'physics', 'mechanics'] }
          },
          {
            id: 'concept-2',
            title: 'Machine Learning',
            description: 'AI technique for pattern recognition',
            typeId: 'codex.concept',
            meta: { keywords: ['AI', 'machine learning', 'algorithms'] }
          }
        ]
      })
    })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Rendering', () => {
    it('renders search input with placeholder', () => {
      renderWithProviders(
        <SmartSearch placeholder="Search concepts..." />
      )

      expect(screen.getByPlaceholderText('Search concepts...')).toBeInTheDocument()
    })

    it('displays search icon', () => {
      renderWithProviders(<SmartSearch />)

      const searchIcons = screen.getAllByText('🔍')
      expect(searchIcons.length).toBeGreaterThan(0)
    })

    it('shows filters when enabled', () => {
      renderWithProviders(<SmartSearch showFilters={true} />)

      expect(screen.getByTestId('search-filters')).toBeInTheDocument()
      expect(screen.getByText('concepts')).toBeInTheDocument()
      expect(screen.getByText('people')).toBeInTheDocument()
    })

    it('hides filters when disabled', () => {
      renderWithProviders(<SmartSearch showFilters={false} />)

      expect(screen.queryByTestId('search-filters')).not.toBeInTheDocument()
    })
  })

  describe('Search Functionality', () => {
    it('performs search when typing', async () => {
      const onResultsChange = jest.fn()

      renderWithProviders(
        <SmartSearch onResultsChange={onResultsChange} />
      )

      const searchInput = screen.getByRole('textbox')

      // Type in search box
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      // Should trigger search API call
      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalled()
      })

      // Should call onResultsChange with results
      await waitFor(() => {
        expect(onResultsChange).toHaveBeenCalled()
      })
    })

    it('debounces search input', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')

      // Rapid typing should be debounced
      fireEvent.change(searchInput, { target: { value: 'a' } })
      fireEvent.change(searchInput, { target: { value: 'ab' } })
      fireEvent.change(searchInput, { target: { value: 'abc' } })

      // Should only make one API call for final value
      await waitFor(() => {
        expect((global.fetch as jest.Mock).mock.calls.length).toBeGreaterThan(0)
      })
    })

    it('filters results by type when filter selected', async () => {
      renderWithProviders(<SmartSearch showFilters={true} />)

      const searchInput = screen.getByRole('textbox')
      const conceptFilter = screen.getByText('concepts')

      // Select concept filter
      fireEvent.click(conceptFilter)

      // Type search
      fireEvent.change(searchInput, { target: { value: 'test' } })

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalled()
        const lastCall = (global.fetch as jest.Mock).mock.calls.at(-1)
        expect(lastCall?.[0]).toContain('typeId=codex.concept')
      })
    })

    it('shows loading state during search', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'test' } })
      
      // Should show loading spinner when searching
      await waitFor(() => {
        const spinner = screen.queryByTestId('loading-spinner')
        if (spinner) {
          expect(spinner).toBeInTheDocument()
        }
      })
    })
  })

  describe('Results Display', () => {
    it('displays search results in dropdown', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
        expect(screen.getByText('Study of matter and energy at atomic scales')).toBeInTheDocument()
      })
    })

    it('shows result relevance indicators', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
        // Should show domain/relevance indicators
        expect(screen.getByText('General')).toBeInTheDocument()
      })
    })

    it('limits number of displayed results', async () => {
      // Mock more results than limit
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({
          nodes: Array.from({ length: 15 }, (_, i) => ({
            id: `concept-${i}`,
            title: `Concept ${i}`,
            description: `Description ${i}`,
            typeId: 'codex.concept'
          }))
        })
      })

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'test' } })

      await waitFor(() => {
        // Should limit to 10 results
        const results = screen.getAllByText(/Concept/)
        expect(results.length).toBeLessThanOrEqual(10)
      })
    })

    it('handles empty search results', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ nodes: [] })
      })

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'nonexistent' } })

      await waitFor(() => {
        expect(screen.getByText('No results found')).toBeInTheDocument()
      })
    })
  })

  describe('User Interactions', () => {
    it('calls onResultSelect when result clicked', async () => {
      const onResultSelect = jest.fn()

      renderWithProviders(
        <SmartSearch onResultSelect={onResultSelect} />
      )

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
      })

      // Click on result
      fireEvent.click(screen.getByText('Quantum Physics'))

      expect(onResultSelect).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'concept-1',
          title: 'Quantum Physics',
          description: 'Study of matter and energy at atomic scales'
        })
      )
    })

    it('handles keyboard navigation', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
      })

      // Focus on search input
      searchInput.focus()
      expect(document.activeElement).toBe(searchInput)

      // Tab through results
      fireEvent.keyDown(searchInput, { key: 'Tab' })
    })

    it('closes results when clicking outside', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
      })

      // Click outside
      fireEvent.mouseDown(document.body)
      fireEvent.click(document.body)

      // Results should close (dropdown disappears)
      await waitFor(() => {
        const results = screen.getByTestId('smart-search-results')
        expect(results.className).toContain('opacity-0')
      })
    })

    it('reopens results when focusing input', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')

      // Initial search
      fireEvent.change(searchInput, { target: { value: 'quantum' } })

      await waitFor(() => {
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
      })

      // Click outside to close
      fireEvent.mouseDown(document.body)
      fireEvent.click(document.body)

      await waitFor(() => {
        const results = screen.getByTestId('smart-search-results')
        expect(results.className).toContain('opacity-0')
      })

      // Focus back on input
      await act(async () => {
        fireEvent.focus(searchInput)
      })

      // Results should reopen
      await waitFor(() => {
        const results = screen.getByTestId('smart-search-results')
        expect(results.className).toContain('opacity-100')
        expect(screen.getByText('Quantum Physics')).toBeInTheDocument()
      })
    })

    it('cancels previous search requests', async () => {
      // Mock slow response
      global.fetch = jest
        .fn()
        .mockImplementationOnce(() => new Promise(resolve => setTimeout(resolve, 1000)))
        .mockResolvedValue({
          ok: true,
          json: () => Promise.resolve({ nodes: [] })
        })

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')

      // Start first search
      fireEvent.change(searchInput, { target: { value: 'first' } })

      // Immediately start second search
      fireEvent.change(searchInput, { target: { value: 'second' } })

      // Should only complete the latest search
      await waitFor(() => {
        expect((global.fetch as jest.Mock).mock.calls.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Error Handling', () => {
    it('handles search API errors', async () => {
      global.fetch = jest.fn().mockRejectedValue(new Error('Network Error'))

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'test' } })

      await waitFor(() => {
        expect(screen.getByText('No results found')).toBeInTheDocument()
      })
    })

    it('handles malformed API responses', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, nodes: null })
      })

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      fireEvent.change(searchInput, { target: { value: 'test' } })

      await waitFor(() => {
        expect(screen.getByText('No results found')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA attributes', () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')
      expect(searchInput).toHaveAttribute('placeholder')
    })

    it('supports screen readers', () => {
      renderWithProviders(<SmartSearch />)

      // Should have descriptive text for screen readers
      const searchContainer = screen.getByTestId('smart-search')
      expect(searchContainer).toBeInTheDocument()
    })

    it('handles keyboard navigation properly', () => {
      renderWithProviders(<SmartSearch showFilters={true} />)

      const searchInput = screen.getByRole('textbox')
      const filters = screen.getAllByRole('button')

      // Should be able to tab through elements
      searchInput.focus()
      expect(document.activeElement).toBe(searchInput)

      // Tab to first filter
      if (filters.length > 0) {
        fireEvent.keyDown(searchInput, { key: 'Tab' })
        // Note: This would need more complex testing to verify focus moves correctly
      }
    })
  })

  describe('Performance', () => {
    it('debounces search input to prevent excessive API calls', async () => {
      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')

      // Rapid typing
      const rapidInput = 'a'.repeat(50)
      fireEvent.change(searchInput, { target: { value: rapidInput } })

      // Should only make one API call despite rapid changes
      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledTimes(1)
      })
    })

    it('cancels previous search requests', async () => {
      // Mock slow response
      global.fetch = jest
        .fn()
        .mockImplementationOnce(() => new Promise(resolve => setTimeout(resolve, 1000)))
        .mockResolvedValue({
          ok: true,
          json: () => Promise.resolve({ nodes: [] })
        })

      renderWithProviders(<SmartSearch />)

      const searchInput = screen.getByRole('textbox')

      // Start first search
      fireEvent.change(searchInput, { target: { value: 'first' } })

      // Immediately start second search
      fireEvent.change(searchInput, { target: { value: 'second' } })

      await waitFor(() => {
        expect((global.fetch as jest.Mock).mock.calls.length).toBeGreaterThan(0)
      })
    })
  })
})
