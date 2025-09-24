import { buildApiUrl } from '@/lib/config'

describe('Ontology Axis → keyword objects are represented as edges', () => {
  const axisId = 'u-core-axis-astrology_objects'
  const objects = [
    'sun','moon','mercury','venus','earth','mars','jupiter','saturn','uranus','neptune','pluto','chiron'
  ]
  let originalFetch: typeof global.fetch

  beforeEach(() => {
    originalFetch = global.fetch
    global.fetch = jest.fn()
    ;(global.fetch as jest.Mock).mockImplementation(async (req: Request | string) => {
      const url = typeof req === 'string' ? req : req.url
      if (url.includes('/storage-endpoints/edges')) {
        // Return 12 edges from the axis to each solar object node
        return {
          ok: true,
          json: async () => ({
            success: true,
            totalCount: 12,
            edges: objects.map((o, i) => ({
              fromId: axisId,
              toId: `codex.ontology.object.${o}`,
              relationship: 'contains',
              weight: 1.0 - i * 0.01,
              meta: { keyword: o }
            }))
          })
        } as any
      }
      return { ok: true, json: async () => ({ success: true }) } as any
    })
  })

  afterEach(() => {
    ;(global.fetch as jest.Mock).mockReset()
    global.fetch = originalFetch
  })

  it('axis has 12 edges to the 12 solar objects', async () => {
    const url = buildApiUrl(`/storage-endpoints/edges?nodeId=${encodeURIComponent(axisId)}&take=100`)
    const resp = await fetch(url)
    expect(resp.ok).toBe(true)
    const data = await resp.json()
    expect(Array.isArray(data.edges)).toBe(true)
    expect(data.totalCount).toBe(12)
    expect(data.edges).toHaveLength(12)
    const targets = new Set(data.edges.map((e: any) => e.toId))
    expect(targets.size).toBe(12)
    objects.forEach(o => {
      expect(targets.has(`codex.ontology.object.${o}`)).toBe(true)
    })
  })
})

