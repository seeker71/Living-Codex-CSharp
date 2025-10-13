'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRealtimeNotifications } from '@/lib/realtime';
import { X, Trophy, Star, Award } from 'lucide-react';

interface AchievementNotif {
  id: string;
  type: 'badge' | 'achievement' | 'level_up' | 'milestone';
  title: string;
  description: string;
  icon: string;
  points?: number;
  rarity?: string;
  timestamp: string;
}

export function AchievementNotificationSystem() {
  const { user } = useAuth();
  const realtime = useRealtimeNotifications(user?.id);
  const [notifications, setNotifications] = useState<AchievementNotif[]>([]);
  const [dismissed, setDismissed] = useState<Set<string>>(new Set());

  // Listen for achievement notifications from realtime
  useEffect(() => {
    realtime.notifications.forEach(notif => {
      if (notif.type === 'achievement' || notif.event === 'badge_earned' || notif.event === 'level_up') {
        const achievementNotif: AchievementNotif = {
          id: notif.id || `${notif.event}-${Date.now()}`,
          type: notif.event === 'badge_earned' ? 'badge' : notif.event === 'level_up' ? 'level_up' : 'achievement',
          title: notif.title || notif.data?.title || 'Achievement Unlocked!',
          description: notif.message || notif.data?.description || 'You earned a new achievement',
          icon: notif.data?.icon || '🏆',
          points: notif.data?.points,
          rarity: notif.data?.rarity,
          timestamp: notif.timestamp || new Date().toISOString()
        };

        setNotifications(prev => {
          if (!prev.some(n => n.id === achievementNotif.id)) {
            return [achievementNotif, ...prev];
          }
          return prev;
        });

        // Auto-dismiss after 10 seconds
        setTimeout(() => {
          dismissNotification(achievementNotif.id);
        }, 10000);
      }
    });
  }, [realtime.notifications]);

  const dismissNotification = (id: string) => {
    setDismissed(prev => new Set([...prev, id]));
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'badge':
        return <Award className="w-6 h-6 text-yellow-400" />;
      case 'level_up':
        return <Star className="w-6 h-6 text-blue-400" />;
      case 'milestone':
        return <Trophy className="w-6 h-6 text-purple-400" />;
      default:
        return <Trophy className="w-6 h-6 text-green-400" />;
    }
  };

  const getRarityColor = (rarity?: string) => {
    switch (rarity?.toLowerCase()) {
      case 'legendary':
        return 'from-orange-500 to-red-500';
      case 'epic':
        return 'from-purple-500 to-pink-500';
      case 'rare':
        return 'from-blue-500 to-cyan-500';
      case 'uncommon':
        return 'from-green-500 to-teal-500';
      default:
        return 'from-gray-500 to-gray-600';
    }
  };

  const visibleNotifications = notifications.filter(n => !dismissed.has(n.id)).slice(0, 3);

  if (visibleNotifications.length === 0) {
    return null;
  }

  return (
    <div className="fixed top-20 right-4 z-50 space-y-3 max-w-sm">
      {visibleNotifications.map((notif) => (
        <div
          key={notif.id}
          className={`bg-gradient-to-r ${getRarityColor(notif.rarity)} p-1 rounded-lg shadow-2xl animate-in slide-in-from-right duration-500`}
        >
          <div className="bg-gray-900 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <div className="flex-shrink-0">
                {getIcon(notif.type)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2">
                  <h4 className="font-bold text-white text-sm">{notif.title}</h4>
                  <button
                    onClick={() => dismissNotification(notif.id)}
                    className="text-gray-400 hover:text-white transition-colors flex-shrink-0"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>
                <p className="text-gray-300 text-xs mt-1">{notif.description}</p>
                {notif.points && (
                  <div className="mt-2 flex items-center gap-2">
                    <span className="px-2 py-0.5 bg-yellow-500/20 text-yellow-400 text-xs rounded-full">
                      +{notif.points} points
                    </span>
                    {notif.rarity && (
                      <span className={`px-2 py-0.5 bg-gradient-to-r ${getRarityColor(notif.rarity)} text-white text-xs rounded-full`}>
                        {notif.rarity}
                      </span>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

