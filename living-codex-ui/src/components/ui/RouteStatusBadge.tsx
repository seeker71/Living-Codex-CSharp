'use client'

import React from 'react'

export type RouteStatus = 
  | 'Stub' 
  | 'Simple' 
  | 'Simulated' 
  | 'Fallback' 
  | 'AiEnabled' 
  | 'ExternalInfo' 
  | 'Untested' 
  | 'PartiallyTested' 
  | 'FullyTested'

interface RouteStatusBadgeProps {
  status: RouteStatus
  size?: 'sm' | 'md' | 'lg'
  showLabel?: boolean
  className?: string
}

const statusConfig: Record<RouteStatus, {
  color: string;
  darkColor: string;
  icon: string;
  label: string;
  description: string;
}> = {
  Stub: {
    color: 'bg-gray-100 text-gray-600 border-gray-200',
    darkColor: 'dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700',
    icon: '🚧',
    label: 'Stub',
    description: 'Placeholder implementation'
  },
  Simple: {
    color: 'bg-blue-100 text-blue-700 border-blue-200',
    darkColor: 'dark:bg-blue-900 dark:text-blue-300 dark:border-blue-800',
    icon: '⚡',
    label: 'Simple',
    description: 'Basic functionality'
  },
  Simulated: {
    color: 'bg-yellow-100 text-yellow-700 border-yellow-200',
    darkColor: 'dark:bg-yellow-900 dark:text-yellow-300 dark:border-yellow-800',
    icon: '🎭',
    label: 'Simulated',
    description: 'Mock/simulated data'
  },
  Fallback: {
    color: 'bg-orange-100 text-orange-700 border-orange-200',
    darkColor: 'dark:bg-orange-900 dark:text-orange-300 dark:border-orange-800',
    icon: '🔄',
    label: 'Fallback',
    description: 'Backup implementation'
  },
  AiEnabled: {
    color: 'bg-purple-100 text-purple-700 border-purple-200',
    darkColor: 'dark:bg-purple-900 dark:text-purple-300 dark:border-purple-800',
    icon: '🤖',
    label: 'AI-Enabled',
    description: 'Enhanced with AI'
  },
  ExternalInfo: {
    color: 'bg-indigo-100 text-indigo-700 border-indigo-200',
    darkColor: 'dark:bg-indigo-900 dark:text-indigo-300 dark:border-indigo-800',
    icon: '🌐',
    label: 'External',
    description: 'Uses external services'
  },
  Untested: {
    color: 'bg-red-100 text-red-700 border-red-200',
    darkColor: 'dark:bg-red-900 dark:text-red-300 dark:border-red-800',
    icon: '❓',
    label: 'Untested',
    description: 'Not yet tested'
  },
  PartiallyTested: {
    color: 'bg-amber-100 text-amber-700 border-amber-200',
    darkColor: 'dark:bg-amber-900 dark:text-amber-300 dark:border-amber-800',
    icon: '🧪',
    label: 'Partial Tests',
    description: 'Some tests passing'
  },
  FullyTested: {
    color: 'bg-green-100 text-green-700 border-green-200',
    darkColor: 'dark:bg-green-900 dark:text-green-300 dark:border-green-800',
    icon: '✅',
    label: 'Fully Tested',
    description: 'All tests passing'
  }
}

const sizeClasses = {
  sm: 'px-2 py-1 text-xs',
  md: 'px-3 py-1.5 text-sm',
  lg: 'px-4 py-2 text-base'
}

export function RouteStatusBadge({
  status,
  size = 'md',
  showLabel = true,
  className = ''
}: RouteStatusBadgeProps) {
  const config = statusConfig[status]
  const sizeClass = sizeClasses[size]

  return (
    <span
      className={`
        inline-flex items-center gap-1.5 rounded-full border font-medium
        ${config.color} ${config.darkColor} ${sizeClass} ${className}
        transition-colors duration-200
      `}
      title={config.description}
    >
      <span className="text-sm" role="img" aria-label={config.label}>
        {config.icon}
      </span>
      {showLabel && (
        <span className="font-medium">
          {config.label}
        </span>
      )}
    </span>
  )
}

export function RouteStatusIndicator({ status }: { status: RouteStatus }) {
  const config = statusConfig[status]
  
  return (
    <div className="flex items-center gap-2">
      <div 
        className={`
          w-3 h-3 rounded-full border-2
          ${config.color.replace('text-', 'border-').replace('bg-', 'bg-')}
          ${config.darkColor.replace('text-', 'border-').replace('bg-', 'bg-')}
        `}
        title={config.description}
      />
      <span className="text-sm text-gray-600 dark:text-gray-400">
        {config.label}
      </span>
    </div>
  )
}

export default RouteStatusBadge
