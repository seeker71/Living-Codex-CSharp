'use client';

import { useState, useEffect, useRef } from 'react';
import { MessageCircle, Reply, Heart, Share2, Clock, Users, Hash, Filter, Search, Plus, X, AlertCircle, Smile, MoreHorizontal, Edit3, Trash2, Pin, Copy } from 'lucide-react';
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
  reactions?: Record<string, string[]>;
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
  reactions?: Record<string, string[]>;
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

function ThreadsLens({ controls = {}, userId, className = '', readOnly = false }: ConversationsLensProps) {
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
  const [showEmojiPicker, setShowEmojiPicker] = useState<string | null>(null);
  const [reactionLoading, setReactionLoading] = useState(false);
  const [typingUsers, setTypingUsers] = useState<Set<string>>(new Set());
  const [messageStatuses, setMessageStatuses] = useState<Map<string, 'sent' | 'delivered' | 'read'>>(new Map());
  const [showMessageActions, setShowMessageActions] = useState<string | null>(null);
  const [editingMessage, setEditingMessage] = useState<string | null>(null);
  const [editText, setEditText] = useState('');
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [hasMoreConversations, setHasMoreConversations] = useState(true);
  const [conversationPage, setConversationPage] = useState(1);
  const [visibleConversations, setVisibleConversations] = useState<Conversation[]>([]);
  const [focusedConversationIndex, setFocusedConversationIndex] = useState(0);


  useEffect(() => {
    loadConversations();
    loadGroups();
  }, []);

  // Virtual scrolling implementation
  useEffect(() => {
    const visibleCount = Math.min(50, conversations.length);
    setVisibleConversations(conversations.slice(0, visibleCount));
    setHasMoreConversations(conversations.length > visibleCount);
  }, [conversations]);

  // Auto-select first conversation when list loads or filters change
  useEffect(() => {
    if (!selectedConversation && visibleConversations.length > 0) {
      setSelectedConversation(visibleConversations[0]);
    }
  }, [visibleConversations]);

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
      console.error('Threads API Error:', error);
      setError(`Threads API Error: ${error instanceof Error ? error.message : 'Unknown error'}`);
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
      console.error('Threads Groups API Error:', err);
      setError(`Threads Groups API Error: ${err instanceof Error ? err.message : 'Unknown error'}`);
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
      const response = await fetch('http://localhost:5002/threads/groups/create', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: newGroup.name,
          description: newGroup.description,
          color: newGroup.color
        })
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      
      if (data.success && data.group) {
        const newGroupData: Group = {
          id: data.group.id,
          name: data.group.name,
          description: data.group.description,
          threadCount: data.group.threadCount || 0,
          color: data.group.color,
          isDefault: data.group.isDefault || false
        };
        
        setGroups(prev => [...prev, newGroupData]);
        setShowCreateGroup(false);
        setNewGroup({ name: '', description: '', color: '#3B82F6' });
        setSuccess('Group created successfully!');
      } else {
        throw new Error(data.error || 'Failed to create group');
      }
    } catch (error) {
      console.error('Error creating group:', error);
      setError(error instanceof Error ? error.message : 'Failed to create group');
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

  const addReaction = async (messageId: string, emoji: string) => {
    if (!user?.id) return;

    setReactionLoading(true);
    try {
      const response = await fetch(buildApiUrl(`/threads/${messageId}/reactions`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emoji, authorId: user.id })
      });

      if (response.ok) {
        await loadConversations();
        setShowEmojiPicker(null);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to add reaction');
      }
    } catch (error) {
      console.error('Error adding reaction:', error);
      setError('Network error adding reaction');
    } finally {
      setReactionLoading(false);
    }
  };

  const removeReaction = async (messageId: string, emoji: string) => {
    if (!user?.id) return;

    setReactionLoading(true);
    try {
      const response = await fetch(buildApiUrl(`/threads/${messageId}/reactions/${encodeURIComponent(emoji)}?authorId=${user.id}`), {
        method: 'DELETE'
      });

      if (response.ok) {
        await loadConversations();
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to remove reaction');
      }
    } catch (error) {
      console.error('Error removing reaction:', error);
      setError('Network error removing reaction');
    } finally {
      setReactionLoading(false);
    }
  };

  const editMessage = async (messageId: string, newContent: string) => {
    if (!user?.id) return;

    try {
      const response = await fetch(buildApiUrl(`/threads/${messageId}/edit`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: newContent, authorId: user.id })
      });

      if (response.ok) {
        await loadConversations();
        setEditingMessage(null);
        setEditText('');
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to edit message');
      }
    } catch (error) {
      console.error('Error editing message:', error);
      setError('Network error editing message');
    }
  };

  const deleteMessage = async (messageId: string) => {
    if (!user?.id || !confirm('Are you sure you want to delete this message?')) return;

    try {
      const response = await fetch(buildApiUrl(`/threads/${messageId}`), {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ authorId: user.id })
      });

      if (response.ok) {
        await loadConversations();
        setShowMessageActions(null);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to delete message');
      }
    } catch (error) {
      console.error('Error deleting message:', error);
      setError('Network error deleting message');
    }
  };

  const pinMessage = async (messageId: string) => {
    if (!user?.id) return;

    try {
      const response = await fetch(buildApiUrl(`/threads/${messageId}/pin`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ authorId: user.id })
      });

      if (response.ok) {
        await loadConversations();
        setShowMessageActions(null);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Failed to pin message');
      }
    } catch (error) {
      console.error('Error pinning message:', error);
      setError('Network error pinning message');
    }
  };

  const copyMessageText = (content: string) => {
    navigator.clipboard.writeText(content);
    setShowMessageActions(null);
    setSuccess('Message copied to clipboard!');
    setTimeout(() => setSuccess(null), 2000);
  };

  // Keyboard shortcuts and accessibility
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Only handle shortcuts when not in input fields
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      switch (e.key) {
        case 'j':
        case 'ArrowDown':
          e.preventDefault();
          if (focusedConversationIndex < visibleConversations.length - 1) {
            setFocusedConversationIndex(prev => prev + 1);
          }
          break;
        case 'k':
        case 'ArrowUp':
          e.preventDefault();
          if (focusedConversationIndex > 0) {
            setFocusedConversationIndex(prev => prev - 1);
          }
          break;
        case 'Enter':
          e.preventDefault();
          if (visibleConversations[focusedConversationIndex]) {
            setSelectedConversation(visibleConversations[focusedConversationIndex]);
          }
          break;
        case 'n':
          e.preventDefault();
          setShowCreateConversation(true);
          break;
        case 'g':
          e.preventDefault();
          setShowCreateGroup(true);
          break;
        case 'Escape':
          e.preventDefault();
          setShowMessageActions(null);
          setShowEmojiPicker(null);
          setEditingMessage(null);
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [focusedConversationIndex, visibleConversations]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div
          className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"
          data-testid="loading-spinner"
        ></div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Enhanced Header - Production Quality */}
      <div className="space-y-6">
        {/* Main Header */}
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div className="flex-1">
            <div className="flex items-center space-x-3 mb-2">
              <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-xl">
                <MessageCircle className="w-6 h-6 text-blue-600 dark:text-blue-400" />
              </div>
          <div>
                <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 tracking-tight">
                  Conversations
                </h1>
                <p className="text-gray-600 dark:text-gray-400 text-sm mt-0.5">
              Meaningful discussions and collaborative exploration
            </p>
          </div>
            </div>
          </div>

          {/* Action Buttons */}
          {!readOnly && (
            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => setShowCreateGroup(true)}
                className="inline-flex items-center px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-medium rounded-xl transition-all duration-200 shadow-sm hover:shadow-md transform hover:-translate-y-0.5"
              >
                <Users className="w-4 h-4 mr-2" />
                <span>New Group</span>
              </button>
              <button
                onClick={() => setShowCreateConversation(true)}
                className="inline-flex items-center px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl transition-all duration-200 shadow-sm hover:shadow-md transform hover:-translate-y-0.5"
              >
                <Plus className="w-4 h-4 mr-2" />
                <span>Start Conversation</span>
              </button>
            </div>
          )}
        </div>

        {/* Online Status & Quick Stats */}
        <div className="flex flex-wrap items-center justify-between gap-4 py-3 px-4 bg-gray-50 dark:bg-gray-800/50 rounded-2xl border border-gray-200 dark:border-gray-700">
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-2">
              <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse" aria-label="System online"></div>
              <span className="text-sm text-gray-600 dark:text-gray-300 font-medium">
                {conversations.length} active conversations
              </span>
            </div>
            <div className="hidden sm:flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
              <Clock className="w-4 h-4" aria-label="Last update time" />
              <span>Last updated: {new Date().toLocaleTimeString()}</span>
            </div>
          </div>

          {/* Keyboard Shortcuts Help */}
          <div className="flex items-center space-x-2">
            <button
              className="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 px-2 py-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              title="Keyboard shortcuts: j/k (navigate), Enter (select), n (new conversation), g (new group), Esc (close menus)"
            >
              ⌨️ Help
            </button>
            <div className="text-xs text-gray-400 dark:text-gray-500 hidden lg:block">
              j/k: navigate • Enter: select • n: new • g: group • Esc: close
            </div>
          </div>

          {/* Pinned Conversations Preview */}
          {conversations.filter(c => c.isResolved).length > 0 && (
            <div className="flex items-center space-x-2">
              <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">
                {conversations.filter(c => c.isResolved).length} resolved
              </span>
              <div className="flex -space-x-1">
                {conversations.filter(c => c.isResolved).slice(0, 3).map((conv, idx) => (
                  <div
                    key={conv.id}
                    className="w-6 h-6 bg-gradient-to-br from-emerald-400 to-emerald-600 rounded-full border-2 border-white dark:border-gray-800 flex items-center justify-center text-white text-xs font-semibold"
                    style={{ zIndex: 3 - idx }}
                  >
                    ✓
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Groups Navigation - Enhanced Visual Design */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Groups</h3>
            <button
              onClick={() => setShowCreateGroup(true)}
              className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium"
            >
              + New Group
            </button>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-2">
            {/* All Conversations Card */}
          <button
            onClick={() => setSelectedGroup(null)}
              className={`group p-3 rounded-xl border-2 transition-all duration-200 text-left ${
              selectedGroup === null
                  ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700 shadow-sm'
                  : 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              <div className="flex items-center space-x-2 mb-2">
                <div className="w-8 h-8 bg-gradient-to-br from-gray-400 to-gray-600 rounded-lg flex items-center justify-center">
                  <Hash className="w-4 h-4 text-white" />
                </div>
                <div className="flex-1 min-w-0">
                  <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">All Conversations</h4>
                  <p className="text-xs text-gray-500 dark:text-gray-400">{conversations.length} total</p>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-xs text-gray-600 dark:text-gray-400">View all</span>
                <div className="w-2 h-2 bg-gray-400 rounded-full"></div>
              </div>
          </button>

            {/* Group Cards */}
          {groups.map((group) => (
            <button
              key={group.id}
              onClick={() => setSelectedGroup(group.id)}
                className={`group p-3 rounded-xl border-2 transition-all duration-200 text-left relative overflow-hidden ${
                selectedGroup === group.id
                    ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700 shadow-sm transform scale-105'
                    : 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 hover:border-gray-300 dark:hover:border-gray-600 hover:shadow-md'
              }`}
            >
                {/* Color indicator */}
              <div 
                  className="absolute top-0 left-0 w-full h-1"
                style={{ backgroundColor: group.color }}
              />

                <div className="flex items-center space-x-2 mb-2">
                  <div
                    className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-semibold shadow-sm"
                    style={{ backgroundColor: group.color }}
                  >
                    {group.name.charAt(0).toUpperCase()}
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{group.name}</h4>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{group.threadCount} threads</p>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-xs text-gray-600 dark:text-gray-400">Active</span>
                  {group.threadCount > 0 && (
                    <div className="flex -space-x-1">
                      {Array.from({ length: Math.min(group.threadCount, 3) }).map((_, idx) => (
                        <div
                          key={idx}
                          className="w-2 h-2 bg-green-400 rounded-full border border-white dark:border-gray-800"
                          style={{ zIndex: 3 - idx }}
                        />
                      ))}
                    </div>
                  )}
                </div>
            </button>
          ))}
          </div>
        </div>

        {/* Search and Filter Controls - Telegram style */}
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
            <input
              type="text"
              placeholder="🔍 Search conversations..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-11 pr-4 py-3 border border-gray-200 dark:border-gray-700 rounded-2xl bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm placeholder-gray-500"
            />
            {searchQuery && (
              <button
                onClick={() => setSearchQuery('')}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                aria-label="Clear search"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>
          <div className="relative">
            <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <select
              value={filterAxis || ''}
              onChange={(e) => setFilterAxis(e.target.value || null)}
              className="pl-10 pr-8 py-3 border border-gray-200 dark:border-gray-700 rounded-2xl bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm min-w-[140px]"
              hidden
            >
              <option value="">📂 All Topics</option>
              {availableAxes.filter(Boolean).map((axis) => (
                <option key={axis} value={axis}>
                  {axis?.charAt(0).toUpperCase() + axis?.slice(1)}
                </option>
              ))}
            </select>
            {filterAxis && (
              <button
                onClick={() => setFilterAxis(null)}
                className="absolute right-2 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                aria-label="Clear filter"
              >
                <X className="w-3 h-3" />
              </button>
            )}
          </div>
        </div>

        {/* Error and Success Messages */}
        {(error || success) && (
          <div className={`p-4 rounded-lg flex items-center justify-between ${
            error ? 'bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200' : 'bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-200'
          }`}>
            <div className="flex items-center space-x-2">
              <AlertCircle className="w-5 h-5" />
              <span data-testid="error-message">{error || success}</span>
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

      {/* Two-pane layout - Telegram style responsive */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 lg:gap-6">
        {/* Left: conversation list - Telegram style with animations */}
        <div className="lg:col-span-1 space-y-1 max-h-[60vh] lg:max-h-[70vh] overflow-y-auto order-2 lg:order-1">
          {filteredConversations.length === 0 ? (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400 animate-fade-in">
              <div className="text-4xl mb-3 animate-bounce">💬</div>
              <p className="text-sm animate-pulse">
                {searchQuery || filterAxis || selectedGroup
                  ? 'No conversations match your search criteria.' 
                  : 'No conversations available yet.'
                }
              </p>
              <p className="text-xs mt-1 text-gray-400 dark:text-gray-500">
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
                  className="mt-3 px-3 py-1.5 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700 transition-all duration-200 transform hover:scale-105"
                >
                  Clear Filters
                </button>
              )}
            </div>
          ) : (
            <div className="space-y-0.5">
              {visibleConversations.map((conversation, index) => {
                const isActive = selectedConversation?.id === conversation.id;
                const lastMessage = conversation.replies.length > 0 
                  ? conversation.replies[conversation.replies.length - 1]
                  : null;
                const previewText = lastMessage?.content || conversation.content;
                const displayName = lastMessage?.author.name || conversation.author.name;
                
                return (
                  <button
                    key={conversation.id}
                    onClick={() => setSelectedConversation(conversation)}
                    className={`w-full text-left p-2.5 rounded-lg border transition-all duration-300 transform hover:scale-[1.02] animate-slide-in-up focus:outline-none focus:ring-2 focus:ring-blue-500 ${
                      isActive
                        ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700 shadow-lg scale-105'
                        : conversation.hasUnread
                        ? 'bg-blue-25 dark:bg-blue-900/10 border-blue-200 dark:border-blue-800 shadow-md hover:shadow-lg'
                        : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-600 hover:shadow-md'
                    } ${focusedConversationIndex === index ? 'ring-2 ring-blue-500' : ''}`}
                    style={{ animationDelay: `${index * 50}ms` }}
                    aria-label={`Select conversation: ${conversation.title} by ${conversation.author.name}${conversation.hasUnread ? ' (unread)' : ''}`}
                    aria-pressed={isActive}
                    role="option"
                    tabIndex={focusedConversationIndex === index ? 0 : -1}
                  >
                    <div className="flex items-start space-x-2.5">
                      <div className="flex-shrink-0 relative">
                        <div className={`w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold text-sm transition-all duration-200 ${
                          conversation.hasUnread
                            ? 'bg-gradient-to-br from-blue-500 to-blue-600 ring-2 ring-blue-200 dark:ring-blue-800'
                            : 'bg-gradient-to-br from-gray-400 to-gray-500'
                        }`}>
                          {conversation.author.name.charAt(0).toUpperCase()}
                        </div>
                        {conversation.hasUnread && (
                          <div className="absolute -bottom-0.5 -right-0.5 w-4 h-4 bg-blue-500 rounded-full border-2 border-white dark:border-gray-900 flex items-center justify-center">
                            <div className="w-2 h-2 bg-white rounded-full"></div>
                          </div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between mb-0.5">
                          <h3 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                            {conversation.title}
                          </h3>
                          <div className="flex items-center space-x-1">
                            <span className="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">
                              {formatTimeAgo(conversation.lastActivity)}
                            </span>
                          </div>
                        </div>
                        <div className="flex items-center justify-between">
                          <p className="text-sm text-gray-600 dark:text-gray-300 truncate flex-1 mr-2">
                            <span className="font-medium text-gray-800 dark:text-gray-200">{displayName}: </span>
                            <span>{previewText}</span>
                          </p>
                          <div className="flex items-center space-x-1">
                            <span className="flex items-center space-x-1 text-xs text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded-full">
                              <MessageCircle className="w-3 h-3" />
                              <span>{conversation.replyCount}</span>
                            </span>
                            {conversation.isResolved && (
                              <span className="px-1.5 py-0.5 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400 text-xs rounded-full">
                                ✓
                              </span>
                            )}
                          </div>
                          </div>
                          {conversation.axes.length > 0 && (
                          <div className="flex space-x-1 mt-1">
                            {conversation.axes.slice(0, 3).map((axis) => (
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
                  </button>
                );
              })}

              {/* Load More Button */}
              {hasMoreConversations && (
                <div className="pt-4 text-center">
                  <button
                    onClick={() => {
                      const nextCount = Math.min(50, conversations.length - visibleConversations.length);
                      const newVisible = conversations.slice(0, visibleConversations.length + nextCount);
                      setVisibleConversations(newVisible);
                      setHasMoreConversations(conversations.length > newVisible.length);
                    }}
                    className="px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-sm rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
                  >
                    Load More Conversations
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right: chat pane - Mobile first */}
        <div className="lg:col-span-2 order-1 lg:order-2">
          <Card className="h-[70vh] lg:h-[75vh] flex flex-col">
            {/* Chat Header - Mobile optimized */}
            <CardHeader className="flex flex-row items-center justify-between pb-3 px-4 lg:px-6 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center space-x-3 min-w-0 flex-1">
                <div className="w-8 h-8 lg:w-10 lg:h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white font-semibold text-sm lg:text-base">
                  {selectedConversation ? selectedConversation.author.name.charAt(0).toUpperCase() : '💬'}
                </div>
                <div className="min-w-0 flex-1">
                  <CardTitle className="text-base lg:text-lg truncate">{selectedConversation ? selectedConversation.title : 'Select a conversation'}</CardTitle>
                  {selectedConversation && (
                    <p className="text-xs lg:text-sm text-gray-500 dark:text-gray-400 truncate">{selectedConversation.replyCount} messages</p>
                  )}
                </div>
              </div>
            </CardHeader>

            {/* Chat Messages - Telegram style with Real-time Features */}
            <CardContent className="flex-1 overflow-y-auto p-3 space-y-2 bg-gray-50 dark:bg-gray-900/50 relative">
              {/* Typing Indicator */}
              {typingUsers.size > 0 && (
                <div className="flex justify-start mb-2">
                  <div className="bg-white dark:bg-gray-800 rounded-2xl rounded-tl-md px-4 py-2 shadow-sm border border-gray-200 dark:border-gray-700">
                    <div className="flex items-center space-x-2">
                      <div className="flex space-x-1">
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
                      </div>
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        {Array.from(typingUsers).join(', ')} {typingUsers.size === 1 ? 'is' : 'are'} typing...
                      </span>
                    </div>
                  </div>
                </div>
              )}

              {!selectedConversation ? (
                <div className="h-full w-full flex items-center justify-center text-gray-500 dark:text-gray-400">
                  <div className="text-center">
                    <div className="text-4xl mb-2">💬</div>
                    <p className="text-sm">Select a conversation to start chatting</p>
                  </div>
                </div>
              ) : (
                <>
                  {/* Original Conversation Message */}
                  <div className="group flex justify-start hover:bg-gray-50 dark:hover:bg-gray-800/50 -mx-2 px-2 py-1 rounded-lg transition-colors">
                    <div className="max-w-[85%] sm:max-w-[70%]">
                      <div className="flex items-center justify-between mb-1">
                        <div className="flex items-center space-x-2">
                          <div className="w-6 h-6 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white font-semibold text-xs">
                      {selectedConversation.author.name.charAt(0).toUpperCase()}
                    </div>
                        <button 
                          onClick={() => window.open(`/node/${selectedConversation.author.id}`, '_blank')}
                            className="font-medium text-blue-600 dark:text-blue-400 hover:underline text-sm"
                        >
                          {selectedConversation.author.name}
                        </button>
                      </div>
                        <div className="flex items-center space-x-2">
                          <span className="text-xs text-gray-500 dark:text-gray-400">{formatTimeAgo(selectedConversation.createdAt)}</span>
                          {/* Message status indicator for original message */}
                          <div className="flex items-center space-x-1">
                            <div className="w-3 h-3 bg-blue-500 rounded-full"></div>
                            <div className="w-3 h-3 bg-blue-500 rounded-full"></div>
                          </div>
                        </div>
                      </div>
                      <div className="bg-white dark:bg-gray-800 rounded-2xl rounded-tl-md px-3 py-2 shadow-sm border border-gray-200 dark:border-gray-700 relative">
                        {editingMessage === selectedConversation.id ? (
                          <div className="space-y-2">
                            <textarea
                              value={editText}
                              onChange={(e) => setEditText(e.target.value)}
                              className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded resize-none focus:outline-none focus:ring-2 focus:ring-blue-500"
                              rows={3}
                              placeholder="Edit your message..."
                            />
                            <div className="flex space-x-2">
                              <button
                                onClick={() => editMessage(selectedConversation.id, editText)}
                                className="px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
                              >
                                Save
                              </button>
                              <button
                                onClick={() => {
                                  setEditingMessage(null);
                                  setEditText('');
                                }}
                                className="px-3 py-1 bg-gray-300 dark:bg-gray-600 text-gray-700 dark:text-gray-300 text-sm rounded hover:bg-gray-400 dark:hover:bg-gray-500"
                              >
                                Cancel
                              </button>
                            </div>
                          </div>
                        ) : (
                          <p className="text-gray-900 dark:text-gray-100 text-sm leading-relaxed">{selectedConversation.content}</p>
                        )}

                        {/* Reactions Display for main message */}
                        {selectedConversation.reactions && Object.keys(selectedConversation.reactions).length > 0 && (
                          <div className="flex flex-wrap gap-1 mt-2">
                            {Object.entries(selectedConversation.reactions).map(([emoji, users]) => (
                              <button
                                key={emoji}
                                onClick={() => users.includes(user?.id || '') ? removeReaction(selectedConversation.id, emoji) : addReaction(selectedConversation.id, emoji)}
                                className={`inline-flex items-center space-x-1 px-2 py-1 text-xs rounded-full transition-colors ${
                                  users.includes(user?.id || '')
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                                    : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
                                }`}
                                disabled={reactionLoading}
                              >
                                <span className="text-sm">{emoji}</span>
                                <span className="text-xs font-medium">{users.length}</span>
                              </button>
                            ))}
                          </div>
                        )}

                        {/* Advanced Message Actions - Telegram style */}
                        <div className="absolute -right-10 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col space-y-1">
                          <button
                            onClick={() => {
                              setReplyText(`@${selectedConversation.author.name} `);
                              replyInputRef.current?.focus();
                            }}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="Reply"
                          >
                            <Reply className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                          <button
                            onClick={() => setShowEmojiPicker(selectedConversation.id)}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="Add Reaction"
                          >
                            <Smile className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                          <button
                            onClick={() => setShowMessageActions(showMessageActions === selectedConversation.id ? null : selectedConversation.id)}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="More Actions"
                          >
                            <MoreHorizontal className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                        </div>

                        {/* Advanced Actions Menu for main message */}
                        {showMessageActions === selectedConversation.id && (
                          <div className="absolute top-0 right-0 z-10 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-1 min-w-[140px]">
                            <button
                              onClick={() => {
                                setEditingMessage(selectedConversation.id);
                                setEditText(selectedConversation.content);
                                setShowMessageActions(null);
                              }}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Edit3 className="w-4 h-4" />
                              <span>Edit</span>
                            </button>
                            <button
                              onClick={() => copyMessageText(selectedConversation.content)}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Copy className="w-4 h-4" />
                              <span>Copy</span>
                            </button>
                            <button
                              onClick={() => pinMessage(selectedConversation.id)}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Pin className="w-4 h-4" />
                              <span>Pin</span>
                            </button>
                            <button
                              onClick={() => {
                                if (navigator.share) {
                                  navigator.share({
                                    title: selectedConversation.title,
                                    text: selectedConversation.content,
                                    url: window.location.href
                                  });
                                }
                              }}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Share2 className="w-4 h-4" />
                              <span>Forward</span>
                            </button>
                            <div className="border-t border-gray-200 dark:border-gray-600 my-1"></div>
                            <button
                              onClick={() => deleteMessage(selectedConversation.id)}
                              className="w-full text-left px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 flex items-center space-x-2"
                            >
                              <Trash2 className="w-4 h-4" />
                              <span>Delete</span>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Replies */}
                  {selectedConversation.replies.map((reply) => (
                    <div key={reply.id} className="group flex justify-start hover:bg-gray-50 dark:hover:bg-gray-800/50 -mx-2 px-2 py-1 rounded-lg transition-colors">
                      <div className="max-w-[85%] sm:max-w-[70%]">
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-2">
                            <div className="w-6 h-6 bg-gradient-to-br from-gray-500 to-gray-600 rounded-full flex items-center justify-center text-white font-semibold text-xs">
                        {reply.author.name.charAt(0).toUpperCase()}
                      </div>
                          <button 
                            onClick={() => window.open(`/node/${reply.author.id}`, '_blank')}
                              className="font-medium text-blue-600 dark:text-blue-400 hover:underline text-sm"
                          >
                            {reply.author.name}
                          </button>
                          </div>
                          <div className="flex items-center space-x-2">
                            <span className="text-xs text-gray-500 dark:text-gray-400">{formatTimeAgo(reply.createdAt)}</span>
                          {reply.isAccepted && (
                              <span className="px-1.5 py-0.5 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 text-xs rounded-full">✓</span>
                            )}
                            {/* Enhanced Message status indicator */}
                            <div className="flex items-center space-x-1">
                              {messageStatuses.get(reply.id) === 'read' && (
                                <>
                                  <div className="w-3 h-3 bg-blue-500 rounded-full"></div>
                                  <div className="w-3 h-3 bg-blue-500 rounded-full"></div>
                                </>
                              )}
                              {messageStatuses.get(reply.id) === 'delivered' && (
                                <>
                                  <div className="w-3 h-3 bg-gray-400 rounded-full"></div>
                                  <div className="w-3 h-3 bg-gray-400 rounded-full"></div>
                                </>
                              )}
                              {(!messageStatuses.get(reply.id) || messageStatuses.get(reply.id) === 'sent') && (
                                <>
                                  <div className="w-3 h-3 bg-gray-300 rounded-full"></div>
                                  <div className="w-3 h-3 bg-gray-300 rounded-full opacity-60"></div>
                                </>
                          )}
                        </div>
                          </div>
                        </div>
                        <div className="bg-white dark:bg-gray-800 rounded-2xl rounded-tl-md px-3 py-2 shadow-sm border border-gray-200 dark:border-gray-700 relative transition-all duration-200 hover:shadow-md">
                          <button 
                            onClick={() => window.open(`/node/${reply.id}`, '_blank')}
                            className="text-gray-900 dark:text-gray-100 hover:text-blue-600 dark:hover:text-blue-400 block w-full text-left text-sm leading-relaxed transition-colors duration-200"
                          >
                            {reply.content}
                          </button>

                        {/* Animated Reactions Display */}
                        {reply.reactions && Object.keys(reply.reactions).length > 0 && (
                          <div className="flex flex-wrap gap-1 mt-2 animate-fade-in-up">
                            {Object.entries(reply.reactions).map(([emoji, users]) => (
                              <button
                                key={emoji}
                                onClick={() => users.includes(user?.id || '') ? removeReaction(reply.id, emoji) : addReaction(reply.id, emoji)}
                                className={`inline-flex items-center space-x-1 px-2 py-1 text-xs rounded-full transition-all duration-200 transform hover:scale-110 ${
                                  users.includes(user?.id || '')
                                    ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 shadow-sm'
                                    : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 hover:shadow-sm'
                                } ${reactionLoading ? 'animate-pulse' : ''}`}
                                disabled={reactionLoading}
                              >
                                <span className="text-sm animate-bounce" style={{ animationDelay: '0.1s' }}>{emoji}</span>
                                <span className="text-xs font-medium">{users.length}</span>
                              </button>
                            ))}
                        </div>
                        )}

                        {/* Advanced Message Actions - Telegram style */}
                        <div className="absolute -right-10 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col space-y-1">
                          <button
                            onClick={() => {
                              setReplyText(`@${reply.author.name} `);
                              replyInputRef.current?.focus();
                            }}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="Reply"
                          >
                            <Reply className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                          <button
                            onClick={() => setShowEmojiPicker(reply.id)}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="Add Reaction"
                          >
                            <Smile className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                          <button
                            onClick={() => setShowMessageActions(showMessageActions === reply.id ? null : reply.id)}
                            className="w-8 h-8 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-full flex items-center justify-center transition-colors"
                            title="More Actions"
                          >
                            <MoreHorizontal className="w-3 h-3 text-gray-600 dark:text-gray-300" />
                          </button>
                        </div>

                        {/* Advanced Actions Menu */}
                        {showMessageActions === reply.id && (
                          <div className="absolute top-0 right-0 z-10 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-1 min-w-[140px]">
                            <button
                              onClick={() => {
                                setEditingMessage(reply.id);
                                setEditText(reply.content);
                                setShowMessageActions(null);
                              }}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Edit3 className="w-4 h-4" />
                              <span>Edit</span>
                            </button>
                            <button
                              onClick={() => copyMessageText(reply.content)}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Copy className="w-4 h-4" />
                              <span>Copy</span>
                            </button>
                            <button
                              onClick={() => pinMessage(reply.id)}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Pin className="w-4 h-4" />
                              <span>Pin</span>
                            </button>
                            <button
                              onClick={() => {
                                if (navigator.share) {
                                  navigator.share({
                                    title: selectedConversation.title,
                                    text: reply.content,
                                    url: window.location.href
                                  });
                                }
                              }}
                              className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center space-x-2"
                            >
                              <Share2 className="w-4 h-4" />
                              <span>Forward</span>
                            </button>
                            <div className="border-t border-gray-200 dark:border-gray-600 my-1"></div>
                            <button
                              onClick={() => deleteMessage(reply.id)}
                              className="w-full text-left px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 flex items-center space-x-2"
                            >
                              <Trash2 className="w-4 h-4" />
                              <span>Delete</span>
                            </button>
                          </div>
                        )}
                      </div>
                      </div>
                    </div>
                  ))}
                </>
              )}
            </CardContent>

            {/* Enhanced Composer - Telegram style with animations */}
            {!readOnly && (
              <div className="p-3 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 transition-all duration-200">
                <div className="flex items-end space-x-2">
                  <div className="flex-1 relative">
                  <input
                    ref={replyInputRef}
                    type="text"
                      placeholder={selectedConversation ? "Write a message..." : "Select a conversation to reply"}
                    value={replyText}
                    onChange={(e) => setReplyText(e.target.value)}
                    onKeyPress={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        if (selectedConversation) createReply();
                      }
                    }}
                      className="w-full px-3 py-2.5 bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl rounded-br-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 resize-none transition-all duration-200 focus:scale-[1.02] hover:bg-gray-50 dark:hover:bg-gray-700"
                    disabled={replyLoading || !selectedConversation}
                  />
                    {replyText && (
                      <div className="absolute right-2 top-1/2 -translate-y-1/2 text-xs text-gray-400">
                        Press Enter to send
                      </div>
                    )}
                  </div>
                  <button 
                    onClick={createReply}
                    disabled={replyLoading || !replyText.trim() || !selectedConversation}
                    className={`px-3 py-2 rounded-full transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center min-w-[40px] h-10 transform ${
                      replyText.trim() && selectedConversation
                        ? 'bg-blue-600 hover:bg-blue-700 text-white shadow-lg hover:shadow-xl hover:scale-110'
                        : 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400'
                    }`}
                  >
                    {replyLoading ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
                    ) : (
                      <Reply className={`w-4 h-4 transition-transform duration-200 ${replyText.trim() && selectedConversation ? 'animate-pulse' : ''}`} />
                    )}
                  </button>
                </div>
                {error && (
                  <div className="mt-2 text-sm text-red-600 dark:text-red-400 flex items-center space-x-2 animate-slide-in-down">
                    <AlertCircle className="w-4 h-4 animate-bounce" />
                    <span>{error}</span>
                  </div>
                )}
              </div>
            )}
            
            {readOnly && (
              <div className="p-3 border-t border-gray-200 dark:border-gray-700 bg-amber-50 dark:bg-amber-900/20">
                <div className="text-center text-amber-700 dark:text-amber-300 text-sm">
                  <span className="inline-flex items-center space-x-2">
                    <span>🔒</span>
                    <span>Sign in to participate in conversations</span>
                  </span>
                </div>
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Emoji Picker */}
      {showEmojiPicker && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-2xl p-4 max-w-sm w-full mx-4 shadow-xl">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Add Reaction</h3>
              <button
                onClick={() => setShowEmojiPicker(null)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="grid grid-cols-6 gap-2">
              {['👍', '❤️', '😂', '😮', '😢', '😡', '🔥', '🎉', '👏', '🤔', '💯', '🙌'].map((emoji) => (
                <button
                  key={emoji}
                  onClick={() => addReaction(showEmojiPicker, emoji)}
                  className="w-12 h-12 text-2xl hover:bg-gray-100 dark:hover:bg-gray-700 rounded-xl transition-colors flex items-center justify-center"
                  disabled={reactionLoading}
                >
                  {emoji}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Thread Detail Modal removed in favor of inline two-pane chat */}
    </div>
  );
}

export default ThreadsLens;
