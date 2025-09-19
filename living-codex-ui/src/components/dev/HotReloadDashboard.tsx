'use client';

import { useState } from 'react';
import { useHotReload, useHotReloadNotifications } from '@/lib/hot-reload';

export function HotReloadDashboard() {
  const {
    status,
    isConnected,
    events,
    startWatching,
    stopWatching,
    regenerateComponent,
    hotSwapComponent,
    getHistory,
    refresh
  } = useHotReload();

  const { notifications, dismissNotification } = useHotReloadNotifications();

  const [componentId, setComponentId] = useState('');
  const [lensSpec, setLensSpec] = useState('');
  const [regenerating, setRegenerating] = useState(false);
  const [showHistory, setShowHistory] = useState(false);

  const handleStartWatching = async () => {
    const result = await startWatching({
      autoRegenerate: true
    });
    if (!result.success) {
      alert('Failed to start watching: ' + result.error);
    }
  };

  const handleStopWatching = async () => {
    const result = await stopWatching();
    if (!result.success) {
      alert('Failed to stop watching: ' + result.error);
    }
  };

  const handleRegenerate = async () => {
    if (!componentId.trim()) {
      alert('Please enter a component ID');
      return;
    }

    setRegenerating(true);
    try {
      const result = await regenerateComponent(componentId, {
        lensSpec: lensSpec || 'Enhanced component with improved UX',
        provider: 'openai',
        model: 'gpt-5-codex'
      });

      if (result.success) {
        alert(`Component regenerated successfully using ${result.aiProvider}/${result.aiModel}`);
      } else {
        alert('Failed to regenerate: ' + result.error);
      }
    } finally {
      setRegenerating(false);
    }
  };

  const getEventIcon = (type: string) => {
    const icons: Record<string, string> = {
      'ui-component-changed': '🔄',
      'ui-component-created': '✨',
      'spec-changed': '📋',
      'module-changed': '📦',
      'component-regeneration': '🤖',
      'component-hot-swap': '🔥'
    };
    return icons[type] || '📝';
  };

  const getEventColor = (type: string) => {
    const colors: Record<string, string> = {
      'ui-component-changed': 'blue',
      'ui-component-created': 'green',
      'spec-changed': 'purple',
      'module-changed': 'orange',
      'component-regeneration': 'indigo',
      'component-hot-swap': 'red'
    };
    return colors[type] || 'gray';
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-900">🔥 Hot Reload Dashboard</h2>
        <div className="flex items-center space-x-2">
          <div className={`w-3 h-3 rounded-full ${isConnected ? 'bg-green-500' : 'bg-red-500'}`}></div>
          <span className="text-sm text-gray-600">
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      </div>

      {/* Status Overview */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-blue-50 rounded-lg p-4">
          <div className="text-blue-600 text-sm font-medium">Watching</div>
          <div className="text-2xl font-bold text-blue-900">
            {status?.isWatching ? 'Active' : 'Inactive'}
          </div>
        </div>
        <div className="bg-green-50 rounded-lg p-4">
          <div className="text-green-600 text-sm font-medium">Watched Paths</div>
          <div className="text-2xl font-bold text-green-900">
            {status?.watchedPaths || 0}
          </div>
        </div>
        <div className="bg-purple-50 rounded-lg p-4">
          <div className="text-purple-600 text-sm font-medium">Components</div>
          <div className="text-2xl font-bold text-purple-900">
            {status?.componentCount || 0}
          </div>
        </div>
        <div className="bg-orange-50 rounded-lg p-4">
          <div className="text-orange-600 text-sm font-medium">Recent Events</div>
          <div className="text-2xl font-bold text-orange-900">
            {events.length}
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
        {/* Watching Controls */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-900">File Watching</h3>
          <div className="flex space-x-3">
            <button
              onClick={handleStartWatching}
              disabled={status?.isWatching}
              className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              ▶️ Start Watching
            </button>
            <button
              onClick={handleStopWatching}
              disabled={!status?.isWatching}
              className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              ⏹️ Stop Watching
            </button>
            <button
              onClick={refresh}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
            >
              🔄 Refresh
            </button>
          </div>
        </div>

        {/* Component Regeneration */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-900">AI Regeneration</h3>
          <div className="space-y-2">
            <input
              type="text"
              value={componentId}
              onChange={(e) => setComponentId(e.target.value)}
              placeholder="Component ID (e.g., NewsCard)"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <input
              type="text"
              value={lensSpec}
              onChange={(e) => setLensSpec(e.target.value)}
              placeholder="Lens spec (optional)"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              onClick={handleRegenerate}
              disabled={regenerating || !componentId.trim()}
              className="w-full px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {regenerating ? '🤖 Regenerating...' : '🤖 AI Regenerate'}
            </button>
          </div>
        </div>
      </div>

      {/* Live Notifications */}
      {notifications.length > 0 && (
        <div className="mb-6">
          <h3 className="text-lg font-medium text-gray-900 mb-3">🔔 Live Notifications</h3>
          <div className="space-y-2">
            {notifications.slice(0, 3).map((notification, index) => (
              <div
                key={index}
                className={`flex items-center justify-between p-3 rounded-lg bg-${getEventColor(notification.type)}-50 border border-${getEventColor(notification.type)}-200`}
              >
                <div className="flex items-center space-x-3">
                  <span className="text-xl">{getEventIcon(notification.type)}</span>
                  <div>
                    <div className="font-medium text-gray-900">
                      {notification.componentId}
                    </div>
                    <div className="text-sm text-gray-600">
                      {notification.details}
                    </div>
                  </div>
                </div>
                <button
                  onClick={() => dismissNotification(notification.timestamp)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Recent Events */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-medium text-gray-900">📊 Recent Events</h3>
          <button
            onClick={() => setShowHistory(!showHistory)}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            {showHistory ? 'Hide History' : 'Show History'}
          </button>
        </div>

        {showHistory && (
          <div className="max-h-64 overflow-y-auto">
            {events.length > 0 ? (
              <div className="space-y-2">
                {events.map((event, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                  >
                    <div className="flex items-center space-x-3">
                      <span className="text-lg">{getEventIcon(event.type)}</span>
                      <div>
                        <div className="font-medium text-gray-900">
                          {event.componentId}
                        </div>
                        <div className="text-sm text-gray-600">
                          {event.type} • {new Date(event.timestamp).toLocaleTimeString()}
                        </div>
                      </div>
                    </div>
                    <div className={`px-2 py-1 rounded-md text-xs font-medium ${
                      event.success 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {event.success ? 'Success' : 'Failed'}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No events yet. Start watching to see real-time updates!
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// Helper functions (duplicated from the main component for clarity)
function getEventIcon(type: string) {
  const icons: Record<string, string> = {
    'ui-component-changed': '🔄',
    'ui-component-created': '✨',
    'spec-changed': '📋',
    'module-changed': '📦',
    'component-regeneration': '🤖',
    'component-hot-swap': '🔥'
  };
  return icons[type] || '📝';
}

function getEventColor(type: string) {
  const colors: Record<string, string> = {
    'ui-component-changed': 'blue',
    'ui-component-created': 'green',
    'spec-changed': 'purple',
    'module-changed': 'orange',
    'component-regeneration': 'indigo',
    'component-hot-swap': 'red'
  };
  return colors[type] || 'gray';
}
