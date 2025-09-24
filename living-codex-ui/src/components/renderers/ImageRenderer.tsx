'use client';

import React, { useState } from 'react';

interface ImageRendererProps {
  content: string; // base64 encoded image or URL
  mediaType: string;
  className?: string;
}

export function ImageRenderer({ content, mediaType, className = '' }: ImageRendererProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const getImageSrc = () => {
    if (content.startsWith('data:') || content.startsWith('http')) {
      return content;
    }
    return `data:${mediaType};base64,${content}`;
  };

  const handleImageError = () => {
    setImageError(true);
    setIsLoading(false);
  };

  const handleImageLoad = () => {
    setIsLoading(false);
  };

  const getImageIcon = (mediaType: string): string => {
    if (mediaType.includes('svg')) return '🎨';
    if (mediaType.includes('gif')) return '🎬';
    if (mediaType.includes('png')) return '🖼️';
    if (mediaType.includes('jpeg') || mediaType.includes('jpg')) return '📷';
    if (mediaType.includes('webp')) return '🌐';
    if (mediaType.includes('bmp')) return '🖼️';
    if (mediaType.includes('tiff')) return '📄';
    return '🖼️';
  };

  const downloadImage = () => {
    const link = document.createElement('a');
    link.href = getImageSrc();
    link.download = `image.${mediaType.split('/')[1] || 'png'}`;
    link.click();
  };

  if (imageError) {
    return (
      <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-2">
          <span className="text-lg">{getImageIcon(mediaType)}</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Image Content</span>
        </div>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-2">❌</div>
          <p>Failed to load image</p>
          <p className="text-sm">Media type: {mediaType}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">{getImageIcon(mediaType)}</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Image Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {mediaType}
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={downloadImage}
            className="px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          >
            💾 Download
          </button>
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-900/40 transition-colors"
          >
            {isExpanded ? 'Collapse' : 'Expand'}
          </button>
        </div>
      </div>
      <div className="p-4">
        <div className={`${isExpanded ? 'max-w-none' : 'max-w-md'} mx-auto relative`}>
          {isLoading && (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-700 rounded-lg">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          )}
          <img
            src={getImageSrc()}
            alt="Content image"
            onError={handleImageError}
            onLoad={handleImageLoad}
            className={`w-full h-auto rounded-lg ${isExpanded ? 'max-h-96' : 'max-h-64'} object-contain transition-opacity duration-200 ${isLoading ? 'opacity-0' : 'opacity-100'}`}
          />
        </div>
      </div>
    </div>
  );
}
