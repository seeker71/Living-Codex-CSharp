'use client';

import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/Card';
import { buildApiUrl } from '@/lib/config';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';

interface GalleryItem {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  thumbnailUrl: string;
  author: {
    id: string;
    name: string;
    avatar: string;
  };
  createdAt: string;
  resonance: number;
  axes: string[];
  tags: string[];
  mediaType: 'image' | 'video' | 'audio';
  dimensions: { width: number; height: number };
  aiGenerated: boolean;
  prompt?: string;
  domain?: string;
  complexity?: number;
  energy?: number;
  imageLoading?: boolean;
  imageError?: string;
}

interface GalleryLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

export function GalleryLens({ controls = {}, userId, className = '' }: GalleryLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  const [items, setItems] = useState<GalleryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedItem, setSelectedItem] = useState<GalleryItem | null>(null);
  const [filterBy, setFilterBy] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('resonance');

  useEffect(() => {
    loadGalleryItems();
  }, [controls]);

  // Function to generate AI image for a concept
  const generateAIImage = async (concept: any): Promise<string> => {
    try {
      // Step 1: Create a concept for image generation
      const conceptResponse = await fetch(buildApiUrl('/image/concept/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: concept.name,
          description: concept.description,
          conceptType: 'visual',
          style: 'modern, clean, artistic, abstract',
          mood: 'inspiring, thoughtful, deep',
          colors: ['blue', 'purple', 'gradient', 'cosmic'],
          elements: concept.axes || [],
          metadata: {
            originalConceptId: concept.id,
            domain: concept.domain,
            complexity: concept.complexity
          }
        })
      });

      if (!conceptResponse.ok) {
        const errorData = await conceptResponse.json();
        throw new Error(`Concept creation failed: ${conceptResponse.status} ${errorData.error || conceptResponse.statusText}`);
      }

      const conceptData = await conceptResponse.json();
      const imageConceptId = conceptData.concept?.id;

      if (!imageConceptId) {
        throw new Error('No concept ID returned from creation service');
      }

      // Step 2: Generate images using the created concept
      const imageResponse = await fetch(buildApiUrl('/image/generate'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          conceptId: imageConceptId,
          imageConfigId: 'dalle-3', // Use DALL-E 3 config
          numberOfImages: 1,
          customPrompt: concept.prompt
        })
      });

      if (!imageResponse.ok) {
        const errorData = await imageResponse.json();
        throw new Error(`Image generation failed: ${imageResponse.status} ${errorData.error || imageResponse.statusText}`);
      }

      const imageData = await imageResponse.json();
      const imageUrl = imageData.images?.[0];
      
      if (!imageUrl) {
        throw new Error('No image URL returned from generation service');
      }
      
      return imageUrl;
    } catch (error) {
      console.error('AI image generation error:', error);
      throw error;
    }
  };

  const loadGalleryItems = async () => {
    setLoading(true);
    try {
      console.log('Gallery: Starting to load concepts...');
      
      // Fetch concepts from the backend
      const conceptsResponse = await fetch(buildApiUrl('/concepts/browse'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          axes: controls.axes || ['resonance'],
          joy: controls.joy || 0.7,
          serendipity: controls.serendipity || 0.5,
          limit: 50
        })
      });
      
      console.log('Gallery: Concepts response status:', conceptsResponse.status);

      if (conceptsResponse.ok) {
        const conceptsData = await conceptsResponse.json();
        const concepts = conceptsData.discoveredConcepts || conceptsData.concepts || conceptsData.data || [];
        
        console.log('Gallery: Found concepts:', concepts.length);

        // For each concept, create a gallery item with AI-generated image
        // Process concepts in batches to avoid overwhelming the API
        const batchSize = 3;
        const galleryItems = [];
        
        for (let i = 0; i < concepts.length; i += batchSize) {
          const batch = concepts.slice(i, i + batchSize);
          const batchResults = await Promise.all(
            batch.map(async (concept: any) => {
            try {
              // Create a rich description using concept metadata
              const axes = concept.tags || [];
              const domain = concept.domain || 'General';
              const complexity = concept.complexity || 1;
              
              // Build contextual prompt for AI image generation
              let contextualPrompt = `A visual representation of "${concept.name}": ${concept.description}`;
              contextualPrompt += ` This concept is in the ${domain} domain with complexity level ${complexity}.`;
              
              if (axes.length > 0) {
                contextualPrompt += ` This concept relates to: ${axes.join(', ')}.`;
              }
              
              contextualPrompt += ` Create a modern, artistic, and inspiring visual that captures the essence and depth of this concept.`;

              // Generate AI image with fallback to placeholder
              let imageUrl;
              try {
                imageUrl = await generateAIImage({ ...concept, prompt: contextualPrompt });
              } catch (aiError) {
                console.warn(`AI generation failed for ${concept.id}, using placeholder:`, aiError);
                // Create sophisticated placeholder with concept-specific styling
                const colors = ['6366f1', '8b5cf6', 'ec4899', 'f59e0b', '10b981', 'ef4444', 'f97316', '84cc16'];
                const colorIndex = concept.id.length % colors.length;
                const bgColor = colors[colorIndex];
                const conceptInitials = concept.name.split(' ').map((word: string) => word[0]).join('').toUpperCase().slice(0, 2);
                imageUrl = `https://via.placeholder.com/400x400/${bgColor}/ffffff?text=${encodeURIComponent(conceptInitials)}`;
              }

              return {
                id: concept.id,
                title: concept.name || 'Untitled Concept',
                description: concept.description || '',
                imageUrl: imageUrl,
                thumbnailUrl: imageUrl,
                author: {
                  id: 'system',
                  name: 'Living Codex',
                  avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC'
                },
                createdAt: concept.createdAt || new Date().toISOString(),
                resonance: concept.resonance || 0.5,
                axes: axes.length > 0 ? axes : ['concept'],
                tags: [...axes, domain].filter(Boolean),
                mediaType: 'image' as const,
                dimensions: { width: 400, height: 400 },
                aiGenerated: true,
                prompt: contextualPrompt,
                domain: domain,
                complexity: complexity,
                energy: concept.energy || 0,
                imageLoading: false,
                imageError: undefined
              };
            } catch (error) {
              console.error(`Error processing concept ${concept.id}:`, error);
              // Return item with placeholder image instead of failing completely
              const colors = ['6366f1', '8b5cf6', 'ec4899', 'f59e0b', '10b981', 'ef4444', 'f97316', '84cc16'];
              const colorIndex = concept.id.length % colors.length;
              const bgColor = colors[colorIndex];
              const conceptInitials = concept.name.split(' ').map((word: string) => word[0]).join('').toUpperCase().slice(0, 2);
              const placeholderUrl = `https://via.placeholder.com/400x400/${bgColor}/ffffff?text=${encodeURIComponent(conceptInitials)}`;
              
              return {
                id: concept.id,
                title: concept.name || 'Untitled Concept',
                description: concept.description || '',
                imageUrl: placeholderUrl,
                thumbnailUrl: placeholderUrl,
                author: {
                  id: 'system',
                  name: 'Living Codex',
                  avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC'
                },
                createdAt: concept.createdAt || new Date().toISOString(),
                resonance: concept.resonance || 0.5,
                axes: concept.tags || ['concept'],
                tags: [...(concept.tags || []), concept.domain || 'General'].filter(Boolean),
                mediaType: 'image' as const,
                dimensions: { width: 400, height: 400 },
                aiGenerated: false,
                prompt: `A visual representation of "${concept.name}": ${concept.description}`,
                domain: concept.domain || 'General',
                complexity: concept.complexity || 1,
                energy: concept.energy || 0,
                imageLoading: false,
                imageError: undefined
              };
            }
            })
          );
          
          galleryItems.push(...batchResults);
          
          // Add a small delay between batches to avoid overwhelming the API
          if (i + batchSize < concepts.length) {
            await new Promise(resolve => setTimeout(resolve, 1000));
          }
        }

        console.log(`Gallery: Loaded ${galleryItems.length} concepts from ${concepts.length} total`);
        setItems(galleryItems);
      } else {
        const errorMessage = `Failed to fetch concepts: ${conceptsResponse.status} ${conceptsResponse.statusText}`;
        console.error(errorMessage);
        setError(errorMessage);
        setItems([]);
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred while loading gallery items';
      console.error('Error loading gallery items:', error);
      setError(errorMessage);
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  const formatTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
  };

  // Filter and sort items
  const filteredItems = items
    .filter(item => {
      if (filterBy === 'all') return true;
      return item.axes.includes(filterBy);
    })
    .sort((a, b) => {
      switch (sortBy) {
        case 'resonance':
          return b.resonance - a.resonance;
        case 'energy':
          return (b.energy || 0) - (a.energy || 0);
        case 'complexity':
          return (b.complexity || 0) - (a.complexity || 0);
        case 'recent':
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        default:
          return 0;
      }
    });

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        <span className="ml-3 text-gray-600 dark:text-gray-400">Loading concepts...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="text-red-500 text-6xl mb-4">⚠️</div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">Error Loading Gallery</h3>
          <p className="text-gray-600 dark:text-gray-400 mb-4">{error}</p>
          <button 
            onClick={() => {
              setError(null);
              loadGalleryItems();
            }}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Instagram-style Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 bg-gradient-to-r from-purple-500 to-blue-600 rounded-full flex items-center justify-center">
            <span className="text-white text-xl">🖼️</span>
          </div>
          <div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Concept Gallery</h2>
            <p className="text-gray-600 dark:text-gray-400">
              Visual expressions of concepts and ideas • {filteredItems.length} concepts
            </p>
          </div>
        </div>
        
        {/* Controls */}
        <div className="flex items-center space-x-4">
          <select
            value={filterBy}
            onChange={(e) => setFilterBy(e.target.value)}
            className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            <option value="all">All Concepts</option>
            <option value="abundance">Abundance</option>
            <option value="unity">Unity</option>
            <option value="resonance">Resonance</option>
            <option value="innovation">Innovation</option>
            <option value="consciousness">Consciousness</option>
          </select>
          
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          >
            <option value="resonance">By Resonance</option>
            <option value="energy">By Energy</option>
            <option value="complexity">By Complexity</option>
            <option value="recent">Most Recent</option>
          </select>
        </div>
      </div>

      {/* Instagram-style Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1">
        {filteredItems.length === 0 ? (
          <div className="col-span-full text-center py-12 text-gray-500">
            <div className="text-6xl mb-4">🖼️</div>
            <p>No concepts available yet.</p>
            <p className="text-sm mt-2">Concepts will appear here as they are discovered!</p>
            <div className="mt-4 text-xs text-gray-400">
              Debug: Total items: {items.length}, Filtered: {filteredItems.length}
            </div>
          </div>
        ) : (
          filteredItems.map((item) => (
            <div
              key={item.id}
              className="relative group cursor-pointer bg-white dark:bg-gray-800 rounded-lg overflow-hidden shadow-sm hover:shadow-lg transition-all duration-300"
              onClick={() => setSelectedItem(item)}
            >
              {/* Instagram-style square image */}
              <div className="aspect-square relative">
                {item.imageError ? (
                  <div className="w-full h-full bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center p-4">
                    <div className="text-red-500 text-2xl mb-2">⚠️</div>
                    <div className="text-red-700 dark:text-red-300 text-xs text-center">
                      <p className="font-semibold">Image Error</p>
                      <p className="mt-1">{item.imageError}</p>
                    </div>
                  </div>
                ) : item.imageUrl ? (
                  <img
                    src={item.imageUrl}
                    alt={item.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                ) : (
                  <div className="w-full h-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center">
                    <div className="text-gray-500 text-2xl">📷</div>
                  </div>
                )}
                
                {/* Status indicators */}
                <div className="absolute top-2 right-2 flex space-x-1">
                  {item.aiGenerated && (
                    <div className="bg-green-600 bg-opacity-80 text-white text-xs px-2 py-1 rounded">
                      AI
                    </div>
                  )}
                </div>
              </div>
              
              {/* Overlay with concept info */}
              <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-30 transition-all duration-300 flex items-end">
                <div className="p-3 text-white opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                  <h3 className="font-semibold text-sm truncate">{item.title}</h3>
                  <p className="text-xs text-gray-200 truncate">{item.domain}</p>
                  <div className="flex items-center mt-1 space-x-2">
                    <span className="text-xs bg-blue-500 px-2 py-1 rounded">Resonance: {item.resonance.toFixed(2)}</span>
                  </div>
                </div>
              </div>

              {/* Always-visible caption */}
              <div className="p-3 border-t border-gray-100 dark:border-gray-700">
                <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate">{item.title}</h3>
                <div className="flex items-center justify-between mt-1">
                  <span className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.domain}</span>
                  {item.author?.name && (
                    <span className="text-xs text-gray-500 dark:text-gray-400 truncate">by {item.author.name}</span>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Concept Detail Modal */}
      {selectedItem && (
        <div className="fixed inset-0 bg-black bg-opacity-90 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="flex flex-col lg:flex-row">
              {/* Image Section */}
              <div className="flex-1 p-6">
                {selectedItem.imageError ? (
                  <div className="w-full h-96 bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center rounded-lg">
                    <div className="text-red-500 text-4xl mb-4">⚠️</div>
                    <h3 className="text-lg font-semibold text-red-700 dark:text-red-300 mb-2">Image Generation Failed</h3>
                    <p className="text-red-600 dark:text-red-400 text-sm text-center max-w-md">{selectedItem.imageError}</p>
                  </div>
                ) : selectedItem.imageUrl ? (
                  <img
                    src={selectedItem.imageUrl}
                    alt={selectedItem.title}
                    className="w-full h-auto rounded-lg"
                  />
                ) : (
                  <div className="w-full h-96 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                    <div className="text-gray-500 text-4xl">📷</div>
                  </div>
                )}
              </div>

              {/* Details Section */}
              <div className="w-full lg:w-96 p-6 border-t lg:border-t-0 lg:border-l border-gray-200 dark:border-gray-700">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                    {selectedItem.title}
                  </h3>
                  <button
                    onClick={() => setSelectedItem(null)}
                    className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                  >
                    ✕
                  </button>
                </div>

                <div className="space-y-4">
                  <div>
                    <p className="text-gray-600 dark:text-gray-400 text-sm mb-2">
                      {selectedItem.description}
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Domain:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.domain}</p>
                    </div>
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Complexity:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.complexity}</p>
                    </div>
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Resonance:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.resonance.toFixed(3)}</p>
                    </div>
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Energy:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.energy || 0}</p>
                    </div>
                  </div>

                  {selectedItem.axes.length > 0 && (
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100 text-sm">Axes:</span>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {selectedItem.axes.map((axis) => (
                          <span
                            key={axis}
                            className="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs px-2 py-1 rounded"
                          >
                            {axis}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  {selectedItem.tags.length > 0 && (
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100 text-sm">Tags:</span>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {selectedItem.tags.slice(0, 8).map((tag) => (
                          <span
                            key={tag}
                            className="bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-xs px-2 py-1 rounded"
                          >
                            {tag}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      Created {formatTimeAgo(selectedItem.createdAt)} • 
                      {selectedItem.aiGenerated ? ' AI Generated' : ' Placeholder'}
                    </p>
                    {selectedItem.prompt && (
                      <details className="mt-2">
                        <summary className="text-xs text-gray-500 dark:text-gray-400 cursor-pointer hover:text-gray-700 dark:hover:text-gray-300">
                          View AI Prompt
                        </summary>
                        <p className="text-xs text-gray-600 dark:text-gray-400 mt-1 p-2 bg-gray-100 dark:bg-gray-700 rounded">
                          {selectedItem.prompt}
                        </p>
                      </details>
                    )}
                    {selectedItem.imageError && (
                      <div className="mt-2 p-2 bg-red-100 dark:bg-red-900 rounded text-red-700 dark:text-red-300 text-xs">
                        <strong>Error:</strong> {selectedItem.imageError}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}