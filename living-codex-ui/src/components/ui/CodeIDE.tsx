'use client'

import React, { useState, useCallback } from 'react'
import FileBrowser from './FileBrowser'
import CodeEditor from './CodeEditor'

interface FileNode {
  id: string
  name?: string
  type?: string
  title?: string
  content?: {
    externalUri?: string
  }
  meta?: {
    absolutePath?: string
    relativePath?: string
    fileName?: string
    extension?: string
    size?: number
    lastModified?: string
    isReadOnly?: boolean
    directory?: string
  }
}

interface CodeIDEProps {
  className?: string
  defaultSplitRatio?: number // 0.0 to 1.0, default 0.3 (30% for browser, 70% for editor)
  onFileSave?: (content: string, nodeId: string) => Promise<void>
}

export function CodeIDE({ 
  className = '',
  defaultSplitRatio = 0.3,
  onFileSave
}: CodeIDEProps) {
  const [selectedNodeId, setSelectedNodeId] = useState<string | undefined>()
  const [selectedFileNode, setSelectedFileNode] = useState<FileNode | undefined>()
  const [splitRatio, setSplitRatio] = useState(defaultSplitRatio)
  const [isDragging, setIsDragging] = useState(false)

  const handleFileSelect = useCallback((nodeId: string, fileNode: FileNode) => {
    setSelectedNodeId(nodeId)
    setSelectedFileNode(fileNode)
  }, [])

  const handleFileSave = useCallback(async (content: string, nodeId: string) => {
    if (onFileSave) {
      await onFileSave(content, nodeId)
    }
  }, [onFileSave])

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }, [])

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging) return
    
    const container = document.getElementById('code-ide-container')
    if (!container) return
    
    const rect = container.getBoundingClientRect()
    const newRatio = Math.max(0.2, Math.min(0.8, (e.clientX - rect.left) / rect.width))
    setSplitRatio(newRatio)
  }, [isDragging])

  const handleMouseUp = useCallback(() => {
    setIsDragging(false)
  }, [])

  // Add global mouse event listeners for dragging
  React.useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
      return () => {
        document.removeEventListener('mousemove', handleMouseMove)
        document.removeEventListener('mouseup', handleMouseUp)
      }
    }
  }, [isDragging, handleMouseMove, handleMouseUp])

  return (
    <div 
      id="code-ide-container"
      className={`flex h-full bg-gray-100 dark:bg-gray-900 ${className}`}
      style={{ userSelect: isDragging ? 'none' : 'auto' }}
    >
      {/* File Browser Panel */}
      <div 
        className="flex-shrink-0 bg-white dark:bg-gray-800"
        style={{ width: `${splitRatio * 100}%` }}
      >
        <FileBrowser
          onFileSelect={handleFileSelect}
          selectedNodeId={selectedNodeId}
          className="h-full border-r border-gray-200 dark:border-gray-700"
        />
      </div>

      {/* Resizer */}
      <div
        className="w-1 bg-gray-200 dark:bg-gray-700 hover:bg-blue-500 dark:hover:bg-blue-600 cursor-col-resize transition-colors"
        onMouseDown={handleMouseDown}
      />

      {/* Code Editor Panel */}
      <div 
        className="flex-1 bg-white dark:bg-gray-800"
        style={{ width: `${(1 - splitRatio) * 100}%` }}
      >
        {selectedNodeId ? (
          <CodeEditor
            nodeId={selectedNodeId}
            onSave={handleFileSave}
            readOnly={selectedFileNode?.meta?.isReadOnly}
            className="h-full"
          />
        ) : (
          <div className="flex flex-col items-center justify-center h-full text-center p-8">
            <div className="mb-4 text-6xl">📝</div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Welcome to the Code Editor
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4 max-w-md">
              Select a file from the browser on the left to start editing. All files in your project 
              are represented as nodes in the Living Codex system.
            </p>
            <div className="text-sm text-gray-500 dark:text-gray-500 space-y-1">
              <p>• Files are automatically saved as you edit</p>
              <p>• Changes are tracked with contribution metadata</p>
              <p>• File watching keeps nodes synchronized</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default CodeIDE



