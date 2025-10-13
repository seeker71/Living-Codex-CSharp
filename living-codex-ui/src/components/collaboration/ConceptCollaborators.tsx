'use client';

import { useConceptCollaborators } from '@/lib/hooks';
import { Users, Zap, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';

interface ConceptCollaboratorsProps {
  conceptId: string;
  className?: string;
}

export function ConceptCollaborators({ conceptId, className = '' }: ConceptCollaboratorsProps) {
  const { data, isLoading, error } = useConceptCollaborators(conceptId);

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" />
            Collaborators
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="flex items-center gap-3 animate-pulse">
                <div className="w-10 h-10 bg-gray-200 dark:bg-gray-700 rounded-full"></div>
                <div className="flex-1">
                  <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2"></div>
                  <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2"></div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-red-600 dark:text-red-400">
            <Users className="w-5 h-5" />
            Collaborators
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-red-600 dark:text-red-400">
            Failed to load collaborators
          </p>
        </CardContent>
      </Card>
    );
  }

  const collaborators = data?.data?.collaborators || [];
  const attuneCount = data?.data?.attuneCount || 0;
  const amplifyCount = data?.data?.amplifyCount || 0;

  const formatDate = (dateStr: string) => {
    try {
      return new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric'
      });
    } catch {
      return dateStr;
    }
  };

  // Group collaborators by user
  const userMap = new Map();
  collaborators.forEach((collab: any) => {
    const existing = userMap.get(collab.userId);
    if (existing) {
      existing.relationships.push(collab.relationshipType);
    } else {
      userMap.set(collab.userId, {
        userId: collab.userId,
        username: collab.username,
        relationships: [collab.relationshipType],
        createdAt: collab.createdAt
      });
    }
  });

  const uniqueUsers = Array.from(userMap.values());

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Users className="w-5 h-5" />
          Collaborators
        </CardTitle>
        <div className="flex items-center gap-4 mt-2 text-sm text-gray-600 dark:text-gray-400">
          <div className="flex items-center gap-1">
            <Zap className="w-4 h-4 text-yellow-500" />
            <span>{attuneCount} attuned</span>
          </div>
          <div className="flex items-center gap-1">
            <TrendingUp className="w-4 h-4 text-blue-500" />
            <span>{amplifyCount} amplified</span>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {uniqueUsers.length === 0 ? (
          <div className="text-center py-8">
            <Users className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
            <p className="text-gray-600 dark:text-gray-400">No collaborators yet</p>
            <p className="text-sm text-gray-500 dark:text-gray-500 mt-1">
              Be the first to attune to this concept!
            </p>
          </div>
        ) : (
          <div className="space-y-3 max-h-[400px] overflow-y-auto">
            {uniqueUsers.map((user: any) => (
              <div
                key={user.userId}
                className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
              >
                <div className="w-10 h-10 bg-gradient-to-br from-purple-500 to-blue-500 rounded-full flex items-center justify-center text-white font-semibold text-sm">
                  {user.username.charAt(0).toUpperCase()}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-gray-900 dark:text-gray-100 truncate">
                    {user.username}
                  </p>
                  <div className="flex items-center gap-2 mt-1">
                    {user.relationships.includes('attuned') && (
                      <span className="flex items-center gap-1 text-xs text-yellow-600 dark:text-yellow-400">
                        <Zap className="w-3 h-3" />
                        Attuned
                      </span>
                    )}
                    {user.relationships.includes('amplified') && (
                      <span className="flex items-center gap-1 text-xs text-blue-600 dark:text-blue-400">
                        <TrendingUp className="w-3 h-3" />
                        Amplified
                      </span>
                    )}
                    <span className="text-xs text-gray-500">•</span>
                    <span className="text-xs text-gray-500 dark:text-gray-500">
                      {formatDate(user.createdAt)}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

