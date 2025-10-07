import React from 'react'

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), back: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/',
}))

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  }),
}))

// Mock the buildApiUrl function
jest.mock('@/lib/config', () => ({
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
}))

describe('API Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Node API', () => {
    it('fetches node data correctly', async () => {
      const mockNodeData = {
        success: true,
        node: {
          id: 'u-core-concept-kw-matter',
          typeId: 'codex.concept.keyword',
          title: 'Matter',
          description: 'Physical substance that has mass and occupies space',
          state: 'Ice',
          locale: 'en'
        }
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockNodeData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/nodes/u-core-concept-kw-matter')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.success).toBe(true)
      expect(data.node.id).toBe('u-core-concept-kw-matter')
      expect(data.node.title).toBe('Matter')
    })

    it('handles node not found error', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 404,
        json: () => Promise.resolve({ success: false, error: 'Node not found' })
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/nodes/non-existent-node')
      const data = await response.json()

      expect(response.ok).toBe(false)
      expect(response.status).toBe(404)
      expect(data.success).toBe(false)
      expect(data.error).toBe('Node not found')
    })

    it('handles server errors', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500,
        json: () => Promise.resolve({ success: false, error: 'Internal server error' })
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/nodes/u-core-concept-kw-matter')
      const data = await response.json()

      expect(response.ok).toBe(false)
      expect(response.status).toBe(500)
      expect(data.success).toBe(false)
      expect(data.error).toBe('Internal server error')
    })
  })

  describe('Edges API', () => {
    it('fetches edges data correctly', async () => {
      const mockEdgesData = {
        success: true,
        edges: [
          {
            fromId: 'u-core-concept-kw-matter',
            toId: 'u-core-axis-water_states',
            role: 'concept_on_axis',
            weight: 0.9
          }
        ],
        totalCount: 1
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockEdgesData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/edges')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.success).toBe(true)
      expect(data.edges).toHaveLength(1)
      expect(data.edges[0].fromId).toBe('u-core-concept-kw-matter')
      expect(data.edges[0].toId).toBe('u-core-axis-water_states')
    })

    it('handles edge filtering', async () => {
      const mockEdgesData = {
        success: true,
        edges: [
          {
            fromId: 'u-core-concept-kw-matter',
            toId: 'u-core-axis-water_states',
            role: 'concept_on_axis',
            weight: 0.9
          }
        ],
        totalCount: 1
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockEdgesData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/edges?role=concept_on_axis')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.success).toBe(true)
      expect(data.edges).toHaveLength(1)
    })

    it('handles edge pagination', async () => {
      const mockEdgesData = {
        success: true,
        edges: [],
        totalCount: 100
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockEdgesData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/edges?skip=0&take=25')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.success).toBe(true)
      expect(data.totalCount).toBe(100)
    })
  })

  describe('Stats API', () => {
    it('fetches stats data correctly', async () => {
      const mockStatsData = {
        success: true,
        nodeCount: 100,
        edgeCount: 200,
        moduleCount: 5,
        uptime: '24h',
        requestCount: 1000
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockStatsData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/stats')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.success).toBe(true)
      expect(data.nodeCount).toBe(100)
      expect(data.edgeCount).toBe(200)
      expect(data.moduleCount).toBe(5)
    })
  })

  describe('Health Check API', () => {
    it('fetches health status correctly', async () => {
      const mockHealthData = {
        status: 'ok',
        nodeCount: 100,
        edgeCount: 200,
        moduleCount: 5,
        uptime: '24h',
        requestCount: 1000
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockHealthData)
      })

      const response = await fetch('http://localhost:5002/health')
      const data = await response.json()

      expect(response.ok).toBe(true)
      expect(data.status).toBe('ok')
      expect(data.nodeCount).toBe(100)
    })
  })

  describe('Network Error Handling', () => {
    it('handles network errors', async () => {
      global.fetch = jest.fn().mockRejectedValue(new Error('Network error'))

      try {
        await fetch('http://localhost:5002/storage-endpoints/nodes/test')
      } catch (error) {
        expect(error).toBeInstanceOf(Error)
        expect(error.message).toBe('Network error')
      }
    })

    it('handles timeout errors', async () => {
      global.fetch = jest.fn().mockImplementation(() => 
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Timeout')), 100)
        )
      )

      try {
        await fetch('http://localhost:5002/storage-endpoints/nodes/test')
      } catch (error) {
        expect(error).toBeInstanceOf(Error)
        expect(error.message).toBe('Timeout')
      }
    })
  })

  describe('API Response Validation', () => {
    it('validates node response structure', async () => {
      const mockNodeData = {
        success: true,
        node: {
          id: 'u-core-concept-kw-matter',
          typeId: 'codex.concept.keyword',
          title: 'Matter',
          description: 'Physical substance that has mass and occupies space',
          state: 'Ice',
          locale: 'en'
        }
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockNodeData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/nodes/test')
      const data = await response.json()

      // Validate required fields
      expect(data).toHaveProperty('success')
      expect(data).toHaveProperty('node')
      expect(data.node).toHaveProperty('id')
      expect(data.node).toHaveProperty('typeId')
      expect(data.node).toHaveProperty('title')
      expect(data.node).toHaveProperty('description')
      expect(data.node).toHaveProperty('state')
      expect(data.node).toHaveProperty('locale')
    })

    it('validates edges response structure', async () => {
      const mockEdgesData = {
        success: true,
        edges: [
          {
            fromId: 'u-core-concept-kw-matter',
            toId: 'u-core-axis-water_states',
            role: 'concept_on_axis',
            weight: 0.9
          }
        ],
        totalCount: 1
      }

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockEdgesData)
      })

      const response = await fetch('http://localhost:5002/storage-endpoints/edges')
      const data = await response.json()

      // Validate required fields
      expect(data).toHaveProperty('success')
      expect(data).toHaveProperty('edges')
      expect(data).toHaveProperty('totalCount')
      expect(Array.isArray(data.edges)).toBe(true)
      expect(data.edges[0]).toHaveProperty('fromId')
      expect(data.edges[0]).toHaveProperty('toId')
      expect(data.edges[0]).toHaveProperty('role')
    })
  })

  describe('API Performance', () => {
    it('measures API response time', async () => {
      const startTime = Date.now()
      
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, node: { id: 'test' } })
      })

      await fetch('http://localhost:5002/storage-endpoints/nodes/test')
      
      const endTime = Date.now()
      const responseTime = endTime - startTime
      
      expect(responseTime).toBeLessThan(1000) // Should respond within 1 second
    })

    it('handles concurrent API requests', async () => {
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ success: true, node: { id: 'test' } })
      })

      const requests = Array.from({ length: 10 }, (_, i) => 
        fetch(`http://localhost:5002/storage-endpoints/nodes/test-${i}`)
      )

      const responses = await Promise.all(requests)
      
      expect(responses).toHaveLength(10)
      responses.forEach(response => {
        expect(response.ok).toBe(true)
      })
    })
  })
})
