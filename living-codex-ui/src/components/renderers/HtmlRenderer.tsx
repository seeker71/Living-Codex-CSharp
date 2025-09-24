'use client';

import React, { useState, useRef, useEffect } from 'react';

interface HtmlRendererProps {
  content: string; // HTML content as string
  className?: string;
}

export function HtmlRenderer({ content, className = '' }: HtmlRendererProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isIframeMode, setIsIframeMode] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  const handleIframeLoad = () => {
    // Ensure iframe content is properly styled
    if (iframeRef.current?.contentDocument) {
      const iframeDoc = iframeRef.current.contentDocument;
      const style = iframeDoc.createElement('style');
      style.textContent = `
        body { 
          margin: 0; 
          padding: 16px; 
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
          background: white;
          color: #1f2937;
        }
        .dark body {
          background: #1f2937;
          color: #f9fafb;
        }
        a { color: #3b82f6; text-decoration: underline; }
        a:hover { color: #1d4ed8; }
        pre { background: #f3f4f6; padding: 12px; border-radius: 6px; overflow-x: auto; }
        code { background: #f3f4f6; padding: 2px 4px; border-radius: 3px; font-family: monospace; }
        .dark pre { background: #374151; }
        .dark code { background: #374151; }
      `;
      iframeDoc.head.appendChild(style);
    }
  };

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">🌐</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">HTML Content</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            ({content.length} characters)
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setIsIframeMode(!isIframeMode)}
            className={`px-2 py-1 text-xs rounded transition-colors ${
              isIframeMode 
                ? 'bg-blue-600 text-white' 
                : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
            }`}
          >
            {isIframeMode ? '📄 Source' : '🖼️ Preview'}
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
        {isIframeMode ? (
          <div className={`${isExpanded ? 'h-96' : 'h-64'} border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden`}>
            <iframe
              ref={iframeRef}
              srcDoc={content}
              className="w-full h-full"
              onLoad={handleIframeLoad}
              sandbox="allow-scripts allow-same-origin allow-forms allow-popups"
              title="HTML Content Preview"
            />
          </div>
        ) : (
          <div className={`${isExpanded ? 'max-h-96' : 'max-h-64'} overflow-auto border border-gray-200 dark:border-gray-600 rounded-lg bg-gray-50 dark:bg-gray-900 p-4`}>
            <pre className="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap font-mono">
              {content}
            </pre>
          </div>
        )}
      </div>
    </div>
  );
}
