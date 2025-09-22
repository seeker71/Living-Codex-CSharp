'use client';

import { useEffect, useState } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface Portal {
  portalId: string;
  name: string;
  description: string;
  portalType: string;
  url?: string;
  entityId?: string;
  status: string;
  capabilities: Record<string, any>;
  createdAt: string;
}

interface TemporalPortal {
  portalId: string;
  temporalType: string;
  targetMoment: string;
  consciousnessLevel: number;
  status: string;
  resonance: number;
}

interface ExplorationSession {
  explorationId: string;
  portalId: string;
  explorationType: string;
  startingMoment: string;
  depth: number;
  status: string;
  discoveredMoments: any[];
  exploredPaths: any[];
}

export default function PortalsPage() {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Data state
  const [portals, setPortals] = useState<Portal[]>([]);
  const [temporalPortals, setTemporalPortals] = useState<TemporalPortal[]>([]);
  const [explorationSessions, setExplorationSessions] = useState<ExplorationSession[]>([]);
  
  // UI state
  const [activeTab, setActiveTab] = useState<'portals' | 'temporal' | 'explorations'>('portals');
  const [loading, setLoading] = useState(true);
  const [showCreatePortal, setShowCreatePortal] = useState(false);
  
  // Portal creation state
  const [newPortal, setNewPortal] = useState({
    name: '',
    description: '',
    url: '',
    portalType: 'website'
  });
  
  // Temporal exploration state
  const [temporalExploration, setTemporalExploration] = useState({
    explorationType: 'consciousness_mapping',
    depth: 5,
    maxBranches: 3,
    startingMoment: new Date().toISOString()
  });

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('portals-page', 'page-visit', { description: 'User visited portals exploration page' });
    }
  }, [user?.id, trackInteraction]);

  // Load portal data
  useEffect(() => {
    loadPortalData();
  }, []);

  const loadPortalData = async () => {
    setLoading(true);
    try {
      // Load active portals
      const portalsResponse = await fetch(buildApiUrl('/portal/list'));
      if (portalsResponse.ok) {
        const portalsData = await portalsResponse.json();
        if (portalsData.portals) {
          setPortals(portalsData.portals);
        }
      }

      // Load temporal portals
      const temporalResponse = await fetch(buildApiUrl('/temporal/portals/list'));
      if (temporalResponse.ok) {
        const temporalData = await temporalResponse.json();
        if (temporalData.portals) {
          setTemporalPortals(temporalData.portals);
        }
      }

      // Load exploration sessions
      const explorationsResponse = await fetch(buildApiUrl('/portal/explorations/list'));
      if (explorationsResponse.ok) {
        const explorationsData = await explorationsResponse.json();
        if (explorationsData.explorations) {
          setExplorationSessions(explorationsData.explorations);
        }
      }

    } catch (error) {
      console.error('Error loading portal data:', error);
    } finally {
      setLoading(false);
    }
  };

  const createPortal = async () => {
    if (!newPortal.name.trim() || !newPortal.url.trim()) {
      alert('Please provide portal name and URL');
      return;
    }

    try {
      const response = await fetch(buildApiUrl('/portal/connect'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: newPortal.name,
          description: newPortal.description,
          url: newPortal.url,
          portalType: newPortal.portalType
        })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await loadPortalData();
          setShowCreatePortal(false);
          setNewPortal({ name: '', description: '', url: '', portalType: 'website' });
          
          // Track portal creation
          if (user?.id) {
            trackInteraction(data.portalId || 'new-portal', 'create-portal', {
              description: `User created portal: ${newPortal.name}`,
              portalType: newPortal.portalType,
              url: newPortal.url
            });
          }
        }
      }
    } catch (error) {
      console.error('Error creating portal:', error);
    }
  };

  const startTemporalExploration = async (portalId: string) => {
    if (!user?.id) return;

    try {
      const response = await fetch(buildApiUrl('/temporal/explore'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          portalId,
          userId: user.id,
          explorationType: temporalExploration.explorationType,
          startingMoment: temporalExploration.startingMoment,
          depth: temporalExploration.depth,
          maxBranches: temporalExploration.maxBranches
        })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await loadPortalData();
          
          // Track temporal exploration
          trackInteraction(portalId, 'temporal-explore', {
            description: `User started temporal exploration`,
            explorationType: temporalExploration.explorationType,
            depth: temporalExploration.depth
          });
        }
      }
    } catch (error) {
      console.error('Error starting temporal exploration:', error);
    }
  };

  const disconnectPortal = async (portalId: string) => {
    try {
      const response = await fetch(buildApiUrl('/portal/disconnect'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ portalId })
      });

      if (response.ok) {
        await loadPortalData();
        
        // Track portal disconnection
        if (user?.id) {
          trackInteraction(portalId, 'disconnect-portal', {
            description: 'User disconnected portal'
          });
        }
      }
    } catch (error) {
      console.error('Error disconnecting portal:', error);
    }
  };

  const getPortalTypeIcon = (type: string): string => {
    const icons: Record<string, string> = {
      'website': '🌐',
      'api': '🔌',
      'living_entity': '🧬',
      'sensor': '📡',
      'temporal': '⏰',
      'consciousness': '🧠'
    };
    return icons[type] || '🚪';
  };

  const getStatusColor = (status: string): string => {
    const colors: Record<string, string> = {
      'connected': 'green',
      'disconnected': 'red',
      'error': 'red',
      'active': 'blue',
      'completed': 'green',
      'failed': 'red',
      'paused': 'yellow'
    };
    return colors[status] || 'gray';
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <Navigation />
      
      <div className="max-w-7xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">🚪 Portal Interface</h1>
          <p className="text-gray-600 dark:text-gray-300">
            Explore external worlds, temporal dimensions, and consciousness connections through unified portals
          </p>
        </div>

        {/* Tabs */}
        <div className="bg-white rounded-lg border border-gray-200 mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex space-x-8 px-6">
              {[
                { id: 'portals', label: 'External Portals', icon: '🌐' },
                { id: 'temporal', label: 'Temporal Portals', icon: '⏰' },
                { id: 'explorations', label: 'Active Explorations', icon: '🔍' }
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  {tab.icon} {tab.label}
                </button>
              ))}
            </nav>
          </div>

          <div className="p-6">
            {loading ? (
              <div className="text-center py-12">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                <p className="mt-4 text-gray-500">Loading portals...</p>
              </div>
            ) : (
              <>
                {/* External Portals Tab */}
                {activeTab === 'portals' && (
                  <div>
                    <div className="flex items-center justify-between mb-6">
                      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        🌐 External Portals ({portals.length})
                      </h2>
                      <button
                        onClick={() => setShowCreatePortal(true)}
                        className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                      >
                        ➕ Create Portal
                      </button>
                    </div>

                    {/* Create Portal Modal */}
                    {showCreatePortal && (
                      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                        <div className="bg-white rounded-lg p-6 w-96 max-w-full">
                          <h3 className="text-lg font-semibold text-gray-900 mb-4">Create New Portal</h3>
                          <div className="space-y-4">
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
                              <input
                                type="text"
                                value={newPortal.name}
                                onChange={(e) => setNewPortal({...newPortal, name: e.target.value})}
                                className="input-standard"
                                placeholder="Portal name..."
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                              <textarea
                                value={newPortal.description}
                                onChange={(e) => setNewPortal({...newPortal, description: e.target.value})}
                                className="input-standard"
                                rows={2}
                                placeholder="Portal description..."
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">URL</label>
                              <input
                                type="url"
                                value={newPortal.url}
                                onChange={(e) => setNewPortal({...newPortal, url: e.target.value})}
                                className="input-standard"
                                placeholder="https://example.com"
                              />
                            </div>
                            <div>
                              <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
                              <select
                                value={newPortal.portalType}
                                onChange={(e) => setNewPortal({...newPortal, portalType: e.target.value})}
                                className="input-standard"
                              >
                                <option value="website">🌐 Website</option>
                                <option value="api">🔌 API</option>
                                <option value="living_entity">🧬 Living Entity</option>
                                <option value="sensor">📡 Sensor</option>
                              </select>
                            </div>
                          </div>
                          <div className="flex space-x-3 mt-6">
                            <button
                              onClick={createPortal}
                              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                            >
                              Create Portal
                            </button>
                            <button
                              onClick={() => setShowCreatePortal(false)}
                              className="flex-1 px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Portals List */}
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                      {portals.length > 0 ? (
                        portals.map((portal) => (
                          <div key={portal.portalId} className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow">
                            <div className="flex items-start justify-between mb-4">
                              <div className="flex items-center space-x-3">
                                <span className="text-2xl">{getPortalTypeIcon(portal.portalType)}</span>
                                <div>
                                  <h3 className="font-semibold text-gray-900">{portal.name}</h3>
                                  <p className="text-sm text-gray-500">{portal.portalType}</p>
                                </div>
                              </div>
                              <span className={`px-2 py-1 rounded-md text-xs font-medium bg-${getStatusColor(portal.status)}-100 text-${getStatusColor(portal.status)}-800`}>
                                {portal.status}
                              </span>
                            </div>
                            
                            <p className="text-gray-600 text-sm mb-4">{portal.description}</p>
                            
                            {portal.url && (
                              <p className="text-blue-600 text-sm mb-4 truncate">
                                🔗 {portal.url}
                              </p>
                            )}
                            
                            <div className="flex space-x-2">
                              <button
                                onClick={() => startTemporalExploration(portal.portalId)}
                                className="flex-1 px-3 py-2 bg-purple-600 text-white text-sm rounded-md hover:bg-purple-700"
                              >
                                🔍 Explore
                              </button>
                              <button
                                onClick={() => disconnectPortal(portal.portalId)}
                                className="px-3 py-2 bg-red-600 text-white text-sm rounded-md hover:bg-red-700"
                              >
                                🔌 Disconnect
                              </button>
                            </div>
                          </div>
                        ))
                      ) : (
                        <div className="col-span-full text-center py-12 text-gray-500">
                          <div className="text-6xl mb-4">🚪</div>
                          <p>No portals connected yet.</p>
                          <p className="text-sm mt-2">Create your first portal to explore external worlds!</p>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Temporal Portals Tab */}
                {activeTab === 'temporal' && (
                  <div>
                    <div className="flex items-center justify-between mb-6">
                      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        ⏰ Temporal Portals ({temporalPortals.length})
                      </h2>
                    </div>

                    {/* Temporal Exploration Controls */}
                    <div className="bg-gray-50 rounded-lg p-6 mb-6">
                      <h3 className="text-lg font-medium text-gray-900 mb-4">🔮 Temporal Exploration Settings</h3>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">Exploration Type</label>
                          <select
                            value={temporalExploration.explorationType}
                            onChange={(e) => setTemporalExploration({...temporalExploration, explorationType: e.target.value})}
                            className="input-standard"
                          >
                            <option value="consciousness_mapping">🧠 Consciousness Mapping</option>
                            <option value="causality_analysis">🔗 Causality Analysis</option>
                            <option value="resonance_tracking">🌊 Resonance Tracking</option>
                            <option value="fractal_navigation">🌀 Fractal Navigation</option>
                          </select>
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">Depth: {temporalExploration.depth}</label>
                          <input
                            type="range"
                            min="1"
                            max="20"
                            value={temporalExploration.depth}
                            onChange={(e) => setTemporalExploration({...temporalExploration, depth: parseInt(e.target.value)})}
                            className="w-full"
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">Branches: {temporalExploration.maxBranches}</label>
                          <input
                            type="range"
                            min="1"
                            max="10"
                            value={temporalExploration.maxBranches}
                            onChange={(e) => setTemporalExploration({...temporalExploration, maxBranches: parseInt(e.target.value)})}
                            className="w-full"
                          />
                        </div>
                      </div>
                    </div>

                    {/* Temporal Portals List */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      {temporalPortals.length > 0 ? (
                        temporalPortals.map((portal) => (
                          <div key={portal.portalId} className="bg-white border border-gray-200 rounded-lg p-6">
                            <div className="flex items-start justify-between mb-4">
                              <div className="flex items-center space-x-3">
                                <span className="text-2xl">⏰</span>
                                <div>
                                  <h3 className="font-semibold text-gray-900">
                                    {portal.temporalType.charAt(0).toUpperCase() + portal.temporalType.slice(1)} Portal
                                  </h3>
                                  <p className="text-sm text-gray-500">
                                    {new Date(portal.targetMoment).toLocaleString()}
                                  </p>
                                </div>
                              </div>
                              <span className={`px-2 py-1 rounded-md text-xs font-medium bg-${getStatusColor(portal.status)}-100 text-${getStatusColor(portal.status)}-800`}>
                                {portal.status}
                              </span>
                            </div>
                            
                            <div className="space-y-2 mb-4">
                              <div className="flex justify-between text-sm">
                                <span className="text-gray-600 dark:text-gray-300">Consciousness Level</span>
                                <span className="font-medium">{(portal.consciousnessLevel * 100).toFixed(1)}%</span>
                              </div>
                              <div className="flex justify-between text-sm">
                                <span className="text-gray-600 dark:text-gray-300">Resonance</span>
                                <span className="font-medium">{(portal.resonance * 100).toFixed(1)}%</span>
                              </div>
                            </div>
                            
                            <button
                              onClick={() => startTemporalExploration(portal.portalId)}
                              className="w-full px-3 py-2 bg-purple-600 text-white text-sm rounded-md hover:bg-purple-700"
                            >
                              🔮 Begin Temporal Exploration
                            </button>
                          </div>
                        ))
                      ) : (
                        <div className="col-span-full text-center py-12 text-gray-500">
                          <div className="text-6xl mb-4">⏰</div>
                          <p>No temporal portals active.</p>
                          <p className="text-sm mt-2">Temporal portals are created automatically during consciousness exploration.</p>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Explorations Tab */}
                {activeTab === 'explorations' && (
                  <div>
                    <h2 className="text-xl font-semibold text-gray-900 mb-6">
                      🔍 Active Explorations ({explorationSessions.length})
                    </h2>

                    {explorationSessions.length > 0 ? (
                      <div className="space-y-6">
                        {explorationSessions.map((session) => (
                          <div key={session.explorationId} className="bg-white border border-gray-200 rounded-lg p-6">
                            <div className="flex items-start justify-between mb-4">
                              <div>
                                <h3 className="font-semibold text-gray-900 mb-1">
                                  {session.explorationType.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase())}
                                </h3>
                                <p className="text-sm text-gray-500">
                                  Portal: {session.portalId} • Started: {new Date(session.startingMoment).toLocaleString()}
                                </p>
                              </div>
                              <span className={`px-2 py-1 rounded-md text-xs font-medium bg-${getStatusColor(session.status)}-100 text-${getStatusColor(session.status)}-800`}>
                                {session.status}
                              </span>
                            </div>
                            
                            <div className="grid grid-cols-3 gap-4 mb-4">
                              <div className="text-center">
                                <div className="text-2xl font-bold text-blue-600">{session.depth}</div>
                                <div className="text-xs text-gray-500">Depth</div>
                              </div>
                              <div className="text-center">
                                <div className="text-2xl font-bold text-green-600">{session.discoveredMoments.length}</div>
                                <div className="text-xs text-gray-500">Moments</div>
                              </div>
                              <div className="text-center">
                                <div className="text-2xl font-bold text-purple-600">{session.exploredPaths.length}</div>
                                <div className="text-xs text-gray-500">Paths</div>
                              </div>
                            </div>

                            {session.discoveredMoments.length > 0 && (
                              <div>
                                <h4 className="font-medium text-gray-900 mb-2">Recent Discoveries</h4>
                                <div className="space-y-2">
                                  {session.discoveredMoments.slice(0, 3).map((moment: any, index: number) => (
                                    <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                                      <span className="text-sm text-gray-700">
                                        {new Date(moment.timestamp).toLocaleTimeString()}
                                      </span>
                                      <span className="text-sm text-blue-600">
                                        C: {(moment.consciousnessLevel * 100).toFixed(0)}% | R: {(moment.resonance * 100).toFixed(0)}%
                                      </span>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div className="text-center py-12 text-gray-500">
                        <div className="text-6xl mb-4">🔍</div>
                        <p>No active explorations.</p>
                        <p className="text-sm mt-2">Start exploring portals to see sessions here!</p>
                      </div>
                    )}
                  </div>
                )}
              </>
            )}
          </div>
        </div>

        {/* Quick Portal Templates */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">🚀 Quick Portal Templates</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              {
                name: 'Wikipedia',
                description: 'Explore collective knowledge',
                url: 'https://en.wikipedia.org',
                type: 'website',
                icon: '📚'
              },
              {
                name: 'GitHub',
                description: 'Connect to code consciousness',
                url: 'https://github.com',
                type: 'website',
                icon: '💻'
              },
              {
                name: 'Nature Cam',
                description: 'Living nature observation',
                url: 'https://explore.org/livecams',
                type: 'sensor',
                icon: '🌿'
              },
              {
                name: 'Consciousness API',
                description: 'AI consciousness interface',
                url: 'https://api.openai.com/v1',
                type: 'api',
                icon: '🤖'
              }
            ].map((template, index) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50">
                <div className="text-center mb-3">
                  <div className="text-3xl mb-2">{template.icon}</div>
                  <h4 className="font-medium text-gray-900">{template.name}</h4>
                  <p className="text-sm text-gray-600">{template.description}</p>
                </div>
                <button
                  onClick={() => {
                    setNewPortal({
                      name: template.name,
                      description: template.description,
                      url: template.url,
                      portalType: template.type
                    });
                    setShowCreatePortal(true);
                  }}
                  className="w-full px-3 py-2 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700"
                >
                  Create Portal
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
