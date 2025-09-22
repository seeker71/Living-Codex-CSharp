'use client'

import React, { useState, useEffect } from 'react'
import { MagnifyingGlassIcon, FunnelIcon, ArrowPathIcon } from '@heroicons/react/24/outline'
import NodeCard, { Node, Edge } from './NodeCard'

export interface NodeBrowserProps {
  initialTypeFilter?: string
  initialStateFilter?: string
  showFilters?: boolean
  showSearch?: boolean
  pageSize?: number
  onNodeSelect?: (node: Node) => void
  onEdgeSelect?: (edge: Edge) => void
}

const stateOptions = [
  { value: '', label: 'All States' },
  { value: 'Ice', label: '🧊 Ice (Persistent)' },
  { value: 'Water', label: '💧 Water (Cached)' },
  { value: 'Gas', label: '💨 Gas (Transient)' }
]

export default function NodeBrowser({
  initialTypeFilter = '',
  initialStateFilter = '',
  showFilters = true,
  showSearch = true,
  pageSize = 20,
  onNodeSelect,
  onEdgeSelect
}: NodeBrowserProps) {
  const [nodes, setNodes] = useState<Node[]>([])
  const [filteredNodes, setFilteredNodes] = useState<Node[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  
  // Filter states
  const [searchTerm, setSearchTerm] = useState('')
  const [typeFilter, setTypeFilter] = useState(initialTypeFilter)
  const [stateFilter, setStateFilter] = useState(initialStateFilter)
  const [currentPage, setCurrentPage] = useState(1)
  
  // Available types (populated from loaded nodes)
  const [availableTypes, setAvailableTypes] = useState<string[]>([])

  useEffect(() => {
    loadNodes()
  }, [])

  useEffect(() => {
    applyFilters()
  }, [nodes, searchTerm, typeFilter, stateFilter])

  const loadNodes = async () => {
    setLoading(true)
    setError(null)
    
    try {
      const response = await fetch('http://localhost:5002/storage-endpoints/nodes')
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
      
      const data = await response.json()
      if (data.success && data.nodes) {
        setNodes(data.nodes)
        
        // Extract unique types
        const types = [...new Set(data.nodes.map((node: Node) => node.typeId))]
          .filter(Boolean)
          .sort()
        setAvailableTypes(types)
      } else {
        throw new Error(data.error || 'Failed to load nodes')
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred'
      setError(errorMessage)
      console.error('Error loading nodes:', err)
    } finally {
      setLoading(false)
    }
  }

  const applyFilters = () => {
    let filtered = nodes

    // Apply search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      filtered = filtered.filter(node =>
        node.id.toLowerCase().includes(term) ||
        node.title?.toLowerCase().includes(term) ||
        node.description?.toLowerCase().includes(term) ||
        node.typeId.toLowerCase().includes(term) ||
        (node.meta && Object.values(node.meta).some(value => 
          String(value).toLowerCase().includes(term)
        ))
      )
    }

    // Apply type filter
    if (typeFilter) {
      filtered = filtered.filter(node => node.typeId === typeFilter)
    }

    // Apply state filter
    if (stateFilter) {
      filtered = filtered.filter(node => node.state === stateFilter)
    }

    setFilteredNodes(filtered)
    setCurrentPage(1) // Reset to first page when filters change
  }

  const handleNodeClick = (nodeId: string) => {
    const node = nodes.find(n => n.id === nodeId)
    if (node && onNodeSelect) {
      onNodeSelect(node)
    }
  }

  const handleEdgeClick = (edge: Edge) => {
    if (onEdgeSelect) {
      onEdgeSelect(edge)
    }
  }

  const clearFilters = () => {
    setSearchTerm('')
    setTypeFilter('')
    setStateFilter('')
  }

  // Pagination
  const totalPages = Math.ceil(filteredNodes.length / pageSize)
  const startIndex = (currentPage - 1) * pageSize
  const endIndex = startIndex + pageSize
  const currentNodes = filteredNodes.slice(startIndex, endIndex)

  const goToPage = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)))
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <p className="text-medium-contrast">Loading nodes...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-red-800 dark:text-red-200 font-medium">Error Loading Nodes</h3>
            <p className="text-red-600 dark:text-red-400 text-sm mt-1">{error}</p>
          </div>
          <button
            onClick={loadNodes}
            className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg flex items-center gap-2"
          >
            <ArrowPathIcon className="w-4 h-4" />
            Retry
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-high-contrast">Node Browser</h2>
          <p className="text-medium-contrast text-sm mt-1">
            {filteredNodes.length} of {nodes.length} nodes
            {searchTerm || typeFilter || stateFilter ? ' (filtered)' : ''}
          </p>
        </div>
        <button
          onClick={loadNodes}
          disabled={loading}
          className="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white px-4 py-2 rounded-lg flex items-center gap-2"
        >
          <ArrowPathIcon className="w-4 h-4" />
          Refresh
        </button>
      </div>

      {/* Filters */}
      {showFilters && (
        <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-4">
          <div className="flex items-center gap-4 mb-4">
            <FunnelIcon className="w-5 h-5 text-medium-contrast" />
            <span className="font-medium text-high-contrast">Filters</span>
            {(searchTerm || typeFilter || stateFilter) && (
              <button
                onClick={clearFilters}
                className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
              >
                Clear all
              </button>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Search */}
            {showSearch && (
              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Search
                </label>
                <div className="relative">
                  <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <input
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    placeholder="Search nodes..."
                    className="input-standard pl-10 w-full"
                  />
                </div>
              </div>
            )}

            {/* Type Filter */}
            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Type
              </label>
              <select
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
                className="input-standard w-full"
              >
                <option value="">All Types</option>
                {availableTypes.map(type => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>

            {/* State Filter */}
            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                State
              </label>
              <select
                value={stateFilter}
                onChange={(e) => setStateFilter(e.target.value)}
                className="input-standard w-full"
              >
                {stateOptions.map(option => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
      )}

      {/* Results */}
      {filteredNodes.length === 0 ? (
        <div className="text-center py-12">
          <div className="text-gray-400 text-6xl mb-4">🔍</div>
          <h3 className="text-lg font-medium text-medium-contrast mb-2">No nodes found</h3>
          <p className="text-medium-contrast">
            {searchTerm || typeFilter || stateFilter
              ? 'Try adjusting your filters to see more results.'
              : 'No nodes are available in the system.'}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Node Cards */}
          <div className="space-y-4">
            {currentNodes.map(node => (
              <NodeCard
                key={node.id}
                node={node}
                showContent={true}
                showEdges={true}
                showMeta={true}
                onNodeClick={handleNodeClick}
                onEdgeClick={handleEdgeClick}
              />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between py-4">
              <div className="text-sm text-medium-contrast">
                Showing {startIndex + 1}-{Math.min(endIndex, filteredNodes.length)} of {filteredNodes.length} nodes
              </div>
              
              <div className="flex items-center gap-2">
                <button
                  onClick={() => goToPage(currentPage - 1)}
                  disabled={currentPage === 1}
                  className="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-800"
                >
                  Previous
                </button>
                
                <div className="flex items-center gap-1">
                  {Array.from({ length: Math.min(7, totalPages) }, (_, i) => {
                    let page
                    if (totalPages <= 7) {
                      page = i + 1
                    } else if (currentPage <= 4) {
                      page = i + 1
                    } else if (currentPage >= totalPages - 3) {
                      page = totalPages - 6 + i
                    } else {
                      page = currentPage - 3 + i
                    }
                    
                    return (
                      <button
                        key={page}
                        onClick={() => goToPage(page)}
                        className={`px-3 py-2 text-sm rounded-lg ${
                          currentPage === page
                            ? 'bg-blue-600 text-white'
                            : 'border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800'
                        }`}
                      >
                        {page}
                      </button>
                    )
                  })}
                </div>
                
                <button
                  onClick={() => goToPage(currentPage + 1)}
                  disabled={currentPage === totalPages}
                  className="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-800"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}



