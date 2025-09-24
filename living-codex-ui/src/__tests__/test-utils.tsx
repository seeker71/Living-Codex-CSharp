import React, { PropsWithChildren } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

// Minimal AuthContext mock matching useAuth shape
import { createContext, useContext } from 'react'

type MockUser = { id: string; username: string; email: string; displayName: string; createdAt: string; isActive: boolean }
type MockAuthState = {
  user: MockUser | null
  token: string | null
  isLoading: boolean
  isAuthenticated: boolean
  login: () => Promise<{ success: boolean }>
  register: () => Promise<{ success: boolean }>
  logout: () => void
  refreshUser: () => Promise<void>
  testConnection: () => Promise<boolean>
}

const DefaultAuthValue: MockAuthState = {
  user: null,
  token: null,
  isLoading: false,
  isAuthenticated: false,
  login: async () => ({ success: true }),
  register: async () => ({ success: true }),
  logout: () => {},
  refreshUser: async () => {},
  testConnection: async () => true,
}

const MockAuthContext = createContext<MockAuthState>(DefaultAuthValue)
export const useMockAuth = () => useContext(MockAuthContext)

function Providers({ children, authValue }: PropsWithChildren<{ authValue?: Partial<MockAuthState> }>) {
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
    <MockAuthContext.Provider value={{ ...DefaultAuthValue, ...(authValue || {}) }}>
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </MockAuthContext.Provider>
  )
}

export function renderWithProviders(ui: React.ReactElement, options?: RenderOptions & { authValue?: Partial<MockAuthState> }) {
  const { authValue, ...rest } = options || {}
  return render(ui, {
    wrapper: ({ children }) => <Providers authValue={authValue}>{children}</Providers>,
    ...rest,
  })
}

