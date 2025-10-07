'use client';

// AI Queue API utilities for handling async AI requests with graceful degradation
import React from 'react';
import { apiCall, ApiResponse } from './api';

export interface AIQueueRequest {
  requestId?: string;
  requestType: string;
  userId?: string;
  model?: string;
  priority?: 'low' | 'normal' | 'high';
  timeout?: number; // in seconds
  estimatedDuration?: number; // in seconds
  parameters?: Record<string, any>;
}

export interface AIQueueResponse {
  success: boolean;
  requestId: string;
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'timeout' | 'cancelled' | 'rejected';
  result?: any;
  processingTime?: number;
  errorMessage?: string;
  estimatedWaitTime?: number; // in seconds
  checkStatusUrl?: string;
  message?: string;
}

export interface AIQueueStatus {
  success: boolean;
  requestId: string;
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'timeout' | 'cancelled' | 'rejected';
  result?: any;
  processingTime?: number;
  errorMessage?: string;
  estimatedWaitTime?: number;
  timestamp: string;
}

export interface AIQueueMetrics {
  success: boolean;
  metrics: {
    timestamp: string;
    configuration: {
      maxConcurrentRequests: number;
      maxQueueSize: number;
    };
    current: {
      activeRequests: number;
      queuedRequests: number;
      completedRequests: number;
    };
    totals: {
      processed: number;
      rejected: number;
      timeout: number;
    };
    utilization: {
      queueUtilization: number;
      queueFullness: number;
    };
    health: {
      score: number;
      status: string;
    };
  };
}

class AIQueueManager {
  private static instance: AIQueueManager;
  private activeRequests = new Map<string, AIQueueStatus>();
  private statusCheckIntervals = new Map<string, NodeJS.Timeout>();

  static getInstance(): AIQueueManager {
    if (!AIQueueManager.instance) {
      AIQueueManager.instance = new AIQueueManager();
    }
    return AIQueueManager.instance;
  }

  /**
   * Queue an AI request with automatic async/sync handling
   */
  async queueRequest(request: AIQueueRequest): Promise<ApiResponse<AIQueueResponse>> {
    try {
      const response = await apiCall<AIQueueResponse>('/ai/queue/request', {
        method: 'POST',
        body: request,
        timeout: 30000, // 30 second timeout for queueing
        retries: 2
      });

      if (response.success && response.data) {
        const queueResponse = response.data;
        
        // Store active request
        this.activeRequests.set(queueResponse.requestId, {
          success: queueResponse.success,
          requestId: queueResponse.requestId,
          status: queueResponse.status,
          result: queueResponse.result,
          processingTime: queueResponse.processingTime,
          errorMessage: queueResponse.errorMessage,
          estimatedWaitTime: queueResponse.estimatedWaitTime,
          timestamp: new Date().toISOString()
        });

        // If async, start status polling
        if (queueResponse.status === 'queued' && queueResponse.checkStatusUrl) {
          this.startStatusPolling(queueResponse.requestId);
        }
      }

      return response;
    } catch (error) {
      console.error('Error queuing AI request:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Get status of a queued request
   */
  async getRequestStatus(requestId: string): Promise<ApiResponse<AIQueueStatus>> {
    try {
      const response = await apiCall<AIQueueStatus>(`/ai/queue/status/${requestId}`, {
        method: 'GET',
        timeout: 10000,
        retries: 1
      });

      if (response.success && response.data) {
        // Update stored status
        this.activeRequests.set(requestId, response.data);
        
        // Stop polling if completed
        if (['completed', 'failed', 'timeout', 'cancelled'].includes(response.data.status)) {
          this.stopStatusPolling(requestId);
        }
      }

      return response;
    } catch (error) {
      console.error('Error getting request status:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Get queue metrics
   */
  async getQueueMetrics(): Promise<ApiResponse<AIQueueMetrics>> {
    try {
      return await apiCall<AIQueueMetrics>('/ai/queue/metrics', {
        method: 'GET',
        timeout: 10000,
        retries: 1
      });
    } catch (error) {
      console.error('Error getting queue metrics:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Cancel a queued request
   */
  async cancelRequest(requestId: string): Promise<ApiResponse<any>> {
    try {
      const response = await apiCall(`/ai/queue/cancel/${requestId}`, {
        method: 'POST',
        timeout: 10000,
        retries: 1
      });

      // Stop polling regardless of response
      this.stopStatusPolling(requestId);

      return response;
    } catch (error) {
      console.error('Error canceling request:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Start polling for request status
   */
  private startStatusPolling(requestId: string): void {
    // Clear any existing interval
    this.stopStatusPolling(requestId);

    const interval = setInterval(async () => {
      try {
        const statusResponse = await this.getRequestStatus(requestId);
        if (statusResponse.success && statusResponse.data) {
          // Emit event for UI updates
          this.emitStatusUpdate(requestId, statusResponse.data);
        }
      } catch (error) {
        console.error('Error polling request status:', error);
        this.stopStatusPolling(requestId);
      }
    }, 2000); // Poll every 2 seconds

    this.statusCheckIntervals.set(requestId, interval);
  }

  /**
   * Stop polling for request status
   */
  private stopStatusPolling(requestId: string): void {
    const interval = this.statusCheckIntervals.get(requestId);
    if (interval) {
      clearInterval(interval);
      this.statusCheckIntervals.delete(requestId);
    }
  }

  /**
   * Emit status update event
   */
  private emitStatusUpdate(requestId: string, status: AIQueueStatus): void {
    // Dispatch custom event for UI components to listen to
    const event = new CustomEvent('ai-request-status-update', {
      detail: { requestId, status }
    });
    window.dispatchEvent(event);
  }

  /**
   * Get all active requests
   */
  getActiveRequests(): Map<string, AIQueueStatus> {
    return new Map(this.activeRequests);
  }

  /**
   * Clear completed requests
   */
  clearCompletedRequests(): void {
    for (const [requestId, status] of this.activeRequests.entries()) {
      if (['completed', 'failed', 'timeout', 'cancelled'].includes(status.status)) {
        this.activeRequests.delete(requestId);
        this.stopStatusPolling(requestId);
      }
    }
  }

  /**
   * Cleanup all requests and intervals
   */
  cleanup(): void {
    // Clear all intervals
    for (const interval of this.statusCheckIntervals.values()) {
      clearInterval(interval);
    }
    this.statusCheckIntervals.clear();
    
    // Clear all requests
    this.activeRequests.clear();
  }
}

// React hook for AI queue management
export function useAIQueue() {
  const queueManager = AIQueueManager.getInstance();

  const queueRequest = async (request: AIQueueRequest): Promise<ApiResponse<AIQueueResponse>> => {
    return await queueManager.queueRequest(request);
  };

  const getRequestStatus = async (requestId: string): Promise<ApiResponse<AIQueueStatus>> => {
    return await queueManager.getRequestStatus(requestId);
  };

  const getQueueMetrics = async (): Promise<ApiResponse<AIQueueMetrics>> => {
    return await queueManager.getQueueMetrics();
  };

  const cancelRequest = async (requestId: string): Promise<ApiResponse<any>> => {
    return await queueManager.cancelRequest(requestId);
  };

  const getActiveRequests = (): Map<string, AIQueueStatus> => {
    return queueManager.getActiveRequests();
  };

  const clearCompletedRequests = (): void => {
    queueManager.clearCompletedRequests();
  };

  return {
    queueRequest,
    getRequestStatus,
    getQueueMetrics,
    cancelRequest,
    getActiveRequests,
    clearCompletedRequests
  };
}

// React hook for monitoring a specific AI request
export function useAIRequestStatus(requestId: string | null) {
  const [status, setStatus] = React.useState<AIQueueStatus | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!requestId) return;

    const queueManager = AIQueueManager.getInstance();
    
    // Get initial status
    const getInitialStatus = async () => {
      setLoading(true);
      try {
        const response = await queueManager.getRequestStatus(requestId);
        if (response.success && response.data) {
          setStatus(response.data);
        } else {
          setError(response.error || 'Failed to get request status');
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    getInitialStatus();

    // Listen for status updates
    const handleStatusUpdate = (event: CustomEvent) => {
      if (event.detail.requestId === requestId) {
        setStatus(event.detail.status);
      }
    };

    window.addEventListener('ai-request-status-update', handleStatusUpdate as EventListener);

    return () => {
      window.removeEventListener('ai-request-status-update', handleStatusUpdate as EventListener);
    };
  }, [requestId]);

  return { status, loading, error };
}

// Convenience functions for common AI requests
export const aiQueue = {
  // Concept extraction
  extractConcepts: (text: string, userId?: string, priority: 'low' | 'normal' | 'high' = 'normal') =>
    AIQueueManager.getInstance().queueRequest({
      requestType: 'concept-extraction',
      userId,
      priority,
      parameters: { text },
      estimatedDuration: 2
    }),

  // Analysis
  analyzeContent: (content: string, analysisType: string, userId?: string, priority: 'low' | 'normal' | 'high' = 'normal') =>
    AIQueueManager.getInstance().queueRequest({
      requestType: 'analysis',
      userId,
      priority,
      parameters: { content, analysisType },
      estimatedDuration: 5
    }),

  // Transformation
  transformContent: (content: string, transformationType: string, userId?: string, priority: 'low' | 'normal' | 'high' = 'normal') =>
    AIQueueManager.getInstance().queueRequest({
      requestType: 'transformation',
      userId,
      priority,
      parameters: { content, transformationType },
      estimatedDuration: 8
    }),

  // Generation
  generateContent: (prompt: string, contentType: string, userId?: string, priority: 'low' | 'normal' | 'high' = 'normal') =>
    AIQueueManager.getInstance().queueRequest({
      requestType: 'generation',
      userId,
      priority,
      parameters: { prompt, contentType },
      estimatedDuration: 10
    })
};

// Export singleton instance
export const aiQueueManager = AIQueueManager.getInstance();
