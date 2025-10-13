'use client';

/**
 * Events Browser
 * Real-time event streaming and history browser
 * Connects to EventStreamingModule for live updates and event replay
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { useRealtimeConnection } from '@/lib/realtime';
import { Activity, Clock, Filter, Play, Pause, RotateCcw, Zap, Radio } from 'lucide-react';

interface StreamEvent {
  eventId: string;
  eventType: string;
  entityType: string;
  entityId: string;
  timestamp: string;
  data: any;
  userId?: string;
}

interface EventSubscription {
  subscriptionId: string;
  eventTypes: string[];
  entityTypes: string[];
  entityIds: string[];
  filters: Record<string, any>;
  createdAt: string;
  isActive: boolean;
}

export default function EventsPage() {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'stream' | 'history' | 'subscriptions'>('stream');
  const [events, setEvents] = useState<StreamEvent[]>([]);
  const [subscriptions, setSubscriptions] = useState<EventSubscription[]>([]);
  const [loading, setLoading] = useState(false);
  
  // Stream controls
  const [isPaused, setIsPaused] = useState(false);
  const [autoScroll, setAutoScroll] = useState(true);
  
  // Filters
  const [eventTypeFilter, setEventTypeFilter] = useState<string>('');
  const [entityTypeFilter, setEntityTypeFilter] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  
  // Real-time connection
  const realtime = useRealtimeConnection({
    onMessage: (message) => {
      if (message.event === 'stream_event' && !isPaused) {
        setEvents(prev => [message.data, ...prev].slice(0, 500)); // Keep last 500
      }
    }
  });

  useEffect(() => {
    loadEventStream();
    loadSubscriptions();
  }, []);

  useEffect(() => {
    if (realtime.isConnected) {
      // Subscribe to event stream channel
      realtime.subscribe('events:stream');
    }
  }, [realtime.isConnected]);

  const loadEventStream = async () => {
    try {
      setLoading(true);
      const response = await api.get('/events/stream', {
        take: 100,
        skip: 0
      });

      if (response?.success && response?.events) {
        setEvents(response.events);
      }
    } catch (error) {
      console.error('Error loading event stream:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadEventHistory = async () => {
    try {
      setLoading(true);
      const response = await api.get('/events/history', {
        take: 200,
        skip: 0
      });

      if (response?.success && response?.events) {
        setEvents(response.events);
      }
    } catch (error) {
      console.error('Error loading event history:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadSubscriptions = async () => {
    try {
      const response = await api.get('/events/subscriptions');
      if (response?.success && response?.subscriptions) {
        setSubscriptions(response.subscriptions);
      }
    } catch (error) {
      console.error('Error loading subscriptions:', error);
    }
  };

  const createSubscription = async () => {
    if (!user?.id) return;

    try {
      const subscriptionId = `sub-${user.id}-${Date.now()}`;
      const response = await api.post('/events/subscribe', {
        subscriptionId,
        eventTypes: eventTypeFilter ? [eventTypeFilter] : [],
        entityTypes: entityTypeFilter ? [entityTypeFilter] : [],
        entityIds: [],
        filters: {}
      });

      if (response?.success) {
        await loadSubscriptions();
      }
    } catch (error) {
      console.error('Error creating subscription:', error);
    }
  };

  const deleteSubscription = async (subscriptionId: string) => {
    try {
      await api.delete(`/events/subscribe/${subscriptionId}`);
      await loadSubscriptions();
    } catch (error) {
      console.error('Error deleting subscription:', error);
    }
  };

  const getEventTypeIcon = (eventType: string) => {
    switch (eventType.toLowerCase()) {
      case 'node_created':
      case 'node_updated':
      case 'node_deleted':
        return '🔵';
      case 'edge_created':
      case 'edge_updated':
      case 'edge_deleted':
        return '🔗';
      case 'user_action':
        return '👤';
      case 'system_event':
        return '⚙️';
      default:
        return '📡';
    }
  };

  const getEventTypeColor = (eventType: string) => {
    if (eventType.includes('created')) return 'text-green-400';
    if (eventType.includes('updated')) return 'text-blue-400';
    if (eventType.includes('deleted')) return 'text-red-400';
    return 'text-gray-400';
  };

  const filteredEvents = events.filter(event => {
    if (eventTypeFilter && event.eventType !== eventTypeFilter) return false;
    if (entityTypeFilter && event.entityType !== entityTypeFilter) return false;
    if (searchQuery && !JSON.stringify(event).toLowerCase().includes(searchQuery.toLowerCase())) return false;
    return true;
  });

  const eventTypes = Array.from(new Set(events.map(e => e.eventType)));
  const entityTypes = Array.from(new Set(events.map(e => e.entityType)));

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent flex items-center gap-3">
              <Radio className="w-10 h-10 text-blue-400" />
              Event Stream Browser
            </h1>
            <p className="text-gray-400 mt-2">
              Real-time event monitoring and history
            </p>
          </div>
          <div className="flex items-center gap-3">
            <div className={`flex items-center gap-2 px-3 py-1 rounded-lg ${realtime.isConnected ? 'bg-green-900/30 text-green-400' : 'bg-gray-800 text-gray-500'}`}>
              <div className={`w-2 h-2 rounded-full ${realtime.isConnected ? 'bg-green-400 animate-pulse' : 'bg-gray-500'}`}></div>
              {realtime.isConnected ? 'Live' : 'Offline'}
            </div>
            <button
              onClick={() => setIsPaused(!isPaused)}
              className={`p-2 rounded-lg ${isPaused ? 'bg-orange-600 hover:bg-orange-700' : 'bg-blue-600 hover:bg-blue-700'} transition-colors`}
              title={isPaused ? 'Resume' : 'Pause'}
            >
              {isPaused ? <Play className="w-5 h-5" /> : <Pause className="w-5 h-5" />}
            </button>
            <button
              onClick={activeTab === 'history' ? loadEventHistory : loadEventStream}
              className="p-2 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors"
              title="Refresh"
            >
              <RotateCcw className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700">
          {[
            { id: 'stream', label: 'Live Stream', icon: Zap },
            { id: 'history', label: 'Event History', icon: Clock },
            { id: 'subscriptions', label: 'Subscriptions', icon: Filter }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => {
                setActiveTab(tab.id as any);
                if (tab.id === 'history') loadEventHistory();
                if (tab.id === 'stream') loadEventStream();
              }}
              className={`flex items-center gap-2 px-4 py-2 border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-blue-400 text-blue-400'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              }`}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
            </button>
          ))}
        </div>

        {/* Filters */}
        <Card className="p-4">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="block text-sm text-gray-400 mb-1">Event Type</label>
              <select
                value={eventTypeFilter}
                onChange={(e) => setEventTypeFilter(e.target.value)}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-gray-200"
              >
                <option value="">All Types</option>
                {eventTypes.map(type => (
                  <option key={type} value={type}>{type}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-400 mb-1">Entity Type</label>
              <select
                value={entityTypeFilter}
                onChange={(e) => setEntityTypeFilter(e.target.value)}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-gray-200"
              >
                <option value="">All Entities</option>
                {entityTypes.map(type => (
                  <option key={type} value={type}>{type}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-400 mb-1">Search</label>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search events..."
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-gray-200"
              />
            </div>
            <div className="flex items-end">
              <button
                onClick={() => {
                  setEventTypeFilter('');
                  setEntityTypeFilter('');
                  setSearchQuery('');
                }}
                className="w-full px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg transition-colors"
              >
                Clear Filters
              </button>
            </div>
          </div>
        </Card>

        {/* Content */}
        {activeTab === 'stream' || activeTab === 'history' ? (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-xl font-semibold text-gray-200">
                {activeTab === 'stream' ? '⚡ Live Event Stream' : '📜 Event History'}
                <span className="text-sm text-gray-500 ml-2">({filteredEvents.length} events)</span>
              </h3>
            </div>

            <div className="space-y-2 max-h-[600px] overflow-y-auto">
              {loading ? (
                <div className="text-center py-12">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-400 mx-auto"></div>
                  <p className="text-gray-400 mt-4">Loading events...</p>
                </div>
              ) : filteredEvents.length > 0 ? (
                filteredEvents.map((event, index) => (
                  <div
                    key={`${event.eventId}-${index}`}
                    className="p-4 bg-gray-800/50 rounded-lg border border-gray-700 hover:border-gray-600 transition-colors"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3 flex-1">
                        <div className="text-2xl">{getEventTypeIcon(event.eventType)}</div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <span className={`font-medium ${getEventTypeColor(event.eventType)}`}>
                              {event.eventType}
                            </span>
                            <span className="text-xs text-gray-500">•</span>
                            <span className="text-sm text-gray-400">{event.entityType}</span>
                          </div>
                          <div className="text-sm text-gray-500 mb-2">
                            Entity ID: {event.entityId}
                          </div>
                          {event.data && (
                            <div className="text-xs text-gray-400 font-mono bg-gray-900/50 p-2 rounded">
                              {JSON.stringify(event.data, null, 2)}
                            </div>
                          )}
                        </div>
                      </div>
                      <div className="text-xs text-gray-500 text-right">
                        <div>{new Date(event.timestamp).toLocaleTimeString()}</div>
                        <div>{new Date(event.timestamp).toLocaleDateString()}</div>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 text-gray-500">
                  <Activity className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No events found</p>
                  <p className="text-sm mt-2">Try adjusting your filters or wait for new events</p>
                </div>
              )}
            </div>
          </Card>
        ) : (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-xl font-semibold text-gray-200">
                🔔 Event Subscriptions ({subscriptions.length})
              </h3>
              <button
                onClick={createSubscription}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
              >
                + Create Subscription
              </button>
            </div>

            <div className="space-y-3">
              {subscriptions.length > 0 ? (
                subscriptions.map((sub) => (
                  <div
                    key={sub.subscriptionId}
                    className="p-4 bg-gray-800/50 rounded-lg border border-gray-700"
                  >
                    <div className="flex items-start justify-between">
                      <div>
                        <div className="font-medium text-gray-200 mb-2">{sub.subscriptionId}</div>
                        <div className="space-y-1 text-sm text-gray-400">
                          {sub.eventTypes.length > 0 && (
                            <div>Event Types: {sub.eventTypes.join(', ')}</div>
                          )}
                          {sub.entityTypes.length > 0 && (
                            <div>Entity Types: {sub.entityTypes.join(', ')}</div>
                          )}
                          <div className="text-xs text-gray-500">
                            Created: {new Date(sub.createdAt).toLocaleString()}
                          </div>
                        </div>
                      </div>
                      <button
                        onClick={() => deleteSubscription(sub.subscriptionId)}
                        className="px-3 py-1 bg-red-600 hover:bg-red-700 rounded text-sm transition-colors"
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 text-gray-500">
                  <Filter className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No active subscriptions</p>
                  <p className="text-sm mt-2">Create a subscription to filter events</p>
                </div>
              )}
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

