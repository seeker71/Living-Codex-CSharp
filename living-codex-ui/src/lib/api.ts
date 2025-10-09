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
  errorCode?: string;
  errorDetails?: any;
  timestamp?: string;
  duration?: number;
  httpStatusCode?: number;
  technicalMessage?: string;
  userMessage?: string;
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

      // Get auth token from localStorage
      const authToken = typeof window !== 'undefined' ? localStorage.getItem('auth_token') : null;
      
      const fetchOptions: RequestInit = {
        method,
        headers: {
          'Content-Type': 'application/json',
          ...(authToken && { 'Authorization': `Bearer ${authToken}` }),
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
        let errorText = '';
        let errorData: any = null;

        try {
          errorText = await response.text();
          // Try to parse as JSON for structured error responses
          if (errorText.trim().startsWith('{')) {
            errorData = JSON.parse(errorText);
          }
        } catch (parseError) {
          // If parsing fails, use raw text
          errorData = { error: errorText };
        }

        // Parse structured error response from backend
        const errorMessage = errorData?.error || errorData?.message || `HTTP ${response.status}: ${response.statusText}`;
        const errorCode = errorData?.code || `HTTP_${response.status}`;
        const technicalMessage = errorData?.technicalMessage || response.statusText;
        const userMessage = errorData?.userMessage || errorMessage;

        const structuredError = {
          success: false,
          error: errorMessage,
          errorCode,
          errorDetails: errorData?.details,
          httpStatusCode: response.status,
          technicalMessage,
          userMessage,
          duration,
          timestamp: new Date().toISOString(),
        };

        if (attempt === retries) {
          ApiLogger.log(method, endpoint, duration, 'error', errorMessage);
          return structuredError;
        }

        // Retry on server errors (5xx) but not client errors (4xx)
        if (response.status >= 500) {
          console.warn(`🔄 Retry ${attempt}/${retries} for ${method} ${endpoint} - ${errorMessage}`);
          await delay(RETRY_DELAY * attempt);
          continue;
        } else {
          ApiLogger.log(method, endpoint, duration, 'error', errorMessage);
          return structuredError;
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
          errorCode: 'NETWORK_ERROR',
          httpStatusCode: 0,
          technicalMessage: errorMessage,
          userMessage: 'Network connection failed. Please check your connection and try again.',
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
  getConcepts: (params?: { searchTerm?: string; skip?: number; take?: number }) => {
    const qs = new URLSearchParams();
    if (params?.searchTerm) qs.set('searchTerm', params.searchTerm);
    if (params?.skip !== undefined) qs.set('skip', String(params.skip));
    if (params?.take !== undefined) qs.set('take', String(params.take));
    const suffix = qs.toString();
    return api.get(`/concepts${suffix ? `?${suffix}` : ''}`);
  },
  createConcept: (concept: Record<string, unknown>) => api.post('/concepts', concept),
  
  // Users and contributions
  getContributionStats: (userId: string) => api.get(`/contributions/stats/${userId}`),
  getCollectiveEnergy: () => api.get('/contributions/abundance/collective-energy'),
  getContributorEnergy: (userId: string) => api.get(`/contributions/abundance/contributor-energy/${userId}`),
  
  // User-concept interactions
  attuneToConcept: (userId: string, conceptId: string) => 
    api.post('/userconcept/link', { userId, conceptId, relationshipType: 'attuned', strength: 1.0 }),
  unattuneConcept: (userId: string, conceptId: string) => 
    api.post('/userconcept/unlink', { userId, conceptId }),
  
  // User interactions (votes, bookmarks, likes, shares)
  setVote: (userId: string, entityId: string, vote: 'up' | 'down' | null, entityType = 'concept') =>
    api.post('/interactions/vote', { userId, entityId, vote, entityType }),
  getVote: (userId: string, entityId: string) =>
    api.get(`/interactions/vote/${userId}/${entityId}`),
  getVoteCounts: (entityId: string) =>
    api.get(`/interactions/votes/${entityId}`),
  
  toggleBookmark: (userId: string, entityId: string, entityType = 'concept') =>
    api.post('/interactions/bookmark', { userId, entityId, entityType }),
  checkBookmark: (userId: string, entityId: string) =>
    api.get(`/interactions/bookmark/${userId}/${entityId}`),
  getBookmarks: (userId: string, skip = 0, take = 50) =>
    api.get(`/interactions/bookmarks/${userId}?skip=${skip}&take=${take}`),
  
  toggleLike: (userId: string, entityId: string, entityType = 'concept') =>
    api.post('/interactions/like', { userId, entityId, entityType }),
  getLikeCount: (entityId: string) =>
    api.get(`/interactions/likes/${entityId}`),
  
  recordShare: (userId: string, entityId: string, shareMethod = 'link', entityType = 'concept') =>
    api.post('/interactions/share', { userId, entityId, shareMethod, entityType }),
  getShareCount: (entityId: string) =>
    api.get(`/interactions/shares/${entityId}`),
  
  getUserInteractions: (userId: string, entityId: string) =>
    api.get(`/interactions/${userId}/${entityId}`),
  
  // News
  getTrendingTopics: (limit = 10, hoursBack = 24) => 
    api.get(`/news/trending?limit=${limit}&hoursBack=${hoursBack}`),
  getNewsFeed: (userId: string, limit = 20, hoursBack = 24, skip = 0) =>
    api.get(`/news/feed/${userId}?limit=${limit}&skip=${skip}&hoursBack=${hoursBack}`),
  getPersonalNewsStream: (userId: string, limit = 20) =>
    api.get(`/news/feed/${userId}?limit=${limit}`),
  getPersonalContributionsFeed: (userId: string, limit = 20) =>
    api.get(`/contributions/user/${userId}?limit=${limit}&sortBy=timestamp&sortDescending=true`),
  searchNews: (query: { interests?: string[]; location?: string; contributions?: string[]; limit?: number; hoursBack?: number; skip?: number; }) => api.post('/news/search', query),
  getNewsStats: (hoursBack = 24, search?: string) => {
    const params = new URLSearchParams();
    params.set('hoursBack', String(hoursBack));
    if (search) params.set('search', search);
    const qs = params.toString();
    return api.get(`/news/stats${qs ? `?${qs}` : ''}`);
  },
  
  // User-concept relationships
  getUserConcepts: (userId: string) => api.get(`/userconcept/user-concepts/${userId}`),
  
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
  searchEdgesAdvanced: (searchRequest: {
    fromId?: string;
    toId?: string;
    nodeId?: string;
    role?: string;
    relationship?: string;
    minWeight?: number;
    maxWeight?: number;
    searchTerm?: string;
    take?: number;
    skip?: number;
  }) => {
    const params = new URLSearchParams();
    if (searchRequest.fromId) params.set('fromId', searchRequest.fromId);
    if (searchRequest.toId) params.set('toId', searchRequest.toId);
    if (searchRequest.nodeId) params.set('nodeId', searchRequest.nodeId);
    if (searchRequest.role) params.set('role', searchRequest.role);
    if (searchRequest.relationship) params.set('relationship', searchRequest.relationship);
    if (searchRequest.minWeight !== undefined) params.set('minWeight', searchRequest.minWeight.toString());
    if (searchRequest.maxWeight !== undefined) params.set('maxWeight', searchRequest.maxWeight.toString());
    if (searchRequest.searchTerm) params.set('searchTerm', searchRequest.searchTerm);
    if (searchRequest.take) params.set('take', searchRequest.take.toString());
    if (searchRequest.skip) params.set('skip', searchRequest.skip.toString());
    
    const queryString = params.toString();
    return api.get(`/storage-endpoints/edges${queryString ? `?${queryString}` : ''}`);
  },
  getEdge: (fromId: string, toId: string) => api.get(`/storage-endpoints/edges/${fromId}/${toId}`),
  getEdgeMetadata: () => api.get('/storage-endpoints/edges/metadata'),
};

// Error handling utilities
export class ApiErrorHandler {
  static getUserMessage(errorResponse: ApiResponse): string {
    if (errorResponse.userMessage) {
      return errorResponse.userMessage;
    }

    if (errorResponse.errorCode) {
      // Map common error codes to user-friendly messages
      const codeMessages: Record<string, string> = {
        'NOT_FOUND': 'The requested resource was not found.',
        'VALIDATION_ERROR': 'Please check your input and try again.',
        'AUTHENTICATION_REQUIRED': 'Please log in to access this feature.',
        'AUTHORIZATION_DENIED': 'You don\'t have permission to perform this action.',
        'RATE_LIMIT_EXCEEDED': 'Too many requests. Please wait a moment and try again.',
        'SERVICE_UNAVAILABLE': 'The service is temporarily unavailable. Please try again later.',
        'NETWORK_ERROR': 'Network connection failed. Please check your connection and try again.',
      };

      return codeMessages[errorResponse.errorCode] || errorResponse.error || 'An unexpected error occurred.';
    }

    return errorResponse.error || 'An unexpected error occurred.';
  }

  static getErrorSeverity(errorResponse: ApiResponse): 'low' | 'medium' | 'high' | 'critical' {
    if (errorResponse.errorCode?.startsWith('NETWORK_')) {
      return 'medium';
    }

    if (errorResponse.httpStatusCode) {
      if (errorResponse.httpStatusCode >= 500) {
        return 'high';
      }
      if (errorResponse.httpStatusCode >= 400) {
        return 'medium';
      }
    }

    return 'low';
  }

  static shouldRetry(errorResponse: ApiResponse): boolean {
    // Retry on network errors and server errors (5xx)
    return errorResponse.errorCode?.startsWith('NETWORK_') ||
           errorResponse.httpStatusCode === 502 ||
           errorResponse.httpStatusCode === 503 ||
           errorResponse.httpStatusCode === 504;
  }

  static getRetryDelay(errorResponse: ApiResponse, attempt: number): number {
    // Exponential backoff with jitter
    const baseDelay = 1000; // 1 second
    const maxDelay = 10000; // 10 seconds
    const exponentialDelay = Math.min(baseDelay * Math.pow(2, attempt - 1), maxDelay);
    const jitter = Math.random() * 500; // Add up to 500ms jitter
    return exponentialDelay + jitter;
  }
}

// Export logger for debugging
export { ApiLogger };
