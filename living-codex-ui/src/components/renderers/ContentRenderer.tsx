'use client';

import React, { useEffect, useState } from 'react';
import { JsonRenderer } from './JsonRenderer';
import { MarkdownRenderer } from './MarkdownRenderer';
import { CodeRenderer } from './CodeRenderer';
import { ImageRenderer } from './ImageRenderer';
import { SvgRenderer } from './SvgRenderer';
import { HtmlRenderer } from './HtmlRenderer';
import { VideoRenderer } from './VideoRenderer';
import { AudioRenderer } from './AudioRenderer';
import { DefaultRenderer } from './DefaultRenderer';
import { buildApiUrl } from '@/lib/config';

interface ContentRef {
  mediaType: string;
  inlineJson?: string;
  inlineBytes?: string;
  externalUri?: string;
}

interface ContentRendererProps {
  content?: ContentRef | null;
  className?: string;
  nodeId?: string; // optional, enables fetching when only externalUri is set
}

export function ContentRenderer({ content, className = '', nodeId }: ContentRendererProps) {
  const safeContent: ContentRef = content ?? { mediaType: 'text/plain' };
  const { mediaType, inlineJson, inlineBytes, externalUri } = safeContent;

  const [fetchedText, setFetchedText] = useState<string | null>(null);
  const [fetchedMediaType, setFetchedMediaType] = useState<string | null>(null);
  const [triedFetch, setTriedFetch] = useState(false);

  // Reset fetch state when node changes to allow re-hydration after server restarts
  useEffect(() => {
    setTriedFetch(false);
    setFetchedText(null);
    setFetchedMediaType(null);
  }, [nodeId]);

  // Fetch content through backend proxy when nodeId is provided and local inline content is missing.
  useEffect(() => {
    let cancelled = false;
    async function fetchExternal() {
      if (!nodeId) return;
      // If we already have inline content, skip fetching
      if (inlineJson || inlineBytes) return;
      // Avoid duplicate requests if we already have fetched content
      if (fetchedText) return;
      try {
        setTriedFetch(true);
        // Use adapters endpoint to resolve external refs (file:, http:, https:)
        const url = buildApiUrl(`/adapters/content/${encodeURIComponent(nodeId)}`);
        const resp = await fetch(url);
        if (!resp.ok) return;
        const data = await resp.json();
        let content = data?.content ?? '';
        const mt = (data?.mediaType as string) || mediaType;
        // Normalize base64 responses for binary renderers via data URL
        if (data?.encoding === 'base64' && typeof content === 'string' && mt) {
          content = `data:${mt};base64,${content}`;
        }
        if (!cancelled && content) {
          setFetchedText(String(content));
          setFetchedMediaType(mt || null);
        }
      } catch {
        // swallow; fallback to DefaultRenderer link behavior
      }
    }
    // Attempt fetch when we have an external URI or when mediaType indicates fetchable text
    if (externalUri) {
      fetchExternal();
    }
    return () => {
      cancelled = true;
    };
  }, [nodeId, inlineJson, inlineBytes, triedFetch, externalUri, fetchedText, mediaType]);

  // Handle external URI content: if fetched text is not yet available, show link as fallback
  if (externalUri && !fetchedText && !inlineJson && !inlineBytes) {
    return <DefaultRenderer content={{ mediaType, externalUri }} className={className} />;
  }

  // Handle JSON content
  const effectiveMediaType = (fetchedMediaType || mediaType || '').toLowerCase();

  // Handle JSON content
  if ((inlineJson || fetchedText) && (effectiveMediaType.includes('json') || effectiveMediaType.includes('application/json'))) {
    if (fetchedText && !inlineJson) {
      return <JsonRenderer content={fetchedText} className={className} />;
    }
    return <JsonRenderer content={inlineJson || ''} className={className} />;
  }

  // Handle Markdown content
  if ((inlineJson || fetchedText) && (effectiveMediaType.includes('markdown') || effectiveMediaType.includes('text/markdown') || effectiveMediaType.includes('text/x-markdown'))) {
    return <MarkdownRenderer content={inlineJson || fetchedText || ''} className={className} />;
  }

  // Handle HTML content
  if ((inlineJson || fetchedText) && (effectiveMediaType.includes('html') || effectiveMediaType.includes('text/html'))) {
    return <HtmlRenderer content={inlineJson || fetchedText || ''} className={className} />;
  }

  // Handle SVG content
  if ((inlineJson || fetchedText) && effectiveMediaType.includes('svg')) {
    return <SvgRenderer content={inlineJson || fetchedText || ''} className={className} />;
  }

  // Handle video content
  if ((inlineBytes || inlineJson || fetchedText) && effectiveMediaType.includes('video/')) {
    return <VideoRenderer content={inlineBytes || inlineJson || fetchedText || ''} mediaType={effectiveMediaType} className={className} />;
  }

  // Handle audio content
  if ((inlineBytes || inlineJson || fetchedText) && effectiveMediaType.includes('audio/')) {
    return <AudioRenderer content={inlineBytes || inlineJson || fetchedText || ''} mediaType={effectiveMediaType} className={className} />;
  }

  // Handle image content (including SVG as bytes)
  if ((inlineBytes || fetchedText) && (effectiveMediaType.includes('image/') || effectiveMediaType.includes('svg'))) {
    return <ImageRenderer content={inlineBytes || fetchedText || ''} mediaType={effectiveMediaType} className={className} />;
  }

  // Handle code/text content
  if ((inlineJson || fetchedText) && (effectiveMediaType.includes('text/') || effectiveMediaType.includes('application/'))) {
    const textContent = inlineJson || fetchedText || '';
    // Render plain text with preserved newlines and wrapping
    if (effectiveMediaType.includes('text/plain')) {
      return (
        <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-3 ${className}`}>
          <pre className="whitespace-pre-wrap break-words text-sm text-gray-900 dark:text-gray-100">{textContent}</pre>
        </div>
      );
    }
    // Check if it looks like code based on media type or content
    if (isCodeContent(effectiveMediaType)) {
      return <CodeRenderer content={textContent} language={getLanguageFromMediaType(effectiveMediaType)} className={className} />;
    }
  }

  // Default renderer for unknown content types
  // If no content provided at all
  if (!content && !fetchedText && !inlineJson && !inlineBytes) {
    return <DefaultRenderer content={null} className={className} />;
  }

  return <DefaultRenderer content={safeContent} className={className} />;
}

function isCodeContent(mediaType: string): boolean {
  const codeTypes = [
    'javascript', 'typescript', 'python', 'java', 'csharp', 'cpp', 'c',
    'html', 'css', 'scss', 'sass', 'sql', 'xml', 'yaml', 'yml', 'toml',
    'json', 'jsx', 'tsx', 'php', 'ruby', 'go', 'rust', 'swift', 'kotlin',
    'scala', 'r', 'matlab', 'bash', 'shell', 'powershell', 'dockerfile'
  ];
  
  return codeTypes.some(type => mediaType.includes(type));
}

function getLanguageFromMediaType(mediaType: string): string {
  const typeMap: Record<string, string> = {
    'text/javascript': 'javascript',
    'application/javascript': 'javascript',
    'text/typescript': 'typescript',
    'application/typescript': 'typescript',
    'text/python': 'python',
    'text/x-python': 'python',
    'text/java': 'java',
    'text/x-java': 'java',
    'text/x-csharp': 'csharp',
    'text/x-c++': 'cpp',
    'text/x-c': 'c',
    'text/html': 'html',
    'text/css': 'css',
    'text/x-scss': 'scss',
    'text/x-sass': 'sass',
    'application/sql': 'sql',
    'text/x-sql': 'sql',
    'text/xml': 'xml',
    'application/xml': 'xml',
    'text/yaml': 'yaml',
    'application/x-yaml': 'yaml',
    'text/x-yaml': 'yaml',
    'text/toml': 'toml',
    'application/x-toml': 'toml',
    'text/x-toml': 'toml',
    'application/json': 'json',
    'text/x-jsx': 'jsx',
    'text/x-tsx': 'tsx',
    'text/x-php': 'php',
    'application/x-php': 'php',
    'text/x-ruby': 'ruby',
    'application/x-ruby': 'ruby',
    'text/x-go': 'go',
    'application/x-go': 'go',
    'text/x-rust': 'rust',
    'application/x-rust': 'rust',
    'text/x-swift': 'swift',
    'application/x-swift': 'swift',
    'text/x-kotlin': 'kotlin',
    'application/x-kotlin': 'kotlin',
    'text/x-scala': 'scala',
    'application/x-scala': 'scala',
    'text/x-r': 'r',
    'application/x-r': 'r',
    'text/x-matlab': 'matlab',
    'application/x-matlab': 'matlab',
    'text/x-bash': 'bash',
    'application/x-bash': 'bash',
    'text/x-shellscript': 'shell',
    'application/x-shellscript': 'shell',
    'text/x-powershell': 'powershell',
    'application/x-powershell': 'powershell',
    'text/x-dockerfile': 'dockerfile',
    'application/x-dockerfile': 'dockerfile',
    'text/plain': 'text',
    'text/x-text': 'text'
  };

  for (const [type, lang] of Object.entries(typeMap)) {
    if (mediaType.includes(type)) {
      return lang;
    }
  }

  // Fallback: try to extract language from media type
  if (mediaType.includes('javascript')) return 'javascript';
  if (mediaType.includes('typescript')) return 'typescript';
  if (mediaType.includes('python')) return 'python';
  if (mediaType.includes('java')) return 'java';
  if (mediaType.includes('csharp')) return 'csharp';
  if (mediaType.includes('cpp')) return 'cpp';
  if (mediaType.includes('c ')) return 'c';
  if (mediaType.includes('html')) return 'html';
  if (mediaType.includes('css')) return 'css';
  if (mediaType.includes('scss')) return 'scss';
  if (mediaType.includes('sass')) return 'sass';
  if (mediaType.includes('sql')) return 'sql';
  if (mediaType.includes('xml')) return 'xml';
  if (mediaType.includes('yaml')) return 'yaml';
  if (mediaType.includes('toml')) return 'toml';
  if (mediaType.includes('json')) return 'json';
  if (mediaType.includes('jsx')) return 'jsx';
  if (mediaType.includes('tsx')) return 'tsx';
  if (mediaType.includes('php')) return 'php';
  if (mediaType.includes('ruby')) return 'ruby';
  if (mediaType.includes('go ')) return 'go';
  if (mediaType.includes('rust')) return 'rust';
  if (mediaType.includes('swift')) return 'swift';
  if (mediaType.includes('kotlin')) return 'kotlin';
  if (mediaType.includes('scala')) return 'scala';
  if (mediaType.includes('r ')) return 'r';
  if (mediaType.includes('matlab')) return 'matlab';
  if (mediaType.includes('bash')) return 'bash';
  if (mediaType.includes('shell')) return 'shell';
  if (mediaType.includes('powershell')) return 'powershell';
  if (mediaType.includes('dockerfile')) return 'dockerfile';

  return 'text';
}
