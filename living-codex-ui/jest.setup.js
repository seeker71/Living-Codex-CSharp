import '@testing-library/jest-dom'
import React from 'react'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      replace: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      prefetch: jest.fn(),
    }
  },
  useSearchParams() {
    return new URLSearchParams()
  },
  usePathname() {
    return '/'
  },
}))

// Do not mock fetch; tests should use real backend endpoints

// Mock markdown rendering dependencies to avoid ESM compatibility issues during tests
jest.mock('react-markdown', () => ({
  __esModule: true,
  default: ({ children }) => React.createElement(React.Fragment, null, children),
}))

jest.mock('remark-gfm', () => () => null)
jest.mock('rehype-highlight', () => () => null)


// Mock syntax highlighting dependencies
jest.mock('react-syntax-highlighter', () => ({
  Prism: ({ children }) => React.createElement('pre', null, children),
}))

jest.mock('react-syntax-highlighter/dist/esm/styles/prism', () => ({
  vscDarkPlus: {},
}))
// Provide global test render utilities with providers
const { renderWithProviders } = require('./src/__tests__/test-utils')
global.renderWithProviders = renderWithProviders

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(), // deprecated
    removeListener: jest.fn(), // deprecated
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
})

// Mock IntersectionObserver
global.IntersectionObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}))

// Mock ResizeObserver
global.ResizeObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}))

// Suppress console warnings during tests
const originalWarn = console.warn
beforeAll(() => {
  console.warn = (...args) => {
    if (
      typeof args[0] === 'string' &&
      args[0].includes('ReactDOM.render is no longer supported')
    ) {
      return
    }
    originalWarn.call(console, ...args)
  }
})

afterAll(() => {
  console.warn = originalWarn
})
