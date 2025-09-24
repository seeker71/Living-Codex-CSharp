'use client';

import React, { useState, useEffect } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';

interface CodeRendererProps {
  content: string;
  language: string;
  className?: string;
}

export function CodeRenderer({ content, language, className = '' }: CodeRendererProps) {
  const [copySuccess, setCopySuccess] = useState(false);
  const [lineNumbers, setLineNumbers] = useState(true);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(content);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error('Failed to copy code:', err);
    }
  };

  const getLanguageDisplayName = (lang: string): string => {
    const langMap: Record<string, string> = {
      javascript: 'JavaScript',
      typescript: 'TypeScript',
      python: 'Python',
      java: 'Java',
      csharp: 'C#',
      cpp: 'C++',
      c: 'C',
      html: 'HTML',
      css: 'CSS',
      scss: 'SCSS',
      sass: 'Sass',
      sql: 'SQL',
      xml: 'XML',
      yaml: 'YAML',
      yml: 'YAML',
      toml: 'TOML',
      json: 'JSON',
      jsx: 'JSX',
      tsx: 'TSX',
      php: 'PHP',
      ruby: 'Ruby',
      go: 'Go',
      rust: 'Rust',
      swift: 'Swift',
      kotlin: 'Kotlin',
      scala: 'Scala',
      r: 'R',
      matlab: 'MATLAB',
      bash: 'Bash',
      shell: 'Shell',
      powershell: 'PowerShell',
      dockerfile: 'Dockerfile',
      text: 'Text',
      plaintext: 'Plain Text'
    };
    return langMap[lang] || lang.toUpperCase();
  };

  const getLanguageIcon = (lang: string): string => {
    const iconMap: Record<string, string> = {
      javascript: '🟨',
      typescript: '🔷',
      python: '🐍',
      java: '☕',
      csharp: '🔷',
      cpp: '⚙️',
      c: '⚙️',
      html: '🌐',
      css: '🎨',
      scss: '🎨',
      sass: '🎨',
      sql: '🗄️',
      xml: '📄',
      yaml: '⚙️',
      yml: '⚙️',
      toml: '⚙️',
      json: '📋',
      jsx: '⚛️',
      tsx: '⚛️',
      php: '🐘',
      ruby: '💎',
      go: '🐹',
      rust: '🦀',
      swift: '🍎',
      kotlin: '🟣',
      scala: '🔴',
      r: '📊',
      matlab: '📊',
      bash: '💻',
      shell: '💻',
      powershell: '💻',
      dockerfile: '🐳',
      text: '📄',
      plaintext: '📄'
    };
    return iconMap[lang] || '💻';
  };

  return (
    <div className={`bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-2">
          <span className="text-lg">{getLanguageIcon(language)}</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">{getLanguageDisplayName(language)}</span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            ({content.length} characters, {content.split('\n').length} lines)
          </span>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setLineNumbers(!lineNumbers)}
            className="px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          >
            {lineNumbers ? 'Hide' : 'Show'} Lines
          </button>
          <button
            onClick={handleCopy}
            className={`px-3 py-1 text-xs rounded transition-colors ${
              copySuccess 
                ? 'bg-green-600 text-white' 
                : 'bg-blue-600 text-white hover:bg-blue-700'
            }`}
          >
            {copySuccess ? '✓ Copied' : '📋 Copy'}
          </button>
        </div>
      </div>
      <div className="overflow-x-auto">
        <SyntaxHighlighter
          language={language}
          style={vscDarkPlus}
          showLineNumbers={lineNumbers}
          customStyle={{
            margin: 0,
            borderRadius: 0,
            fontSize: '0.875rem',
            lineHeight: '1.5',
          }}
          lineNumberStyle={{
            color: '#6b7280',
            marginRight: '1rem',
            userSelect: 'none',
          }}
          wrapLines={true}
          wrapLongLines={true}
        >
          {content}
        </SyntaxHighlighter>
      </div>
    </div>
  );
}
