'use client';

import React from 'react';
import { Card, CardContent } from './Card';

interface EdgeCardProps {
  edge: {
    fromId: string;
    toId: string;
    role: string;
    weight?: number;
    meta?: Record<string, any>;
  };
  onNodeClick?: (nodeId: string) => void;
  onEdgeClick?: (fromId: string, toId: string, role: string) => void;
  className?: string;
}

export function EdgeCard({ edge, onNodeClick, onEdgeClick, className = '' }: EdgeCardProps) {
  const { fromId, toId, role, weight, meta } = edge;

  const handleNodeClick = (nodeId: string) => {
    if (onNodeClick) {
      onNodeClick(nodeId);
    } else {
      // Default behavior: open in new tab
      window.open(`/node/${encodeURIComponent(nodeId)}`, '_blank');
    }
  };

  const handleEdgeClick = () => {
    if (onEdgeClick) {
      onEdgeClick(fromId, toId, role);
    } else {
      // Default behavior: open edge view in new tab
      window.open(`/edge/${encodeURIComponent(fromId)}/${encodeURIComponent(toId)}`, '_blank');
    }
  };

  const getRoleColor = (role: string) => {
    const roleLower = role.toLowerCase();
    if (roleLower.includes('parent') || roleLower.includes('contains')) return 'bg-blue-100 text-blue-800 border-blue-200';
    if (roleLower.includes('child') || roleLower.includes('belongs')) return 'bg-green-100 text-green-800 border-green-200';
    if (roleLower.includes('related') || roleLower.includes('similar')) return 'bg-purple-100 text-purple-800 border-purple-200';
    if (roleLower.includes('references') || roleLower.includes('links')) return 'bg-orange-100 text-orange-800 border-orange-200';
    return 'bg-gray-100 text-gray-800 border-gray-200';
  };

  const getWeightColor = (weight?: number) => {
    if (!weight) return 'bg-gray-100 text-gray-600';
    if (weight >= 0.8) return 'bg-red-100 text-red-800';
    if (weight >= 0.6) return 'bg-yellow-100 text-yellow-800';
    if (weight >= 0.4) return 'bg-blue-100 text-blue-800';
    return 'bg-green-100 text-green-800';
  };

  const truncateId = (id: string, maxLength: number = 40) => {
    if (id.length <= maxLength) return id;
    return `${id.substring(0, maxLength - 3)}...`;
  };

  return (
    <Card className={`hover:shadow-md transition-shadow ${className}`}>
      <CardContent className="p-4">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            {/* Role and Weight badges */}
            <div className="flex items-center gap-2 mb-3 flex-wrap">
              <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium border ${getRoleColor(role)}`}>
                {role}
              </span>
              {weight !== undefined && (
                <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getWeightColor(weight)}`}>
                  Weight: {weight.toFixed(2)}
                </span>
              )}
              {meta?.relationship && (
                <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800">
                  {meta.relationship}
                </span>
              )}
            </div>

            {/* Edge relationship */}
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400 min-w-0 flex-shrink-0">From:</span>
                <button
                  onClick={() => handleNodeClick(fromId)}
                  className="text-sm text-blue-600 hover:text-blue-800 hover:underline font-mono truncate max-w-xs"
                  title={fromId}
                >
                  {truncateId(fromId)}
                </button>
              </div>
              
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400 min-w-0 flex-shrink-0">To:</span>
                <button
                  onClick={() => handleNodeClick(toId)}
                  className="text-sm text-blue-600 hover:text-blue-800 hover:underline font-mono truncate max-w-xs"
                  title={toId}
                >
                  {truncateId(toId)}
                </button>
              </div>
            </div>

            {/* Additional metadata */}
            {meta && Object.keys(meta).length > 1 && (
              <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
                <details className="group">
                  <summary className="text-xs text-gray-500 cursor-pointer hover:text-gray-700 dark:hover:text-gray-300">
                    Metadata ({Object.keys(meta).length - 1} properties)
                  </summary>
                  <div className="mt-2 text-xs text-gray-600 dark:text-gray-400 font-mono bg-gray-50 dark:bg-gray-800 p-2 rounded max-h-32 overflow-y-auto">
                    <pre className="whitespace-pre-wrap">
                      {JSON.stringify(meta, null, 2)}
                    </pre>
                  </div>
                </details>
              </div>
            )}
          </div>

          {/* Action button */}
          <div className="ml-4 flex-shrink-0">
            <button
              onClick={handleEdgeClick}
              className="text-blue-600 hover:text-blue-800 text-sm font-medium px-3 py-1 rounded-md hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
              title="View edge details"
            >
              View Edge →
            </button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
