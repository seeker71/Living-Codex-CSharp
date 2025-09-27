import '@testing-library/jest-dom'

// Global test timeout
jest.setTimeout(30000)

// Mock fetch for API calls
global.fetch = jest.fn()

// Mock environment variables
process.env.NEXT_PUBLIC_API_URL = 'http://localhost:5002'