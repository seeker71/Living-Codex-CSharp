'use client';

import { useState, useEffect, useRef } from 'react';
import { MessageCircle, Reply, Heart, Share2, Clock, Users, Hash, Filter, Search, Plus, X, AlertCircle } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';
import { UXPrimitives } from '@/components/primitives/UXPrimitives';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/Card';

interface Conversation {
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
  lastActivity: string;
  replies: Reply[];
  resonance: number;
  axes: string[];
  isResolved: boolean;
  primaryGroupId: string;
  groupIds: string[];
  replyCount: number;
  hasUnread: boolean;
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

interface ConversationsLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

interface Group {
  id: string;
  name: string;
  description: string;
  threadCount: number;
  color: string;
  isDefault: boolean;
}

export function ConversationsLens({ controls = {}, userId, className = '', readOnly = false }: ConversationsLensProps) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const replyInputRef = useRef<HTMLInputElement>(null);
  
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [groups, setGroups] = useState<Group[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedConversation, setSelectedConversation] = useState<Conversation | null>(null);
  const [selectedGroup, setSelectedGroup] = useState<string | null>(null);
  const [showCreateConversation, setShowCreateConversation] = useState(false);
  const [showCreateGroup, setShowCreateGroup] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterAxis, setFilterAxis] = useState<string | null>(null);
  const [newConversation, setNewConversation] = useState({
    title: '',
    content: '',
    groupId: ''
  });
  const [newGroup, setNewGroup] = useState({
    name: '',
    description: '',
    color: '#3B82F6'
  });
  const [replyText, setReplyText] = useState('');
  const [replyLoading, setReplyLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);


  useEffect(() => {
    loadConversations();
    loadGroups();
  }, []);

  // Auto-select first conversation when list loads or filters change
  useEffect(() => {
    if (!selectedConversation && conversations.length > 0) {
      setSelectedConversation(conversations[0]);
    }
  }, [conversations]);

  const loadConversations = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(buildApiUrl('/threads/list'));
      if (response.ok) {
        const data = await response.json();
        if (data.threads && data.threads.length > 0) {
          setConversations(data.threads);
        } else {
          setConversations([]);
        }
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to load conversations');
        setConversations([]);
      }
    } catch (error) {
      console.error('Error loading conversations:', error);
      setError('Network error loading conversations');
      setConversations([]);
    } finally {
      setLoading(false);
    }
  };

  const loadGroups = async () => {
    setError(null);
    try {
      const response = await fetch(buildApiUrl('/threads/groups'));
      if (response.ok) {
        const data = await response.json();
        setGroups(data.groups || []);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to load groups');
      }
    } catch (err) {
      console.error('Error loading groups:', err);
      setError('Network error loading groups');
    }
  };

  const createConversation = async () => {
    if (!newConversation.title.trim() || !newConversation.content.trim() || !user?.id) {
      setError('Title and content are required');
      return;
    }

    setError(null);
    setSuccess(null);
    try {
      const payload: Record<string, any> = {
        title: newConversation.title,
        content: newConversation.content,
        authorId: user.id,
        axes: ['consciousness', 'unity'],
        groupId: newConversation.groupId || undefined
      };

      const response = await fetch(buildApiUrl('/threads/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          await loadConversations();
          setShowCreateConversation(false);
          setNewConversation({ title: '', content: '', groupId: '' });
          setSuccess('Conversation started successfully!');

          trackInteraction(data.threadId || 'new-conversation', 'create-conversation', {
            description: `User created conversation: ${newConversation.title}`
          });
        } else {
          setError(data.message || 'Failed to create conversation');
        }
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to create conversation');
      }
    } catch (error) {
      console.error('Error creating conversation:', error);
      setError('Network error creating conversation');
    }
  };

  const createGroup = async () => {
    if (!newGroup.name.trim() || !newGroup.description.trim()) {
      setError('Group name and description are required');
      return;
    }

    setError(null);
    setSuccess(null);
    try {
      // Mock group creation - in a real implementation, this would call an API
      const newGroupData: Group = {
        id: `group-${Date.now()}`,
        name: newGroup.name,
        description: newGroup.description,
        threadCount: 0,
        color: newGroup.color,
        isDefault: false
      };
      
      setGroups(prev => [...prev, newGroupData]);
      setShowCreateGroup(false);
      setNewGroup({ name: '', description: '', color: '#3B82F6' });
      setSuccess('Group created successfully!');
    } catch (error) {
      console.error('Error creating group:', error);
      setError('Failed to create group');
    }
  };

  const createReply = async () => {
    if (!replyText.trim() || !selectedConversation || !user?.id) {
      setError('Reply text is required');
      return;
    }

    setReplyLoading(true);
    setError(null);
    setSuccess(null);
    
    try {
      const payload = {
        threadId: selectedConversation.id,
        content: replyText,
        authorId: user.id
      };

      const response = await fetch(buildApiUrl('/threads/reply'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        const data = await response.json();
        if (data.success) {
          setReplyText('');
          await loadConversations();
          setSuccess('Reply sent successfully!');
          
          // Update the selected conversation with the new reply
          const updatedConversation = conversations.find(c => c.id === selectedConversation.id);
          if (updatedConversation) {
            setSelectedConversation(updatedConversation);
          }
        } else {
          setError(data.message || 'Failed to send reply');
        }
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to send reply');
      }
    } catch (error) {
      console.error('Error creating reply:', error);
      setError('Network error sending reply');
    } finally {
      setReplyLoading(false);
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

  const filteredConversations = conversations.filter(conversation => {
    const matchesSearch = !searchQuery || 
      conversation.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      conversation.content.toLowerCase().includes(searchQuery.toLowerCase()) ||
      conversation.author.name.toLowerCase().includes(searchQuery.toLowerCase());
    
    const matchesAxis = !filterAxis || conversation.axes.includes(filterAxis);
    
    const matchesGroup = !selectedGroup || conversation.primaryGroupId === selectedGroup;
    
    return matchesSearch && matchesAxis && matchesGroup;
  });

  useEffect(() => {
    if (filteredConversations.length > 0) {
      if (!selectedConversation || !filteredConversations.find(c => c.id === selectedConversation.id)) {
        setSelectedConversation(filteredConversations[0]);
      }
    } else {
      setSelectedConversation(null);
    }
  }, [searchQuery, filterAxis, selectedGroup]);

  const availableAxes = Array.from(new Set(conversations.flatMap(c => c.axes)));

  const clearMessages = () => {
    setError(null);
    setSuccess(null);
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
      {/* Header with Groups and Search */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center">
              💬 Conversations
            </h2>
            <p className="text-gray-600 dark:text-gray-300 mt-1">
              Meaningful discussions and collaborative exploration
            </p>
          </div>
          {!readOnly && (
            <div className="flex space-x-2">
              <button
                onClick={() => setShowCreateGroup(true)}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors flex items-center space-x-2"
              >
                <Users className="w-4 h-4" />
                <span>New Group</span>
              </button>
              <button
                onClick={() => setShowCreateConversation(true)}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors flex items-center space-x-2"
              >
                <Plus className="w-4 h-4" />
                <span>Start Conversation</span>
              </button>
            </div>
          )}
        </div>

        {/* Groups Navigation */}
        <div className="flex space-x-2 overflow-x-auto pb-2">
          <button
            onClick={() => setSelectedGroup(null)}
            className={`px-4 py-2 rounded-lg whitespace-nowrap transition-colors ${
              selectedGroup === null
                ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                : 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
            }`}
          >
            <Hash className="w-4 h-4 inline mr-2" />
            All Conversations
          </button>
          {groups.map((group) => (
            <button
              key={group.id}
              onClick={() => setSelectedGroup(group.id)}
              className={`px-4 py-2 rounded-lg whitespace-nowrap transition-colors flex items-center space-x-2 ${
                selectedGroup === group.id
                  ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                  : 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
              }`}
            >
              <div 
                className="w-3 h-3 rounded-full" 
                style={{ backgroundColor: group.color }}
              />
              <span>{group.name}</span>
              <span className="text-xs bg-gray-200 dark:bg-gray-700 px-2 py-0.5 rounded-full">
                {group.threadCount}
              </span>
            </button>
          ))}
        </div>

        {/* Search and Filter Controls */}
        <div className="flex space-x-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <input
              type="text"
              placeholder="Search conversations, authors, or content..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="relative">
            <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <select
              value={filterAxis || ''}
              onChange={(e) => setFilterAxis(e.target.value || null)}
              className="pl-10 pr-8 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Topics</option>
              {availableAxes.map((axis) => (
                <option key={axis} value={axis}>
                  {axis.charAt(0).toUpperCase() + axis.slice(1)}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Error and Success Messages */}
        {(error || success) && (
          <div className={`p-4 rounded-lg flex items-center justify-between ${
            error ? 'bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200' : 'bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-200'
          }`}>
            <div className="flex items-center space-x-2">
              <AlertCircle className="w-5 h-5" />
              <span>{error || success}</span>
            </div>
            <button
              onClick={clearMessages}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>

      {/* Create Group Modal */}
      {showCreateGroup && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <CardHeader className="pb-4">
              <CardTitle className="text-xl flex items-center space-x-2">
                <Users className="w-5 h-5" />
                <span>Create New Group</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Group Name</label>
                <input
                  type="text"
                  value={newGroup.name}
                  onChange={(e) => setNewGroup({ ...newGroup, name: e.target.value })}
                  placeholder="Enter group name..."
                  className="input-standard"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Description</label>
                <textarea
                  value={newGroup.description}
                  onChange={(e) => setNewGroup({ ...newGroup, description: e.target.value })}
                  placeholder="Describe the group's purpose..."
                  rows={3}
                  className="input-standard"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Color</label>
                <div className="flex space-x-2">
                  {['#3B82F6', '#10B981', '#8B5CF6', '#F59E0B', '#EF4444', '#06B6D4'].map((color) => (
                    <button
                      key={color}
                      onClick={() => setNewGroup({ ...newGroup, color })}
                      className={`w-8 h-8 rounded-full border-2 ${
                        newGroup.color === color ? 'border-gray-400' : 'border-gray-200'
                      }`}
                      style={{ backgroundColor: color }}
                    />
                  ))}
                </div>
              </div>
            </CardContent>
            <CardFooter className="flex space-x-3">
              <button
                onClick={createGroup}
                className="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-500 transition-colors"
              >
                Create Group
              </button>
              <button
                onClick={() => setShowCreateGroup(false)}
                className="flex-1 px-4 py-2 bg-slate-700 text-white rounded-md hover:bg-slate-600 transition-colors"
              >
                Cancel
              </button>
            </CardFooter>
          </Card>
        </div>
      )}

      {/* Create Conversation Modal */}
      {showCreateConversation && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <CardHeader className="pb-4">
              <CardTitle className="text-xl">Start New Conversation</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Group</label>
                <select
                  value={newConversation.groupId}
                  onChange={(e) => setNewConversation({ ...newConversation, groupId: e.target.value })}
                  className="input-standard"
                >
                  <option value="">Select a group (optional)</option>
                  {groups.map((group) => (
                    <option key={group.id} value={group.id}>
                      {group.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Title</label>
                <input
                  type="text"
                  value={newConversation.title}
                  onChange={(e) => setNewConversation({ ...newConversation, title: e.target.value })}
                  placeholder="What would you like to explore?"
                  className="input-standard"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Content</label>
                <textarea
                  value={newConversation.content}
                  onChange={(e) => setNewConversation({ ...newConversation, content: e.target.value })}
                  placeholder="Share your thoughts and invite discussion..."
                  rows={6}
                  className="input-standard"
                />
              </div>
            </CardContent>
            <CardFooter className="flex space-x-3">
              <button
                onClick={createConversation}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-500 transition-colors"
              >
                Start Conversation
              </button>
              <button
                onClick={() => setShowCreateConversation(false)}
                className="flex-1 px-4 py-2 bg-slate-700 text-white rounded-md hover:bg-slate-600 transition-colors"
              >
                Cancel
              </button>
            </CardFooter>
          </Card>
        </div>
      )}

      {/* Two-pane layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: conversation list */}
        <div className="lg:col-span-1 space-y-4">
          {filteredConversations.length === 0 ? (
            <div className="text-center py-12 text-gray-500 dark:text-gray-400">
              <div className="text-6xl mb-4">💬</div>
              <p>
                {searchQuery || filterAxis || selectedGroup
                  ? 'No conversations match your search criteria.' 
                  : 'No conversations available yet.'
                }
              </p>
              <p className="text-sm mt-2">
                {searchQuery || filterAxis || selectedGroup
                  ? 'Try adjusting your search or filters.' 
                  : 'Start the first conversation!'
                }
              </p>
              {(searchQuery || filterAxis || selectedGroup) && (
                <button
                  onClick={() => {
                    setSearchQuery('');
                    setFilterAxis(null);
                    setSelectedGroup(null);
                  }}
                  className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Clear Filters
                </button>
              )}
            </div>
          ) : (
            <div className="space-y-2">
              {filteredConversations.map((conversation) => {
                const isActive = selectedConversation?.id === conversation.id;
                const lastMessage = conversation.replies.length > 0 
                  ? conversation.replies[conversation.replies.length - 1]
                  : null;
                const previewText = lastMessage?.content || conversation.content;
                
                return (
                  <button
                    key={conversation.id}
                    onClick={() => setSelectedConversation(conversation)}
                    className={`w-full text-left p-3 rounded-lg border transition-colors ${
                      isActive
                        ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700'
                        : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'
                    }`}
                  >
                    <div className="flex items-start space-x-3">
                      <div className="flex-shrink-0">
                        <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white font-semibold text-sm">
                          {conversation.author.name.charAt(0).toUpperCase()}
                        </div>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between mb-1">
                          <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate">
                            {conversation.title}
                          </h3>
                          <div className="flex items-center space-x-1">
                            {conversation.hasUnread && (
                              <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                            )}
                            <span className="text-xs text-gray-500 dark:text-gray-400">
                              {formatTimeAgo(conversation.lastActivity)}
                            </span>
                          </div>
                        </div>
                        <p className="text-sm text-gray-600 dark:text-gray-300 truncate mb-1">
                          {lastMessage ? `${lastMessage.author.name}: ${previewText}` : previewText}
                        </p>
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-2 text-xs text-gray-500 dark:text-gray-400">
                            <span className="flex items-center space-x-1">
                              <MessageCircle className="w-3 h-3" />
                              <span>{conversation.replyCount}</span>
                            </span>
                            {conversation.isResolved && (
                              <span className="px-1.5 py-0.5 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400 text-xs rounded-full">
                                ✓ Resolved
                              </span>
                            )}
                          </div>
                          {conversation.axes.length > 0 && (
                            <div className="flex space-x-1">
                              {conversation.axes.slice(0, 2).map((axis) => (
                                <span
                                  key={axis}
                                  className={`px-1.5 py-0.5 text-xs rounded-full ${getAxisColor(axis)}`}
                                >
                                  {axis}
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Right: chat pane */}
        <div className="lg:col-span-2">
          <Card className="h-[65vh] lg:h-[70vh] flex flex-col">
            {/* Chat Header */}
            <CardHeader className="flex flex-row items-center justify-between pb-4 border-b border-gray-700/60">
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white font-semibold">
                  {selectedConversation ? selectedConversation.author.name.charAt(0).toUpperCase() : '💬'}
                </div>
                <div>
                  <CardTitle className="text-lg">{selectedConversation ? selectedConversation.title : 'Select a conversation'}</CardTitle>
                  {selectedConversation && (
                    <p className="text-sm text-muted">{selectedConversation.replyCount} messages</p>
                  )}
                </div>
              </div>
            </CardHeader>

            {/* Chat Messages */}
            <CardContent className="flex-1 overflow-y-auto p-4 space-y-4">
              {!selectedConversation ? (
                <div className="h-full w-full flex items-center justify-center text-muted">
                  Select a conversation to start chatting
                </div>
              ) : (
                <>
                  {/* Original Conversation Message */}
                  <div className="flex items-start space-x-3">
                    <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white font-semibold text-sm">
                      {selectedConversation.author.name.charAt(0).toUpperCase()}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-1">
                        <button 
                          onClick={() => window.open(`/node/${selectedConversation.author.id}`, '_blank')}
                          className="font-medium text-blue-600 dark:text-blue-400 hover:underline"
                        >
                          {selectedConversation.author.name}
                        </button>
                        <span className="text-xs text-muted">{formatTimeAgo(selectedConversation.createdAt)}</span>
                      </div>
                      <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3">
                        <p className="text-medium-contrast">{selectedConversation.content}</p>
                      </div>
                    </div>
                  </div>

                  {/* Replies */}
                  {selectedConversation.replies.map((reply) => (
                    <div key={reply.id} className="flex items-start space-x-3">
                      <div className="w-8 h-8 bg-gradient-to-br from-gray-500 to-gray-600 rounded-full flex items-center justify-center text-white font-semibold text-sm">
                        {reply.author.name.charAt(0).toUpperCase()}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center space-x-2 mb-1">
                          <button 
                            onClick={() => window.open(`/node/${reply.author.id}`, '_blank')}
                            className="font-medium text-blue-600 dark:text-blue-400 hover:underline"
                          >
                            {reply.author.name}
                          </button>
                          <span className="text-xs text-muted">{formatTimeAgo(reply.createdAt)}</span>
                          {reply.isAccepted && (
                            <span className="px-2 py-0.5 bg-emerald-500/10 text-emerald-300 text-xs rounded">✓ Accepted</span>
                          )}
                        </div>
                        <div className="bg-gray-100 dark:bg-gray-800 rounded-lg p-3">
                          <button 
                            onClick={() => window.open(`/node/${reply.id}`, '_blank')}
                            className="text-medium-contrast hover:text-blue-600 dark:hover:text-blue-400 block w-full text-left"
                          >
                            {reply.content}
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </>
              )}
            </CardContent>

            {/* Inline Composer */}
            {!readOnly && (
              <div className="p-4 border-t border-gray-700/60">
                <div className="flex items-center space-x-3">
                  <input
                    ref={replyInputRef}
                    type="text"
                    placeholder={selectedConversation ? "Type a reply..." : "Select a conversation to reply"}
                    value={replyText}
                    onChange={(e) => setReplyText(e.target.value)}
                    onKeyPress={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        if (selectedConversation) createReply();
                      }
                    }}
                    className="flex-1 px-3 py-2 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                    disabled={replyLoading || !selectedConversation}
                  />
                  <button 
                    onClick={createReply}
                    disabled={replyLoading || !replyText.trim() || !selectedConversation}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2"
                  >
                    {replyLoading ? (
                      <>
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                        <span>Sending...</span>
                      </>
                    ) : (
                      <>
                        <Reply className="w-4 h-4" />
                        <span>Send</span>
                      </>
                    )}
                  </button>
                </div>
                {error && (
                  <div className="mt-2 text-sm text-red-600 dark:text-red-400 flex items-center space-x-2">
                    <AlertCircle className="w-4 h-4" />
                    <span>{error}</span>
                  </div>
                )}
              </div>
            )}
            
            {readOnly && (
              <div className="p-4 border-t border-gray-700/60 bg-amber-50 dark:bg-amber-900/20">
                <div className="text-center text-amber-700 dark:text-amber-300 text-sm">
                  <span className="inline-flex items-center space-x-2">
                    <span>👁️</span>
                    <span>Sign in to participate in conversations</span>
                  </span>
                </div>
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Thread Detail Modal removed in favor of inline two-pane chat */}
    </div>
  );
}
