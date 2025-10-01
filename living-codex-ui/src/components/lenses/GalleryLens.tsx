'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Image as ImageIcon, Heart, Share2, MessageCircle, ExternalLink, AlertCircle, RefreshCw } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { ApiErrorHandler } from '@/lib/api';

interface GalleryLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

export function GalleryLens({ controls = {}, userId, className = '', readOnly = false }: GalleryLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [images, setImages] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  const fetchImages = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      // Use the improved API layer with structured error handling
      const response = await fetch('http://localhost:5002/gallery/list', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        let errorText = '';
        let errorData;
        try {
          // Try to get text if the method exists (for mocked responses)
          if (typeof response.text === 'function') {
            errorText = await response.text();
          } else {
            // For real API responses, try to get JSON
            errorData = await response.json();
            errorText = errorData.message || `HTTP ${response.status}`;
          }
        } catch {
          errorText = `HTTP ${response.status}`;
        }
        
        if (!errorData) {
          try {
            errorData = JSON.parse(errorText);
          } catch {
            errorData = { error: errorText };
          }
        }

        const errorMessage = errorData?.error || `HTTP ${response.status}: ${response.statusText}`;
        const errorCode = errorData?.code || `HTTP_${response.status}`;

        throw new Error(`${errorMessage} (Code: ${errorCode})`);
      }

      const data = await response.json();

      if (!data.success) {
        const errorMessage = data.error || 'Unknown API error';
        const errorCode = data.code || 'API_ERROR';
        throw new Error(`${errorMessage} (Code: ${errorCode})`);
      }

      if (!data.items || !Array.isArray(data.items)) {
        throw new Error('Invalid response format - missing items array');
      }

      // Transform the API response to match the expected format
      const transformedImages = data.items.map((item: any) => ({
        id: item.id,
        title: item.title || 'Untitled',
        description: item.description || 'No description available',
        url: item.imageUrl || item.thumbnailUrl || '',
        author: item.author?.name || 'Unknown',
        likes: item.likes || 0,
        comments: item.comments || 0,
        tags: item.tags || [],
        resonance: item.resonance || 0,
        axes: item.axes || [],
        mediaType: item.mediaType || 'image',
        aiGenerated: item.aiGenerated || false,
        createdAt: item.createdAt,
        // Add structured error handling metadata
        apiResponse: {
          success: true,
          timestamp: new Date().toISOString(),
        }
      }));

      setImages(transformedImages);
      setRetryCount(0); // Reset retry count on success

      // Track successful gallery load
      trackInteraction('gallery-load', 'gallery-view', {
        itemCount: transformedImages.length,
        timestamp: new Date().toISOString(),
      });

    } catch (err) {
      console.error('Gallery API Error:', err);
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';

      // Use the improved error handler
      const userMessage = ApiErrorHandler.getUserMessage({
        success: false,
        error: errorMessage,
        errorCode: 'GALLERY_FETCH_ERROR',
        timestamp: new Date().toISOString(),
      });

      setError(userMessage);
      setImages([]);
    } finally {
      setLoading(false);
    }
  }, [user?.id, trackInteraction]);

  useEffect(() => {
    fetchImages();
  }, [user?.id, retryCount]);

  const handleRetry = () => {
    setRetryCount(prev => prev + 1);
  };

  const handleLike = async (imageId: string) => {
    if (readOnly || !userId || !user?.id) return;

    try {
      // Optimistically update UI
      setImages(prev => prev.map(img =>
        img.id === imageId ? { ...img, likes: img.likes + 1 } : img
      ));

      // Track the interaction
      trackInteraction(imageId, 'gallery-like', {
        userId: user.id,
        timestamp: new Date().toISOString(),
      });

    } catch (error) {
      // Revert optimistic update on error
      setImages(prev => prev.map(img =>
        img.id === imageId ? { ...img, likes: img.likes - 1 } : img
      ));
      console.error('Failed to like image:', error);
    }
  };

  const handleShare = async (image: any) => {
    if (!user?.id) return;

    try {
      if (typeof navigator !== 'undefined' && 'share' in navigator) {
        await navigator.share({
          title: image.title,
          text: image.description,
          url: window.location.href,
        });
      } else {
        // Fallback: basic sharing functionality
        console.log('Share functionality available but not implemented in this demo');
      }

      trackInteraction(image.id, 'gallery-share', {
        userId: user.id,
        method: typeof navigator !== 'undefined' && 'share' in navigator ? 'native' : 'clipboard',
        timestamp: new Date().toISOString(),
      });
    } catch (error) {
      console.error('Failed to share image:', error);
    }
  };

  if (loading) {
    return (
      <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 ${className}`}>
        {[...Array(6)].map((_, i) => (
          <div key={i} className="animate-pulse">
            <div className="bg-gray-200 rounded-lg h-64" />
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-red-600 dark:text-red-400 mb-4">
          <AlertCircle className="w-16 h-16 mx-auto mb-4 opacity-50" />
          <h3 className="text-lg font-semibold mb-2">Gallery Unavailable</h3>
          <p className="text-sm mb-4">{error}</p>
          <div className="flex items-center justify-center space-x-4">
            <button
              onClick={handleRetry}
              disabled={loading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors flex items-center space-x-2 disabled:opacity-50"
            >
              <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
              <span>Try Again</span>
            </button>
            <button
              onClick={() => window.location.reload()}
              className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors"
            >
              Reload Page
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          Visual Discovery Gallery
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Explore concepts through visual representations and artistic interpretations
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {images.map((image) => (
          <Card key={image.id} role="article" className="group overflow-hidden hover:shadow-xl transition-all duration-300 hover:-translate-y-1">
            <div className="relative">
              <img
                src={image.url}
                alt={image.title}
                className="w-full h-64 object-cover group-hover:scale-105 transition-transform duration-300"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/50 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

              {/* Overlay actions */}
              <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                <button className="p-2 bg-white/80 hover:bg-white rounded-full text-gray-700 hover:text-gray-900 transition-colors">
                  <ExternalLink className="w-4 h-4" />
                </button>
              </div>
            </div>

            <CardContent className="p-4">
              <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-2 line-clamp-2">
                {image.title}
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-300 mb-3 line-clamp-2">
                {image.description}
              </p>

              <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 mb-3">
                <span>by {image.author}</span>
                <div className="flex items-center space-x-3">
                  <div className="flex items-center space-x-1">
                    <Heart className="w-3 h-3" />
                    <span>{image.likes}</span>
                  </div>
                  <div className="flex items-center space-x-1">
                    <MessageCircle className="w-3 h-3" />
                    <span>{image.comments}</span>
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap gap-1 mb-3">
                {image.tags.map((tag: string) => (
                  <span
                    key={tag}
                    className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-xs rounded-full"
                  >
                    {tag}
                  </span>
                ))}
              </div>

              {!readOnly && userId && (
                <div className="flex items-center space-x-2">
                  <button
                    onClick={() => handleLike(image.id)}
                    className="flex items-center space-x-1 px-3 py-1.5 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                  >
                    <Heart className="w-4 h-4" />
                    <span className="text-xs">Like</span>
                  </button>
                  <button
                    onClick={() => handleShare(image)}
                    className="flex items-center space-x-1 px-3 py-1.5 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
                  >
                    <Share2 className="w-4 h-4" />
                    <span className="text-xs">Share</span>
                  </button>
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {images.length === 0 && (
        <div className="text-center py-12">
          <ImageIcon className="w-16 h-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
            No images available
          </h3>
          <p className="text-gray-600 dark:text-gray-300">
            The visual gallery is currently empty. Check back later for new visual discoveries.
          </p>
        </div>
      )}
    </div>
  );
}

export default GalleryLens;