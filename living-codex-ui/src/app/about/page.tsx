'use client';

import { useState, useEffect } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { api, ApiLogger } from '@/lib/api';

export default function AboutPage() {
  const [healthStatus, setHealthStatus] = useState<Record<string, unknown> | null>(null);
  const [apiLogs, setApiLogs] = useState<Array<Record<string, unknown>>>([]);
  const [showLogs, setShowLogs] = useState(false);

  useEffect(() => {
    // Check backend health
    api.health().then(response => {
      if (response.success) {
        setHealthStatus(response.data as Record<string, unknown>);
      }
    });

    // Update logs every 5 seconds
    const interval = setInterval(() => {
      setApiLogs(ApiLogger.getLogs() as Array<Record<string, unknown>>);
    }, 5000);

    return () => clearInterval(interval);
  }, []);

  const failedCalls = ApiLogger.getFailedCalls();
  const slowCalls = ApiLogger.getSlowCalls(3000); // Calls over 3 seconds

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">Living Codex</h1>
            </div>
            <Navigation />
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Page Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            About Living Codex
          </h1>
          <p className="text-gray-600">
            A fractal knowledge system where everything is a node
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* About Content */}
          <div className="space-y-6">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">
                🌐 Everything is a Node
              </h2>
              <p className="text-gray-600 mb-4">
                Living Codex implements a fractal knowledge architecture where all data, 
                structure, flow, state, deltas, policies, and specs exist as nodes with edges.
              </p>
              <ul className="space-y-2 text-sm text-gray-600">
                <li>• <strong>Ice Nodes:</strong> Immutable, persistent knowledge stored in federated storage</li>
                <li>• <strong>Water Nodes:</strong> Mutable, semi-persistent data in local cache</li>
                <li>• <strong>Gas Nodes:</strong> Transient, derivable information generated on-demand</li>
              </ul>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">
                🔮 Resonance Architecture
              </h2>
              <p className="text-gray-600 mb-4">
                The system uses resonance patterns to connect related concepts, people, and ideas.
                Contributions amplify energy and strengthen connections across the knowledge graph.
              </p>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="font-medium text-gray-900">Meta-Nodes</div>
                  <div className="text-gray-600">Describe structure and relationships</div>
                </div>
                <div>
                  <div className="font-medium text-gray-900">Tiny Deltas</div>
                  <div className="text-gray-600">Minimal patches preserve history</div>
                </div>
                <div>
                  <div className="font-medium text-gray-900">Single Lifecycle</div>
                  <div className="text-gray-600">Compose → Expand → Validate → Contract</div>
                </div>
                <div>
                  <div className="font-medium text-gray-900">Deterministic</div>
                  <div className="text-gray-600">APIs derive from meta-nodes</div>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">
                🚀 Technology Stack
              </h2>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="font-medium text-gray-900">Backend</div>
                  <ul className="text-gray-600 space-y-1">
                    <li>• .NET 6 / C#</li>
                    <li>• PostgreSQL (Ice)</li>
                    <li>• SQLite (Water)</li>
                    <li>• Module Architecture</li>
                  </ul>
                </div>
                <div>
                  <div className="font-medium text-gray-900">Frontend</div>
                  <ul className="text-gray-600 space-y-1">
                    <li>• Next.js 14</li>
                    <li>• TypeScript</li>
                    <li>• Tailwind CSS</li>
                    <li>• Real-time Updates</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>

          {/* System Status */}
          <div className="space-y-6">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">
                📊 System Status
              </h2>
              
              {healthStatus ? (
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Backend Status</span>
                    <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      ✅ Healthy
                    </span>
                  </div>
                  
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <div className="text-gray-600">Total Nodes</div>
                      <div className="font-medium">{Number(healthStatus.totalNodes)?.toLocaleString() || 'N/A'}</div>
                    </div>
                    <div>
                      <div className="text-gray-600">Modules Loaded</div>
                      <div className="font-medium">{String(healthStatus.modulesLoaded) || 'N/A'}</div>
                    </div>
                    <div>
                      <div className="text-gray-600">API Routes</div>
                      <div className="font-medium">{String(healthStatus.apiRoutes) || 'N/A'}</div>
                    </div>
                    <div>
                      <div className="text-gray-600">Uptime</div>
                      <div className="font-medium">{String(healthStatus.uptime) || 'N/A'}</div>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-600">Backend Status</span>
                  <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
                    ❌ Disconnected
                  </span>
                </div>
              )}
            </div>

            {/* API Call Monitoring */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold text-gray-900">
                  🔍 API Monitoring
                </h2>
                <button
                  onClick={() => setShowLogs(!showLogs)}
                  className="text-sm text-blue-600 hover:text-blue-800"
                >
                  {showLogs ? 'Hide' : 'Show'} Logs
                </button>
              </div>

              <div className="grid grid-cols-3 gap-4 mb-4">
                <div className="text-center">
                  <div className="text-2xl font-bold text-gray-900">{apiLogs.length}</div>
                  <div className="text-sm text-gray-600">Total Calls</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-red-600">{failedCalls.length}</div>
                  <div className="text-sm text-gray-600">Failed</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-orange-600">{slowCalls.length}</div>
                  <div className="text-sm text-gray-600">Slow (&gt;3s)</div>
                </div>
              </div>

              {(failedCalls.length > 0 || slowCalls.length > 0) && (
                <div className="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                  <div className="text-sm text-yellow-800">
                    ⚠️ {failedCalls.length} failed calls and {slowCalls.length} slow calls detected.
                    Check logs below for details.
                  </div>
                </div>
              )}

              {showLogs && (
                <div className="mt-4">
                  <div className="bg-gray-50 rounded-lg p-4 max-h-64 overflow-y-auto">
                    <div className="text-xs font-mono space-y-1">
                      {apiLogs.slice(-20).reverse().map((log, index) => (
                        <div key={index} className={`${
                          log.status === 'error' ? 'text-red-600' :
                          log.status === 'timeout' ? 'text-orange-600' :
                          Number(log.duration) > 3000 ? 'text-yellow-600' :
                          'text-gray-600'
                        }`}>
                          <span className="text-gray-400">{new Date(String(log.timestamp)).toLocaleTimeString()}</span>
                          {' '}
                          <span className="font-medium">{String(log.method)}</span>
                          {' '}
                          <span>{String(log.url)}</span>
                          {' '}
                          <span>({String(log.duration)}ms)</span>
                          {log.error ? <span className="text-red-500"> - {String(log.error)}</span> : null}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
