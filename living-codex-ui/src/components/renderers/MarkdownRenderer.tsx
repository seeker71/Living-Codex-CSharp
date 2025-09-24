'use client';

import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';
import 'highlight.js/styles/github-dark.css';

interface MarkdownRendererProps {
  content: string;
  className?: string;
}

export function MarkdownRenderer({ content, className = '' }: MarkdownRendererProps) {
  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center space-x-2 p-4 border-b border-gray-200 dark:border-gray-700">
        <span className="text-lg">📝</span>
        <span className="font-medium text-gray-900 dark:text-gray-100">Markdown Content</span>
        <span className="text-xs text-gray-500 dark:text-gray-400">
          ({content.length} characters)
        </span>
      </div>
      <div className="p-4">
        <div className="prose prose-sm max-w-none prose-gray dark:prose-invert prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-p:text-gray-700 dark:prose-p:text-gray-300 prose-a:text-blue-600 dark:prose-a:text-blue-400 prose-code:text-gray-900 dark:prose-code:text-gray-100 prose-pre:bg-gray-100 dark:prose-pre:bg-gray-900 prose-pre:border prose-pre:border-gray-200 dark:prose-pre:border-gray-700">
          <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeHighlight]}
            components={{
              // Custom components for better styling
              h1: ({ children }) => (
                <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-8 mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">
                  {children}
                </h1>
              ),
              h2: ({ children }) => (
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mt-6 mb-3">
                  {children}
                </h2>
              ),
              h3: ({ children }) => (
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mt-4 mb-2">
                  {children}
                </h3>
              ),
              h4: ({ children }) => (
                <h4 className="text-base font-semibold text-gray-900 dark:text-gray-100 mt-3 mb-2">
                  {children}
                </h4>
              ),
              h5: ({ children }) => (
                <h5 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mt-2 mb-1">
                  {children}
                </h5>
              ),
              h6: ({ children }) => (
                <h6 className="text-xs font-semibold text-gray-900 dark:text-gray-100 mt-2 mb-1">
                  {children}
                </h6>
              ),
              p: ({ children }) => (
                <p className="text-gray-700 dark:text-gray-300 mb-4 leading-relaxed">
                  {children}
                </p>
              ),
              ul: ({ children }) => (
                <ul className="list-disc list-inside mb-4 space-y-1 text-gray-700 dark:text-gray-300">
                  {children}
                </ul>
              ),
              ol: ({ children }) => (
                <ol className="list-decimal list-inside mb-4 space-y-1 text-gray-700 dark:text-gray-300">
                  {children}
                </ol>
              ),
              li: ({ children }) => (
                <li className="text-gray-700 dark:text-gray-300">
                  {children}
                </li>
              ),
              blockquote: ({ children }) => (
                <blockquote className="border-l-4 border-blue-500 pl-4 py-2 mb-4 bg-blue-50 dark:bg-blue-900/20 text-gray-700 dark:text-gray-300 italic">
                  {children}
                </blockquote>
              ),
              code: ({ children, className }) => {
                const isInline = !className;
                if (isInline) {
                  return (
                    <code className="bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded text-sm font-mono text-gray-900 dark:text-gray-100">
                      {children}
                    </code>
                  );
                }
                return <code className={className}>{children}</code>;
              },
              pre: ({ children }) => (
                <pre className="bg-gray-100 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg p-4 overflow-x-auto mb-4">
                  {children}
                </pre>
              ),
              a: ({ children, href }) => (
                <a
                  href={href}
                  className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 underline"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {children}
                </a>
              ),
              table: ({ children }) => (
                <div className="overflow-x-auto mb-4">
                  <table className="min-w-full border border-gray-200 dark:border-gray-700 rounded-lg">
                    {children}
                  </table>
                </div>
              ),
              thead: ({ children }) => (
                <thead className="bg-gray-50 dark:bg-gray-800">
                  {children}
                </thead>
              ),
              tbody: ({ children }) => (
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {children}
                </tbody>
              ),
              tr: ({ children }) => (
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                  {children}
                </tr>
              ),
              th: ({ children }) => (
                <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider border-b border-gray-200 dark:border-gray-700">
                  {children}
                </th>
              ),
              td: ({ children }) => (
                <td className="px-4 py-2 text-sm text-gray-900 dark:text-gray-100">
                  {children}
                </td>
              ),
              hr: () => (
                <hr className="my-6 border-gray-200 dark:border-gray-700" />
              ),
            }}
          >
            {content}
          </ReactMarkdown>
        </div>
      </div>
    </div>
  );
}
