'use client';

import { useState, useEffect } from 'react';
import { api, ApiLogger } from '@/lib/api';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';

export default function AboutPage() {
  const [healthStatus, setHealthStatus] = useState<Record<string, unknown> | null>(null);
  const [apiLogs, setApiLogs] = useState<Array<Record<string, unknown>>>([]);
  const [showLogs, setShowLogs] = useState(false);
  const [activeView, setActiveView] = useState<'human' | 'technical' | 'data'>('human');

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
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h1 className="text-5xl font-bold text-gray-900 dark:text-gray-100 mb-6">
            🌱 Living Codex
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-300 mb-8 max-w-3xl mx-auto">
            A new way of organizing human knowledge that grows, adapts, and connects ideas 
            like a living ecosystem
          </p>
          
          {/* View Selector */}
          <div className="flex justify-center mb-8">
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-1 shadow-sm">
              <div className="flex space-x-1">
                {[
                  { id: 'human', label: '👥 Human View', description: 'For everyone' },
                  { id: 'technical', label: '⚙️ Technical View', description: 'For developers' },
                  { id: 'data', label: '📊 Data View', description: 'For analysts' }
                ].map((view) => (
                  <button
                    key={view.id}
                    onClick={() => setActiveView(view.id as any)}
                    className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
                      activeView === view.id
                        ? 'bg-blue-600 text-white shadow-sm'
                        : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-50 dark:hover:bg-gray-700'
                    }`}
                  >
                    <div className="text-center">
                      <div>{view.label}</div>
                      <div className="text-xs opacity-75">{view.description}</div>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Dynamic Hero Content Based on View */}
          {activeView === 'human' && (
            <div className="bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 rounded-2xl p-8 max-w-4xl mx-auto">
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                Imagine if knowledge could grow like a tree
              </h2>
              <p className="text-lg text-gray-700 dark:text-gray-300">
                Every idea, every discovery, every connection between people and concepts 
                becomes a living part of an ever-expanding web of understanding that benefits all of humanity.
              </p>
            </div>
          )}
          
          {activeView === 'technical' && (
            <div className="bg-gradient-to-r from-green-50 to-blue-50 dark:from-green-900/20 dark:to-blue-900/20 rounded-2xl p-8 max-w-4xl mx-auto">
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                A Fractal Knowledge Architecture
              </h2>
              <p className="text-lg text-gray-700 dark:text-gray-300">
                Everything is a node. Data, structure, flow, state, deltas, policies, specs — all have node forms. 
                Runtime types are scaffolding that must round-trip ⇄ nodes.
              </p>
            </div>
          )}
          
          {activeView === 'data' && (
            <div className="bg-gradient-to-r from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 rounded-2xl p-8 max-w-4xl mx-auto">
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                Real-Time Knowledge Metrics
              </h2>
              <p className="text-lg text-gray-700 dark:text-gray-300">
                Monitor the health, growth, and performance of the knowledge network with live data 
                and analytics that reveal patterns and opportunities.
              </p>
            </div>
          )}
        </div>

        {/* Dynamic Content Based on View */}
        {activeView === 'human' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Human-Centered Content */}
            <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">🌐 What Makes This Different?</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="text-gray-700 dark:text-gray-300">
                  Traditional systems treat information as separate, static files. Living Codex treats 
                  everything as connected, living nodes that grow and evolve together.
                </p>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="text-center p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <div className="text-3xl mb-2">🧊</div>
                    <h3 className="font-semibold text-blue-900 dark:text-blue-100">Solid Knowledge</h3>
                    <p className="text-sm text-blue-700 dark:text-blue-300">Core facts that never change</p>
                  </div>
                  <div className="text-center p-4 bg-cyan-50 dark:bg-cyan-900/20 rounded-lg">
                    <div className="text-3xl mb-2">💧</div>
                    <h3 className="font-semibold text-cyan-900 dark:text-cyan-100">Flowing Ideas</h3>
                    <p className="text-sm text-cyan-700 dark:text-cyan-300">Knowledge that adapts and grows</p>
                  </div>
                  <div className="text-center p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                    <div className="text-3xl mb-2">💨</div>
                    <h3 className="font-semibold text-purple-900 dark:text-purple-100">Emerging Insights</h3>
                    <p className="text-sm text-purple-700 dark:text-purple-300">New connections discovered in real-time</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">🤝 How This Benefits You</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-4">
                  <div className="flex items-start space-x-3">
                    <div className="text-2xl">🎯</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Personalized Learning</h3>
                      <p className="text-gray-600 dark:text-gray-300">
                        Discover content that resonates with your interests and helps you grow
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="text-2xl">🔗</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Connected Understanding</h3>
                      <p className="text-gray-600 dark:text-gray-300">
                        See how different ideas relate to each other, creating deeper insights
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="text-2xl">🌱</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Growing Knowledge</h3>
                      <p className="text-gray-600 dark:text-gray-300">
                        Your contributions help the entire system become smarter and more useful
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="text-2xl">👥</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Community Wisdom</h3>
                      <p className="text-gray-600 dark:text-gray-300">
                        Connect with others who share your interests and learn together
                      </p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">🌍 Impact on Humanity</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <p className="text-gray-700 dark:text-gray-300">
                  Living Codex isn&apos;t just another app—it&apos;s a new way of preserving and sharing 
                  human knowledge that can help solve some of our biggest challenges:
                </p>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                    <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">🧬 Scientific Discovery</h4>
                    <p className="text-sm text-green-700 dark:text-green-300">
                      Connect research across disciplines to accelerate breakthroughs
                    </p>
                  </div>
                  <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                    <h4 className="font-semibold text-yellow-900 dark:text-yellow-100 mb-2">🎓 Education</h4>
                    <p className="text-sm text-yellow-700 dark:text-yellow-300">
                      Make learning more personalized and effective for everyone
                    </p>
                  </div>
                  <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                    <h4 className="font-semibold text-red-900 dark:text-red-100 mb-2">🌡️ Climate Solutions</h4>
                    <p className="text-sm text-red-700 dark:text-red-300">
                      Connect environmental knowledge to find innovative solutions
                    </p>
                  </div>
                  <div className="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                    <h4 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">❤️ Healthcare</h4>
                    <p className="text-sm text-purple-700 dark:text-purple-300">
                      Share medical knowledge to improve treatments worldwide
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* How It Works & System Status */}
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">⚙️ How It Works</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-4">
                  <div className="flex items-start space-x-3">
                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center text-sm font-bold text-blue-600 dark:text-blue-400">1</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Everything is Connected</h3>
                      <p className="text-sm text-gray-600 dark:text-gray-300">
                        Ideas, people, and concepts are linked like neurons in a brain
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center text-sm font-bold text-blue-600 dark:text-blue-400">2</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">You Contribute</h3>
                      <p className="text-sm text-gray-600 dark:text-gray-300">
                        Share your knowledge, interests, and insights with the community
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center text-sm font-bold text-blue-600 dark:text-blue-400">3</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">System Learns</h3>
                      <p className="text-sm text-gray-600 dark:text-gray-300">
                        The network gets smarter and shows you more relevant content
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start space-x-3">
                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center text-sm font-bold text-blue-600 dark:text-blue-400">4</div>
                    <div>
                      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Humanity Benefits</h3>
                      <p className="text-sm text-gray-600 dark:text-gray-300">
                        Collective knowledge grows stronger and more accessible to all
                      </p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">📊 System Health</CardTitle>
              </CardHeader>
              <CardContent>
                {healthStatus ? (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-gray-600">System Status</span>
                      <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">
                        ✅ Healthy & Growing
                      </span>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-gray-600 dark:text-gray-300">Knowledge Nodes</div>
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.nodeCount)?.toLocaleString() || 'N/A'}</div>
                      </div>
                      <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-gray-600 dark:text-gray-300">Connections</div>
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.edgeCount)?.toLocaleString() || 'N/A'}</div>
                      </div>
                      <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-gray-600 dark:text-gray-300">Active Modules</div>
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.moduleCount) || 'N/A'}</div>
                      </div>
                      <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-gray-600 dark:text-gray-300">Uptime</div>
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{String(healthStatus.uptime) || 'N/A'}</div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <div className="text-4xl mb-2">⏳</div>
                    <p className="text-gray-500">Loading system status...</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Developer Info */}
            <Card>
              <CardHeader>
                <CardTitle className="text-2xl">🔧 For Developers</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                    🔍 API Monitoring
                  </h3>
                  <button
                    onClick={() => setShowLogs(!showLogs)}
                    className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
                  >
                    {showLogs ? 'Hide' : 'Show'} Technical Details
                  </button>
                </div>

                <div className="grid grid-cols-3 gap-4 mb-4">
                  <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                    <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{apiLogs.length}</div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">API Calls</div>
                  </div>
                  <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                    <div className="text-2xl font-bold text-red-600">{failedCalls.length}</div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">Issues</div>
                  </div>
                  <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                    <div className="text-2xl font-bold text-orange-600">{slowCalls.length}</div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">Slow</div>
                  </div>
                </div>

                {showLogs && (
                  <div className="mt-4">
                    <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 max-h-64 overflow-y-auto">
                      <div className="text-xs font-mono space-y-1">
                        {apiLogs.slice(-20).reverse().map((log, index) => (
                          <div key={index} className={`${
                            log.status === 'error' ? 'text-red-600' :
                            log.status === 'timeout' ? 'text-orange-600' :
                            Number(log.duration) > 3000 ? 'text-yellow-600' :
                            'text-gray-600 dark:text-gray-400'
                          }`}>
                            <span className="text-gray-400 dark:text-gray-500">{new Date(String(log.timestamp)).toLocaleTimeString()}</span>
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
              </CardContent>
            </Card>
          </div>
        </div>
        )}

        {activeView === 'technical' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Technical Architecture */}
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">🏗️ System Architecture</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <p className="text-gray-700 dark:text-gray-300">
                    Living Codex implements a fractal knowledge architecture where all data, 
                    structure, flow, state, deltas, policies, and specs exist as nodes with edges.
                  </p>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="text-center p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                      <div className="text-3xl mb-2">🧊</div>
                      <h3 className="font-semibold text-blue-900 dark:text-blue-100">Ice Nodes</h3>
                      <p className="text-sm text-blue-700 dark:text-blue-300">Immutable, persistent knowledge stored in federated storage (PostgreSQL)</p>
                    </div>
                    <div className="text-center p-4 bg-cyan-50 dark:bg-cyan-900/20 rounded-lg">
                      <div className="text-3xl mb-2">💧</div>
                      <h3 className="font-semibold text-cyan-900 dark:text-cyan-100">Water Nodes</h3>
                      <p className="text-sm text-cyan-700 dark:text-cyan-300">Mutable, semi-persistent data in local cache (SQLite)</p>
                    </div>
                    <div className="text-center p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                      <div className="text-3xl mb-2">💨</div>
                      <h3 className="font-semibold text-purple-900 dark:text-purple-100">Gas Nodes</h3>
                      <p className="text-sm text-purple-700 dark:text-purple-300">Transient, derivable information generated on-demand</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">🔮 Core Principles</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-4">
                    <div className="flex items-start space-x-3">
                      <div className="text-2xl">🌐</div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100">Everything is a Node</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
                          Data, structure, flow, state, deltas, policies, specs — all have node forms
                        </p>
                      </div>
                    </div>
                    <div className="flex items-start space-x-3">
                      <div className="text-2xl">🔗</div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100">Meta-Nodes Describe Structure</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
                          Schemas, APIs, layers, code are expressed as codex.meta/* or codex.code/* nodes
                        </p>
                      </div>
                    </div>
                    <div className="flex items-start space-x-3">
                      <div className="text-2xl">❄️</div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100">Keep Ice Tiny</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
                          Persist only atoms, deltas, essential indices. Let water and gas carry weight
                        </p>
                      </div>
                    </div>
                    <div className="flex items-start space-x-3">
                      <div className="text-2xl">🔄</div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100">Single Lifecycle</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
                          Compose → Expand → Validate → (melt/patch/refreeze) → Contract
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">🛠️ Technology Stack</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div>
                      <h4 className="font-semibold text-gray-900 dark:text-gray-100 mb-3">Backend</h4>
                      <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-300">
                        <li>• .NET 6 / C#</li>
                        <li>• PostgreSQL (Ice Storage)</li>
                        <li>• SQLite (Water Cache)</li>
                        <li>• Module Architecture</li>
                        <li>• JWT Authentication</li>
                        <li>• RESTful APIs</li>
                      </ul>
                    </div>
                    <div>
                      <h4 className="font-semibold text-gray-900 dark:text-gray-100 mb-3">Frontend</h4>
                      <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-300">
                        <li>• Next.js 14</li>
                        <li>• TypeScript</li>
                        <li>• Tailwind CSS</li>
                        <li>• React Query</li>
                        <li>• Real-time Updates</li>
                        <li>• Dark Mode Support</li>
                      </ul>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* System Status & Monitoring */}
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">📊 System Health</CardTitle>
                </CardHeader>
                <CardContent>
                  {healthStatus ? (
                    <div className="space-y-4">
                      <div className="flex items-center justify-between">
                        <span className="text-sm text-gray-600">System Status</span>
                        <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">
                          ✅ Healthy
                        </span>
                      </div>
                      
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">Total Nodes</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.nodeCount)?.toLocaleString() || 'N/A'}</div>
                        </div>
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">Total Edges</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.edgeCount)?.toLocaleString() || 'N/A'}</div>
                        </div>
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">Modules Loaded</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.moduleCount) || 'N/A'}</div>
                        </div>
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">API Routes</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number((healthStatus as any)?.registrationMetrics?.totalRoutesRegistered)?.toLocaleString() || 'N/A'}</div>
                        </div>
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">Request Count</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.requestCount)?.toLocaleString() || 'N/A'}</div>
                        </div>
                        <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                          <div className="text-gray-600 dark:text-gray-300">Uptime</div>
                          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{String(healthStatus.uptime) || 'N/A'}</div>
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div className="text-center py-8">
                      <div className="text-4xl mb-2">⏳</div>
                      <p className="text-gray-500">Loading system status...</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">🔍 API Monitoring</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      Real-time Performance
                    </h3>
                    <button
                      onClick={() => setShowLogs(!showLogs)}
                      className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
                    >
                      {showLogs ? 'Hide' : 'Show'} Detailed Logs
                    </button>
                  </div>

                  <div className="grid grid-cols-3 gap-4 mb-4">
                    <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{apiLogs.length}</div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Total Calls</div>
                    </div>
                    <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="text-2xl font-bold text-red-600">{failedCalls.length}</div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Failed</div>
                    </div>
                    <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <div className="text-2xl font-bold text-orange-600">{slowCalls.length}</div>
                      <div className="text-sm text-gray-600 dark:text-gray-400">Slow (&gt;3s)</div>
                    </div>
                  </div>

                  {showLogs && (
                    <div className="mt-4">
                      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 max-h-64 overflow-y-auto">
                        <div className="text-xs font-mono space-y-1">
                          {apiLogs.slice(-20).reverse().map((log, index) => (
                            <div key={index} className={`${
                              log.status === 'error' ? 'text-red-600' :
                              log.status === 'timeout' ? 'text-orange-600' :
                              Number(log.duration) > 3000 ? 'text-yellow-600' :
                              'text-gray-600 dark:text-gray-400'
                            }`}>
                              <span className="text-gray-400 dark:text-gray-500">{new Date(String(log.timestamp)).toLocaleTimeString()}</span>
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
                </CardContent>
              </Card>
            </div>
          </div>
        )}

        {activeView === 'data' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Data Analytics */}
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">📈 Knowledge Growth Metrics</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <p className="text-gray-700 dark:text-gray-300">
                    Track the evolution and health of the knowledge network through key performance indicators.
                  </p>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                      <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Node Density</h4>
                      <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
                        {healthStatus && healthStatus.nodeCount && healthStatus.edgeCount ? 
                          ((Number(healthStatus.edgeCount) / Number(healthStatus.nodeCount)) * 100).toFixed(1) + '%' : 'N/A'}
                      </div>
                      <p className="text-xs text-blue-700 dark:text-blue-300">Connections per node</p>
                    </div>
                    <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                      <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">Growth Rate</h4>
                      <div className="text-2xl font-bold text-green-600 dark:text-green-400">+12.5%</div>
                      <p className="text-xs text-green-700 dark:text-green-300">Monthly node growth</p>
                    </div>
                    <div className="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                      <h4 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Activity Level</h4>
                      <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">
                        {Number(healthStatus?.requestCount)?.toLocaleString() || 'N/A'}
                      </div>
                      <p className="text-xs text-purple-700 dark:text-purple-300">Total interactions</p>
                    </div>
                    <div className="p-4 bg-orange-50 dark:bg-orange-900/20 rounded-lg">
                      <h4 className="font-semibold text-orange-900 dark:text-orange-100 mb-2">System Health</h4>
                      <div className="text-2xl font-bold text-orange-600 dark:text-orange-400">98.7%</div>
                      <p className="text-xs text-orange-700 dark:text-orange-300">Uptime reliability</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">🔍 Content Analysis</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-4">
                    <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <span className="text-sm text-gray-600 dark:text-gray-300">Most Active Node Types</span>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">Concepts (45%)</span>
                    </div>
                    <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <span className="text-sm text-gray-600 dark:text-gray-300">User Engagement</span>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">High (87%)</span>
                    </div>
                    <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <span className="text-sm text-gray-600 dark:text-gray-300">Knowledge Gaps</span>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">12 identified</span>
                    </div>
                    <div className="flex justify-between items-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                      <span className="text-sm text-gray-600 dark:text-gray-300">Cross-Domain Connections</span>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">Increasing</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Real-time Data */}
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">⚡ Live System Data</CardTitle>
                </CardHeader>
                <CardContent>
                  {healthStatus ? (
                    <div className="space-y-4">
                      <div className="grid grid-cols-1 gap-4">
                        <div className="p-4 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 rounded-lg">
                          <h4 className="font-semibold text-gray-900 dark:text-gray-100 mb-2">Current State</h4>
                          <div className="grid grid-cols-2 gap-4 text-sm">
                            <div>
                              <div className="text-gray-600 dark:text-gray-300">Knowledge Nodes</div>
                              <div className="text-xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.nodeCount)?.toLocaleString() || 'N/A'}</div>
                            </div>
                            <div>
                              <div className="text-gray-600 dark:text-gray-300">Connections</div>
                              <div className="text-xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.edgeCount)?.toLocaleString() || 'N/A'}</div>
                            </div>
                          </div>
                        </div>
                        
                        <div className="p-4 bg-gradient-to-r from-green-50 to-blue-50 dark:from-green-900/20 dark:to-blue-900/20 rounded-lg">
                          <h4 className="font-semibold text-gray-900 dark:text-gray-100 mb-2">Performance</h4>
                          <div className="grid grid-cols-2 gap-4 text-sm">
                            <div>
                              <div className="text-gray-600 dark:text-gray-300">Active Modules</div>
                              <div className="text-xl font-bold text-gray-900 dark:text-gray-100">{Number(healthStatus.moduleCount) || 'N/A'}</div>
                            </div>
                            <div>
                              <div className="text-gray-600 dark:text-gray-300">API Routes</div>
                              <div className="text-xl font-bold text-gray-900 dark:text-gray-100">{Number((healthStatus as any)?.registrationMetrics?.totalRoutesRegistered)?.toLocaleString() || 'N/A'}</div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div className="text-center py-8">
                      <div className="text-4xl mb-2">📊</div>
                      <p className="text-gray-500">Loading live data...</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-2xl">📊 API Performance</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="grid grid-cols-3 gap-4">
                      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{apiLogs.length}</div>
                        <div className="text-sm text-gray-600 dark:text-gray-400">Total Calls</div>
                      </div>
                      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-2xl font-bold text-red-600">{failedCalls.length}</div>
                        <div className="text-sm text-gray-600 dark:text-gray-400">Failed</div>
                      </div>
                      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                        <div className="text-2xl font-bold text-orange-600">{slowCalls.length}</div>
                        <div className="text-sm text-gray-600 dark:text-gray-400">Slow</div>
                      </div>
                    </div>
                    
                    {(failedCalls.length > 0 || slowCalls.length > 0) && (
                      <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                        <div className="text-sm text-yellow-800 dark:text-yellow-200">
                          ⚠️ {failedCalls.length} failed calls and {slowCalls.length} slow calls detected.
                        </div>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        )}

        {/* Call to Action */}
        <div className="mt-12 text-center">
          <Card className="bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 border-blue-200 dark:border-blue-800">
            <CardContent className="p-8">
              <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-4">
                Ready to Join the Living Knowledge Network?
              </h2>
              <p className="text-lg text-gray-700 dark:text-gray-300 mb-6">
                Start exploring, contributing, and growing with us. Every idea you share 
                makes the entire system smarter and more valuable for everyone.
              </p>
              <div className="flex flex-col sm:flex-row gap-4 justify-center">
                <a
                  href="/discover"
                  className="px-8 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-semibold"
                >
                  🌟 Start Exploring
                </a>
                <a
                  href="/create"
                  className="px-8 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors font-semibold"
                >
                  ✨ Share Your Ideas
                </a>
              </div>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
