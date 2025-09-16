'use client';

import { useState, useEffect } from 'react';
import { ConceptStreamCard } from './ConceptStreamCard';
import { useConceptDiscovery, useUserDiscovery } from '@/lib/hooks';
import { UILens } from '@/lib/atoms';

interface StreamLensProps {
  lens: UILens;
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

export function StreamLens({ lens, controls = {}, userId, className = '' }: StreamLensProps) {
  const [items, setItems] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Use the lens adapters to fetch data
  const conceptQuery = useConceptDiscovery({
    axes: controls.axes || ['resonance'],
    joy: controls.joy || 0.7,
    serendipity: controls.serendipity || 0.5,
  });

  const userQuery = useUserDiscovery({
    interests: controls.axes || ['resonance'],
  });

  // Fallback test data when API returns empty
  const testConcepts = [
    {
      id: 'concept-quantum-resonance',
      name: 'Quantum Resonance',
      description: 'The fundamental principle that all matter vibrates at specific frequencies, creating resonance fields that can be amplified and harmonized.',
      axes: ['resonance', 'science', 'consciousness'],
      resonance: 0.95,
      type: 'concept',
    },
    {
      id: 'concept-fractal-consciousness',
      name: 'Fractal Consciousness',
      description: 'The idea that consciousness exhibits fractal patterns at every scale, from individual thoughts to collective awareness.',
      axes: ['consciousness', 'unity', 'innovation'],
      resonance: 0.88,
      type: 'concept',
    },
    {
      id: 'concept-abundance-mindset',
      name: 'Abundance Mindset',
      description: 'A way of thinking that focuses on limitless possibilities and collaborative growth rather than scarcity and competition.',
      axes: ['abundance', 'unity', 'impact'],
      resonance: 0.92,
      type: 'concept',
    }
  ];

  const testUsers = [
    {
      id: 'user-alex-resonance',
      name: 'Alex Resonance',
      description: 'Exploring the intersection of quantum physics and consciousness',
      axes: ['resonance', 'science', 'consciousness'],
      type: 'user',
    },
    {
      id: 'user-maya-fractal',
      name: 'Maya Fractal',
      description: 'Artist and researcher studying fractal patterns in nature and mind',
      axes: ['consciousness', 'unity', 'innovation'],
      type: 'user',
    }
  ];

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);

      try {
        // Combine concepts and users into a unified stream
        const concepts = conceptQuery.data?.concepts || conceptQuery.data?.discoveredConcepts || [];
        const users = userQuery.data?.users || [];
        
        // Use test data if API returns empty
        const finalConcepts = concepts.length > 0 ? concepts : testConcepts;
        const finalUsers = users.length > 0 ? users : testUsers;
        
        // Transform and merge data
        const conceptItems = finalConcepts.map((concept: any) => ({
          ...concept,
          type: 'concept',
        }));

        const userItems = finalUsers.map((user: any) => ({
          id: user.id,
          name: user.name || user.username,
          description: user.bio || user.description || `User interested in ${user.interests?.join(', ')}`,
          type: 'user',
          axes: user.interests || user.axes || [],
        }));

        // Apply ranking if specified
        let combinedItems = [...conceptItems, ...userItems];
        
        if (lens.ranking === 'resonance*joy*recency') {
          combinedItems = combinedItems.sort((a, b) => {
            const aScore = (a.resonance || 0.5) * (controls.joy || 0.7) * (a.timestamp || 1);
            const bScore = (b.resonance || 0.5) * (controls.joy || 0.7) * (b.timestamp || 1);
            return bScore - aScore;
          });
        }

        setItems(combinedItems);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load stream');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [conceptQuery.data, userQuery.data, controls, lens.ranking]);

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
    <div className={`space-y-4 ${className}`}>
      {items.map((item) => (
        <ConceptStreamCard
          key={item.id}
          concept={item}
          userId={userId}
          onAction={handleAction}
        />
      ))}
    </div>
  );
}
