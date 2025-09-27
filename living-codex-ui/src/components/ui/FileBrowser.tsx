'use client'

import React, { useState, useEffect } from 'react'
import { RouteStatusBadge, type RouteStatus } from './RouteStatusBadge'
import { buildApiUrl } from '@/lib/config'

interface FileNode {
  id: string
  name: string
  type: string
  relativePath: string
  absolutePath: string
  size: number
  lastModified: string
  meta?: {
    fileName?: string
    relativePath?: string
    absolutePath?: string
    size?: number
    lastModified?: string
    isReadOnly?: boolean
  }
  contentRef: {
    mediaType?: string
    externalUri?: string
    hasInlineContent?: boolean
  }
}

type RawFileNode = {
  id: string
  name?: string
  title?: string
  type?: string
  typeId?: string
  relativePath?: string
  absolutePath?: string
  size?: number
  lastModified?: string
  meta?: {
    fileName?: string
    relativePath?: string
    absolutePath?: string
    size?: number
    lastModified?: string
    isReadOnly?: boolean
  }
  content?: {
    mediaType?: string
    externalUri?: string
    inlineJson?: unknown
    inlineBytes?: unknown
  }
  contentRef?: {
    mediaType?: string
    externalUri?: string
    hasInlineContent?: boolean
  }
}

const normalizeFileNode = (node: RawFileNode): FileNode => {
  const meta = node.meta ?? {}
  const fallbackName = node.name || node.title || meta.fileName || node.id
  const relativePath = node.relativePath || meta.relativePath || fallbackName || ''
  const absolutePath = node.absolutePath || meta.absolutePath || relativePath

  return {
    id: node.id,
    name: fallbackName || '',
    type: node.type || node.typeId || 'file',
    relativePath,
    absolutePath,
    size: typeof node.size === 'number' ? node.size : meta.size ?? 0,
    lastModified: node.lastModified || meta.lastModified || new Date().toISOString(),
    meta: {
      ...meta,
      fileName: meta.fileName || fallbackName,
      relativePath,
      absolutePath,
      size: meta.size ?? node.size ?? 0,
      lastModified: meta.lastModified || node.lastModified,
    },
    contentRef: {
      mediaType: node.contentRef?.mediaType ?? node.content?.mediaType,
      externalUri: node.contentRef?.externalUri ?? node.content?.externalUri,
      hasInlineContent:
        node.contentRef?.hasInlineContent ??
        Boolean(node.content?.inlineJson || node.content?.inlineBytes),
    },
  }
}

interface TreeNode {
  name: string
  path: string
  isDirectory: boolean
  children?: TreeNode[]
  fileNode?: FileNode
}

interface FileBrowserProps {
  onFileSelect?: (nodeId: string, fileNode: FileNode) => void
  selectedNodeId?: string
  className?: string
}

export function FileBrowser({ 
  onFileSelect, 
  selectedNodeId,
  className = '' 
}: FileBrowserProps) {
  const [fileNodes, setFileNodes] = useState<FileNode[]>([])
  const [treeStructure, setTreeStructure] = useState<TreeNode[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [expandedPaths, setExpandedPaths] = useState<Set<string>>(new Set())
  const [status, setStatus] = useState<RouteStatus>('Untested')

  useEffect(() => {
    loadFileNodes()
  }, [])

  const loadFileNodes = async () => {
    setLoading(true)
    setError(null)
    
    try {
      const response = await fetch(buildApiUrl('/filesystem/files?limit=1000'))
      if (!response.ok) {
        throw new Error(`Failed to load files: ${response.statusText}`)
      }
      
      const data = await response.json()
      const rawNodes: RawFileNode[] = Array.isArray(data)
        ? data
        : data.files || data.nodes || []

      const normalizedNodes = rawNodes.map(normalizeFileNode)
      setFileNodes(normalizedNodes)
      setTreeStructure(buildTreeStructure(normalizedNodes))
      setStatus('FullyTested')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load files')
      setStatus('Untested')
    } finally {
      setLoading(false)
    }
  }

  const buildTreeStructure = (nodes: FileNode[]): TreeNode[] => {
    const tree: TreeNode[] = []
    const pathMap = new Map<string, TreeNode>()

    // First pass: create all directory nodes
    nodes.forEach(node => {
      const relativePath = node.relativePath || node.meta?.relativePath || node.name || ''
      if (!relativePath) return // Skip nodes without valid paths
      const pathParts = relativePath.split('/').filter(part => part.length > 0)
      
      let currentPath = ''
      pathParts.forEach((part, index) => {
        const parentPath = currentPath
        currentPath = currentPath ? `${currentPath}/${part}` : part
        
        if (!pathMap.has(currentPath)) {
          const isFile = index === pathParts.length - 1
          const treeNode: TreeNode = {
            name: part,
            path: currentPath,
            isDirectory: !isFile,
            children: isFile ? undefined : [],
            fileNode: isFile ? node : undefined
          }
          
          pathMap.set(currentPath, treeNode)
          
          if (parentPath) {
            const parent = pathMap.get(parentPath)
            if (parent && parent.children) {
              parent.children.push(treeNode)
            }
          } else {
            tree.push(treeNode)
          }
        } else if (index === pathParts.length - 1) {
          // Update existing node with file data
          const existing = pathMap.get(currentPath)!
          existing.fileNode = node
        }
      })
    })

    // Sort tree structure
    const sortTree = (nodes: TreeNode[]) => {
      nodes.sort((a, b) => {
        // Directories first, then files
        if (a.isDirectory !== b.isDirectory) {
          return a.isDirectory ? -1 : 1
        }
        return a.name.localeCompare(b.name)
      })
      
      nodes.forEach(node => {
        if (node.children) {
          sortTree(node.children)
        }
      })
    }
    
    sortTree(tree)
    return tree
  }

  const toggleExpanded = (path: string) => {
    const newExpanded = new Set(expandedPaths)
    if (newExpanded.has(path)) {
      newExpanded.delete(path)
    } else {
      newExpanded.add(path)
    }
    setExpandedPaths(newExpanded)
  }

  const handleFileClick = (node: TreeNode) => {
    if (node.isDirectory) {
      toggleExpanded(node.path)
    } else if (node.fileNode && onFileSelect) {
      onFileSelect(node.fileNode.id, node.fileNode)
    }
  }

  const getFileIcon = (fileName: string): string => {
    if (!fileName) return '📄' // Default icon for files without names
    const ext = fileName.split('.').pop()?.toLowerCase()
    
    switch (ext) {
      case 'cs': return '🔷'
      case 'js': case 'jsx': return '📄'
      case 'ts': case 'tsx': return '🔷'
      case 'json': return '📋'
      case 'md': return '📝'
      case 'html': return '🌐'
      case 'css': return '🎨'
      case 'xml': return '📄'
      case 'yaml': case 'yml': return '📋'
      case 'txt': return '📄'
      case 'log': return '📜'
      default: return '📄'
    }
  }

  const renderTreeNode = (node: TreeNode, depth: number = 0): React.ReactNode => {
    const isSelected = node.fileNode?.id === selectedNodeId
    const isExpanded = expandedPaths.has(node.path)
    
    return (
      <div key={node.path}>
        <div
          className={`
            flex items-center gap-2 px-2 py-1 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 rounded
            ${isSelected ? 'bg-blue-50 dark:bg-blue-900/20 border-l-2 border-blue-500' : ''}
            ${node.isDirectory ? 'font-medium' : ''}
          `}
          style={{ paddingLeft: `${depth * 16 + 8}px` }}
          onClick={() => handleFileClick(node)}
        >
          {node.isDirectory ? (
            <>
              <span className="text-sm">
                {isExpanded ? '📂' : '📁'}
              </span>
              <span className="text-gray-700 dark:text-gray-300">
                {node.name}
              </span>
            </>
          ) : (
            <>
              <span className="text-sm">
                {getFileIcon(node.name)}
              </span>
              <span className={`text-gray-600 dark:text-gray-400 ${isSelected ? 'font-medium' : ''}`}>
                {node.name}
              </span>
              {node.fileNode?.meta?.isReadOnly && (
                <span className="text-xs text-gray-400">🔒</span>
              )}
              {node.fileNode?.contentRef?.hasInlineContent === false && (
                <span className="text-xs text-blue-500">🔗</span>
              )}
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  window.open(`/node/${encodeURIComponent(node.fileNode!.id)}`, '_blank');
                }}
                className="ml-2 text-xs text-blue-600 hover:text-blue-800 hover:underline"
                title="View node details"
              >
                View Node
              </button>
            </>
          )}
        </div>
        
        {node.isDirectory && isExpanded && node.children && (
          <div>
            {node.children.map(child => renderTreeNode(child, depth + 1))}
          </div>
        )}
      </div>
    )
  }

  if (loading) {
    return (
      <div className={`flex items-center justify-center h-64 bg-gray-50 dark:bg-gray-800 rounded-lg ${className}`}>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-gray-600 dark:text-gray-300">Loading files...</p>
        </div>
      </div>
    )
  }

  return (
    <div className={`flex flex-col h-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-3">
          <h3 className="font-medium text-gray-900 dark:text-gray-100">
            Project Files
          </h3>
          <RouteStatusBadge status={status} size="sm" />
        </div>
        
        <button
          onClick={loadFileNodes}
          className="p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
          title="Refresh"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mx-4 mt-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md">
          <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
        </div>
      )}

      {/* File Tree */}
      <div className="flex-1 overflow-y-auto p-2">
        {treeStructure.length === 0 ? (
          <div className="flex items-center justify-center h-32">
            <p className="text-gray-500 dark:text-gray-400">No files found</p>
          </div>
        ) : (
          <div className="space-y-1">
            {treeStructure.map(node => renderTreeNode(node))}
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-50 dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700">
        <div className="text-xs text-gray-500 dark:text-gray-400">
          {fileNodes.length} files
        </div>
        
        <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
          <span>📁 Directory</span>
          <span>📄 File</span>
          <span>🔒 Read-only</span>
        </div>
      </div>
    </div>
  )
}

export default FileBrowser
