'use client';

// Backend API configuration and utilities
import { config } from './config';

const BACKEND_BASE_URL = config.backend.baseUrl;
const DEFAULT_TIMEOUT = 10000; // 10 seconds
const RETRY_ATTEMPTS = 3;
const RETRY_DELAY = 1000; // 1 second

export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  timestamp?: string;
  duration?: number;
}

export interface ApiOptions {
  timeout?: number;
  retries?: number;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  headers?: Record<string, string>;
}

class ApiLogger {
  private static logs: Array<{
    timestamp: string;
    method: string;
    url: string;
    duration: number;
    status: 'success' | 'error' | 'timeout';
    error?: string;
  }> = [];

  static log(method: string, url: string, duration: number, status: 'success' | 'error' | 'timeout', error?: string) {
    const logEntry = {
      timestamp: new Date().toISOString(),
      method,
      url,
      duration,
      status,
      error,
    };
    
    this.logs.push(logEntry);
    
    // Keep only last 100 logs
    if (this.logs.length > 100) {
      this.logs.shift();
    }

    // Console logging with proper formatting
    const logLevel = status === 'success' ? 'info' : status === 'timeout' ? 'warn' : 'error';
    const message = `[API ${method}] ${url} - ${duration}ms - ${status.toUpperCase()}${error ? `: ${error}` : ''}`;
    
    if (logLevel === 'info') {
      console.log(`✅ ${message}`);
    } else if (logLevel === 'warn') {
      console.warn(`⚠️ ${message}`);
    } else {
      console.error(`❌ ${message}`);
    }
  }

  static getLogs() {
    return [...this.logs];
  }

  static getFailedCalls() {
    return this.logs.filter(log => log.status === 'error' || log.status === 'timeout');
  }

  static getSlowCalls(threshold = 5000) {
    return this.logs.filter(log => log.duration > threshold);
  }
}

async function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

export async function apiCall<T = unknown>(
  endpoint: string,
  options: ApiOptions = {}
): Promise<ApiResponse<T>> {
  const {
    timeout = DEFAULT_TIMEOUT,
    retries = RETRY_ATTEMPTS,
    method = 'GET',
    body,
    headers = {},
  } = options;

  const url = `${BACKEND_BASE_URL}${endpoint}`;
  const startTime = Date.now();

  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), timeout);

      const fetchOptions: RequestInit = {
        method,
        headers: {
          'Content-Type': 'application/json',
          ...headers,
        },
        signal: controller.signal,
      };

      if (body && (method === 'POST' || method === 'PUT')) {
        fetchOptions.body = JSON.stringify(body);
      }

      const response = await fetch(url, fetchOptions);
      clearTimeout(timeoutId);

      const duration = Date.now() - startTime;
      
      if (!response.ok) {
        const errorText = await response.text();
        const error = `HTTP ${response.status}: ${errorText}`;
        
        if (attempt === retries) {
          ApiLogger.log(method, endpoint, duration, 'error', error);
          return {
            success: false,
            error,
            duration,
            timestamp: new Date().toISOString(),
          };
        }
        
        // Retry on server errors (5xx) but not client errors (4xx)
        if (response.status >= 500) {
          console.warn(`🔄 Retry ${attempt}/${retries} for ${method} ${endpoint} - ${error}`);
          await delay(RETRY_DELAY * attempt);
          continue;
        } else {
          ApiLogger.log(method, endpoint, duration, 'error', error);
          return {
            success: false,
            error,
            duration,
            timestamp: new Date().toISOString(),
          };
        }
      }

      const data = await response.json();
      ApiLogger.log(method, endpoint, duration, 'success');
      
      return {
        success: true,
        data,
        duration,
        timestamp: new Date().toISOString(),
      };

    } catch (error) {
      const duration = Date.now() - startTime;
      
      if (error instanceof Error && error.name === 'AbortError') {
        if (attempt === retries) {
          ApiLogger.log(method, endpoint, duration, 'timeout', `Timeout after ${timeout}ms`);
          return {
            success: false,
            error: `Request timeout after ${timeout}ms`,
            duration,
            timestamp: new Date().toISOString(),
          };
        }
        
        console.warn(`⏱️ Timeout ${attempt}/${retries} for ${method} ${endpoint} - retrying...`);
        await delay(RETRY_DELAY * attempt);
        continue;
      }

      if (attempt === retries) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        ApiLogger.log(method, endpoint, duration, 'error', errorMessage);
        return {
          success: false,
          error: errorMessage,
          duration,
          timestamp: new Date().toISOString(),
        };
      }

      console.warn(`🔄 Network error ${attempt}/${retries} for ${method} ${endpoint} - retrying...`);
      await delay(RETRY_DELAY * attempt);
    }
  }

  // This should never be reached, but TypeScript requires it
  return {
    success: false,
    error: 'Maximum retries exceeded',
    timestamp: new Date().toISOString(),
  };
}

// Convenience methods for common API calls
export const api = {
  get: <T = unknown>(endpoint: string, options?: Omit<ApiOptions, 'method'>) =>
    apiCall<T>(endpoint, { ...options, method: 'GET' }),

  post: <T = unknown>(endpoint: string, body?: unknown, options?: Omit<ApiOptions, 'method' | 'body'>) =>
    apiCall<T>(endpoint, { ...options, method: 'POST', body }),

  put: <T = unknown>(endpoint: string, body?: unknown, options?: Omit<ApiOptions, 'method' | 'body'>) =>
    apiCall<T>(endpoint, { ...options, method: 'PUT', body }),

  delete: <T = unknown>(endpoint: string, options?: Omit<ApiOptions, 'method'>) =>
    apiCall<T>(endpoint, { ...options, method: 'DELETE' }),

  // Health check with fast timeout
  health: () => apiCall('/health', { timeout: 3000, retries: 1 }),

  // Get API logs for debugging
  getLogs: () => ApiLogger.getLogs(),
  getFailedCalls: () => ApiLogger.getFailedCalls(),
  getSlowCalls: (threshold?: number) => ApiLogger.getSlowCalls(threshold),
};

// Specific API endpoints
export const endpoints = {
  // Health and system
  health: () => api.get('/health'),
  storageStats: () => api.get('/storage-endpoints/stats'),
  
  // Authentication (Unified)
  login: (usernameOrEmail: string, password: string, rememberMe: boolean = false) => 
    api.post('/auth/login', { usernameOrEmail, password, rememberMe }),
  register: (username: string, email: string, password: string, displayName?: string) => 
    api.post('/auth/register', { username, email, password, displayName }),
  logout: (token: string) => 
    api.post('/auth/logout', { token }),
  validateToken: (token: string) => 
    api.post('/auth/validate', { token }),
  getUserProfile: (userId: string) =>
    api.get(`/auth/profile/${userId}`),
  updateUserProfile: (userId: string, updates: { displayName?: string; email?: string }) =>
    api.put(`/auth/profile/${userId}`, updates),
  changePassword: (userId: string, currentPassword: string, newPassword: string) =>
    api.post('/auth/change-password', { userId, currentPassword, newPassword }),
  
  // Concepts
  getConcepts: () => api.get('/concepts'),
  createConcept: (concept: Record<string, unknown>) => api.post('/concepts', concept),
  
  // Users and contributions
  getContributionStats: (userId: string) => api.get(`/contributions/stats/${userId}`),
  getCollectiveEnergy: () => api.get('/contributions/abundance/collective-energy'),
  getContributorEnergy: (userId: string) => api.get(`/contributions/abundance/contributor-energy/${userId}`),
  
  // User-concept interactions
  attuneToConcept: (userId: string, conceptId: string) => 
    api.post('/concept/user/link', { userId, conceptId, relation: 'attuned' }),
  unattuneConcept: (userId: string, conceptId: string) => 
    api.post('/concept/user/unlink', { userId, conceptId }),
  
  // News
  getTrendingTopics: (limit = 10, hoursBack = 24) => 
    api.get(`/news/trending?limit=${limit}&hoursBack=${hoursBack}`),
  getNewsFeed: (userId: string, limit = 20, hoursBack = 24) =>
    api.get(`/news/feed/${userId}?limit=${limit}&hoursBack=${hoursBack}`),
  getPersonalNewsStream: (userId: string, limit = 20) =>
    api.get(`/news/feed/${userId}?limit=${limit}`),
  getPersonalContributionsFeed: (userId: string, limit = 20) =>
    api.get(`/contributions/user/${userId}?limit=${limit}&sortBy=timestamp&sortDescending=true`),
  searchNews: (query: Record<string, unknown>) => api.post('/news/search', query),
  
  // User-concept relationships
  getUserConcepts: (userId: string) => api.get(`/concept/user/${userId}`),
  
  // Contributions
  recordContribution: (contribution: {
    userId: string;
    entityId: string;
    entityType?: string;
    contributionType?: string;
    description?: string;
    value?: number;
    metadata?: Record<string, any>;
  }) => api.post('/contributions/record', contribution),
  
  getUserContributions: (userId: string, query?: Record<string, any>) => {
    const params = query ? new URLSearchParams(query).toString() : '';
    return api.get(`/contributions/user/${userId}${params ? `?${params}` : ''}`);
  },
  
  // Storage and nodes
  getNodes: (typeId?: string, limit?: number) => {
    const params = new URLSearchParams();
    if (typeId) params.set('typeId', typeId);
    if (limit) params.set('limit', limit.toString());
    const queryString = params.toString();
    return api.get(`/storage-endpoints/nodes${queryString ? `?${queryString}` : ''}`);
  },
  getNodeTypes: () => api.get('/storage-endpoints/types'),
  searchNodesAdvanced: (searchRequest: {
    typeIds?: string[];
    searchTerm?: string;
    states?: string[];
    take?: number;
    skip?: number;
    sortBy?: string;
    sortDescending?: boolean;
  }) => api.post('/storage-endpoints/nodes/search', searchRequest),
  getNode: (id: string) => api.get(`/storage-endpoints/nodes/${id}`),
  searchNodes: (query: Record<string, unknown>) => api.post('/storage-endpoints/nodes/search', query),
  
  // Edges
  getEdges: (limit?: number) => {
    const params = new URLSearchParams();
    if (limit) params.set('limit', limit.toString());
    const queryString = params.toString();
    return api.get(`/storage-endpoints/edges${queryString ? `?${queryString}` : ''}`);
  },
  getEdge: (fromId: string, toId: string) => api.get(`/storage-endpoints/edges/${fromId}/${toId}`),
};

// Export logger for debugging
export { ApiLogger };
