'use client';

import { usePathname } from 'next/navigation';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { useState, useEffect } from 'react';

export function Navigation() {
  const pathname = usePathname();
  const { isAuthenticated, user, isLoading } = useAuth();
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const publicNavItems = [
    { href: '/', label: 'Home', icon: '🏠' },
    { href: '/discover', label: 'Discover', icon: '🔍' },
    { href: '/news', label: 'News', icon: '📰' },
    { href: '/ontology', label: 'Ontology', icon: '🧠' },
    { href: '/graph', label: 'Graph', icon: '🕸️' },
    { href: '/resonance', label: 'Resonance', icon: '🌊' },
    { href: '/about', label: 'About', icon: 'ℹ️' },
  ];

  const authNavItems = [
    ...publicNavItems,
    { href: '/people', label: 'People', icon: '🌍' },
    { href: '/create', label: 'Create', icon: '✨' },
    { href: '/dev', label: 'Dev', icon: '🛠️' },
    { href: '/profile', label: 'Profile', icon: '👤' },
  ];

  const navItems = isAuthenticated ? authNavItems : publicNavItems;

  return (
    <nav className="flex items-center space-x-4">
      <div className="flex space-x-4">
        {navItems.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={`flex items-center space-x-2 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
              pathname === item.href
                ? 'text-blue-600 bg-blue-50'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
            }`}
          >
            <span>{item.icon}</span>
            <span>{item.label}</span>
          </Link>
        ))}
      </div>
      
      {/* Auth Status */}
      <div className="flex items-center space-x-2 border-l border-gray-200 pl-4">
        {!isClient || isLoading ? (
          <div className="w-16 h-8 bg-gray-200 rounded animate-pulse"></div>
        ) : isAuthenticated && user ? (
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center">
              <span className="text-xs font-bold text-white">
                {(user.displayName || user.username || 'U').charAt(0).toUpperCase()}
              </span>
            </div>
            <span className="text-sm text-gray-700">{user.displayName || user.username || 'User'}</span>
          </div>
        ) : (
          <Link
            href="/auth"
            className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            Sign In
          </Link>
        )}
      </div>
    </nav>
  );
}
