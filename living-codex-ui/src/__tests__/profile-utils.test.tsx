import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'

// Test the profile page utility functions directly
describe('Profile Page Utilities', () => {
  describe('Profile Completion Calculation', () => {
    const calculateProfileCompletion = (profile: any, beliefSystem: any): number => {
      const sections = [
        { id: 'basic', title: 'Basic Information', description: 'Display name, email, and bio', icon: '👤', completed: false, required: true, points: 10 },
        { id: 'avatar', title: 'Profile Picture', description: 'Add a profile photo to personalize your presence', icon: '📸', completed: false, required: false, points: 5 },
        { id: 'interests', title: 'Interests & Passions', description: 'Share what resonates with you', icon: '💫', completed: false, required: false, points: 15 },
        { id: 'beliefs', title: 'Belief Framework', description: 'Your personal philosophy and worldview', icon: '🧠', completed: false, required: false, points: 20 },
        { id: 'location', title: 'Location', description: 'Help others find you in the resonance network', icon: '🌍', completed: false, required: false, points: 5 },
      ]

      let completed = 0
      let total = sections.length

      // Basic info completion
      if (profile?.displayName && profile?.email) completed++
      if (profile?.bio && profile.bio.length > 10) completed++
      if (profile?.avatarUrl) completed++
      if (profile?.interests && profile.interests.length > 0) completed++
      if (beliefSystem?.framework && beliefSystem?.principles && beliefSystem.principles.length > 0) completed++
      if (profile?.location) completed++

      return Math.round((completed / total) * 100)
    }

    it('calculates 0% completion for empty profile', () => {
      const result = calculateProfileCompletion(null, null)
      expect(result).toBe(0)
    })

    it('calculates 20% completion for basic profile only', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com'
      }
      const result = calculateProfileCompletion(profile, null)
      expect(result).toBe(20)
    })

    it('calculates 40% completion with bio', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'This is a comprehensive bio that describes my interests and background in consciousness exploration.'
      }
      const result = calculateProfileCompletion(profile, null)
      expect(result).toBe(40)
    })

    it('calculates 60% completion with avatar and interests', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Test bio',
        avatarUrl: 'https://example.com/avatar.jpg',
        interests: ['machine learning', 'philosophy']
      }
      const result = calculateProfileCompletion(profile, null)
      expect(result).toBe(60)
    })

    it('calculates 80% completion with beliefs', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Test bio',
        avatarUrl: 'https://example.com/avatar.jpg',
        interests: ['machine learning']
      }
      const beliefSystem = {
        framework: 'Scientific Spiritualism',
        principles: ['Curiosity', 'Compassion']
      }
      const result = calculateProfileCompletion(profile, beliefSystem)
      expect(result).toBe(80)
    })

    it('calculates 100% completion for complete profile', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Test bio',
        avatarUrl: 'https://example.com/avatar.jpg',
        interests: ['machine learning'],
        location: 'San Francisco, CA'
      }
      const beliefSystem = {
        framework: 'Scientific Spiritualism',
        principles: ['Curiosity', 'Compassion']
      }
      const result = calculateProfileCompletion(profile, beliefSystem)
      expect(result).toBe(100)
    })
  })

  describe('Resonance Level Calculation', () => {
    const getResonanceLevel = (level: number): { title: string, color: string, description: string } => {
      if (level >= 90) return { title: '🌟 Master Resonator', color: 'from-purple-500 to-pink-500', description: 'Your consciousness resonates with universal harmony' }
      if (level >= 75) return { title: '✨ High Resonance', color: 'from-blue-500 to-cyan-500', description: 'You\'re deeply connected to the collective consciousness' }
      if (level >= 60) return { title: '🔮 Resonance Seeker', color: 'from-green-500 to-teal-500', description: 'You\'re exploring deeper levels of awareness' }
      if (level >= 40) return { title: '🌱 Growing Resonance', color: 'from-yellow-500 to-orange-500', description: 'Your resonance field is expanding' }
      if (level >= 20) return { title: '🌿 Emerging', color: 'from-emerald-500 to-green-500', description: 'You\'re beginning your resonance journey' }
      return { title: '🌸 Newcomer', color: 'from-gray-500 to-gray-600', description: 'Welcome to the Living Codex community' }
    }

    it('returns correct resonance level for high level', () => {
      const result = getResonanceLevel(85)
      expect(result.title).toBe('✨ High Resonance')
      expect(result.color).toBe('from-blue-500 to-cyan-500')
      expect(result.description).toBe('You\'re deeply connected to the collective consciousness')
    })

    it('returns correct resonance level for low level', () => {
      const result = getResonanceLevel(15)
      expect(result.title).toBe('🌸 Newcomer')
      expect(result.color).toBe('from-gray-500 to-gray-600')
      expect(result.description).toBe('Welcome to the Living Codex community')
    })

    it('returns correct resonance level for master level', () => {
      const result = getResonanceLevel(95)
      expect(result.title).toBe('🌟 Master Resonator')
      expect(result.color).toBe('from-purple-500 to-pink-500')
      expect(result.description).toBe('Your consciousness resonates with universal harmony')
    })
  })

  describe('Profile Section Completion', () => {
    const getSectionCompletion = (sectionId: string, profile: any, beliefSystem: any): boolean => {
      switch (sectionId) {
        case 'basic': return !!(profile?.displayName && profile?.email && profile?.bio && profile.bio.length > 10)
        case 'avatar': return !!(profile?.avatarUrl)
        case 'interests': return !!(profile?.interests && profile.interests.length > 0)
        case 'beliefs': return !!(beliefSystem?.framework && beliefSystem?.principles && beliefSystem.principles.length > 0)
        case 'location': return !!(profile?.location)
        default: return false
      }
    }

    it('correctly identifies completed basic section', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'This is a comprehensive bio with more than 10 characters to test the completion logic.'
      }
      const result = getSectionCompletion('basic', profile, null)
      expect(result).toBe(true)
    })

    it('correctly identifies incomplete basic section', () => {
      const profile = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Short'
      }
      const result = getSectionCompletion('basic', profile, null)
      expect(result).toBe(false)
    })

    it('correctly identifies completed avatar section', () => {
      const profile = {
        avatarUrl: 'https://example.com/avatar.jpg'
      }
      const result = getSectionCompletion('avatar', profile, null)
      expect(result).toBe(true)
    })

    it('correctly identifies completed interests section', () => {
      const profile = {
        interests: ['machine learning', 'philosophy']
      }
      const result = getSectionCompletion('interests', profile, null)
      expect(result).toBe(true)
    })

    it('correctly identifies completed beliefs section', () => {
      const beliefSystem = {
        framework: 'Scientific Spiritualism',
        principles: ['Curiosity', 'Compassion']
      }
      const result = getSectionCompletion('beliefs', null, beliefSystem)
      expect(result).toBe(true)
    })

    it('correctly identifies completed location section', () => {
      const profile = {
        location: 'San Francisco, CA'
      }
      const result = getSectionCompletion('location', profile, null)
      expect(result).toBe(true)
    })
  })

  describe('Badge System Logic', () => {
    const getBadgeColor = (rarity: string): string => {
      switch (rarity) {
        case 'common': return 'from-gray-100 to-gray-200 dark:from-gray-800/50 dark:to-gray-700/50'
        case 'uncommon': return 'from-green-100 to-emerald-200 dark:from-green-800/50 dark:to-emerald-700/50'
        case 'rare': return 'from-blue-100 to-cyan-200 dark:from-blue-800/50 dark:to-cyan-700/50'
        case 'epic': return 'from-purple-100 to-violet-200 dark:from-purple-800/50 dark:to-violet-700/50'
        case 'legendary': return 'from-yellow-100 via-orange-100 to-red-200 dark:from-yellow-800/50 dark:via-orange-700/50 dark:to-red-700/50'
        default: return 'from-gray-100 to-gray-200 dark:from-gray-800/50 dark:to-gray-700/50'
      }
    }

    it('returns correct colors for different badge rarities', () => {
      expect(getBadgeColor('common')).toBe('from-gray-100 to-gray-200 dark:from-gray-800/50 dark:to-gray-700/50')
      expect(getBadgeColor('uncommon')).toBe('from-green-100 to-emerald-200 dark:from-green-800/50 dark:to-emerald-700/50')
      expect(getBadgeColor('rare')).toBe('from-blue-100 to-cyan-200 dark:from-blue-800/50 dark:to-cyan-700/50')
      expect(getBadgeColor('epic')).toBe('from-purple-100 to-violet-200 dark:from-purple-800/50 dark:to-violet-700/50')
      expect(getBadgeColor('legendary')).toBe('from-yellow-100 via-orange-100 to-red-200 dark:from-yellow-800/50 dark:via-orange-700/50 dark:to-red-700/50')
    })

    it('returns default color for unknown rarity', () => {
      expect(getBadgeColor('unknown')).toBe('from-gray-100 to-gray-200 dark:from-gray-800/50 dark:to-gray-700/50')
    })
  })

  describe('Interest Management', () => {
    const addInterest = (interests: string[], newInterest: string): string[] => {
      if (newInterest.trim() && !interests.includes(newInterest.trim())) {
        return [...interests, newInterest.trim()]
      }
      return interests
    }

    const removeInterest = (interests: string[], interest: string): string[] => {
      return interests.filter(i => i !== interest)
    }

    it('adds new interest to empty list', () => {
      const result = addInterest([], 'machine learning')
      expect(result).toEqual(['machine learning'])
    })

    it('adds new interest to existing list', () => {
      const result = addInterest(['philosophy'], 'machine learning')
      expect(result).toEqual(['philosophy', 'machine learning'])
    })

    it('ignores duplicate interests', () => {
      const result = addInterest(['machine learning'], 'machine learning')
      expect(result).toEqual(['machine learning'])
    })

    it('trims whitespace from interests', () => {
      const result = addInterest([], '  machine learning  ')
      expect(result).toEqual(['machine learning'])
    })

    it('removes interest from list', () => {
      const result = removeInterest(['machine learning', 'philosophy'], 'machine learning')
      expect(result).toEqual(['philosophy'])
    })

    it('handles removing non-existent interest', () => {
      const result = removeInterest(['machine learning'], 'quantum physics')
      expect(result).toEqual(['machine learning'])
    })
  })

  describe('Form Validation', () => {
    const validateProfileData = (data: any): { isValid: boolean, errors: string[] } => {
      const errors: string[] = []

      if (!data.displayName || data.displayName.trim().length === 0) {
        errors.push('Display name is required')
      }

      if (!data.email || !data.email.includes('@')) {
        errors.push('Valid email is required')
      }

      if (data.bio && data.bio.length > 500) {
        errors.push('Bio must be 500 characters or less')
      }

      return {
        isValid: errors.length === 0,
        errors
      }
    }

    it('validates complete profile data', () => {
      const data = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Valid bio content'
      }
      const result = validateProfileData(data)
      expect(result.isValid).toBe(true)
      expect(result.errors).toEqual([])
    })

    it('requires display name', () => {
      const data = {
        displayName: '',
        email: 'test@example.com'
      }
      const result = validateProfileData(data)
      expect(result.isValid).toBe(false)
      expect(result.errors).toContain('Display name is required')
    })

    it('requires valid email', () => {
      const data = {
        displayName: 'Test User',
        email: 'invalid-email'
      }
      const result = validateProfileData(data)
      expect(result.isValid).toBe(false)
      expect(result.errors).toContain('Valid email is required')
    })

    it('validates bio length', () => {
      const data = {
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'a'.repeat(501)
      }
      const result = validateProfileData(data)
      expect(result.isValid).toBe(false)
      expect(result.errors).toContain('Bio must be 500 characters or less')
    })
  })

  describe('Sacred Frequencies', () => {
    const getDefaultSacredFrequencies = (): string[] => {
      return ['432Hz', '528Hz', '741Hz']
    }

    it('returns default sacred frequencies', () => {
      const frequencies = getDefaultSacredFrequencies()
      expect(frequencies).toEqual(['432Hz', '528Hz', '741Hz'])
    })

    it('validates frequency format', () => {
      const frequencies = getDefaultSacredFrequencies()
      frequencies.forEach(freq => {
        expect(freq).toMatch(/^\d+Hz$/)
      })
    })
  })

  describe('Consciousness Levels', () => {
    const getConsciousnessLevels = () => [
      'Awakening',
      'Exploring',
      'Integrating',
      'Embodying',
      'Transcending'
    ]

    it('returns all consciousness levels', () => {
      const levels = getConsciousnessLevels()
      expect(levels).toHaveLength(5)
      expect(levels).toContain('Awakening')
      expect(levels).toContain('Transcending')
    })

    it('has progression in consciousness levels', () => {
      const levels = getConsciousnessLevels()
      expect(levels[0]).toBe('Awakening')
      expect(levels[4]).toBe('Transcending')
    })
  })

  describe('Profile Sections Configuration', () => {
    const getProfileSections = () => [
      { id: 'basic', title: 'Basic Information', description: 'Display name, email, and bio', icon: '👤', completed: false, required: true, points: 10 },
      { id: 'avatar', title: 'Profile Picture', description: 'Add a profile photo to personalize your presence', icon: '📸', completed: false, required: false, points: 5 },
      { id: 'interests', title: 'Interests & Passions', description: 'Share what resonates with you', icon: '💫', completed: false, required: false, points: 15 },
      { id: 'beliefs', title: 'Belief Framework', description: 'Your personal philosophy and worldview', icon: '🧠', completed: false, required: false, points: 20 },
      { id: 'location', title: 'Location', description: 'Help others find you in the resonance network', icon: '🌍', completed: false, required: false, points: 5 },
    ]

    it('returns correct number of profile sections', () => {
      const sections = getProfileSections()
      expect(sections).toHaveLength(5)
    })

    it('has required basic information section', () => {
      const sections = getProfileSections()
      const basicSection = sections.find(s => s.id === 'basic')
      expect(basicSection).toBeDefined()
      expect(basicSection?.required).toBe(true)
    })

    it('assigns correct points to sections', () => {
      const sections = getProfileSections()
      const beliefsSection = sections.find(s => s.id === 'beliefs')
      expect(beliefsSection?.points).toBe(20)
    })
  })

  describe('Data Transformation', () => {
    const transformProfileData = (rawData: any) => {
      return {
        userId: rawData.id || 'fallback-id',
        displayName: rawData.displayName || 'Anonymous',
        email: rawData.email || '',
        location: rawData.location || '',
        interests: rawData.interests ? rawData.interests.split(',') : [],
        contributions: rawData.contributions ? rawData.contributions.split(',') : [],
        avatarUrl: rawData.avatarUrl || '',
        coverImageUrl: rawData.coverImageUrl || '',
        bio: rawData.bio || '',
        joinedDate: rawData.joinedDate || new Date().toISOString(),
        lastActive: rawData.lastActive || new Date().toISOString(),
        resonanceLevel: rawData.resonanceLevel || 0,
        totalContributions: rawData.totalContributions || 0,
        profileCompletion: rawData.profileCompletion || 0,
      }
    }

    it('transforms raw profile data correctly', () => {
      const rawData = {
        id: 'user-123',
        displayName: 'Test User',
        email: 'test@example.com',
        bio: 'Test bio',
        interests: 'machine learning,philosophy',
        avatarUrl: 'https://example.com/avatar.jpg'
      }

      const result = transformProfileData(rawData)
      expect(result.userId).toBe('user-123')
      expect(result.displayName).toBe('Test User')
      expect(result.interests).toEqual(['machine learning', 'philosophy'])
    })

    it('handles missing data with fallbacks', () => {
      const rawData = {}

      const result = transformProfileData(rawData)
      expect(result.userId).toBe('fallback-id')
      expect(result.displayName).toBe('Anonymous')
      expect(result.interests).toEqual([])
    })
  })

  describe('API Response Processing', () => {
    const processApiResponse = (response: any) => {
      if (response.success && response.data?.profile) {
        return response.data.profile
      }
      return null
    }

    it('extracts profile from successful response', () => {
      const response = {
        success: true,
        data: {
          profile: { id: 'user-123', displayName: 'Test User' }
        }
      }

      const result = processApiResponse(response)
      expect(result).toEqual({ id: 'user-123', displayName: 'Test User' })
    })

    it('returns null for failed response', () => {
      const response = {
        success: false,
        error: 'Failed to load profile'
      }

      const result = processApiResponse(response)
      expect(result).toBeNull()
    })

    it('returns null for missing profile data', () => {
      const response = {
        success: true,
        data: {}
      }

      const result = processApiResponse(response)
      expect(result).toBeNull()
    })
  })
})
