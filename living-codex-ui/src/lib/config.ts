// Configuration utility to avoid hard-coded URLs and ports
export const config = {
  // Backend API configuration
  backend: {
    baseUrl: process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5002',
    wsUrl: process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:5002',
    timeout: parseInt(process.env.NEXT_PUBLIC_API_TIMEOUT || '10000'),
  },
  
  // Frontend configuration
  frontend: {
    baseUrl: process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
  },
  
  // Development configuration
  dev: {
    hotReload: process.env.NODE_ENV === 'development',
    enableLogging: process.env.NODE_ENV === 'development',
  }
};

// Helper function to build API URLs
export function buildApiUrl(endpoint: string): string {
  const baseUrl = config.backend.baseUrl.replace(/\/$/, ''); // Remove trailing slash
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  return `${baseUrl}${cleanEndpoint}`;
}

// Helper function to build frontend URLs
export function buildFrontendUrl(path: string): string {
  const baseUrl = config.frontend.baseUrl.replace(/\/$/, '');
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  return `${baseUrl}${cleanPath}`;
}
