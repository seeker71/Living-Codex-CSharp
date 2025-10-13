'use client';

import { useState } from 'react';
import { useConceptDiscussions, useDiscussionReplies, useCreateDiscussion, useReplyToDiscussion } from '@/lib/hooks';
import { useAuth } from '@/contexts/AuthContext';
import { MessageSquare, Send, ChevronDown, ChevronUp, Plus, Lightbulb, HelpCircle, AlertCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Button } from '@/components/ui/button';

interface ConceptDiscussionProps {
  conceptId: string;
  className?: string;
}

interface DiscussionThreadProps {
  discussion: any;
  onReplyAdded?: () => void;
}

function DiscussionThread({ discussion, onReplyAdded }: DiscussionThreadProps) {
  const { user } = useAuth();
  const [isExpanded, setIsExpanded] = useState(false);
  const [replyText, setReplyText] = useState('');
  const { data: repliesData, isLoading: repliesLoading } = useDiscussionReplies(isExpanded ? discussion.id : '');
  const replyMutation = useReplyToDiscussion();

  const handleReply = async () => {
    if (!user?.id || !replyText.trim()) return;

    try {
      await replyMutation.mutateAsync({
        discussionId: discussion.id,
        userId: user.id,
        content: replyText
      });
      setReplyText('');
      onReplyAdded?.();
    } catch (error) {
      console.error('Failed to reply:', error);
    }
  };

  const replies = repliesData?.data?.replies || [];

  const getDiscussionIcon = (type: string) => {
    switch (type) {
      case 'proposal':
        return <Lightbulb className="w-4 h-4 text-amber-500" />;
      case 'question':
        return <HelpCircle className="w-4 h-4 text-blue-500" />;
      case 'issue':
        return <AlertCircle className="w-4 h-4 text-red-500" />;
      default:
        return <MessageSquare className="w-4 h-4 text-purple-500" />;
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      return new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      <div
        className="p-4 bg-white dark:bg-gray-800 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/80 transition-colors"
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3 flex-1">
            {getDiscussionIcon(discussion.discussionType)}
            <div className="flex-1 min-w-0">
              <h4 className="font-medium text-gray-900 dark:text-gray-100">
                {discussion.title}
              </h4>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1 line-clamp-2">
                {discussion.content}
              </p>
              <div className="flex items-center gap-3 mt-2 text-xs text-gray-500">
                <span>{discussion.username}</span>
                <span>•</span>
                <span>{formatDate(discussion.createdAt)}</span>
                <span>•</span>
                <span>{discussion.replyCount} {discussion.replyCount === 1 ? 'reply' : 'replies'}</span>
              </div>
            </div>
          </div>
          <button className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
            {isExpanded ? <ChevronUp className="w-5 h-5" /> : <ChevronDown className="w-5 h-5" />}
          </button>
        </div>
      </div>

      {isExpanded && (
        <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
          <div className="p-4">
            <div className="prose dark:prose-invert max-w-none mb-4">
              <p className="text-sm">{discussion.content}</p>
            </div>

            {/* Replies */}
            {repliesLoading ? (
              <div className="space-y-3 mb-4">
                {[...Array(2)].map((_, i) => (
                  <div key={i} className="animate-pulse">
                    <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2"></div>
                    <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2"></div>
                  </div>
                ))}
              </div>
            ) : replies.length > 0 ? (
              <div className="space-y-3 mb-4">
                {replies.map((reply: any) => (
                  <div key={reply.id} className="bg-white dark:bg-gray-800 p-3 rounded-lg">
                    <p className="text-sm text-gray-900 dark:text-gray-100">
                      {reply.content}
                    </p>
                    <div className="flex items-center gap-2 mt-2 text-xs text-gray-500">
                      <span>{reply.username}</span>
                      <span>•</span>
                      <span>{formatDate(reply.createdAt)}</span>
                    </div>
                  </div>
                ))}
              </div>
            ) : null}

            {/* Reply input */}
            {user && (
              <div className="flex gap-2">
                <input
                  type="text"
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleReply()}
                  placeholder="Write a reply..."
                  className="flex-1 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                />
                <Button
                  onClick={handleReply}
                  disabled={!replyText.trim() || replyMutation.isPending}
                  size="sm"
                >
                  <Send className="w-4 h-4" />
                </Button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export function ConceptDiscussion({ conceptId, className = '' }: ConceptDiscussionProps) {
  const { user } = useAuth();
  const [showNewDiscussion, setShowNewDiscussion] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newContent, setNewContent] = useState('');
  const [discussionType, setDiscussionType] = useState<'general' | 'question' | 'proposal' | 'issue'>('general');

  const { data, isLoading, error, refetch } = useConceptDiscussions(conceptId);
  const createMutation = useCreateDiscussion();

  const handleCreateDiscussion = async () => {
    if (!user?.id || !newTitle.trim() || !newContent.trim()) return;

    try {
      await createMutation.mutateAsync({
        conceptId,
        userId: user.id,
        title: newTitle,
        content: newContent,
        discussionType
      });
      setNewTitle('');
      setNewContent('');
      setDiscussionType('general');
      setShowNewDiscussion(false);
      refetch();
    } catch (error) {
      console.error('Failed to create discussion:', error);
    }
  };

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="w-5 h-5" />
            Discussions
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[...Array(2)].map((_, i) => (
              <div key={i} className="animate-pulse">
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2"></div>
                <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2"></div>
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
            <MessageSquare className="w-5 h-5" />
            Discussions
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-red-600 dark:text-red-400">
            Failed to load discussions
          </p>
        </CardContent>
      </Card>
    );
  }

  const discussions = data?.data?.discussions || [];

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <MessageSquare className="w-5 h-5" />
              Discussions
            </CardTitle>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              {discussions.length} {discussions.length === 1 ? 'discussion' : 'discussions'}
            </p>
          </div>
          {user && (
            <Button
              onClick={() => setShowNewDiscussion(!showNewDiscussion)}
              size="sm"
              variant="outline"
            >
              <Plus className="w-4 h-4 mr-1" />
              New Discussion
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {/* New Discussion Form */}
        {showNewDiscussion && user && (
          <div className="mb-6 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
            <h4 className="font-medium mb-3">Start a New Discussion</h4>
            
            <select
              value={discussionType}
              onChange={(e) => setDiscussionType(e.target.value as any)}
              className="w-full px-3 py-2 mb-3 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
            >
              <option value="general">General Discussion</option>
              <option value="question">Question</option>
              <option value="proposal">Improvement Proposal</option>
              <option value="issue">Issue</option>
            </select>

            <input
              type="text"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="Discussion title..."
              className="w-full px-3 py-2 mb-3 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent"
            />

            <textarea
              value={newContent}
              onChange={(e) => setNewContent(e.target.value)}
              placeholder="What would you like to discuss?"
              rows={4}
              className="w-full px-3 py-2 mb-3 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-purple-500 focus:border-transparent resize-none"
            />

            <div className="flex gap-2">
              <Button
                onClick={handleCreateDiscussion}
                disabled={!newTitle.trim() || !newContent.trim() || createMutation.isPending}
                size="sm"
              >
                Post Discussion
              </Button>
              <Button
                onClick={() => {
                  setShowNewDiscussion(false);
                  setNewTitle('');
                  setNewContent('');
                }}
                size="sm"
                variant="outline"
              >
                Cancel
              </Button>
            </div>
          </div>
        )}

        {/* Discussions List */}
        {discussions.length === 0 ? (
          <div className="text-center py-8">
            <MessageSquare className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
            <p className="text-gray-600 dark:text-gray-400">No discussions yet</p>
            {user && (
              <p className="text-sm text-gray-500 dark:text-gray-500 mt-1">
                Start the conversation!
              </p>
            )}
          </div>
        ) : (
          <div className="space-y-3">
            {discussions.map((discussion: any) => (
              <DiscussionThread
                key={discussion.id}
                discussion={discussion}
                onReplyAdded={refetch}
              />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

