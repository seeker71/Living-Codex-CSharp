import React, { PropsWithChildren } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/contexts/AuthContext'

// Test infrastructure validation
export const TEST_INFRASTRUCTURE = {
  config: {
    backend: {
      baseUrl: 'http://localhost:5002',
      timeout: 10000,
    },
    frontend: {
      baseUrl: 'http://localhost:3000',
    },
    dev: {
      hotReload: false,
      enableLogging: false,
    }
  },
  auth: {
    user: { id: 'test-user', username: 'testuser' },
    token: 'test-token',
    isLoading: false,
    isAuthenticated: true,
  },
  api: {
    buildApiUrl: (path: string) => `http://localhost:5002${path}`,
    buildFrontendUrl: (path: string) => `http://localhost:3000${path}`,
  }
}

// Mock the config module
jest.mock('@/lib/config', () => ({
  config: {
    backend: {
      baseUrl: 'http://localhost:5002',
      timeout: 10000,
    },
    frontend: {
      baseUrl: 'http://localhost:3000',
    },
    dev: {
      hotReload: false,
      enableLogging: false,
    }
  },
  buildApiUrl: (path: string) => `http://localhost:5002${path}`,
  buildFrontendUrl: (path: string) => `http://localhost:3000${path}`,
}))

// Mock problematic dependencies
jest.mock('react-markdown', () => {
  return function MockMarkdown({ children }: { children: React.ReactNode }) {
    return <div data-testid="mock-markdown">{children}</div>
  }
})

jest.mock('react-syntax-highlighter', () => ({
  Prism: ({ children }: { children: React.ReactNode }) => <pre data-testid="mock-syntax-highlighter">{children}</pre>,
}))

jest.mock('remark-gfm', () => ({
  default: () => ({}),
}))

jest.mock('rehype-highlight', () => ({
  default: () => ({}),
}))

// Mock lucide-react icons as React components
jest.mock('lucide-react', () => {
  const MockIcon = ({ className }: { className?: string }) => <div className={className} data-testid="mock-icon">Icon</div>
  return {
    Heart: MockIcon,
    Share2: MockIcon,
    MessageCircle: MockIcon,
    ExternalLink: MockIcon,
    AlertCircle: MockIcon,
    RefreshCw: MockIcon,
    X: MockIcon,
    Plus: MockIcon,
    Users: MockIcon,
    Search: MockIcon,
    Filter: MockIcon,
    Clock: MockIcon,
    Image: MockIcon,
    ChevronDown: MockIcon,
    ChevronUp: MockIcon,
    Menu: MockIcon,
    Home: MockIcon,
    User: MockIcon,
    Settings: MockIcon,
    LogOut: MockIcon,
    Eye: MockIcon,
    EyeOff: MockIcon,
    Check: MockIcon,
    XCircle: MockIcon,
    CheckCircle: MockIcon,
    AlertTriangle: MockIcon,
    Info: MockIcon,
    Loader2: MockIcon,
    MoreHorizontal: MockIcon,
    Edit3: MockIcon,
    Trash2: MockIcon,
    Pin: MockIcon,
    Copy: MockIcon,
    Reply: MockIcon,
    Smile: MockIcon,
    Hash: MockIcon,
    Network: MockIcon,
    Sparkles: MockIcon,
    Star: MockIcon,
    TrendingUp: MockIcon,
    RotateCcw: MockIcon,
    MessageSquare: MockIcon,
  }
})

function Providers({ children }: PropsWithChildren) {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 0 },
      mutations: { retry: false },
    },
    logger: {
      log: console.log,
      warn: console.warn,
      error: () => {},
    },
  })

  return (
    <AuthProvider>
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </AuthProvider>
  )
}

export function renderWithProviders(ui: React.ReactElement, options?: RenderOptions) {
  return render(ui, {
    wrapper: ({ children }) => <Providers>{children}</Providers>,
    ...options,
  })
}

// Test helper to verify infrastructure is working
export function createTestInfrastructure() {
  return {
    config: {
      backend: {
        baseUrl: 'http://localhost:5002',
        timeout: 10000,
      },
      frontend: {
        baseUrl: 'http://localhost:3000',
      },
      dev: {
        hotReload: false,
        enableLogging: false,
      }
    },
    auth: {
      user: { id: 'test-user', username: 'testuser' },
      token: 'test-token',
      isLoading: false,
      isAuthenticated: true,
    },
    api: {
      buildApiUrl: (path: string) => `http://localhost:5002${path}`,
      buildFrontendUrl: (path: string) => `http://localhost:3000${path}`,
    }
  }
}

// Mock implementations for common APIs
export const mockFetch = (responses: any[]) => {
  let callCount = 0
  return jest.fn().mockImplementation(() => {
    const response = responses[callCount % responses.length]
    callCount++
    return Promise.resolve(response)
  })
}

export const mockClipboard = {
  writeText: jest.fn().mockResolvedValue(undefined),
  readText: jest.fn().mockResolvedValue(''),
}

export const mockNavigator = {
  share: jest.fn().mockResolvedValue(undefined),
  clipboard: mockClipboard,
}

// Setup global mocks
Object.assign(navigator, mockNavigator)

// Mock window.confirm
export const mockConfirm = jest.fn()
Object.defineProperty(window, 'confirm', {
  writable: true,
  value: mockConfirm,
})

// Mock window.alert
export const mockAlert = jest.fn()
Object.defineProperty(window, 'alert', {
  writable: true,
  value: mockAlert,
})

// Performance mock
export const mockPerformance = {
  now: jest.fn(() => 0),
  mark: jest.fn(),
  measure: jest.fn(),
}
Object.defineProperty(window, 'performance', {
  writable: true,
  value: mockPerformance,
})

