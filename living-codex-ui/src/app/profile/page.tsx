'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Navigation } from '@/components/ui/Navigation';
import { useAuth } from '@/contexts/AuthContext';
import { useContributorEnergy, usePersonalNewsStream, useUserConcepts } from '@/lib/hooks';

interface UserStats {
  energyLevel: number;
  totalContributions: number;
  resonanceLevel: number;
  totalValue: number;
  lastUpdated: string;
}

interface NewsItem {
  id: string;
  title: string;
  description: string;
  url?: string;
  timestamp: string;
  resonanceScore?: number;
  conceptTags?: string[];
}

export default function ProfilePage() {
  const { user, isAuthenticated, logout, isLoading: authLoading } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  // Use hooks for data fetching
  const { data: userStats, isLoading: statsLoading } = useContributorEnergy(user?.username || '');
  const { data: newsStream, isLoading: newsLoading } = usePersonalNewsStream(user?.username || '', 20);
  const { data: userConcepts, isLoading: conceptsLoading } = useUserConcepts(user?.username || '');

  const loading = statsLoading || newsLoading || conceptsLoading;

  // Redirect if not authenticated
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/auth');
    }
  }, [isAuthenticated, authLoading, router]);

  // Extract news items from the response
  const newsFeed = newsStream?.success && newsStream.data ? 
    (newsStream.data as any).newsItems || [] : [];

  // Extract user concepts for interaction tracking
  const conceptInteractions = userConcepts?.success && userConcepts.data ?
    (userConcepts.data as any).concepts || [] : [];

  if (authLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return null; // Will redirect
  }

  const handleLogout = () => {
    logout();
    router.push('/');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">Living Codex</h1>
            </div>
            <div className="flex items-center space-x-4">
              <Navigation />
              <button
                onClick={handleLogout}
                className="text-gray-600 hover:text-gray-900 text-sm font-medium"
              >
                Sign Out
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Profile Header */}
        <div className="mb-8">
          <div className="flex items-center space-x-4">
            <div className="w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center">
              <span className="text-2xl font-bold text-white">
                {(user.displayName || user.username || 'U').charAt(0).toUpperCase()}
              </span>
            </div>
            <div>
              <h1 className="text-3xl font-bold text-gray-900">{user.displayName || user.username || 'User'}</h1>
              <p className="text-gray-600">@{user.username || 'unknown'}</p>
              <p className="text-sm text-gray-500">
                Member since {user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}
              </p>
            </div>
          </div>
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="text-sm text-red-700">{error}</div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Energy Balance */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">
                ⚡ Energy Balance
              </h2>
              
              {loading ? (
                <div className="space-y-4">
                  <div className="animate-pulse bg-gray-200 rounded h-8"></div>
                  <div className="animate-pulse bg-gray-200 rounded h-4"></div>
                  <div className="animate-pulse bg-gray-200 rounded h-4"></div>
                </div>
              ) : userStats?.success && userStats.data ? (
                <div className="space-y-4">
                  <div className="text-center mb-6">
                    <div className="text-4xl font-bold text-indigo-600 mb-2">
                      {(userStats.data as any).energyLevel.toFixed(1)}
                    </div>
                    <div className="text-sm text-gray-600">Current Energy Level</div>
                  </div>

                  <div className="space-y-3">
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Contributions</span>
                      <span className="font-medium">{(userStats.data as any).totalContributions}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Resonance Level</span>
                      <span className="font-medium">{(userStats.data as any).resonanceLevel.toFixed(1)}%</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Total Value</span>
                      <span className="font-medium">{(userStats.data as any).totalValue.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600">Concepts Linked</span>
                      <span className="font-medium">{conceptInteractions.length}</span>
                    </div>
                  </div>

                  <div className="mt-6">
                    <div className="flex justify-between text-sm text-gray-600 mb-2">
                      <span>Energy Progress</span>
                      <span>{(userStats.data as any).energyLevel.toFixed(1)}</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-3">
                      <div 
                        className="bg-gradient-to-r from-indigo-500 to-purple-500 h-3 rounded-full transition-all duration-500"
                        style={{ width: `${Math.min(((userStats.data as any).energyLevel / 10) * 100, 100)}%` }}
                      ></div>
                    </div>
                  </div>

                  <div className="mt-6 p-4 bg-indigo-50 rounded-lg">
                    <div className="text-sm text-indigo-800">
                      💡 Last updated: {new Date((userStats.data as any).lastUpdated).toLocaleString()}
                    </div>
                  </div>

                  <div className="mt-4">
                    <button
                      onClick={() => window.location.href = '/resonance'}
                      className="w-full bg-indigo-600 text-white py-2 px-4 rounded-md hover:bg-indigo-700 transition-colors"
                    >
                      View Resonance Field
                    </button>
                  </div>
                </div>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <div className="text-4xl mb-4">🌱</div>
                  <div>Start contributing to build your energy profile</div>
                </div>
              )}
            </div>
          </div>

          {/* Personal News Feed */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold text-gray-900">
                  📰 Personal News Feed
                </h2>
                <span className="text-sm text-gray-500">
                  Real-time • Concepts you&apos;ve interacted with
                </span>
              </div>

              {loading ? (
                <div className="space-y-4">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="animate-pulse">
                      <div className="bg-gray-200 rounded-lg h-24"></div>
                    </div>
                  ))}
                </div>
              ) : newsFeed.length > 0 ? (
                <div className="space-y-4">
                  {newsFeed.map((item: any) => (
                    <div key={item.id} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h3 className="font-medium text-gray-900 mb-2">
                            {item.title}
                          </h3>
                          <p className="text-sm text-gray-600 mb-3">
                            {item.description}
                          </p>
                          
                          {item.conceptTags && item.conceptTags.length > 0 && (
                            <div className="flex flex-wrap gap-2 mb-3">
                              {item.conceptTags.map((tag: any, index: number) => (
                                <span
                                  key={index}
                                  className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                                >
                                  {tag}
                                </span>
                              ))}
                            </div>
                          )}
                          
                          <div className="flex items-center justify-between text-xs text-gray-500">
                            <span>{new Date(item.timestamp).toLocaleString()}</span>
                            {item.resonanceScore && (
                              <span className="bg-purple-100 text-purple-700 px-2 py-1 rounded">
                                Resonance: {(item.resonanceScore * 100).toFixed(1)}%
                              </span>
                            )}
                          </div>
                        </div>
                        
                        {item.url && (
                          <div className="ml-4">
                            <a
                              href={item.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-blue-600 hover:text-blue-700 text-sm font-medium"
                            >
                              Read More →
                            </a>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center text-gray-500 py-12">
                  <div className="text-4xl mb-4">📡</div>
                  <div className="text-lg font-medium mb-2">Your Personal Feed is Empty</div>
                  <p className="text-sm mb-6">
                    Start exploring concepts and contributing to see personalized content here
                  </p>
                  <div className="space-x-4">
                    <button
                      onClick={() => router.push('/discover')}
                      className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
                    >
                      Discover Concepts
                    </button>
                    <button
                      onClick={() => router.push('/graph')}
                      className="bg-gray-600 text-white px-4 py-2 rounded-md hover:bg-gray-700 transition-colors"
                    >
                      Explore Graph
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Linked Concepts */}
            {conceptInteractions.length > 0 && (
              <div className="mt-6 bg-white rounded-lg border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">
                  🔗 Your Linked Concepts
                </h3>
                
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {conceptInteractions.slice(0, 6).map((concept: any, index: number) => (
                    <div key={index} className="bg-blue-50 rounded-lg p-3">
                      <div className="font-medium text-blue-900 text-sm">
                        {concept.name || concept.conceptId || 'Unknown Concept'}
                      </div>
                      <div className="text-xs text-blue-700 mt-1">
                        {concept.relation || 'attuned'} • {concept.domain || 'general'}
                      </div>
                    </div>
                  ))}
                </div>
                
                {conceptInteractions.length > 6 && (
                  <div className="mt-3 text-center">
                    <button
                      onClick={() => router.push('/discover')}
                      className="text-blue-600 hover:text-blue-700 text-sm font-medium"
                    >
                      View all {conceptInteractions.length} concepts →
                    </button>
                  </div>
                )}
              </div>
            )}

            {/* Recent Activity */}
            <div className="mt-6 bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">
                🔄 Recent Activity
              </h3>
              
              <div className="space-y-3">
                <div className="flex items-center space-x-3 text-sm">
                  <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                  <span className="text-gray-600">Joined Living Codex</span>
                  <span className="text-gray-400">•</span>
                  <span className="text-gray-500">
                    {user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}
                  </span>
                </div>
                
                {userStats?.success && (userStats.data as any).totalContributions > 0 && (
                  <div className="flex items-center space-x-3 text-sm">
                    <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                    <span className="text-gray-600">Made {(userStats.data as any).totalContributions} contributions</span>
                    <span className="text-gray-400">•</span>
                    <span className="text-gray-500">{new Date((userStats.data as any).lastUpdated).toLocaleDateString()}</span>
                  </div>
                )}
                
                {conceptInteractions.length > 0 && (
                  <div className="flex items-center space-x-3 text-sm">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    <span className="text-gray-600">Linked to {conceptInteractions.length} concepts</span>
                    <span className="text-gray-400">•</span>
                    <span className="text-gray-500">Active interactions</span>
                  </div>
                )}
                
                <div className="flex items-center space-x-3 text-sm">
                  <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                  <span className="text-gray-600">Current resonance level</span>
                  <span className="text-gray-400">•</span>
                  <span className="text-gray-500">
                    {userStats?.success ? `${(userStats.data as any).resonanceLevel.toFixed(1)}%` : 'Calculating...'}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
