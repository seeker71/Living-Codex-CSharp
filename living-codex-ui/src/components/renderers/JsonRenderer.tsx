'use client';

import React, { useState } from 'react';

interface JsonRendererProps {
  content: string;
  className?: string;
}

export function JsonRenderer({ content, className = '' }: JsonRendererProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [copySuccess, setCopySuccess] = useState(false);

  let formattedJson: string;
  try {
    const parsed = JSON.parse(content);
    formattedJson = JSON.stringify(parsed, null, 2);
  } catch {
    formattedJson = content;
  }

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(formattedJson);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error('Failed to copy JSON:', err);
    }
  };

  const isLargeJson = formattedJson.length > 1000;

  return (
    <div className={`bg-gray-50 dark:bg-gray-800 rounded-lg border ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">📋</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">JSON Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            ({formattedJson.length} characters)
          </span>
        </div>
        <div className="flex items-center space-x-2">
          {isLargeJson && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded hover:bg-blue-200"
            >
              {isExpanded ? 'Collapse' : 'Expand'}
            </button>
          )}
          <button
            onClick={handleCopy}
            className={`px-2 py-1 text-xs rounded ${
              copySuccess 
                ? 'bg-green-100 text-green-800' 
                : 'bg-gray-100 text-gray-800 hover:bg-gray-200'
            }`}
          >
            {copySuccess ? '✓ Copied' : '📋 Copy'}
          </button>
        </div>
      </div>
      <div className="p-3">
        <pre className={`text-sm font-mono text-gray-800 dark:text-gray-200 overflow-x-auto ${
          isLargeJson && !isExpanded ? 'max-h-32 overflow-y-auto' : ''
        }`}>
          {formattedJson}
        </pre>
      </div>
    </div>
  );
}
