'use client';

import React, { useState } from 'react';

interface AudioRendererProps {
  content: string; // base64 encoded audio or URL
  mediaType: string;
  className?: string;
}

export function AudioRenderer({ content, mediaType, className = '' }: AudioRendererProps) {
  const [audioError, setAudioError] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);

  const getAudioSrc = () => {
    if (content.startsWith('data:') || content.startsWith('http')) {
      return content;
    }
    return `data:${mediaType};base64,${content}`;
  };

  const handleAudioError = () => {
    setAudioError(true);
  };

  const handlePlay = () => {
    setIsPlaying(true);
  };

  const handlePause = () => {
    setIsPlaying(false);
  };

  const handleEnded = () => {
    setIsPlaying(false);
  };

  if (audioError) {
    return (
      <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-2">
          <span className="text-lg">🎵</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Audio Content</span>
        </div>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-2">❌</div>
          <p>Failed to load audio</p>
          <p className="text-sm">Media type: {mediaType}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">🎵</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">Audio Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {mediaType}
          </span>
        </div>
        {isPlaying && (
          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">
            🔴 Playing
          </span>
        )}
      </div>
      <div className="p-4">
        <div className="max-w-md mx-auto">
          <audio
            src={getAudioSrc()}
            controls
            onError={handleAudioError}
            onPlay={handlePlay}
            onPause={handlePause}
            onEnded={handleEnded}
            className="w-full"
          >
            Your browser does not support the audio element.
          </audio>
        </div>
      </div>
    </div>
  );
}
