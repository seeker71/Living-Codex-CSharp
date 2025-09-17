'use client';

import { useEffect, useState } from 'react';
import { Navigation } from '@/components/ui/Navigation';
import { api } from '@/lib/api';

// StatusBadge component for route status display
function StatusBadge({ status }: { status: string }) {
  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'fullytested': return 'bg-green-100 text-green-800';
      case 'partiallytested': return 'bg-yellow-100 text-yellow-800';
      case 'simple': return 'bg-blue-100 text-blue-800';
      case 'aienabled': return 'bg-purple-100 text-purple-800';
      case 'externalinfo': return 'bg-indigo-100 text-indigo-800';
      case 'stub': return 'bg-gray-100 text-gray-800';
      case 'simulated': return 'bg-orange-100 text-orange-800';
      case 'untested': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-600';
    }
  };

  return (
    <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(status)}`}>
      {status}
    </span>
  );
}

type StorageStats = {
  success?: boolean;
  stats?: {
    nodeCount?: number;
    edgeCount?: number;
    totalSizeBytes?: number;
    lastUpdated?: string;
  };
};

export default function GraphPage() {
  const [stats, setStats] = useState<StorageStats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadStorageStats() {
      try {
        const response = await api.get('/storage-endpoints/stats');
        if (response.success) {
          setStats(response.data as StorageStats);
        } else {
          setError(response.error || 'Failed to load storage stats');
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      }
    }

    loadStorageStats();
  }, []);

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900">Graph</h1>
              <StatusBadge status="Simple" />
            </div>
            <Navigation />
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto p-6">
        <section className="mb-6">
          <h2 className="text-lg font-semibold mb-2">Storage Overview</h2>
          {error && <div className="text-red-600">{error}</div>}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="rounded-lg border bg-white p-4">
              <div className="text-gray-500">Nodes</div>
              <div className="text-2xl font-semibold">{stats?.stats?.nodeCount ?? '—'}</div>
            </div>
            <div className="rounded-lg border bg-white p-4">
              <div className="text-gray-500">Edges</div>
              <div className="text-2xl font-semibold">{stats?.stats?.edgeCount ?? '—'}</div>
            </div>
            <div className="rounded-lg border bg-white p-4">
              <div className="text-gray-500">Last Updated</div>
              <div className="text-2xl font-semibold">{stats?.stats?.lastUpdated ? new Date(stats.stats.lastUpdated).toLocaleString() : '—'}</div>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-lg font-semibold mb-2">Coming Soon</h2>
          <p className="text-gray-600">Interactive graph with lens layouts and node adapters.</p>
        </section>
      </main>
    </div>
  );
}


