'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';

import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

import { SmartSearch } from '@/components/ui/SmartSearch';
import KnowledgeMap, { useMockKnowledgeNodes } from '@/components/ui/KnowledgeMap';

interface OntologyNode {
  id: string;
  typeId: string;
  title?: string;
  description?: string;
  meta?: {
    name?: string;
    description?: string;
    keywords?: string[];
    axis?: string;
    parentAxes?: string[];
  };
  level?: number;
}

interface KnowledgeDomain {
  id: string;
  name: string;
  icon: string;
  description: string;
  color: string;
  keywords: string[];
  concepts: OntologyNode[];
}

interface ConceptSummary {
  id: string;
  title: string;
  description: string;
  domain: string;
  keywords: string[];
  typeId: string;
}

const domainDefinitions: KnowledgeDomain[] = [
  {
    id: 'science',
    name: 'Science & Technology',
    icon: '🔬',
    description: 'Scientific discoveries, technological innovations, and research',
    color: 'bg-blue-500',
    keywords: ['science', 'technology', 'research', 'discovery', 'innovation', 'physics', 'chemistry', 'biology'],
    concepts: [],
  },
  {
    id: 'arts',
    name: 'Arts & Culture',
    icon: '🎨',
    description: 'Art, music, literature, philosophy, and cultural expressions',
    color: 'bg-purple-500',
    keywords: ['art', 'music', 'literature', 'culture', 'philosophy', 'creativity', 'expression'],
    concepts: [],
  },
  {
    id: 'society',
    name: 'Society & Humanity',
    icon: '👥',
    description: 'Social sciences, history, politics, and human behavior',
    color: 'bg-green-500',
    keywords: ['society', 'history', 'politics', 'psychology', 'sociology', 'humanity', 'culture'],
    concepts: [],
  },
  {
    id: 'nature',
    name: 'Nature & Environment',
    icon: '🌍',
    description: 'Ecology, environment, biology, and natural sciences',
    color: 'bg-emerald-500',
    keywords: ['nature', 'environment', 'ecology', 'biology', 'earth', 'climate', 'sustainability'],
    concepts: [],
  },
  {
    id: 'health',
    name: 'Health & Wellness',
    icon: '🏥',
    description: 'Medicine, health, psychology, and well-being',
    color: 'bg-red-500',
    keywords: ['health', 'medicine', 'wellness', 'psychology', 'fitness', 'nutrition', 'healing'],
    concepts: [],
  },
  {
    id: 'business',
    name: 'Business & Economics',
    icon: '💼',
    description: 'Business, economics, finance, and entrepreneurship',
    color: 'bg-amber-500',
    keywords: ['business', 'economics', 'finance', 'entrepreneurship', 'management', 'marketing'],
    concepts: [],
  },
];

const quickActions = [
  {
    label: 'Explore All Domains',
    icon: '🌍',
    description: 'Discover domains and their concepts',
    href: '/ontology',
  },
  {
    label: 'Search Knowledge',
    icon: '🔍',
    description: 'Find specific concepts and ideas',
    href: '/ontology?view=search',
  },
  {
    label: 'View Knowledge Map',
    icon: '🕸️',
    description: 'Explore the interconnected web of concepts',
    href: '/graph',
  },
  {
    label: 'Discover New Concepts',
    icon: '🎯',
    description: 'Explore recommended concepts for you',
    href: '/discover',
  },
];

function normalizeTitle(node: OntologyNode): string {
  return node.title || node.meta?.name || node.id;
}

function matchDomain(node: OntologyNode): string {
  const keywords = (node.meta?.keywords || []).map((k) => k.toLowerCase());
  const description = (node.description || node.meta?.description || '').toLowerCase();

  for (const definition of domainDefinitions) {
    if (
      definition.keywords.some((kw) =>
        keywords.some((k) => k.includes(kw)) || description.includes(kw)
      )
    ) {
      return definition.id;
    }
  }

  return 'science';
}

function buildConceptSummaries(nodes: OntologyNode[]): ConceptSummary[] {
  return nodes
    .filter((node) => node.typeId.includes('concept') || node.typeId.includes('axis'))
    .map((node) => ({
      id: node.id,
      title: normalizeTitle(node),
      description: node.description || node.meta?.description || 'No description available',
      domain: matchDomain(node),
      keywords: node.meta?.keywords || [],
      typeId: node.typeId,
    }));
}

export default function OntologyPage() {
  const router = useRouter();
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [nodes, setNodes] = useState<OntologyNode[]>([]);
  const [domains, setDomains] = useState<KnowledgeDomain[]>(domainDefinitions);
  const [viewMode, setViewMode] = useState<'cards' | 'map'>('cards');
  const [activeDomain, setActiveDomain] = useState<string>('overview');
  const [searchResults, setSearchResults] = useState<ConceptSummary[]>([]);
  const [searchCount, setSearchCount] = useState(0);
  const [exploredDomains, setExploredDomains] = useState<Set<string>>(new Set());
  const [discoveredConcepts, setDiscoveredConcepts] = useState<Set<string>>(new Set());

  const knowledgeMapNodes = useMockKnowledgeNodes(24);

  useEffect(() => {
    const fetchNodes = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await fetch(buildApiUrl('/storage-endpoints/nodes')); // mocked in tests
        if (!response.ok) {
          throw new Error(response.statusText || 'Failed to load ontology');
        }

        const payload = await response.json();
        const fetchedNodes: OntologyNode[] = payload.nodes || [];
        setNodes(fetchedNodes);

        const summaries = buildConceptSummaries(fetchedNodes);
        setSearchResults(summaries);

        const mappedDomains = domainDefinitions.map((definition) => ({
          ...definition,
          concepts: summaries
            .filter((concept) => concept.domain === definition.id)
            .map((concept) => ({
              id: concept.id,
              typeId: 'codex.concept',
              title: concept.title,
              description: concept.description,
              meta: { keywords: concept.keywords },
            })),
        }));

        setDomains(mappedDomains);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load ontology');
      } finally {
        setLoading(false);
      }
    };

    fetchNodes();
  }, []);

  const conceptsByDomain = useMemo(() => {
    const grouped: Record<string, ConceptSummary[]> = {};
    searchResults.forEach((concept) => {
      if (!grouped[concept.domain]) {
        grouped[concept.domain] = [];
      }
      grouped[concept.domain].push(concept);
    });
    return grouped;
  }, [searchResults]);

  const getExplorationProgress = () => {
    const totalDomains = domainDefinitions.length;
    const exploredCount = exploredDomains.size;
    const discoveredCount = discoveredConcepts.size;

    return {
      domainsExplored: exploredCount,
      totalDomains,
      conceptsDiscovered: discoveredCount,
      searchCount,
      completionPercentage: totalDomains === 0 ? 0 : Math.round((exploredCount / totalDomains) * 100),
    };
  };

  const handleRetry = () => {
    setActiveDomain('overview');
    setLoading(true);
    setError(null);
    setNodes([]);
    setSearchResults([]);
    setDomains(domainDefinitions);
    setSearchCount(0);
    setExploredDomains(new Set());
    setDiscoveredConcepts(new Set());
  };

  const handleDomainClick = (domainId: string) => {
    setExploredDomains((prev) => new Set(prev).add(domainId));
    setActiveDomain(domainId);
  };

  const handleBackToOverview = () => {
    setActiveDomain('overview');
  };

const handleSearchResultsChange = (results: ConceptSummary[]) => {
  setSearchCount((count) => count + 1);
  setSearchResults(results);
  results.forEach((concept) => {
    setDiscoveredConcepts((prev) => new Set(prev).add(concept.id));
  });
};

  const renderLoading = () => (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center" aria-live="polite">
      <div className="text-center space-y-4">
        <div className="text-gray-400 text-6xl animate-pulse">🌐</div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Loading Knowledge Universe</h2>
        <p className="text-gray-600 dark:text-gray-300">Connecting concepts and building knowledge networks...</p>
      </div>
    </div>
  );

  const renderError = () => (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
      <div className="text-center space-y-4">
        <div className="text-gray-400 text-6xl">😕</div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Connection Lost</h2>
        <p className="text-gray-600 dark:text-gray-300">
          We're having trouble connecting to the knowledge network. This might be temporary.
        </p>
        <div className="flex items-center justify-center gap-3 text-sm">
          <button
            type="button"
            className="px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700"
            onClick={handleRetry}
          >
            🔄 Try Again
          </button>
          <button
            type="button"
            className="px-4 py-2 rounded-lg bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200"
            onClick={() => window.location.reload()}
          >
            🔃 Reload Page
          </button>
        </div>
        {error && (
          <p className="text-xs text-gray-500 dark:text-gray-400">{error}</p>
        )}
      </div>
    </div>
  );

  const renderQuickActions = () => (
    <section aria-label="Quick Actions" className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Quick Actions</h2>
        <span className="text-xs text-gray-500 dark:text-gray-400">Jump into common ontology workflows</span>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {quickActions.map((action) => (
          <button
            key={action.label}
            type="button"
            className="flex items-center justify-between px-4 py-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-blue-300 hover:shadow transition-all"
            onClick={() => router.push(action.href)}
          >
            <div className="flex items-center gap-3">
              <span className="text-2xl" aria-hidden="true">{action.icon}</span>
              <div className="text-left">
                <div className="text-sm font-semibold text-gray-900 dark:text-gray-100">{action.label}</div>
                <div className="text-xs text-gray-600 dark:text-gray-400">{action.description}</div>
              </div>
            </div>
            <span className="text-gray-400">→</span>
          </button>
        ))}
      </div>
    </section>
  );

  const renderDomainOverview = () => (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Featured Knowledge Areas</h3>
          <p className="text-sm text-gray-600 dark:text-gray-400">Browse high-level domains to start your journey.</p>
        </div>
        <div className="text-sm text-blue-600 dark:text-blue-400">More Knowledge Areas →</div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {domains.map((domain) => (
          <div
            key={domain.id}
            className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6 hover:shadow-lg transition-all cursor-pointer"
            onClick={() => handleDomainClick(domain.id)}
          >
            <div className="flex items-center justify-between mb-4">
              <div className={`w-12 h-12 ${domain.color} rounded-full flex items-center justify-center text-white text-xl`}>
                {domain.icon}
              </div>
              <div className="text-right">
                <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{domain.concepts.length}</div>
                <div className="text-xs text-gray-500">Concepts</div>
              </div>
            </div>

            <h4 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">{domain.name}</h4>
            <p className="text-sm text-gray-600 dark:text-gray-300 mb-4">{domain.description}</p>

            <div className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
              {domain.concepts.slice(0, 3).map((concept) => (
                <div key={concept.id} className="truncate">{normalizeTitle(concept)}</div>
              ))}
              {domain.concepts.length > 3 && (
                <div className="text-xs text-gray-500">+{domain.concepts.length - 3} more concepts</div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderKnowledgeMapSection = () => (
    <div className="space-y-4">
      <KnowledgeMap
        nodes={knowledgeMapNodes}
        className="w-full"
        onNodeClick={(nodeId) => {
          trackInteraction('knowledge-map', 'node-click', { nodeId });
          router.push(`/node/${encodeURIComponent(nodeId)}`);
        }}
      />
      <div className="text-center text-sm text-gray-600 dark:text-gray-400">
        Interactive map showing concept connections. Use Cards to browse summaries.
      </div>
    </div>
  );

  const renderDomainDetail = (domainId: string) => {
    const domain = domains.find((d) => d.id === domainId);
    if (!domain) {
      return null;
    }

    return (
      <section className="space-y-6" aria-label={`${domain.name} Details`}>
        <button
          type="button"
          className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
          onClick={handleBackToOverview}
        >
          ← Back to Overview
        </button>

        <header className="space-y-2">
          <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">{domain.icon} {domain.name}</h2>
          <p className="text-sm text-gray-600 dark:text-gray-400">{domain.description}</p>
        </header>

        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">Concepts</h3>
          {domain.concepts.length === 0 ? (
            <p className="text-sm text-gray-600 dark:text-gray-400">No concepts available yet.</p>
          ) : (
            <ul className="space-y-2">
              {domain.concepts.map((concept) => (
                <li key={concept.id} className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100">{normalizeTitle(concept)}</h4>
                      <p className="text-xs text-gray-600 dark:text-gray-400">
                        {concept.description || concept.meta?.description || 'No description available'}
                      </p>
                    </div>
                    <button
                      type="button"
                      className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
                      onClick={() => router.push(`/node/${encodeURIComponent(concept.id)}`)}
                    >
                      View →
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    );
  };

  const renderSearchSection = () => (
    <section className="space-y-4" aria-label="Knowledge Search">
      <div className="space-y-1">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Search Knowledge</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400">Search concepts, people, and ideas.</p>
      </div>
      <SmartSearch
        placeholder="Search knowledge..."
        showFilters
        onResultSelect={(result) => {
          setDiscoveredConcepts((prev) => new Set(prev).add(result.id));
          router.push(`/node/${encodeURIComponent(result.id)}`);
        }}
        onResultsChange={(results) => handleSearchResultsChange(results.map((result) => ({
          id: result.id,
          title: result.title,
          description: result.description,
          domain: matchDomain({
            id: result.id,
            typeId: result.typeId,
            title: result.title,
            description: result.description,
            meta: { keywords: result.tags },
          }),
          keywords: result.tags,
          typeId: result.typeId,
        })))}
      />
    </section>
  );

  const renderConceptCardsSection = () => (
    <section className="space-y-6" aria-label="Featured Concepts">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Featured Concepts</h2>
        <button
          type="button"
          className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
          onClick={() => router.push('/discover')}
        >
          Discover more →
        </button>
      </div>

      {searchResults.length === 0 ? (
        <p className="text-sm text-gray-600 dark:text-gray-400">No concepts to display yet. Try searching to discover more.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {searchResults.slice(0, 6).map((concept) => (
            <div key={concept.id} className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 space-y-2">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">{concept.title}</h3>
                <span className="text-xs text-gray-500 capitalize">{concept.domain}</span>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-300 line-clamp-2">{concept.description}</p>
              <button
                type="button"
                className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
                onClick={() => router.push(`/node/${encodeURIComponent(concept.id)}`)}
              >
                View concept →
              </button>
            </div>
          ))}
        </div>
      )}
    </section>
  );

  const renderInsightsSection = () => {
    const progress = getExplorationProgress();

    return (
      <section className="grid grid-cols-1 lg:grid-cols-3 gap-6" aria-label="Knowledge Insights">
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 space-y-4">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">
              🔄 Fractal Navigation
            </h3>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Navigate between concepts to see how knowledge branches across domains.
            </p>
            <div className="text-xs text-gray-500 dark:text-gray-400">
              Currently viewing: {activeDomain === 'overview' ? 'Overview' : domains.find((d) => d.id === activeDomain)?.name || 'Overview'}
            </div>
          </div>
        </div>

        <div className="space-y-6">
          <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 space-y-3">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">🏆 Your Journey</h3>
            <div className="text-sm text-gray-600 dark:text-gray-300 space-y-1">
              <div className="flex items-center justify-between">
                <span>Domains Explored</span>
                <strong>{progress.domainsExplored}</strong>
              </div>
              <div className="flex items-center justify_between">
                <span>Concepts Discovered</span>
                <strong>{progress.conceptsDiscovered}</strong>
              </div>
              <div className="flex items-center justify-between">
                <span>Searches Performed</span>
                <strong>{progress.searchCount}</strong>
              </div>
            </div>
            <div className="text-xs text-gray-500 dark:text-gray-400">
              Completion: {progress.completionPercentage}% of domains explored
            </div>
          </div>

          <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 space-y-3">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">💡 Quick Tips</h3>
            <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-300">
              <li>Click any domain card to explore concepts in that area.</li>
              <li>Use the search bar to find specific topics quickly.</li>
              <li>Switch to map view to visualize concept connections.</li>
            </ul>
          </div>

          <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 space-y-3">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">📈 Domain Insights</h3>
            <p className="text-sm text-gray-600 dark:text-gray-300">
              {domains
                .map((domain) => `${domain.icon} ${domain.name}: ${domain.concepts.length} concepts`)
                .slice(0, 4)
                .join(' • ')}
            </p>
          </div>
        </div>
      </section>
    );
  };

  const renderFloatingQuickActionsButton = () => (
    <button
      type="button"
      aria-label="Quick Actions"
      className="fixed bottom-6 right-6 px-4 py-3 rounded-full bg-blue-600 text-white shadow-lg hover:bg-blue-700"
      onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
    >
      Quick Actions
    </button>
  );

  if (loading) {
    return renderLoading();
  }

  if (error) {
    return renderError();
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900" data-testid="ontology-page">
      <main className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-12">
        <header className="space-y-2">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">🌍 Knowledge Universe</h1>
          <p className="text-gray-600 dark:text-gray-300 max-w-2xl">
            Explore human knowledge through interconnected concepts.
          </p>
        </header>

        {renderQuickActions()}

        <section className="space-y-4" aria-label="Featured Knowledge Areas Toggle">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Featured Knowledge Areas</h2>
            <div className="inline-flex rounded-lg border border-gray-200 dark:border-gray-700">
              <button
                type="button"
                className={`px-3 py-1 text-sm font-medium ${viewMode === 'cards' ? 'bg-gray-900 text-white' : 'text-gray-600 dark:text-gray-300'}`}
                onClick={() => setViewMode('cards')}
              >
                📋 Cards
              </button>
              <button
                type="button"
                className={`px-3 py-1 text-sm font-medium ${viewMode === 'map' ? 'bg-gray-900 text-white' : 'text-gray-600 dark:text-gray-300'}`}
                onClick={() => setViewMode('map')}
              >
                🕸️ Map
              </button>
            </div>
          </div>

          {viewMode === 'cards' ? renderDomainOverview() : renderKnowledgeMapSection()}
        </section>

        <section className="space-y-4" aria-label="Domain Filter">
          <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 uppercase tracking-wide">
            Filter by Domain
          </h3>
          <select
            className="w-full sm:w-64 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-sm"
            value={activeDomain}
            onChange={(event) => handleDomainClick(event.target.value)}
          >
            <option value="overview">All Domains</option>
            {domains.map((domain) => (
              <option key={domain.id} value={domain.id}>
                {domain.name}
              </option>
            ))}
          </select>
        </section>

        {activeDomain !== 'overview' && renderDomainDetail(activeDomain)}

        {renderSearchSection()}

        {renderConceptCardsSection()}

        {renderInsightsSection()}
      </main>

      {renderFloatingQuickActionsButton()}
    </div>
  );
}
