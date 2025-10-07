'use client';

import { useState, useEffect, useMemo } from 'react';
import { Card, CardHeader, CardTitle, CardContent, CardFooter, CardDescription } from '@/components/ui/Card';
import { buildApiUrl } from '@/lib/config';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';

interface ChatsLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
}

interface ThreadSummary {
  id: string;
  title: string;
  content: string;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt?: string;
  updatedAt?: string;
  resonance?: number;
  axes?: string[];
  replies?: ThreadReply[];
}

interface ThreadReply {
  id: string;
  content: string;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt?: string;
  resonance?: number;
  isAccepted?: boolean;
}

interface ThreadDetail extends ThreadSummary {
  replies: ThreadReply[];
}

export function ChatsLens({ controls = {}, userId, className = '' }: ChatsLensProps) {
  const { user, isAuthenticated } = useAuth();
  const effectiveUserId = userId || user?.id;
  const trackInteraction = useTrackInteraction();

  const [threads, setThreads] = useState<ThreadSummary[]>([]);
  const [threadsLoading, setThreadsLoading] = useState(false);
  const [threadsError, setThreadsError] = useState<string | null>(null);

  const [selectedThreadId, setSelectedThreadId] = useState<string | null>(null);
  const [threadDetail, setThreadDetail] = useState<ThreadDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  const [composerOpen, setComposerOpen] = useState(false);
  const [composeTitle, setComposeTitle] = useState('');
  const [composeBody, setComposeBody] = useState('');
  const [sendingThread, setSendingThread] = useState(false);

  const [messageInput, setMessageInput] = useState('');
  const [sendingMessage, setSendingMessage] = useState(false);
  const [conversationBanner, setConversationBanner] = useState<{ type: 'info' | 'success' | 'error'; text: string } | null>(null);
  const [messageBanner, setMessageBanner] = useState<{ type: 'info' | 'success' | 'error'; text: string } | null>(null);

  const bannerStyles: Record<'info' | 'success' | 'error', string> = {
    info: 'bg-blue-50 dark:bg-blue-900/20 text-blue-800 dark:text-blue-200 border border-blue-200 dark:border-blue-800',
    success: 'bg-emerald-50 dark:bg-emerald-900/20 text-emerald-700 dark:text-emerald-200 border border-emerald-200 dark:border-emerald-800',
    error: 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-200 border border-red-200 dark:border-red-800',
  };

  const axes = useMemo(() => Array.isArray(controls.axes) ? controls.axes : [], [controls.axes]);

  useEffect(() => {
    loadThreads();
  }, []);

  useEffect(() => {
    if (threads.length === 0) {
      setSelectedThreadId(null);
      setThreadDetail(null);
      return;
    }

    if (!selectedThreadId || !threads.find(t => t.id === selectedThreadId)) {
      const nextId = threads[0].id;
      setSelectedThreadId(nextId);
      fetchThreadDetail(nextId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [threads]);

  const loadThreads = async () => {
    setThreadsLoading(true);
    setThreadsError(null);
    setConversationBanner(null);

    try {
      const response = await fetch(buildApiUrl('/threads/list'));
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = await response.json();
      const list: ThreadSummary[] = Array.isArray(data.threads) ? data.threads : [];
      setThreads(list);
    } catch (error) {
      console.error('ChatsLens list error:', error);
      setThreadsError('Unable to load conversations right now.');
      setThreads([]);
      setConversationBanner({ type: 'error', text: 'Unable to load conversations right now.' });
    } finally {
      setThreadsLoading(false);
    }
  };

  const fetchThreadDetail = async (threadId: string) => {
    setDetailLoading(true);
    setDetailError(null);
    setMessageBanner(null);

    try {
      const response = await fetch(buildApiUrl(`/threads/${threadId}`));
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = await response.json();
      if (data?.thread) {
        setThreadDetail(data.thread as ThreadDetail);
      } else {
        setThreadDetail(null);
      }
    } catch (error) {
      console.error('ChatsLens detail error:', error);
      setDetailError('Unable to load conversation.');
      setThreadDetail(null);
      setMessageBanner({ type: 'error', text: 'Unable to load conversation.' });
    } finally {
      setDetailLoading(false);
    }
  };

  const handleSelectThread = (threadId: string) => {
    setSelectedThreadId(threadId);
    fetchThreadDetail(threadId);
    trackInteraction(threadId, 'chat-open-thread', {
      description: 'Opened conversation in Chats lens',
      axes,
    });
    setMessageBanner(null);
  };

  const handleCreateThread = async () => {
    if (!user?.id) {
      setThreadsError('Sign in to start a conversation.');
      return;
    }
    if (!composeTitle.trim() || !composeBody.trim()) {
      return;
    }

    setSendingThread(true);
    setConversationBanner({ type: 'info', text: 'Creating conversation…' });
    try {
      const payload = {
        title: composeTitle.trim(),
        content: composeBody.trim(),
        authorId: user.id,
        authorName: user.displayName || user.username || 'Anonymous',
        axes: axes.length > 0 ? axes : ['unity', 'resonance'],
      };
      const response = await fetch(buildApiUrl('/threads/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const data = await response.json();
      if (response.ok && data?.success) {
        trackInteraction(data.threadId || 'new-thread', 'chat-create-thread', {
          description: `Created chat thread: ${composeTitle.trim()}`,
          axes,
        });
        setComposerOpen(false);
        setComposeTitle('');
        setComposeBody('');
        setConversationBanner({ type: 'success', text: 'Conversation created successfully.' });
        window.setTimeout(() => setConversationBanner(null), 5000);
        await loadThreads();
        if (data.threadId) {
          handleSelectThread(data.threadId);
        }
      } else {
        throw new Error(data?.error || 'Failed to create thread');
      }
    } catch (error) {
      console.error('ChatsLens create error:', error);
      setThreadsError('Unable to start a new conversation.');
      setConversationBanner({ type: 'error', text: 'Unable to start a new conversation.' });
    } finally {
      setSendingThread(false);
    }
  };

  const handleSendMessage = async () => {
    if (!threadDetail || !selectedThreadId) {
      return;
    }
    if (!user?.id) {
      setDetailError('Sign in to send messages.');
      return;
    }
    const trimmed = messageInput.trim();
    if (!trimmed) {
      return;
    }

    setSendingMessage(true);
    setMessageBanner({ type: 'info', text: 'Sending message…' });

    const optimisticId = `temp-${Date.now()}`;
    const optimisticReply: ThreadReply = {
      id: optimisticId,
      content: trimmed,
      author: {
        id: user.id,
        name: user.displayName || user.username || 'You',
        avatar: undefined,
      },
      createdAt: new Date().toISOString(),
      resonance: 0.5,
      isAccepted: false,
    };

    setThreadDetail(prev => prev ? { ...prev, replies: [...prev.replies, optimisticReply] } : prev);
    setMessageInput('');

    try {
      const payload = {
        content: trimmed,
        authorId: user.id,
        authorName: user.displayName || user.username || 'Anonymous',
      };
      const response = await fetch(buildApiUrl(`/threads/${selectedThreadId}/reply`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      const data = await response.json();
      if (response.ok && data?.success) {
        trackInteraction(selectedThreadId, 'chat-send-message', {
          description: 'Sent message in conversation',
          wordCount: trimmed.split(/\s+/).length,
        });
        await fetchThreadDetail(selectedThreadId);
        setMessageBanner({ type: 'success', text: 'Message sent.' });
        window.setTimeout(() => setMessageBanner(null), 4000);
      } else {
        throw new Error(data?.error || 'Message failed');
      }
    } catch (error) {
      console.error('ChatsLens send error:', error);
      setDetailError('Unable to send message.');
      setThreadDetail(prev => prev ? { ...prev, replies: prev.replies.filter(reply => reply.id !== optimisticId) } : prev);
      setMessageInput(trimmed);
      setMessageBanner({ type: 'error', text: 'Unable to send message. Please try again.' });
    } finally {
      setSendingMessage(false);
    }
  };

  const formatTime = (iso?: string) => {
    if (!iso) return '';
    const date = new Date(iso);
    return date.toLocaleString();
  };

  const filteredThreads = useMemo(() => {
    if (!axes.length) return threads;
    return threads.filter(thread => thread.axes?.some(axis => axes.includes(axis)));
  }, [threads, axes]);

  // Show sign-in prompt if no user is authenticated
  if (!isAuthenticated || !user) {
    return (
      <div className={`space-y-6 ${className}`}>
        <Card>
          <CardHeader className="pb-4">
            <CardTitle className="text-2xl">💬 Chats</CardTitle>
            <CardDescription>
              Ongoing conversations tuned to your resonance.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-center py-8">
            <div className="text-4xl mb-4">🔐</div>
            <div className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
              Sign in to join conversations
            </div>
            <div className="text-gray-600 dark:text-gray-300 mb-4">
              Connect with others who share your resonance and participate in meaningful discussions
            </div>
            <a
              href="/login"
              className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
            >
              Sign In
            </a>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      <Card>
        <CardHeader className="pb-4">
          <div className="flex items-center justify-between gap-4">
            <div>
              <CardTitle className="text-2xl">💬 Chats</CardTitle>
              <CardDescription>
                Ongoing conversations tuned to your resonance.
              </CardDescription>
            </div>
            <button
              type="button"
              onClick={() => setComposerOpen(true)}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              Start Conversation
            </button>
          </div>
      </CardHeader>
    </Card>

      {conversationBanner && (
        <div className={`${bannerStyles[conversationBanner.type]} rounded-lg px-3 py-2 text-sm`}>
          {conversationBanner.text}
        </div>
      )}

      {threadsLoading && (
        <div className="animate-pulse">
          <div className="bg-gray-200 dark:bg-gray-700 rounded-lg h-24" />
        </div>
      )}

      {threadsError && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 text-sm text-red-700 dark:text-red-300">
          {threadsError}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 space-y-3">
          {filteredThreads.length === 0 && !threadsLoading ? (
            <Card>
              <CardContent className="py-10 text-center text-sm text-gray-500 dark:text-gray-300">
                No conversations yet. Start one to invite resonance.
              </CardContent>
            </Card>
          ) : (
            filteredThreads.map(thread => (
              <Card
                key={thread.id}
                onClick={() => handleSelectThread(thread.id)}
                className={`p-4 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 ${selectedThreadId === thread.id ? 'border-blue-400 dark:border-blue-500' : ''}`}
              >
                <CardTitle className="text-base mb-1 text-gray-900 dark:text-gray-100">
                  {thread.title || 'Untitled conversation'}
                </CardTitle>
                <p className="text-xs text-gray-600 dark:text-gray-300 line-clamp-2">
                  {thread.content}
                </p>
                <div className="flex items-center justify-between mt-3 text-xs text-gray-500 dark:text-gray-400">
                  {thread.author?.id ? (
                    <a href={`/node/${thread.author.id}`} className="hover:underline">
                      {thread.author?.name || 'Unknown'}
                    </a>
                  ) : (
                    <span>{thread.author?.name || 'Unknown'}</span>
                  )}
                  <span>{formatTime(thread.updatedAt || thread.createdAt)}</span>
                </div>
              </Card>
            ))
          )}

        </div>

        <div className="lg:col-span-2">
          {detailLoading && (
            <div className="animate-pulse">
              <div className="bg-gray-200 dark:bg-gray-700 rounded-lg h-64" />
            </div>
          )}

          {detailError && !detailLoading && (
            <Card>
              <CardContent className="p-6 text-sm text-red-600 dark:text-red-300">
                {detailError}
              </CardContent>
            </Card>
          )}

          {!detailLoading && !detailError && threadDetail && (
            <Card className="flex flex-col h-full">
              <CardHeader className="pb-4">
                <CardTitle className="text-xl text-gray-900 dark:text-gray-100">
                  {threadDetail.title}
                </CardTitle>
                <CardDescription className="text-sm text-gray-600 dark:text-gray-300">
                  {threadDetail.author?.id ? (
                    <a href={`/node/${threadDetail.author.id}`} className="hover:underline">
                      {threadDetail.author?.name || 'Unknown author'}
                    </a>
                  ) : (
                    <span>{threadDetail.author?.name || 'Unknown author'}</span>
                  )}
                  {` · ${formatTime(threadDetail.createdAt)}`}
                </CardDescription>
                {threadDetail.axes && threadDetail.axes.length > 0 && (
                  <div className="flex flex-wrap gap-2 mt-3">
                    {threadDetail.axes.slice(0, 6).map(axis => (
                      <span
                        key={axis}
                        className="px-2 py-1 text-xs rounded-full bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-200"
                      >
                        {axis}
                      </span>
                    ))}
                  </div>
                )}
              </CardHeader>

              <CardContent className="flex-1 overflow-y-auto space-y-4">
                <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                  <p className="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap">
                    {threadDetail.content}
                  </p>
                </div>

                {threadDetail.replies && threadDetail.replies.length > 0 ? (
                  threadDetail.replies.map(reply => (
                    <div
                      key={reply.id}
                      className={`flex ${reply.author?.id === effectiveUserId ? 'justify-end' : 'justify-start'}`}
                    >
                      <div
                        className={`max-w-xl rounded-lg px-3 py-2 text-sm shadow ${reply.author?.id === effectiveUserId ? 'bg-blue-600 text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100'}`}
                      >
                        <div className="text-xs mb-1 opacity-80">
                          {reply.author?.id ? (
                            <a href={`/node/${reply.author.id}`} className="hover:underline">
                              {reply.author?.name || 'Participant'}
                            </a>
                          ) : (
                            <span>{reply.author?.name || 'Participant'}</span>
                          )}
                          {` · ${formatTime(reply.createdAt)}`}
                        </div>
                        <div className="whitespace-pre-wrap leading-relaxed">
                          {reply.content}
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-sm text-gray-500 dark:text-gray-400 italic">
                    No replies yet. Be the first to add to this conversation.
                  </p>
                )}
              </CardContent>

              <CardFooter className="mt-auto border-t border-gray-100 dark:border-gray-700 pt-4 flex flex-col gap-3">
                {messageBanner && (
                  <div className={`${bannerStyles[messageBanner.type]} w-full rounded-md px-3 py-2 text-xs`}>
                    {messageBanner.text}
                  </div>
                )}
                <textarea
                  value={messageInput}
                  onChange={(event) => setMessageInput(event.target.value)}
                  placeholder={user ? 'Share your thoughts…' : 'Sign in to reply'}
                  rows={3}
                  className="w-full input-standard"
                  disabled={!user}
                />
                <div className="flex justify-end">
                  <button
                    type="button"
                    onClick={handleSendMessage}
                    disabled={!user || sendingMessage}
                    className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors disabled:opacity-60"
                  >
                    {sendingMessage ? 'Sending…' : 'Send Message'}
                  </button>
                </div>
              </CardFooter>
            </Card>
          )}
        </div>
      </div>

      {composerOpen && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <CardHeader>
              <CardTitle>Start a Conversation</CardTitle>
              <CardDescription>
                Invite others into a new dialogue aligned with your resonance axes.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Title</label>
                <input
                  type="text"
                  value={composeTitle}
                  onChange={(event) => setComposeTitle(event.target.value)}
                  className="input-standard"
                  placeholder="Conversation title"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-secondary mb-1">Opening message</label>
                <textarea
                  value={composeBody}
                  onChange={(event) => setComposeBody(event.target.value)}
                  className="input-standard"
                  rows={5}
                  placeholder="Share context, questions, or inspiration for this chat."
                />
              </div>
            </CardContent>
            <CardFooter className="flex gap-3">
              <button
                type="button"
                onClick={handleCreateThread}
                disabled={sendingThread}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors disabled:opacity-60"
              >
                {sendingThread ? 'Creating…' : 'Create Conversation'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setComposerOpen(false);
                  setComposeTitle('');
                  setComposeBody('');
                }}
                className="flex-1 px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
              >
                Cancel
              </button>
            </CardFooter>
          </Card>
        </div>
      )}
    </div>
  );
}

export default ChatsLens;
