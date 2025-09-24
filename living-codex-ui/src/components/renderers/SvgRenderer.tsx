'use client';

import React, { useState } from 'react';

interface SvgRendererProps {
  content: string; // SVG content as string
  className?: string;
}

export function SvgRenderer({ content, className = '' }: SvgRendererProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [svgError, setSvgError] = useState(false);

  const handleSvgError = () => {
    setSvgError(true);
  };

  if (svgError) {
    return (
      <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-2">
          <span className="text-lg">🎨</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">SVG Content</span>
        </div>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-2">❌</div>
          <p>Failed to render SVG</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">🎨</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">SVG Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            ({content.length} characters)
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => {
              const blob = new Blob([content], { type: 'image/svg+xml' });
              const url = URL.createObjectURL(blob);
              const a = document.createElement('a');
              a.href = url;
              a.download = 'content.svg';
              a.click();
              URL.revokeObjectURL(url);
            }}
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
        <div className={`${isExpanded ? 'max-w-none' : 'max-w-md'} mx-auto`}>
          <div 
            className={`${isExpanded ? 'max-h-96' : 'max-h-64'} overflow-auto border border-gray-200 dark:border-gray-600 rounded-lg bg-white`}
            dangerouslySetInnerHTML={{ __html: content }}
            onError={handleSvgError}
          />
        </div>
      </div>
    </div>
  );
}
