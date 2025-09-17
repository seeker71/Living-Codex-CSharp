'use client';

import { useState } from 'react';
import { ResonanceControls } from '@/components/ui/ResonanceControls';
import { StreamLens } from '@/components/lenses/StreamLens';
import { usePages, useLenses } from '@/lib/hooks';
import { defaultAtoms } from '@/lib/atoms';

const lensTabs = [
  { id: 'lens.stream', name: 'Stream', icon: '📱' },
  { id: 'lens.threads', name: 'Threads', icon: '🧵' },
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

  const { data: pages, isLoading: pagesLoading } = usePages();
  const { data: lenses, isLoading: lensesLoading } = useLenses();

  const currentPage = pages?.find(p => p.path === '/discover') || defaultAtoms.pages[1];
  const currentLens = lenses?.find(l => l.id === activeLens) || defaultAtoms.lenses[0];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">
                Living Codex
              </h1>
            </div>
            <nav className="flex space-x-8">
              <a href="/discover" className="text-blue-600 font-medium">
                Discover
              </a>
              <a href="/resonance" className="text-gray-600 hover:text-gray-900">
                Resonance
              </a>
              <a href="/about" className="text-gray-600 hover:text-gray-900">
                About
              </a>
            </nav>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Page Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Discover
          </h1>
          <p className="text-gray-600">
            Explore concepts, people, and ideas through different lenses
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Controls Sidebar */}
          <div className="lg:col-span-1">
            <ResonanceControls
              onControlsChange={(newControls) => setControls(prev => ({...prev, ...newControls}))}
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
                          ? 'border-blue-500 text-blue-600'
                          : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
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
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              {pagesLoading || lensesLoading ? (
                <div className="space-y-4">
                  {[...Array(3)].map((_, i) => (
                    <div key={i} className="animate-pulse">
                      <div className="bg-gray-200 rounded-lg h-32" />
                    </div>
                  ))}
                </div>
              ) : (
                <StreamLens
                  lens={currentLens}
                  controls={controls}
                  userId="demo-user"
                />
              )}
            </div>

            {/* Lens Info */}
            <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                    <span className="text-blue-600 text-sm font-medium">i</span>
                  </div>
                </div>
                <div className="ml-3">
                  <h4 className="text-sm font-medium text-blue-900">
                    {currentLens.name}
                  </h4>
                  <p className="text-sm text-blue-700">
                    Status: <span className="font-medium">{currentLens.status}</span>
                    {currentLens.ranking && (
                      <span className="ml-2">
                        • Ranking: {currentLens.ranking}
                      </span>
                    )}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
