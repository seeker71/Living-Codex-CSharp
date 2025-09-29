'use client'

import React, { useEffect, useMemo, useRef, useState } from 'react'
import { buildApiUrl } from '@/lib/config'
import RouteStatusBadge from '@/components/ui/RouteStatusBadge'

interface SearchResult {
  id: string
  title: string
  description: string
  typeId: string
  domain: string
  tags: string[]
  relevance: number
}

export interface SmartSearchProps {
  placeholder?: string
  onResultSelect?: (result: SearchResult) => void
  onResultsChange?: (results: SearchResult[]) => void
  className?: string
  showFilters?: boolean
  autoFocus?: boolean
  debounceMs?: number
  status?: 'Stub' | 'Simple' | 'Simulated' | 'Fallback' | 'AiEnabled' | 'ExternalInfo' | 'Untested' | 'PartiallyTested' | 'FullyTested'
  onSearchStart?: () => void
  onSearchComplete?: (results: SearchResult[]) => void
  initialFilter?: 'all' | 'concepts' | 'people' | 'code' | 'news' | 'images'
}

export function SmartSearch({
  placeholder = 'Search knowledge...',
  onResultSelect,
  onResultsChange,
  className = '',
  showFilters = false,
  autoFocus = false,
  debounceMs = 300,
  status = 'PartiallyTested',
  onSearchStart,
  onSearchComplete,
  initialFilter = 'all'
}: SmartSearchProps) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<SearchResult[]>([])
  const [isSearching, setIsSearching] = useState(false)
  const [selectedFilter, setSelectedFilter] = useState<string>(initialFilter)
  const [showResults, setShowResults] = useState(false)

  const containerRef = useRef<HTMLDivElement | null>(null)
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null)
  const activeSearchIdRef = useRef(0)
  const activeControllerRef = useRef<AbortController | null>(null)

  const filters = useMemo(
    () => [
      { id: 'all', label: 'all', icon: '🔍' },
      { id: 'concepts', label: 'concepts', icon: '💡' },
      { id: 'people', label: 'people', icon: '👥' },
      { id: 'code', label: 'code', icon: '💻' },
      { id: 'news', label: 'news', icon: '📰' },
      { id: 'images', label: 'images', icon: '🖼️' }
    ],
    []
  )

  useEffect(() => () => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
    }
  }, [])

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setShowResults(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleQueryChange = (value: string) => {
    setQuery(value)

    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
    }

    if (value.trim().length < 3) {
      setResults([])
      setShowResults(false)
      setIsSearching(false)
      onResultsChange?.([])
      onSearchComplete?.([])
      return
    }

    debounceTimerRef.current = setTimeout(() => {
      performSearch(value.trim(), selectedFilter)
    }, debounceMs)
  }

  const handleFilterChange = (filterId: string) => {
    setSelectedFilter(filterId)

    if (query.trim().length >= 3) {
      performSearch(query.trim(), filterId)
    }
  }

  const performSearch = async (searchTerm: string, filterId: string) => {
    const searchId = ++activeSearchIdRef.current
    setIsSearching(true)
    onSearchStart?.()

    try {
      const filterSegment = filterId !== 'all' ? `&typeId=${encodeURIComponent(getFilterTypeId(filterId))}` : ''
      const url = buildApiUrl(
        `/storage-endpoints/nodes/search?searchTerm=${encodeURIComponent(searchTerm)}${filterSegment}&take=20`
      )
      activeControllerRef.current?.abort()
      const controller = new AbortController()
      activeControllerRef.current = controller

      const response = await fetch(url, { signal: controller.signal })

      if (activeSearchIdRef.current !== searchId) {
        return
      }

      if (response.ok) {
        const data = await response.json()
        const processed = processResults(data.nodes || [], searchTerm)
        setResults(processed)
        setShowResults(true)
        onResultsChange?.(processed)
        onSearchComplete?.(processed)
      } else {
        handleEmptyResults()
      }
    } catch (error) {
      if ((error as DOMException)?.name === 'AbortError') {
        return
      }
      if (activeSearchIdRef.current === searchId) {
        handleEmptyResults()
      }
    } finally {
      if (activeSearchIdRef.current === searchId) {
        setIsSearching(false)
      }
    }
  }

  const handleEmptyResults = () => {
    setResults([])
    setShowResults(true)
    onResultsChange?.([])
    onSearchComplete?.([])
  }

  const processResults = (nodes: any[], searchTerm: string): SearchResult[] => {
    return nodes
      .map((node) => {
        const title = node.title || node.meta?.name || node.id
        const description = node.description || node.meta?.description || ''
        const tags = node.meta?.keywords || []

        const titleMatch = title.toLowerCase().includes(searchTerm.toLowerCase())
        const descriptionMatch = description.toLowerCase().includes(searchTerm.toLowerCase())
        const keywordMatch = tags.some((tag: string) => tag.toLowerCase().includes(searchTerm.toLowerCase()))

        return {
          id: node.id,
          title,
          description,
          typeId: node.typeId,
          domain: getDomain(tags),
          tags,
          relevance: (titleMatch ? 3 : 0) + (descriptionMatch ? 2 : 0) + (keywordMatch ? 1 : 0)
        }
      })
      .sort((a, b) => b.relevance - a.relevance)
      .slice(0, 10)
  }

  const getDomain = (tags: string[]): string => {
    const normalized = tags.map((tag) => tag.toLowerCase())

    if (normalized.some((tag) => ['ai', 'machine learning', 'algorithms'].includes(tag))) {
      return 'Technology'
    }

    if (normalized.some((tag) => ['quantum', 'physics', 'mechanics'].includes(tag))) {
      return 'General'
    }

    return 'General'
  }

  const getFilterTypeId = (filter: string): string => {
    const mapping: Record<string, string> = {
      concepts: 'codex.concept',
      people: 'codex.user',
      code: 'codex.file',
      news: 'codex.news',
      images: 'codex.image'
    }

    return mapping[filter] || ''
  }

  const handleResultClick = (result: SearchResult) => {
    setShowResults(false)
    setQuery(result.title)
    onResultSelect?.(result)
  }

  const handleInputFocus = () => {
    if (query.trim().length >= 3) {
      setShowResults(results.length > 0)
      if (results.length === 0) {
        performSearch(query.trim(), selectedFilter)
      }
    }
  }

  const handleInputBlur = () => {
    setTimeout(() => {
      if (!containerRef.current) return
      const active = document.activeElement
      if (active && containerRef.current.contains(active)) {
        return
      }
      setShowResults(false)
    }, 100)
  }

  return (
    <div ref={containerRef} className={`relative ${className}`} data-testid="smart-search">
      <RouteStatusBadge status={status} className="mb-2" />

      <div className="relative">
        <input
          type="text"
          value={query}
          onChange={(e) => handleQueryChange(e.target.value)}
          onFocus={handleInputFocus}
          onBlur={handleInputBlur}
          placeholder={placeholder}
          className="smart-search-input w-full px-4 py-3 pr-12 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          data-testid="search-input"
          autoFocus={autoFocus}
        />

        <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
          {isSearching ? (
            <div
              className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"
              role="status"
              aria-live="polite"
              data-testid="loading-spinner"
            ></div>
          ) : (
            <span className="text-gray-400 text-lg">🔍</span>
          )}
        </div>
      </div>

      {showFilters && (
        <div className="flex flex-wrap gap-2 mt-3 mb-2" data-testid="search-filters">
          {filters.map((filter) => (
            <button
              key={filter.id}
              onClick={() => handleFilterChange(filter.id)}
              className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
                selectedFilter === filter.id
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              }`}
              data-testid={`filter-${filter.id}`}
            >
              <span className="mr-1">{filter.icon}</span>
              {filter.label}
            </button>
          ))}
        </div>
      )}

      <div
        className={`absolute z-50 w-full mt-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-96 overflow-y-auto transition-opacity duration-150 ${
          showResults ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'
        }`}
        data-testid="smart-search-results"
      >
        {results.length > 0 ? (
          <div className="py-2">
            {results.map((result) => (
              <button
                key={result.id}
                onClick={() => handleResultClick(result)}
                className="w-full px-4 py-3 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors border-b border-gray-100 dark:border-gray-700 last:border-b-0"
              >
                <div className="flex items-start space-x-3">
                  <span className="text-lg">{getResultIcon(result.typeId)}</span>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between">
                      <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                        {result.title}
                      </h4>
                      <span className="px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                        {result.domain}
                      </span>
                    </div>

                    <p className="text-xs text-gray-600 dark:text-gray-300 mt-1 line-clamp-2">
                      {result.description}
                    </p>

                    <div className="flex items-center justify-between mt-2">
                      <div className="flex flex-wrap gap-1">
                        {result.tags.slice(0, 3).map((tag) => (
                          <span
                            key={tag}
                            className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded-full text-xs"
                          >
                            {tag}
                          </span>
                        ))}
                      </div>

                      <span className="text-xs text-gray-500">
                        {result.typeId.replace('codex.', '')}
                      </span>
                    </div>
                  </div>
                </div>
              </button>
            ))}
          </div>
        ) : (
          <div className="py-8 text-center">
            <div className="text-4xl mb-2">🔍</div>
            <p className="text-sm text-gray-500 dark:text-gray-400">No results found</p>
            {query.trim().length >= 3 && (
              <p className="text-xs text-gray-400 mt-1">Try different keywords or remove filters</p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

const getResultIcon = (typeId: string): string => {
  if (typeId.includes('concept')) return '💡'
  if (typeId.includes('user')) return '👥'
  if (typeId.includes('file') || typeId.includes('code')) return '💻'
  if (typeId.includes('news')) return '📰'
  if (typeId.includes('image')) return '🖼️'
  return '🔵'
}


