'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { api, endpoints } from '@/lib/api';

interface User {
  id: string;
  username: string;
  email: string;
  displayName: string;
  createdAt: string;
  isActive: boolean;
}

interface AuthState {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
}

interface AuthContextType extends AuthState {
  login: (username: string, password: string) => Promise<{ success: boolean; error?: string }>;
  register: (username: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
  refreshUser: () => Promise<void>;
  testConnection: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    token: null,
    isLoading: true,
    isAuthenticated: false,
  });
  const [isClient, setIsClient] = useState(false);

  // Ensure we're on the client side
  useEffect(() => {
    setIsClient(true);
  }, []);

  // Initialize auth state from localStorage (client-side only)
  useEffect(() => {
    if (!isClient) return;

    const initializeAuth = async () => {
      try {
        const storedToken = localStorage.getItem('auth_token');
        const storedUser = localStorage.getItem('auth_user');

        if (storedToken && storedUser) {
          const user = JSON.parse(storedUser);
          
          // Validate token with backend
          const response = await endpoints.validateToken(storedToken);
          
          if (response.success) {
            setAuthState({
              user,
              token: storedToken,
              isLoading: false,
              isAuthenticated: true,
            });
          } else {
            // Token invalid, clear storage
            localStorage.removeItem('auth_token');
            localStorage.removeItem('auth_user');
            setAuthState({
              user: null,
              token: null,
              isLoading: false,
              isAuthenticated: false,
            });
          }
        } else {
          setAuthState(prev => ({ ...prev, isLoading: false }));
        }
      } catch (error) {
        console.error('Auth initialization error:', error);
        setAuthState({
          user: null,
          token: null,
          isLoading: false,
          isAuthenticated: false,
        });
      }
    };

    initializeAuth();
  }, [isClient]);

  const login = async (username: string, password: string) => {
    try {
      console.log('🔐 Attempting login for:', username);
      const response = await endpoints.login(username, password, false);
      console.log('📡 Login response:', response);
      
      if (response.success && response.data) {
        const responseData = response.data as any;
        const { token, user: userData } = responseData;
        
        if (!token) {
          console.error('❌ No token in response:', responseData);
          return { success: false, error: 'No authentication token received from server' };
        }
        
        // Use the user data from the unified auth response
        const user: User = {
          id: userData?.id || `user.${username}`,
          username: userData?.username || username,
          email: userData?.email || '',
          displayName: userData?.displayName || userData?.username || username,
          createdAt: userData?.createdAt || new Date().toISOString(),
          isActive: userData?.isActive !== false,
        };

        // Store in localStorage
        localStorage.setItem('auth_token', token);
        localStorage.setItem('auth_user', JSON.stringify(user));

        setAuthState({
          user,
          token,
          isLoading: false,
          isAuthenticated: true,
        });

        console.log('✅ Login successful for:', user.username);
        return { success: true };
      } else {
        console.error('❌ Login failed:', response.error);
        return { success: false, error: response.error || 'Login failed - please check credentials' };
      }
    } catch (error) {
      console.error('❌ Network error during login:', error);
      return { 
        success: false, 
        error: `Network error: ${error instanceof Error ? error.message : 'Unable to connect to server'}. Please check if backend is running on port 5002.` 
      };
    }
  };

  const register = async (username: string, email: string, password: string) => {
    try {
      console.log('📝 Attempting registration for:', username, email);
      const response = await endpoints.register(username, email, password, username);
      console.log('📡 Registration response:', response);
      
      if (response.success && response.data) {
        const responseData = response.data as any;
        const { token, user: userData } = responseData;
        
        if (token && userData) {
          // Registration successful with immediate login
          const user: User = {
            id: userData.id || `user.${username}`,
            username: userData.username || username,
            email: userData.email || email,
            displayName: userData.displayName || username,
            createdAt: userData.createdAt || new Date().toISOString(),
            isActive: userData.isActive !== false,
          };

          localStorage.setItem('auth_token', token);
          localStorage.setItem('auth_user', JSON.stringify(user));

          setAuthState({
            user,
            token,
            isLoading: false,
            isAuthenticated: true,
          });

          console.log('✅ Registration successful for:', user.username);
          return { success: true };
        } else {
          console.log('⚠️ Registration successful but no immediate login - trying to login');
          // Registration successful but no immediate login - try to login
          return await login(username, password);
        }
      } else {
        console.error('❌ Registration failed:', response.error);
        return { success: false, error: response.error || 'Registration failed - please check your information' };
      }
    } catch (error) {
      console.error('❌ Network error during registration:', error);
      return { 
        success: false, 
        error: `Network error: ${error instanceof Error ? error.message : 'Unable to connect to server'}. Please check if backend is running on port 5002.` 
      };
    }
  };

  const testConnection = async () => {
    try {
      console.log('🔍 Testing backend connection...');
      const response = await endpoints.health();
      console.log('📡 Health check response:', response);
      return response.success;
    } catch (error) {
      console.error('❌ Backend connection failed:', error);
      return false;
    }
  };

  const logout = async () => {
    try {
      const token = authState.token;
      if (token) {
        // Call logout endpoint to invalidate token on server
        await endpoints.logout(token);
      }
    } catch (error) {
      console.error('Logout API call failed:', error);
    } finally {
      // Clear local storage and state regardless of API call result
      localStorage.removeItem('auth_token');
      localStorage.removeItem('auth_user');
      setAuthState({
        user: null,
        token: null,
        isLoading: false,
        isAuthenticated: false,
      });
    }
  };

  const refreshUser = async () => {
    if (!authState.user?.id) return;

    try {
      const response = await api.get(`/identity/users/${authState.user.id}`);
      if (response.success && response.data) {
        const user = response.data as User;
        localStorage.setItem('auth_user', JSON.stringify(user));
        setAuthState(prev => ({ ...prev, user }));
      }
    } catch (error) {
      console.error('Error refreshing user:', error);
    }
  };

  const value: AuthContextType = {
    ...authState,
    login,
    register,
    logout,
    refreshUser,
    testConnection,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

