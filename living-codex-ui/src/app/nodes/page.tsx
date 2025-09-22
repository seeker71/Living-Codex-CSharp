'use client'

import React, { useState } from 'react'
import NodeBrowser from '@/components/ui/NodeBrowser'
import { Node, Edge } from '@/components/ui/NodeCard'

export default function NodesPage() {
  const [selectedNode, setSelectedNode] = useState<Node | null>(null)
  const [selectedEdge, setSelectedEdge] = useState<Edge | null>(null)

  const handleNodeSelect = (node: Node) => {
    setSelectedNode(node)
    setSelectedEdge(null) // Clear edge selection when node is selected
  }

  const handleEdgeSelect = (edge: Edge) => {
    setSelectedEdge(edge)
  }

  return (
    <div className="min-h-screen bg-page text-foreground">
      <div className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto">
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-high-contrast mb-4">System Nodes</h1>
            <p className="text-medium-contrast max-w-2xl">
              Explore all nodes in the system. Each node contains content, metadata, and relationships to other nodes. 
              Use the filters to find specific types of nodes or search for particular content.
            </p>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-4 gap-8">
            {/* Main browser */}
            <div className="xl:col-span-3">
              <NodeBrowser
                showFilters={true}
                showSearch={true}
                pageSize={10}
                onNodeSelect={handleNodeSelect}
                onEdgeSelect={handleEdgeSelect}
              />
            </div>

            {/* Sidebar with selection details */}
            <div className="xl:col-span-1">
              <div className="sticky top-8 space-y-6">
                {/* Selected Node */}
                {selectedNode && (
                  <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                    <h3 className="text-lg font-semibold text-high-contrast mb-4">Selected Node</h3>
                    <div className="space-y-3">
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">ID:</span>
                        <p className="text-sm font-mono text-high-contrast break-all">{selectedNode.id}</p>
                      </div>
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">Type:</span>
                        <p className="text-sm text-high-contrast">{selectedNode.typeId}</p>
                      </div>
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">State:</span>
                        <p className="text-sm text-high-contrast">{selectedNode.state}</p>
                      </div>
                      {selectedNode.title && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Title:</span>
                          <p className="text-sm text-high-contrast">{selectedNode.title}</p>
                        </div>
                      )}
                      {selectedNode.description && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Description:</span>
                          <p className="text-sm text-high-contrast">{selectedNode.description}</p>
                        </div>
                      )}
                      {selectedNode.locale && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Locale:</span>
                          <p className="text-sm text-high-contrast">{selectedNode.locale}</p>
                        </div>
                      )}
                      {selectedNode.content && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Content Type:</span>
                          <p className="text-sm text-high-contrast">
                            {selectedNode.content.mediaType || 
                             (selectedNode.content.inlineJson ? 'JSON' : 
                              selectedNode.content.inlineBytes ? 'Binary' :
                              selectedNode.content.externalUri ? 'External' : 'Unknown')}
                          </p>
                        </div>
                      )}
                      {selectedNode.meta && Object.keys(selectedNode.meta).length > 0 && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Metadata:</span>
                          <p className="text-xs text-medium-contrast">
                            {Object.keys(selectedNode.meta).length} properties
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Selected Edge */}
                {selectedEdge && (
                  <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                    <h3 className="text-lg font-semibold text-high-contrast mb-4">Selected Relationship</h3>
                    <div className="space-y-3">
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">From:</span>
                        <p className="text-sm font-mono text-high-contrast break-all">{selectedEdge.fromId}</p>
                      </div>
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">To:</span>
                        <p className="text-sm font-mono text-high-contrast break-all">{selectedEdge.toId}</p>
                      </div>
                      <div>
                        <span className="text-sm font-medium text-medium-contrast">Role:</span>
                        <p className="text-sm text-high-contrast">{selectedEdge.role}</p>
                      </div>
                      {selectedEdge.weight !== undefined && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Weight:</span>
                          <p className="text-sm text-high-contrast">{selectedEdge.weight}</p>
                        </div>
                      )}
                      {selectedEdge.meta && Object.keys(selectedEdge.meta).length > 0 && (
                        <div>
                          <span className="text-sm font-medium text-medium-contrast">Edge Metadata:</span>
                          <div className="mt-2 bg-gray-50 dark:bg-gray-800 rounded p-2">
                            <pre className="text-xs text-high-contrast font-mono whitespace-pre-wrap">
                              {JSON.stringify(selectedEdge.meta, null, 2)}
                            </pre>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Help */}
                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-blue-800 dark:text-blue-200 mb-3">
                    💡 Tips
                  </h3>
                  <ul className="text-sm text-blue-700 dark:text-blue-300 space-y-2">
                    <li>• Click node titles to select them</li>
                    <li>• Expand nodes to see content and relationships</li>
                    <li>• Click relationships to see edge details</li>
                    <li>• Use filters to narrow down results</li>
                    <li>• Search across all node properties</li>
                  </ul>
                </div>

                {/* Node States Info */}
                <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-high-contrast mb-3">Node States</h3>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">🧊</span>
                      <div>
                        <p className="text-sm font-medium text-high-contrast">Ice</p>
                        <p className="text-xs text-medium-contrast">Persistent, immutable</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-lg">💧</span>
                      <div>
                        <p className="text-sm font-medium text-high-contrast">Water</p>
                        <p className="text-xs text-medium-contrast">Cached, mutable</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-lg">💨</span>
                      <div>
                        <p className="text-sm font-medium text-high-contrast">Gas</p>
                        <p className="text-xs text-medium-contrast">Transient, computed</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}



