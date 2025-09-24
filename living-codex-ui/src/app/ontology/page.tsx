'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
// Navigation is provided globally via RootLayout
import { PaginationControls } from '@/components/ui/PaginationControls';
import { Card, CardContent } from '@/components/ui/Card';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface OntologyAxis {
  id: string;
  name: string;
  keywords: string[];
  description: string;
  conceptCount?: number;
  score?: number;
}

interface ConceptNode {
  id: string;
  title: string;
  description: string;
  typeId: string;
  axis?: string;
  relationships: string[];
  score?: number;
}

interface AxisRelationship {
  from: string;
  to: string;
  strength: number;
  type: string;
}

export default function OntologyPage() {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Data state
  const [ontologyAxes, setOntologyAxes] = useState<OntologyAxis[]>([]);
  const [concepts, setConcepts] = useState<ConceptNode[]>([]);
  const [relationships, setRelationships] = useState<AxisRelationship[]>([]);
  
  // UI state
  const [selectedAxis, setSelectedAxis] = useState<string>('');
  const [selectedConcept, setSelectedConcept] = useState<string>('');
  const [viewMode, setViewMode] = useState<'axes' | 'concepts' | 'relationships'>('axes');
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  
  // Concepts pagination state (client-side for now)
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(50);
  const [totalConcepts, setTotalConcepts] = useState(0);
  const [loadingConcepts, setLoadingConcepts] = useState(false);

  // Axes pagination (server-side via storage-endpoints)
  const [axesPage, setAxesPage] = useState(1);
  const [axesPageSize] = useState(20);
  const [axesTotalCount, setAxesTotalCount] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string>('');

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('ontology-page', 'page-visit', { description: 'User visited U-CORE ontology browser' });
    }
  }, [user?.id, trackInteraction]);

  // Load ontology data
  useEffect(() => {
    console.log('useEffect calling loadOntologyData');
    loadOntologyData();
  }, []);

  // Load concepts when filters or pagination change
  useEffect(() => {
    if (viewMode === 'concepts') {
      loadConceptsWithFilters();
    }
  }, [viewMode, selectedAxis, searchQuery, currentPage]);

  // Debug: Log when ontologyAxes changes
  useEffect(() => {
    console.log('ontologyAxes state changed, length:', ontologyAxes.length);
  }, [ontologyAxes]);

  const loadOntologyData = async () => {
    console.log('loadOntologyData called, loading state:', loading);
    if (loading) {
      console.log('loadOntologyData already in progress, skipping');
      return;
    }
    console.log('Starting loadOntologyData');
    setLoading(true);
    setErrorMessage('');
    try {
      // Load U-CORE axes from storage with server-side pagination
      const skipAxes = (axesPage - 1) * axesPageSize;
      const axesUrl = buildApiUrl(`/storage-endpoints/nodes?typeId=codex.ontology.axis&skip=${skipAxes}&take=${axesPageSize}`);
      console.log('Loading axes from:', axesUrl);
      console.log('About to call fetch...');
      const axesResponse = await fetch(axesUrl);
      console.log('Fetch completed, response status:', axesResponse.status);
      let axes: OntologyAxis[] = [];
      if (axesResponse.ok) {
        const axesData = await axesResponse.json();
        console.log('Axes data received:', axesData);
        if (axesData.nodes) {
          axes = axesData.nodes.map((node: any) => ({
            id: node.id,
            name: node.meta?.name || node.title || node.id,
            keywords: Array.isArray(node.meta?.keywords) ? node.meta.keywords : [],
            description: node.description || `U-CORE ontology axis: ${node.meta?.name || node.title || node.id}`,
            conceptCount: 0
          }));
          console.log('Mapped axes:', axes);
          if (typeof axesData.totalCount === 'number') {
            setAxesTotalCount(axesData.totalCount);
          } else {
            setAxesTotalCount(axes.length);
          }
        }
      } else {
        setErrorMessage(`Failed to load ontology axes: ${axesResponse.status} ${axesResponse.statusText}`);
      }

      // If no axes found, use default U-CORE axes
      if (axes.length === 0) {
        const defaultAxes: OntologyAxis[] = [
          {
            id: 'abundance',
            name: 'Abundance',
            keywords: ['abundance', 'amplification', 'growth', 'prosperity', 'opportunity'],
            description: 'Concepts related to growth, prosperity, and amplification of positive outcomes',
            conceptCount: 0
          },
          {
            id: 'unity',
            name: 'Unity',
            keywords: ['unity', 'collaboration', 'collective', 'community', 'global'],
            description: 'Concepts fostering connection, collaboration, and collective harmony',
            conceptCount: 0
          },
          {
            id: 'resonance',
            name: 'Resonance',
            keywords: ['resonance', 'harmony', 'coherence', 'joy', 'love', 'peace', 'wisdom'],
            description: 'Concepts that create harmonic alignment and positive vibration',
            conceptCount: 0
          },
          {
            id: 'innovation',
            name: 'Innovation',
            keywords: ['innovation', 'breakthrough', 'cutting-edge', 'new', 'discovery'],
            description: 'Concepts driving breakthrough thinking and novel solutions',
            conceptCount: 0
          },
          {
            id: 'science',
            name: 'Science',
            keywords: ['science', 'research', 'study', 'experiment', 'data'],
            description: 'Concepts grounded in scientific method and empirical understanding',
            conceptCount: 0
          },
          {
            id: 'consciousness',
            name: 'Consciousness',
            keywords: ['awareness', 'consciousness', 'mind', 'intention', 'presence', 'clarity'],
            description: 'Concepts exploring awareness, mindfulness, and expanded consciousness',
            conceptCount: 0
          },
          {
            id: 'impact',
            name: 'Impact',
            keywords: ['impact', 'transformation', 'change', 'influence', 'effect'],
            description: 'Concepts focused on creating meaningful transformation and positive change',
            conceptCount: 0
          }
        ];
        axes = defaultAxes;
      }
      
      console.log('Setting ontology axes to:', axes);
      setOntologyAxes(axes);
      console.log('Ontology axes set, skipping concepts loading for now...');

      // Temporarily skip concepts loading to isolate the issue
      // TODO: Re-enable concepts loading once axes are working
      
      console.log('loadOntologyData completed successfully');

    } catch (error) {
      console.error('Error loading ontology data:', error);
      console.error('Error details:', error);
      setErrorMessage(error instanceof Error ? error.message : 'Unknown error while loading ontology');
    } finally {
      console.log('loadOntologyData finally block - setting loading to false');
      setLoading(false);
    }
  };

  const loadConceptsWithFilters = async () => {
    setLoadingConcepts(true);
    try {
      // Server-side pagination via storage endpoints
      const skip = (currentPage - 1) * pageSize;
      const params = new URLSearchParams();
      params.set('typeId', 'codex.concept');
      params.set('skip', String(skip));
      params.set('take', String(pageSize));
      if (searchQuery.trim()) params.set('searchTerm', searchQuery.trim());
      const response = await fetch(buildApiUrl(`/storage-endpoints/nodes?${params.toString()}`));
      if (response.ok) {
        const data = await response.json();
        if (data.nodes) {
          let conceptNodes = data.nodes.map((node: any) => ({
            id: node.id,
            title: node.meta?.name || node.title,
            description: node.meta?.description || node.description || '',
            typeId: node.typeId || 'codex.concept',
            axis: determineAxis(node, ontologyAxes),
            relationships: [],
            score: 0
          }));

          // Apply search filter client-side
          if (searchQuery.trim()) {
            const query = searchQuery.toLowerCase();
            conceptNodes = conceptNodes.filter((concept: ConceptNode) => 
              concept.title.toLowerCase().includes(query) || 
              concept.description.toLowerCase().includes(query)
            );
          }

          // Apply axis filter client-side
          if (selectedAxis) {
            conceptNodes = conceptNodes.filter((concept: ConceptNode) => concept.axis === selectedAxis);
          }

          setConcepts(conceptNodes);
          setTotalConcepts(typeof data.totalCount === 'number' ? data.totalCount : conceptNodes.length);
        }
      }
    } catch (error) {
      console.error('Error loading filtered concepts:', error);
    } finally {
      setLoadingConcepts(false);
    }
  };

  const determineAxis = (node: any, axes: OntologyAxis[]): string => {
    const title = node.name || node.title || '';
    const description = node.description || '';
    const text = `${title} ${description}`.toLowerCase();
    let bestAxis = '';
    let bestScore = 0;

    axes.forEach(axis => {
      const score = axis.keywords.reduce((acc, keyword) => {
        return acc + (text.includes(keyword.toLowerCase()) ? 1 : 0);
      }, 0);
      
      if (score > bestScore) {
        bestScore = score;
        bestAxis = axis.id;
      }
    });

    return bestAxis;
  };

  const updateAxisConceptCounts = (axes: OntologyAxis[], conceptNodes: ConceptNode[]) => {
    const updatedAxes = axes.map(axis => ({
      ...axis,
      conceptCount: conceptNodes.filter(concept => concept.axis === axis.id).length
    }));
    setOntologyAxes(updatedAxes);
  };

  const selectAxis = (axisId: string) => {
    setSelectedAxis(axisId);
    setSelectedConcept('');
    setCurrentPage(1); // Reset pagination when axis changes
    
    // Track axis exploration
    if (user?.id) {
      trackInteraction(axisId, 'explore-axis', {
        description: `User explored ${axisId} axis`,
        axisName: ontologyAxes.find(a => a.id === axisId)?.name
      });
    }
  };

  const handleSearchChange = (query: string) => {
    setSearchQuery(query);
    setCurrentPage(1); // Reset pagination when search changes
  };

  const handleAxisFilterChange = (axisId: string) => {
    setSelectedAxis(axisId);
    setCurrentPage(1); // Reset pagination when filter changes
  };

  const selectConcept = (conceptId: string) => {
    setSelectedConcept(conceptId);
    
    // Track concept exploration
    if (user?.id) {
      trackInteraction(conceptId, 'explore-concept', {
        description: `User explored concept: ${concepts.find(c => c.id === conceptId)?.title}`,
        conceptType: concepts.find(c => c.id === conceptId)?.typeId
      });
    }
  };

  const getAxisColor = (axisId: string): string => {
    const colors: Record<string, string> = {
      abundance: 'green',
      unity: 'blue',
      resonance: 'purple',
      innovation: 'orange',
      science: 'cyan',
      consciousness: 'indigo',
      impact: 'red'
    };
    return colors[axisId] || 'gray';
  };

  // Concepts are now filtered server-side, no need for client-side filtering
  const filteredConcepts = concepts;

  const getAxisStats = (axis: OntologyAxis) => {
    const axisConcepts = concepts.filter(c => c.axis === axis.id);
    const avgScore = axisConcepts.length > 0 
      ? axisConcepts.reduce((sum, c) => sum + (c.score || 0), 0) / axisConcepts.length 
      : 0;
    
    return {
      conceptCount: axisConcepts.length,
      avgScore: avgScore.toFixed(2),
      topConcepts: axisConcepts.slice(0, 3)
    };
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      
      <div className="max-w-7xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">🧠 U-CORE Ontology Browser</h1>
          <p className="text-gray-600 dark:text-gray-300">
            Explore the Universal Consciousness Ontology for Reality Enhancement (U-CORE) axes and concept relationships
          </p>
        </div>

        {/* Controls */}
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 mb-6">
          {errorMessage && (
            <div className="mb-4 p-3 rounded-md border border-red-300 bg-red-50 text-red-700 flex items-center justify-between">
              <span className="text-sm">{errorMessage}</span>
              <button
                onClick={() => loadOntologyData()}
                className="ml-4 px-3 py-1 text-sm bg-red-600 text-white rounded-md hover:bg-red-700"
              >
                Retry
              </button>
            </div>
          )}
          <div className="flex flex-col md:flex-row gap-4">
            {/* View Mode */}
            <div className="flex space-x-2">
              {[
                { id: 'axes', label: 'Axes', icon: '🌟' },
                { id: 'concepts', label: 'Concepts', icon: '🧩' },
                { id: 'relationships', label: 'Relations', icon: '🔗' }
              ].map((mode) => (
                <button
                  key={mode.id}
                  onClick={() => setViewMode(mode.id as any)}
                  className={`px-4 py-2 rounded-md font-medium text-sm transition-colors ${
                    viewMode === mode.id
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
                >
                  {mode.icon} {mode.label}
                </button>
              ))}
            </div>

            {/* Search */}
            <div className="flex-1">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => handleSearchChange(e.target.value)}
                placeholder="Search concepts..."
                className="input-standard"
              />
            </div>

            {/* Axis Filter */}
            <div>
              <select
                value={selectedAxis}
                onChange={(e) => handleAxisFilterChange(e.target.value)}
                className="input-standard"
              >
                <option value="">All Axes</option>
                {ontologyAxes.map(axis => (
                  <option key={axis.id} value={axis.id}>
                    {axis.name} ({axis.conceptCount})
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {loading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-500">Loading U-CORE ontology...</p>
            <p className="mt-2 text-sm text-gray-400">Loading state: {loading.toString()}, Axes count: {ontologyAxes.length}</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Main Content */}
            <div className="lg:col-span-2">
              {/* Axes View */}
              {viewMode === 'axes' && (
                <Card className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
                  <CardContent>
                    <div className="flex items-center justify-between mb-4">
                      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        🌟 U-CORE Ontology Axes ({axesTotalCount})
                      </h2>
                      {axesTotalCount > axesPageSize && (
                        <PaginationControls
                          currentPage={axesPage}
                          pageSize={axesPageSize}
                          totalCount={axesTotalCount}
                          onPageChange={(p) => {
                            setAxesPage(p);
                            // Reload axes for the new page
                            loadOntologyData();
                          }}
                        />
                      )}
                    </div>
                    <div className="divide-y divide-gray-200 dark:divide-gray-700">
                    {ontologyAxes.map((axis) => {
                      const stats = getAxisStats(axis);
                      const color = getAxisColor(axis.id);
                      return (
                        <div 
                          key={axis.id} 
                          className={`p-6 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer ${
                            selectedAxis === axis.id ? 'bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 dark:border-blue-400' : ''
                          }`}
                          onClick={() => selectAxis(axis.id)}
                        >
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <div className="flex items-center space-x-3 mb-2">
                                <div className={`w-4 h-4 rounded-full bg-${color}-500`}></div>
                                <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                                  {axis.name}
                                </h3>
                                <span className="text-sm text-gray-500 dark:text-gray-400">
                                  {stats.conceptCount} concepts
                                </span>
                              </div>
                              <p className="text-gray-600 dark:text-gray-300 mb-3">{axis.description}</p>
                              
                              {/* Keywords */}
                              <div className="flex flex-wrap gap-1 mb-3">
                                {axis.keywords.slice(0, 6).map((keyword, idx) => (
                                  <span
                                    key={idx}
                                    className={`px-2 py-1 bg-${color}-100 dark:bg-${color}-900/30 text-${color}-800 dark:text-${color}-300 rounded-md text-xs`}
                                  >
                                    {keyword}
                                  </span>
                                ))}
                                {axis.keywords.length > 6 && (
                                  <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-md text-xs">
                                    +{axis.keywords.length - 6} more
                                  </span>
                                )}
                              </div>

                              {/* Top Concepts */}
                              {stats.topConcepts.length > 0 && (
                                <div className="text-sm">
                                  <span className="text-gray-500 dark:text-gray-400">Top concepts: </span>
                                  {stats.topConcepts.map((concept, idx) => (
                                    <span key={idx} className="text-gray-700 dark:text-gray-200">
                                      {concept.title}
                                      {idx < stats.topConcepts.length - 1 && ', '}
                                    </span>
                                  ))}
                                </div>
                              )}
                            </div>
                            
                            <div className="text-right ml-4">
                              <div className="text-2xl mb-2">
                                {selectedAxis === axis.id ? '🔍' : '👁️'}
                              </div>
                              <div className="text-xs text-gray-500 dark:text-gray-400">
                                Avg Score: {stats.avgScore}
                              </div>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Concepts View */}
              {viewMode === 'concepts' && (
                <Card className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
                  <div className="p-6 border-b border-gray-200 dark:border-gray-700">
                    <div className="flex items-center justify-between">
                      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                        🧩 Concepts ({totalConcepts})
                        {selectedAxis && (
                          <span className="text-blue-600 dark:text-blue-400 ml-2">
                            in {ontologyAxes.find(a => a.id === selectedAxis)?.name}
                          </span>
                        )}
                      </h2>
                      {loadingConcepts && (
                        <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500"></div>
                          Loading...
                        </div>
                      )}
                    </div>
                    {totalConcepts > pageSize && (
                      <div className="mt-4">
                        <PaginationControls
                          currentPage={currentPage}
                          pageSize={pageSize}
                          totalCount={totalConcepts}
                          onPageChange={(p) => setCurrentPage(p)}
                        />
                      </div>
                    )}
                  </div>
                  <div className="divide-y divide-gray-200 dark:divide-gray-700">
                    {filteredConcepts.length > 0 ? (
                      filteredConcepts.map((concept) => {
                        const axis = ontologyAxes.find(a => a.id === concept.axis);
                        const color = getAxisColor(concept.axis || 'gray');
                        const nodeDetailHref = `/node/${encodeURIComponent(concept.id)}`;
                        return (
                          <div 
                            key={concept.id}
                            className={`p-6 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer ${
                              selectedConcept === concept.id ? 'bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 dark:border-blue-400' : ''
                            }`}
                            onClick={() => selectConcept(concept.id)}
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1">
                                <div className="flex items-center space-x-3 mb-2">
                                  <div className={`w-3 h-3 rounded-full bg-${color}-500`}></div>
                                  <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
                                    {concept.title}
                                  </h3>
                                  {axis && (
                                    <span className={`px-2 py-1 bg-${color}-100 dark:bg-${color}-900/30 text-${color}-800 dark:text-${color}-300 rounded-md text-xs`}>
                                      {axis.name}
                                    </span>
                                  )}
                                </div>
                                <p className="text-gray-600 dark:text-gray-300 mb-2">{concept.description}</p>
                                <div className="flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400">
                                  <span>Type: {concept.typeId}</span>
                                  <span>ID: {concept.id}</span>
                                  <Link
                                    href={nodeDetailHref}
                                    onClick={(e) => e.stopPropagation()}
                                    className="text-blue-600 hover:text-blue-800 hover:underline"
                                  >
                                    View Node →
                                  </Link>
                                </div>
                              </div>
                              <div className="text-right ml-4">
                                <div className="text-2xl">
                                  {selectedConcept === concept.id ? '🔍' : '🧩'}
                                </div>
                              </div>
                            </div>
                          </div>
                        );
                      })
                    ) : (
                      <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                        No concepts found matching your criteria.
                      </div>
                    )}
                  </div>
                </Card>
              )}

              {/* Relationships View */}
              {viewMode === 'relationships' && (
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
                  <div className="p-6 border-b border-gray-200">
                    <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                      🔗 Axis Relationships
                    </h2>
                  </div>
                  <div className="p-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      {ontologyAxes.map((axis, index) => (
                        <div key={axis.id} className="text-center">
                          <div className={`w-16 h-16 rounded-full bg-${getAxisColor(axis.id)}-500 text-white flex items-center justify-center text-lg font-bold mx-auto mb-2`}>
                            {axis.name.charAt(0)}
                          </div>
                          <h3 className="font-medium text-gray-900">{axis.name}</h3>
                          <p className="text-sm text-gray-500">{axis.conceptCount} concepts</p>
                        </div>
                      ))}
                    </div>
                    
                    <div className="mt-8 text-center text-gray-500">
                      <p>Relationship mapping visualization coming soon...</p>
                      <p className="text-sm mt-2">
                        This will show how concepts flow between different U-CORE axes
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Sidebar */}
            <div className="space-y-6">
              {/* Selected Axis Details */}
              {selectedAxis && (
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                    🌟 {ontologyAxes.find(a => a.id === selectedAxis)?.name} Axis
                  </h3>
                  {(() => {
                    const axis = ontologyAxes.find(a => a.id === selectedAxis);
                    const stats = axis ? getAxisStats(axis) : null;
                    return axis && stats ? (
                      <div className="space-y-3">
                        <p className="text-gray-600 text-sm">{axis.description}</p>
                        <div className="space-y-2">
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-300">Concepts</span>
                            <span className="font-medium">{stats.conceptCount}</span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-300">Avg Score</span>
                            <span className="font-medium">{stats.avgScore}</span>
                          </div>
                        </div>
                        <div>
                          <p className="text-sm text-gray-600 mb-2">Keywords:</p>
                          <div className="flex flex-wrap gap-1">
                            {axis.keywords.map((keyword, idx) => (
                              <span
                                key={idx}
                                className={`px-2 py-1 bg-${getAxisColor(axis.id)}-100 text-${getAxisColor(axis.id)}-800 rounded-md text-xs`}
                              >
                                {keyword}
                              </span>
                            ))}
                          </div>
                        </div>
                      </div>
                    ) : null;
                  })()}
                </div>
              )}

              {/* Ontology Stats */}
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">📊 Ontology Stats</h3>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-300">Total Axes</span>
                    <span className="font-medium">{ontologyAxes.length}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-300">Total Concepts</span>
                    <span className="font-medium">{concepts.length}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600 dark:text-gray-300">Filtered</span>
                    <span className="font-medium">{filteredConcepts.length}</span>
                  </div>
                </div>
              </div>

              {/* Quick Actions */}
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">⚡ Quick Actions</h3>
                <div className="space-y-3">
                  <button
                    onClick={() => {
                      setSelectedAxis('');
                      setSelectedConcept('');
                      setSearchQuery('');
                      setViewMode('axes');
                    }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                  >
                    🌟 View All Axes
                  </button>
                  <button
                    onClick={() => {
                      setViewMode('concepts');
                      setSelectedAxis('consciousness');
                    }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                  >
                    🧘 Consciousness Concepts
                  </button>
                  <button
                    onClick={() => {
                      setViewMode('concepts');
                      setSelectedAxis('innovation');
                    }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                  >
                    💡 Innovation Hub
                  </button>
                  <button
                    onClick={() => {
                      setViewMode('relationships');
                    }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors"
                  >
                    🔗 Explore Relations
                  </button>
                </div>
              </div>

              {/* U-CORE Guide */}
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">🧠 U-CORE Guide</h3>
                <div className="text-sm text-gray-600 dark:text-gray-300 space-y-2">
                  <p>
                    <strong>U-CORE</strong> is the Universal Consciousness Ontology for Reality Enhancement.
                  </p>
                  <p>
                    The 7 axes represent fundamental dimensions of conscious experience and reality creation.
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-3">
                    💡 Tip: Click on axes or concepts to explore their relationships and discover new connections!
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
