import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import CodeEditor from '../CodeEditor'

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

describe('CodeEditor', () => {
  beforeEach(() => {
    mockFetch.mockClear()
  })

  it('renders empty state when no nodeId provided', () => {
    render(<CodeEditor />)
    
    expect(screen.getByText('Select a file to edit')).toBeInTheDocument()
  })

  it('shows loading state when loading file', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves
    
    render(<CodeEditor nodeId="test-node" />)
    
    expect(screen.getByText('Loading file...')).toBeInTheDocument()
  })

  it('loads and displays file content', async () => {
    const mockNode = {
      id: 'test-node',
      title: 'test.cs',
      meta: {
        fileName: 'test.cs',
        relativePath: 'src/test.cs',
        size: 100
      }
    }
    
    const mockContent = {
      content: 'using System;\n\nclass Test { }'
    }

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent)
      })

    render(<CodeEditor nodeId="test-node" />)

    await waitFor(() => {
      expect(screen.getByText('test.cs')).toBeInTheDocument()
    })

    expect(screen.getByText('src/test.cs')).toBeInTheDocument()
    const textarea = screen.getByRole('textbox')
    expect(textarea).toHaveValue('using System;\n\nclass Test { }')
  })

  it('handles save operation', async () => {
    const mockNode = {
      id: 'test-node',
      title: 'test.cs',
      meta: {
        fileName: 'test.cs',
        relativePath: 'src/test.cs'
      }
    }
    
    const mockContent = {
      content: 'original content'
    }

    const mockSaveResponse = {
      success: true,
      message: 'File saved'
    }

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockSaveResponse)
      })
      // Mock the reload after save
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ content: 'modified content' })
      })

    const onSave = jest.fn()
    render(<CodeEditor nodeId="test-node" onSave={onSave} />)

    await waitFor(() => {
      expect(screen.getByDisplayValue('original content')).toBeInTheDocument()
    })

    // Modify content
    const textarea = screen.getByDisplayValue('original content')
    fireEvent.change(textarea, { target: { value: 'modified content' } })

    // Save button should be enabled
    const saveButton = screen.getByText('Save')
    expect(saveButton).not.toBeDisabled()

    // Click save
    fireEvent.click(saveButton)

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/filesystem/content/test-node',
        expect.objectContaining({
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            content: 'modified content',
            authorId: 'ui-user',
            changeReason: 'File updated via UI editor'
          })
        })
      )
    })

    expect(onSave).toHaveBeenCalledWith('modified content', 'test-node')
  })

  it('displays error when file loading fails', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'))

    render(<CodeEditor nodeId="test-node" />)

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument()
    })
  })

  it('shows read-only state correctly', async () => {
    const mockNode = {
      id: 'test-node',
      title: 'test.cs',
      meta: {
        fileName: 'test.cs',
        isReadOnly: true
      }
    }
    
    const mockContent = {
      content: 'readonly content'
    }

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent)
      })

    render(<CodeEditor nodeId="test-node" readOnly={true} />)

    await waitFor(() => {
      expect(screen.getByText('Read Only')).toBeInTheDocument()
    })

    // Save button should not be present
    expect(screen.queryByText('Save')).not.toBeInTheDocument()
  })

  it('tracks unsaved changes correctly', async () => {
    const mockNode = {
      id: 'test-node',
      title: 'test.cs',
      meta: {
        fileName: 'test.cs'
      }
    }
    
    const mockContent = {
      content: 'original'
    }

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent)
      })

    render(<CodeEditor nodeId="test-node" />)

    await waitFor(() => {
      expect(screen.getByDisplayValue('original')).toBeInTheDocument()
    })

    // Initially no changes
    expect(screen.queryByText('Unsaved changes')).not.toBeInTheDocument()

    // Modify content
    const textarea = screen.getByDisplayValue('original')
    fireEvent.change(textarea, { target: { value: 'modified' } })

    // Should show unsaved changes
    expect(screen.getByText('Unsaved changes')).toBeInTheDocument()
  })

  it('displays file metadata correctly', async () => {
    const mockNode = {
      id: 'test-node',
      title: 'test.cs',
      meta: {
        fileName: 'test.cs',
        relativePath: 'src/test.cs',
        size: 1024,
        lastModified: '2023-01-01T12:00:00Z',
        lastEditedBy: 'developer'
      }
    }
    
    const mockContent = {
      content: 'test content'
    }

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockNode)
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent)
      })

    render(<CodeEditor nodeId="test-node" />)

    await waitFor(() => {
      expect(screen.getByText('Size: 1024 bytes')).toBeInTheDocument()
    })

    expect(screen.getByText(/Modified:/)).toBeInTheDocument()
    expect(screen.getByText('Last edited by: developer')).toBeInTheDocument()
    expect(screen.getByText('Language: csharp')).toBeInTheDocument()
    expect(screen.getByText('Lines: 1')).toBeInTheDocument()
    expect(screen.getByText('Characters: 12')).toBeInTheDocument()
  })
})
