'use client';

import { useState } from 'react';
import { Heart, Share2, MessageCircle, Link2, Zap } from 'lucide-react';
import { useAttune, useAmplify } from '@/lib/hooks';

interface Concept {
  id: string;
  name: string;
  description: string;
  axes?: string[];
  resonance?: number;
  type?: string;
  meta?: Record<string, any>;
}

interface ConceptStreamCardProps {
  concept: Concept;
  userId?: string;
  onAction?: (action: string, conceptId: string) => void;
}

export function ConceptStreamCard({ concept, userId, onAction }: ConceptStreamCardProps) {
  const [isAttuned, setIsAttuned] = useState(false);
  const [isAmplifying, setIsAmplifying] = useState(false);
  
  const attuneMutation = useAttune();
  const amplifyMutation = useAmplify();

  const handleAttune = async () => {
    if (!userId) return;
    
    try {
      if (isAttuned) {
        // TODO: Implement unattune
        setIsAttuned(false);
      } else {
        await attuneMutation.mutateAsync({ userId, conceptId: concept.id });
        setIsAttuned(true);
      }
      onAction?.('attune', concept.id);
    } catch (error) {
      console.error('Attune error:', error);
    }
  };

  const handleAmplify = async () => {
    if (!userId) return;
    
    try {
      setIsAmplifying(true);
      await amplifyMutation.mutateAsync({ 
        userId, 
        conceptId: concept.id, 
        contribution: `Amplified concept: ${concept.name}` 
      });
      onAction?.('amplify', concept.id);
    } catch (error) {
      console.error('Amplify error:', error);
    } finally {
      setIsAmplifying(false);
    }
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

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            {concept.name}
          </h3>
          <p className="text-gray-600 text-sm leading-relaxed">
            {concept.description}
          </p>
        </div>
        
        {/* Resonance Score */}
        {concept.resonance && (
          <div className="ml-4 text-right">
            <div className="text-2xl font-bold text-purple-600">
              {Math.round(concept.resonance * 100)}%
            </div>
            <div className="text-xs text-gray-500">Resonance</div>
          </div>
        )}
      </div>

      {/* Axes Tags */}
      {concept.axes && concept.axes.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-4">
          {concept.axes.map((axis) => (
            <span
              key={axis}
              className={`px-2 py-1 rounded-full text-xs font-medium ${getAxisColor(axis)}`}
            >
              {axis}
            </span>
          ))}
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center justify-between pt-4 border-t border-gray-100">
        <div className="flex items-center space-x-4">
          <button
            onClick={handleAttune}
            disabled={attuneMutation.isPending}
            className={`flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium transition-colors ${
              isAttuned
                ? 'bg-purple-100 text-purple-700 hover:bg-purple-200'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            <Heart className={`w-4 h-4 ${isAttuned ? 'fill-current' : ''}`} />
            <span>{isAttuned ? 'Attuned' : 'Attune'}</span>
          </button>

          <button
            onClick={handleAmplify}
            disabled={amplifyMutation.isPending || isAmplifying}
            className="flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-yellow-100 text-yellow-700 hover:bg-yellow-200 transition-colors"
          >
            <Zap className="w-4 h-4" />
            <span>{isAmplifying ? 'Amplifying...' : 'Amplify'}</span>
          </button>

          <button className="flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors">
            <MessageCircle className="w-4 h-4" />
            <span>Reflect</span>
          </button>

          <button className="flex items-center space-x-1 px-3 py-1 rounded-full text-sm font-medium bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors">
            <Link2 className="w-4 h-4" />
            <span>Weave</span>
          </button>
        </div>

        {/* Type Badge */}
        {concept.type && (
          <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full">
            {concept.type}
          </span>
        )}
      </div>
    </div>
  );
}
