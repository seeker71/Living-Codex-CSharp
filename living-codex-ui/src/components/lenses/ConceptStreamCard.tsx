'use client';

import { useState, useRef, useEffect } from 'react';
import { Heart, Share2, MessageCircle, Link2, Zap, Network, Users, TrendingUp, Clock, Star, Sparkles, ChevronDown, ChevronUp, ThumbsUp, ThumbsDown, Reply, MoreHorizontal, Bookmark, Flag } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/Card';
import { useAttune, useAmplify } from '@/lib/hooks';
import { formatRelativeTime } from '@/lib/utils';
import { endpoints } from '@/lib/api';

interface Concept {
  id: string;
  name: string;
  description: string;
  axes?: string[];
  resonance?: number;
  type?: string;
  meta?: Record<string, any>;
  contributors?: Array<{
    id: string;
    name: string;
    avatar?: string;
    contribution: string;
    timestamp: string;
  }>;
  contributionCount?: number;
  trendingScore?: number;
  lastActivity?: string;
  relatedConcepts?: Array<{
    id: string;
    name: string;
    strength: number;
  }>;
  energyLevel?: number;
  isNew?: boolean;
  isTrending?: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Social feed features
  upvotes?: number;
  downvotes?: number;
  userVote?: 'up' | 'down' | null;
  commentCount?: number;
  shareCount?: number;
  isBookmarked?: boolean;
  threadDepth?: number;
  parentConceptId?: string;
  replies?: Concept[];
  isHot?: boolean;
  score?: number;
  // Notion-style collaborative features
  properties?: Record<string, any>;
  backlinks?: Array<{ id: string; name: string; type: string }>;
  forwardLinks?: Array<{ id: string; name: string; type: string }>;
  lastEditedBy?: { id: string; name: string; avatar?: string; timestamp: string };
  collaborators?: Array<{ id: string; name: string; avatar?: string; role: string; isActive: boolean }>;
  version?: number;
  isLocked?: boolean;
  template?: string;
  status?: 'draft' | 'review' | 'published' | 'archived';
  priority?: 'low' | 'medium' | 'high' | 'urgent';
  dueDate?: string;
  tags?: string[];
  databaseView?: 'table' | 'board' | 'timeline' | 'calendar' | 'list' | 'gallery';
}

interface ConceptStreamCardProps {
  concept: Concept;
  userId?: string;
  onAction?: (action: string, conceptId: string) => void;
}

export function ConceptStreamCard({ concept, userId, onAction }: ConceptStreamCardProps) {
  const [isAttuned, setIsAttuned] = useState(false);
  const [isAmplifying, setIsAmplifying] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [showFullDescription, setShowFullDescription] = useState(false);
  const [userVote, setUserVote] = useState<'up' | 'down' | null>(concept.userVote || null);
  const [isBookmarked, setIsBookmarked] = useState(concept.isBookmarked || false);
  const [showReplies, setShowReplies] = useState(false);
  // Initialize with concept data or default values
  const [upvotes, setUpvotes] = useState(concept.upvotes || 0);
  const [downvotes, setDownvotes] = useState(concept.downvotes || 0);
  const [commentCount, setCommentCount] = useState(concept.commentCount || 0);
  const [shareCount, setShareCount] = useState(concept.shareCount || 0);
  const [activeCollaborators, setActiveCollaborators] = useState(concept.collaborators?.filter(c => c.isActive) || []);
  const [showProperties, setShowProperties] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);
  const [actionMessage, setActionMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [showMoreActions, setShowMoreActions] = useState(false);
  const [loadingInteractions, setLoadingInteractions] = useState(true);

  const attuneMutation = useAttune();
  const amplifyMutation = useAmplify();

  // Load user interactions from backend on mount
  useEffect(() => {
    const loadUserInteractions = async () => {
      if (!userId || !concept.id) {
        setLoadingInteractions(false);
        return;
      }

      try {
        // Load all user interactions for this concept
        const response = await endpoints.getUserInteractions(userId, concept.id);
        
        if (response.success && response.data) {
          const data = response.data as any;
          setUserVote(data.vote || null);
          setIsBookmarked(data.bookmarked || false);
          // isAttuned is loaded via /userconcept/relationship endpoint if needed
        }

        // Load vote counts
        const voteCounts = await endpoints.getVoteCounts(concept.id);
        if (voteCounts.success && voteCounts.data) {
          const voteData = voteCounts.data as any;
          setUpvotes(voteData.upvotes || 0);
          setDownvotes(voteData.downvotes || 0);
        }

        // Load share count
        const shareCounts = await endpoints.getShareCount(concept.id);
        if (shareCounts.success && shareCounts.data) {
          const shareData = shareCounts.data as any;
          setShareCount(shareData.shares || 0);
        }
      } catch (error) {
        console.error('Failed to load user interactions:', error);
      } finally {
        setLoadingInteractions(false);
      }
    };

    loadUserInteractions();
  }, [userId, concept.id]);

  // Auto-expand trending or new concepts
  useEffect(() => {
    if (concept.isTrending || concept.isNew) {
      setIsExpanded(true);
    }
  }, [concept.isTrending, concept.isNew]);

  // Voting functions with backend persistence
  const handleVote = async (voteType: 'up' | 'down') => {
    if (!userId) return;

    const newVote = userVote === voteType ? null : voteType;
    
    // Optimistic UI update
    const previousVote = userVote;
    setUserVote(newVote);

    // Update counts based on previous state
    if (userVote === 'up' && voteType === 'down') {
      setUpvotes(prev => prev - 1);
      setDownvotes(prev => prev + 1);
    } else if (userVote === 'down' && voteType === 'up') {
      setDownvotes(prev => prev - 1);
      setUpvotes(prev => prev + 1);
    } else if (userVote === voteType) {
      // Removing vote
      if (voteType === 'up') setUpvotes(prev => prev - 1);
      else setDownvotes(prev => prev - 1);
    } else {
      // Adding new vote
      if (voteType === 'up') setUpvotes(prev => prev + 1);
      else setDownvotes(prev => prev + 1);
    }

    // Persist to backend
    try {
      await endpoints.setVote(userId, concept.id, newVote);
    } catch (error) {
      console.error('Failed to save vote:', error);
      // Revert on error
      setUserVote(previousVote);
      setActionMessage({ type: 'error', text: 'Failed to save vote' });
      setTimeout(() => setActionMessage(null), 2000);
    }
  };

  const handleBookmark = async () => {
    if (!userId) return;
    
    // Optimistic UI update
    const previousState = isBookmarked;
    setIsBookmarked(!isBookmarked);
    setActionMessage({ type: 'success', text: !isBookmarked ? 'Saved to your list' : 'Removed from your list' });
    setTimeout(() => setActionMessage(null), 2000);

    // Persist to backend
    try {
      await endpoints.toggleBookmark(userId, concept.id);
    } catch (error) {
      console.error('Failed to toggle bookmark:', error);
      // Revert on error
      setIsBookmarked(previousState);
      setActionMessage({ type: 'error', text: 'Failed to save bookmark' });
      setTimeout(() => setActionMessage(null), 2000);
    }
  };

  const handleShare = async () => {
    if (!userId) return;
    
    try {
      // Check if Web Share API is available
      if (navigator.share) {
        await navigator.share({
          title: concept.name,
          text: concept.description,
          url: `${window.location.origin}/node/${concept.id}`
        });
        setShareCount(prev => prev + 1);
        onAction?.('share', concept.id);
        
        // Record share in backend
        endpoints.recordShare(userId, concept.id, 'native').catch(err => 
          console.error('Failed to record share:', err)
        );
        
        setActionMessage({ type: 'success', text: 'Shared successfully' });
        setTimeout(() => setActionMessage(null), 2000);
      } else {
        // Fallback: copy to clipboard
        const shareUrl = `${window.location.origin}/node/${concept.id}`;
        await navigator.clipboard.writeText(`${concept.name}\n\n${concept.description}\n\n${shareUrl}`);
        setShareCount(prev => prev + 1);
        onAction?.('share', concept.id);
        
        // Record share in backend
        endpoints.recordShare(userId, concept.id, 'clipboard').catch(err => 
          console.error('Failed to record share:', err)
        );
        
        setActionMessage({ type: 'success', text: 'Link copied to clipboard' });
        setTimeout(() => setActionMessage(null), 2000);
      }
    } catch (error) {
      console.error('Share error:', error);
      // Fallback: copy to clipboard
      try {
        const shareUrl = `${window.location.origin}/node/${concept.id}`;
        await navigator.clipboard.writeText(`${concept.name}\n\n${concept.description}\n\n${shareUrl}`);
        setShareCount(prev => prev + 1);
        onAction?.('share', concept.id);
        
        // Record share in backend
        endpoints.recordShare(userId, concept.id, 'clipboard').catch(err => 
          console.error('Failed to record share:', err)
        );
        
        setActionMessage({ type: 'success', text: 'Link copied to clipboard' });
        setTimeout(() => setActionMessage(null), 2000);
      } catch (clipboardError) {
        console.error('Clipboard error:', clipboardError);
        setActionMessage({ type: 'error', text: 'Unable to share. Please copy link manually.' });
        setTimeout(() => setActionMessage(null), 2500);
      }
    }
  };

  const totalScore = upvotes - downvotes;

  const handleAttune = async () => {
    if (!userId) return;
    
    try {
      if (isAttuned) {
        // TODO: Implement unattune
        setIsAttuned(false);
        setActionMessage({ type: 'success', text: 'Stopped following concept' });
      } else {
        await attuneMutation.mutateAsync({ userId, conceptId: concept.id });
        setIsAttuned(true);
        setActionMessage({ type: 'success', text: 'Now following concept' });
      }
      onAction?.('attune', concept.id);
    } catch (error) {
      console.error('Attune error:', error);
      setActionMessage({ type: 'error', text: 'Could not update follow state' });
    } finally {
      setTimeout(() => setActionMessage(null), 2000);
    }
  };

  const handleAmplify = async () => {
    if (!userId) return;
    
    try {
      setIsAmplifying(true);
      await amplifyMutation.mutateAsync({ 
        userId, 
        conceptId: concept.id, 
        contribution: `Amplified concept: ${concept.name}` 
      });
      onAction?.('amplify', concept.id);
      setActionMessage({ type: 'success', text: 'Sent your boost' });
    } catch (error) {
      console.error('Amplify error:', error);
      setActionMessage({ type: 'error', text: 'Boost failed. Try again.' });
    } finally {
      setIsAmplifying(false);
      setTimeout(() => setActionMessage(null), 2000);
    }
  };

  const handleCopyLink = async () => {
    try {
      const shareUrl = `${window.location.origin}/node/${concept.id}`;
      await navigator.clipboard.writeText(shareUrl);
      setActionMessage({ type: 'success', text: 'Link copied' });
    } catch {
      setActionMessage({ type: 'error', text: 'Copy failed' });
    } finally {
      setTimeout(() => setActionMessage(null), 2000);
    }
  };

  const handleOpenConcept = () => {
    window.open(`/node/${concept.id}`, '_blank');
  };

  const handleReport = () => {
    setActionMessage({ type: 'success', text: 'Thanks for the report' });
    setTimeout(() => setActionMessage(null), 2000);
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

  return (
    <Card
      ref={cardRef}
      className={`group relative overflow-hidden transition-all duration-500 hover:shadow-2xl hover:-translate-y-2 hover:scale-[1.02] border-0 rounded-2xl cursor-pointer focus-within:ring-2 focus-within:ring-blue-500 focus-within:ring-offset-2 ${
        concept.isTrending
          ? 'bg-gradient-to-br from-orange-50 via-white to-orange-50 dark:from-orange-950/30 dark:via-gray-900 dark:to-orange-950/30 shadow-orange-200/50 dark:shadow-orange-900/20'
          : concept.isNew
          ? 'bg-gradient-to-br from-green-50 via-white to-green-50 dark:from-green-950/30 dark:via-gray-900 dark:to-green-950/30 shadow-green-200/50 dark:shadow-green-900/20'
          : 'bg-gradient-to-br from-slate-50 via-white to-slate-50 dark:from-slate-950/30 dark:via-gray-900 dark:to-slate-950/30 shadow-slate-200/50 dark:shadow-slate-900/20'
      } ${isExpanded ? 'ring-2 ring-blue-300 dark:ring-blue-700 shadow-blue-200/50 dark:shadow-blue-900/20' : ''}`}
      onClick={() => setIsExpanded(!isExpanded)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          setIsExpanded(!isExpanded);
        }
      }}
      tabIndex={0}
      role="button"
      aria-expanded={isExpanded}
      aria-label={`Concept: ${concept.name}. ${isExpanded ? 'Click to collapse' : 'Click to expand'}`}
    >
      {/* Lightweight toast */}
      {actionMessage && (
        <div className={`absolute top-2 right-2 z-30 px-3 py-2 rounded-lg text-xs font-medium shadow-md ${
          actionMessage.type === 'success'
            ? 'bg-emerald-600 text-white'
            : 'bg-red-600 text-white'
        }`}>
          {actionMessage.text}
        </div>
      )}

      <CardHeader className="pb-4">
        {/* Reddit/Twitter-style header with voting and thread info */}
        <div className="flex space-x-4">
          {/* Voting section with accessibility */}
          <div className="flex flex-col items-center space-y-1 pt-1">
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleVote('up');
              }}
              disabled={!userId}
              className={`p-1 rounded-full transition-all hover:scale-110 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2 ${
                userVote === 'up'
                  ? 'text-orange-500 bg-orange-50 dark:bg-orange-900/20'
                  : 'text-gray-400 hover:text-orange-500 hover:bg-orange-50 dark:hover:bg-orange-900/20'
              }`}
              aria-label={`${userVote === 'up' ? 'Remove upvote' : 'Upvote'} concept: ${concept.name}`}
              aria-pressed={userVote === 'up'}
            >
              <ThumbsUp className="w-5 h-5" />
            </button>

            <div
              className={`text-sm font-bold min-w-[2rem] text-center ${
                totalScore > 0 ? 'text-green-600 dark:text-green-400' :
                totalScore < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-600 dark:text-gray-400'
              }`}
              aria-label={`Score: ${totalScore > 0 ? 'positive' : totalScore < 0 ? 'negative' : 'neutral'} ${Math.abs(totalScore)} points`}
            >
              {totalScore > 0 ? '+' : ''}{totalScore}
            </div>

            <button
              onClick={(e) => {
                e.stopPropagation();
                handleVote('down');
              }}
              disabled={!userId}
              className={`p-1 rounded-full transition-all hover:scale-110 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
                userVote === 'down'
                  ? 'text-blue-500 bg-blue-50 dark:bg-blue-900/20'
                  : 'text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20'
              }`}
              aria-label={`${userVote === 'down' ? 'Remove downvote' : 'Downvote'} concept: ${concept.name}`}
              aria-pressed={userVote === 'down'}
            >
              <ThumbsDown className="w-5 h-5" />
            </button>
          </div>

          {/* Main content */}
          <div className="flex-1 space-y-3">
            {/* Thread indicator and title */}
            <div>
              {concept.threadDepth && concept.threadDepth > 0 && (
                <div className="flex items-center space-x-2 mb-2 text-xs text-gray-500 dark:text-gray-400">
                  <Reply className="w-3 h-3 rotate-180" />
                  <span>Thread reply</span>
                  <span>•</span>
                  <span>{concept.threadDepth} levels deep</span>
                </div>
              )}

              <div className="flex items-center space-x-2 mb-1">
                <CardTitle className="text-2xl font-bold text-gray-900 dark:text-gray-100 leading-tight hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      window.open(`/node/${concept.id}`, '_blank');
                    }}
                    className="text-left focus:outline-none focus:ring-2 focus:ring-blue-500 rounded-sm"
                  >
                    {concept.name}
                  </button>
                </CardTitle>
                {concept.energyLevel && concept.energyLevel > 0.9 && (
                  <div className="flex items-center space-x-1 px-2 py-1 bg-gradient-to-r from-amber-400 to-orange-500 text-white text-xs font-bold rounded-full shadow-sm">
                    <Star className="w-3.5 h-3.5 fill-current" />
                    <span>HIGH ENERGY</span>
                  </div>
                )}
                {concept.isHot && (
                  <div className="flex items-center space-x-1 px-2 py-1 bg-gradient-to-r from-red-500 to-orange-500 text-white text-xs font-bold rounded-full shadow-sm">
                    <Zap className="w-3.5 h-3.5 fill-current" />
                    <span>HOT</span>
                  </div>
                )}
              </div>

              {/* Clean description with better typography */}
              <div className="relative">
                <CardDescription className="text-gray-600 dark:text-gray-300 text-base leading-relaxed line-clamp-3">
                  {concept.description}
                </CardDescription>
                {concept.description.length > 180 && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setShowFullDescription(!showFullDescription);
                    }}
                    className="mt-1 text-sm text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium transition-colors"
                  >
                    {showFullDescription ? 'Show less' : 'Read more'}
                  </button>
                )}
              </div>
            </div>

            {/* Enhanced resonance display */}
            {concept.resonance && (
              <div className="ml-6 text-right flex-shrink-0">
                <div className="relative group">
                  <div className={`text-4xl font-black ${
                    concept.resonance > 0.8
                      ? 'text-transparent bg-gradient-to-br from-purple-600 to-pink-600 bg-clip-text'
                      : concept.resonance > 0.6
                      ? 'text-transparent bg-gradient-to-br from-blue-600 to-cyan-600 bg-clip-text'
                      : 'text-gray-600 dark:text-gray-400'
                  }`}>
                    {Math.round(concept.resonance * 100)}
                  </div>
                  <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Resonance</div>
                  {/* Enhanced animated ring */}
                  <div className="absolute -inset-2 rounded-full border-2 border-purple-200/50 dark:border-purple-800/30 group-hover:border-purple-300 dark:group-hover:border-purple-700 transition-colors"></div>
                  <div className="absolute -inset-2 rounded-full border-2 border-transparent bg-gradient-to-r from-purple-400/10 to-pink-400/10 animate-pulse"></div>
                </div>
              </div>
            )}
          </div>

        {/* Enhanced activity indicators with context */}
        <div className="flex items-center justify-between mt-3">
          <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
            {concept.lastActivity && (
              <div className="flex items-center space-x-1">
                <Clock className="w-3 h-3" />
                <span>Updated {concept.lastActivity}</span>
              </div>
            )}
            {concept.contributionCount && concept.contributionCount > 0 && (
              <div className="flex items-center space-x-1">
                <Users className="w-3 h-3" />
                <span>{concept.contributionCount} contributions</span>
              </div>
            )}
            {concept.trendingScore && concept.trendingScore > 0 && (
              <div className="flex items-center space-x-1">
                <TrendingUp className="w-3 h-3 text-orange-500" />
                <span>+{concept.trendingScore} trending</span>
              </div>
            )}
          </div>

          {/* Context indicators */}
          <div className="flex items-center space-x-2">
            {concept.energyLevel && concept.energyLevel > 0.9 && (
              <div className="flex items-center space-x-1 px-2 py-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300 text-xs rounded-full">
                <Zap className="w-3 h-3" />
                <span>High Energy</span>
              </div>
            )}
            {concept.isNew && (
              <div className="flex items-center space-x-1 px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 text-xs rounded-full">
                <Sparkles className="w-3 h-3" />
                <span>Fresh</span>
              </div>
            )}
            {concept.type && (
              <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 text-xs rounded-full capitalize">
                {concept.type}
              </span>
            )}
          </div>
        </div>
        </div>
      </CardHeader>

      <CardContent className="pt-0">
        {/* Pinterest-style content organization */}
        <div className="space-y-4">
          {/* Enhanced axes with better visual design */}
          {concept.axes && concept.axes.length > 0 && (
            <div>
              <div className="flex flex-wrap gap-2">
                {concept.axes.map((axis) => (
                  <span
                    key={axis}
                    className={`px-4 py-2 rounded-full text-sm font-bold transition-all hover:scale-110 hover:shadow-md cursor-pointer ${getAxisColor(axis)}`}
                  >
                    {axis.toUpperCase()}
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Enhanced tags with better spacing and interaction */}
          {concept.tags && concept.tags.length > 0 && (
            <div>
              <div className="flex flex-wrap gap-2">
                {concept.tags.slice(0, 8).map((tag) => (
                  <button
                    key={tag}
                    onClick={(e) => e.stopPropagation()}
                    className="px-3 py-1.5 bg-gradient-to-r from-gray-100 to-gray-50 dark:from-gray-800 dark:to-gray-700 text-gray-700 dark:text-gray-300 text-sm rounded-lg hover:from-blue-100 hover:to-blue-50 dark:hover:from-blue-900/30 dark:hover:to-blue-800/30 hover:text-blue-700 dark:hover:text-blue-300 transition-all hover:scale-105 border border-gray-200 dark:border-gray-600"
                  >
                    #{tag}
                  </button>
                ))}
                {concept.tags.length > 8 && (
                  <span className="px-3 py-1.5 bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 text-sm rounded-lg">
                    +{concept.tags.length - 8} more
                  </span>
                )}
              </div>
            </div>
          )}

          {/* Pinterest-style contributor showcase */}
          {concept.contributors && concept.contributors.length > 0 && (
            <div className="relative">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center space-x-2">
                  <Users className="w-4 h-4 text-gray-600 dark:text-gray-400" />
                  <span className="text-sm font-semibold text-gray-700 dark:text-gray-300">Community</span>
                  <span className="px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-xs rounded-full">
                    {concept.contributors.length}
                  </span>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    setIsExpanded(!isExpanded);
                  }}
                  className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 flex items-center space-x-1 font-medium transition-colors"
                >
                  <span>{isExpanded ? 'Show less' : 'See all'}</span>
                  {isExpanded ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
                </button>
              </div>

              {/* Enhanced contributor avatars with hover effects */}
              <div className="flex -space-x-2 justify-start">
                {concept.contributors.slice(0, 6).map((contributor, index) => (
                  <div
                    key={contributor.id}
                    className="relative group"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <div
                      className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 via-purple-500 to-pink-500 flex items-center justify-center text-white text-sm font-bold border-3 border-white dark:border-gray-800 hover:z-20 hover:scale-125 transition-all duration-300 cursor-pointer shadow-lg"
                      title={`${contributor.name} - ${contributor.contribution}`}
                    >
                      {contributor.avatar ? (
                        <img
                          src={contributor.avatar}
                          alt={contributor.name}
                          className="w-full h-full rounded-full object-cover"
                        />
                      ) : (
                        contributor.name.charAt(0).toUpperCase()
                      )}
                    </div>

                    {/* Tooltip on hover */}
                    <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-2 py-1 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-30">
                      {contributor.name}
                      <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-4 border-r-4 border-t-4 border-l-transparent border-r-transparent border-t-gray-900"></div>
                    </div>
                  </div>
                ))}

                {concept.contributors.length > 6 && (
                  <div className="w-10 h-10 rounded-full bg-gradient-to-r from-gray-300 to-gray-400 dark:from-gray-600 dark:to-gray-700 flex items-center justify-center text-gray-700 dark:text-gray-300 text-sm font-bold border-3 border-white dark:border-gray-800 hover:scale-110 transition-transform cursor-pointer">
                    +{concept.contributors.length - 6}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Expanded content with Notion-style collaborative features */}
          {isExpanded && (
            <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700 animate-in slide-in-from-top-2 duration-500">
              {/* Notion-style properties panel */}
              <div className="mb-6">
                <div className="flex items-center justify-between mb-3">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Properties</h4>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setShowProperties(!showProperties);
                    }}
                    className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 flex items-center space-x-1"
                  >
                    <span>{showProperties ? 'Hide' : 'Show'} details</span>
                    {showProperties ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
                  </button>
                </div>

                {showProperties && (
                  <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 space-y-3">
                    {/* Status and Priority */}
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Status</label>
                        <div className="flex items-center space-x-2 mt-1">
                          <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                            concept.status === 'published' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' :
                            concept.status === 'review' ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300' :
                            concept.status === 'draft' ? 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300' :
                            'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
                          }`}>
                            {concept.status || 'draft'}
                          </span>
                        </div>
                      </div>
                      <div>
                        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Priority</label>
                        <div className="flex items-center space-x-2 mt-1">
                          <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                            concept.priority === 'urgent' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300' :
                            concept.priority === 'high' ? 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300' :
                            concept.priority === 'medium' ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300' :
                            'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300'
                          }`}>
                            {concept.priority || 'medium'}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Last edited and version info */}
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Last Edited</label>
                        {concept.lastEditedBy && (
                          <div className="flex items-center space-x-2 mt-1">
                            <div className="w-6 h-6 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-xs font-semibold">
                              {concept.lastEditedBy.avatar ? (
                                <img src={concept.lastEditedBy.avatar} alt={concept.lastEditedBy.name} className="w-full h-full rounded-full object-cover" />
                              ) : (
                                concept.lastEditedBy.name.charAt(0).toUpperCase()
                              )}
                            </div>
                            <div>
                              <div className="text-sm font-medium text-gray-700 dark:text-gray-300">{concept.lastEditedBy.name}</div>
                              <div className="text-xs text-gray-500 dark:text-gray-400">{concept.lastEditedBy.timestamp}</div>
                            </div>
                          </div>
                        )}
                      </div>
                      <div>
                        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Version</label>
                        <div className="mt-1 text-sm font-medium text-gray-700 dark:text-gray-300">v{concept.version || 1}</div>
                      </div>
                    </div>

                    {/* Active collaborators */}
                    {activeCollaborators.length > 0 && (
                      <div>
                        <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Active Now</label>
                        <div className="flex -space-x-2 mt-1">
                          {activeCollaborators.map((collaborator) => (
                            <div key={collaborator.id} className="relative">
                              <div className="w-6 h-6 rounded-full bg-gradient-to-br from-green-400 to-blue-500 flex items-center justify-center text-white text-xs font-semibold border-2 border-white dark:border-gray-800">
                                {collaborator.avatar ? (
                                  <img src={collaborator.avatar} alt={collaborator.name} className="w-full h-full rounded-full object-cover" />
                                ) : (
                                  collaborator.name.charAt(0).toUpperCase()
                                )}
                              </div>
                              <div className="absolute -bottom-1 -right-1 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-800"></div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>

              {/* Enhanced contribution celebration */}
              {concept.contributionCount && concept.contributionCount > 20 && (
                <div className="mb-6 p-4 bg-gradient-to-br from-amber-50 via-orange-50 to-yellow-50 dark:from-amber-900/30 dark:via-orange-900/30 dark:to-yellow-900/30 rounded-xl border border-amber-200 dark:border-amber-800 shadow-sm">
                  <div className="flex items-center space-x-3">
                    <div className="w-12 h-12 bg-gradient-to-br from-amber-400 to-orange-500 rounded-full flex items-center justify-center shadow-lg">
                      <Star className="w-6 h-6 text-white fill-current" />
                    </div>
                    <div className="flex-1">
                      <div className="text-lg font-bold text-amber-800 dark:text-amber-300 mb-1">
                        🌟 Highly Contributed Concept!
                      </div>
                      <div className="text-sm text-amber-700 dark:text-amber-400">
                        {concept.contributionCount} brilliant minds have shaped this concept
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-2xl font-black text-amber-600 dark:text-amber-400">
                        {concept.contributionCount}
                      </div>
                      <div className="text-xs text-amber-600 dark:text-amber-500 uppercase tracking-wide">
                        Contributors
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* Notion-style linked references */}
              {(concept.backlinks && concept.backlinks.length > 0) || (concept.forwardLinks && concept.forwardLinks.length > 0) ? (
                <div className="mb-6">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center space-x-1">
                    <Link2 className="w-4 h-4" />
                    <span>Linked References</span>
                  </h4>

                  <div className="space-y-3">
                    {/* Backlinks */}
                    {concept.backlinks && concept.backlinks.length > 0 && (
                      <div>
                        <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-2">Referenced by</div>
                        <div className="space-y-2">
                          {concept.backlinks.slice(0, 3).map((backlink) => (
                            <button
                              key={backlink.id}
                              onClick={() => window.open(`/node/${backlink.id}`, '_blank')}
                              className="flex items-center space-x-2 p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 transition-colors text-left w-full"
                            >
                              <div className="w-6 h-6 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-xs font-semibold">
                                {backlink.name.charAt(0).toUpperCase()}
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="text-sm font-medium text-blue-700 dark:text-blue-300">{backlink.name}</div>
                                <div className="text-xs text-gray-500 dark:text-gray-400 capitalize">{backlink.type}</div>
                              </div>
                              <Reply className="w-4 h-4 text-gray-400 rotate-180" />
                            </button>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Forward links */}
                    {concept.forwardLinks && concept.forwardLinks.length > 0 && (
                      <div>
                        <div className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-2">References</div>
                        <div className="space-y-2">
                          {concept.forwardLinks.slice(0, 3).map((forwardLink) => (
                            <button
                              key={forwardLink.id}
                              onClick={() => window.open(`/node/${forwardLink.id}`, '_blank')}
                              className="flex items-center space-x-2 p-2 bg-green-50 dark:bg-green-900/20 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/30 transition-colors text-left w-full"
                            >
                              <div className="w-6 h-6 rounded-full bg-gradient-to-br from-green-400 to-blue-500 flex items-center justify-center text-white text-xs font-semibold">
                                {forwardLink.name.charAt(0).toUpperCase()}
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="text-sm font-medium text-green-700 dark:text-green-300">{forwardLink.name}</div>
                                <div className="text-xs text-gray-500 dark:text-gray-400 capitalize">{forwardLink.type}</div>
                              </div>
                              <Reply className="w-4 h-4 text-gray-400" />
                            </button>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              ) : null}

              {/* Related concepts with Notion-style organization */}
              {concept.relatedConcepts && concept.relatedConcepts.length > 0 && (
                <div className="mb-6">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center space-x-1">
                    <Network className="w-4 h-4" />
                    <span>Related Concepts</span>
                  </h4>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    {concept.relatedConcepts.map((related) => (
                      <button
                        key={related.id}
                        onClick={() => window.open(`/node/${related.id}`, '_blank')}
                        className="flex items-center space-x-3 p-3 bg-gradient-to-r from-slate-50 to-gray-50 dark:from-slate-800/50 dark:to-gray-800/50 rounded-lg hover:from-blue-50 hover:to-purple-50 dark:hover:from-blue-900/20 dark:hover:to-purple-900/20 transition-all hover:shadow-sm text-left group"
                      >
                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white text-sm font-bold group-hover:scale-110 transition-transform">
                          {related.name.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="text-sm font-semibold text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                            {related.name}
                          </div>
                          <div className="text-xs text-gray-500 dark:text-gray-400">
                            {Math.round(related.strength * 100)}% connection strength
                          </div>
                        </div>
                        <Network className="w-4 h-4 text-gray-400 group-hover:text-blue-500 transition-colors" />
                      </button>
                    ))}
                  </div>
                </div>
              )}

            {/* Thread replies section */}
            {concept.replies && concept.replies.length > 0 && (
              <div className="mb-6">
                <div className="flex items-center justify-between mb-3">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 flex items-center space-x-1">
                    <MessageCircle className="w-4 h-4" />
                    <span>Thread Replies</span>
                    <span className="px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-xs rounded-full">
                      {concept.replies.length}
                    </span>
                  </h4>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setShowReplies(!showReplies);
                    }}
                    className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 flex items-center space-x-1"
                  >
                    <span>{showReplies ? 'Hide' : 'Show'} replies</span>
                    {showReplies ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
                  </button>
                </div>

                {showReplies && (
                  <div className="space-y-3 pl-4 border-l-2 border-gray-200 dark:border-gray-600">
                    {concept.replies.slice(0, 3).map((reply, index) => (
                      <div key={reply.id} className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3">
                        <div className="flex items-center space-x-2 mb-2">
                          <div className="w-6 h-6 rounded-full bg-gradient-to-br from-green-400 to-blue-500 flex items-center justify-center text-white text-xs font-semibold">
                            {reply.name.charAt(0).toUpperCase()}
                          </div>
                          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{reply.name}</span>
                          <span className="text-xs text-gray-500 dark:text-gray-400">{reply.lastActivity}</span>
                        </div>
                        <p className="text-sm text-gray-600 dark:text-gray-300 line-clamp-2">{reply.description}</p>
                        <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500 dark:text-gray-400">
                          <button className="hover:text-blue-600 dark:hover:text-blue-400">Reply</button>
                          <button className="hover:text-green-600 dark:hover:text-green-400">Share</button>
                          <span>{reply.upvotes || 0} upvotes</span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Recent activity with enhanced social proof */}
            {concept.contributors && concept.contributors.length > 0 && (
              <div>
                <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 flex items-center space-x-1">
                  <Users className="w-4 h-4" />
                  <span>Community Activity</span>
                </h4>
                <div className="space-y-3 max-h-40 overflow-y-auto">
                  {concept.contributors.slice(0, 4).map((contributor, index) => (
                    <div key={contributor.id} className="flex items-center space-x-3 text-sm p-2 rounded-lg bg-gray-50 dark:bg-gray-800/50">
                      <div className="relative">
                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center text-white text-xs font-semibold border-2 border-white dark:border-gray-800">
                          {contributor.avatar ? (
                            <img
                              src={contributor.avatar}
                              alt={contributor.name}
                              className="w-full h-full rounded-full object-cover"
                            />
                          ) : (
                            contributor.name.charAt(0).toUpperCase()
                          )}
                        </div>
                        {index === 0 && concept.contributors && concept.contributors.length > 1 && (
                          <div className="absolute -top-1 -right-1 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-800"></div>
                        )}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-gray-700 dark:text-gray-300">
                          <span className="font-medium text-blue-600 dark:text-blue-400">{contributor.name}</span> {contributor.contribution}
                        </p>
                        <div className="flex items-center space-x-2 mt-1">
                          <p className="text-xs text-gray-500 dark:text-gray-400">{contributor.timestamp}</p>
                          {contributor.id === userId && (
                            <span className="px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-xs rounded-full">
                              You
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}

                  {concept.contributors.length > 4 && (
                    <div className="text-center pt-2">
                      <button className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium">
                        View all {concept.contributors.length} contributors
                      </button>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}
        </div>
      </CardContent>

      <CardFooter className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between border-t border-gray-200 dark:border-gray-700 pt-4 bg-gray-50/30 dark:bg-gray-800/20 rounded-b-2xl gap-3">
        {/* Reddit-style engagement bar with mobile optimization */}
        <div className="flex items-center justify-center sm:justify-start space-x-4 sm:space-x-6 flex-wrap">
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleAttune();
            }}
            disabled={attuneMutation.isPending}
            className={`flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-semibold transition-all hover:scale-105 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:ring-offset-2 ${
              isAttuned
                ? 'bg-gradient-to-r from-purple-500 to-pink-500 text-white shadow-md'
                : 'bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-purple-50 dark:hover:bg-purple-900/20 hover:text-purple-700 dark:hover:text-purple-300 border border-gray-200 dark:border-gray-600'
            }`}
            aria-label={`${isAttuned ? 'Remove attunement from' : 'Attune to'} concept: ${concept.name}`}
            aria-pressed={isAttuned}
          >
            <Heart className={`w-4 h-4 ${isAttuned ? 'fill-current' : ''}`} />
            <span>{isAttuned ? 'Attuned' : 'Attune'}</span>
            {isAttuned && <Sparkles className="w-3 h-3 animate-pulse" />}
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              handleAmplify();
            }}
            disabled={amplifyMutation.isPending || isAmplifying}
            className="flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-semibold bg-gradient-to-r from-yellow-400 to-orange-500 text-white hover:from-yellow-500 hover:to-orange-600 shadow-md transition-all hover:scale-105 focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-offset-2"
            aria-label={`${isAmplifying ? 'Amplifying' : 'Amplify'} concept: ${concept.name}`}
          >
            <Zap className="w-4 h-4" />
            <span>{isAmplifying ? 'Amplifying...' : 'Amplify'}</span>
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              // Open concept in new tab for reflection/exploration
              window.open(`/node/${concept.id}`, '_blank');
            }}
            className="flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-semibold bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:text-blue-700 dark:hover:text-blue-300 transition-all hover:scale-105 border border-gray-200 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            title="Reflect: open concept details to contemplate and comment"
            aria-label={`Reflect on concept: ${concept.name}`}
          >
            <MessageCircle className="w-4 h-4" />
            <span>Reflect</span>
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              handleShare();
            }}
            className="flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-semibold bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-green-50 dark:hover:bg-green-900/20 hover:text-green-700 dark:hover:text-green-300 transition-all hover:scale-105 border border-gray-200 dark:border-gray-600 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
            title="Share: send this concept to others or copy a link"
            aria-label={`Share concept: ${concept.name}`}
          >
            <Share2 className="w-4 h-4" />
            <span>Share</span>
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              handleBookmark();
            }}
            className={`flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-semibold transition-all hover:scale-105 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
              isBookmarked
                ? 'bg-blue-500 text-white'
                : 'bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:text-blue-700 dark:hover:text-blue-300 border border-gray-200 dark:border-gray-600'
            }`}
            title={isBookmarked ? 'Saved to your list' : 'Save to your list'}
            aria-label={`${isBookmarked ? 'Remove bookmark from' : 'Bookmark'} concept: ${concept.name}`}
            aria-pressed={isBookmarked}
          >
            <Bookmark className={`w-4 h-4 ${isBookmarked ? 'fill-current' : ''}`} />
            <span>{isBookmarked ? 'Saved' : 'Save'}</span>
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              setShowMoreActions(prev => !prev);
            }}
            className="p-2 rounded-full text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-all focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2"
            aria-label={`More options for concept: ${concept.name}`}
          >
            <MoreHorizontal className="w-4 h-4" />
          </button>

          {showMoreActions && (
            <div className="relative">
              <div className="absolute right-0 mt-2 z-30 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-1 w-44">
                <button
                  onClick={(e) => { e.stopPropagation(); handleOpenConcept(); setShowMoreActions(false); }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  Open
                </button>
                <button
                  onClick={(e) => { e.stopPropagation(); handleCopyLink(); setShowMoreActions(false); }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  Copy link
                </button>
                <button
                  onClick={(e) => { e.stopPropagation(); handleShare(); setShowMoreActions(false); }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  Share…
                </button>
                <button
                  onClick={(e) => { e.stopPropagation(); handleBookmark(); setShowMoreActions(false); }}
                  className="w-full text-left px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  {isBookmarked ? 'Unsave' : 'Save'}
                </button>
                <div className="border-t border-gray-200 dark:border-gray-700 my-1"></div>
                <button
                  onClick={(e) => { e.stopPropagation(); handleReport(); setShowMoreActions(false); }}
                  className="w-full text-left px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                >
                  Report…
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Score and time info */}
        <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
          <div className="text-center">
            <div className="font-semibold">{totalScore > 0 ? '+' : ''}{totalScore}</div>
            <div>score</div>
          </div>
          <div className="text-center">
            <div className="font-semibold">{concept.lastActivity}</div>
            <div>updated</div>
          </div>
          <button
            onClick={(e) => {
              e.stopPropagation();
              window.open(`/node/${concept.id}`, '_blank');
            }}
            className="flex items-center space-x-2 px-4 py-2 rounded-full text-sm font-bold bg-gradient-to-r from-blue-500 to-purple-600 text-white hover:from-blue-600 hover:to-purple-700 shadow-lg transition-all hover:scale-105"
          >
            <Network className="w-4 h-4" />
            <span>Explore</span>
          </button>
        </div>
      </CardFooter>
    </Card>
  );
}
