'use client';

import { useState, useCallback } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useTrackInteraction } from '@/lib/hooks';
import { buildApiUrl } from '@/lib/config';

interface WeaveRequest {
  sourceId: string;
  targetId: string;
  relationship: string;
  strength: number;
}

interface ReflectRequest {
  contentId: string;
  reflectionType: string;
  depth: number;
}

interface InviteRequest {
  targetUserId?: string;
  contentId: string;
  inviteType: string;
  message: string;
}

// Weave Primitive - Connect concepts, people, or content
export function WeaveAction({ 
  sourceId, 
  targetId, 
  onWeave,
  className = ""
}: {
  sourceId: string;
  targetId?: string;
  onWeave?: (result: any) => void;
  className?: string;
}) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [isWeaving, setIsWeaving] = useState(false);
  const [showWeaveDialog, setShowWeaveDialog] = useState(false);
  const [weaveConfig, setWeaveConfig] = useState({
    relationship: 'related',
    strength: 70
  });

  const performWeave = useCallback(async () => {
    if (!user?.id || !targetId) return;

    setIsWeaving(true);
    try {
      const response = await fetch(buildApiUrl('/weave/create'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sourceId,
          targetId,
          relationship: weaveConfig.relationship,
          strength: weaveConfig.strength / 100,
          userId: user.id
        })
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          onWeave?.(result);
          setShowWeaveDialog(false);
          
          // Track weave action
          trackInteraction(sourceId, 'weave', {
            description: `User wove connection: ${sourceId} → ${targetId}`,
            targetId,
            relationship: weaveConfig.relationship,
            strength: weaveConfig.strength
          });
        }
      }
    } catch (error) {
      console.error('Error performing weave:', error);
    } finally {
      setIsWeaving(false);
    }
  }, [user?.id, sourceId, targetId, weaveConfig, onWeave, trackInteraction]);

  if (!targetId) {
    return (
      <button
        onClick={() => setShowWeaveDialog(true)}
        className={`inline-flex items-center px-3 py-1 text-sm font-medium text-purple-600 hover:text-purple-800 transition-colors ${className}`}
      >
        🕸️ Weave
      </button>
    );
  }

  return (
    <>
      <button
        onClick={() => setShowWeaveDialog(true)}
        disabled={isWeaving}
        className={`inline-flex items-center px-3 py-1 text-sm font-medium text-purple-600 hover:text-purple-800 transition-colors disabled:opacity-50 ${className}`}
      >
        {isWeaving ? '🕸️ Weaving...' : '🕸️ Weave'}
      </button>

      {/* Weave Configuration Dialog */}
      {showWeaveDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-96 max-w-full">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🕸️ Weave Connection</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Relationship Type</label>
                <select
                  value={weaveConfig.relationship}
                  onChange={(e) => setWeaveConfig({...weaveConfig, relationship: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                >
                  <option value="related">🔗 Related</option>
                  <option value="similar">🤝 Similar</option>
                  <option value="complementary">⚖️ Complementary</option>
                  <option value="resonant">🌊 Resonant</option>
                  <option value="causal">➡️ Causal</option>
                  <option value="inspired_by">💡 Inspired By</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Connection Strength: {weaveConfig.strength}%
                </label>
                <input
                  type="range"
                  min="10"
                  max="100"
                  value={weaveConfig.strength}
                  onChange={(e) => setWeaveConfig({...weaveConfig, strength: parseInt(e.target.value)})}
                  className="w-full"
                />
              </div>
            </div>
            <div className="flex space-x-3 mt-6">
              <button
                onClick={performWeave}
                disabled={isWeaving}
                className="flex-1 px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 disabled:opacity-50"
              >
                {isWeaving ? 'Weaving...' : 'Create Weave'}
              </button>
              <button
                onClick={() => setShowWeaveDialog(false)}
                className="flex-1 px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

// Reflect Primitive - Deep contemplation and insight generation
export function ReflectAction({
  contentId,
  onReflect,
  className = ""
}: {
  contentId: string;
  onReflect?: (result: any) => void;
  className?: string;
}) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [isReflecting, setIsReflecting] = useState(false);
  const [showReflectDialog, setShowReflectDialog] = useState(false);
  const [reflectConfig, setReflectConfig] = useState({
    reflectionType: 'insight',
    depth: 3
  });

  const performReflect = useCallback(async () => {
    if (!user?.id) return;

    setIsReflecting(true);
    try {
      const response = await fetch(buildApiUrl('/reflect/generate'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contentId,
          reflectionType: reflectConfig.reflectionType,
          depth: reflectConfig.depth,
          userId: user.id
        })
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success && result.reflection) {
          onReflect?.(result);
          setShowReflectDialog(false);
          
          // Show reflection result
          alert(`💭 Reflection:\n\n${result.reflection.insight}\n\n🎯 Guidance: ${result.reflection.guidance}`);
          
          // Track reflect action
          trackInteraction(contentId, 'reflect', {
            description: `User reflected on content: ${contentId}`,
            reflectionType: reflectConfig.reflectionType,
            depth: reflectConfig.depth
          });
        }
      }
    } catch (error) {
      console.error('Error performing reflection:', error);
    } finally {
      setIsReflecting(false);
    }
  }, [user?.id, contentId, reflectConfig, onReflect, trackInteraction]);

  return (
    <>
      <button
        onClick={() => setShowReflectDialog(true)}
        disabled={isReflecting}
        className={`inline-flex items-center px-3 py-1 text-sm font-medium text-indigo-600 hover:text-indigo-800 transition-colors disabled:opacity-50 ${className}`}
      >
        {isReflecting ? '💭 Reflecting...' : '💭 Reflect'}
      </button>

      {/* Reflect Configuration Dialog */}
      {showReflectDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-96 max-w-full">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">💭 Deep Reflection</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Reflection Type</label>
                <select
                  value={reflectConfig.reflectionType}
                  onChange={(e) => setReflectConfig({...reflectConfig, reflectionType: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="insight">💡 Insight Generation</option>
                  <option value="wisdom">🧘 Wisdom Extraction</option>
                  <option value="connection">🔗 Connection Discovery</option>
                  <option value="transformation">🦋 Transformation Potential</option>
                  <option value="resonance">🌊 Resonance Analysis</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Reflection Depth: {reflectConfig.depth}
                </label>
                <input
                  type="range"
                  min="1"
                  max="10"
                  value={reflectConfig.depth}
                  onChange={(e) => setReflectConfig({...reflectConfig, depth: parseInt(e.target.value)})}
                  className="w-full"
                />
                <div className="flex justify-between text-xs text-gray-500 mt-1">
                  <span>Surface</span>
                  <span>Deep</span>
                </div>
              </div>
            </div>
            <div className="flex space-x-3 mt-6">
              <button
                onClick={performReflect}
                disabled={isReflecting}
                className="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50"
              >
                {isReflecting ? 'Reflecting...' : 'Begin Reflection'}
              </button>
              <button
                onClick={() => setShowReflectDialog(false)}
                className="flex-1 px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

// Invite Primitive - Share content and invite collaboration
export function InviteAction({
  contentId,
  onInvite,
  className = ""
}: {
  contentId: string;
  onInvite?: (result: any) => void;
  className?: string;
}) {
  const { user } = useAuth();
  const trackInteraction = useTrackInteraction();
  const [isInviting, setIsInviting] = useState(false);
  const [showInviteDialog, setShowInviteDialog] = useState(false);
  const [inviteConfig, setInviteConfig] = useState({
    inviteType: 'collaboration',
    message: '',
    targetUserId: ''
  });

  const performInvite = useCallback(async () => {
    if (!user?.id) return;

    setIsInviting(true);
    try {
      const response = await fetch(buildApiUrl('/invite/send'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contentId,
          inviteType: inviteConfig.inviteType,
          message: inviteConfig.message,
          targetUserId: inviteConfig.targetUserId || null,
          fromUserId: user.id
        })
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          onInvite?.(result);
          setShowInviteDialog(false);
          setInviteConfig({ inviteType: 'collaboration', message: '', targetUserId: '' });
          
          alert(`🤝 Invitation sent successfully!`);
          
          // Track invite action
          trackInteraction(contentId, 'invite', {
            description: `User sent invitation for content: ${contentId}`,
            inviteType: inviteConfig.inviteType,
            targetUserId: inviteConfig.targetUserId
          });
        }
      }
    } catch (error) {
      console.error('Error performing invite:', error);
    } finally {
      setIsInviting(false);
    }
  }, [user?.id, contentId, inviteConfig, onInvite, trackInteraction]);

  return (
    <>
      <button
        onClick={() => setShowInviteDialog(true)}
        disabled={isInviting}
        className={`inline-flex items-center px-3 py-1 text-sm font-medium text-green-600 hover:text-green-800 transition-colors disabled:opacity-50 ${className}`}
      >
        {isInviting ? '🤝 Inviting...' : '🤝 Invite'}
      </button>

      {/* Invite Configuration Dialog */}
      {showInviteDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-96 max-w-full">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🤝 Send Invitation</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Invitation Type</label>
                <select
                  value={inviteConfig.inviteType}
                  onChange={(e) => setInviteConfig({...inviteConfig, inviteType: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                >
                  <option value="collaboration">🤝 Collaboration</option>
                  <option value="exploration">🔍 Joint Exploration</option>
                  <option value="discussion">💬 Discussion</option>
                  <option value="contribution">🎯 Contribution</option>
                  <option value="resonance">🌊 Resonance Sharing</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Target User (optional)</label>
                <input
                  type="text"
                  value={inviteConfig.targetUserId}
                  onChange={(e) => setInviteConfig({...inviteConfig, targetUserId: e.target.value})}
                  placeholder="Leave empty for public invitation"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Message</label>
                <textarea
                  value={inviteConfig.message}
                  onChange={(e) => setInviteConfig({...inviteConfig, message: e.target.value})}
                  placeholder="Share your invitation message..."
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
            </div>
            <div className="flex space-x-3 mt-6">
              <button
                onClick={performInvite}
                disabled={isInviting || !inviteConfig.message.trim()}
                className="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {isInviting ? 'Sending...' : 'Send Invitation'}
              </button>
              <button
                onClick={() => setShowInviteDialog(false)}
                className="flex-1 px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

// Combined UX Primitives Component
export function UXPrimitives({
  contentId,
  targetId,
  showWeave = true,
  showReflect = true,
  showInvite = true,
  onWeave,
  onReflect,
  onInvite,
  className = ""
}: {
  contentId: string;
  targetId?: string;
  showWeave?: boolean;
  showReflect?: boolean;
  showInvite?: boolean;
  onWeave?: (result: any) => void;
  onReflect?: (result: any) => void;
  onInvite?: (result: any) => void;
  className?: string;
}) {
  return (
    <div className={`flex items-center space-x-2 ${className}`}>
      {showWeave && (
        <WeaveAction
          sourceId={contentId}
          targetId={targetId}
          onWeave={onWeave}
        />
      )}
      {showReflect && (
        <ReflectAction
          contentId={contentId}
          onReflect={onReflect}
        />
      )}
      {showInvite && (
        <InviteAction
          contentId={contentId}
          onInvite={onInvite}
        />
      )}
    </div>
  );
}
