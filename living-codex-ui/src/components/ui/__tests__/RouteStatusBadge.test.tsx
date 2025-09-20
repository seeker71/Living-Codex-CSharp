import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { RouteStatusBadge, RouteStatusIndicator, type RouteStatus } from '../RouteStatusBadge'

describe('RouteStatusBadge', () => {
  const statuses: RouteStatus[] = [
    'Stub', 'Simple', 'Simulated', 'Fallback', 'AiEnabled', 
    'ExternalInfo', 'Untested', 'PartiallyTested', 'FullyTested'
  ]

  describe('Component Rendering', () => {
    it('renders all status types without crashing', () => {
      statuses.forEach(status => {
        const { unmount } = render(<RouteStatusBadge status={status} />)
        
        // Check that the component renders without crashing
        expect(screen.getByRole('img')).toBeInTheDocument()
        
        unmount()
      })
    })

    it('renders with label by default', () => {
      render(<RouteStatusBadge status="Simple" />)
      
      expect(screen.getByText('Simple')).toBeInTheDocument()
      expect(screen.getByRole('img')).toBeInTheDocument()
    })

    it('hides label when showLabel is false', () => {
      render(<RouteStatusBadge status="Simple" showLabel={false} />)
      
      // Icon should be present
      expect(screen.getByRole('img')).toBeInTheDocument()
      
      // Label should not be present
      expect(screen.queryByText('Simple')).not.toBeInTheDocument()
    })

    it('renders badge with proper structure', () => {
      render(<RouteStatusBadge status="AiEnabled" />)
      
      expect(screen.getByText('AI-Enabled')).toBeInTheDocument()
      expect(screen.getByText('🤖')).toBeInTheDocument()
    })
  })

  describe('Status-Specific Behavior', () => {
    it('renders Stub status correctly', () => {
      render(<RouteStatusBadge status="Stub" />)
      
      expect(screen.getByText('🚧')).toBeInTheDocument()
      expect(screen.getByText('Stub')).toBeInTheDocument()
    })

    it('renders FullyTested status correctly', () => {
      render(<RouteStatusBadge status="FullyTested" />)
      
      expect(screen.getByText('✅')).toBeInTheDocument()
      expect(screen.getByText('Fully Tested')).toBeInTheDocument()
    })

    it('renders AiEnabled status correctly', () => {
      render(<RouteStatusBadge status="AiEnabled" />)
      
      expect(screen.getByText('🤖')).toBeInTheDocument()
      expect(screen.getByText('AI-Enabled')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('has proper ARIA labels', () => {
      render(<RouteStatusBadge status="Simple" />)
      
      const icon = screen.getByRole('img')
      expect(icon).toHaveAttribute('aria-label', 'Simple')
    })

    it('renders accessible content', () => {
      render(<RouteStatusBadge status="ExternalInfo" />)
      
      expect(screen.getByText('External')).toBeInTheDocument()
      expect(screen.getByRole('img')).toBeInTheDocument()
    })
  })
})

describe('RouteStatusIndicator', () => {
  it('renders status indicator correctly', () => {
    render(<RouteStatusIndicator status="FullyTested" />)
    
    expect(screen.getByText('Fully Tested')).toBeInTheDocument()
  })

  it('renders all status types without crashing', () => {
    const statuses: RouteStatus[] = [
      'Stub', 'Simple', 'Simulated', 'Fallback', 'AiEnabled', 
      'ExternalInfo', 'Untested', 'PartiallyTested', 'FullyTested'
    ]

    statuses.forEach(status => {
      const { unmount } = render(<RouteStatusIndicator status={status} />)
      
      expect(screen.getByText(getExpectedLabel(status))).toBeInTheDocument()
      
      unmount()
    })
  })
})

// Helper function to get expected label for status
function getExpectedLabel(status: RouteStatus): string {
  const labelMap = {
    'Stub': 'Stub',
    'Simple': 'Simple',
    'Simulated': 'Simulated', 
    'Fallback': 'Fallback',
    'AiEnabled': 'AI-Enabled',
    'ExternalInfo': 'External',
    'Untested': 'Untested',
    'PartiallyTested': 'Partial Tests',
    'FullyTested': 'Fully Tested'
  }
  return labelMap[status]
}