/**
 * Spatial Graph API - Hooks and utilities for SpatialGraphModule
 */

import { useQuery } from '@tanstack/react-query';
import { api } from './api';

// Types matching SpatialGraphModule backend
export interface ViewportNode {
  id: string;
  typeId: string;
  state: string;
  title: string;
  description: string;
  x: number;
  y: number;
  size: number;
  connectionCount: number;
}

export interface NodeCluster {
  id: string;
  typeId: string;
  state: string;
  nodeCount: number;
  centerX: number;
  centerY: number;
  title: string;
  memberIds: string[];
}

export interface ViewportEdge {
  id: string;
  fromId: string;
  toId: string;
  role: string;
  weight: number | null;
}

export interface SpatialGraphResponse {
  success: boolean;
  viewportNodes: ViewportNode[];
  clusters: NodeCluster[];
  edges: ViewportEdge[];
  zoomLevel: number;
  totalNodesInGraph: number;
  viewportNodeCount: number;
  clusterCount: number;
  message: string;
}

export interface ViewportQueryRequest {
  zoomFactor: number;
  centerX: number;
  centerY: number;
  viewportWidth: number;
  viewportHeight: number;
  focusNodeId?: string | null;
  typeFilter?: string[] | null;
}

export interface NodeDto {
  id: string;
  typeId: string;
  state: string;
  locale: string;
  title: string;
  description: string;
  meta: Record<string, any>;
}

export interface EdgeDto {
  fromId: string;
  toId: string;
  role: string;
  weight: number | null;
  meta: Record<string, any>;
}

export interface DrillDownResponse {
  success: boolean;
  centerNode: NodeDto;
  nodes: NodeDto[];
  edges: EdgeDto[];
  outgoingCount: number;
  incomingCount: number;
  message: string;
}

export interface ClusterMembersResponse {
  success: boolean;
  clusterId: string;
  members: NodeDto[];
  count: number;
  message: string;
}

export interface GraphStatsResponse {
  success: boolean;
  totalNodes: number;
  totalEdges: number;
  byType: Record<string, number>;
  byState: Record<string, number>;
  averageConnections: number;
  message: string;
}

/**
 * Hook to fetch viewport graph data
 */
export function useViewportGraph(request: ViewportQueryRequest, enabled = true) {
  return useQuery({
    queryKey: ['viewport-graph', request],
    queryFn: async () => {
      const response = await api.post('/graph/viewport', request);
      if (response.success) {
        return response as SpatialGraphResponse;
      }
      throw new Error('Failed to load viewport graph');
    },
    enabled,
    staleTime: 30000, // 30 seconds
  });
}

/**
 * Hook to fetch drilldown data for a specific node
 */
export function useDrillDown(nodeId: string | null, depth = 2) {
  return useQuery({
    queryKey: ['drilldown', nodeId, depth],
    queryFn: async () => {
      if (!nodeId) return null;
      const response = await api.get(`/graph/drilldown/${nodeId}?depth=${depth}`);
      if (response.success) {
        return response as DrillDownResponse;
      }
      throw new Error('Failed to load drilldown data');
    },
    enabled: !!nodeId,
    staleTime: 60000, // 1 minute
  });
}

/**
 * Hook to fetch cluster members
 */
export function useClusterMembers(clusterId: string | null) {
  return useQuery({
    queryKey: ['cluster-members', clusterId],
    queryFn: async () => {
      if (!clusterId) return null;
      const response = await api.get(`/graph/cluster/${clusterId}`);
      if (response.success) {
        return response as ClusterMembersResponse;
      }
      throw new Error('Failed to load cluster members');
    },
    enabled: !!clusterId,
    staleTime: 60000, // 1 minute
  });
}

/**
 * Hook to fetch graph statistics
 */
export function useGraphStats() {
  return useQuery({
    queryKey: ['graph-stats'],
    queryFn: async () => {
      const response = await api.get('/graph/stats');
      if (response.success) {
        return response as GraphStatsResponse;
      }
      throw new Error('Failed to load graph stats');
    },
    staleTime: 120000, // 2 minutes
  });
}

/**
 * Utility: Calculate zoom level from zoom factor
 */
export function calculateZoomLevel(zoomFactor: number): 0 | 1 | 2 {
  if (zoomFactor < 0.3) return 0; // Galaxy
  if (zoomFactor < 1.0) return 1; // System
  return 2; // Detail
}

/**
 * Utility: Get zoom level name
 */
export function getZoomLevelName(level: 0 | 1 | 2): string {
  switch (level) {
    case 0: return 'Galaxy View';
    case 1: return 'System View';
    case 2: return 'Detail View';
  }
}

