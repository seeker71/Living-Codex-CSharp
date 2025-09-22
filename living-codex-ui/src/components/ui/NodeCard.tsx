'use client'

import React, { useState, useEffect } from 'react'
import { ChevronDownIcon, ChevronRightIcon, LinkIcon, ExternalLinkIcon, EyeIcon, CodeBracketIcon } from '@heroicons/react/24/outline'

export interface Node {
  id: string
  typeId: string
  state: 'Ice' | 'Water' | 'Gas'
  locale?: string
  title?: string
  description?: string
  content?: ContentRef
  meta?: Record<string, any>
}

export interface ContentRef {
  mediaType?: string
  inlineJson?: string
  inlineBytes?: string
  externalUri?: string
  selector?: string
  query?: string
  headers?: Record<string, string>
  authRef?: string
  cacheKey?: string
}

export interface Edge {
  fromId: string
  toId: string
  role: string
  weight?: number
  meta?: Record<string, any>
}

export interface NodeCardProps {
  node: Node
  showContent?: boolean
  showEdges?: boolean
  showMeta?: boolean
  maxContentHeight?: number
  onNodeClick?: (nodeId: string) => void
  onEdgeClick?: (edge: Edge) => void
}

const stateColors = {
  Ice: 'bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100',
  Water: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-800 dark:text-cyan-100', 
  Gas: 'bg-purple-100 text-purple-800 dark:bg-purple-800 dark:text-purple-100'
}

const stateIcons = {
  Ice: '🧊',
  Water: '💧',
  Gas: '💨'
}

export default function NodeCard({
  node,
  showContent = true,
  showEdges = true,
  showMeta = false,
  maxContentHeight = 300,
  onNodeClick,
  onEdgeClick
}: NodeCardProps) {
  const [expanded, setExpanded] = useState(false)
  const [contentExpanded, setContentExpanded] = useState(false)
  const [metaExpanded, setMetaExpanded] = useState(false)
  const [edgesExpanded, setEdgesExpanded] = useState(false)
  const [outgoingEdges, setOutgoingEdges] = useState<Edge[]>([])
  const [incomingEdges, setIncomingEdges] = useState<Edge[]>([])
  const [relatedNodes, setRelatedNodes] = useState<Record<string, Node>>({})
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (showEdges && expanded) {
      loadEdges()
    }
  }, [node.id, expanded, showEdges])

  const loadEdges = async () => {
    setLoading(true)
    try {
      // Load outgoing edges
      const outgoingResponse = await fetch(`http://localhost:5002/storage-endpoints/edges/from/${node.id}`)
      if (outgoingResponse.ok) {
        const outgoingData = await outgoingResponse.json()
        setOutgoingEdges(outgoingData.edges || [])
      }

      // Load incoming edges  
      const incomingResponse = await fetch(`http://localhost:5002/storage-endpoints/edges/to/${node.id}`)
      if (incomingResponse.ok) {
        const incomingData = await incomingResponse.json()
        setIncomingEdges(incomingData.edges || [])
      }

      // Load related nodes
      const allEdges = [...(outgoingResponse.ok ? (await outgoingResponse.json()).edges || [] : []), 
                      ...(incomingResponse.ok ? (await incomingResponse.json()).edges || [] : [])]
      const nodeIds = new Set<string>()
      
      allEdges.forEach((edge: Edge) => {
        if (edge.fromId !== node.id) nodeIds.add(edge.fromId)
        if (edge.toId !== node.id) nodeIds.add(edge.toId)
      })

      const nodePromises = Array.from(nodeIds).map(async (nodeId) => {
        try {
          const response = await fetch(`http://localhost:5002/storage-endpoints/nodes/${nodeId}`)
          if (response.ok) {
            const data = await response.json()
            return { id: nodeId, node: data.node }
          }
        } catch (error) {
          console.error(`Error loading node ${nodeId}:`, error)
        }
        return null
      })

      const nodeResults = await Promise.all(nodePromises)
      const nodesMap: Record<string, Node> = {}
      nodeResults.forEach(result => {
        if (result) {
          nodesMap[result.id] = result.node
        }
      })
      setRelatedNodes(nodesMap)

    } catch (error) {
      console.error('Error loading edges:', error)
    } finally {
      setLoading(false)
    }
  }

  const renderContent = () => {
    if (!node.content) return null

    const { mediaType, inlineJson, inlineBytes, externalUri } = node.content

    if (inlineJson) {
      try {
        const parsed = JSON.parse(inlineJson)
        return (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-medium-contrast">JSON Content</span>
              <button
                onClick={() => setContentExpanded(!contentExpanded)}
                className="text-blue-600 dark:text-blue-400 hover:underline text-sm flex items-center gap-1"
              >
                {contentExpanded ? <ChevronDownIcon className="w-4 h-4" /> : <ChevronRightIcon className="w-4 h-4" />}
                {contentExpanded ? 'Collapse' : 'Expand'}
              </button>
            </div>
            <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg p-3 font-mono text-sm overflow-auto ${
              contentExpanded ? '' : `max-h-[${maxContentHeight}px]`
            }`}>
              <pre className="whitespace-pre-wrap">
                {JSON.stringify(parsed, null, 2)}
              </pre>
            </div>
          </div>
        )
      } catch (error) {
        return (
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3">
            <span className="text-red-600 dark:text-red-400 text-sm">Invalid JSON content</span>
          </div>
        )
      }
    }

    if (inlineBytes) {
      return (
        <div className="space-y-2">
          <span className="text-sm font-medium text-medium-contrast">Binary Content</span>
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
            <span className="text-gray-600 dark:text-gray-400 text-sm">
              Binary data ({inlineBytes.length} bytes)
            </span>
          </div>
        </div>
      )
    }

    if (externalUri) {
      return (
        <div className="space-y-2">
          <span className="text-sm font-medium text-medium-contrast">External Content</span>
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
            <a
              href={externalUri}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-2"
            >
              <ExternalLinkIcon className="w-4 h-4" />
              {externalUri}
            </a>
            {mediaType && (
              <div className="mt-2 text-sm text-medium-contrast">
                Type: {mediaType}
              </div>
            )}
          </div>
        </div>
      )
    }

    return (
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
        <span className="text-gray-600 dark:text-gray-400 text-sm">No content available</span>
      </div>
    )
  }

  const renderEdges = () => {
    if (!showEdges || (!outgoingEdges.length && !incomingEdges.length)) return null

    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-medium-contrast">Relationships</span>
          <button
            onClick={() => setEdgesExpanded(!edgesExpanded)}
            className="text-blue-600 dark:text-blue-400 hover:underline text-sm flex items-center gap-1"
          >
            {edgesExpanded ? <ChevronDownIcon className="w-4 h-4" /> : <ChevronRightIcon className="w-4 h-4" />}
            {edgesExpanded ? 'Collapse' : `Show ${outgoingEdges.length + incomingEdges.length} relationships`}
          </button>
        </div>

        {edgesExpanded && (
          <div className="space-y-3">
            {outgoingEdges.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-high-contrast mb-2">Outgoing ({outgoingEdges.length})</h4>
                <div className="space-y-2">
                  {outgoingEdges.map((edge, idx) => (
                    <div
                      key={idx}
                      className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3 cursor-pointer hover:bg-green-100 dark:hover:bg-green-900/40"
                      onClick={() => onEdgeClick?.(edge)}
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <LinkIcon className="w-4 h-4 text-green-600 dark:text-green-400" />
                          <span className="font-medium text-green-800 dark:text-green-200">{edge.role}</span>
                          {edge.weight && (
                            <span className="text-xs bg-green-200 dark:bg-green-700 text-green-800 dark:text-green-200 px-2 py-1 rounded">
                              {edge.weight}
                            </span>
                          )}
                        </div>
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            onNodeClick?.(edge.toId)
                          }}
                          className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
                        >
                          {relatedNodes[edge.toId]?.title || edge.toId}
                        </button>
                      </div>
                      {relatedNodes[edge.toId] && (
                        <div className="mt-2 text-sm text-medium-contrast">
                          {relatedNodes[edge.toId].description}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {incomingEdges.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-high-contrast mb-2">Incoming ({incomingEdges.length})</h4>
                <div className="space-y-2">
                  {incomingEdges.map((edge, idx) => (
                    <div
                      key={idx}
                      className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 cursor-pointer hover:bg-blue-100 dark:hover:bg-blue-900/40"
                      onClick={() => onEdgeClick?.(edge)}
                    >
                      <div className="flex items-center justify-between">
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            onNodeClick?.(edge.fromId)
                          }}
                          className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
                        >
                          {relatedNodes[edge.fromId]?.title || edge.fromId}
                        </button>
                        <div className="flex items-center gap-2">
                          <span className="font-medium text-blue-800 dark:text-blue-200">{edge.role}</span>
                          {edge.weight && (
                            <span className="text-xs bg-blue-200 dark:bg-blue-700 text-blue-800 dark:text-blue-200 px-2 py-1 rounded">
                              {edge.weight}
                            </span>
                          )}
                          <LinkIcon className="w-4 h-4 text-blue-600 dark:text-blue-400 rotate-180" />
                        </div>
                      </div>
                      {relatedNodes[edge.fromId] && (
                        <div className="mt-2 text-sm text-medium-contrast">
                          {relatedNodes[edge.fromId].description}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    )
  }

  const renderMeta = () => {
    if (!showMeta || !node.meta || Object.keys(node.meta).length === 0) return null

    return (
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-medium-contrast">Metadata</span>
          <button
            onClick={() => setMetaExpanded(!metaExpanded)}
            className="text-blue-600 dark:text-blue-400 hover:underline text-sm flex items-center gap-1"
          >
            {metaExpanded ? <ChevronDownIcon className="w-4 h-4" /> : <ChevronRightIcon className="w-4 h-4" />}
            {metaExpanded ? 'Collapse' : `Show ${Object.keys(node.meta).length} properties`}
          </button>
        </div>

        {metaExpanded && (
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
            <div className="space-y-2">
              {Object.entries(node.meta).map(([key, value]) => (
                <div key={key} className="flex justify-between items-start">
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400 min-w-0 flex-1 mr-2">
                    {key}:
                  </span>
                  <span className="text-sm text-high-contrast font-mono break-all">
                    {typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    )
  }

  return (
    <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm hover:shadow-md transition-shadow">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${stateColors[node.state]}`}>
                <span role="img" aria-label={node.state}>
                  {stateIcons[node.state]}
                </span>
                {node.state}
              </span>
              <span className="text-xs text-medium-contrast bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                {node.typeId}
              </span>
            </div>
            <h3 
              className="text-lg font-semibold text-high-contrast cursor-pointer hover:text-blue-600 dark:hover:text-blue-400"
              onClick={() => onNodeClick?.(node.id)}
            >
              {node.title || node.id}
            </h3>
            {node.description && (
              <p className="text-sm text-medium-contrast mt-1 line-clamp-2">
                {node.description}
              </p>
            )}
            <div className="mt-2 text-xs text-low-contrast font-mono">
              ID: {node.id}
            </div>
          </div>
          <button
            onClick={() => setExpanded(!expanded)}
            className="ml-4 p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            {expanded ? (
              <ChevronDownIcon className="w-5 h-5" />
            ) : (
              <ChevronRightIcon className="w-5 h-5" />
            )}
          </button>
        </div>
      </div>

      {/* Expanded Content */}
      {expanded && (
        <div className="p-4 space-y-6">
          {loading && (
            <div className="flex items-center justify-center py-4">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500"></div>
              <span className="ml-2 text-sm text-medium-contrast">Loading relationships...</span>
            </div>
          )}

          {showContent && renderContent()}
          {renderEdges()}
          {renderMeta()}
        </div>
      )}
    </div>
  )
}



