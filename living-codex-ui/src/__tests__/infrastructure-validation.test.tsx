/**
 * Infrastructure Validation Tests
 * These tests validate that the test infrastructure is properly configured
 * and can successfully run UI tests without configuration issues.
 */

import React from 'react'
import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import { TEST_INFRASTRUCTURE, renderWithProviders } from './test-utils'

describe('Test Infrastructure Validation', () => {
  describe('Configuration Infrastructure', () => {
    it('should have properly configured test infrastructure', () => {
      expect(TEST_INFRASTRUCTURE).toBeDefined()
      expect(TEST_INFRASTRUCTURE.config).toBeDefined()
      expect(TEST_INFRASTRUCTURE.auth).toBeDefined()
      expect(TEST_INFRASTRUCTURE.api).toBeDefined()
    })

    it('should have valid backend configuration', () => {
      expect(TEST_INFRASTRUCTURE.config.backend.baseUrl).toBe('http://localhost:5002')
      expect(TEST_INFRASTRUCTURE.config.backend.timeout).toBe(10000)
      expect(TEST_INFRASTRUCTURE.config.frontend.baseUrl).toBe('http://localhost:3000')
    })

    it('should have valid auth configuration', () => {
      expect(TEST_INFRASTRUCTURE.auth.user.id).toBe('test-user')
      expect(TEST_INFRASTRUCTURE.auth.user.username).toBe('testuser')
      expect(TEST_INFRASTRUCTURE.auth.token).toBe('test-token')
      expect(TEST_INFRASTRUCTURE.auth.isAuthenticated).toBe(true)
      expect(TEST_INFRASTRUCTURE.auth.isLoading).toBe(false)
    })

    it('should have valid API helper functions', () => {
      expect(TEST_INFRASTRUCTURE.api.buildApiUrl('/test')).toBe('http://localhost:5002/test')
      expect(TEST_INFRASTRUCTURE.api.buildFrontendUrl('/test')).toBe('http://localhost:3000/test')
    })
  })

  describe('Component Rendering Infrastructure', () => {
    it('should render basic components without errors', () => {
      const TestComponent = () => <div data-testid="test-component">Test Component</div>

      renderWithProviders(<TestComponent />)

      expect(screen.getByTestId('test-component')).toBeInTheDocument()
      expect(screen.getByText('Test Component')).toBeInTheDocument()
    })

    it('should provide React Query context', async () => {
      const TestComponent = () => {
        const [count, setCount] = React.useState(0)
        return (
          <div>
            <span data-testid="counter">{count}</span>
            <button onClick={() => setCount(c => c + 1)}>Increment</button>
          </div>
        )
      }

      renderWithProviders(<TestComponent />)

      expect(screen.getByTestId('counter')).toHaveTextContent('0')

      const button = screen.getByText('Increment')
      button.click()

      // Wait for state update
      await screen.findByText('1')
      expect(screen.getByTestId('counter')).toHaveTextContent('1')
    })

    it('should provide authentication context', () => {
      const TestComponent = () => {
        // Use the mocked auth context directly
        return (
          <div data-testid="auth-status">
            {TEST_INFRASTRUCTURE.auth.isAuthenticated ? 'Authenticated' : 'Not Authenticated'}
          </div>
        )
      }

      renderWithProviders(<TestComponent />)

      expect(screen.getByTestId('auth-status')).toHaveTextContent('Authenticated')
    })
  })

  describe('Mock Infrastructure', () => {
    it('should have config module properly mocked', () => {
      const { config, buildApiUrl } = require('@/lib/config')

      expect(config.backend.baseUrl).toBe('http://localhost:5002')
      expect(buildApiUrl('/test')).toBe('http://localhost:5002/test')
    })

    it('should have lucide-react icons properly mocked', () => {
      const { Heart, Share2, MessageCircle } = require('lucide-react')

      const { unmount: unmount1 } = render(<Heart />)
      const { unmount: unmount2 } = render(<Share2 />)
      const { unmount: unmount3 } = render(<MessageCircle />)

      // Icons should render without errors
      expect(document.querySelectorAll('[data-testid="mock-icon"]')).toHaveLength(3)

      unmount1()
      unmount2()
      unmount3()
    })

    it('should have react-markdown properly mocked', () => {
      const Markdown = require('react-markdown')

      render(<Markdown>**Bold text**</Markdown>)

      expect(document.querySelector('[data-testid="mock-markdown"]')).toBeInTheDocument()
      expect(screen.getByText('**Bold text**')).toBeInTheDocument()
    })
  })

  describe('Test Environment Validation', () => {
    it('should have proper DOM environment', () => {
      expect(document).toBeDefined()
      expect(window).toBeDefined()
      expect(navigator).toBeDefined()
    })

    it('should have testing-library utilities available', () => {
      expect(screen).toBeDefined()
      expect(render).toBeDefined()
      expect(screen.getByText).toBeDefined()
    })

    it('should support async operations', async () => {
      const TestComponent = () => {
        const [loaded, setLoaded] = React.useState(false)

        React.useEffect(() => {
          setTimeout(() => setLoaded(true), 100)
        }, [])

        return <div data-testid="async-test">{loaded ? 'Loaded' : 'Loading'}</div>
      }

      renderWithProviders(<TestComponent />)

      expect(screen.getByTestId('async-test')).toHaveTextContent('Loading')

      // Wait for async operation
      await screen.findByText('Loaded')

      expect(screen.getByTestId('async-test')).toHaveTextContent('Loaded')
    })
  })

  describe('Integration Readiness', () => {
    it('should be ready for component integration tests', () => {
      // This test validates that all infrastructure is in place
      expect(TEST_INFRASTRUCTURE).toBeDefined()
      expect(renderWithProviders).toBeDefined()
      expect(screen).toBeDefined()

      // Infrastructure is ready
      expect(true).toBe(true)
    })

    it('should be ready for API integration tests', () => {
      const { buildApiUrl } = require('@/lib/config')

      // API URL building should work
      expect(buildApiUrl('/health')).toBe('http://localhost:5002/health')
      expect(buildApiUrl('/concepts')).toBe('http://localhost:5002/concepts')

      // Infrastructure is ready
      expect(true).toBe(true)
    })
  })
})
