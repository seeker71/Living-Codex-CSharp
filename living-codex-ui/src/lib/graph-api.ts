/**
 * Graph API Client
 * Provides access to the Living Codex graph backend
 * Core principle: Everything is a Node
 */

import config from './config';

export interface Node {
  id: string;
  typeId: string;
  state: 'ice' | 'water' | 'gas';
  locale: string;
  title: string;
  description: string;
  content: {
    mediaType: string;
    inlineJson?: string;
    url?: string;
  };
  meta: Record<string, any>;
}

export interface Edge {
  id: string;
  fromId: string;
  toId: string;
  typeId: string;
  role: string;
  meta: Record<string, any>;
  createdAt: string;
}

export interface GraphQueryRequest {
  startNodeId?: string;
  typeIds?: string[];
  maxDepth?: number;
  maxNodes?: number;
  includeEdges?: boolean;
}

export interface GraphQueryResponse {
  nodes: Node[];
  edges: Edge[];
  totalNodes: number;
  totalEdges: number;
}

export interface NodeStats {
  totalNodes: number;
  byType: Record<string, number>;
  byState: Record<string, number>;
}

/**
 * Get all nodes (paginated)
 */
export async function getNodes(limit = 100, offset = 0): Promise<Node[]> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/nodes?limit=${limit}&offset=${offset}`
  );
  
  if (!response.ok) {
    throw new Error(`Failed to fetch nodes: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get a specific node by ID
 */
export async function getNode(id: string): Promise<Node> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/nodes/${encodeURIComponent(id)}`
  );
  
  if (!response.ok) {
    throw new Error(`Failed to fetch node ${id}: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get edges from a node
 */
export async function getEdgesFrom(nodeId: string): Promise<Edge[]> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/edges/from/${encodeURIComponent(nodeId)}`
  );
  
  if (!response.ok) {
    throw new Error(`Failed to fetch edges from ${nodeId}: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get edges to a node
 */
export async function getEdgesTo(nodeId: string): Promise<Edge[]> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/edges/to/${encodeURIComponent(nodeId)}`
  );
  
  if (!response.ok) {
    throw new Error(`Failed to fetch edges to ${nodeId}: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Query graph with filters
 */
export async function queryGraph(query: GraphQueryRequest): Promise<GraphQueryResponse> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/graph/query`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(query),
    }
  );
  
  if (!response.ok) {
    throw new Error(`Failed to query graph: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get node statistics
 */
export async function getNodeStats(): Promise<NodeStats> {
  const nodes = await getNodes(10000); // Get large sample
  
  const stats: NodeStats = {
    totalNodes: nodes.length,
    byType: {},
    byState: {},
  };
  
  nodes.forEach(node => {
    stats.byType[node.typeId] = (stats.byType[node.typeId] || 0) + 1;
    stats.byState[node.state] = (stats.byState[node.state] || 0) + 1;
  });
  
  return stats;
}

/**
 * Search nodes by title or description
 */
export async function searchNodes(query: string, limit = 50): Promise<Node[]> {
  const nodes = await getNodes(1000); // Get sample
  const lowerQuery = query.toLowerCase();
  
  return nodes
    .filter(node => 
      node.title.toLowerCase().includes(lowerQuery) ||
      node.description.toLowerCase().includes(lowerQuery)
    )
    .slice(0, limit);
}

/**
 * Get nodes by type
 */
export async function getNodesByType(typeId: string, limit = 100): Promise<Node[]> {
  const nodes = await getNodes(1000);
  
  return nodes
    .filter(node => node.typeId === typeId)
    .slice(0, limit);
}

