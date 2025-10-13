'use client';

/**
 * Global Activity Feed
 * Shows activity across all concepts in the Living Codex
 * Uses ConceptCollaborationModule endpoints aggregated
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { 
  Activity, 
  Zap, 
  TrendingUp, 
  MessageSquare, 
  User, 
  Clock,
  Filter,
  Search
} from 'lucide-react';

interface ActivityItem {
  type: 'attune' | 'amplify' | 'discussion' | 'concept-create' | 'user-join';
  userId: string;
  username: string;
  conceptId?: string;
  conceptTitle?: string;
  timestamp: string;
  description: string;
  preview?: string;
  contributionType?: string;
}

export default function ActivityPage() {
  const { user, isAuthenticated } = useAuth();
  const [activities, setActivities] = useState<ActivityItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'my-network' | 'following'>('all');
  const [activityTypeFilter, setActivityTypeFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    loadGlobalActivity();
  }, [filter]);

  const loadGlobalActivity = async () => {
    try {
      setLoading(true);

      // Aggregate activity from multiple sources
      const activityItems: ActivityItem[] = [];

      // Get contributions (amplifications)
      try {
        const contribRes = await api.get('/storage-endpoints/nodes?typeId=codex.contribution&take=50');
        if (contribRes?.success && contribRes.nodes) {
          contribRes.nodes.forEach((node: any) => {
            const userId = node.meta?.userId || '';
            const conceptId = node.meta?.entityId || '';
            if (userId && conceptId) {
              activityItems.push({
                type: 'amplify',
                userId,
                username: node.meta?.username || 'Unknown User',
                conceptId,
                conceptTitle: node.meta?.conceptTitle || 'Unknown Concept',
                timestamp: node.meta?.createdAt || new Date().toISOString(),
                description: node.meta?.description || `Amplified a concept`,
                contributionType: node.meta?.contributionType || '',
              });
            }
          });
        }
      } catch (err) {
        console.log('Could not load contributions');
      }

      // Get discussions
      try {
        const discussionsRes = await api.get('/storage-endpoints/nodes?typeId=codex.discussion&take=50');
        if (discussionsRes?.success && discussionsRes.nodes) {
          discussionsRes.nodes.forEach((node: any) => {
            const userId = node.meta?.userId || '';
            const conceptId = node.meta?.conceptId || '';
            if (userId && conceptId) {
              activityItems.push({
                type: 'discussion',
                userId,
                username: node.meta?.username || 'Unknown User',
                conceptId,
                conceptTitle: node.meta?.conceptTitle || 'Concept',
                timestamp: node.meta?.createdAt || new Date().toISOString(),
                description: `Started a discussion: ${node.title}`,
                preview: node.description,
              });
            }
          });
        }
      } catch (err) {
        console.log('Could not load discussions');
      }

      // Sort by timestamp
      activityItems.sort((a, b) => 
        new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
      );

      setActivities(activityItems);
    } catch (error) {
      console.error('Error loading activity:', error);
    } finally {
      setLoading(false);
    }
  };

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'attune':
        return <Zap className="w-5 h-5 text-yellow-500" />;
      case 'amplify':
        return <TrendingUp className="w-5 h-5 text-blue-500" />;
      case 'discussion':
        return <MessageSquare className="w-5 h-5 text-purple-500" />;
      case 'concept-create':
        return <Activity className="w-5 h-5 text-green-500" />;
      case 'user-join':
        return <User className="w-5 h-5 text-cyan-500" />;
      default:
        return <Activity className="w-5 h-5 text-gray-500" />;
    }
  };

  const formatTimestamp = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      const now = new Date();
      const diffMs = now.getTime() - date.getTime();
      const diffMins = Math.floor(diffMs / 60000);
      const diffHours = Math.floor(diffMs / 3600000);
      const diffDays = Math.floor(diffMs / 86400000);

      if (diffMins < 1) return 'just now';
      if (diffMins < 60) return `${diffMins}m ago`;
      if (diffHours < 24) return `${diffHours}h ago`;
      if (diffDays < 7) return `${diffDays}d ago`;
      return date.toLocaleDateString();
    } catch {
      return timestamp;
    }
  };

  const filteredActivities = activities.filter(activity => {
    // Type filter
    if (activityTypeFilter !== 'all' && activity.type !== activityTypeFilter) {
      return false;
    }

    // Search filter
    if (searchQuery && !activity.description.toLowerCase().includes(searchQuery.toLowerCase()) &&
        !activity.username.toLowerCase().includes(searchQuery.toLowerCase())) {
      return false;
    }

    // Network filter
    if (filter === 'my-network' && user?.id) {
      // Only show activities from users in my network (simplified - just not me)
      return activity.userId !== user.id;
    }

    return true;
  });

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
        <div className="max-w-4xl mx-auto">
          <div className="animate-pulse space-y-6">
            <div className="h-12 bg-gray-700 rounded w-1/3"></div>
            <div className="space-y-3">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="h-24 bg-gray-700 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-purple-400 to-pink-400 bg-clip-text text-transparent flex items-center gap-3">
            <Activity className="w-10 h-10 text-purple-400" />
            Global Activity Feed
          </h1>
          <p className="text-gray-400 mt-2">
            See what's happening across the Living Codex
          </p>
        </div>

        {/* Filters */}
        <Card className="p-4">
          <div className="flex flex-col md:flex-row gap-4">
            {/* View Filter */}
            <div className="flex gap-2">
              {(['all', 'my-network', 'following'] as const).map(f => (
                <button
                  key={f}
                  onClick={() => setFilter(f)}
                  className={`px-3 py-1.5 rounded-lg text-sm transition-colors ${
                    filter === f
                      ? 'bg-purple-600 text-white'
                      : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                  }`}
                >
                  {f === 'all' ? 'All' : f === 'my-network' ? 'My Network' : 'Following'}
                </button>
              ))}
            </div>

            {/* Type Filter */}
            <div className="flex items-center gap-2 flex-1">
              <Filter className="w-4 h-4 text-gray-400" />
              <select
                value={activityTypeFilter}
                onChange={(e) => setActivityTypeFilter(e.target.value)}
                className="flex-1 px-3 py-1.5 bg-gray-800 text-gray-300 rounded-lg text-sm border border-gray-700 focus:border-purple-500 focus:outline-none"
              >
                <option value="all">All Activities</option>
                <option value="attune">Attunes</option>
                <option value="amplify">Amplifications</option>
                <option value="discussion">Discussions</option>
                <option value="concept-create">Concept Creation</option>
              </select>
            </div>

            {/* Search */}
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search activity..."
                className="w-full pl-10 pr-3 py-1.5 bg-gray-800 text-gray-300 rounded-lg text-sm border border-gray-700 focus:border-purple-500 focus:outline-none"
              />
            </div>
          </div>
        </Card>

        {/* Activity Stream */}
        <div className="space-y-3">
          {filteredActivities.length === 0 ? (
            <Card className="p-12">
              <div className="text-center text-gray-500">
                <Activity className="w-16 h-16 mx-auto mb-4 opacity-50" />
                <div className="text-xl mb-2">No activity found</div>
                <p className="text-sm">
                  {searchQuery 
                    ? 'Try adjusting your search or filters'
                    : 'Be the first to create activity on the platform!'}
                </p>
              </div>
            </Card>
          ) : (
            filteredActivities.map((activity, index) => (
              <Card
                key={`${activity.type}-${activity.userId}-${index}`}
                className="p-4 hover:border-purple-500 transition-all cursor-pointer"
              >
                <div className="flex items-start gap-4">
                  {/* Icon */}
                  <div className="mt-1">
                    {getActivityIcon(activity.type)}
                  </div>

                  {/* Content */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1">
                        <p className="text-white">{activity.description}</p>
                        {activity.preview && (
                          <p className="text-sm text-gray-400 mt-1 line-clamp-2">
                            {activity.preview}
                          </p>
                        )}
                        {activity.conceptTitle && (
                          <a
                            href={`/reflect/${activity.conceptId}`}
                            className="inline-block mt-2 text-sm text-purple-400 hover:text-purple-300 underline"
                            onClick={(e) => e.stopPropagation()}
                          >
                            View concept: {activity.conceptTitle}
                          </a>
                        )}
                      </div>
                    </div>

                    {/* Metadata */}
                    <div className="flex items-center gap-3 mt-2 text-xs text-gray-500">
                      <div className="flex items-center gap-1">
                        <User className="w-3 h-3" />
                        <span>{activity.username}</span>
                      </div>
                      <span>•</span>
                      <div className="flex items-center gap-1">
                        <Clock className="w-3 h-3" />
                        <span>{formatTimestamp(activity.timestamp)}</span>
                      </div>
                      {activity.contributionType && (
                        <>
                          <span>•</span>
                          <span className="px-2 py-0.5 bg-purple-900/30 text-purple-400 rounded-full">
                            {activity.contributionType}
                          </span>
                        </>
                      )}
                    </div>
                  </div>
                </div>
              </Card>
            ))
          )}
        </div>

        {/* Load More */}
        {filteredActivities.length > 0 && filteredActivities.length >= 50 && (
          <div className="text-center">
            <button
              onClick={loadGlobalActivity}
              className="px-6 py-3 bg-purple-600 hover:bg-purple-700 rounded-lg transition-colors"
            >
              Load More Activity
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

