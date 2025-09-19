'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface GlobalControlsState {
  resonanceLevel: number;
  joyLevel: number;
  serendipityLevel: number;
  curiosityLevel: number;
}

interface ResonanceCompassData {
  currentResonance: number;
  targetResonance: number;
  resonanceDirection: string;
  harmonicFrequency: number;
}

export function GlobalControls() {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  // Global control states
  const [controls, setControls] = useState<GlobalControlsState>({
    resonanceLevel: 50,
    joyLevel: 70,
    serendipityLevel: 30,
    curiosityLevel: 80
  });
  
  // Resonance compass data
  const [resonanceData, setResonanceData] = useState<ResonanceCompassData>({
    currentResonance: 0.65,
    targetResonance: 0.85,
    resonanceDirection: 'ascending',
    harmonicFrequency: 432
  });
  
  const [isExpanded, setIsExpanded] = useState(false);
  const [activeControl, setActiveControl] = useState<string>('');

  // Load user's control preferences
  useEffect(() => {
    if (user?.id) {
      loadUserControlPreferences();
    }
  }, [user?.id]);

  const loadUserControlPreferences = async () => {
    try {
      const response = await fetch(buildApiUrl(`/user-preferences/${user?.id}/controls`));
      if (response.ok) {
        const data = await response.json();
        if (data.controls) {
          setControls(data.controls);
        }
      }
    } catch (error) {
      console.error('Error loading control preferences:', error);
    }
  };

  const saveControlPreferences = async (newControls: GlobalControlsState) => {
    if (!user?.id) return;
    
    try {
      await fetch(buildApiUrl(`/user-preferences/${user.id}/controls`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ controls: newControls })
      });
      
      // Track control adjustment
      trackInteraction('global-controls', 'adjust', {
        description: 'User adjusted global controls',
        controls: newControls,
        activeControl
      });
    } catch (error) {
      console.error('Error saving control preferences:', error);
    }
  };

  const updateControl = (controlName: keyof GlobalControlsState, value: number) => {
    const newControls = { ...controls, [controlName]: value };
    setControls(newControls);
    setActiveControl(controlName);
    saveControlPreferences(newControls);
  };

  const getControlColor = (value: number): string => {
    if (value >= 80) return 'green';
    if (value >= 60) return 'blue';
    if (value >= 40) return 'yellow';
    if (value >= 20) return 'orange';
    return 'red';
  };

  const getResonanceCompassAngle = (resonance: number): number => {
    return (resonance * 360) % 360;
  };

  const triggerSerendipity = async () => {
    if (!user?.id) return;
    
    try {
      const response = await fetch(buildApiUrl('/serendipity/trigger'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: user.id,
          serendipityLevel: controls.serendipityLevel,
          currentContext: window.location.pathname
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.suggestion) {
          // Show serendipitous suggestion
          alert(`🎲 Serendipitous Discovery: ${data.suggestion.title}\n\n${data.suggestion.description}`);
        }
      }
      
      trackInteraction('serendipity-dial', 'trigger', {
        description: 'User triggered serendipity discovery',
        level: controls.serendipityLevel
      });
    } catch (error) {
      console.error('Error triggering serendipity:', error);
    }
  };

  const generateCuriosityPrompt = async () => {
    if (!user?.id) return;
    
    try {
      const response = await fetch(buildApiUrl('/curiosity/generate-prompt'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: user.id,
          curiosityLevel: controls.curiosityLevel,
          currentInterests: ['consciousness', 'technology', 'unity']
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.prompt) {
          alert(`🤔 Curiosity Prompt: ${data.prompt.question}\n\n💡 ${data.prompt.guidance}`);
        }
      }
      
      trackInteraction('curiosity-prompts', 'generate', {
        description: 'User generated curiosity prompt',
        level: controls.curiosityLevel
      });
    } catch (error) {
      console.error('Error generating curiosity prompt:', error);
    }
  };

  return (
    <div className="fixed bottom-4 right-4 z-50">
      {/* Collapsed State */}
      {!isExpanded && (
        <button
          onClick={() => setIsExpanded(true)}
          className="w-16 h-16 bg-gradient-to-br from-purple-500 to-blue-600 text-white rounded-full shadow-lg hover:shadow-xl transition-all duration-300 flex items-center justify-center"
        >
          <span className="text-2xl">🌊</span>
        </button>
      )}

      {/* Expanded State */}
      {isExpanded && (
        <div className="bg-white rounded-2xl shadow-2xl border border-gray-200 p-6 w-80 max-h-96 overflow-y-auto">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-lg font-semibold text-gray-900">🌊 Global Controls</h3>
            <button
              onClick={() => setIsExpanded(false)}
              className="text-gray-400 hover:text-gray-600 text-xl"
            >
              ×
            </button>
          </div>

          {/* Resonance Compass */}
          <div className="mb-6">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700">🧭 Resonance Compass</label>
              <span className="text-sm text-gray-500">{controls.resonanceLevel}%</span>
            </div>
            
            {/* Compass Visual */}
            <div className="relative w-20 h-20 mx-auto mb-3">
              <div className="absolute inset-0 rounded-full border-4 border-gray-200"></div>
              <div 
                className="absolute inset-2 rounded-full bg-gradient-to-br from-blue-400 to-purple-500"
                style={{
                  transform: `rotate(${getResonanceCompassAngle(resonanceData.currentResonance)}deg)`,
                  transformOrigin: 'center'
                }}
              ></div>
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="w-2 h-2 bg-white rounded-full"></div>
              </div>
            </div>
            
            <input
              type="range"
              min="0"
              max="100"
              value={controls.resonanceLevel}
              onChange={(e) => updateControl('resonanceLevel', parseInt(e.target.value))}
              className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
            />
            <div className="flex justify-between text-xs text-gray-500 mt-1">
              <span>Dissonance</span>
              <span>Harmony</span>
            </div>
          </div>

          {/* Joy Tuner */}
          <div className="mb-6">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700">😊 Joy Tuner</label>
              <span className={`text-sm font-medium text-${getControlColor(controls.joyLevel)}-600`}>
                {controls.joyLevel}%
              </span>
            </div>
            <input
              type="range"
              min="0"
              max="100"
              value={controls.joyLevel}
              onChange={(e) => updateControl('joyLevel', parseInt(e.target.value))}
              className={`w-full h-2 bg-${getControlColor(controls.joyLevel)}-200 rounded-lg appearance-none cursor-pointer`}
            />
            <div className="flex justify-between text-xs text-gray-500 mt-1">
              <span>Neutral</span>
              <span>Blissful</span>
            </div>
          </div>

          {/* Serendipity Dial */}
          <div className="mb-6">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700">🎲 Serendipity Dial</label>
              <button
                onClick={triggerSerendipity}
                className="px-3 py-1 bg-purple-600 text-white text-xs rounded-md hover:bg-purple-700"
              >
                Trigger
              </button>
            </div>
            <input
              type="range"
              min="0"
              max="100"
              value={controls.serendipityLevel}
              onChange={(e) => updateControl('serendipityLevel', parseInt(e.target.value))}
              className="w-full h-2 bg-purple-200 rounded-lg appearance-none cursor-pointer"
            />
            <div className="flex justify-between text-xs text-gray-500 mt-1">
              <span>Predictable</span>
              <span>Surprising</span>
            </div>
          </div>

          {/* Curiosity Prompts */}
          <div className="mb-6">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700">🤔 Curiosity Level</label>
              <button
                onClick={generateCuriosityPrompt}
                className="px-3 py-1 bg-indigo-600 text-white text-xs rounded-md hover:bg-indigo-700"
              >
                Prompt
              </button>
            </div>
            <input
              type="range"
              min="0"
              max="100"
              value={controls.curiosityLevel}
              onChange={(e) => updateControl('curiosityLevel', parseInt(e.target.value))}
              className="w-full h-2 bg-indigo-200 rounded-lg appearance-none cursor-pointer"
            />
            <div className="flex justify-between text-xs text-gray-500 mt-1">
              <span>Satisfied</span>
              <span>Exploring</span>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="grid grid-cols-2 gap-2">
            <button
              onClick={() => {
                setControls({
                  resonanceLevel: 85,
                  joyLevel: 90,
                  serendipityLevel: 60,
                  curiosityLevel: 85
                });
              }}
              className="px-3 py-2 bg-green-600 text-white text-sm rounded-md hover:bg-green-700"
            >
              ✨ Optimize
            </button>
            <button
              onClick={() => {
                setControls({
                  resonanceLevel: 50,
                  joyLevel: 50,
                  serendipityLevel: 50,
                  curiosityLevel: 50
                });
              }}
              className="px-3 py-2 bg-gray-600 text-white text-sm rounded-md hover:bg-gray-700"
            >
              🔄 Reset
            </button>
          </div>

          {/* Status Indicator */}
          <div className="mt-4 pt-4 border-t border-gray-200">
            <div className="flex items-center justify-between text-xs text-gray-500">
              <span>Global Resonance</span>
              <span className={`font-medium text-${getControlColor((controls.resonanceLevel + controls.joyLevel) / 2)}-600`}>
                {Math.round((controls.resonanceLevel + controls.joyLevel + controls.serendipityLevel + controls.curiosityLevel) / 4)}%
              </span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
