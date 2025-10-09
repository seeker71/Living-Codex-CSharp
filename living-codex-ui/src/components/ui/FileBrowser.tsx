'use client'

import React, { useState, useEffect } from 'react'

interface FileNode {
  id: string
  name: string
  type: string
  relativePath: string
  absolutePath: string
  size: number
  lastModified: string
}

interface FileBrowserProps {
  onFileSelect?: (nodeId: string, fileNode: FileNode) => void
  selectedNodeId?: string
  className?: string
  initialFileNodes?: FileNode[]
}

function FileBrowser({ 
  onFileSelect, 
  selectedNodeId,
  className = '',
  initialFileNodes = []
}: FileBrowserProps) {
  const [fileNodes, setFileNodes] = useState<FileNode[]>(initialFileNodes)
  const [loading, setLoading] = useState(initialFileNodes.length === 0)
  const [error, setError] = useState<string | null>(null)

  const loadFileNodes = async () => {
    try {
      setLoading(true)
      setError(null)
      
      const response = await fetch('http://localhost:5002/filesystem/files?limit=1000', {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        mode: 'cors'
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }

      const data = await response.json()
      
      if (data && data.success && Array.isArray(data.files)) {
        setFileNodes(data.files)
      } else {
        setError('Invalid data format received from server')
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Failed to load files')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (initialFileNodes.length === 0) {
      loadFileNodes()
    }
  }, [initialFileNodes.length])

  // Build tree structure from file nodes
  const buildTreeStructure = (nodes: FileNode[]) => {
    const tree: any = {}
    
    nodes.forEach(node => {
      const pathParts = node.relativePath.split('/')
      let current = tree
      
      pathParts.forEach((part, index) => {
        if (!current[part]) {
          current[part] = {
            name: part,
            type: index === pathParts.length - 1 ? 'file' : 'folder',
            children: {},
            node: index === pathParts.length - 1 ? node : null
          }
        }
        current = current[part].children
      })
    })
    
    return tree
  }

  // Convert tree to array for rendering with proper sorting
  const treeToArray = (tree: any, level = 0): any[] => {
    return Object.entries(tree)
      .sort(([nameA, itemA], [nameB, itemB]) => {
        // Sort folders first, then files
        if ((itemA as any).type === 'folder' && (itemB as any).type !== 'folder') return -1
        if ((itemA as any).type !== 'folder' && (itemB as any).type === 'folder') return 1
        
        // Within same type, sort alphabetically (case-insensitive)
        return nameA.toLowerCase().localeCompare(nameB.toLowerCase())
      })
      .map(([name, item]: [string, any]) => ({
        name,
        type: item.type,
        node: item.node,
        level,
        children: Object.keys(item.children).length > 0 ? treeToArray(item.children, level + 1) : []
      }))
  }

  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(new Set(['src', 'living-codex-ui']))
  
  const toggleFolder = (folderPath: string) => {
    const newExpanded = new Set(expandedFolders)
    if (newExpanded.has(folderPath)) {
      newExpanded.delete(folderPath)
    } else {
      newExpanded.add(folderPath)
    }
    setExpandedFolders(newExpanded)
  }

  const renderTreeItem = (item: any, path = ''): React.ReactNode => {
    const currentPath = path ? `${path}/${item.name}` : item.name
    const isExpanded = expandedFolders.has(currentPath)
    const isFile = item.type === 'file'
    const hasChildren = item.children && item.children.length > 0

    return (
      <div key={currentPath}>
        <div 
          className={`flex items-center cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 px-2 py-1 text-sm ${
            selectedNodeId === item.node?.id 
              ? 'bg-blue-100 dark:bg-blue-900 text-blue-900 dark:text-blue-100' 
              : 'text-gray-700 dark:text-gray-300'
          }`}
          style={{ paddingLeft: `${item.level * 16 + 8}px` }}
          onClick={() => {
            if (isFile && item.node) {
              onFileSelect?.(item.node.id, item.node)
            } else if (!isFile) {
              toggleFolder(currentPath)
            }
          }}
        >
          {!isFile && hasChildren && (
            <span className="mr-1 text-gray-500">
              {isExpanded ? '📂' : '📁'}
            </span>
          )}
          {!isFile && !hasChildren && (
            <span className="mr-1 text-gray-400">📁</span>
          )}
          {isFile && (
            <span className="mr-1 text-gray-500">
              {item.node?.name.endsWith('.tsx') ? '⚛️' :
               item.node?.name.endsWith('.ts') ? '🔷' :
               item.node?.name.endsWith('.js') ? '🟨' :
               item.node?.name.endsWith('.cs') ? '🔷' :
               item.node?.name.endsWith('.json') ? '📋' :
               item.node?.name.endsWith('.md') ? '📝' :
               item.node?.name.endsWith('.css') ? '🎨' :
               '📄'}
            </span>
          )}
          <span className="truncate">{item.name}</span>
        </div>
        
        {!isFile && hasChildren && isExpanded && (
          <div>
            {item.children.map((child: any) => renderTreeItem(child, currentPath))}
          </div>
        )}
      </div>
    )
  }

  // Count files and folders
  const fileCount = fileNodes.filter(n => n.type && n.type.startsWith('codex.file')).length
  const folderCount = new Set(fileNodes.map(n => n.relativePath.split('/')[0])).size
  const tree = buildTreeStructure(fileNodes)
  const treeArray = treeToArray(tree)

  return (
    <div className={`flex flex-col h-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex-shrink-0 p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium text-gray-900 dark:text-gray-100">Files</h3>
          <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
            <span>{fileCount} files</span>
            <span>•</span>
            <span>{folderCount} folders</span>
          </div>
        </div>
      </div>
      
      <div className="flex-1 overflow-y-auto">
        {loading && (
          <div className="p-4 text-center">
            <div className="text-sm text-gray-500 dark:text-gray-400">Loading files...</div>
          </div>
        )}
        
        {error && (
          <div className="p-4 text-center">
            <div className="text-sm text-red-500 dark:text-red-400">{error}</div>
            <button 
              onClick={loadFileNodes}
              className="mt-2 px-3 py-1 bg-blue-500 text-white text-xs rounded hover:bg-blue-600"
            >
              Retry
            </button>
          </div>
        )}
        
        {!loading && !error && fileNodes.length === 0 && (
          <div className="p-4 text-center">
            <div className="text-sm text-gray-500 dark:text-gray-400">No files found</div>
            <button 
              onClick={loadFileNodes}
              className="mt-2 px-2 py-1 bg-blue-500 text-white text-xs rounded"
            >
              Refresh
            </button>
          </div>
        )}
        
        {!loading && !error && fileNodes.length > 0 && (
          <div className="py-1">
            {treeArray.map((item) => renderTreeItem(item))}
          </div>
        )}
      </div>
    </div>
  )
}

export default FileBrowser