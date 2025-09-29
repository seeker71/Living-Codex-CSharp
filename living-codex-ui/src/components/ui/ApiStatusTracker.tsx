'use client';

import { useState, useEffect } from 'react';
import { CheckCircle, XCircle, AlertCircle, Loader2 } from 'lucide-react';

interface ApiStatus {
  endpoint: string;
  status: 'checking' | 'available' | 'unavailable' | 'error';
  error?: string;
  errorCode?: string;
  errorDetails?: any;
  responseTime?: number;
  lastChecked: Date;
}

interface ApiStatusTrackerProps {
  className?: string;
}

const API_ENDPOINTS = [
  { name: 'Health API', endpoint: 'http://localhost:5002/health', method: 'GET' },
  { name: 'Concepts API', endpoint: 'http://localhost:5002/concepts/browse', method: 'POST' },
  { name: 'Gallery API', endpoint: 'http://localhost:5002/gallery/list', method: 'GET' },
  { name: 'Threads API', endpoint: 'http://localhost:5002/threads/groups', method: 'GET' },
  { name: 'Users API', endpoint: 'http://localhost:5002/users/discover', method: 'POST' },
  { name: 'Storage API', endpoint: 'http://localhost:5002/storage-endpoints/stats', method: 'GET' },
  { name: 'News API', endpoint: 'http://localhost:5002/news/feed/test-user', method: 'GET' },
  { name: 'AI Health API', endpoint: 'http://localhost:5002/ai/health', method: 'GET' },
];

export function ApiStatusTracker({ className = '' }: ApiStatusTrackerProps) {
  const [apiStatuses, setApiStatuses] = useState<ApiStatus[]>([]);
  const [isChecking, setIsChecking] = useState(false);

  const checkApiStatus = async (endpoint: string, method: string = 'GET') => {
    const startTime = Date.now();

    try {
      const fetchOptions: RequestInit = {
        method,
        headers: { 'Content-Type': 'application/json' },
        signal: AbortSignal.timeout(5000), // 5 second timeout
      };

      // Add request body for POST endpoints
      if (method === 'POST') {
        fetchOptions.body = JSON.stringify({});
      }

      const response = await fetch(endpoint, fetchOptions);

      const responseTime = Date.now() - startTime;

      // Try to parse JSON response to get structured error information
      let responseData: any = null;
      const contentType = response.headers.get('content-type');

      if (contentType && contentType.includes('application/json')) {
        try {
          responseData = await response.json();
        } catch (parseError) {
          // If JSON parsing fails, use the raw text
          responseData = { error: await response.text() };
        }
      }

      if (response.ok) {
        return {
          status: 'available' as const,
          error: undefined,
          responseTime,
          errorCode: responseData?.code,
          errorDetails: responseData?.details
        };
      } else {
        // Parse structured error response from backend
        const errorMessage = responseData?.error || response.statusText || 'Unknown error';
        const errorCode = responseData?.code || `HTTP_${response.status}`;

        return {
          status: 'unavailable' as const,
          error: errorMessage,
          errorCode,
          errorDetails: responseData?.details,
          responseTime
        };
      }
    } catch (error) {
      const responseTime = Date.now() - startTime;
      const errorMessage = error instanceof Error ? error.message : 'Network error';

      return {
        status: 'error' as const,
        error: errorMessage,
        errorCode: 'NETWORK_ERROR',
        responseTime
      };
    }
  };

  const checkAllApis = async () => {
    setIsChecking(true);
    const statuses: ApiStatus[] = [];

    for (const api of API_ENDPOINTS) {
      const status: ApiStatus = {
        endpoint: api.endpoint,
        status: 'checking',
        lastChecked: new Date(),
      };
      statuses.push(status);
    }

    setApiStatuses(statuses);

    // Check each API
    for (let i = 0; i < API_ENDPOINTS.length; i++) {
      const api = API_ENDPOINTS[i];
      const result = await checkApiStatus(api.endpoint, api.method);
      setApiStatuses(prev => prev.map((status, index) =>
        index === i
          ? { ...status, ...result, lastChecked: new Date() }
          : status
      ));
    }

    setIsChecking(false);
  };

  useEffect(() => {
    checkAllApis();
  }, []);

  const getStatusIcon = (status: ApiStatus['status']) => {
    switch (status) {
      case 'checking':
        return <Loader2 className="w-4 h-4 animate-spin text-blue-500" />;
      case 'available':
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'unavailable':
        return <XCircle className="w-4 h-4 text-red-500" />;
      case 'error':
        return <AlertCircle className="w-4 h-4 text-yellow-500" />;
    }
  };

  const getStatusText = (status: ApiStatus['status']) => {
    switch (status) {
      case 'checking':
        return 'Checking...';
      case 'available':
        return 'Available';
      case 'unavailable':
        return 'Unavailable';
      case 'error':
        return 'Error';
    }
  };

  const getStatusColor = (status: ApiStatus['status']) => {
    switch (status) {
      case 'checking':
        return 'text-blue-600';
      case 'available':
        return 'text-green-600';
      case 'unavailable':
        return 'text-red-600';
      case 'error':
        return 'text-yellow-600';
    }
  };

  const availableCount = apiStatuses.filter(s => s.status === 'available').length;
  const totalCount = apiStatuses.length;

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          API Status Tracker
        </h3>
        <div className="flex items-center space-x-2">
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {availableCount}/{totalCount} Available
          </span>
          <button
            onClick={checkAllApis}
            disabled={isChecking}
            className="px-3 py-1 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded-md hover:bg-blue-200 dark:hover:bg-blue-900/50 disabled:opacity-50"
          >
            {isChecking ? 'Checking...' : 'Refresh'}
          </button>
        </div>
      </div>

      <div className="space-y-2">
        {API_ENDPOINTS.map((api, index) => {
          const status = apiStatuses[index];
          return (
            <div key={api.endpoint} className="flex items-center justify-between p-2 bg-gray-50 dark:bg-gray-700 rounded-md">
              <div className="flex items-center space-x-3">
                {status ? getStatusIcon(status.status) : <Loader2 className="w-4 h-4 animate-spin text-blue-500" />}
                <div>
                  <div className="font-medium text-gray-900 dark:text-gray-100">
                    {api.name}
                  </div>
                  <div className="text-xs text-gray-500 dark:text-gray-400">
                    {api.endpoint}
                  </div>
                  {status?.error && (
                    <div className="text-xs text-red-500 mt-1">
                      <div className="font-medium">{status.error}</div>
                      {status.errorCode && (
                        <div className="text-gray-500">Code: {status.errorCode}</div>
                      )}
                      {status.responseTime && (
                        <div className="text-gray-500">Response: {status.responseTime}ms</div>
                      )}
                    </div>
                  )}
                </div>
              </div>
              <div className={`text-sm font-medium ${status ? getStatusColor(status.status) : 'text-blue-600'}`}>
                {status ? getStatusText(status.status) : 'Checking...'}
              </div>
            </div>
          );
        })}
      </div>

      {availableCount === 0 && totalCount > 0 && (
        <div className="mt-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md">
          <div className="text-sm text-red-800 dark:text-red-200">
            <strong>Backend Services Unavailable:</strong> All backend APIs are currently offline or experiencing issues.
            This indicates the Living Codex backend server is not running or has connectivity problems.
          </div>
        </div>
      )}

      {availableCount > 0 && availableCount < totalCount && (
        <div className="mt-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-md">
          <div className="text-sm text-yellow-800 dark:text-yellow-200">
            <strong>Partial Service Availability:</strong> {availableCount} of {totalCount} APIs are responding correctly.
            Some features may be limited due to unavailable backend services.
          </div>
        </div>
      )}

      {availableCount === totalCount && totalCount > 0 && (
        <div className="mt-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-md">
          <div className="text-sm text-green-800 dark:text-green-200">
            <strong>All Backend Services Operational:</strong> All {totalCount} APIs are available and responding correctly.
            The Living Codex system is fully operational.
          </div>
        </div>
      )}
    </div>
  );
}

export default ApiStatusTracker;
