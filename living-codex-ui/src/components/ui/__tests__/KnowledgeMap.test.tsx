import React from 'react'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import { renderWithProviders } from '../../../__tests__/test-utils'
import { KnowledgeMap, useMockKnowledgeNodes } from '../KnowledgeMap'

// Mock canvas context
const mockCanvasContext = {
  clearRect: jest.fn(),
  fillRect: jest.fn(),
  strokeRect: jest.fn(),
  beginPath: jest.fn(),
  arc: jest.fn(),
  fill: jest.fn(),
  stroke: jest.fn(),
  moveTo: jest.fn(),
  lineTo: jest.fn(),
  createRadialGradient: jest.fn(() => ({
    addColorStop: jest.fn()
  })),
  fillText: jest.fn(),
  strokeStyle: '',
  fillStyle: '',
  lineWidth: 0,
  font: '',
  textAlign: '',
  textBaseline: ''
}

describe('KnowledgeMap', () => {
  beforeEach(() => {
    jest.clearAllMocks()

    // Mock canvas getContext
    HTMLCanvasElement.prototype.getContext = jest.fn(() => mockCanvasContext)
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Rendering', () => {
    it('renders canvas element', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      expect(screen.getByTestId('knowledge-map')).toBeInTheDocument()
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })

    it('shows legend with domain colors', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      expect(screen.getByTestId('map-legend')).toBeInTheDocument()
      expect(screen.getByText('Science & Technology')).toBeInTheDocument()
    })

    it('displays map statistics', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: ['node-2'],
          size: 1,
          resonance: 80
        },
        {
          id: 'node-2',
          title: 'Machine Learning',
          domain: 'Science & Tech',
          x: 0.6,
          y: 0.6,
          connections: ['node-1'],
          size: 1,
          resonance: 75
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      expect(screen.getByTestId('map-stats')).toBeInTheDocument()
      expect(screen.getByText('Nodes: 2')).toBeInTheDocument()
      expect(screen.getByText('Connections: 1')).toBeInTheDocument()
    })

    it('shows empty state when no nodes', () => {
      renderWithProviders(<KnowledgeMap nodes={[]} />)

      expect(screen.getByText('No nodes to display')).toBeInTheDocument()
      expect(screen.getByText('🕸️')).toBeInTheDocument()
    })
  })

  describe('Canvas Drawing', () => {
    it('draws nodes with correct colors and positions', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Canvas should be drawn (context methods called)
      expect(mockCanvasContext.clearRect).toHaveBeenCalled()
      expect(mockCanvasContext.arc).toHaveBeenCalled()
      expect(mockCanvasContext.fill).toHaveBeenCalled()
    })

    it('draws connections between nodes', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: ['node-2'],
          size: 1,
          resonance: 80
        },
        {
          id: 'node-2',
          title: 'Machine Learning',
          domain: 'Science & Tech',
          x: 0.6,
          y: 0.6,
          connections: ['node-1'],
          size: 1,
          resonance: 75
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should draw lines between connected nodes
      expect(mockCanvasContext.moveTo).toHaveBeenCalled()
      expect(mockCanvasContext.lineTo).toHaveBeenCalled()
      expect(mockCanvasContext.stroke).toHaveBeenCalled()
    })

    it('applies glow effects for high resonance nodes', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 90 // High resonance
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should create radial gradient for glow effect
      expect(mockCanvasContext.createRadialGradient).toHaveBeenCalled()
    })

    it('highlights selected nodes', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} selectedNodeId="node-1" />)

      // Should apply selection styling (stroke)
      expect(mockCanvasContext.stroke).toHaveBeenCalled()
    })
  })

  describe('User Interactions', () => {
    it('handles canvas clicks on nodes', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      const onNodeClick = jest.fn()

      renderWithProviders(
        <KnowledgeMap nodes={mockNodes} onNodeClick={onNodeClick} />
      )

      const canvas = screen.getByTestId('map-canvas')

      // Simulate click on node position
      fireEvent.click(canvas, {
        clientX: 300, // Mock position where node should be
        clientY: 400
      })

      // Should call onNodeClick with node ID
      expect(onNodeClick).toHaveBeenCalledWith('node-1')
    })

    it('does not trigger click for empty canvas areas', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      const onNodeClick = jest.fn()

      renderWithProviders(
        <KnowledgeMap nodes={mockNodes} onNodeClick={onNodeClick} />
      )

      const canvas = screen.getByTestId('map-canvas')

      // Click on empty area
      fireEvent.click(canvas, {
        clientX: 100, // Mock position outside node
        clientY: 100
      })

      // Should not call onNodeClick
      expect(onNodeClick).not.toHaveBeenCalled()
    })

    it('handles mouse events for interaction', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      const canvas = screen.getByTestId('map-canvas')

      // Test mouse events
      fireEvent.mouseDown(canvas)
      fireEvent.mouseMove(canvas)
      fireEvent.mouseUp(canvas)
      fireEvent.mouseLeave(canvas)

      // Should handle mouse events without errors
      expect(canvas).toBeInTheDocument()
    })
  })

  describe('Responsive Design', () => {
    it('adapts to different container sizes', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      // Mock different container widths
      const container = document.createElement('div')
      container.style.width = '600px'
      container.style.height = '400px'

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />, {
        container
      })

      // Should adapt to container size
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })

    it('handles window resize events', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Simulate window resize
      fireEvent(window, new Event('resize'))

      // Should handle resize without errors
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })
  })

  describe('Data Processing', () => {
    it('processes node data correctly', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: ['node-2'],
          size: 1,
          resonance: 80
        },
        {
          id: 'node-2',
          title: 'Machine Learning',
          domain: 'Science & Tech',
          x: 0.6,
          y: 0.6,
          connections: ['node-1'],
          size: 1,
          resonance: 75
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should process and display both nodes
      expect(screen.getByText('Nodes: 2')).toBeInTheDocument()
      expect(screen.getByText('Connections: 1')).toBeInTheDocument()
    })

    it('handles duplicate connections correctly', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.3,
          y: 0.4,
          connections: ['node-2', 'node-2'], // Duplicate
          size: 1,
          resonance: 80
        },
        {
          id: 'node-2',
          title: 'Machine Learning',
          domain: 'Science & Tech',
          x: 0.6,
          y: 0.6,
          connections: ['node-1'],
          size: 1,
          resonance: 75
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should handle duplicates without errors
      expect(screen.getByText('Nodes: 2')).toBeInTheDocument()
    })

    it('validates node positions within bounds', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: -0.1, // Out of bounds
          y: 1.5,  // Out of bounds
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should handle out-of-bounds positions gracefully
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })
  })

  describe('Performance', () => {
    it('handles large number of nodes efficiently', () => {
      const largeNodeSet = Array.from({ length: 50 }, (_, i) => ({
        id: `node-${i}`,
        title: `Concept ${i}`,
        domain: 'Science & Tech',
        x: Math.random(),
        y: Math.random(),
        connections: [],
        size: 1,
        resonance: Math.floor(Math.random() * 100)
      }))

      renderWithProviders(<KnowledgeMap nodes={largeNodeSet} />)

      // Should render without performance issues
      expect(screen.getByText('Nodes: 50')).toBeInTheDocument()
    })

    it('optimizes canvas drawing', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should clear canvas once per render
      expect(mockCanvasContext.clearRect).toHaveBeenCalledTimes(1)
    })

    it('handles rapid prop changes', () => {
      const { rerender } = renderWithProviders(<KnowledgeMap nodes={[]} />)

      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      // Rapid prop changes
      rerender(<KnowledgeMap nodes={mockNodes} />)
      rerender(<KnowledgeMap nodes={mockNodes} selectedNodeId="node-1" />)
      rerender(<KnowledgeMap nodes={mockNodes} selectedNodeId={undefined} />)

      // Should handle rapid changes without errors
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })
  })

  describe('useMockKnowledgeNodes Hook', () => {
    it('generates mock nodes with correct structure', () => {
      const mockNodes = useMockKnowledgeNodes(5)

      expect(mockNodes).toHaveLength(5)
      expect(mockNodes[0]).toHaveProperty('id')
      expect(mockNodes[0]).toHaveProperty('title')
      expect(mockNodes[0]).toHaveProperty('domain')
      expect(mockNodes[0]).toHaveProperty('x')
      expect(mockNodes[0]).toHaveProperty('y')
      expect(mockNodes[0]).toHaveProperty('connections')
      expect(mockNodes[0]).toHaveProperty('size')
      expect(mockNodes[0]).toHaveProperty('resonance')
    })

    it('limits node count when exceeding available concepts', () => {
      const mockNodes = useMockKnowledgeNodes(50)

      // Should be limited to available concepts (around 20)
      expect(mockNodes.length).toBeLessThanOrEqual(20)
    })

    it('generates consistent data structure', () => {
      const mockNodes1 = useMockKnowledgeNodes(3)
      const mockNodes2 = useMockKnowledgeNodes(3)

      // Should have same structure
      expect(mockNodes1[0]).toHaveProperty('id')
      expect(mockNodes2[0]).toHaveProperty('id')
    })
  })

  describe('Error Handling', () => {
    it('handles canvas context creation failure', () => {
      // Mock getContext to return null
      HTMLCanvasElement.prototype.getContext = jest.fn(() => null)

      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should handle gracefully without crashing
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })

    it('handles invalid node data', () => {
      const invalidNodes = [
        {
          id: 'node-1',
          // Missing required properties
          title: 'Quantum Physics'
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={invalidNodes as any} />)

      // Should handle invalid data gracefully
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })

    it('handles canvas drawing errors', () => {
      // Mock canvas methods to throw errors
      mockCanvasContext.arc = jest.fn(() => {
        throw new Error('Canvas error')
      })

      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should handle canvas errors gracefully
      expect(screen.getByTestId('map-canvas')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('provides alternative text for screen readers', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should have accessible elements
      expect(screen.getByText('Legend')).toBeInTheDocument()
      expect(screen.getByText('Stats')).toBeInTheDocument()
    })

    it('supports keyboard navigation', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      const canvas = screen.getByTestId('map-canvas')

      // Should be focusable
      canvas.focus()
      expect(document.activeElement).toBe(canvas)
    })

    it('provides meaningful content for assistive technologies', () => {
      const mockNodes = [
        {
          id: 'node-1',
          title: 'Quantum Physics',
          domain: 'Science & Tech',
          x: 0.5,
          y: 0.5,
          connections: [],
          size: 1,
          resonance: 80
        }
      ]

      renderWithProviders(<KnowledgeMap nodes={mockNodes} />)

      // Should provide meaningful descriptions
      expect(screen.getByText('Click nodes to explore • Lines show connections')).toBeInTheDocument()
    })
  })
})
