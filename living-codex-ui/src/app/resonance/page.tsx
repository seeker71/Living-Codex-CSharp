'use client';

import { useState, useEffect } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { endpoints } from '@/lib/api';

interface ResonanceData {
  success: boolean;
  collectiveResonance: number;
  totalContributors: number;
  totalAbundanceEvents: number;
  recentAbundanceEvents: number;
  averageAbundanceMultiplier: number;
  totalCollectiveValue: number;
  timestamp: string;
}

interface ContributorEnergy {
  success: boolean;
  userId: string;
  energyLevel: number;
  baseEnergy: number;
  amplifiedEnergy: number;
  resonanceLevel: number;
  totalContributions: number;
  totalValue: number;
  totalCollectiveValue: number;
  averageAbundanceMultiplier: number;
  lastUpdated: string;
}

export default function ResonancePage() {
  const [collectiveData, setCollectiveData] = useState<ResonanceData | null>(null);
  const [contributorData, setContributorData] = useState<ContributorEnergy | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [userId] = useState('demo-user'); // In real app, this would come from auth

  useEffect(() => {
    async function loadResonanceData() {
      setLoading(true);
      setError(null);

      try {
        // Load collective energy data
        const collectiveResponse = await endpoints.getCollectiveEnergy();
        if (collectiveResponse.success) {
          setCollectiveData(collectiveResponse.data as ResonanceData);
        } else {
          throw new Error(collectiveResponse.error || 'Failed to load collective energy');
        }

        // Load contributor energy data
        const contributorResponse = await endpoints.getContributorEnergy(userId);
        if (contributorResponse.success) {
          setContributorData(contributorResponse.data as ContributorEnergy);
        } else {
          console.warn('Failed to load contributor energy:', contributorResponse.error);
          // Don't throw error for contributor data - it's optional
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        setLoading(false);
      }
    }

    loadResonanceData();
  }, [userId]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center h-16">
              <div className="flex items-center">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Living Codex</h1>
              </div>
              <Navigation />
            </div>
          </div>
        </header>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="animate-pulse space-y-4">
            <div className="h-8 bg-gray-200 rounded w-1/4"></div>
            <div className="h-32 bg-gray-200 rounded"></div>
            <div className="h-32 bg-gray-200 rounded"></div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Living Codex</h1>
            </div>
            <Navigation />
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Page Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Resonance Field
          </h1>
          <p className="text-gray-600 dark:text-gray-300">
            Explore the collective resonance and energy patterns across the Living Codex
          </p>
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <span className="text-red-400">⚠️</span>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-red-800">
                  Error loading resonance data
                </h3>
                <p className="mt-1 text-sm text-red-700">{error}</p>
              </div>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Collective Resonance */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">
              🌊 Collective Resonance
            </h2>
            
            {collectiveData ? (
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="bg-blue-50 rounded-lg p-4">
                    <div className="text-sm text-blue-600 font-medium">Resonance Level</div>
                    <div className="text-2xl font-bold text-blue-900">
                      {collectiveData.collectiveResonance.toFixed(1)}%
                    </div>
                  </div>
                  <div className="bg-purple-50 rounded-lg p-4">
                    <div className="text-sm text-purple-600 font-medium">Abundance Events</div>
                    <div className="text-2xl font-bold text-purple-900">
                      {collectiveData.totalAbundanceEvents}
                    </div>
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  <div className="bg-green-50 rounded-lg p-4">
                    <div className="text-sm text-green-600 font-medium">Total Contributors</div>
                    <div className="text-2xl font-bold text-green-900">
                      {collectiveData.totalContributors.toLocaleString()}
                    </div>
                  </div>
                  <div className="bg-orange-50 rounded-lg p-4">
                    <div className="text-sm text-orange-600 font-medium">Amplification</div>
                    <div className="text-2xl font-bold text-orange-900">
                      {collectiveData.averageAbundanceMultiplier.toFixed(2)}x
                    </div>
                  </div>
                </div>

                <div className="mt-6">
                  <div className="flex justify-between text-sm text-gray-600 mb-2">
                    <span>Resonance Strength</span>
                    <span>{collectiveData.collectiveResonance.toFixed(1)}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-3">
                    <div 
                      className="bg-gradient-to-r from-blue-500 to-purple-600 h-3 rounded-full transition-all duration-500"
                      style={{ width: `${Math.min(collectiveData.collectiveResonance, 100)}%` }}
                    ></div>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-gray-500 dark:text-gray-400">No collective resonance data available</div>
            )}
          </div>

          {/* Personal Resonance */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">
              ✨ Your Resonance
            </h2>
            
            {contributorData ? (
              <div className="space-y-4">
                <div className="text-center mb-6">
                  <div className="text-4xl font-bold text-indigo-600 mb-2">
                    {contributorData.energyLevel.toFixed(1)}
                  </div>
                  <div className="text-sm text-gray-600">Personal Energy Level</div>
                </div>

                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-gray-600">Contributions</span>
                    <span className="font-medium">{contributorData.totalContributions}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-gray-600">Resonance Level</span>
                    <span className="font-medium">{contributorData.resonanceLevel.toFixed(1)}%</span>
                  </div>
                </div>

                <div className="mt-6">
                  <div className="flex justify-between text-sm text-gray-600 mb-2">
                    <span>Personal Resonance</span>
                    <span>{contributorData.resonanceLevel.toFixed(1)}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-3">
                    <div 
                      className="bg-gradient-to-r from-indigo-500 to-pink-500 h-3 rounded-full transition-all duration-500"
                      style={{ width: `${Math.min(contributorData.resonanceLevel, 100)}%` }}
                    ></div>
                  </div>
                </div>

                <div className="mt-6 p-4 bg-indigo-50 rounded-lg">
                  <div className="text-sm text-indigo-800">
                    💡 Your energy contributes to the collective resonance field. 
                    Keep contributing to amplify the shared consciousness!
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center text-gray-500 py-8">
                <div className="text-4xl mb-4">🌱</div>
                <div>Start contributing to build your resonance profile</div>
                <button 
                  onClick={() => window.location.href = '/discover'}
                  className="mt-4 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors"
                >
                  Discover Concepts
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Resonance Insights */}
        <div className="mt-8 bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">
            🔮 Resonance Insights
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-3xl mb-2">🌊</div>
              <h3 className="font-medium text-gray-900 mb-2">Wave Patterns</h3>
              <p className="text-sm text-gray-600">
                Resonance flows in waves across the collective consciousness. 
                Higher frequency contributions create stronger amplification effects.
              </p>
            </div>
            
            <div className="text-center">
              <div className="text-3xl mb-2">🔗</div>
              <h3 className="font-medium text-gray-900 mb-2">Connection Strength</h3>
              <p className="text-sm text-gray-600">
                Nodes with similar resonance patterns naturally form stronger connections, 
                creating emergent knowledge structures.
              </p>
            </div>
            
            <div className="text-center">
              <div className="text-3xl mb-2">⚡</div>
              <h3 className="font-medium text-gray-900 mb-2">Energy Amplification</h3>
              <p className="text-sm text-gray-600">
                Contributing to concepts that resonate with others amplifies your energy 
                and strengthens the collective field.
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
