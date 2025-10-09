import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import FileBrowser from '../FileBrowser'

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

describe('FileBrowser', () => {
  beforeEach(() => {
    mockFetch.mockClear()
  })

  it('renders loading state initially', () => {
    mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves
    
    render(<FileBrowser />)
    
    expect(screen.getByText('Loading files...')).toBeInTheDocument()
  })

  it('loads and displays file nodes', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'Program.cs',
          size: 1024
        }
      },
      {
        id: 'file2',
        title: 'README.md',
        meta: {
          fileName: 'README.md',
          relativePath: 'README.md',
          size: 512
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalled()
    })

    await waitFor(() => {
      expect(screen.getByText('Program.cs')).toBeInTheDocument()
    })

    expect(screen.getByText('README.md')).toBeInTheDocument()
  })

  it('builds correct tree structure', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'src/Program.cs'
        }
      },
      {
        id: 'file2',
        title: 'Helper.cs',
        meta: {
          fileName: 'Helper.cs',
          relativePath: 'src/Utils/Helper.cs'
        }
      },
      {
        id: 'file3',
        title: 'README.md',
        meta: {
          fileName: 'README.md',
          relativePath: 'README.md'
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('src')).toBeInTheDocument()
    })

    // Initially Program.cs should not be visible (it's inside src directory)
    expect(screen.queryByText('Program.cs')).not.toBeInTheDocument()
    expect(screen.queryByText('Utils')).not.toBeInTheDocument()
    expect(screen.getByText('README.md')).toBeInTheDocument() // This is at root level
  })

  it('handles directory expansion and collapse', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Helper.cs',
        meta: {
          fileName: 'Helper.cs',
          relativePath: 'src/Utils/Helper.cs'
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('src')).toBeInTheDocument()
    })

    // Initially collapsed, Helper.cs should not be visible
    expect(screen.queryByText('Helper.cs')).not.toBeInTheDocument()

    // Click to expand src directory
    fireEvent.click(screen.getByText('src'))

    // Utils directory should now be visible
    expect(screen.getByText('Utils')).toBeInTheDocument()

    // Click to expand Utils directory
    fireEvent.click(screen.getByText('Utils'))

    // Helper.cs should now be visible
    expect(screen.getByText('Helper.cs')).toBeInTheDocument()
  })

  it('calls onFileSelect when file is clicked', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'Program.cs'
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    const onFileSelect = jest.fn()
    render(<FileBrowser onFileSelect={onFileSelect} />)

    await waitFor(() => {
      expect(screen.getByText('Program.cs')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Program.cs'))

    expect(onFileSelect).toHaveBeenCalledWith(
      'file1',
      expect.objectContaining({
        id: 'file1',
        meta: expect.objectContaining({ fileName: 'Program.cs' }),
      })
    )
  })

  it('highlights selected file', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'Program.cs'
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser selectedNodeId="file1" />)

    await waitFor(() => {
      const fileElement = screen.getByText('Program.cs')
      expect(fileElement.closest('div')).toHaveClass('bg-blue-50')
    })
  })

  it('displays read-only indicator', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'readonly.txt',
        meta: {
          fileName: 'readonly.txt',
          relativePath: 'readonly.txt',
          isReadOnly: true
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('readonly.txt')).toBeInTheDocument()
    })

    expect(screen.getByText('🔒')).toBeInTheDocument()
  })

  it('handles error loading files', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'))

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument()
    })
  })

  it('refreshes files when refresh button is clicked', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'Program.cs'
        }
      }
    ]

    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('Program.cs')).toBeInTheDocument()
    })

    // Click refresh button
    const refreshButton = screen.getByTitle('Refresh')
    fireEvent.click(refreshButton)

    // Should call fetch again
    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2)
    })
  })

  it('shows empty state when no files', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: [] })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('No files found')).toBeInTheDocument()
    })

    expect(screen.getByText('0 files')).toBeInTheDocument()
  })

  it('displays correct file icons', async () => {
    const mockNodes = [
      {
        id: 'file1',
        title: 'Program.cs',
        meta: {
          fileName: 'Program.cs',
          relativePath: 'Program.cs'
        }
      },
      {
        id: 'file2',
        title: 'package.json',
        meta: {
          fileName: 'package.json',
          relativePath: 'package.json'
        }
      }
    ]

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ success: true, files: mockNodes })
    })

    render(<FileBrowser />)

    await waitFor(() => {
      expect(screen.getByText('Program.cs')).toBeInTheDocument()
    })

    // Check that different file types get different icons
    const container = screen.getByText('Program.cs').closest('div')
    expect(container).toContainHTML('🔷') // C# files get blue diamond

    const jsonContainer = screen.getByText('package.json').closest('div')
    expect(jsonContainer).toContainHTML('📋') // JSON files get clipboard
  })
})
