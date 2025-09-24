'use client';

import React, { useState } from 'react';

interface ContentRef {
  mediaType?: string;
  inlineJson?: string;
  inlineBytes?: string;
  externalUri?: string;
}

interface DefaultRendererProps {
  content: ContentRef | null;
  className?: string;
}

export function DefaultRenderer({ content, className = '' }: DefaultRendererProps) {
  const [copySuccess, setCopySuccess] = useState(false);

  if (!content) {
    return (
      <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border p-4 ${className}`}>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-2">📄</div>
          <p>No content available</p>
        </div>
      </div>
    );
  }

  const handleCopy = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error('Failed to copy content:', err);
    }
  };

  const renderContent = () => {
    // External URI
    if (content.externalUri) {
      return (
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              External URI
            </label>
            <a
              href={content.externalUri}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 hover:text-blue-800 underline break-all"
            >
              {content.externalUri}
            </a>
          </div>
          {content.mediaType && (
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                Media Type
              </label>
              <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 rounded text-sm font-mono">
                {content.mediaType}
              </span>
            </div>
          )}
        </div>
      );
    }

    // Inline JSON
    if (content.inlineJson) {
      return (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
              Content
            </label>
            <button
              onClick={() => handleCopy(content.inlineJson!)}
              className={`px-2 py-1 text-xs rounded transition-colors ${
                copySuccess 
                  ? 'bg-green-100 text-green-800' 
                  : 'bg-gray-100 text-gray-800 hover:bg-gray-200'
              }`}
            >
              {copySuccess ? '✓ Copied' : '📋 Copy'}
            </button>
          </div>
          <pre className="bg-gray-100 dark:bg-gray-700 p-3 rounded text-sm font-mono overflow-x-auto max-h-64 overflow-y-auto">
            {content.inlineJson}
          </pre>
        </div>
      );
    }

    // Inline Bytes
    if (content.inlineBytes) {
      return (
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              Binary Content
            </label>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              Binary content available ({content.inlineBytes.length} bytes)
            </p>
            {content.mediaType && (
              <span className="inline-block mt-2 px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 rounded text-sm font-mono">
                {content.mediaType}
              </span>
            )}
          </div>
        </div>
      );
    }

    // No content
    return (
      <div className="text-center py-8 text-gray-500">
        <div className="text-4xl mb-2">📄</div>
        <p>No content available</p>
        {content.mediaType && (
          <p className="text-sm mt-2">
            Media type: <span className="font-mono">{content.mediaType}</span>
          </p>
        )}
      </div>
    );
  };

  return (
    <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border p-4 ${className}`}>
      <div className="flex items-center space-x-2 mb-3">
        <span className="text-lg">📄</span>
        <span className="font-medium text-gray-900 dark:text-gray-100">Content</span>
        {content.mediaType && (
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {content.mediaType}
          </span>
        )}
      </div>
      {renderContent()}
    </div>
  );
}
