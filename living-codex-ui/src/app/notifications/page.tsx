'use client';

/**
 * Notifications Center
 * Connects to PushNotificationModule for notification management
 * Displays notification history, preferences, and subscription management
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { Bell, Settings, Clock, CheckCircle, XCircle, AlertCircle, Info, Zap, MessageSquare, Wifi, WifiOff } from 'lucide-react';
import { useRealtimeNotifications } from '@/lib/realtime';

interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  priority: 'low' | 'normal' | 'high' | 'urgent';
  createdAt: string;
  status: 'pending' | 'sent' | 'delivered' | 'read' | 'failed';
  data?: Record<string, any>;
}

interface NotificationSubscription {
  userId: string;
  notificationTypes: string[];
  channels: string[];
  filters: Record<string, any>;
  isActive: boolean;
  createdAt: string;
}

export default function NotificationsPage() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [subscription, setSubscription] = useState<NotificationSubscription | null>(null);
  const [activeTab, setActiveTab] = useState<'all' | 'unread' | 'preferences'>('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Real-time notifications via WebSocket
  const realtime = useRealtimeNotifications(user?.id);
  
  // Merge real-time notifications with existing ones
  useEffect(() => {
    if (realtime.notifications.length > 0) {
      setNotifications(prev => {
        const newNotifs = realtime.notifications.filter(
          rtn => !prev.some(n => n.id === rtn.id)
        );
        return [...newNotifs, ...prev];
      });
    }
  }, [realtime.notifications]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/auth');
      return;
    }

    if (isAuthenticated && user?.id) {
      loadNotificationsData();
    }
  }, [isAuthenticated, isLoading, user, router]);

  const loadNotificationsData = async () => {
    if (!user?.id) return;

    try {
      setLoading(true);
      setError(null);

      // Load notification history
      const historyRes = await api.get('/notifications/history');
      if (historyRes?.success && historyRes.notifications) {
        setNotifications(historyRes.notifications);
      }

      // Load subscription preferences (may not exist yet)
      // The backend doesn't have a GET endpoint for single user subscription,
      // so we'll just track if they're subscribed
    } catch (err) {
      console.error('Error loading notifications:', err);
      setError('Some features may be unavailable');
    } finally {
      setLoading(false);
    }
  };

  const handleSubscribe = async () => {
    if (!user?.id) return;

    try {
      const response = await api.post('/notifications/subscribe', {
        userId: user.id,
        notificationTypes: ['info', 'success', 'warning', 'error'],
        channels: ['web', 'push'],
        filters: {},
      });

      if (response.success) {
        setSubscription(response.subscription);
        alert('Successfully subscribed to notifications!');
      }
    } catch (error) {
      console.error('Failed to subscribe:', error);
      alert('Failed to subscribe to notifications');
    }
  };

  const handleUnsubscribe = async () => {
    if (!user?.id) return;

    try {
      const response = await api.delete(`/notifications/subscribe/${user.id}`);
      if (response.success) {
        setSubscription(null);
        alert('Successfully unsubscribed from notifications');
      }
    } catch (error) {
      console.error('Failed to unsubscribe:', error);
      alert('Failed to unsubscribe from notifications');
    }
  };

  const markAsRead = async (notificationId: string) => {
    // Update locally (backend doesn't have mark as read endpoint yet)
    setNotifications(prev =>
      prev.map(n => n.id === notificationId ? { ...n, status: 'read' as const } : n)
    );
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'success':
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case 'warning':
        return <AlertCircle className="w-5 h-5 text-yellow-500" />;
      case 'error':
        return <XCircle className="w-5 h-5 text-red-500" />;
      default:
        return <Info className="w-5 h-5 text-blue-500" />;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case 'urgent':
        return <span className="px-2 py-0.5 bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-400 text-xs rounded-full">Urgent</span>;
      case 'high':
        return <span className="px-2 py-0.5 bg-orange-100 dark:bg-orange-900/30 text-orange-800 dark:text-orange-400 text-xs rounded-full">High</span>;
      case 'low':
        return <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-400 text-xs rounded-full">Low</span>;
      default:
        return null;
    }
  };

  const filteredNotifications = activeTab === 'unread'
    ? notifications.filter(n => n.status !== 'read')
    : notifications;

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
        <div className="max-w-5xl mx-auto">
          <div className="animate-pulse space-y-6">
            <div className="h-12 bg-gray-700 rounded w-1/3"></div>
            <div className="space-y-3">
              {[1, 2, 3].map(i => (
                <div key={i} className="h-24 bg-gray-700 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900 p-8">
      <div className="max-w-5xl mx-auto space-y-8">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent flex items-center gap-3">
              <Bell className="w-10 h-10 text-blue-400" />
              Notifications
              {realtime.isConnected ? (
                <span className="flex items-center gap-1 text-sm text-green-400 font-normal">
                  <Wifi className="w-4 h-4" />
                  Live
                </span>
              ) : (
                <span className="flex items-center gap-1 text-sm text-gray-500 font-normal">
                  <WifiOff className="w-4 h-4" />
                  Offline
                </span>
              )}
            </h1>
            <p className="text-gray-400 mt-2">
              Stay updated with platform activity {realtime.unreadCount > 0 && `(${realtime.unreadCount} unread)`}
            </p>
          </div>
          <button
            onClick={() => setActiveTab('preferences')}
            className="flex items-center gap-2 px-4 py-2 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors"
          >
            <Settings className="w-4 h-4" />
            Settings
          </button>
        </div>

        {/* Error Message */}
        {error && (
          <Card className="p-4 bg-yellow-900/20 border-yellow-500">
            <p className="text-yellow-400 text-sm">{error}</p>
          </Card>
        )}

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700">
          {(['all', 'unread', 'preferences'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 font-medium transition-colors capitalize ${
                activeTab === tab
                  ? 'text-blue-400 border-b-2 border-blue-400'
                  : 'text-gray-400 hover:text-gray-300'
              }`}
            >
              {tab}
              {tab === 'unread' && notifications.filter(n => n.status !== 'read').length > 0 && (
                <span className="ml-2 px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">
                  {notifications.filter(n => n.status !== 'read').length}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Content */}
        {activeTab === 'preferences' ? (
          <div className="space-y-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-blue-400 mb-4 flex items-center gap-2">
                <Settings className="w-5 h-5" />
                Notification Preferences
              </h3>

              {subscription ? (
                <div className="space-y-4">
                  <div className="p-4 bg-green-900/20 border border-green-500 rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <CheckCircle className="w-5 h-5 text-green-400" />
                      <span className="font-medium text-green-400">Subscribed</span>
                    </div>
                    <p className="text-sm text-gray-400">
                      You're receiving notifications through: {subscription.channels.join(', ')}
                    </p>
                  </div>

                  <button
                    onClick={handleUnsubscribe}
                    className="px-4 py-2 bg-red-600 hover:bg-red-700 rounded-lg transition-colors"
                  >
                    Unsubscribe
                  </button>
                </div>
              ) : (
                <div className="space-y-4">
                  <p className="text-gray-400">
                    Subscribe to receive real-time notifications about platform activity
                  </p>

                  <div className="space-y-3">
                    <div className="p-3 bg-gray-800/50 rounded-lg">
                      <div className="flex items-center gap-2 mb-1">
                        <Zap className="w-4 h-4 text-yellow-500" />
                        <span className="font-medium text-white">Concept Activities</span>
                      </div>
                      <p className="text-sm text-gray-400">
                        When someone attunes to your concepts or amplifies them
                      </p>
                    </div>

                    <div className="p-3 bg-gray-800/50 rounded-lg">
                      <div className="flex items-center gap-2 mb-1">
                        <MessageSquare className="w-4 h-4 text-purple-500" />
                        <span className="font-medium text-white">Discussion Replies</span>
                      </div>
                      <p className="text-sm text-gray-400">
                        When someone replies to your discussions
                      </p>
                    </div>

                    <div className="p-3 bg-gray-800/50 rounded-lg">
                      <div className="flex items-center gap-2 mb-1">
                        <Bell className="w-4 h-4 text-blue-500" />
                        <span className="font-medium text-white">System Updates</span>
                      </div>
                      <p className="text-sm text-gray-400">
                        Important platform announcements and updates
                      </p>
                    </div>
                  </div>

                  <button
                    onClick={handleSubscribe}
                    className="px-6 py-3 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors font-medium"
                  >
                    Enable Notifications
                  </button>
                </div>
              )}
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold text-purple-400 mb-4">Notification Channels</h3>
              <div className="space-y-3">
                <div className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg">
                  <div>
                    <div className="font-medium text-white">Web Notifications</div>
                    <div className="text-sm text-gray-400">In-browser alerts</div>
                  </div>
                  <div className="text-green-400 text-sm">Active</div>
                </div>

                <div className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg opacity-50">
                  <div>
                    <div className="font-medium text-white">Email Notifications</div>
                    <div className="text-sm text-gray-400">Receive updates via email</div>
                  </div>
                  <div className="text-gray-500 text-sm">Coming Soon</div>
                </div>

                <div className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg opacity-50">
                  <div>
                    <div className="font-medium text-white">Push Notifications</div>
                    <div className="text-sm text-gray-400">Mobile and desktop push</div>
                  </div>
                  <div className="text-gray-500 text-sm">Coming Soon</div>
                </div>
              </div>
            </Card>
          </div>
        ) : (
          <div className="space-y-4">
            {filteredNotifications.length === 0 ? (
              <Card className="p-12">
                <div className="text-center text-gray-500">
                  <Bell className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <div className="text-xl mb-2">
                    {activeTab === 'unread' ? 'No unread notifications' : 'No notifications yet'}
                  </div>
                  <p className="text-sm">
                    {activeTab === 'unread' 
                      ? 'All caught up! Check back later for updates.'
                      : 'Start exploring the platform to receive notifications about activity.'}
                  </p>
                </div>
              </Card>
            ) : (
              filteredNotifications.map(notification => (
                <Card
                  key={notification.id}
                  className={`p-4 transition-all cursor-pointer ${
                    notification.status === 'read' 
                      ? 'opacity-60 hover:opacity-80' 
                      : 'hover:border-blue-500'
                  }`}
                  onClick={() => markAsRead(notification.id)}
                >
                  <div className="flex items-start gap-4">
                    <div className="mt-1">
                      {getNotificationIcon(notification.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <h3 className="font-semibold text-white">{notification.title}</h3>
                        <div className="flex items-center gap-2">
                          {getPriorityBadge(notification.priority)}
                          {notification.status !== 'read' && (
                            <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                          )}
                        </div>
                      </div>
                      <p className="text-sm text-gray-300 mt-1">{notification.message}</p>
                      <div className="flex items-center gap-2 mt-2 text-xs text-gray-500">
                        <Clock className="w-3 h-3" />
                        <span>{new Date(notification.createdAt).toLocaleString()}</span>
                      </div>
                    </div>
                  </div>
                </Card>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  );
}

