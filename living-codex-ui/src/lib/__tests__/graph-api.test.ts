/**
 * Graph API Client Tests
 */

import { 
  getNodes, 
  getNode, 
  getEdgesFrom, 
  getEdgesTo, 
  queryGraph, 
  getNodeStats, 
  searchNodes, 
  getNodesByType 
} from '../graph-api';

// Mock config
jest.mock('../config', () => ({
  config: {
    backend: {
      baseUrl: 'http://localhost:5002'
    }
  }
}));

// Mock fetch
global.fetch = jest.fn();

describe('Graph API Client', () => {
  beforeEach(() => {
    (fetch as jest.Mock).mockClear();
  });

  describe('getNodes', () => {
    it('should fetch nodes with default pagination', async () => {
      const mockNodes = [
        { id: 'node1', typeId: 'codex.concept', state: 'ice', title: 'Test Node' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      const result = await getNodes();
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/nodes?limit=100&offset=0'
      );
      expect(result).toEqual(mockNodes);
    });

    it('should fetch nodes with custom pagination', async () => {
      const mockNodes = [{ id: 'node1' }];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      await getNodes(50, 25);
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/nodes?limit=50&offset=25'
      );
    });

    it('should throw error on failed request', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        statusText: 'Not Found'
      });

      await expect(getNodes()).rejects.toThrow('Failed to fetch nodes: Not Found');
    });
  });

  describe('getNode', () => {
    it('should fetch a specific node by ID', async () => {
      const mockNode = { id: 'node1', title: 'Test Node' };
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNode
      });

      const result = await getNode('node1');
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/nodes/node1'
      );
      expect(result).toEqual(mockNode);
    });

    it('should encode node ID properly', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({})
      });

      await getNode('node with spaces');
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/nodes/node%20with%20spaces'
      );
    });
  });

  describe('getEdgesFrom', () => {
    it('should fetch edges from a node', async () => {
      const mockEdges = [
        { id: 'edge1', fromId: 'node1', toId: 'node2', typeId: 'connects' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockEdges
      });

      const result = await getEdgesFrom('node1');
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/edges/from/node1'
      );
      expect(result).toEqual(mockEdges);
    });
  });

  describe('getEdgesTo', () => {
    it('should fetch edges to a node', async () => {
      const mockEdges = [
        { id: 'edge1', fromId: 'node2', toId: 'node1', typeId: 'connects' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockEdges
      });

      const result = await getEdgesTo('node1');
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/edges/to/node1'
      );
      expect(result).toEqual(mockEdges);
    });
  });

  describe('queryGraph', () => {
    it('should query graph with filters', async () => {
      const mockResponse = {
        nodes: [{ id: 'node1' }],
        edges: [{ id: 'edge1' }],
        totalNodes: 1,
        totalEdges: 1
      };
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse
      });

      const query = {
        startNodeId: 'node1',
        typeIds: ['codex.concept'],
        maxDepth: 2,
        maxNodes: 100,
        includeEdges: true
      };

      const result = await queryGraph(query);
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/graph/query',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(query)
        }
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getNodeStats', () => {
    it('should calculate node statistics', async () => {
      const mockNodes = [
        { typeId: 'codex.concept', state: 'ice' },
        { typeId: 'codex.concept', state: 'water' },
        { typeId: 'codex.meta/type', state: 'ice' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      const result = await getNodeStats();
      
      expect(result).toEqual({
        totalNodes: 3,
        byType: {
          'codex.concept': 2,
          'codex.meta/type': 1
        },
        byState: {
          'ice': 2,
          'water': 1
        }
      });
    });
  });

  describe('searchNodes', () => {
    it('should search nodes by title and description', async () => {
      const mockNodes = [
        { id: 'node1', title: 'Test Concept', description: 'A test concept' },
        { id: 'node2', title: 'Another Test', description: 'Another test concept' },
        { id: 'node3', title: 'Different', description: 'Not matching' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      const result = await searchNodes('test');
      
      expect(result).toHaveLength(2);
      expect(result[0].title).toBe('Test Concept');
      expect(result[1].title).toBe('Another Test');
    });

    it('should limit search results', async () => {
      const mockNodes = Array(100).fill(null).map((_, i) => ({
        id: `node${i}`,
        title: `Test ${i}`,
        description: `Test description ${i}`
      }));
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      const result = await searchNodes('test', 10);
      
      expect(result).toHaveLength(10);
    });
  });

  describe('getNodesByType', () => {
    it('should filter nodes by type', async () => {
      const mockNodes = [
        { id: 'node1', typeId: 'codex.concept' },
        { id: 'node2', typeId: 'codex.concept' },
        { id: 'node3', typeId: 'codex.meta/type' }
      ];
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockNodes
      });

      const result = await getNodesByType('codex.concept');
      
      expect(result).toHaveLength(2);
      expect(result.every(node => node.typeId === 'codex.concept')).toBe(true);
    });
  });
});
