'use client'

import React, { useState, useEffect } from 'react'
import { RouteStatusBadge, type RouteStatus } from './RouteStatusBadge'

interface FileNode {
  id: string
  name: string
  type: string
  relativePath: string
  absolutePath: string
  size: number
  lastModified: string
  contentRef: {
    mediaType?: string
    externalUri?: string
    hasInlineContent?: boolean
  }
}

interface CodeEditorProps {
  nodeId?: string
  onSave?: (content: string, nodeId: string) => Promise<void>
  onClose?: () => void
  readOnly?: boolean
  className?: string
}

interface FileUpdateRequest {
  content: string
  authorId?: string
  changeReason?: string
}

export function CodeEditor({ 
  nodeId, 
  onSave, 
  onClose, 
  readOnly = false,
  className = '' 
}: CodeEditorProps) {
  const [fileNode, setFileNode] = useState<FileNode | null>(null)
  const [content, setContent] = useState('')
  const [originalContent, setOriginalContent] = useState('')
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hasChanges, setHasChanges] = useState(false)
  const [status, setStatus] = useState<RouteStatus>('Untested')

  // Load file content when nodeId changes
  useEffect(() => {
    if (!nodeId) {
      setFileNode(null)
      setContent('')
      setOriginalContent('')
      setHasChanges(false)
      return
    }

    loadFileContent(nodeId)
  }, [nodeId])

  // Track changes
  useEffect(() => {
    setHasChanges(content !== originalContent)
  }, [content, originalContent])

  const loadFileContent = async (id: string) => {
    setLoading(true)
    setError(null)
    
    try {
      // First get the node metadata from storage endpoints
      const nodeResponse = await fetch(`http://localhost:5002/storage-endpoints/nodes/${encodeURIComponent(id)}`)
      if (!nodeResponse.ok) {
        throw new Error(`Failed to load node: ${nodeResponse.statusText}`)
      }
      
      const nodeData = await nodeResponse.json()
      
      // Convert storage node to FileNode format
      const storageNode = nodeData.node || nodeData
      if (storageNode) {
        const fileNode: FileNode = {
          id: storageNode.id,
          name: storageNode.meta?.fileName || storageNode.title || storageNode.id,
          type: storageNode.typeId,
          relativePath: storageNode.meta?.relativePath || '',
          absolutePath: storageNode.meta?.absolutePath || '',
          size: storageNode.meta?.size || 0,
          lastModified: storageNode.meta?.lastModified || new Date().toISOString(),
          contentRef: {
            mediaType: storageNode.content?.mediaType,
            externalUri: storageNode.content?.externalUri,
            hasInlineContent: !!storageNode.content?.inlineJson || !!storageNode.content?.inlineBytes
          }
        }
        setFileNode(fileNode)
      }
      
      // Then get the file content
      const contentResponse = await fetch(`http://localhost:5002/filesystem/content/${encodeURIComponent(id)}`)
      if (!contentResponse.ok) {
        throw new Error(`Failed to load content: ${contentResponse.statusText}`)
      }
      
      const contentData = await contentResponse.json()
      const fileContent = contentData.content || ''
      
      setContent(fileContent)
      setOriginalContent(fileContent)
      setStatus('FullyTested') // File loaded successfully
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load file')
      setStatus('Untested')
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    if (!nodeId || !hasChanges) return

    setSaving(true)
    setError(null)
    
    try {
      const updateRequest: FileUpdateRequest = {
        content,
        authorId: 'ui-user', // In a real app, this would come from auth context
        changeReason: 'File updated via UI editor'
      }

      const response = await fetch(`http://localhost:5002/filesystem/content/${encodeURIComponent(nodeId)}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(updateRequest)
      })

      if (!response.ok) {
        throw new Error(`Failed to save: ${response.statusText}`)
      }

      const result = await response.json()
      
      if (!result.success) {
        throw new Error(result.message || 'Save failed')
      }

      setOriginalContent(content)
      setStatus('FullyTested')
      
      // Call external save handler if provided
      if (onSave) {
        await onSave(content, nodeId)
      }
      
      // Reload to get updated metadata
      await loadFileContent(nodeId)
      
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save file')
      setStatus('Untested')
    } finally {
      setSaving(false)
    }
  }

  const getFileLanguage = (fileName?: string): string => {
    if (!fileName) return 'text'
    
    const ext = fileName.split('.').pop()?.toLowerCase()
    
    switch (ext) {
      case 'cs': return 'csharp'
      case 'js': case 'jsx': return 'javascript'
      case 'ts': case 'tsx': return 'typescript'
      case 'json': return 'json'
      case 'md': return 'markdown'
      case 'html': return 'html'
      case 'css': return 'css'
      case 'xml': return 'xml'
      case 'yaml': case 'yml': return 'yaml'
      default: return 'text'
    }
  }

  if (!nodeId) {
    return (
      <div className={`flex items-center justify-center h-64 bg-gray-50 dark:bg-gray-800 rounded-lg ${className}`}>
        <p className="text-gray-500 dark:text-gray-400">Select a file to edit</p>
      </div>
    )
  }

  if (loading) {
    return (
      <div className={`flex items-center justify-center h-64 bg-gray-50 dark:bg-gray-800 rounded-lg ${className}`}>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-gray-600 dark:text-gray-300">Loading file...</p>
        </div>
      </div>
    )
  }

  return (
    <div className={`flex flex-col h-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-3">
          <div className="flex flex-col">
            <h3 className="font-medium text-gray-900 dark:text-gray-100">
              {fileNode?.meta?.fileName || 'Unknown File'}
            </h3>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {fileNode?.meta?.relativePath}
            </p>
          </div>
          <RouteStatusBadge status={status} size="sm" />
        </div>
        
        <div className="flex items-center gap-2">
          {hasChanges && (
            <span className="text-sm text-orange-600 dark:text-orange-400">
              Unsaved changes
            </span>
          )}
          
          {!readOnly && (
            <button
              onClick={handleSave}
              disabled={!hasChanges || saving}
              className="px-3 py-1.5 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
            >
              {saving ? 'Saving...' : 'Save'}
            </button>
          )}
          
          {onClose && (
            <button
              onClick={onClose}
              className="p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mx-4 mt-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md">
          <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
        </div>
      )}

      {/* File Metadata */}
      {fileNode && (
        <div className="px-4 py-2 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
            <span>Size: {fileNode.size} bytes</span>
            {fileNode.lastModified && (
              <span>Modified: {new Date(fileNode.lastModified).toLocaleString()}</span>
            )}
            <span>Type: {fileNode.type}</span>
          </div>
        </div>
      )}

      {/* Editor */}
      <div className="flex-1 relative">
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          readOnly={readOnly}
          className="w-full h-full p-4 font-mono text-sm bg-transparent border-none resize-none focus:outline-none focus:ring-0 text-gray-900 dark:text-gray-100"
          placeholder={readOnly ? "File content will appear here..." : "Start typing..."}
          spellCheck={false}
          style={{
            minHeight: '400px',
            lineHeight: '1.5'
          }}
        />
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-50 dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
          <span>Language: {getFileLanguage(fileNode?.meta?.fileName)}</span>
          <span>Lines: {content.split('\n').length}</span>
          <span>Characters: {content.length}</span>
        </div>
        
        {fileNode?.meta?.isReadOnly && (
          <span className="text-xs text-orange-600 dark:text-orange-400">
            Read Only
          </span>
        )}
      </div>
    </div>
  )
}

export default CodeEditor
