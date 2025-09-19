import { useEffect, useState, useCallback } from 'react';
import { endpoints } from './api';

interface HotReloadEvent {
  type: string;
  componentId: string;
  timestamp: string;
  success: boolean;
  details: string;
}

interface HotReloadStatus {
  isWatching: boolean;
  watchedPaths: number;
  componentCount: number;
  recentEvents: HotReloadEvent[];
}

export function useHotReload() {
  const [status, setStatus] = useState<HotReloadStatus | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [events, setEvents] = useState<HotReloadEvent[]>([]);

  // Fetch hot-reload status
  const fetchStatus = useCallback(async () => {
    try {
      const response = await fetch('http://localhost:5002/self-update/hot-reload-status');
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          setStatus(data);
          setEvents(data.recentEvents || []);
          setIsConnected(true);
        }
      }
    } catch (error) {
      console.error('Error fetching hot-reload status:', error);
      setIsConnected(false);
    }
  }, []);

  // Start watching
  const startWatching = useCallback(async (config?: {
    paths?: string[];
    extensions?: string[];
    autoRegenerate?: boolean;
  }) => {
    try {
      const response = await fetch('http://localhost:5002/self-update/start-watching', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config || {})
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await fetchStatus();
          return { success: true };
        }
      }
      return { success: false, error: 'Failed to start watching' };
    } catch (error) {
      console.error('Error starting hot-reload watching:', error);
      return { success: false, error: 'Network error' };
    }
  }, [fetchStatus]);

  // Stop watching
  const stopWatching = useCallback(async () => {
    try {
      const response = await fetch('http://localhost:5002/self-update/stop-watching', {
        method: 'POST'
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await fetchStatus();
          return { success: true };
        }
      }
      return { success: false, error: 'Failed to stop watching' };
    } catch (error) {
      console.error('Error stopping hot-reload watching:', error);
      return { success: false, error: 'Network error' };
    }
  }, [fetchStatus]);

  // Regenerate component
  const regenerateComponent = useCallback(async (componentId: string, options?: {
    lensSpec?: string;
    componentType?: string;
    requirements?: string;
    provider?: string;
    model?: string;
  }) => {
    try {
      const response = await fetch('http://localhost:5002/self-update/regenerate-component', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          componentId,
          lensSpec: options?.lensSpec || 'Enhanced component with improved UX',
          componentType: options?.componentType || 'list',
          requirements: options?.requirements || 'TypeScript + Tailwind, modern design, accessibility',
          provider: options?.provider || 'openai',
          model: options?.model || 'gpt-5-codex'
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await fetchStatus();
          return { 
            success: true, 
            generatedCode: data.generatedCode,
            aiProvider: data.aiProvider,
            aiModel: data.aiModel
          };
        }
      }
      return { success: false, error: 'Failed to regenerate component' };
    } catch (error) {
      console.error('Error regenerating component:', error);
      return { success: false, error: 'Network error' };
    }
  }, [fetchStatus]);

  // Hot-swap component
  const hotSwapComponent = useCallback(async (componentPath: string, newCode: string) => {
    try {
      const response = await fetch('http://localhost:5002/self-update/hot-swap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          componentPath,
          newCode,
          createBackup: true
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await fetchStatus();
          return { 
            success: true, 
            backupPath: data.backupPath 
          };
        }
      }
      return { success: false, error: 'Failed to hot-swap component' };
    } catch (error) {
      console.error('Error hot-swapping component:', error);
      return { success: false, error: 'Network error' };
    }
  }, [fetchStatus]);

  // Get history
  const getHistory = useCallback(async (limit: number = 50) => {
    try {
      const response = await fetch(`http://localhost:5002/self-update/hot-reload-history?limit=${limit}`);
      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          return { success: true, events: data.events };
        }
      }
      return { success: false, error: 'Failed to get history' };
    } catch (error) {
      console.error('Error getting hot-reload history:', error);
      return { success: false, error: 'Network error' };
    }
  }, []);

  // Initialize and set up polling
  useEffect(() => {
    fetchStatus();
    
    // Poll for status updates every 5 seconds
    const interval = setInterval(fetchStatus, 5000);
    
    return () => clearInterval(interval);
  }, [fetchStatus]);

  return {
    status,
    isConnected,
    events,
    startWatching,
    stopWatching,
    regenerateComponent,
    hotSwapComponent,
    getHistory,
    refresh: fetchStatus
  };
}

export function useHotReloadNotifications() {
  const [notifications, setNotifications] = useState<HotReloadEvent[]>([]);

  useEffect(() => {
    // In a real implementation, this would connect to WebSocket/SSE
    // For now, we'll poll for recent events
    const pollForEvents = async () => {
      try {
        const response = await fetch('http://localhost:5002/self-update/hot-reload-history?limit=5');
        if (response.ok) {
          const data = await response.json();
          if (data.success && data.events) {
            setNotifications(data.events);
          }
        }
      } catch (error) {
        console.error('Error polling for hot-reload events:', error);
      }
    };

    const interval = setInterval(pollForEvents, 2000);
    return () => clearInterval(interval);
  }, []);

  const dismissNotification = useCallback((timestamp: string) => {
    setNotifications(prev => prev.filter(n => n.timestamp !== timestamp));
  }, []);

  return {
    notifications,
    dismissNotification
  };
}
