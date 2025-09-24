'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

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

  // Track page visit
  useEffect(() => {
    if (user?.id) {
      trackInteraction('create-page', 'page-visit', { description: 'User visited concept creation page' });
    }
  }, [user?.id, trackInteraction]);

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
      const response = await fetch(buildApiUrl('/ai/extract-concepts'), {
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

  const createConcept = async () => {
    if (!conceptName.trim() || !conceptDescription.trim()) {
      alert('Please provide both name and description for the concept');
      return;
    }

    setLoading(true);
    try {
      const conceptRequest: ConceptCreationRequest = {
        name: conceptName,
        description: conceptDescription,
        domain: conceptDomain,
        complexity: conceptComplexity,
        tags: conceptTags
      };

      const response = await fetch(buildApiUrl('/concept/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(conceptRequest)
      });

      if (response.ok) {
        const result = await response.json();
        setCreationResult(result);
        
        // Track concept creation
        if (user?.id) {
          trackInteraction(result.conceptId || 'new-concept', 'create', {
            description: `User created concept: ${conceptName}`,
            domain: conceptDomain,
            complexity: conceptComplexity,
            tagCount: conceptTags.length
          });
        }
        
        // Reset form
        setConceptName('');
        setConceptDescription('');
        setConceptTags([]);
        setAiPrompt('');
        setAiSuggestions([]);
        setExtractedConcepts([]);
      } else {
        console.error('Failed to create concept');
      }
    } catch (error) {
      console.error('Error creating concept:', error);
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

      const response = await fetch(buildApiUrl('/image/concept/create'), {
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
          <h1 className="text-3xl font-bold text-gray-900 mb-2">✨ Concept Creation</h1>
          <p className="text-gray-600 dark:text-gray-300">
            Create new concepts with AI assistance and generate visual representations
          </p>
        </div>

        {/* Success Message */}
        {creationResult && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
            <div className="flex items-center">
              <div className="text-green-600 text-xl mr-3">✅</div>
              <div>
                <h3 className="text-green-800 font-medium">
                  {creationResult.message || 'Concept created successfully!'}
                </h3>
                {creationResult.conceptId && (
                  <p className="text-green-700 text-sm mt-1">
                    Concept ID: {creationResult.conceptId}
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
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Concept Name *
                      </label>
                      <input
                        type="text"
                        value={conceptName}
                        onChange={(e) => setConceptName(e.target.value)}
                        placeholder="e.g., Quantum Consciousness Bridge"
                        className="input-standard"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Description *
                      </label>
                      <textarea
                        value={conceptDescription}
                        onChange={(e) => setConceptDescription(e.target.value)}
                        placeholder="Describe your concept in detail..."
                        rows={4}
                        className="input-standard"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Domain
                      </label>
                      <select
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
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Complexity Level: {conceptComplexity}
                      </label>
                      <input
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
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Tags
                      </label>
                      <div className="flex space-x-2 mb-2">
                        <input
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

                <div className="flex justify-center pt-6">
                  <button
                    onClick={createConcept}
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

        {/* Quick Start Templates */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">🚀 Quick Start Templates</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {[
              {
                name: 'Consciousness Bridge',
                description: 'A concept connecting different states of awareness',
                domain: 'consciousness',
                tags: ['awareness', 'bridge', 'states']
              },
              {
                name: 'Unity Fractal',
                description: 'Self-similar patterns that represent universal unity',
                domain: 'unity',
                tags: ['fractal', 'patterns', 'universal']
              },
              {
                name: 'Abundance Flow',
                description: 'Dynamic system for manifesting abundance in all forms',
                domain: 'abundance',
                tags: ['flow', 'manifestation', 'dynamic']
              }
            ].map((template, index) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50">
                <h4 className="font-medium text-gray-900 mb-2">{template.name}</h4>
                <p className="text-sm text-gray-600 mb-3">{template.description}</p>
                <div className="flex flex-wrap gap-1 mb-3">
                  {template.tags.map((tag, idx) => (
                    <span key={idx} className="px-2 py-1 bg-gray-100 text-gray-700 text-xs rounded">
                      {tag}
                    </span>
                  ))}
                </div>
                <button
                  onClick={() => {
                    setConceptName(template.name);
                    setConceptDescription(template.description);
                    setConceptDomain(template.domain);
                    setConceptTags(template.tags);
                    setActiveTab('concept');
                  }}
                  className="w-full px-3 py-2 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700"
                >
                  Use Template
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
