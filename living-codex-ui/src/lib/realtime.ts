import { useEffect, useRef, useState, useCallback } from 'react';
import { config } from './config';

export interface RealtimeMessage {
  type: string;
  event: string;
  data: any;
  timestamp: string;
}

export interface RealtimeOptions {
  onMessage?: (message: RealtimeMessage) => void;
  onConnect?: () => void;
  onDisconnect?: () => void;
  onError?: (error: Event) => void;
  reconnectInterval?: number;
  maxReconnectAttempts?: number;
}

/**
 * Custom hook for WebSocket real-time communication
 */
export function useRealtimeConnection(options: RealtimeOptions = {}) {
  const {
    onMessage,
    onConnect,
    onDisconnect,
    onError,
    reconnectInterval = 3000,
    maxReconnectAttempts = 5
  } = options;

  const wsRef = useRef<WebSocket | null>(null);
  const reconnectAttemptsRef = useRef(0);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  
  const [isConnected, setIsConnected] = useState(false);
  const [connectionState, setConnectionState] = useState<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');

  const connect = useCallback(() => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      return;
    }

    const wsUrl = `${config.backend.wsUrl || 'ws://localhost:5002'}/ws`;
    console.log('Connecting to WebSocket:', wsUrl);
    
    const ws = new WebSocket(wsUrl);
    wsRef.current = ws;
    setConnectionState('connecting');

    ws.onopen = () => {
      console.log('WebSocket connected');
      setIsConnected(true);
      setConnectionState('connected');
      reconnectAttemptsRef.current = 0;
      onConnect?.();
    };

    ws.onmessage = (event) => {
      try {
        const message: RealtimeMessage = JSON.parse(event.data);
        onMessage?.(message);
      } catch (error) {
        console.error('Error parsing WebSocket message:', error);
      }
    };

    ws.onerror = (error) => {
      console.error('WebSocket error:', error);
      setConnectionState('error');
      onError?.(error);
    };

    ws.onclose = () => {
      console.log('WebSocket disconnected');
      setIsConnected(false);
      setConnectionState('disconnected');
      wsRef.current = null;
      onDisconnect?.();

      // Attempt to reconnect
      if (reconnectAttemptsRef.current < maxReconnectAttempts) {
        reconnectAttemptsRef.current++;
        console.log(`Reconnecting in ${reconnectInterval}ms (attempt ${reconnectAttemptsRef.current}/${maxReconnectAttempts})`);
        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, reconnectInterval);
      } else {
        console.log('Max reconnect attempts reached');
      }
    };
  }, [onMessage, onConnect, onDisconnect, onError, reconnectInterval, maxReconnectAttempts]);

  const disconnect = useCallback(() => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
      reconnectTimeoutRef.current = null;
    }
    if (wsRef.current) {
      wsRef.current.close();
      wsRef.current = null;
    }
    setIsConnected(false);
    setConnectionState('disconnected');
  }, []);

  const sendMessage = useCallback((message: any) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(JSON.stringify(message));
    } else {
      console.warn('WebSocket not connected, cannot send message');
    }
  }, []);

  const subscribe = useCallback((channel: string) => {
    sendMessage({
      action: 'subscribe',
      channel
    });
  }, [sendMessage]);

  const unsubscribe = useCallback((channel: string) => {
    sendMessage({
      action: 'unsubscribe',
      channel
    });
  }, [sendMessage]);

  useEffect(() => {
    connect();

    return () => {
      disconnect();
    };
  }, [connect, disconnect]);

  return {
    isConnected,
    connectionState,
    sendMessage,
    subscribe,
    unsubscribe,
    connect,
    disconnect
  };
}

/**
 * Hook for real-time notifications
 */
export function useRealtimeNotifications(userId?: string) {
  const [notifications, setNotifications] = useState<any[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  const handleMessage = useCallback((message: RealtimeMessage) => {
    if (message.event === 'notification') {
      setNotifications(prev => [message.data, ...prev]);
      setUnreadCount(prev => prev + 1);
    }
  }, []);

  const realtime = useRealtimeConnection({
    onMessage: handleMessage,
    onConnect: () => {
      console.log('Real-time notifications connected');
    }
  });

  useEffect(() => {
    if (realtime.isConnected && userId) {
      // Subscribe to user's notification channel
      realtime.subscribe(`notifications:${userId}`);
      
      return () => {
        realtime.unsubscribe(`notifications:${userId}`);
      };
    }
  }, [realtime.isConnected, userId, realtime]);

  const markAsRead = useCallback((notificationId: string) => {
    setNotifications(prev => 
      prev.map(n => n.id === notificationId ? { ...n, read: true } : n)
    );
    setUnreadCount(prev => Math.max(0, prev - 1));
  }, []);

  const clearAll = useCallback(() => {
    setNotifications([]);
    setUnreadCount(0);
  }, []);

  return {
    notifications,
    unreadCount,
    markAsRead,
    clearAll,
    isConnected: realtime.isConnected,
    connectionState: realtime.connectionState
  };
}

/**
 * Hook for real-time activity feed
 */
export function useRealtimeActivity(filter?: string) {
  const [activities, setActivities] = useState<any[]>([]);

  const handleMessage = useCallback((message: RealtimeMessage) => {
    if (message.event === 'activity') {
      setActivities(prev => [message.data, ...prev].slice(0, 100)); // Keep last 100
    }
  }, []);

  const realtime = useRealtimeConnection({
    onMessage: handleMessage
  });

  useEffect(() => {
    if (realtime.isConnected) {
      const channel = filter ? `activity:${filter}` : 'activity:global';
      realtime.subscribe(channel);
      
      return () => {
        realtime.unsubscribe(channel);
      };
    }
  }, [realtime.isConnected, filter, realtime]);

  return {
    activities,
    isConnected: realtime.isConnected,
    connectionState: realtime.connectionState
  };
}

