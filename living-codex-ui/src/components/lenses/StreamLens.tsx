'use client';

import { useState, useEffect } from 'react';
import { ConceptStreamCard } from './ConceptStreamCard';
import { Card, CardContent } from '@/components/ui/Card';
import { PaginationControls } from '@/components/ui/PaginationControls';
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
  const [totalCount, setTotalCount] = useState<number>(0);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const pageSize = 12;
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
    const fetchData = async () => {
      setLoading(true);
      setError(null);

      try {
        // Combine concepts and users into a unified stream
        const concepts = conceptQuery.data?.concepts || conceptQuery.data?.discoveredConcepts || [];
        const conceptTotal = (conceptQuery.data?.totalCount ?? conceptQuery.data?.totalDiscovered) || concepts.length;
        const users = userQuery.data?.users || [];
        const usersTotal = userQuery.data?.totalCount || users.length;
        
        // Transform and merge data
        const conceptItems = concepts.map((concept: any) => ({
          ...concept,
          type: 'concept',
        }));

        const userItems = users.map((user: any) => ({
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
        setTotalCount(conceptTotal + usersTotal);
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

      {totalCount > pageSize && (
        <Card>
          <CardContent className="p-4">
            <PaginationControls
              currentPage={currentPage}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setCurrentPage}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
