'use client';

import { useState, useEffect } from 'react';
import { Heart, Share2, Download, Eye, Sparkles, Palette } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';
import { UXPrimitives } from '@/components/primitives/UXPrimitives';

interface GalleryItem {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  thumbnailUrl: string;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt: string;
  resonance: number;
  axes: string[];
  tags: string[];
  mediaType: 'image' | 'video' | 'audio' | 'interactive';
  dimensions?: {
    width: number;
    height: number;
  };
  aiGenerated: boolean;
  prompt?: string;
}

interface GalleryLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

export function GalleryLens({ controls = {}, userId, className = '' }: GalleryLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  const [items, setItems] = useState<GalleryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedItem, setSelectedItem] = useState<GalleryItem | null>(null);
  const [viewMode, setViewMode] = useState<'masonry' | 'grid'>('masonry');
  const [filterBy, setFilterBy] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('resonance');


  useEffect(() => {
    loadGalleryItems();
  }, []);

  const loadGalleryItems = async () => {
    setLoading(true);
    try {
      const response = await fetch(buildApiUrl('/gallery/list'));
      if (response.ok) {
        const data = await response.json();
        if (data.items && data.items.length > 0) {
          setItems(data.items);
        } else {
          setItems([]);
        }
      } else {
        setItems([]);
      }
    } catch (error) {
      console.error('Error loading gallery items:', error);
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  const formatTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) return 'Just now';
    if (diffInHours < 24) return `${diffInHours}h ago`;
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays}d ago`;
  };

  const getAxisColor = (axis: string) => {
    const colors: Record<string, string> = {
      abundance: 'bg-green-100 text-green-800',
      unity: 'bg-blue-100 text-blue-800',
      resonance: 'bg-purple-100 text-purple-800',
      innovation: 'bg-orange-100 text-orange-800',
      science: 'bg-cyan-100 text-cyan-800',
      consciousness: 'bg-indigo-100 text-indigo-800',
      impact: 'bg-red-100 text-red-800',
    };
    return colors[axis] || 'bg-gray-100 text-gray-800';
  };

  const getMediaTypeIcon = (type: string) => {
    const icons: Record<string, string> = {
      image: '🖼️',
      video: '🎥',
      audio: '🎵',
      interactive: '🎮'
    };
    return icons[type] || '📄';
  };

  // Filter and sort items
  const filteredItems = items
    .filter(item => filterBy === 'all' || item.axes.includes(filterBy))
    .sort((a, b) => {
      switch (sortBy) {
        case 'resonance':
          return b.resonance - a.resonance;
        case 'recent':
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        case 'popular':
          // Mock popularity score
          return (b.resonance * 0.7 + Math.random() * 0.3) - (a.resonance * 0.7 + Math.random() * 0.3);
        default:
          return 0;
      }
    });

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 flex items-center">
            🖼️ Gallery
          </h2>
          <p className="text-gray-600 mt-1">
            Visual expressions of consciousness and creativity
          </p>
        </div>
        
        {/* View Controls */}
        <div className="flex items-center space-x-4">
          <select
            value={filterBy}
            onChange={(e) => setFilterBy(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">All Axes</option>
            <option value="abundance">Abundance</option>
            <option value="unity">Unity</option>
            <option value="resonance">Resonance</option>
            <option value="innovation">Innovation</option>
            <option value="science">Science</option>
            <option value="consciousness">Consciousness</option>
            <option value="impact">Impact</option>
          </select>
          
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="resonance">By Resonance</option>
            <option value="recent">Most Recent</option>
            <option value="popular">Most Popular</option>
          </select>

          <div className="flex border border-gray-300 rounded-md">
            <button
              onClick={() => setViewMode('masonry')}
              className={`px-3 py-2 ${viewMode === 'masonry' ? 'bg-blue-600 text-white' : 'bg-white text-gray-700'} rounded-l-md`}
            >
              Masonry
            </button>
            <button
              onClick={() => setViewMode('grid')}
              className={`px-3 py-2 ${viewMode === 'grid' ? 'bg-blue-600 text-white' : 'bg-white text-gray-700'} rounded-r-md`}
            >
              Grid
            </button>
          </div>
        </div>
      </div>

      {/* Gallery Grid */}
      <div className={
        viewMode === 'masonry' 
          ? 'columns-1 md:columns-2 lg:columns-3 xl:columns-4 gap-6 space-y-6'
          : 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6'
      }>
        {filteredItems.length === 0 ? (
          <div className="col-span-full text-center py-12 text-gray-500">
            <div className="text-6xl mb-4">🖼️</div>
            <p>No gallery items available yet.</p>
            <p className="text-sm mt-2">Be the first to share visual expressions!</p>
          </div>
        ) : (
          filteredItems.map((item) => (
          <div
            key={item.id}
            className="bg-white rounded-lg border border-gray-200 overflow-hidden hover:shadow-lg transition-all duration-300 cursor-pointer"
            onClick={() => setSelectedItem(item)}
          >
            {/* Image */}
            <div className="relative">
              <img
                src={item.thumbnailUrl}
                alt={item.title}
                className="w-full h-48 object-cover"
                loading="lazy"
              />
              
              {/* Overlay */}
              <div className="absolute inset-0 bg-black bg-opacity-0 hover:bg-opacity-20 transition-all duration-300 flex items-center justify-center">
                <div className="opacity-0 hover:opacity-100 transition-opacity duration-300 flex space-x-2">
                  <button className="p-2 bg-white rounded-full text-gray-700 hover:text-blue-600">
                    <Eye className="w-4 h-4" />
                  </button>
                  <button className="p-2 bg-white rounded-full text-gray-700 hover:text-green-600">
                    <Heart className="w-4 h-4" />
                  </button>
                  <button className="p-2 bg-white rounded-full text-gray-700 hover:text-purple-600">
                    <Share2 className="w-4 h-4" />
                  </button>
                </div>
              </div>

              {/* Media Type Badge */}
              <div className="absolute top-2 left-2 px-2 py-1 bg-black bg-opacity-70 text-white text-xs rounded">
                {getMediaTypeIcon(item.mediaType)}
              </div>

              {/* AI Generated Badge */}
              {item.aiGenerated && (
                <div className="absolute top-2 right-2 px-2 py-1 bg-gradient-to-r from-purple-600 to-blue-600 text-white text-xs rounded flex items-center space-x-1">
                  <Sparkles className="w-3 h-3" />
                  <span>AI</span>
                </div>
              )}
            </div>

            {/* Content */}
            <div className="p-4">
              <h3 className="font-semibold text-gray-900 mb-2 line-clamp-2">{item.title}</h3>
              <p className="text-sm text-gray-600 mb-3 line-clamp-2">{item.description}</p>
              
              {/* Author */}
              <div className="flex items-center space-x-2 mb-3">
                <img
                  src={item.author.avatar}
                  alt={item.author.name}
                  className="w-6 h-6 rounded-full"
                />
                <span className="text-sm text-gray-700">{item.author.name}</span>
                <span className="text-xs text-gray-500">•</span>
                <span className="text-xs text-gray-500">{formatTimeAgo(item.createdAt)}</span>
              </div>

              {/* Axes Tags */}
              <div className="flex flex-wrap gap-1 mb-3">
                {item.axes.slice(0, 2).map((axis) => (
                  <span
                    key={axis}
                    className={`px-2 py-1 text-xs font-medium rounded-full ${getAxisColor(axis)}`}
                  >
                    {axis}
                  </span>
                ))}
                {item.axes.length > 2 && (
                  <span className="px-2 py-1 text-xs text-gray-500">
                    +{item.axes.length - 2}
                  </span>
                )}
              </div>

              {/* Resonance Bar */}
              <div className="mb-3">
                <div className="flex items-center justify-between text-xs text-gray-600 mb-1">
                  <span>Resonance</span>
                  <span>{(item.resonance * 100).toFixed(0)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-1.5">
                  <div
                    className="bg-gradient-to-r from-blue-500 to-purple-600 h-1.5 rounded-full transition-all duration-300"
                    style={{ width: `${item.resonance * 100}%` }}
                  />
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-between">
                <UXPrimitives
                  contentId={item.id}
                  showWeave={true}
                  showReflect={true}
                  showInvite={false}
                  className="text-xs"
                />
                
                <div className="flex items-center space-x-2">
                  <button className="p-1 text-gray-400 hover:text-red-500 transition-colors">
                    <Heart className="w-4 h-4" />
                  </button>
                  <button className="p-1 text-gray-400 hover:text-blue-500 transition-colors">
                    <Download className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        ))
        )}
      </div>

      {/* Gallery Item Detail Modal */}
      {selectedItem && (
        <div className="fixed inset-0 bg-black bg-opacity-90 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg w-full max-w-6xl max-h-[90vh] overflow-y-auto">
            <div className="flex">
              {/* Image Section */}
              <div className="flex-1 p-6">
                <img
                  src={selectedItem.imageUrl}
                  alt={selectedItem.title}
                  className="w-full h-auto rounded-lg"
                />
              </div>

              {/* Details Section */}
              <div className="w-96 p-6 border-l border-gray-200">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900">{selectedItem.title}</h3>
                  <button
                    onClick={() => setSelectedItem(null)}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    ✕
                  </button>
                </div>

                <p className="text-gray-700 mb-4">{selectedItem.description}</p>

                {/* Author */}
                <div className="flex items-center space-x-3 mb-4">
                  <img
                    src={selectedItem.author.avatar}
                    alt={selectedItem.author.name}
                    className="w-10 h-10 rounded-full"
                  />
                  <div>
                    <div className="font-medium text-gray-900">{selectedItem.author.name}</div>
                    <div className="text-sm text-gray-500">{formatTimeAgo(selectedItem.createdAt)}</div>
                  </div>
                </div>

                {/* Axes Tags */}
                <div className="mb-4">
                  <h4 className="text-sm font-medium text-gray-700 mb-2">Axes</h4>
                  <div className="flex flex-wrap gap-2">
                    {selectedItem.axes.map((axis) => (
                      <span
                        key={axis}
                        className={`px-2 py-1 text-xs font-medium rounded-full ${getAxisColor(axis)}`}
                      >
                        {axis}
                      </span>
                    ))}
                  </div>
                </div>

                {/* AI Prompt */}
                {selectedItem.aiGenerated && selectedItem.prompt && (
                  <div className="mb-4">
                    <h4 className="text-sm font-medium text-gray-700 mb-2">AI Prompt</h4>
                    <p className="text-sm text-gray-600 bg-gray-50 p-3 rounded">{selectedItem.prompt}</p>
                  </div>
                )}

                {/* Resonance */}
                <div className="mb-4">
                  <div className="flex items-center justify-between text-sm text-gray-600 mb-2">
                    <span>Resonance</span>
                    <span>{(selectedItem.resonance * 100).toFixed(0)}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div
                      className="bg-gradient-to-r from-blue-500 to-purple-600 h-2 rounded-full"
                      style={{ width: `${selectedItem.resonance * 100}%` }}
                    />
                  </div>
                </div>

                {/* Actions */}
                <div className="space-y-3">
                  <UXPrimitives
                    contentId={selectedItem.id}
                    showWeave={true}
                    showReflect={true}
                    showInvite={true}
                  />
                  
                  <div className="flex space-x-2">
                    <button className="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors flex items-center justify-center space-x-2">
                      <Heart className="w-4 h-4" />
                      <span>Like</span>
                    </button>
                    <button className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors flex items-center justify-center space-x-2">
                      <Download className="w-4 h-4" />
                      <span>Download</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
