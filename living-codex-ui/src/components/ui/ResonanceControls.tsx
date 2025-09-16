'use client';

import { useState, useEffect } from 'react';
import { useResonanceControls } from '@/lib/hooks';

interface ResonanceControlsProps {
  onControlsChange?: (controls: Record<string, any>) => void;
  className?: string;
}

export function ResonanceControls({ onControlsChange, className = '' }: ResonanceControlsProps) {
  const { data: controls, isLoading } = useResonanceControls();
  const [values, setValues] = useState({
    axes: ['resonance'],
    joy: 0.7,
    serendipity: 0.5,
  });

  useEffect(() => {
    if (onControlsChange) {
      onControlsChange(values);
    }
  }, [values, onControlsChange]);

  if (isLoading || !controls) {
    return <div className="animate-pulse bg-gray-200 h-20 rounded-lg" />;
  }

  const handleAxisChange = (axis: string, checked: boolean) => {
    setValues(prev => ({
      ...prev,
      axes: checked 
        ? [...prev.axes, axis]
        : prev.axes.filter(a => a !== axis)
    }));
  };

  const handleRangeChange = (field: string, value: number) => {
    setValues(prev => ({
      ...prev,
      [field]: value
    }));
  };

  return (
    <div className={`bg-white rounded-lg border p-4 space-y-4 ${className}`}>
      <h3 className="text-lg font-semibold text-gray-900">Resonance Controls</h3>
      
      {/* Axes Selection */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          U-CORE Axes
        </label>
        <div className="grid grid-cols-2 gap-2">
          {controls.fields.find(f => f.id === 'axes')?.options?.map((axis) => (
            <label key={axis} className="flex items-center space-x-2">
              <input
                type="checkbox"
                checked={values.axes.includes(axis)}
                onChange={(e) => handleAxisChange(axis, e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 capitalize">{axis}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Joy Tuner */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Joy Level: {Math.round(values.joy * 100)}%
        </label>
        <input
          type="range"
          min="0"
          max="1"
          step="0.1"
          value={values.joy}
          onChange={(e) => handleRangeChange('joy', parseFloat(e.target.value))}
          className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
        />
      </div>

      {/* Serendipity Dial */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Serendipity: {Math.round(values.serendipity * 100)}%
        </label>
        <input
          type="range"
          min="0"
          max="1"
          step="0.1"
          value={values.serendipity}
          onChange={(e) => handleRangeChange('serendipity', parseFloat(e.target.value))}
          className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
        />
      </div>

      {/* Status Badge */}
      <div className="flex items-center space-x-2">
        <span className="text-xs text-gray-500">Status:</span>
        <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded-full">
          {controls.status}
        </span>
      </div>
    </div>
  );
}
