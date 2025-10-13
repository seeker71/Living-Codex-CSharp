'use client';

import { usePathname } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { useState, useEffect, useRef } from 'react';
import { api } from '@/lib/api';

export function Navigation() {
  const pathname = usePathname();
  const { isAuthenticated, user, isLoading } = useAuth();
  const [isClient, setIsClient] = useState(false);
  const [moreOpen, setMoreOpen] = useState(false);
  const moreMenuRef = useRef<HTMLDivElement | null>(null);
  const [userPoints, setUserPoints] = useState<{ totalPoints: number; level: number } | null>(null);

  useEffect(() => {
    setIsClient(true);
  }, []);

  // Load user gamification data
  useEffect(() => {
    if (isAuthenticated && user?.id) {
      api.get(`/gamification/points/${user.id}`)
        .then(response => {
          if (response.success && response.data) {
            setUserPoints({
              totalPoints: response.data.totalPoints,
              level: response.data.level
            });
          }
        })
        .catch(() => {
          // Silently fail if gamification not available
          setUserPoints(null);
        });
    } else {
      setUserPoints(null);
    }
  }, [isAuthenticated, user]);

  // Close the more menu on route change and outside clicks
  useEffect(() => {
    setMoreOpen(false);
  }, [pathname]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (moreMenuRef.current && !moreMenuRef.current.contains(e.target as Node)) {
        setMoreOpen(false);
      }
    }
    if (moreOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [moreOpen]);

  const publicNavItems = [
    { href: '/', label: 'Home', icon: '🏠' },
    { href: '/discover', label: 'Discover', icon: '🔍' },
    { href: '/news', label: 'News', icon: '📰' },
    { href: '/ontology', label: 'Ontology', icon: '🧠' },
    { href: '/code', label: 'Code', icon: '💻' },
    { href: '/graph', label: 'Graph', icon: '🕸️' },
    { href: '/resonance', label: 'Resonance', icon: '🌊' },
    { href: '/analytics', label: 'Analytics', icon: '📈' },
    { href: '/about', label: 'About', icon: 'ℹ️' },
  ];

  const authNavItems = [
    ...publicNavItems,
    { href: '/people', label: 'People', icon: '🌍' },
    { href: '/create', label: 'Create', icon: '✨' },
    { href: '/achievements', label: 'Achievements', icon: '🏆' },
    { href: '/activity', label: 'Activity', icon: '📊' },
    { href: '/notifications', label: 'Notifications', icon: '🔔' },
    { href: '/events', label: 'Events', icon: '📡' },
    { href: '/security', label: 'Security', icon: '🔐' },
    { href: '/predictions', label: 'Predictions', icon: '🔮' },
    { href: '/portals', label: 'Portals', icon: '🚪' },
    { href: '/admin', label: 'Admin', icon: '🛡️' },
    { href: '/ai-dashboard', label: 'AI Dashboard', icon: '🧠' },
    { href: '/dev', label: 'Dev', icon: '🛠️' },
    // Expose Swagger for developers
    { href: process.env.NEXT_PUBLIC_BACKEND_BASE_URL ? `${process.env.NEXT_PUBLIC_BACKEND_BASE_URL}/swagger` : 'http://127.0.0.1:5002/swagger', label: 'Swagger', icon: '📜' },
    { href: '/profile', label: 'Profile', icon: '👤' },
  ];

  // Note: Reflect navigation requires a conceptId, so we don't add it to the main nav
  // Instead, it's accessible from concept cards and detail pages

  let navItems = isAuthenticated ? authNavItems : publicNavItems;
  // Logical ordering: Explore → Create/Community → Dev/Code → About/Profile
  const orderWeight: Record<string, number> = {
    '/': 0,
    '/discover': 10,
    '/news': 20,
    '/ontology': 30,
    '/graph': 40,
    '/resonance': 50,
    '/analytics': 55,
    '/events': 57,
    '/security': 58,
    '/predictions': 59,
    '/people': 60,
    '/create': 70,
    '/achievements': 75,
    '/activity': 76,
    '/notifications': 77,
    '/portals': 80,
    '/admin': 82,
    '/ai-dashboard': 85,
    '/code': 90,
    '/dev': 100,
    '/profile': 110,
    '/about': 120,
  };
  // External swagger link weight (kept after dev/code)
  if (navItems.some(n => n.label === 'Swagger')) {
    orderWeight[navItems.find(n => n.label === 'Swagger')!.href] = 105;
  }
  navItems = navItems
    .slice()
    .sort((a, b) => (orderWeight[a.href] ?? 999) - (orderWeight[b.href] ?? 999));

  // Split into primary/secondary to avoid overflow; secondary goes under "More"
  const primaryItems = navItems.slice(0, 6);
  const secondaryItems = navItems.slice(6);

  return (
    <nav className="flex items-center justify-between gap-2 w-full">
      {/* Left: primary items with horizontal scroll on narrow viewports */}
      <div className="flex items-center gap-2 sm:gap-3 md:gap-4 overflow-x-auto whitespace-nowrap scrollbar-thin scrollbar-thumb-gray-300 dark:scrollbar-thumb-gray-700 pr-1">
        {primaryItems.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={`flex items-center space-x-2 px-3 py-2 rounded-md text-sm font-medium transition-colors border border-transparent ${
              pathname === item.href
                ? 'text-blue-600 dark:text-blue-400 bg-blue-50/60 dark:bg-blue-900/10 border-blue-200 dark:border-blue-800'
                : 'text-gray-700 dark:text-gray-200 hover:text-gray-900 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-700'
            }`}
          >
            <span>{item.icon}</span>
            <span>{item.label}</span>
          </Link>
        ))}
      </div>

      {/* More menu for secondary items - placed outside scroller to avoid clipping */}
      {secondaryItems.length > 0 && (
        <div className="relative" ref={moreMenuRef}>
          <button
            type="button"
            aria-haspopup="menu"
            aria-expanded={moreOpen}
            onClick={() => setMoreOpen(v => !v)}
            className="flex items-center space-x-2 px-3 py-2 rounded-md text-sm font-medium border border-transparent text-gray-700 dark:text-gray-200 hover:text-gray-900 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-700"
          >
            <span>⋯</span>
            <span>More</span>
          </button>
          {moreOpen && (
            <div role="menu" className="absolute mt-2 left-0 z-50 min-w-[200px] bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-2">
              {secondaryItems.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center justify-between px-3 py-2 text-sm transition-colors ${
                    pathname === item.href
                      ? 'text-blue-600 dark:text-blue-400 bg-blue-50/60 dark:bg-blue-900/10'
                      : 'text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }`}
                  role="menuitem"
                  onClick={() => setMoreOpen(false)}
                >
                  <span className="flex items-center gap-2">
                    <span>{item.icon}</span>
                    <span>{item.label}</span>
                  </span>
                </Link>
              ))}
            </div>
          )}
        </div>
      )}
      
      {/* Right: Auth Status */}
      <div className="flex items-center space-x-2 border-l border-gray-200 dark:border-gray-700 pl-2 sm:pl-4">
        {!isClient || isLoading ? (
          <div className="w-16 h-8 bg-gray-200 dark:bg-gray-700/60 rounded animate-pulse"></div>
        ) : isAuthenticated && user ? (
          <div className="flex items-center space-x-3">
            {/* Gamification Display */}
            {userPoints && (
              <Link 
                href="/achievements" 
                className="hidden sm:flex items-center space-x-2 px-3 py-1.5 bg-gradient-to-r from-purple-600/20 to-pink-600/20 border border-purple-500/30 rounded-lg hover:border-purple-500/50 transition-colors group"
                title="View achievements"
              >
                <span className="text-lg group-hover:scale-110 transition-transform">🏆</span>
                <div className="flex flex-col items-start">
                  <div className="text-xs font-bold text-purple-400">Level {userPoints.level}</div>
                  <div className="text-[10px] text-gray-400">{userPoints.totalPoints} pts</div>
                </div>
              </Link>
            )}
            
            {/* User Profile */}
            <Link href="/profile" className="flex items-center space-x-2 group">
              <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center group-hover:opacity-90">
                <span className="text-xs font-bold text-white">
                  {(user.displayName || user.username || 'U').charAt(0).toUpperCase()}
                </span>
              </div>
              <span className="hidden md:inline text-sm text-gray-800 dark:text-gray-100 group-hover:underline">{user.displayName || user.username || 'User'}</span>
            </Link>
          </div>
        ) : (
          <Link
            href="/auth"
            className="bg-blue-600/90 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-blue-600 transition-colors"
          >
            Sign In
          </Link>
        )}
      </div>
    </nav>
  );
}
