import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import '@testing-library/jest-dom'
import CodeIDE from '../CodeIDE'

// Mock the child components
jest.mock('../FileBrowser', () => {
  return function MockFileBrowser({ onFileSelect, selectedNodeId }: any) {
    return (
      <div data-testid="file-browser">
        <div>File Browser</div>
        <div>Selected: {selectedNodeId || 'none'}</div>
        <button
          onClick={() => onFileSelect?.('test-file-id', { 
            id: 'test-file-id', 
            title: 'test.cs',
            meta: { fileName: 'test.cs' }
          })}
        >
          Select Test File
        </button>
      </div>
    )
  }
})

jest.mock('../CodeEditor', () => {
  return function MockCodeEditor({ nodeId, onSave }: any) {
    if (!nodeId) {
      return (
        <div data-testid="code-editor">
          <div className="flex flex-col items-center justify-center h-full text-center p-8">
            <div className="mb-4 text-6xl">📝</div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Welcome to the Code Editor
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4 max-w-md">
              Select a file from the browser on the left to start editing. All files in your project 
              are represented as nodes in the Living Codex system.
            </p>
          </div>
        </div>
      )
    }
    
    return (
      <div data-testid="code-editor">
        <div>Code Editor</div>
        <div>Node ID: {nodeId}</div>
        <button
          onClick={() => onSave?.('test content', nodeId)}
        >
          Save File
        </button>
      </div>
    )
  }
})

describe('CodeIDE', () => {
  it('renders both file browser and code editor', () => {
    render(<CodeIDE />)
    
    expect(screen.getByTestId('file-browser')).toBeInTheDocument()
    expect(screen.getByText('File Browser')).toBeInTheDocument()
    expect(screen.getByText('Welcome to the Code Editor')).toBeInTheDocument()
  })

  it('initially shows no file selected', () => {
    render(<CodeIDE />)
    
    expect(screen.getByText('Selected: none')).toBeInTheDocument()
    expect(screen.getByText('Welcome to the Code Editor')).toBeInTheDocument()
  })

  it('handles file selection from browser', () => {
    render(<CodeIDE />)
    
    // Click to select a file
    fireEvent.click(screen.getByText('Select Test File'))
    
    // Should update both components
    expect(screen.getByText('Selected: test-file-id')).toBeInTheDocument()
    expect(screen.getByText('Node ID: test-file-id')).toBeInTheDocument()
  })

  it('handles file save from editor', () => {
    const onFileSave = jest.fn()
    render(<CodeIDE onFileSave={onFileSave} />)
    
    // First select a file
    fireEvent.click(screen.getByText('Select Test File'))
    
    // Then save it
    fireEvent.click(screen.getByText('Save File'))
    
    expect(onFileSave).toHaveBeenCalledWith('test content', 'test-file-id')
  })

  it('applies custom className', () => {
    const { container } = render(<CodeIDE className="custom-class" />)
    
    expect(container.firstChild).toHaveClass('custom-class')
  })

  it('has resizable panels structure', () => {
    render(<CodeIDE />)
    
    // Should have the main container with flex layout
    const container = screen.getByTestId('file-browser').parentElement
    expect(container).toHaveStyle({ width: '30%' })
    
    // Should have a resizer element
    const resizer = document.querySelector('.cursor-col-resize')
    expect(resizer).toBeInTheDocument()
  })

  it('handles mouse events for resizing', () => {
    render(<CodeIDE />)
    
    const resizer = document.querySelector('.cursor-col-resize')
    expect(resizer).toBeInTheDocument()
    
    // Should be able to mouse down on resizer
    fireEvent.mouseDown(resizer!)
    
    // Note: Full drag testing would require more complex setup with mouse move events
    // This test just ensures the resizer element is present and clickable
  })

  it('uses custom split ratio', () => {
    render(<CodeIDE defaultSplitRatio={0.4} />)
    
    const container = screen.getByTestId('file-browser').parentElement
    expect(container).toHaveStyle({ width: '40%' })
  })

  it('prevents text selection during drag', () => {
    render(<CodeIDE />)
    
    const resizer = document.querySelector('.cursor-col-resize')
    const ideContainer = document.getElementById('code-ide-container')
    
    expect(ideContainer).toHaveStyle({ userSelect: 'auto' })
    
    // Start dragging
    fireEvent.mouseDown(resizer!)
    
    // During drag, text selection should be disabled
    // Note: This test checks the initial state; full drag state testing would need more setup
  })
})
