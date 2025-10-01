'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { MapPin, Users, Clock, MessageCircle, Heart, ExternalLink } from 'lucide-react';
import { formatRelativeTime } from '@/lib/utils';

interface NearbyLensProps {
  controls?: Record<string, any>;
  userId?: string;
  className?: string;
  readOnly?: boolean;
}

interface NearbyUser {
  id: string;
  name: string;
  avatar?: string;
  location: string;
  distance: string;
  lastSeen: string;
  interests: string[];
  isOnline: boolean;
  mutualConnections: number;
}

export function NearbyLens({ controls = {}, userId, className = '', readOnly = false }: NearbyLensProps) {
  const [nearbyUsers, setNearbyUsers] = useState<NearbyUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);

  useEffect(() => {
    const fetchNearbyUsers = async () => {
      setLoading(true);
      setError(null);

      try {
        // Get user's location
        if (navigator.geolocation) {
          navigator.geolocation.getCurrentPosition(
            (position) => {
              setUserLocation({
                lat: position.coords.latitude,
                lng: position.coords.longitude
              });
            },
            (error) => {
              console.warn('Geolocation not available:', error);
              // Use mock location for demo
              setUserLocation({ lat: 40.7128, lng: -74.0060 }); // NYC
            }
          );
        } else {
          // Mock location for demo
          setUserLocation({ lat: 40.7128, lng: -74.0060 }); // NYC
        }

        // Fetch nearby users from API
        try {
          const response = await fetch('http://localhost:5002/users/discover', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
              location: userLocation ? { lat: userLocation.lat, lng: userLocation.lng } : null,
              radiusKm: 50,
              limit: 20
            })
          });

          if (response.ok) {
            const data = await response.json();
            if (data.success && data.users) {
              const nearbyUsers: NearbyUser[] = data.users.map((user: any) => ({
                id: user.id,
                name: user.displayName || user.name || 'Unknown User',
                location: user.location || 'Unknown Location',
                distance: user.distance ? `${user.distance} km` : 'Unknown',
                lastSeen: user.lastSeen || 'Unknown',
                interests: user.interests || [],
                isOnline: user.isOnline || false,
                mutualConnections: user.mutualConnections || 0
              }));
              setNearbyUsers(nearbyUsers);
            } else {
              throw new Error('No nearby users found from API');
            }
          } else {
            throw new Error(`Nearby Users API Error: HTTP ${response.status} - ${response.statusText}`);
          }
        } catch (error) {
          console.error('Error fetching nearby users:', error);
          setNearbyUsers([]);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load nearby users');
      } finally {
        setLoading(false);
      }
    };

    fetchNearbyUsers();
  }, [controls]);

  const handleConnect = (userId: string) => {
    if (readOnly || !userId) return;

    // Mock connection logic
    console.log('Connecting with user:', userId);
  };

  const getInterestColor = (interest: string) => {
    const colors: Record<string, string> = {
      'AI': 'bg-blue-100 text-blue-800',
      'Machine Learning': 'bg-green-100 text-green-800',
      'Art': 'bg-purple-100 text-purple-800',
      'Quantum Computing': 'bg-indigo-100 text-indigo-800',
      'Physics': 'bg-cyan-100 text-cyan-800',
      'Philosophy': 'bg-orange-100 text-orange-800',
      'Sustainability': 'bg-emerald-100 text-emerald-800',
      'Climate Science': 'bg-teal-100 text-teal-800',
      'Innovation': 'bg-pink-100 text-pink-800',
      'Blockchain': 'bg-yellow-100 text-yellow-800',
      'Cryptocurrency': 'bg-amber-100 text-amber-800',
      'Economics': 'bg-rose-100 text-rose-800',
    };
    return colors[interest] || 'bg-gray-100 text-gray-800';
  };

  if (loading) {
    return (
      <div className={`space-y-4 ${className}`}>
        {[...Array(4)].map((_, i) => (
          <div key={i} className="animate-pulse">
            <div className="bg-gray-200 rounded-lg h-32" />
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className}`}>
        <div className="text-red-800 font-medium">Error loading nearby users</div>
        <div className="text-red-600 text-sm mt-1">{error}</div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          People Nearby
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Discover and connect with people in your area who share your interests
        </p>
      </div>

      {!userLocation && (
        <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4 text-center">
          <MapPin className="w-8 h-8 text-amber-600 dark:text-amber-400 mx-auto mb-2" />
          <p className="text-amber-800 dark:text-amber-200 font-medium">
            Location access needed
          </p>
          <p className="text-amber-700 dark:text-amber-300 text-sm mt-1">
            Enable location services to see people nearby and find local connections
          </p>
        </div>
      )}

      <div className="space-y-4">
        {nearbyUsers.map((user) => (
          <Card key={user.id} className="group hover:shadow-lg transition-all duration-300 hover:-translate-y-0.5">
            <CardContent className="p-6">
              <div className="flex items-start space-x-4">
                {/* Avatar */}
                <div className="relative flex-shrink-0">
                  <div className="w-12 h-12 bg-gradient-to-br from-blue-400 to-purple-500 rounded-full flex items-center justify-center text-white font-semibold text-lg">
                    {user.avatar ? (
                      <img
                        src={user.avatar}
                        alt={user.name}
                        className="w-full h-full rounded-full object-cover"
                      />
                    ) : (
                      user.name.charAt(0).toUpperCase()
                    )}
                  </div>
                  {user.isOnline && (
                    <div className="absolute -bottom-0.5 -right-0.5 w-4 h-4 bg-green-500 rounded-full border-2 border-white dark:border-gray-800"></div>
                  )}
                </div>

                {/* User Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center space-x-2 mb-1">
                    <h3 className="font-semibold text-gray-900 dark:text-gray-100">
                      {user.name}
                    </h3>
                    <div className="flex items-center space-x-1 text-xs text-gray-500 dark:text-gray-400">
                      <MapPin className="w-3 h-3" />
                      <span>{user.distance}</span>
                    </div>
                  </div>

                  <p className="text-sm text-gray-600 dark:text-gray-300 mb-2">
                    {user.location}
                  </p>

                  <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400 mb-3">
                    <div className="flex items-center space-x-1">
                      <Clock className="w-3 h-3" />
                      <span>{formatRelativeTime(user.lastSeen)}</span>
                    </div>
                    {user.mutualConnections > 0 && (
                      <div className="flex items-center space-x-1">
                        <Users className="w-3 h-3" />
                        <span>{user.mutualConnections} mutual</span>
                      </div>
                    )}
                  </div>

                  {/* Interests */}
                  <div className="flex flex-wrap gap-1 mb-4">
                    {user.interests.slice(0, 4).map((interest) => (
                      <span
                        key={interest}
                        className={`px-2 py-1 text-xs rounded-full ${getInterestColor(interest)}`}
                      >
                        {interest}
                      </span>
                    ))}
                    {user.interests.length > 4 && (
                      <span className="px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-xs rounded-full">
                        +{user.interests.length - 4}
                      </span>
                    )}
                  </div>

                  {/* Actions */}
                  {!readOnly && userId && (
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => handleConnect(user.id)}
                        className="flex items-center space-x-1 px-3 py-1.5 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 transition-colors"
                      >
                        <Heart className="w-4 h-4" />
                        <span>Connect</span>
                      </button>
                      <button className="flex items-center space-x-1 px-3 py-1.5 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors">
                        <MessageCircle className="w-4 h-4" />
                        <span>Message</span>
                      </button>
                    </div>
                  )}
                </div>

                {/* View Profile */}
                <div className="flex-shrink-0">
                  <button className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors">
                    <ExternalLink className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {nearbyUsers.length === 0 && (
        <div className="text-center py-12">
          <MapPin className="w-16 h-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
            No one nearby right now
          </h3>
          <p className="text-gray-600 dark:text-gray-300">
            Check back later or expand your search radius to find more people.
          </p>
        </div>
      )}
    </div>
  );
}

export default NearbyLens;