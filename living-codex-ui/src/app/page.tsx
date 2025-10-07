'use client';

import { useState, useEffect, useCallback } from 'react';
import { ResonanceControls } from '@/components/ui/ResonanceControls';
import { StreamLens } from '@/components/lenses/StreamLens';
import { usePages, useLenses } from '@/lib/hooks';
import { defaultAtoms } from '@/lib/atoms';
import { bootstrapUI } from '@/lib/bootstrap';
import { useAuth } from '@/contexts/AuthContext';

export default function HomePage() {
  const { user, isAuthenticated } = useAuth();
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
  const streamLens = lenses?.find(l => l.id === 'lens.stream') || defaultAtoms.lenses[0];

  // Memoize the controls change handler to prevent infinite re-renders
  const handleControlsChange = useCallback((newControls: any) => {
    setControls(prev => ({...prev, ...newControls}));
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h2 className="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-4">
            Find ideas that resonate
          </h2>
          <p className="text-xl text-gray-600 dark:text-gray-300 mb-8 max-w-3xl mx-auto">
            Meet people who amplify them. See the world&apos;s news through a living ontology.
          </p>
          <p className="text-lg text-gray-500 dark:text-gray-400 mb-6">
            Everything is a Node. Explore concepts, people, and moments connected by resonance.
          </p>
          
          {/* Quick Links */}
          <div className="flex gap-4 justify-center mb-8">
            <a
              href="/graph"
              className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-semibold"
            >
              🌐 Explore Knowledge Graph
            </a>
            <a
              href="/nodes"
              className="px-6 py-3 bg-gray-700 text-white rounded-lg hover:bg-gray-600 transition-colors font-semibold"
            >
              📊 Browse All Nodes
            </a>
          </div>
        </div>

        {/* Controls and Stream */}
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Resonance Controls */}
          <div className="lg:col-span-1">
            <ResonanceControls
              onControlsChange={handleControlsChange}
              className="sticky top-8"
            />
          </div>

          {/* Main Stream */}
          <div className="lg:col-span-3">
            <div className="mb-6">
              <h3 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-2">
                Now Resonating
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
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
            ) : isAuthenticated && user ? (
              <StreamLens
                lens={streamLens}
                controls={controls}
                userId={user.id}
              />
            ) : (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 text-center">
                <div className="text-blue-800 font-medium mb-2">Sign in to see your personalized stream</div>
                <div className="text-blue-600 text-sm mb-4">
                  Connect with concepts and people that resonate with your interests
                </div>
                <a
                  href="/login"
                  className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Sign In
                </a>
              </div>
            )}
          </div>
        </div>

        {/* Quick Actions */}
        <div className="mt-12 text-center">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
            Quick Actions
          </h3>
          <div className="flex justify-center space-x-4 flex-wrap gap-4">
            <a
              href="/discover"
              className="bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition-colors"
            >
              🔍 Discover Concepts
            </a>
            <a
              href="/code"
              className="bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 transition-colors"
            >
              💻 Browse & Edit Code
            </a>
            <a
              href="/resonance"
              className="bg-purple-600 text-white px-6 py-3 rounded-lg hover:bg-purple-700 transition-colors"
            >
              🌊 Compare Resonance
            </a>
            <a
              href="/about"
              className="bg-gray-600 text-white px-6 py-3 rounded-lg hover:bg-gray-700 transition-colors"
            >
              ℹ️ Learn More
            </a>
          </div>
        </div>
      </main>
    </div>
  );
}