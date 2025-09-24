'use client';

import { useState, useMemo, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from '@/components/ui/Card';
import { useConceptDiscovery, useTrackInteraction, useAttune, useAmplify } from '@/lib/hooks';
import { useAuth } from '@/contexts/AuthContext';

interface SwipeLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

const PAGE_SIZE = 12;

export function SwipeLens({ controls = {}, userId, className = '' }: SwipeLensProps) {
  const { user } = useAuth();
  const effectiveUserId = userId || user?.id || 'demo-user';
  const trackInteraction = useTrackInteraction();
  const attune = useAttune();
  const amplify = useAmplify();

  const [page, setPage] = useState(1);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [dismissedIds, setDismissedIds] = useState<Set<string>>(new Set());

  const axes = Array.isArray(controls.axes) && controls.axes.length > 0 ? controls.axes : ['resonance'];
  const joy = typeof controls.joy === 'number' ? controls.joy : 0.7;
  const serendipity = typeof controls.serendipity === 'number' ? controls.serendipity : 0.5;

  const discoveryParams = useMemo(() => ({
    axes,
    joy,
    serendipity,
    take: PAGE_SIZE,
    skip: (page - 1) * PAGE_SIZE,
  }), [axes, joy, serendipity, page]);

  const discoveryQuery = useConceptDiscovery(discoveryParams);
  const { data, isLoading, isFetching } = discoveryQuery;

  const concepts: Array<Record<string, any>> = useMemo(() => {
    if (!data) return [];
    if (Array.isArray(data.discoveredConcepts)) {
      return data.discoveredConcepts;
    }
    if (Array.isArray((data as any).concepts)) {
      return (data as any).concepts;
    }
    return [];
  }, [data]);

  // Reset index when page or data changes
  useEffect(() => {
    setCurrentIndex(0);
  }, [page, concepts.length]);

  const currentConcept = concepts[currentIndex];

  const handleSkip = () => {
    if (currentConcept?.id) {
      setDismissedIds(prev => new Set(prev).add(currentConcept.id));
      trackInteraction(currentConcept.id, 'swipe-skip', {
        description: 'Concept skipped in Swipe lens',
        axes,
        joy,
        serendipity,
      });
    }

    if (currentIndex < concepts.length - 1) {
      setCurrentIndex((index) => index + 1);
    } else {
      setPage((prev) => prev + 1);
    }
  };

  const handleAttune = async () => {
    if (!currentConcept?.id) return;
    try {
      await attune.mutateAsync({ userId: effectiveUserId, conceptId: currentConcept.id });
      trackInteraction(currentConcept.id, 'swipe-attune', {
        description: 'Concept attuned via Swipe lens',
        axes,
        joy,
        serendipity,
      });
      handleSkip();
    } catch (error) {
      console.error('SwipeLens attune error:', error);
    }
  };

  const handleAmplify = async () => {
    if (!currentConcept?.id) return;
    try {
      await amplify.mutateAsync({
        userId: effectiveUserId,
        conceptId: currentConcept.id,
        contribution: `Amplified from Swipe lens: ${currentConcept.name || currentConcept.title}`,
      });
      trackInteraction(currentConcept.id, 'swipe-amplify', {
        description: 'Concept amplified via Swipe lens',
        axes,
        joy,
        serendipity,
      });
      handleSkip();
    } catch (error) {
      console.error('SwipeLens amplify error:', error);
    }
  };

  const renderLoading = () => (
    <div className="space-y-4">
      {[...Array(2)].map((_, index) => (
        <div key={index} className="animate-pulse">
          <div className="bg-gray-200 dark:bg-gray-700 rounded-lg h-32" />
        </div>
      ))}
    </div>
  );

  const renderEmptyState = () => (
    <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 text-center">
      <p className="text-sm text-gray-600 dark:text-gray-300">
        No additional concepts match your current resonance. Try adjusting the axes, joy, or serendipity controls.
      </p>
    </div>
  );

  return (
    <div className={`space-y-6 ${className}`}>
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center space-x-2">
            <span>👆 Swipe Concepts</span>
          </CardTitle>
          <CardDescription>
            Rapidly explore concepts tuned to your resonance. Attune to keep, amplify to boost, or skip to see the next match.
          </CardDescription>
        </CardHeader>
      </Card>

      {(isLoading || isFetching) && renderLoading()}

      {!isLoading && !isFetching && (!currentConcept || concepts.length === 0) && renderEmptyState()}

      {!isLoading && currentConcept && (
        <Card className="md:w-3/4 mx-auto">
          <CardHeader className="pb-2">
            <CardTitle className="flex items-center justify-between">
              <span>{currentConcept.name || currentConcept.title || currentConcept.id}</span>
              {typeof currentConcept.resonance === 'number' && (
                <span className="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-200">
                  Resonance {Math.round(currentConcept.resonance * 100)}%
                </span>
              )}
            </CardTitle>
            {currentConcept.axes && Array.isArray(currentConcept.axes) && currentConcept.axes.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-2">
                {currentConcept.axes.slice(0, 6).map((axis) => (
                  <span
                    key={axis}
                    className="px-2 py-1 text-xs rounded-full bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-200"
                  >
                    {axis}
                  </span>
                ))}
              </div>
            )}
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
              {currentConcept.description || 'No description available for this concept yet.'}
            </p>
            {currentConcept.meta?.category && (
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Category: {currentConcept.meta.category}
              </p>
            )}
          </CardContent>
          <CardFooter className="flex flex-wrap items-center justify-between gap-3 border-t border-gray-100 dark:border-gray-700">
            <div className="flex items-center space-x-2 text-xs text-gray-500 dark:text-gray-400">
              <span>Card {currentIndex + 1}</span>
              <span>•</span>
              <span>
                Page {page}
              </span>
            </div>
            <div className="flex items-center space-x-2">
              <button
                type="button"
                onClick={handleSkip}
                className="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
              >
                Skip
              </button>
              <button
                type="button"
                onClick={handleAttune}
                disabled={attune.isPending}
                className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors disabled:opacity-70"
              >
                {attune.isPending ? 'Attuning…' : 'Attune'}
              </button>
              <button
                type="button"
                onClick={handleAmplify}
                disabled={amplify.isPending}
                className="px-4 py-2 bg-amber-500 text-white rounded-md hover:bg-amber-600 transition-colors disabled:opacity-70"
              >
                {amplify.isPending ? 'Amplifying…' : 'Amplify'}
              </button>
            </div>
          </CardFooter>
        </Card>
      )}
    </div>
  );
}

export default SwipeLens;
