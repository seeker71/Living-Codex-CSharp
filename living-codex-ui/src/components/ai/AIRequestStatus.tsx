'use client';

import React from 'react';
import { useAIRequestStatus, AIQueueStatus, useAIQueue } from '@/lib/ai-queue-api';

interface AIRequestStatusProps {
  requestId: string | null;
  onComplete?: (result: any) => void;
  onError?: (error: string) => void;
  showProgress?: boolean;
  className?: string;
}

export function AIRequestStatus({ 
  requestId, 
  onComplete, 
  onError, 
  showProgress = true,
  className = ''
}: AIRequestStatusProps) {
  const { status, loading, error } = useAIRequestStatus(requestId);

  React.useEffect(() => {
    if (status) {
      if (status.status === 'completed' && status.result && onComplete) {
        onComplete(status.result);
      } else if (status.status === 'failed' && status.errorMessage && onError) {
        onError(status.errorMessage);
      }
    }
  }, [status, onComplete, onError]);

  if (!requestId) {
    return null;
  }

  if (loading) {
    return (
      <div className={`flex items-center space-x-2 text-sm text-gray-600 ${className}`}>
        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
        <span>Checking request status...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`flex items-center space-x-2 text-sm text-red-600 ${className}`}>
        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
        </svg>
        <span>Error: {error}</span>
      </div>
    );
  }

  if (!status) {
    return null;
  }

  const getStatusIcon = () => {
    switch (status.status) {
      case 'queued':
        return (
          <div className="animate-pulse rounded-full h-4 w-4 bg-yellow-400"></div>
        );
      case 'processing':
        return (
          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
        );
      case 'completed':
        return (
          <svg className="w-4 h-4 text-green-600" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
          </svg>
        );
      case 'failed':
        return (
          <svg className="w-4 h-4 text-red-600" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
        );
      case 'timeout':
        return (
          <svg className="w-4 h-4 text-orange-600" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
          </svg>
        );
      case 'cancelled':
        return (
          <svg className="w-4 h-4 text-gray-600" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
        );
      default:
        return null;
    }
  };

  const getStatusText = () => {
    switch (status.status) {
      case 'queued':
        return 'Request queued for processing';
      case 'processing':
        return 'Processing request...';
      case 'completed':
        return 'Request completed';
      case 'failed':
        return 'Request failed';
      case 'timeout':
        return 'Request timed out';
      case 'cancelled':
        return 'Request cancelled';
      default:
        return status.status;
    }
  };

  const getStatusColor = () => {
    switch (status.status) {
      case 'queued':
        return 'text-yellow-600';
      case 'processing':
        return 'text-blue-600';
      case 'completed':
        return 'text-green-600';
      case 'failed':
        return 'text-red-600';
      case 'timeout':
        return 'text-orange-600';
      case 'cancelled':
        return 'text-gray-600';
      default:
        return 'text-gray-600';
    }
  };

  return (
    <div className={`flex items-center space-x-2 text-sm ${getStatusColor()} ${className}`}>
      {getStatusIcon()}
      <span>{getStatusText()}</span>
      
      {status.estimatedWaitTime && status.status === 'queued' && (
        <span className="text-xs text-gray-500">
          (Est. {Math.round(status.estimatedWaitTime)}s)
        </span>
      )}
      
      {status.processingTime && status.status === 'completed' && (
        <span className="text-xs text-gray-500">
          ({Math.round(status.processingTime / 1000)}s)
        </span>
      )}
      
      {showProgress && status.status === 'processing' && (
        <div className="flex-1 max-w-xs">
          <div className="bg-gray-200 rounded-full h-1">
            <div className="bg-blue-600 h-1 rounded-full animate-pulse" style={{ width: '60%' }}></div>
          </div>
        </div>
      )}
    </div>
  );
}

interface AIRequestListProps {
  className?: string;
}

export function AIRequestList({ className = '' }: AIRequestListProps) {
  const { getActiveRequests, clearCompletedRequests } = useAIQueue();
  const [activeRequests, setActiveRequests] = React.useState<Map<string, AIQueueStatus>>(new Map());

  React.useEffect(() => {
    const updateRequests = () => {
      setActiveRequests(getActiveRequests());
    };

    // Initial load
    updateRequests();

    // Listen for status updates
    const handleStatusUpdate = () => {
      updateRequests();
    };

    window.addEventListener('ai-request-status-update', handleStatusUpdate);

    // Cleanup interval
    const interval = setInterval(updateRequests, 5000);

    return () => {
      window.removeEventListener('ai-request-status-update', handleStatusUpdate);
      clearInterval(interval);
    };
  }, [getActiveRequests]);

  const requests = Array.from(activeRequests.values());

  if (requests.length === 0) {
    return null;
  }

  return (
    <div className={`bg-white rounded-lg shadow-sm border p-4 ${className}`}>
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-medium text-gray-900">AI Requests</h3>
        <button
          onClick={clearCompletedRequests}
          className="text-xs text-gray-500 hover:text-gray-700"
        >
          Clear Completed
        </button>
      </div>
      
      <div className="space-y-2">
        {requests.map((request) => (
          <AIRequestStatus
            key={request.requestId}
            requestId={request.requestId}
            className="text-xs"
          />
        ))}
      </div>
    </div>
  );
}
