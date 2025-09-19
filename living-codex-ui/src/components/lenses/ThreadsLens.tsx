'use client';

import { useState, useEffect } from 'react';
import { MessageCircle, Reply, Heart, Share2, Clock } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';
import { UXPrimitives } from '@/components/primitives/UXPrimitives';

interface Thread {
  id: string;
  title: string;
  content: string;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt: string;
  updatedAt: string;
  replies: Reply[];
  resonance: number;
  axes: string[];
  isResolved: boolean;
}

interface Reply {
  id: string;
  content: string;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt: string;
  resonance: number;
  isAccepted: boolean;
}

interface ThreadsLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

export function ThreadsLens({ controls = {}, userId, className = '' }: ThreadsLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  
  const [threads, setThreads] = useState<Thread[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedThread, setSelectedThread] = useState<Thread | null>(null);
  const [showCreateThread, setShowCreateThread] = useState(false);
  const [newThread, setNewThread] = useState({
    title: '',
    content: ''
  });


  useEffect(() => {
    loadThreads();
  }, []);

  const loadThreads = async () => {
    setLoading(true);
    try {
      const response = await fetch(buildApiUrl('/threads/list'));
      if (response.ok) {
        const data = await response.json();
        if (data.threads && data.threads.length > 0) {
          setThreads(data.threads);
        } else {
          setThreads([]);
        }
      } else {
        setThreads([]);
      }
    } catch (error) {
      console.error('Error loading threads:', error);
      setThreads([]);
    } finally {
      setLoading(false);
    }
  };

  const createThread = async () => {
    if (!newThread.title.trim() || !newThread.content.trim() || !user?.id) return;

    try {
      const response = await fetch(buildApiUrl('/threads/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: newThread.title,
          content: newThread.content,
          authorId: user.id,
          axes: ['consciousness', 'unity']
        })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await loadThreads();
          setShowCreateThread(false);
          setNewThread({ title: '', content: '' });
          
          trackInteraction(data.threadId || 'new-thread', 'create-thread', {
            description: `User created thread: ${newThread.title}`
          });
        }
      }
    } catch (error) {
      console.error('Error creating thread:', error);
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
            💬 Threads
          </h2>
          <p className="text-gray-600 mt-1">
            Deep conversations and collaborative exploration
          </p>
        </div>
        <button
          onClick={() => setShowCreateThread(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          ➕ Start Thread
        </button>
      </div>

      {/* Create Thread Modal */}
      {showCreateThread && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Start New Thread</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Title</label>
                <input
                  type="text"
                  value={newThread.title}
                  onChange={(e) => setNewThread({...newThread, title: e.target.value})}
                  placeholder="What would you like to explore?"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Content</label>
                <textarea
                  value={newThread.content}
                  onChange={(e) => setNewThread({...newThread, content: e.target.value})}
                  placeholder="Share your thoughts and invite discussion..."
                  rows={6}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>
            <div className="flex space-x-3 mt-6">
              <button
                onClick={createThread}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Create Thread
              </button>
              <button
                onClick={() => setShowCreateThread(false)}
                className="flex-1 px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Threads List */}
      <div className="space-y-4">
        {threads.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <div className="text-6xl mb-4">💬</div>
            <p>No threads available yet.</p>
            <p className="text-sm mt-2">Start the first conversation!</p>
          </div>
        ) : (
          threads.map((thread) => (
          <div key={thread.id} className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
            {/* Thread Header */}
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{thread.title}</h3>
                <p className="text-gray-700 mb-3">{thread.content}</p>
                
                {/* Thread Meta */}
                <div className="flex items-center space-x-4 text-sm text-gray-500 mb-3">
                  <div className="flex items-center space-x-2">
                    <img
                      src={thread.author.avatar}
                      alt={thread.author.name}
                      className="w-6 h-6 rounded-full"
                    />
                    <span>{thread.author.name}</span>
                  </div>
                  <div className="flex items-center space-x-1">
                    <Clock className="w-4 h-4" />
                    <span>{formatTimeAgo(thread.createdAt)}</span>
                  </div>
                  <div className="flex items-center space-x-1">
                    <MessageCircle className="w-4 h-4" />
                    <span>{thread.replies.length} replies</span>
                  </div>
                  {thread.isResolved && (
                    <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded-full">
                      ✓ Resolved
                    </span>
                  )}
                </div>

                {/* Axes Tags */}
                <div className="flex flex-wrap gap-2 mb-4">
                  {thread.axes.map((axis) => (
                    <span
                      key={axis}
                      className={`px-2 py-1 text-xs font-medium rounded-full ${getAxisColor(axis)}`}
                    >
                      {axis}
                    </span>
                  ))}
                </div>

                {/* Resonance Bar */}
                <div className="mb-4">
                  <div className="flex items-center justify-between text-sm text-gray-600 mb-1">
                    <span>Resonance</span>
                    <span>{(thread.resonance * 100).toFixed(0)}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div
                      className="bg-gradient-to-r from-blue-500 to-purple-600 h-2 rounded-full transition-all duration-300"
                      style={{ width: `${thread.resonance * 100}%` }}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4">
                <button
                  onClick={() => setSelectedThread(thread)}
                  className="flex items-center space-x-2 px-3 py-1 text-blue-600 hover:text-blue-800 transition-colors"
                >
                  <Reply className="w-4 h-4" />
                  <span>Reply</span>
                </button>
                
                <UXPrimitives
                  contentId={thread.id}
                  showWeave={true}
                  showReflect={true}
                  showInvite={true}
                />
              </div>

              <div className="flex items-center space-x-2">
                <button className="p-2 text-gray-400 hover:text-red-500 transition-colors">
                  <Heart className="w-4 h-4" />
                </button>
                <button className="p-2 text-gray-400 hover:text-blue-500 transition-colors">
                  <Share2 className="w-4 h-4" />
                </button>
              </div>
            </div>

            {/* Recent Replies Preview */}
            {thread.replies.length > 0 && (
              <div className="mt-4 pt-4 border-t border-gray-100">
                <h4 className="text-sm font-medium text-gray-700 mb-3">Recent Replies</h4>
                <div className="space-y-3">
                  {thread.replies.slice(0, 2).map((reply) => (
                    <div key={reply.id} className="flex items-start space-x-3">
                      <img
                        src={reply.author.avatar}
                        alt={reply.author.name}
                        className="w-8 h-8 rounded-full"
                      />
                      <div className="flex-1">
                        <div className="flex items-center space-x-2 mb-1">
                          <span className="text-sm font-medium text-gray-900">{reply.author.name}</span>
                          <span className="text-xs text-gray-500">{formatTimeAgo(reply.createdAt)}</span>
                          {reply.isAccepted && (
                            <span className="px-1 py-0.5 bg-green-100 text-green-800 text-xs rounded">
                              Accepted
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-700">{reply.content}</p>
                        <div className="mt-1">
                          <span className="text-xs text-purple-600">
                            Resonance: {(reply.resonance * 100).toFixed(0)}%
                          </span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        ))
        )}
      </div>

      {/* Thread Detail Modal */}
      {selectedThread && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-4xl max-h-[80vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-900">{selectedThread.title}</h3>
              <button
                onClick={() => setSelectedThread(null)}
                className="text-gray-400 hover:text-gray-600"
              >
                ✕
              </button>
            </div>

            {/* Full thread content and all replies would go here */}
            <div className="space-y-4">
              <p className="text-gray-700">{selectedThread.content}</p>
              {/* All replies would be displayed here */}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
