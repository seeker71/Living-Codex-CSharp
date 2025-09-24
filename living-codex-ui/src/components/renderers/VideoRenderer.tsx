'use client';

import React, { useState } from 'react';

interface VideoRendererProps {
  content: string; // base64 encoded video or URL
  mediaType: string;
  className?: string;
}

export function VideoRenderer({ content, mediaType, className = '' }: VideoRendererProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [videoError, setVideoError] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);

  const getVideoSrc = () => {
    if (content.startsWith('data:') || content.startsWith('http')) {
      return content;
    }
    return `data:${mediaType};base64,${content}`;
  };

  const handleVideoError = () => {
    setVideoError(true);
  };

  const handlePlay = () => {
    setIsPlaying(true);
  };

  const handlePause = () => {
    setIsPlaying(false);
  };

  if (videoError) {
    return (
      <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-2">
          <span className="text-lg">🎥</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Video Content</span>
        </div>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-2">❌</div>
          <p>Failed to load video</p>
          <p className="text-sm">Media type: {mediaType}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">🎥</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Video Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {mediaType}
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded hover:bg-blue-200 dark:hover:bg-blue-900/40 transition-colors"
          >
            {isExpanded ? 'Collapse' : 'Expand'}
          </button>
        </div>
      </div>
      <div className="p-4">
        <div className={`${isExpanded ? 'max-w-none' : 'max-w-md'} mx-auto`}>
          <video
            src={getVideoSrc()}
            controls
            onError={handleVideoError}
            onPlay={handlePlay}
            onPause={handlePause}
            className={`w-full h-auto rounded-lg ${isExpanded ? 'max-h-96' : 'max-h-64'} object-contain`}
            poster="data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAiIGhlaWdodD0iNDAiIHZpZXdCb3g9IjAgMCA0MCA0MCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHJlY3Qgd2lkdGg9IjQwIiBoZWlnaHQ9IjQwIiBmaWxsPSIjRjNGNEY2Ii8+CjxwYXRoIGQ9Ik0xNiAxMlYyOEwyOCAyMEwxNiAxMloiIGZpbGw9IiM2QjcyODAiLz4KPC9zdmc+"
          >
            Your browser does not support the video tag.
          </video>
          {isPlaying && (
            <div className="mt-2 text-center">
              <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">
                🔴 Playing
              </span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
