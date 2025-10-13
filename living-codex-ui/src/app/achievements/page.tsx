'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { useRouter } from 'next/navigation';

interface UserBadge {
  badgeId: string;
  name: string;
  description: string;
  icon: string;
  rarity: string;
  points: number;
  earnedAt: string;
}

interface Achievement {
  achievementId: string;
  name: string;
  description: string;
  points: number;
  category: string;
  earnedAt: string;
}

interface UserPointsData {
  userId: string;
  totalPoints: number;
  level: number;
  badges: UserBadge[];
  achievements: Achievement[];
  lastUpdated: string;
}

interface AvailableBadge {
  badgeId: string;
  name: string;
  description: string;
  icon: string;
  rarity: string;
  points: number;
}

export default function AchievementsPage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [userPoints, setUserPoints] = useState<UserPointsData | null>(null);
  const [availableBadges, setAvailableBadges] = useState<AvailableBadge[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'badges' | 'achievements' | 'leaderboard'>('overview');

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/auth');
      return;
    }

    if (isAuthenticated && user?.id) {
      loadGamificationData();
    }
  }, [isAuthenticated, authLoading, user, router]);

  const loadGamificationData = async () => {
    if (!user?.id) return;

    try {
      setLoading(true);
      setError(null);

      // Load user points and achievements
      const pointsResponse = await api.get(`/gamification/points/${user.id}`);
      if (pointsResponse.success && pointsResponse.data) {
        setUserPoints(pointsResponse.data);
      }

      // Load available badges
      const badgesResponse = await api.get('/gamification/badges');
      if (badgesResponse.success && badgesResponse.badges) {
        setAvailableBadges(badgesResponse.badges);
      }
    } catch (err) {
      console.error('Error loading gamification data:', err);
      setError('Failed to load achievements. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity.toLowerCase()) {
      case 'common': return 'text-gray-400';
      case 'uncommon': return 'text-green-400';
      case 'rare': return 'text-blue-400';
      case 'epic': return 'text-purple-400';
      case 'legendary': return 'text-yellow-400';
      default: return 'text-gray-400';
    }
  };

  const getRarityBorder = (rarity: string) => {
    switch (rarity.toLowerCase()) {
      case 'common': return 'border-gray-400';
      case 'uncommon': return 'border-green-400';
      case 'rare': return 'border-blue-400';
      case 'epic': return 'border-purple-400';
      case 'legendary': return 'border-yellow-400';
      default: return 'border-gray-400';
    }
  };

  const getProgressToNextLevel = () => {
    if (!userPoints) return 0;
    const pointsInCurrentLevel = userPoints.totalPoints % 100;
    return (pointsInCurrentLevel / 100) * 100;
  };

  const getPointsToNextLevel = () => {
    if (!userPoints) return 100;
    return 100 - (userPoints.totalPoints % 100);
  };

  if (authLoading || loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="animate-pulse space-y-8">
            <div className="h-12 bg-gray-700 rounded w-1/3"></div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {[1, 2, 3].map(i => (
                <div key={i} className="h-32 bg-gray-700 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
        <div className="max-w-7xl mx-auto">
          <Card className="p-6 bg-red-900/20 border-red-500">
            <p className="text-red-400">{error}</p>
            <button
              onClick={loadGamificationData}
              className="mt-4 px-4 py-2 bg-red-600 hover:bg-red-700 rounded transition-colors"
            >
              Retry
            </button>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-purple-400 to-pink-400 bg-clip-text text-transparent">
            🏆 Achievements & Progression
          </h1>
          <p className="text-gray-400 mt-2">
            Track your journey through the Living Codex
          </p>
        </div>

        {/* Stats Overview */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <Card className="p-6 bg-gradient-to-br from-purple-900/50 to-pink-900/50 border-purple-500">
            <div className="text-center">
              <div className="text-5xl font-bold text-purple-400">{userPoints?.totalPoints || 0}</div>
              <div className="text-gray-400 mt-2">Total Points</div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-blue-900/50 to-cyan-900/50 border-blue-500">
            <div className="text-center">
              <div className="text-5xl font-bold text-blue-400">Level {userPoints?.level || 1}</div>
              <div className="text-gray-400 mt-2">Current Level</div>
              <div className="mt-3">
                <div className="w-full bg-gray-700 rounded-full h-2">
                  <div
                    className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${getProgressToNextLevel()}%` }}
                  ></div>
                </div>
                <div className="text-xs text-gray-500 mt-1">
                  {getPointsToNextLevel()} points to level {(userPoints?.level || 1) + 1}
                </div>
              </div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-yellow-900/50 to-orange-900/50 border-yellow-500">
            <div className="text-center">
              <div className="text-5xl font-bold text-yellow-400">{userPoints?.badges.length || 0}</div>
              <div className="text-gray-400 mt-2">Badges Earned</div>
              <div className="text-xs text-gray-500 mt-1">
                of {availableBadges.length} available
              </div>
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-green-900/50 to-emerald-900/50 border-green-500">
            <div className="text-center">
              <div className="text-5xl font-bold text-green-400">{userPoints?.achievements.length || 0}</div>
              <div className="text-gray-400 mt-2">Achievements</div>
            </div>
          </Card>
        </div>

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700">
          {(['overview', 'badges', 'achievements', 'leaderboard'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 font-medium transition-colors ${
                activeTab === tab
                  ? 'text-purple-400 border-b-2 border-purple-400'
                  : 'text-gray-400 hover:text-gray-300'
              }`}
            >
              {tab.charAt(0).toUpperCase() + tab.slice(1)}
            </button>
          ))}
        </div>

        {/* Content */}
        {activeTab === 'overview' && (
          <div className="space-y-6">
            {/* Recent Achievements */}
            <Card className="p-6">
              <h2 className="text-2xl font-bold text-purple-400 mb-4">Recent Achievements</h2>
              {userPoints?.achievements && userPoints.achievements.length > 0 ? (
                <div className="space-y-3">
                  {userPoints.achievements.slice(0, 5).map(achievement => (
                    <div
                      key={achievement.achievementId}
                      className="flex items-center justify-between p-4 bg-gray-800/50 rounded-lg border border-gray-700"
                    >
                      <div className="flex-1">
                        <div className="font-medium text-white">{achievement.name}</div>
                        <div className="text-sm text-gray-400">{achievement.description}</div>
                        <div className="text-xs text-gray-500 mt-1">
                          {new Date(achievement.earnedAt).toLocaleDateString()}
                        </div>
                      </div>
                      <div className="text-2xl font-bold text-purple-400">+{achievement.points}</div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-gray-500">
                  No achievements yet. Start exploring to earn your first achievement!
                </div>
              )}
            </Card>

            {/* Latest Badges */}
            <Card className="p-6">
              <h2 className="text-2xl font-bold text-purple-400 mb-4">Latest Badges</h2>
              {userPoints?.badges && userPoints.badges.length > 0 ? (
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  {userPoints.badges.slice(-4).reverse().map(badge => (
                    <div
                      key={badge.badgeId}
                      className={`p-4 bg-gray-800/50 rounded-lg border-2 ${getRarityBorder(badge.rarity)} text-center`}
                    >
                      <div className="text-6xl mb-2">{badge.icon}</div>
                      <div className={`font-bold ${getRarityColor(badge.rarity)}`}>{badge.name}</div>
                      <div className="text-xs text-gray-400 mt-1">{badge.description}</div>
                      <div className="text-xs text-gray-500 mt-2 capitalize">{badge.rarity}</div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-gray-500">
                  No badges yet. Complete activities to earn badges!
                </div>
              )}
            </Card>
          </div>
        )}

        {activeTab === 'badges' && (
          <Card className="p-6">
            <h2 className="text-2xl font-bold text-purple-400 mb-4">Badge Collection</h2>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-4">
              {availableBadges.map(badge => {
                const earned = userPoints?.badges.find(b => b.badgeId === badge.badgeId);
                return (
                  <div
                    key={badge.badgeId}
                    className={`p-4 rounded-lg border-2 text-center transition-all ${
                      earned
                        ? `${getRarityBorder(badge.rarity)} bg-gray-800/50`
                        : 'border-gray-700 bg-gray-800/20 opacity-50'
                    }`}
                  >
                    <div className="text-5xl mb-2">{badge.icon}</div>
                    <div className={`font-bold text-sm ${earned ? getRarityColor(badge.rarity) : 'text-gray-500'}`}>
                      {badge.name}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">{badge.description}</div>
                    <div className="text-xs mt-2">
                      <span className={earned ? getRarityColor(badge.rarity) : 'text-gray-600'}>
                        {badge.points} points
                      </span>
                    </div>
                    {earned && (
                      <div className="text-xs text-green-400 mt-1">
                        ✓ Earned {new Date(earned.earnedAt).toLocaleDateString()}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </Card>
        )}

        {activeTab === 'achievements' && (
          <Card className="p-6">
            <h2 className="text-2xl font-bold text-purple-400 mb-4">Achievement History</h2>
            {userPoints?.achievements && userPoints.achievements.length > 0 ? (
              <div className="space-y-3">
                {userPoints.achievements
                  .sort((a, b) => new Date(b.earnedAt).getTime() - new Date(a.earnedAt).getTime())
                  .map(achievement => (
                    <div
                      key={achievement.achievementId}
                      className="flex items-center justify-between p-4 bg-gray-800/50 rounded-lg border border-gray-700 hover:border-purple-500 transition-colors"
                    >
                      <div className="flex-1">
                        <div className="flex items-center space-x-3">
                          <div className="text-2xl">🏆</div>
                          <div>
                            <div className="font-medium text-white">{achievement.name}</div>
                            <div className="text-sm text-gray-400">{achievement.description}</div>
                            <div className="flex items-center space-x-4 mt-1">
                              <span className="text-xs text-gray-500">
                                {new Date(achievement.earnedAt).toLocaleString()}
                              </span>
                              <span className="text-xs px-2 py-1 bg-purple-900/30 text-purple-400 rounded">
                                {achievement.category}
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div className="text-2xl font-bold text-purple-400">+{achievement.points}</div>
                    </div>
                  ))}
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500">
                <div className="text-6xl mb-4">🎯</div>
                <div className="text-xl mb-2">No achievements yet</div>
                <div className="text-sm">Start exploring, creating, and collaborating to earn achievements!</div>
              </div>
            )}
          </Card>
        )}

        {activeTab === 'leaderboard' && (
          <Card className="p-6">
            <h2 className="text-2xl font-bold text-purple-400 mb-4">Leaderboard</h2>
            <div className="text-center py-12 text-gray-500">
              <div className="text-6xl mb-4">🏅</div>
              <div className="text-xl mb-2">Leaderboard Coming Soon</div>
              <div className="text-sm">See how you rank against other Living Codex explorers!</div>
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

