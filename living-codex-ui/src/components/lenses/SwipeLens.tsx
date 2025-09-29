'use client';

import { useState, useEffect, useRef } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Heart, X, Star, Users, MessageCircle, ExternalLink, RotateCcw, Info } from 'lucide-react';

interface SwipeLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

interface SwipeItem {
  id: string;
  name: string;
  description: string;
  type: 'concept' | 'person';
  image?: string;
  tags: string[];
  resonance: number;
  contributors: number;
  trending?: boolean;
}

export function SwipeLens({ controls = {}, userId, className = '', readOnly = false }: SwipeLensProps) {
  const [items, setItems] = useState<SwipeItem[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [swipeDirection, setSwipeDirection] = useState<'left' | 'right' | null>(null);
  const [showUndo, setShowUndo] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const fetchSwipeItems = async () => {
      setLoading(true);
      setError(null);

      try {
        // Fetch concepts and users in parallel
        const [conceptsResponse, usersResponse] = await Promise.all([
          fetch('http://localhost:5002/concepts/browse', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
          }),
          fetch('http://localhost:5002/users/discover', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ interests: [], limit: 10 })
          })
        ]);

        const items: SwipeItem[] = [];

        // Process concepts
        if (conceptsResponse.ok) {
          const conceptsData = await conceptsResponse.json();
          if (conceptsData.success && conceptsData.discoveredConcepts) {
            const conceptItems = conceptsData.discoveredConcepts.slice(0, 5).map((concept: any) => ({
              id: `concept-${concept.id}`,
              name: concept.title || concept.name || 'Untitled Concept',
              description: concept.description || 'No description available',
              type: 'concept' as const,
              image: concept.imageUrl || 'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=400&h=400&fit=crop',
              tags: concept.tags || concept.axes || [],
              resonance: concept.resonance || 0.5,
              contributors: Math.floor(Math.random() * 50) + 10, // TODO: Get real contributor count
              trending: (concept.resonance || 0) > 0.8
            }));
            items.push(...conceptItems);
          }
        }

        // Process users
        if (usersResponse.ok) {
          const usersData = await usersResponse.json();
          if (usersData.success && usersData.users) {
            const userItems = usersData.users.slice(0, 5).map((user: any) => ({
              id: `user-${user.id}`,
              name: user.displayName || user.name || 'Unknown User',
              description: user.bio || user.description || 'No description available',
              type: 'person' as const,
              image: user.avatar || 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=400&h=400&fit=crop',
              tags: user.interests || user.tags || [],
              resonance: user.resonance || 0.5,
              contributors: Math.floor(Math.random() * 100) + 20, // TODO: Get real contributor count
              trending: (user.resonance || 0) > 0.8
            }));
            items.push(...userItems);
          }
        }

        // No fallback - if no data, show error
        if (items.length === 0) {
          throw new Error('No swipe items available from backend APIs');
        }

        setItems(items);
        setCurrentIndex(0);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load swipe items');
      } finally {
        setLoading(false);
      }
    };

    fetchSwipeItems();
  }, [controls]);

  const currentItem = items[currentIndex];
  const hasMoreItems = currentIndex < items.length - 1;

  const handleSwipe = (direction: 'left' | 'right') => {
    if (!currentItem || readOnly || !userId) return;

    setSwipeDirection(direction);
    setShowUndo(true);

    // Simulate API call
    setTimeout(() => {
      if (direction === 'right') {
        // Handle positive interaction (like/attune)
        console.log('Liked:', currentItem.name);
      } else {
        // Handle negative interaction (pass)
        console.log('Passed:', currentItem.name);
      }

      // Move to next item
      if (hasMoreItems) {
        setCurrentIndex(prev => prev + 1);
        setSwipeDirection(null);
        setTimeout(() => setShowUndo(false), 300);
      } else {
        // No more items
        setSwipeDirection(null);
        setShowUndo(false);
      }
    }, 300);
  };

  const handleUndo = () => {
    if (!currentItem || readOnly || !userId) return;

    setSwipeDirection(null);
    setShowUndo(false);
    // In a real app, this would undo the previous action
  };

  const handleKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'ArrowLeft') {
      handleSwipe('left');
    } else if (e.key === 'ArrowRight') {
      handleSwipe('right');
    } else if (e.key === 'Escape' && showUndo) {
      handleUndo();
    }
  };

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [currentItem, showUndo]);

  if (loading) {
    return (
      <div className={`flex items-center justify-center py-12 ${className}`}>
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className}`}>
        <div className="text-red-800 font-medium">Error loading swipe items</div>
        <div className="text-red-600 text-sm mt-1">{error}</div>
      </div>
    );
  }

  if (!currentItem) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-6xl mb-4">🎉</div>
        <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2">
          All caught up!
        </h3>
        <p className="text-gray-600 dark:text-gray-300">
          You've seen all available items. Check back later for more discoveries.
        </p>
      </div>
    );
  }

  return (
    <div className={`relative ${className}`}>
      {/* Instructions */}
      <div className="text-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          Discovery Swipe
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Swipe right to connect, left to pass. Use arrow keys or buttons below.
        </p>

        {/* Keyboard shortcuts hint */}
        <div className="flex items-center justify-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
          <div className="flex items-center space-x-1">
            <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 rounded text-xs">←</kbd>
            <span>Pass</span>
          </div>
          <div className="flex items-center space-x-1">
            <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 rounded text-xs">→</kbd>
            <span>Like</span>
          </div>
          <div className="flex items-center space-x-1">
            <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 rounded text-xs">Esc</kbd>
            <span>Undo</span>
          </div>
        </div>
      </div>

      {/* Main swipe card */}
      <div className="relative max-w-md mx-auto">
        <div
          ref={cardRef}
          className={`relative bg-white dark:bg-gray-800 rounded-3xl shadow-2xl overflow-hidden transform transition-all duration-300 ${
            swipeDirection === 'left' ? '-rotate-12 -translate-x-32 opacity-0' :
            swipeDirection === 'right' ? 'rotate-12 translate-x-32 opacity-0' : ''
          }`}
          style={{
            transform: swipeDirection
              ? (swipeDirection === 'left' ? 'rotate(-12deg) translateX(-8rem)' : 'rotate(12deg) translateX(8rem)')
              : 'rotate(0deg) translateX(0)'
          }}
        >
          {/* Card Image */}
          {currentItem.image && (
            <div className="relative h-80 overflow-hidden">
              <img
                src={currentItem.image}
                alt={currentItem.name}
                className="w-full h-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />

              {/* Type badge */}
              <div className="absolute top-4 left-4">
                <span className={`px-3 py-1 rounded-full text-sm font-semibold ${
                  currentItem.type === 'concept'
                    ? 'bg-blue-500/80 text-white'
                    : 'bg-purple-500/80 text-white'
                }`}>
                  {currentItem.type === 'concept' ? '💡 Concept' : '👤 Person'}
                </span>
              </div>

              {/* Trending badge */}
              {currentItem.trending && (
                <div className="absolute top-4 right-4">
                  <span className="px-3 py-1 bg-orange-500/80 text-white rounded-full text-sm font-semibold">
                    🔥 Trending
                  </span>
                </div>
              )}
            </div>
          )}

          {/* Card Content */}
          <div className="p-6">
            <h3 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              {currentItem.name}
            </h3>

            <p className="text-gray-600 dark:text-gray-300 mb-4 leading-relaxed">
              {currentItem.description}
            </p>

            {/* Tags */}
            <div className="flex flex-wrap gap-2 mb-4">
              {currentItem.tags.map((tag) => (
                <span
                  key={tag}
                  className="px-3 py-1 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm rounded-full"
                >
                  {tag}
                </span>
              ))}
            </div>

            {/* Stats */}
            <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
              <div className="flex items-center space-x-4">
                <div className="flex items-center space-x-1">
                  <Users className="w-4 h-4" />
                  <span>{currentItem.contributors} contributors</span>
                </div>
              </div>
              <div className="flex items-center space-x-1">
                <Star className="w-4 h-4 text-yellow-500" />
                <span>{Math.round(currentItem.resonance * 100)}% resonance</span>
              </div>
            </div>
          </div>
        </div>

        {/* Swipe indicators */}
        <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
          {swipeDirection === 'left' && (
            <div className="bg-red-500 text-white px-4 py-2 rounded-full text-sm font-bold animate-bounce">
              PASS
            </div>
          )}
          {swipeDirection === 'right' && (
            <div className="bg-green-500 text-white px-4 py-2 rounded-full text-sm font-bold animate-bounce">
              LIKE
            </div>
          )}
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex items-center justify-center space-x-4 mt-8">
        <button
          onClick={() => handleSwipe('left')}
          disabled={!currentItem || readOnly || !userId}
          className="w-16 h-16 bg-red-500 hover:bg-red-600 disabled:bg-gray-300 text-white rounded-full flex items-center justify-center shadow-lg hover:shadow-xl transition-all duration-200 transform hover:scale-110 disabled:transform-none disabled:shadow-none"
          aria-label="Pass on this item"
        >
          <X className="w-6 h-6" />
        </button>

        {showUndo && (
          <button
            onClick={handleUndo}
            className="px-4 py-2 bg-gray-600 hover:bg-gray-700 text-white rounded-full text-sm font-medium transition-colors"
          >
            <RotateCcw className="w-4 h-4 inline mr-1" />
            Undo
          </button>
        )}

        <button
          onClick={() => handleSwipe('right')}
          disabled={!currentItem || readOnly || !userId}
          className="w-16 h-16 bg-green-500 hover:bg-green-600 disabled:bg-gray-300 text-white rounded-full flex items-center justify-center shadow-lg hover:shadow-xl transition-all duration-200 transform hover:scale-110 disabled:transform-none disabled:shadow-none"
          aria-label="Like this item"
        >
          <Heart className="w-6 h-6" />
        </button>
      </div>

      {/* Progress indicator */}
      <div className="flex justify-center mt-6">
        <div className="flex space-x-2">
          {items.slice(0, 5).map((_, index) => (
            <div
              key={index}
              className={`w-2 h-2 rounded-full transition-colors ${
                index === currentIndex ? 'bg-blue-500' :
                index < currentIndex ? 'bg-green-400' : 'bg-gray-300'
              }`}
            />
          ))}
        </div>
      </div>

      {/* Item counter */}
      <div className="text-center text-sm text-gray-500 dark:text-gray-400 mt-4">
        {currentIndex + 1} of {items.length}
      </div>

      {/* Read-only message */}
      {readOnly || !userId ? (
        <div className="text-center mt-6 p-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
          <Info className="w-6 h-6 text-amber-600 dark:text-amber-400 mx-auto mb-2" />
          <p className="text-amber-800 dark:text-amber-200 font-medium">
            Sign in to start swiping and connecting
          </p>
          <p className="text-amber-700 dark:text-amber-300 text-sm mt-1">
            Create an account to personalize your discovery experience
          </p>
        </div>
      ) : null}
    </div>
  );
}

export default SwipeLens;