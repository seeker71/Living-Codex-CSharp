'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface UserProfile {
  userId: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  location?: string;
  latitude?: number;
  longitude?: number;
  interests?: string[];
  contributions?: string[];
  metadata?: Record<string, any>;
}

interface ConceptContributor {
  userId: string;
  displayName: string;
  email: string;
  contributionType: string;
  contributionScore: number;
  avatarUrl?: string;
  location?: string;
  relevantInterests?: string[];
}

export default function PeoplePage() {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [discoveredUsers, setDiscoveredUsers] = useState<UserProfile[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [conceptContributors, setConceptContributors] = useState<ConceptContributor[]>([]);
  const [loading, setLoading] = useState(false);
  const [discoveryType, setDiscoveryType] = useState<string>('interests');
  const [searchQuery, setSearchQuery] = useState('');
  const [locationQuery, setLocationQuery] = useState('');
  const [radiusKm, setRadiusKm] = useState(50);
  const [selectedConcept, setSelectedConcept] = useState('');

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('people-page', 'page-visit', { description: 'User visited people discovery page' });
    }
  }, [user?.id, trackInteraction]);

  const discoverUsers = async () => {
    if (!searchQuery.trim() && !locationQuery.trim() && !selectedConcept.trim()) return;
    
    setLoading(true);
    try {
      const discoveryRequest: any = { limit: pageSize, skip: (currentPage - 1) * pageSize };
      
      switch (discoveryType) {
        case 'interests':
          discoveryRequest.interests = searchQuery.split(',').map(s => s.trim()).filter(s => s);
          break;
        case 'location':
          discoveryRequest.location = locationQuery;
          discoveryRequest.radiusKm = radiusKm;
          break;
        case 'concept':
          discoveryRequest.conceptId = selectedConcept;
          break;
      }

      const response = await fetch(buildApiUrl('/users/discover'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(discoveryRequest)
      });
      
      const data = await response.json();
      if (data.users) {
        setDiscoveredUsers(data.users);
        if (typeof data.totalCount === 'number') setTotalCount(data.totalCount);
        
        // Track discovery interaction
        if (user?.id) {
          trackInteraction('people-discovery', 'search', { 
            description: `User discovered people by ${discoveryType}`,
            discoveryType,
            query: searchQuery || locationQuery || selectedConcept,
            resultCount: data.users.length
          });
        }
      }
    } catch (error) {
      console.error('Error discovering users:', error);
    } finally {
      setLoading(false);
    }
  };

  const discoverConceptContributors = async (conceptId: string) => {
    if (!conceptId.trim()) return;
    
    setLoading(true);
    try {
      const response = await fetch(buildApiUrl(`/concepts/${conceptId}/contributors`));
      const data = await response.json();
      if (data.contributors) {
        setConceptContributors(data.contributors);
      }
    } catch (error) {
      console.error('Error discovering concept contributors:', error);
    } finally {
      setLoading(false);
    }
  };

  const calculateResonanceOverlap = (userInterests: string[] = [], myInterests: string[] = ['technology', 'science']) => {
    const overlap = userInterests.filter(interest => 
      myInterests.some(myInt => myInt.toLowerCase().includes(interest.toLowerCase()) || 
                               interest.toLowerCase().includes(myInt.toLowerCase()))
    );
    return Math.round((overlap.length / Math.max(userInterests.length, myInterests.length, 1)) * 100);
  };

  const getAvatarInitials = (displayName: string) => {
    return displayName.split(' ').map(name => name.charAt(0)).join('').toUpperCase().slice(0, 2);
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">🌍 People Discovery</h1>
          <p className="text-gray-600 dark:text-gray-300">
            Discover people through resonance overlap, shared interests, and concept contributions
          </p>
        </div>

        {/* Discovery Controls */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Discovery Type */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                Discovery Method
              </label>
              <select
                value={discoveryType}
                onChange={(e) => setDiscoveryType(e.target.value)}
                className="input-standard"
              >
                <option value="interests">🎯 By Interests</option>
                <option value="location">📍 By Location</option>
                <option value="concept">🧠 By Concept</option>
              </select>
            </div>

            {/* Search Input */}
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                {discoveryType === 'interests' ? 'Interests (comma-separated)' :
                 discoveryType === 'location' ? 'Location' : 'Concept ID'}
              </label>
              <input
                type="text"
                value={discoveryType === 'location' ? locationQuery : 
                       discoveryType === 'concept' ? selectedConcept : searchQuery}
                onChange={(e) => {
                  if (discoveryType === 'location') setLocationQuery(e.target.value);
                  else if (discoveryType === 'concept') setSelectedConcept(e.target.value);
                  else setSearchQuery(e.target.value);
                }}
                placeholder={
                  discoveryType === 'interests' ? 'technology, science, art...' :
                  discoveryType === 'location' ? 'San Francisco, CA' :
                  'concept-ai-123'
                }
                className="input-standard"
                onKeyPress={(e) => e.key === 'Enter' && discoverUsers()}
              />
            </div>

            {/* Action Button */}
            <div className="flex items-end">
              <button
                onClick={discoverUsers}
                disabled={loading}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
              >
                {loading ? '🔍 Discovering...' : '🔍 Discover People'}
              </button>
            </div>
          </div>

          {/* Location Radius (only for location discovery) */}
          {discoveryType === 'location' && (
            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
                Search Radius: {radiusKm} km
              </label>
              <input
                type="range"
                min="1"
                max="500"
                value={radiusKm}
                onChange={(e) => setRadiusKm(parseInt(e.target.value))}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-gray-500 mt-1">
                <span>1 km</span>
                <span>500 km</span>
              </div>
            </div>
          )}
        </div>

        {/* Results */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Results */}
          <div className="lg:col-span-2">
            <Card className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
              <CardHeader>
                <CardTitle className="text-xl text-gray-900 dark:text-gray-100 flex items-center justify-between">
                  <span>🌍 Discovered People ({totalCount})</span>
                </CardTitle>
              </CardHeader>

              <CardContent>
                <div className="divide-y divide-gray-200 dark:divide-gray-700">
                {loading ? (
                  <div className="p-8 text-center">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-2 text-gray-500">Discovering people...</p>
                  </div>
                ) : discoveredUsers.length > 0 ? (
                  discoveredUsers.map((person, index) => {
                    const resonanceOverlap = calculateResonanceOverlap(person.interests);
                    return (
                      <div key={index} className="p-6 hover:bg-gray-50 dark:hover:bg-gray-700">
                        <div className="flex items-start space-x-4">
                          {/* Avatar */}
                          <div className="flex-shrink-0">
                            {person.avatarUrl ? (
                              <img
                                src={person.avatarUrl}
                                alt={person.displayName}
                                className="w-12 h-12 rounded-full object-cover"
                              />
                            ) : (
                              <div className="w-12 h-12 rounded-full bg-blue-500 flex items-center justify-center text-white font-medium">
                                {getAvatarInitials(person.displayName)}
                              </div>
                            )}
                          </div>

                          {/* User Info */}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center justify-between">
                              <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                                {person.displayName}
                              </h3>
                              <div className="flex items-center space-x-2">
                                <span className="text-sm text-blue-600 dark:text-blue-400 font-medium">
                                  {resonanceOverlap}% resonance
                                </span>
                                <div className={`w-3 h-3 rounded-full ${
                                  resonanceOverlap > 70 ? 'bg-green-500' :
                                  resonanceOverlap > 40 ? 'bg-yellow-500' : 'bg-gray-400'
                                }`}></div>
                              </div>
                            </div>
                            
                            {person.location && (
                              <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                                📍 {person.location}
                              </p>
                            )}
                            
                            {person.interests && person.interests.length > 0 && (
                              <div className="mb-3">
                                <p className="text-sm text-gray-600 dark:text-gray-300 mb-1">Interests:</p>
                                <div className="flex flex-wrap gap-1">
                                  {person.interests.slice(0, 5).map((interest, idx) => (
                                    <span
                                      key={idx}
                                      className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded-md text-xs"
                                    >
                                      {interest}
                                    </span>
                                  ))}
                                  {person.interests.length > 5 && (
                                    <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-md text-xs">
                                      +{person.interests.length - 5} more
                                    </span>
                                  )}
                                </div>
                              </div>
                            )}

                            {person.contributions && person.contributions.length > 0 && (
                              <div className="mb-3">
                                <p className="text-sm text-gray-600 dark:text-gray-300 mb-1">Recent Contributions:</p>
                                <div className="flex flex-wrap gap-1">
                                  {person.contributions.slice(0, 3).map((contrib, idx) => (
                                    <span
                                      key={idx}
                                      className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 rounded-md text-xs"
                                    >
                                      {contrib}
                                    </span>
                                  ))}
                                </div>
                              </div>
                            )}

                            <div className="flex items-center space-x-3 text-sm">
                              <button className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 transition-colors">
                                🤝 Connect
                              </button>
                              <button className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 transition-colors">
                                💬 Message
                              </button>
                              <button className="text-purple-600 dark:text-purple-400 hover:text-purple-800 dark:hover:text-purple-300 transition-colors">
                                🔗 View Profile
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })
                ) : (
                  <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                    {discoveryType === 'interests' 
                      ? "Enter interests to discover people with similar passions"
                      : discoveryType === 'location'
                      ? "Enter a location to find people nearby"
                      : "Enter a concept ID to find contributors"}
                  </div>
                )}
                </div>

                {totalCount > pageSize && (
                  <div className="pt-4">
                    <PaginationControls
                      currentPage={currentPage}
                      pageSize={pageSize}
                      totalCount={totalCount}
                      onPageChange={(p) => { setCurrentPage(p); discoverUsers(); }}
                    />
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Discovery Stats */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">📊 Discovery Stats</h3>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">People Found</span>
                  <span className="font-medium">{discoveredUsers.length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-300">Method</span>
                  <span className="font-medium capitalize">{discoveryType}</span>
                </div>
                {discoveryType === 'location' && (
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-300">Radius</span>
                    <span className="font-medium">{radiusKm} km</span>
                  </div>
                )}
              </div>
            </div>

            {/* Quick Discovery */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">⚡ Quick Discovery</h3>
              <div className="space-y-3">
                <button
                  onClick={() => {
                    setDiscoveryType('interests');
                    setSearchQuery('technology, AI, consciousness');
                    setTimeout(discoverUsers, 100);
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  🤖 Tech Enthusiasts
                </button>
                <button
                  onClick={() => {
                    setDiscoveryType('interests');
                    setSearchQuery('science, research, innovation');
                    setTimeout(discoverUsers, 100);
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  🔬 Science Researchers
                </button>
                <button
                  onClick={() => {
                    setDiscoveryType('interests');
                    setSearchQuery('consciousness, spirituality, resonance');
                    setTimeout(discoverUsers, 100);
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  🧘 Consciousness Explorers
                </button>
                <button
                  onClick={() => {
                    setDiscoveryType('location');
                    setLocationQuery('San Francisco, CA');
                    setRadiusKm(25);
                    setTimeout(discoverUsers, 100);
                  }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                >
                  📍 Bay Area People
                </button>
              </div>
            </div>

            {/* Resonance Guide */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">🌊 Resonance Guide</h3>
              <div className="space-y-3 text-sm">
                <div className="flex items-center space-x-2">
                  <div className="w-3 h-3 rounded-full bg-green-500"></div>
                  <span>70%+ High Resonance</span>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
                  <span>40-70% Medium Resonance</span>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-3 h-3 rounded-full bg-gray-400"></div>
                  <span>&lt;40% Low Resonance</span>
                </div>
                <p className="text-xs text-gray-500 mt-3">
                  💡 Resonance is calculated based on shared interests and concept interactions
                </p>
              </div>
            </div>

            {/* Your Interests */}
            {user && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">🎨 Your Interests</h3>
                <div className="text-sm text-gray-600">
                  <p className="mb-2">People discovery is based on your profile:</p>
                  <div className="flex flex-wrap gap-2">
                    {['technology', 'science', 'consciousness', 'innovation'].map((interest) => (
                      <span
                        key={interest}
                        className="px-2 py-1 bg-purple-100 text-purple-800 rounded-md text-xs"
                      >
                        {interest}
                      </span>
                    ))}
                  </div>
                  <p className="mt-3 text-xs">
                    💡 Tip: Interact with more concepts to improve discovery matching!
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Concept Contributors Section */}
        {conceptContributors.length > 0 && (
          <div className="mt-8">
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                  🧠 Concept Contributors ({conceptContributors.length})
                </h2>
              </div>

              <div className="divide-y divide-gray-200">
                {conceptContributors.map((contributor, index) => (
                  <div key={index} className="p-6 hover:bg-gray-50">
                    <div className="flex items-start space-x-4">
                      <div className="flex-shrink-0">
                        {contributor.avatarUrl ? (
                          <img
                            src={contributor.avatarUrl}
                            alt={contributor.displayName}
                            className="w-10 h-10 rounded-full object-cover"
                          />
                        ) : (
                          <div className="w-10 h-10 rounded-full bg-green-500 flex items-center justify-center text-white font-medium text-sm">
                            {getAvatarInitials(contributor.displayName)}
                          </div>
                        )}
                      </div>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between">
                          <h4 className="text-lg font-medium text-gray-900">
                            {contributor.displayName}
                          </h4>
                          <div className="flex items-center space-x-2">
                            <span className="text-sm text-green-600 font-medium">
                              {contributor.contributionType}
                            </span>
                            <span className="text-sm text-gray-500">
                              Score: {contributor.contributionScore.toFixed(1)}
                            </span>
                          </div>
                        </div>
                        
                        {contributor.location && (
                          <p className="text-sm text-gray-500">📍 {contributor.location}</p>
                        )}
                        
                        {contributor.relevantInterests && (
                          <div className="mt-2">
                            <div className="flex flex-wrap gap-1">
                              {contributor.relevantInterests.slice(0, 3).map((interest, idx) => (
                                <span
                                  key={idx}
                                  className="px-2 py-1 bg-green-100 text-green-800 rounded-md text-xs"
                                >
                                  {interest}
                                </span>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
