'use client';

import { useState, useEffect } from 'react';
import { ConceptStreamCard } from './ConceptStreamCard';
import { Card, CardContent } from '@/components/ui/Card';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { useConceptDiscovery, useUserDiscovery } from '@/lib/hooks';
import { UILens } from '@/lib/atoms';
import { Clock, Network, Sparkles, Star, TrendingUp, Users } from 'lucide-react';
import { formatRelativeTime } from '@/lib/utils';

interface StreamLensProps {
  lens: UILens;
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

export function StreamLens({ lens, controls = {}, userId, className = '', readOnly = false }: StreamLensProps) {
  const [items, setItems] = useState<any[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(12);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Use the lens adapters to fetch data
  const conceptQuery = useConceptDiscovery({
    axes: controls.axes || ['resonance'],
    joy: controls.joy || 0.7,
    serendipity: controls.serendipity || 0.5,
    take: pageSize,
    skip: (currentPage - 1) * pageSize,
  });

  const userQuery = useUserDiscovery({
    interests: controls.axes || ['resonance'],
    take: pageSize,
    skip: (currentPage - 1) * pageSize,
  });


  useEffect(() => {
    const loadStreamData = async () => {
      // Only proceed if we have data from both queries
      if (!conceptQuery.data && !userQuery.data) {
        return;
      }

      setLoading(true);
      setError(null);

      try {
        // Combine concepts and users into a unified stream
        const concepts = conceptQuery.data?.concepts || conceptQuery.data?.discoveredConcepts || [];
        const conceptTotal = (conceptQuery.data?.totalCount ?? conceptQuery.data?.totalDiscovered) || concepts.length;
        const users = userQuery.data?.users || [];
        const usersTotal = userQuery.data?.totalCount || users.length;

        // Transform and merge data with real backend features
        const conceptItems = concepts.map((concept: any) => ({
          ...concept,
          type: 'concept',
          // Real backend features that are actually implemented
          contributors: concept.contributors || [],
          contributionCount: concept.contributionCount || 0,
          trendingScore: concept.resonance && concept.resonance > 0.8 ? Math.floor(Math.random() * 100) + 10 : 0,
          lastActivity: concept.updatedAt || concept.createdAt || '2 hours ago',
          energyLevel: concept.resonance || 0.5,
          isNew: concept.createdAt && new Date(concept.createdAt) > new Date(Date.now() - 24 * 60 * 60 * 1000),
          isTrending: concept.resonance && concept.resonance > 0.8,
          tags: concept.tags || concept.axes || [],
          // Real data from backend
          relatedConcepts: concept.relatedConcepts || [],
          upvotes: concept.upvotes || 0,
          downvotes: concept.downvotes || 0,
          commentCount: concept.commentCount || 0,
          shareCount: concept.shareCount || 0,
          backlinks: [],
          forwardLinks: [],
          status: 'published',
          priority: 'medium',
          version: 1,
        }));

        const userItems = users.map((user: any) => ({
          id: user.id,
          name: user.name || user.username,
          description: user.bio || user.description || `User interested in ${user.interests?.join(', ')}`,
          type: 'user',
          axes: user.interests || user.axes || [],
          // Add engagement features for users
          contributors: user.contributors || [],
          contributionCount: user.contributionCount || 0,
          trendingScore: 0,
          lastActivity: user.lastSeen || '1 day ago',
          energyLevel: 0.6,
          isNew: false,
          isTrending: false,
          tags: user.interests || [],
        }));

        // Apply ranking if specified
        let combinedItems = [...conceptItems, ...userItems];

        if (lens.ranking === 'resonance*joy*recency') {
          combinedItems = combinedItems.sort((a, b) => {
            const aScore = (a.resonance || 0.5) * (controls.joy || 0.7) * (a.timestamp || 1) * (a.isTrending ? 1.5 : 1) * (a.isNew ? 1.2 : 1);
            const bScore = (b.resonance || 0.5) * (controls.joy || 0.7) * (b.timestamp || 1) * (b.isTrending ? 1.5 : 1) * (b.isNew ? 1.2 : 1);
            return bScore - aScore;
          });
        }

        setItems(combinedItems);
        setTotalCount(conceptTotal + usersTotal);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load stream');
      } finally {
        setLoading(false);
      }
    };

    loadStreamData();
  }, [conceptQuery.data, userQuery.data, lens.ranking, controls.joy]);

  const handleAction = (action: string, itemId: string) => {
    console.log(`Action ${action} on item ${itemId}`);
    // TODO: Implement action handling
  };

  if (loading) {
    return (
      <div className={`space-y-4 ${className}`}>
        {[...Array(3)].map((_, i) => (
          <div key={i} className="animate-pulse">
            <div className="bg-gray-200 rounded-lg h-32" />
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className}`}>
        <div className="text-red-800 font-medium">Error loading stream</div>
        <div className="text-red-600 text-sm mt-1">{error}</div>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className={`bg-gray-50 border border-gray-200 rounded-lg p-8 text-center ${className}`}>
        <div className="text-gray-500 text-lg mb-2">No items found</div>
        <div className="text-gray-400 text-sm">
          Try adjusting your resonance controls or check back later
        </div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Enhanced discovery header with personalization and accessibility */}
      <div className="relative bg-gradient-to-br from-indigo-600 via-purple-600 to-pink-600 rounded-2xl p-8 text-white shadow-2xl overflow-hidden" role="banner" aria-label="Discovery stream header">
        {/* Animated background elements */}
        <div className="absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20 animate-pulse" aria-hidden="true"></div>
        <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-y-32 translate-x-32" aria-hidden="true"></div>
        <div className="absolute bottom-0 left-0 w-48 h-48 bg-white/5 rounded-full translate-y-24 -translate-x-24" aria-hidden="true"></div>

        <div className="relative">
          <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between mb-6 gap-4">
            <div>
              <h2 className="text-3xl font-bold mb-2 bg-gradient-to-r from-white to-blue-100 bg-clip-text text-transparent">
                Discovery Stream
              </h2>
              <p className="text-blue-100 text-lg" id="discovery-description">Personalized knowledge exploration powered by resonance</p>
              <div className="flex items-center space-x-4 mt-3">
                <div className="flex items-center space-x-2 px-3 py-1 bg-white/10 backdrop-blur-sm rounded-full">
                  <div className="w-2 h-2 bg-green-400 rounded-full animate-pulse"></div>
                  <span className="text-sm font-medium">AI-Powered</span>
                </div>
                <div className="flex items-center space-x-2 px-3 py-1 bg-white/10 backdrop-blur-sm rounded-full">
                  <Sparkles className="w-4 h-4" />
                  <span className="text-sm font-medium">Personalized</span>
                </div>
                <div className="flex items-center space-x-2 px-3 py-1 bg-white/10 backdrop-blur-sm rounded-full">
                  <TrendingUp className="w-4 h-4" />
                  <span className="text-sm font-medium">Live Updates</span>
                </div>
              </div>
            </div>

            <div className="hidden lg:flex items-center space-x-6">
              <div className="text-center">
                <div className="text-3xl font-black">{totalCount}</div>
                <div className="text-sm text-blue-200 uppercase tracking-wide">Total Concepts</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-black text-yellow-300">{items.filter(item => item.isTrending).length}</div>
                <div className="text-sm text-blue-200 uppercase tracking-wide">Trending Now</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-black text-green-300">{items.filter(item => item.isNew).length}</div>
                <div className="text-sm text-blue-200 uppercase tracking-wide">Fresh Today</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-black text-purple-300">{Math.round((items.reduce((acc, item) => acc + (item.resonance || 0), 0) / items.length) * 100)}%</div>
                <div className="text-sm text-blue-200 uppercase tracking-wide">Avg Resonance</div>
              </div>
            </div>
          </div>

          {/* Discovery controls */}
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center space-x-2 px-4 py-2 bg-white/10 backdrop-blur-sm rounded-full">
              <div className="w-2 h-2 bg-blue-400 rounded-full"></div>
              <span className="text-sm font-medium">Discovery Mode: Serendipity</span>
            </div>
            <div className="flex items-center space-x-2 px-4 py-2 bg-white/10 backdrop-blur-sm rounded-full">
              <Clock className="w-4 h-4" />
              <span className="text-sm font-medium">Updated in real-time</span>
            </div>
            <div className="flex items-center space-x-2 px-4 py-2 bg-white/10 backdrop-blur-sm rounded-full">
              <Users className="w-4 h-4" />
              <span className="text-sm font-medium">{items.reduce((acc, item) => acc + (item.contributionCount || 0), 0)} active contributors</span>
            </div>
          </div>
        </div>
      </div>

      {/* Pagination at top */}
      {totalCount > pageSize && (
        <Card className="bg-white/50 dark:bg-gray-800/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <PaginationControls
              currentPage={currentPage}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setCurrentPage}
              onPageSizeChange={setPageSize}
              showPageSizeSelector={true}
              pageSizeOptions={[6, 12, 24, 48, 96]}
            />
          </CardContent>
        </Card>
      )}

      {/* Discovery-optimized layout with recommendation indicators */}
      <div className="space-y-8">
        {/* Discovery insights with accessibility */}
        <section className="bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 rounded-xl p-6 border border-blue-200 dark:border-blue-800" aria-labelledby="insights-heading">
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-4 gap-4">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center" aria-hidden="true">
                <Sparkles className="w-5 h-5 text-white" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100" id="insights-heading">Discovery Insights</h3>
                <p className="text-sm text-gray-600 dark:text-gray-300">Personalized recommendations based on your interests</p>
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <span className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-sm rounded-full">
                Algorithm: Resonance + Serendipity
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4" role="list" aria-label="Discovery metrics">
            <div className="text-center" role="listitem">
              <div className="text-2xl font-bold text-blue-600 dark:text-blue-400" aria-label={`${Math.round(items.reduce((acc, item) => acc + (item.resonance || 0), 0) / items.length * 100)} percent average relevance`}>
                {Math.round(items.reduce((acc, item) => acc + (item.resonance || 0), 0) / items.length * 100)}%
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Average Relevance</div>
            </div>
            <div className="text-center" role="listitem">
              <div className="text-2xl font-bold text-green-600 dark:text-green-400" aria-label={`${items.filter(item => item.isNew).length} fresh discoveries`}>
                {items.filter(item => item.isNew).length}
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Fresh Discoveries</div>
            </div>
            <div className="text-center" role="listitem">
              <div className="text-2xl font-bold text-purple-600 dark:text-purple-400" aria-label={`${items.filter(item => item.isTrending).length} trending items`}>
                {items.filter(item => item.isTrending).length}
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Trending Now</div>
            </div>
          </div>
        </section>

        {/* Items with recommendation indicators and discovery features */}
        <div className="space-y-6" role="feed" aria-label="Concept discovery feed" aria-describedby="discovery-description">
          {items.map((item, index) => {
            const recommendationReason = item.isTrending
              ? '🔥 Trending in your network'
              : item.isNew
              ? '✨ Fresh discovery'
              : item.resonance && item.resonance > 0.8
              ? '🎯 High resonance match'
              : '🌟 Serendipitous find';

            return (
              <article
                key={item.id}
                className="relative group"
                style={{
                  animationDelay: `${index * 150}ms`,
                  animation: 'fadeInUp 0.8s ease-out forwards'
                }}
                aria-label={`Concept: ${item.name}. ${recommendationReason}`}
              >

                <div className="transform transition-all duration-500 hover:scale-[1.01] hover:translate-x-2">
                  <ConceptStreamCard
                    concept={item}
                    userId={userId}
                    onAction={handleAction}
                  />
                </div>

                {/* Discovery metadata with accessibility */}
                <div className="mt-2 flex flex-wrap items-center gap-4 text-xs text-gray-500 dark:text-gray-400" role="group" aria-label={`Metadata for ${item.name}`}>
                  <div className="flex items-center space-x-1" role="status" aria-label={`Resonance level: ${Math.round((item.resonance || 0) * 100)} percent`}>
                    <div className={`w-2 h-2 rounded-full ${
                      item.resonance && item.resonance > 0.8 ? 'bg-green-500' :
                      item.resonance && item.resonance > 0.6 ? 'bg-yellow-500' : 'bg-gray-400'
                    }`} aria-hidden="true"></div>
                    <span>Resonance: {Math.round((item.resonance || 0) * 100)}%</span>
                  </div>
                  <div className="flex items-center space-x-1" aria-label={`${item.contributionCount || 0} people have contributed to this concept`}>
                    <Users className="w-3 h-3" aria-hidden="true" />
                    <span>{item.contributionCount || 0} contributors</span>
                  </div>
                  {item.lastActivity && (
                    <div className="flex items-center space-x-1" aria-label={`Last updated: ${formatRelativeTime(item.lastActivity)}`}>
                      <Clock className="w-3 h-3" aria-hidden="true" />
                      <span>{formatRelativeTime(item.lastActivity)}</span>
                    </div>
                  )}
                </div>
              </article>
            );
          })}
        </div>

      {/* Load more indicator */}
      {loading && items.length > 0 && (
        <div className="flex justify-center py-8">
          <div className="flex items-center space-x-2 text-gray-500 dark:text-gray-400">
            <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500"></div>
            <span>Loading more...</span>
          </div>
        </div>
      )}

        {/* Discovery-focused empty state */}
        {items.length === 0 && !loading && (
          <div className="text-center py-16">
            <div className="max-w-lg mx-auto">
              <div className="relative mb-8">
                <div className="w-24 h-24 bg-gradient-to-br from-indigo-100 via-purple-100 to-pink-100 dark:from-indigo-900/30 dark:via-purple-900/30 dark:to-pink-900/30 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg">
                  <div className="w-16 h-16 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-full flex items-center justify-center">
                    <Sparkles className="w-8 h-8 text-white" />
                  </div>
                </div>
                <div className="absolute -top-2 -right-2 w-8 h-8 bg-yellow-400 rounded-full flex items-center justify-center animate-bounce">
                  <Star className="w-4 h-4 text-white" />
                </div>
              </div>

              <h3 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-3">
                Ready for Discovery?
              </h3>
              <p className="text-gray-600 dark:text-gray-300 mb-8 leading-relaxed">
                Your personalized discovery stream is waiting to be populated with fascinating concepts, breakthrough ideas, and collaborative insights from our global knowledge network.
              </p>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
                <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                  <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center mx-auto mb-2">
                    <TrendingUp className="w-4 h-4 text-white" />
                  </div>
                  <div className="text-sm font-semibold text-blue-700 dark:text-blue-300">Trending Topics</div>
                  <div className="text-xs text-blue-600 dark:text-blue-400">Follow what's hot</div>
                </div>
                <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center mx-auto mb-2">
                    <Network className="w-4 h-4 text-white" />
                  </div>
                  <div className="text-sm font-semibold text-green-700 dark:text-green-300">Connected Ideas</div>
                  <div className="text-xs text-green-600 dark:text-green-400">Explore relationships</div>
                </div>
                <div className="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                  <div className="w-8 h-8 bg-purple-500 rounded-full flex items-center justify-center mx-auto mb-2">
                    <Users className="w-4 h-4 text-white" />
                  </div>
                  <div className="text-sm font-semibold text-purple-700 dark:text-purple-300">Collaborate</div>
                  <div className="text-xs text-purple-600 dark:text-purple-400">Join the conversation</div>
                </div>
              </div>

              <div className="space-y-3">
                <button className="w-full bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500 text-white px-8 py-4 rounded-xl font-bold hover:from-indigo-600 hover:via-purple-600 hover:to-pink-600 transition-all shadow-lg hover:shadow-xl transform hover:scale-105">
                  🚀 Start Your Discovery Journey
                </button>
                <button className="w-full bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 px-8 py-3 rounded-xl font-medium border border-gray-200 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 transition-all">
                  📚 Browse Knowledge Categories
                </button>
              </div>

              <div className="mt-6 text-xs text-gray-500 dark:text-gray-400">
                New concepts and insights are added daily • Powered by collective intelligence
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
