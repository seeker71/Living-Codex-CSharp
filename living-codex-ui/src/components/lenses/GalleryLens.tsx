'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent } from '@/components/ui/Card';
import { PaginationControls } from '@/components/ui/PaginationControls';
import { buildApiUrl } from '@/lib/config';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';

interface GalleryItem {
  id: string;
  title: string;
  description: string;
  summary?: string;
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
  summaryLoading?: boolean;
  summaryError?: string;
}

interface GalleryLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

export function GalleryLens({ controls = {}, userId, className = '', readOnly = false }: GalleryLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  const [items, setItems] = useState<GalleryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedItem, setSelectedItem] = useState<GalleryItem | null>(null);
  const [filterBy, setFilterBy] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('resonance');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(12);

  useEffect(() => {
    console.log('Gallery: useEffect triggered, calling loadGalleryItems...');
    loadGalleryItems();
  }, []); // Run on component mount

  // Function to generate AI summary for a concept
  const generateAISummary = async (concept: any): Promise<string> => {
    try {
      const summaryResponse = await fetch(buildApiUrl('/ai/summarize'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          concept: {
            id: concept.id,
            name: concept.name,
            description: concept.description,
            domain: concept.domain,
            complexity: concept.complexity,
            tags: concept.tags || []
          },
          context: 'concept-gallery',
          style: 'engaging, insightful, concise',
          length: 'medium'
        })
      });

      if (!summaryResponse.ok) {
        throw new Error(`Summary generation failed: ${summaryResponse.status}`);
      }

      const summaryData = await summaryResponse.json();
      return summaryData.summary || summaryData.content || concept.description;
    } catch (error) {
      console.warn(`AI summary generation failed for ${concept.id}, using description:`, error);
      return concept.description;
    }
  };

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
    console.log('Gallery: loadGalleryItems called');
    setLoading(true);
    try {
      console.log('Gallery: Starting to load concepts...');
      
      // Fetch concepts from the backend
      const conceptsResponse = await fetch(buildApiUrl('/concepts'), {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' }
      });
      
      console.log('Gallery: Concepts response status:', conceptsResponse.status);

      if (conceptsResponse.ok) {
        const conceptsData = await conceptsResponse.json();
        const concepts = conceptsData.concepts || conceptsData.data || [];
        
        console.log('Gallery: Found concepts:', concepts.length);

        // Create gallery items with real AI-generated images
        const galleryItems = await Promise.allSettled(
          concepts.map(async (concept: any) => {
            const axes = concept.tags || [];
            const domain = concept.domain || 'General';
            const complexity = concept.complexity || 1;
            
            let imageUrl = '';
            let imageError = '';
            let aiGenerated = false;
            let prompt = '';
            
            console.log(`Gallery: Generating AI image for concept "${concept.name}"...`);
            
            // Generate AI image using the concept image pipeline - NO FALLBACKS
            imageUrl = await generateAIImage(concept);
            aiGenerated = true;
            prompt = `AI-generated image for "${concept.name}": ${concept.description}`;
            
            console.log(`Gallery: Successfully generated AI image for "${concept.name}"`);
            
            return {
              id: concept.id,
              title: concept.name || 'Untitled Concept',
              description: concept.description || '',
              summary: concept.description || '',
              imageUrl: imageUrl,
              thumbnailUrl: imageUrl,
              author: {
                id: 'system',
                name: 'Living Codex',
                avatar: `data:image/svg+xml;base64,${btoa(`
                  <svg width="40" height="40" xmlns="http://www.w3.org/2000/svg">
                    <defs>
                      <linearGradient id="avatar-grad" x1="0%" y1="0%" x2="100%" y2="100%">
                        <stop offset="0%" style="stop-color:#6366f1;stop-opacity:1" />
                        <stop offset="100%" style="stop-color:#8b5cf6;stop-opacity:1" />
                      </linearGradient>
                    </defs>
                    <rect width="40" height="40" rx="20" fill="url(#avatar-grad)"/>
                    <text x="20" y="25" text-anchor="middle" dy=".3em" 
                          font-family="system-ui, sans-serif" font-size="16" font-weight="bold" 
                          fill="white">LC</text>
                  </svg>
                `)}`
              },
              createdAt: concept.createdAt || new Date().toISOString(),
              resonance: concept.resonance || 0.5,
              axes: axes.length > 0 ? axes : ['concept'],
              tags: [...axes, domain].filter(Boolean),
              mediaType: 'image' as const,
              dimensions: { width: 400, height: 400 },
              aiGenerated: aiGenerated,
              prompt: prompt,
              domain: domain,
              complexity: complexity,
              energy: concept.energy || 0,
              imageLoading: false,
              imageError: imageError || undefined,
              summaryLoading: false,
              summaryError: undefined
            };
          })
        );
        
        // Extract successful results and handle failures
        const successfulItems = galleryItems
          .filter((result): result is PromiseFulfilledResult<GalleryItem> => result.status === 'fulfilled')
          .map(result => result.value);
        
        const failedItems = galleryItems
          .filter((result): result is PromiseRejectedResult => result.status === 'rejected')
          .map(result => result.reason);
        
        if (failedItems.length > 0) {
          console.warn(`Gallery: ${failedItems.length} items failed to load:`, failedItems);
        }

        console.log(`Gallery: Loaded ${successfulItems.length} concepts from ${concepts.length} total`);
        setItems(successfulItems);
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

  const handleContribute = async (concept: GalleryItem) => {
    try {
      // Track the interaction
      trackInteraction(concept.id, 'concept_contribute', { conceptName: concept.title });
      
      // Show contribution modal or redirect to contribution page
      alert(`Contribute to "${concept.title}" - This would open a contribution interface where you can add your insights, resources, or improvements to this concept.`);
    } catch (error) {
      console.error('Error handling contribution:', error);
    }
  };

  const handleInvest = async (concept: GalleryItem) => {
    try {
      // Track the interaction
      trackInteraction(concept.id, 'concept_invest', { conceptName: concept.title });
      
      // Show investment modal or redirect to investment page
      alert(`Invest in "${concept.title}" - This would open an investment interface where you can allocate resources, time, or energy to support this concept's development.`);
    } catch (error) {
      console.error('Error handling investment:', error);
    }
  };

  const handleStartThread = async (concept: GalleryItem) => {
    try {
      // Track the interaction
      trackInteraction(concept.id, 'concept_thread_start', { conceptName: concept.title });
      
      // Show thread modal or redirect to thread page
      alert(`Start a discussion about "${concept.title}" - This would open a conversation interface with an AI bot that has this concept in mind, allowing you to explore ideas, ask questions, and connect with others who resonate with this concept.`);
    } catch (error) {
      console.error('Error starting thread:', error);
    }
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

  // Paginate items
  const totalCount = filteredItems.length;
  const startIndex = (currentPage - 1) * pageSize;
  const endIndex = startIndex + pageSize;
  const paginatedItems = filteredItems.slice(startIndex, endIndex);

  // Reset to first page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [filterBy, sortBy]);

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
              Visual expressions of concepts and ideas • {totalCount} concepts
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

      {/* Pagination at top */}
      {totalCount > pageSize && (
        <Card>
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

      {/* Instagram-style Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1">
            {paginatedItems.length === 0 ? (
              <div className="col-span-full text-center py-12 text-red-500">
                <div className="text-6xl mb-4">⚠️</div>
                <p>No concepts available.</p>
                <p className="text-sm mt-2">AI image generation is required for all concepts.</p>
                <div className="mt-4 text-xs text-gray-400">
                  Debug: Total items: {items.length}, Filtered: {totalCount}, Page: {currentPage}
                </div>
              </div>
            ) : (
          paginatedItems.map((item) => (
            <div
              key={item.id}
              className="relative group cursor-pointer bg-white dark:bg-gray-800 rounded-lg overflow-hidden shadow-sm hover:shadow-lg transition-all duration-300"
              onClick={() => setSelectedItem(item)}
            >
              {/* Instagram-style square image */}
              <div className="aspect-square relative">
                {item.imageUrl ? (
                  <img
                    src={item.imageUrl}
                    alt={item.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                ) : (
                  <div className="w-full h-full bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center p-4">
                    <div className="text-red-500 text-2xl mb-2">⚠️</div>
                    <div className="text-red-700 dark:text-red-300 text-xs text-center">
                      <p className="font-semibold">No Image Available</p>
                      <p className="mt-1">AI image generation required</p>
                    </div>
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

              {/* Enhanced caption with more node information */}
              <div className="p-3 border-t border-gray-100 dark:border-gray-700">
                <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate mb-1">{item.title}</h3>
                <p className="text-xs text-gray-600 dark:text-gray-300 line-clamp-2 mb-2">{item.description}</p>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.domain}</span>
                  <span className="text-xs text-purple-600 dark:text-purple-400 font-medium">
                    {Math.round(item.resonance * 100)}% resonance
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex flex-wrap gap-1">
                    {item.axes.slice(0, 2).map((axis) => (
                      <span
                        key={axis}
                        className="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs px-2 py-0.5 rounded-full"
                      >
                        {axis}
                      </span>
                    ))}
                    {item.axes.length > 2 && (
                      <span className="text-xs text-gray-500 dark:text-gray-400">+{item.axes.length - 2}</span>
                    )}
                  </div>
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
                    {selectedItem.imageUrl ? (
                      <img
                        src={selectedItem.imageUrl}
                        alt={selectedItem.title}
                        className="w-full h-auto rounded-lg"
                      />
                    ) : (
                      <div className="w-full h-96 bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center rounded-lg">
                        <div className="text-red-500 text-4xl mb-4">⚠️</div>
                        <h3 className="text-lg font-semibold text-red-700 dark:text-red-300 mb-2">No Image Available</h3>
                        <p className="text-red-600 dark:text-red-400 text-sm text-center max-w-md">AI image generation required</p>
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
                    <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">Description</h4>
                    <p className="text-gray-600 dark:text-gray-400 text-sm mb-2 leading-relaxed">
                      {selectedItem.description}
                    </p>
                  </div>

                  {selectedItem.aiGenerated && (
                    <div className="bg-gradient-to-r from-purple-50 to-blue-50 dark:from-purple-900/20 dark:to-blue-900/20 p-4 rounded-lg border border-purple-200 dark:border-purple-800">
                      <div className="flex items-center space-x-2 mb-2">
                        <span className="text-purple-600 dark:text-purple-400">✨</span>
                        <h4 className="text-sm font-medium text-purple-900 dark:text-purple-100">AI-Generated Image</h4>
                      </div>
                      <p className="text-purple-700 dark:text-purple-300 text-xs">
                        This image was created using our enhanced AI prompt system with inspiring, creative, and joyful visualizations that capture the essence of this concept.
                      </p>
                    </div>
                  )}

                  {selectedItem.summary && selectedItem.summary !== selectedItem.description && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">AI Summary</h4>
                      <p className="text-gray-600 dark:text-gray-400 text-sm mb-2 bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg border-l-4 border-blue-500">
                        {selectedItem.summary}
                      </p>
                    </div>
                  )}

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
                    {readOnly ? (
                      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3 mb-4">
                        <div className="text-center text-amber-700 dark:text-amber-300 text-sm">
                          <span className="inline-flex items-center space-x-2">
                            <span>👁️</span>
                            <span>Sign in to contribute, invest, or discuss this concept</span>
                          </span>
                        </div>
                      </div>
                    ) : (
                      <div className="flex space-x-2 mb-4">
                        <button
                          onClick={() => handleContribute(selectedItem)}
                          className="flex-1 bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors text-sm font-medium"
                        >
                          💡 Contribute
                        </button>
                        <button
                          onClick={() => handleInvest(selectedItem)}
                          className="flex-1 bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700 transition-colors text-sm font-medium"
                        >
                          💰 Invest
                        </button>
                        <button
                          onClick={() => handleStartThread(selectedItem)}
                          className="flex-1 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium"
                        >
                          💬 Discuss
                        </button>
                      </div>
                    )}
                    
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