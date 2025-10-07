import '@testing-library/jest-dom'

// Global test timeout
jest.setTimeout(30000)

// Increase default waitFor timeout for async operations
import { configure } from '@testing-library/react'
configure({ asyncUtilTimeout: 5000 }) // 5 seconds for waitFor

// Add fetch polyfill for Node.js compatibility
import 'whatwg-fetch'

// Use real fetch for API calls - we want to test the real system
// global.fetch = jest.fn()

// Mock AbortSignal.timeout for Node.js compatibility
if (!AbortSignal.timeout) {
  AbortSignal.timeout = function(ms) {
    const controller = new AbortController()
    setTimeout(() => controller.abort(), ms)
    return controller.signal
  }
}

// Mock environment variables
process.env.NEXT_PUBLIC_API_URL = 'http://localhost:5002'

// ESM-heavy libs mocking for Jest CommonJS runtime
jest.mock('react-markdown', () => ({ __esModule: true, default: ({ children }) => (
  require('react').createElement('div', { 'data-testid': 'markdown' }, children)
) }));
jest.mock('react-syntax-highlighter', () => ({
  __esModule: true,
  Light: ({ children }) => require('react').createElement('pre', { 'data-testid': 'code' }, children),
  default: ({ children }) => require('react').createElement('pre', { 'data-testid': 'code' }, children),
}));
// Also stub style imports from ESM path to avoid Jest parsing ESM exports
jest.mock('react-syntax-highlighter/dist/esm/styles/prism', () => ({}));

// Mock renderer components to avoid ESM parsing issues in import-only tests
jest.mock('@/components/renderers/MarkdownRenderer', () => ({
  __esModule: true,
  default: ({ content, className = '' }) => 
    require('react').createElement('div', { 
      'data-testid': 'markdown-renderer', 
      className 
    }, content)
}));

jest.mock('@/components/renderers/CodeRenderer', () => ({
  __esModule: true,
  default: ({ content, language, className = '' }) => 
    require('react').createElement('pre', { 
      'data-testid': 'code-renderer', 
      className 
    }, content)
}));

// Do not mock ContentRenderer globally; some tests validate real rendering

// Mock utility functions
jest.mock('@/lib/utils', () => ({
  ...jest.requireActual('@/lib/utils'),
  cn: (...classes) => classes.filter(Boolean).join(' '),
  buildApiUrl: (path) => `http://localhost:5002${path}`,
}));

// Mock KnowledgeMap hook
// Do not mock KnowledgeMap globally; component and integration tests rely on it