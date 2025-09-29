'use client';

import { useState, useCallback } from 'react';
import { ResonanceControls } from '@/components/ui/ResonanceControls';
import { StreamLens } from '@/components/lenses/StreamLens';
import ThreadsLens from '@/components/lenses/ThreadsLens';
import { GalleryLens } from '@/components/lenses/GalleryLens';
import { NearbyLens } from '@/components/lenses/NearbyLens';
import { SwipeLens } from '@/components/lenses/SwipeLens';
import { Card, CardContent } from '@/components/ui/Card';
import { usePages, useLenses } from '@/lib/hooks';
import { defaultAtoms } from '@/lib/atoms';
import { useAuth } from '@/contexts/AuthContext';

const lensTabs = [
  { id: 'lens.stream', name: 'Stream', icon: '📱' },
  { id: 'lens.threads', name: 'Conversations', icon: '💬' },
  { id: 'lens.gallery', name: 'Gallery', icon: '🖼️' },
  { id: 'lens.nearby', name: 'Nearby', icon: '📍' },
  { id: 'lens.swipe', name: 'Swipe', icon: '👆' },
];

export default function DiscoverPage() {
  const [activeLens, setActiveLens] = useState('lens.stream');
  const [controls, setControls] = useState({
    axes: ['resonance'],
    joy: 0.7,
    serendipity: 0.5,
  });

  const { isLoading: pagesLoading } = usePages();
  const { data: lenses, isLoading: lensesLoading } = useLenses();
  const { user, isAuthenticated } = useAuth();

  const findLens = (lensId: string) =>
    lenses?.find(l => l.id === lensId) || defaultAtoms.lenses.find(l => l.id === lensId);

  const currentLens = findLens(activeLens) || defaultAtoms.lenses[0];

  // Memoize the controls change handler to prevent infinite re-renders
  const handleControlsChange = useCallback((newControls: any) => {
    setControls(prev => ({...prev, ...newControls}));
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Page Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Discover
          </h1>
          <p className="text-gray-600 dark:text-gray-300">
            Explore concepts, people, and ideas through different lenses
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Controls Sidebar */}
          <div className="lg:col-span-1">
            <ResonanceControls
              onControlsChange={handleControlsChange}
              className="sticky top-8"
            />
          </div>

          {/* Main Content */}
          <div className="lg:col-span-3">
            {/* Lens Tabs */}
            <div className="mb-6">
              <div className="border-b border-gray-200">
                <nav className="-mb-px flex space-x-8">
                  {lensTabs.map((tab) => (
                    <button
                      key={tab.id}
                      onClick={() => setActiveLens(tab.id)}
                      className={`py-2 px-1 border-b-2 font-medium text-sm ${
                        activeLens === tab.id
                          ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                          : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
                      }`}
                    >
                      <span className="mr-2">{tab.icon}</span>
                      {tab.name}
                    </button>
                  ))}
                </nav>
              </div>
            </div>

            {/* Lens Content */}
            <Card>
              <CardContent className="p-6">
                {pagesLoading || lensesLoading ? (
                  <div className="space-y-4">
                    {[...Array(3)].map((_, i) => (
                      <div key={i} className="animate-pulse">
                        <div className="bg-gray-200 rounded-lg h-32" />
                      </div>
                    ))}
                  </div>
                ) : (
                  <>
                    {/* Read-only mode indicator */}
                    {(!isAuthenticated || !user) && (
                      <div className="mb-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
                        <div className="flex items-center space-x-2">
                          <span className="text-amber-600 dark:text-amber-400">👁️</span>
                          <span className="text-sm text-amber-800 dark:text-amber-200 font-medium">
                            Read-only mode
                          </span>
                          <span className="text-xs text-amber-700 dark:text-amber-300">
                            Sign in to interact and personalize your experience
                          </span>
                          <a
                            href="/login"
                            className="ml-auto text-xs bg-amber-600 text-white px-3 py-1 rounded hover:bg-amber-700 transition-colors"
                          >
                            Sign In
                          </a>
                        </div>
                      </div>
                    )}
                    
                    {(() => {
                      switch (activeLens) {
                        case 'lens.stream':
                          return (
                            <StreamLens
                              lens={currentLens}
                              controls={controls}
                              userId={user?.id}
                              readOnly={!isAuthenticated || !user}
                            />
                          );
                        case 'lens.threads':
                          return (
                            <ThreadsLens
                              controls={controls}
                              userId={user?.id}
                              readOnly={!isAuthenticated || !user}
                            />
                          );
                        case 'lens.gallery':
                          return (
                            <GalleryLens
                              controls={controls}
                              userId={user?.id}
                              readOnly={!isAuthenticated || !user}
                            />
                          );
                        case 'lens.nearby':
                          return (
                            <NearbyLens
                              controls={controls}
                              userId={user?.id}
                              readOnly={!isAuthenticated || !user}
                            />
                          );
                        case 'lens.swipe':
                          return (
                            <SwipeLens
                              controls={controls}
                              userId={user?.id}
                              readOnly={!isAuthenticated || !user}
                            />
                          );
                        default:
                          return (
                            <div className="bg-gray-50 dark:bg-gray-800 border border-dashed border-gray-300 dark:border-gray-700 rounded-lg p-6 text-center">
                              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                                {currentLens.name || 'Lens Coming Soon'}
                              </h3>
                              <p className="text-sm text-gray-600 dark:text-gray-300">
                                This experience is not wired up yet. Current status: {currentLens.status || 'Planned'}.
                              </p>
                            </div>
                          );
                      }
                    })()}
                  </>
                )}
              </CardContent>
            </Card>

            {/* Lens Info */}
            <Card className="mt-6">
              <CardContent className="p-4">
                <div className="flex items-center">
                  <div className="flex-shrink-0">
                    <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/40 rounded-full flex items-center justify-center">
                      <span className="text-blue-600 dark:text-blue-400 text-sm font-medium">i</span>
                    </div>
                  </div>
                  <div className="ml-3">
                    <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100">
                      {currentLens.name}
                    </h4>
                    <p className="text-sm text-blue-700 dark:text-blue-300">
                      Status: <span className="font-medium">{currentLens.status}</span>
                      {currentLens.ranking && (
                        <span className="ml-2">
                          • Ranking: {currentLens.ranking}
                        </span>
                      )}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </main>
    </div>
  );
}
