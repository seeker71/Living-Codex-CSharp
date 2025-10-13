'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { createConcept, CONCEPT_DOMAINS, COMPLEXITY_LEVELS } from '@/lib/concepts-api';

interface ConceptCreationRequest {
  name: string;
  description: string;
  domain: string;
  complexity: number;
  tags: string[];
}

interface ConceptImageRequest {
  title: string;
  description: string;
  conceptType: string;
  style: string;
  mood: string;
  colors: string[];
  elements: string[];
  metadata: Record<string, any>;
}

interface AIAssistanceRequest {
  prompt: string;
  context: string;
  task: string;
}

interface ExtractedConcept {
  concept: string;
  score: number;
  description: string;
  category: string;
  confidence: number;
}

export default function CreatePage() {
  const { user } = useAuth();
  const router = useRouter();
  const trackInteraction = useTrackInteraction();
  
  // Form state
  const [conceptName, setConceptName] = useState('');
  const [conceptDescription, setConceptDescription] = useState('');
  const [conceptDomain, setConceptDomain] = useState('consciousness');
  const [conceptComplexity, setConceptComplexity] = useState(5);
  const [conceptTags, setConceptTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState('');
  
  // AI Assistance state
  const [aiPrompt, setAiPrompt] = useState('');
  const [aiSuggestions, setAiSuggestions] = useState<string[]>([]);
  const [extractedConcepts, setExtractedConcepts] = useState<ExtractedConcept[]>([]);
  const [aiLoading, setAiLoading] = useState(false);
  
  // Image generation state
  const [imageStyle, setImageStyle] = useState('abstract');
  const [imageMood, setImageMood] = useState('inspiring');
  const [imageColors, setImageColors] = useState<string[]>(['blue', 'gold']);
  const [imageElements, setImageElements] = useState<string[]>(['sacred geometry', 'light']);
  
  // UI state
  const [activeTab, setActiveTab] = useState('concept');
  const [loading, setLoading] = useState(false);
  const [creationResult, setCreationResult] = useState<any>(null);
  
  // Draft and template state
  const [savedDrafts, setSavedDrafts] = useState<any[]>([]);
  const [currentDraftId, setCurrentDraftId] = useState<string | null>(null);
  const [showDrafts, setShowDrafts] = useState(false);
  const [templates, setTemplates] = useState<any[]>([]);
  const [showTemplates, setShowTemplates] = useState(false);
  
  // Version history state
  const [versionHistory, setVersionHistory] = useState<any[]>([]);
  const [showVersions, setShowVersions] = useState(false);
  
  // Relationship editor state
  const [relationships, setRelationships] = useState<any[]>([]);
  const [showRelationships, setShowRelationships] = useState(false);
  const [searchNodes, setSearchNodes] = useState('');
  const [foundNodes, setFoundNodes] = useState<any[]>([]);
  
  // Import state
  const [showImport, setShowImport] = useState(false);
  const [importUrl, setImportUrl] = useState('');
  const [importLoading, setImportLoading] = useState(false);

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('create-page', 'page-visit', { description: 'User visited concept creation page' });
      loadDrafts();
      loadTemplates();
    }
  }, [user?.id, trackInteraction]);

  // Auto-save draft every 30 seconds
  useEffect(() => {
    const interval = setInterval(() => {
      if (conceptName || conceptDescription) {
        saveDraft(true); // Auto-save
      }
    }, 30000);
    return () => clearInterval(interval);
  }, [conceptName, conceptDescription, conceptDomain, conceptComplexity, conceptTags]);

  const loadDrafts = () => {
    const drafts = localStorage.getItem('concept-drafts');
    if (drafts) {
      setSavedDrafts(JSON.parse(drafts));
    }
  };

  const saveDraft = (auto = false) => {
    const draft = {
      id: currentDraftId || `draft-${Date.now()}`,
      name: conceptName,
      description: conceptDescription,
      domain: conceptDomain,
      complexity: conceptComplexity,
      tags: conceptTags,
      timestamp: new Date().toISOString(),
      auto
    };

    const existingDrafts = JSON.parse(localStorage.getItem('concept-drafts') || '[]');
    const draftIndex = existingDrafts.findIndex((d: any) => d.id === draft.id);
    
    if (draftIndex >= 0) {
      existingDrafts[draftIndex] = draft;
    } else {
      existingDrafts.push(draft);
      setCurrentDraftId(draft.id);
    }

    localStorage.setItem('concept-drafts', JSON.stringify(existingDrafts));
    setSavedDrafts(existingDrafts);
    
    if (!auto) {
      setCreationResult({ success: true, message: 'Draft saved successfully!' });
      setTimeout(() => setCreationResult(null), 2000);
    }
  };

  const loadDraft = (draft: any) => {
    setConceptName(draft.name);
    setConceptDescription(draft.description);
    setConceptDomain(draft.domain);
    setConceptComplexity(draft.complexity);
    setConceptTags(draft.tags);
    setCurrentDraftId(draft.id);
    setShowDrafts(false);
  };

  const deleteDraft = (draftId: string) => {
    const updatedDrafts = savedDrafts.filter(d => d.id !== draftId);
    localStorage.setItem('concept-drafts', JSON.stringify(updatedDrafts));
    setSavedDrafts(updatedDrafts);
  };

  const loadTemplates = async () => {
    try {
      // TODO: Connect to actual template API endpoint
      const builtInTemplates = [
        {
          id: 'consciousness-bridge',
          name: 'Consciousness Bridge',
          description: 'A concept connecting different states of awareness',
          domain: 'consciousness',
          complexity: 7,
          tags: ['awareness', 'bridge', 'states'],
          category: 'Consciousness'
        },
        {
          id: 'unity-fractal',
          name: 'Unity Fractal',
          description: 'Self-similar patterns that represent universal unity',
          domain: 'unity',
          complexity: 8,
          tags: ['fractal', 'patterns', 'universal'],
          category: 'Unity'
        },
        {
          id: 'abundance-flow',
          name: 'Abundance Flow',
          description: 'Dynamic system for manifesting abundance in all forms',
          domain: 'abundance',
          complexity: 6,
          tags: ['flow', 'manifestation', 'dynamic'],
          category: 'Abundance'
        },
        {
          id: 'resonance-field',
          name: 'Resonance Field',
          description: 'Harmonic field that amplifies coherent vibrations',
          domain: 'energy',
          complexity: 9,
          tags: ['resonance', 'harmony', 'field'],
          category: 'Energy'
        },
        {
          id: 'wisdom-spiral',
          name: 'Wisdom Spiral',
          description: 'Evolutionary pattern of expanding understanding',
          domain: 'wisdom',
          complexity: 7,
          tags: ['evolution', 'spiral', 'growth'],
          category: 'Wisdom'
        }
      ];
      setTemplates(builtInTemplates);
    } catch (error) {
      console.error('Error loading templates:', error);
    }
  };

  const applyTemplate = (template: any) => {
    setConceptName(template.name);
    setConceptDescription(template.description);
    setConceptDomain(template.domain);
    setConceptComplexity(template.complexity);
    setConceptTags(template.tags);
    setShowTemplates(false);
    setActiveTab('concept');
  };

  const saveVersion = () => {
    const version = {
      id: `version-${Date.now()}`,
      name: conceptName,
      description: conceptDescription,
      domain: conceptDomain,
      complexity: conceptComplexity,
      tags: conceptTags,
      timestamp: new Date().toISOString()
    };

    setVersionHistory([version, ...versionHistory]);
  };

  const restoreVersion = (version: any) => {
    setConceptName(version.name);
    setConceptDescription(version.description);
    setConceptDomain(version.domain);
    setConceptComplexity(version.complexity);
    setConceptTags(version.tags);
    setShowVersions(false);
  };

  const searchNodesForRelationship = async () => {
    if (!searchNodes.trim()) return;

    try {
      const response = await fetch(`http://localhost:5002/nodes/search?query=${encodeURIComponent(searchNodes)}`);
      if (response.ok) {
        const data = await response.json();
        setFoundNodes(data.nodes || []);
      }
    } catch (error) {
      console.error('Error searching nodes:', error);
    }
  };

  const addRelationship = (node: any, relationshipType: string) => {
    const relationship = {
      id: node.id,
      title: node.title || node.id,
      type: relationshipType,
      timestamp: new Date().toISOString()
    };

    if (!relationships.find(r => r.id === node.id)) {
      setRelationships([...relationships, relationship]);
    }
  };

  const removeRelationship = (relationshipId: string) => {
    setRelationships(relationships.filter(r => r.id !== relationshipId));
  };

  const importFromUrl = async () => {
    if (!importUrl.trim()) return;

    setImportLoading(true);
    try {
      // TODO: Connect to actual import API endpoint
      const response = await fetch('http://localhost:5002/import/url', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: importUrl })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.concept) {
          setConceptName(data.concept.title || data.concept.name);
          setConceptDescription(data.concept.description || data.concept.content);
          setConceptDomain(data.concept.domain || 'consciousness');
          setConceptTags(data.concept.tags || []);
          setShowImport(false);
          setCreationResult({ success: true, message: 'Content imported successfully!' });
        }
      }
    } catch (error) {
      console.error('Error importing:', error);
      setCreationResult({ success: false, error: 'Failed to import content' });
    } finally {
      setImportLoading(false);
    }
  };

  const addTag = () => {
    if (tagInput.trim() && !conceptTags.includes(tagInput.trim())) {
      setConceptTags([...conceptTags, tagInput.trim()]);
      setTagInput('');
    }
  };

  const removeTag = (tag: string) => {
    setConceptTags(conceptTags.filter(t => t !== tag));
  };

  const getAIAssistance = async () => {
    if (!aiPrompt.trim()) return;
    
    setAiLoading(true);
    try {
      // Use AI module for concept assistance
      const response = await fetch('http://localhost:5002/ai/extract-concepts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: conceptName || 'Untitled Concept',
          content: aiPrompt,
          categories: [conceptDomain],
          source: 'concept-creation',
          url: ''
        })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.concepts) {
          setExtractedConcepts(data.concepts);
          
          // Generate suggestions based on extracted concepts
          const suggestions = data.concepts
            .filter((c: ExtractedConcept) => c.confidence > 0.7)
            .map((c: ExtractedConcept) => c.description)
            .slice(0, 5);
          setAiSuggestions(suggestions);
        }
        
        // Track AI assistance usage
        if (user?.id) {
          trackInteraction('ai-assistance', 'concept-extraction', {
            description: 'User requested AI assistance for concept creation',
            prompt: aiPrompt,
            resultCount: data.concepts?.length || 0
          });
        }
      }
    } catch (error) {
      console.error('Error getting AI assistance:', error);
    } finally {
      setAiLoading(false);
    }
  };

  const applySuggestion = (suggestion: string) => {
    if (!conceptDescription.includes(suggestion)) {
      setConceptDescription(prev => prev + (prev ? ' ' : '') + suggestion);
    }
  };

  const createConceptHandler = async () => {
    if (!conceptName.trim() || !conceptDescription.trim()) {
      setCreationResult({ 
        success: false, 
        error: 'Please provide both name and description for the concept' 
      });
      return;
    }

    setLoading(true);
    setCreationResult(null);
    
    try {
      const response = await createConcept({
        name: conceptName,
        description: conceptDescription,
        domain: conceptDomain,
        complexity: conceptComplexity,
        tags: conceptTags
      });

      if (response.success && response.conceptId) {
        setCreationResult({ 
          success: true, 
          conceptId: response.conceptId,
          message: response.message
        });
        
        // Track concept creation
        if (user?.id) {
          trackInteraction(response.conceptId, 'create', {
            description: `User created concept: ${conceptName}`,
            domain: conceptDomain,
            complexity: conceptComplexity,
            tagCount: conceptTags.length
          });
        }
        
        // Navigate to created concept after brief delay
        setTimeout(() => {
          router.push(`/node/${encodeURIComponent(response.conceptId!)}`);
        }, 2000);
        
      } else {
        setCreationResult({ 
          success: false, 
          error: response.message || 'Failed to create concept' 
        });
      }
    } catch (error) {
      console.error('Error creating concept:', error);
      setCreationResult({ 
        success: false, 
        error: error instanceof Error ? error.message : 'An unexpected error occurred' 
      });
    } finally {
      setLoading(false);
    }
  };

  const createConceptImage = async () => {
    if (!conceptName.trim()) {
      alert('Please provide a concept name first');
      return;
    }

    setLoading(true);
    try {
      const imageRequest: ConceptImageRequest = {
        title: conceptName,
        description: conceptDescription,
        conceptType: conceptDomain,
        style: imageStyle,
        mood: imageMood,
        colors: imageColors,
        elements: imageElements,
        metadata: {
          complexity: conceptComplexity,
          tags: conceptTags,
          creator: user?.id
        }
      };

      const response = await fetch('http://localhost:5002/image/concept/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(imageRequest)
      });

      if (response.ok) {
        const result = await response.json();
        setCreationResult(result);
        
        // Track image concept creation
        if (user?.id) {
          trackInteraction('concept-image', 'create', {
            description: `User created concept image: ${conceptName}`,
            style: imageStyle,
            mood: imageMood,
            elements: imageElements.join(', ')
          });
        }
      }
    } catch (error) {
      console.error('Error creating concept image:', error);
    } finally {
      setLoading(false);
    }
  };

  const domains = [
    'consciousness', 'transformation', 'unity', 'love', 'wisdom', 
    'energy', 'healing', 'abundance', 'sacred', 'fractal', 'technology', 'science'
  ];

  const styles = ['abstract', 'geometric', 'organic', 'mystical', 'futuristic', 'minimalist'];
  const moods = ['inspiring', 'peaceful', 'energetic', 'mystical', 'joyful', 'contemplative'];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">✨ Concept Creation</h1>
              <p className="text-gray-600 dark:text-gray-300">
                Create new concepts with AI assistance and generate visual representations
              </p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowDrafts(true)}
                className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 text-sm"
              >
                📝 Drafts ({savedDrafts.length})
              </button>
              <button
                onClick={() => setShowTemplates(true)}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 text-sm"
              >
                📋 Templates
              </button>
              <button
                onClick={() => saveDraft(false)}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm"
              >
                💾 Save Draft
              </button>
              <button
                onClick={() => setShowImport(true)}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm"
              >
                📥 Import
              </button>
            </div>
          </div>
        </div>

        {/* Success/Error Message */}
        {creationResult && (
          <div className={`${creationResult.success ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'} border rounded-lg p-4 mb-6`}>
            <div className="flex items-center">
              <div className={`${creationResult.success ? 'text-green-600' : 'text-red-600'} text-xl mr-3`}>
                {creationResult.success ? '✅' : '❌'}
              </div>
              <div>
                <h3 className={`${creationResult.success ? 'text-green-800' : 'text-red-800'} font-medium`}>
                  {creationResult.message || creationResult.error || 'Operation completed'}
                </h3>
                {creationResult.conceptId && (
                  <p className="text-green-700 text-sm mt-1">
                    Redirecting to concept... ID: {creationResult.conceptId}
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div className="bg-white rounded-lg border border-gray-200 mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex space-x-8 px-6">
              {[
                { id: 'concept', label: '🧠 Concept', icon: '🧠' },
                { id: 'ai-assist', label: '🤖 AI Assistant', icon: '🤖' },
                { id: 'image', label: '🎨 Visual Creation', icon: '🎨' }
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`py-4 px-2 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  {tab.icon} {tab.label}
                </button>
              ))}
            </nav>
          </div>

          <div className="p-6">
            {/* Concept Creation Tab */}
            {activeTab === 'concept' && (
              <div className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Basic Information */}
                  <div className="space-y-4">
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Basic Information</h3>
                    
                    <div>
                      <label htmlFor="concept-name" className="block text-sm font-medium text-gray-700 mb-2">
                        Concept Name *
                      </label>
                      <input
                        id="concept-name"
                        type="text"
                        value={conceptName}
                        onChange={(e) => setConceptName(e.target.value)}
                        placeholder="e.g., Quantum Consciousness Bridge"
                        className="input-standard"
                      />
                    </div>

                    <div>
                      <label htmlFor="concept-description" className="block text-sm font-medium text-gray-700 mb-2">
                        Description *
                      </label>
                      <textarea
                        id="concept-description"
                        value={conceptDescription}
                        onChange={(e) => setConceptDescription(e.target.value)}
                        placeholder="Describe your concept in detail..."
                        rows={4}
                        className="input-standard"
                      />
                    </div>

                    <div>
                      <label htmlFor="concept-domain" className="block text-sm font-medium text-gray-700 mb-2">
                        Domain
                      </label>
                      <select
                        id="concept-domain"
                        value={conceptDomain}
                        onChange={(e) => setConceptDomain(e.target.value)}
                        className="input-standard"
                      >
                        {domains.map(domain => (
                          <option key={domain} value={domain}>
                            {domain.charAt(0).toUpperCase() + domain.slice(1)}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  {/* Advanced Properties */}
                  <div className="space-y-4">
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Advanced Properties</h3>
                    
                    <div>
                      <label htmlFor="concept-complexity" className="block text-sm font-medium text-gray-700 mb-2">
                        Complexity Level: {conceptComplexity}
                      </label>
                      <input
                        id="concept-complexity"
                        type="range"
                        min="1"
                        max="10"
                        value={conceptComplexity}
                        onChange={(e) => setConceptComplexity(parseInt(e.target.value))}
                        className="w-full"
                      />
                      <div className="flex justify-between text-xs text-gray-500 mt-1">
                        <span>Simple</span>
                        <span>Complex</span>
                      </div>
                    </div>

                    <div>
                      <label htmlFor="tag-input" className="block text-sm font-medium text-gray-700 mb-2">
                        Tags
                      </label>
                      <div className="flex space-x-2 mb-2">
                        <input
                          id="tag-input"
                          type="text"
                          value={tagInput}
                          onChange={(e) => setTagInput(e.target.value)}
                          placeholder="Add a tag..."
                          className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                          onKeyPress={(e) => e.key === 'Enter' && addTag()}
                        />
                        <button
                          onClick={addTag}
                          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                        >
                          Add
                        </button>
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {conceptTags.map((tag, index) => (
                          <span
                            key={index}
                            className="px-3 py-1 bg-blue-100 text-blue-800 rounded-md text-sm flex items-center"
                          >
                            {tag}
                            <button
                              onClick={() => removeTag(tag)}
                              className="ml-2 text-blue-600 hover:text-blue-800"
                            >
                              ×
                            </button>
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between pt-6">
                  <div className="flex gap-2">
                    <button
                      onClick={() => setShowRelationships(true)}
                      className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm"
                    >
                      🔗 Relationships ({relationships.length})
                    </button>
                    <button
                      onClick={saveVersion}
                      className="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 text-sm"
                    >
                      📚 Save Version
                    </button>
                    {versionHistory.length > 0 && (
                      <button
                        onClick={() => setShowVersions(true)}
                        className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 text-sm"
                      >
                        📜 History ({versionHistory.length})
                      </button>
                    )}
                  </div>
                  <button
                    onClick={createConceptHandler}
                    disabled={loading || !conceptName.trim() || !conceptDescription.trim()}
                    className="px-8 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {loading ? '✨ Creating Concept...' : '✨ Create Concept'}
                  </button>
                </div>
              </div>
            )}

            {/* AI Assistant Tab */}
            {activeTab === 'ai-assist' && (
              <div className="space-y-6">
                <div className="text-center mb-6">
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">🤖 AI Concept Assistant</h3>
                  <p className="text-gray-600 dark:text-gray-300">
                    Describe your idea and get AI-powered concept suggestions and improvements
                  </p>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Describe Your Idea
                  </label>
                  <textarea
                    value={aiPrompt}
                    onChange={(e) => setAiPrompt(e.target.value)}
                    placeholder="I want to create a concept about..."
                    rows={4}
                    className="input-standard"
                  />
                </div>

                <div className="flex justify-center">
                  <button
                    onClick={getAIAssistance}
                    disabled={aiLoading || !aiPrompt.trim()}
                    className="px-6 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-purple-500 disabled:opacity-50"
                  >
                    {aiLoading ? '🤖 Analyzing...' : '🤖 Get AI Assistance'}
                  </button>
                </div>

                {/* AI Suggestions */}
                {aiSuggestions.length > 0 && (
                  <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
                    <h4 className="font-medium text-purple-900 mb-3">💡 AI Suggestions</h4>
                    <div className="space-y-2">
                      {aiSuggestions.map((suggestion, index) => (
                        <div key={index} className="flex items-center justify-between bg-white p-3 rounded-md">
                          <span className="text-gray-700 dark:text-gray-200">{suggestion}</span>
                          <button
                            onClick={() => applySuggestion(suggestion)}
                            className="px-3 py-1 bg-purple-600 text-white text-sm rounded-md hover:bg-purple-700"
                          >
                            Apply
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Extracted Concepts */}
                {extractedConcepts.length > 0 && (
                  <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                    <h4 className="font-medium text-blue-900 mb-3">🧠 Extracted Concepts</h4>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {extractedConcepts.map((concept, index) => (
                        <div key={index} className="bg-white p-3 rounded-md">
                          <div className="flex items-center justify-between mb-1">
                            <span className="font-medium text-gray-900">{concept.concept}</span>
                            <span className="text-sm text-blue-600">
                              {Math.round(concept.confidence * 100)}%
                            </span>
                          </div>
                          <p className="text-sm text-gray-600 mb-2">{concept.description}</p>
                          <div className="flex items-center justify-between">
                            <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
                              {concept.category}
                            </span>
                            <button
                              onClick={() => {
                                setConceptName(concept.concept);
                                setConceptDescription(concept.description);
                                setConceptDomain(concept.category);
                                setActiveTab('concept');
                              }}
                              className="text-xs text-blue-600 hover:text-blue-800"
                            >
                              Use This →
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Visual Creation Tab */}
            {activeTab === 'image' && (
              <div className="space-y-6">
                <div className="text-center mb-6">
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">🎨 Visual Concept Creation</h3>
                  <p className="text-gray-600 dark:text-gray-300">
                    Generate visual representations of your concept with AI-powered image creation
                  </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Style Configuration */}
                  <div className="space-y-4">
                    <h4 className="font-medium text-gray-900">Visual Style</h4>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Art Style
                      </label>
                      <select
                        value={imageStyle}
                        onChange={(e) => setImageStyle(e.target.value)}
                        className="input-standard"
                      >
                        {styles.map(style => (
                          <option key={style} value={style}>
                            {style.charAt(0).toUpperCase() + style.slice(1)}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Mood
                      </label>
                      <select
                        value={imageMood}
                        onChange={(e) => setImageMood(e.target.value)}
                        className="input-standard"
                      >
                        {moods.map(mood => (
                          <option key={mood} value={mood}>
                            {mood.charAt(0).toUpperCase() + mood.slice(1)}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  {/* Elements Configuration */}
                  <div className="space-y-4">
                    <h4 className="font-medium text-gray-900">Visual Elements</h4>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Colors (comma-separated)
                      </label>
                      <input
                        type="text"
                        value={imageColors.join(', ')}
                        onChange={(e) => setImageColors(e.target.value.split(',').map(c => c.trim()))}
                        placeholder="blue, gold, white"
                        className="input-standard"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Elements (comma-separated)
                      </label>
                      <input
                        type="text"
                        value={imageElements.join(', ')}
                        onChange={(e) => setImageElements(e.target.value.split(',').map(e => e.trim()))}
                        placeholder="sacred geometry, light, fractals"
                        className="input-standard"
                      />
                    </div>
                  </div>
                </div>

                <div className="flex justify-center pt-6">
                  <button
                    onClick={createConceptImage}
                    disabled={loading || !conceptName.trim()}
                    className="px-8 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-purple-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {loading ? '🎨 Creating Visual...' : '🎨 Create Visual Concept'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Drafts Modal */}
        {showDrafts && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">📝 Saved Drafts</h3>
                <button onClick={() => setShowDrafts(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6 space-y-3">
                {savedDrafts.length === 0 ? (
                  <p className="text-gray-500 text-center py-8">No saved drafts yet</p>
                ) : (
                  savedDrafts.map((draft) => (
                    <div key={draft.id} className="border border-gray-200 dark:border-gray-600 rounded-lg p-4 hover:bg-gray-50 dark:hover:bg-gray-700">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <h4 className="font-medium text-gray-900 dark:text-gray-100">{draft.name || 'Untitled'}</h4>
                          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">{draft.description?.substring(0, 100)}...</p>
                          <p className="text-xs text-gray-500 mt-2">
                            {new Date(draft.timestamp).toLocaleDateString()} {draft.auto && '(auto-saved)'}
                          </p>
                        </div>
                        <div className="flex gap-2 ml-4">
                          <button
                            onClick={() => loadDraft(draft)}
                            className="px-3 py-1 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700"
                          >
                            Load
                          </button>
                          <button
                            onClick={() => deleteDraft(draft.id)}
                            className="px-3 py-1 bg-red-600 text-white text-sm rounded-md hover:bg-red-700"
                          >
                            Delete
                          </button>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}

        {/* Templates Modal */}
        {showTemplates && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-4xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">📋 Concept Templates</h3>
                <button onClick={() => setShowTemplates(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {templates.map((template) => (
                    <div key={template.id} className="border border-gray-200 dark:border-gray-600 rounded-lg p-4 hover:bg-gray-50 dark:hover:bg-gray-700">
                      <div className="flex items-start justify-between mb-2">
                        <h4 className="font-medium text-gray-900 dark:text-gray-100">{template.name}</h4>
                        <span className="px-2 py-1 bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-400 text-xs rounded">
                          {template.category}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{template.description}</p>
                      <div className="flex flex-wrap gap-1 mb-3">
                        {template.tags.map((tag: string, idx: number) => (
                          <span key={idx} className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-xs rounded">
                            {tag}
                          </span>
                        ))}
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-gray-500">Complexity: {template.complexity}/10</span>
                        <button
                          onClick={() => applyTemplate(template)}
                          className="px-3 py-1 bg-purple-600 text-white text-sm rounded-md hover:bg-purple-700"
                        >
                          Use Template
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Version History Modal */}
        {showVersions && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-2xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">📜 Version History</h3>
                <button onClick={() => setShowVersions(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6 space-y-3">
                {versionHistory.map((version, index) => (
                  <div key={version.id} className="border border-gray-200 dark:border-gray-600 rounded-lg p-4 hover:bg-gray-50 dark:hover:bg-gray-700">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="font-medium text-gray-900 dark:text-gray-100">{version.name}</h4>
                          {index === 0 && (
                            <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400 text-xs rounded">
                              Latest
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-600 dark:text-gray-400">{version.description?.substring(0, 80)}...</p>
                        <p className="text-xs text-gray-500 mt-1">
                          {new Date(version.timestamp).toLocaleString()}
                        </p>
                      </div>
                      <button
                        onClick={() => restoreVersion(version)}
                        className="px-3 py-1 bg-orange-600 text-white text-sm rounded-md hover:bg-orange-700 ml-4"
                      >
                        Restore
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Relationships Modal */}
        {showRelationships && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-3xl w-full max-h-[80vh] overflow-y-auto">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">🔗 Concept Relationships</h3>
                <button onClick={() => setShowRelationships(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6 space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Search for concepts to connect
                  </label>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={searchNodes}
                      onChange={(e) => setSearchNodes(e.target.value)}
                      placeholder="Search concepts..."
                      className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                    />
                    <button
                      onClick={searchNodesForRelationship}
                      className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
                    >
                      Search
                    </button>
                  </div>
                </div>

                {foundNodes.length > 0 && (
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">Search Results</h4>
                    <div className="space-y-2 max-h-48 overflow-y-auto">
                      {foundNodes.map((node) => (
                        <div key={node.id} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 rounded-lg">
                          <span className="text-gray-900 dark:text-gray-100">{node.title || node.id}</span>
                          <select
                            onChange={(e) => addRelationship(node, e.target.value)}
                            className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-sm"
                            defaultValue=""
                          >
                            <option value="" disabled>Add as...</option>
                            <option value="related">Related</option>
                            <option value="influences">Influences</option>
                            <option value="depends-on">Depends On</option>
                            <option value="extends">Extends</option>
                          </select>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {relationships.length > 0 && (
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100 mb-2">Connected Concepts</h4>
                    <div className="space-y-2">
                      {relationships.map((rel) => (
                        <div key={rel.id} className="flex items-center justify-between p-3 bg-indigo-50 dark:bg-indigo-900/20 rounded-lg">
                          <div>
                            <span className="text-gray-900 dark:text-gray-100">{rel.title}</span>
                            <span className="text-sm text-indigo-600 dark:text-indigo-400 ml-2">({rel.type})</span>
                          </div>
                          <button
                            onClick={() => removeRelationship(rel.id)}
                            className="text-red-600 hover:text-red-700 text-sm"
                          >
                            Remove
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Import Modal */}
        {showImport && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white dark:bg-gray-800 rounded-lg max-w-lg w-full">
              <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">📥 Import Content</h3>
                <button onClick={() => setShowImport(false)} className="text-gray-500 hover:text-gray-700">✕</button>
              </div>
              <div className="p-6 space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Import from URL
                  </label>
                  <input
                    type="text"
                    value={importUrl}
                    onChange={(e) => setImportUrl(e.target.value)}
                    placeholder="https://example.com/article"
                    className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                  />
                </div>
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => setShowImport(false)}
                    className="px-4 py-2 bg-gray-300 text-gray-700 rounded-lg hover:bg-gray-400"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={importFromUrl}
                    disabled={importLoading || !importUrl.trim()}
                    className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                  >
                    {importLoading ? 'Importing...' : 'Import'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
