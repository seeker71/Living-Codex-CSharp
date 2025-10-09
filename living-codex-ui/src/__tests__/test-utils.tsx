import React, { PropsWithChildren } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider, useAuth } from '@/contexts/AuthContext'

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

// Mock next/navigation
const mockPush = jest.fn();
const mockBack = jest.fn();
const mockRouter = {
  push: mockPush,
  back: mockBack,
  forward: jest.fn(),
  refresh: jest.fn(),
  replace: jest.fn(),
  prefetch: jest.fn(),
};

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
  usePathname: () => '/',
  useSearchParams: () => new URLSearchParams(),
  useParams: () => ({}),
}));

// Mock next/image
jest.mock('next/image', () => ({
  __esModule: true,
  default: function MockImage(props: any) {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />
  },
}));

// Note: useTrackInteraction and other hooks should be mocked in individual test files as needed

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

// Mock AuthContext
const MockAuthContext = React.createContext<any>(null)

function MockAuthProvider({ children, authValue }: PropsWithChildren<{ authValue?: any }>) {
  const defaultAuthValue = {
    user: null,
    token: null,
    isLoading: false,
    isAuthenticated: false,
    login: jest.fn(),
    register: jest.fn(),
    logout: jest.fn(),
    refreshUser: jest.fn(),
    testConnection: jest.fn(),
  }

  return (
    <MockAuthContext.Provider value={authValue || defaultAuthValue}>
      {children}
    </MockAuthContext.Provider>
  )
}

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  ...jest.requireActual('@/contexts/AuthContext'),
  useAuth: () => {
    const context = React.useContext(MockAuthContext)
    if (!context) {
      return {
        user: null,
        token: null,
        isLoading: false,
        isAuthenticated: false,
        login: jest.fn(),
        register: jest.fn(),
        logout: jest.fn(),
        refreshUser: jest.fn(),
        testConnection: jest.fn(),
      }
    }
    return context
  },
}))

function Providers({ children, authValue }: PropsWithChildren<{ authValue?: any }>) {
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
    <MockAuthProvider authValue={authValue}>
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </MockAuthProvider>
  )
}

export function renderWithProviders(ui: React.ReactElement, options?: { authValue?: any } & RenderOptions) {
  const { authValue, ...renderOptions } = options || {}
  return render(ui, {
    wrapper: ({ children }) => <Providers authValue={authValue}>{children}</Providers>,
    ...renderOptions,
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
    
    // Create a proper Response-like object
    const mockResponse = {
      ok: response.success !== false,
      status: response.success !== false ? 200 : 400,
      statusText: response.success !== false ? 'OK' : 'Bad Request',
      json: () => Promise.resolve(response),
      text: () => Promise.resolve(JSON.stringify(response)),
      headers: new Headers(),
      url: '',
      type: 'basic' as ResponseType,
      redirected: false,
      clone: () => mockResponse,
      body: null,
      bodyUsed: false,
      arrayBuffer: () => Promise.resolve(new ArrayBuffer(0)),
      blob: () => Promise.resolve(new Blob()),
      formData: () => Promise.resolve(new FormData()),
    }
    
    return Promise.resolve(mockResponse as Response)
  })
}

// Use real API calls - we want to test the real system
// jest.mock('@/lib/api', () => ({
//   api: {
//     get: jest.fn(),
//     post: jest.fn(),
//     put: jest.fn(),
//     delete: jest.fn(),
//   },
//   endpoints: {
//     login: jest.fn(),
//     register: jest.fn(),
//     logout: jest.fn(),
//     validateToken: jest.fn(),
//     getUserProfile: jest.fn(),
//     health: jest.fn(),
//   },
// }))

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

