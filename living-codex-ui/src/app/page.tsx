'use client';

import { useState, useEffect } from 'react';
import { ResonanceControls } from '@/components/ui/ResonanceControls';
import { StreamLens } from '@/components/lenses/StreamLens';
import { usePages, useLenses } from '@/lib/hooks';
import { defaultAtoms } from '@/lib/atoms';
import { bootstrapUI } from '@/lib/bootstrap';

export default function HomePage() {
  const [controls, setControls] = useState({
    axes: ['resonance'],
    joy: 0.7,
    serendipity: 0.5,
  });
  const [bootstrapped, setBootstrapped] = useState(false);

  // Bootstrap on first load
  useEffect(() => {
    if (!bootstrapped) {
      bootstrapUI().then(() => setBootstrapped(true));
    }
  }, [bootstrapped]);

  // Try to fetch from server, fallback to defaults
  const { data: pages, isLoading: pagesLoading } = usePages();
  const { data: lenses, isLoading: lensesLoading } = useLenses();

  // Use server data if available, otherwise use defaults
  const currentPage = pages?.find(p => p.path === '/') || defaultAtoms.pages[0];
  const streamLens = lenses?.find(l => l.id === 'lens.stream') || defaultAtoms.lenses[0];

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
              <span className="ml-2 text-sm text-gray-500">v0.1</span>
            </div>
            <nav className="flex space-x-8">
              <a href="/discover" className="text-gray-600 hover:text-gray-900">
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
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h2 className="text-4xl font-bold text-gray-900 mb-4">
            Find ideas that resonate
          </h2>
          <p className="text-xl text-gray-600 mb-8 max-w-3xl mx-auto">
            Meet people who amplify them. See the world's news through a living ontology.
          </p>
          <p className="text-lg text-gray-500 mb-8">
            Everything is a Node. Explore concepts, people, and moments connected by resonance.
          </p>
        </div>

        {/* Controls and Stream */}
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Resonance Controls */}
          <div className="lg:col-span-1">
            <ResonanceControls
              onControlsChange={setControls}
              className="sticky top-8"
            />
          </div>

          {/* Main Stream */}
          <div className="lg:col-span-3">
            <div className="mb-6">
              <h3 className="text-2xl font-semibold text-gray-900 mb-2">
                Now Resonating
              </h3>
              <p className="text-gray-600">
                Concepts and people that align with your resonance field
              </p>
            </div>

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
                lens={streamLens}
                controls={controls}
                userId="demo-user"
              />
            )}
          </div>
        </div>

        {/* Quick Actions */}
        <div className="mt-12 text-center">
          <h3 className="text-xl font-semibold text-gray-900 mb-4">
            Quick Actions
          </h3>
          <div className="flex justify-center space-x-4">
            <a
              href="/discover"
              className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition-colors"
            >
              Discover Concepts
            </a>
            <a
              href="/resonance"
              className="bg-purple-600 text-white px-6 py-3 rounded-lg hover:bg-purple-700 transition-colors"
            >
              Compare Resonance
            </a>
            <a
              href="/about"
              className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 transition-colors"
            >
              Learn More
            </a>
          </div>
        </div>
      </main>
    </div>
  );
}