'use client';

import { useState, useCallback, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { buildApiUrl } from '@/lib/config';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';

interface NearbyLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

interface DiscoveredUser {
  userId: string;
  displayName: string;
  email?: string;
  avatarUrl?: string;
  location?: string;
  latitude?: number;
  longitude?: number;
  interests?: string[];
  resonanceScore?: number;
  contributions?: string[];
}

const DEFAULT_PAGE_SIZE = 12;

export function NearbyLens({ controls = {}, userId, className = '', readOnly = false }: NearbyLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();

  const [locationInput, setLocationInput] = useState('');
  const [activeLocation, setActiveLocation] = useState('');
  const [radiusKm, setRadiusKm] = useState(50);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [users, setUsers] = useState<DiscoveredUser[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasSearchCriteria = activeLocation.trim().length > 0;
  const axes = Array.isArray(controls.axes) && controls.axes.length > 0 ? controls.axes : undefined;

  const fetchNearbyUsers = useCallback(async (requestedPage: number) => {
    if (!activeLocation) {
      setUsers([]);
      setTotalCount(0);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const skip = (requestedPage - 1) * pageSize;
      const payload: Record<string, any> = {
        location: activeLocation,
        radiusKm,
        limit: pageSize,
        skip,
      };

      if (axes) {
        payload.interests = axes;
      }

      const response = await fetch(buildApiUrl('/users/discover'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      const discovered = Array.isArray(data.users) ? data.users : [];
      setUsers(discovered);
      setTotalCount(typeof data.totalCount === 'number' ? data.totalCount : discovered.length);

      if (user?.id) {
        trackInteraction('lens.nearby', 'search', {
          description: 'Nearby lens search executed',
          location: activeLocation,
          radiusKm,
          interestAxes: axes,
          resultCount: discovered.length,
        });
      }
    } catch (err) {
      console.error('NearbyLens error:', err);
      setError(err instanceof Error ? err.message : 'Failed to load nearby users');
      setUsers([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  }, [activeLocation, radiusKm, pageSize, axes, user?.id, trackInteraction]);

  useEffect(() => {
    setPage(1);
    if (hasSearchCriteria) {
      fetchNearbyUsers(1);
    }
  }, [activeLocation, radiusKm, fetchNearbyUsers, hasSearchCriteria]);

  const handleSearch = () => {
    const trimmed = locationInput.trim();
    if (!trimmed) {
      setActiveLocation('');
      setUsers([]);
      setTotalCount(0);
      return;
    }
    setActiveLocation(trimmed);
  };

  const handlePageChange = (nextPage: number) => {
    setPage(nextPage);
    fetchNearbyUsers(nextPage);
  };

  return (
    <div className={`space-y-6 ${className}`}>
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center space-x-2">
            <span>📍 Nearby Explorers</span>
          </CardTitle>
          <CardDescription>
            Discover people within {radiusKm} km who align with your resonance axes{axes ? ` (${axes.join(', ')})` : ''}.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-secondary mb-2">
                Location
              </label>
              <div className="flex space-x-2">
                <input
                  type="text"
                  value={locationInput}
                  onChange={(event) => setLocationInput(event.target.value)}
                  placeholder="City, region, or landmark"
                  className="flex-1 input-standard"
                  onKeyDown={(event) => {
                    if (event.key === 'Enter') {
                      handleSearch();
                    }
                  }}
                />
                <button
                  type="button"
                  onClick={handleSearch}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  Search
                </button>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-secondary mb-2">
                Radius: {radiusKm} km
              </label>
              <input
                type="range"
                min={5}
                max={500}
                step={5}
                value={radiusKm}
                onChange={(event) => setRadiusKm(Number(event.target.value))}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-muted mt-1">
                <span>Local</span>
                <span>Global</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {loading && (
        <div className="space-y-4">
          {[...Array(3)].map((_, index) => (
            <div key={index} className="animate-pulse">
              <div className="bg-gray-200 dark:bg-gray-700 rounded-lg h-24" />
            </div>
          ))}
        </div>
      )}

      {!loading && error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="text-red-800 dark:text-red-200 font-medium">Unable to load nearby explorers.</div>
          <div className="text-sm text-red-700 dark:text-red-300 mt-1">{error}</div>
        </div>
      )}

      {!loading && !error && !hasSearchCriteria && (
        <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 text-center">
          <p className="text-sm text-gray-600 dark:text-gray-300">
            Enter a location to find resonant collaborators nearby.
          </p>
        </div>
      )}

      {!loading && !error && hasSearchCriteria && users.length === 0 && (
        <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 text-center">
          <p className="text-sm text-gray-600 dark:text-gray-300">
            No nearby explorers found. Try widening your radius or adjusting resonance axes.
          </p>
        </div>
      )}

      {!loading && users.length > 0 && (
        <div className="space-y-4">
          {/* Pagination at top */}
          {totalCount > pageSize && (
            <Card>
              <CardContent className="p-4">
                <PaginationControls
                  currentPage={page}
                  pageSize={pageSize}
                  totalCount={totalCount}
                  onPageChange={handlePageChange}
                  onPageSizeChange={setPageSize}
                  showPageSizeSelector={true}
                  pageSizeOptions={[6, 12, 24, 48, 96]}
                />
              </CardContent>
            </Card>
          )}

          {/* Users grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {users.map((item) => (
              <Card key={item.userId} className="h-full">
                <CardContent className="p-4 space-y-3">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                        {item.displayName || item.userId}
                      </h3>
                      {item.location && (
                        <p className="text-sm text-gray-600 dark:text-gray-300">{item.location}</p>
                      )}
                    </div>
                    {typeof item.resonanceScore === 'number' && (
                      <span className="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-200">
                        Resonance {Math.round(item.resonanceScore * 100)}%
                      </span>
                    )}
                  </div>

                  {item.interests && item.interests.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {item.interests.slice(0, 6).map((interest) => (
                        <span
                          key={interest}
                          className="px-2 py-1 text-xs rounded-full bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-200"
                        >
                          {interest}
                        </span>
                      ))}
                    </div>
                  )}

                  {item.contributions && item.contributions.length > 0 && (
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      Contributions: {item.contributions.join(', ')}
                    </p>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default NearbyLens;
