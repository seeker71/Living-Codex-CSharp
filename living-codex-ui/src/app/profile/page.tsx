'use client'

import React, { useState, useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/lib/api'

interface UserProfile {
  userId: string
  displayName: string
  email: string
  location?: string
  latitude?: number
  longitude?: number
  interests?: string[]
  contributions?: string[]
  avatarUrl?: string
  metadata?: Record<string, any>
  coverImageUrl?: string
  bio?: string
  joinedDate?: string
  lastActive?: string
  resonanceLevel?: number
  totalContributions?: number
  profileCompletion?: number
  totalPoints?: number
  level?: number
  badges?: UserBadge[]
}

interface BeliefSystem {
  userId: string
  framework: string
  principles: string[]
  values: string[]
  language: string
  culturalContext: string
  spiritualTradition?: string
  scientificBackground?: string
  resonanceThreshold: number
  sacredFrequencies?: string[]
  consciousnessLevel?: string
}

interface ProfileSection {
  id: string
  title: string
  description: string
  icon: string
  completed: boolean
  required: boolean
  points: number
}

interface UserBadge {
  badgeId: string
  name: string
  description: string
  icon: string
  rarity: string
  points: number
  earnedAt: string
}

interface UserPointsData {
  userId: string
  totalPoints: number
  level: number
  badges: UserBadge[]
  achievements: any[]
  lastUpdated: string
}

const DEFAULT_PROFILE_SECTIONS: ProfileSection[] = [
  { id: 'basic', title: 'Basic Information', description: 'Display name, email, and bio', icon: '👤', completed: false, required: true, points: 10 },
  { id: 'avatar', title: 'Profile Picture', description: 'Add a profile photo to personalize your presence', icon: '📸', completed: false, required: false, points: 5 },
  { id: 'interests', title: 'Interests & Passions', description: 'Share what resonates with you', icon: '💫', completed: false, required: false, points: 15 },
  { id: 'beliefs', title: 'Belief Framework', description: 'Your personal philosophy and worldview', icon: '🧠', completed: false, required: false, points: 20 },
  { id: 'location', title: 'Location', description: 'Help others find you in the resonance network', icon: '🌍', completed: false, required: false, points: 5 }
]

export default function ProfilePage() {
  const auth = useAuth()
  const user = auth?.user
  const authLoading = auth?.isLoading ?? false
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [beliefSystem, setBeliefSystem] = useState<BeliefSystem | null>(null)
  // const [pointsData, setPointsData] = useState<UserPointsData | null>(null)
  const [loading, setLoading] = useState<boolean>(authLoading || !!user?.id)
  const [saving, setSaving] = useState(false)
  const [activeSection, setActiveSection] = useState('overview')
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null)
  const [showOnboarding, setShowOnboarding] = useState(false)

  // Form states
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [bio, setBio] = useState('')
  const [location, setLocation] = useState('')
  const [interests, setInterests] = useState<string[]>([])
  const [newInterest, setNewInterest] = useState('')
  const [framework, setFramework] = useState('')
  const [principles, setPrinciples] = useState<string[]>([])
  const [values, setValues] = useState<string[]>([])
  const [language, setLanguage] = useState('en')
  const [culturalContext, setCulturalContext] = useState('')
  const [spiritualTradition, setSpiritualTradition] = useState('')
  const [scientificBackground, setScientificBackground] = useState('')
  const [resonanceThreshold, setResonanceThreshold] = useState(0.7)
  const [consciousnessLevel, setConsciousnessLevel] = useState('')

  // Profile sections for completion tracking
  const [profileSections, setProfileSections] = useState<ProfileSection[]>(DEFAULT_PROFILE_SECTIONS)

  // Calculate profile completion
  const calculateProfileCompletion = (profile: UserProfile | null, beliefSystem: BeliefSystem | null): number => {
    let completed = 0
    const total = DEFAULT_PROFILE_SECTIONS.length

    // Basic info completion
    if (profile?.displayName && profile?.email) completed++
    if (profile?.bio && profile.bio.length > 10) completed++
    if (profile?.avatarUrl) completed++
    if (profile?.interests && profile.interests.length > 0) completed++
    if (beliefSystem?.framework && beliefSystem?.principles && beliefSystem.principles.length > 0) completed++
    if (profile?.location) completed++

    if (total === 0) return 0
    const cappedCompleted = Math.min(completed, total)
    return (cappedCompleted / total) * 100
  }

  // Get resonance level description
  const getResonanceLevel = (level: number): { title: string, color: string, description: string } => {
    if (level >= 90) return { title: '🌟 Master Resonator', color: 'from-purple-500 to-pink-500', description: 'Your consciousness resonates with universal harmony' }
    if (level >= 75) return { title: '🌟 High Resonance', color: 'from-blue-500 to-cyan-500', description: 'You\'re deeply connected to the collective consciousness' }
    if (level >= 60) return { title: '🔮 Resonance Seeker', color: 'from-green-500 to-teal-500', description: 'You\'re exploring deeper levels of awareness' }
    if (level >= 40) return { title: '🌱 Growing Resonance', color: 'from-yellow-500 to-orange-500', description: 'Your resonance field is expanding' }
    if (level >= 20) return { title: '🌿 Emerging', color: 'from-emerald-500 to-green-500', description: 'You&apos;re beginning your resonance journey' }
    return { title: '🌸 Newcomer', color: 'from-gray-500 to-gray-600', description: 'Welcome to the Living Codex community' }
  }

  useEffect(() => {
    let isCancelled = false

    if (authLoading) {
      setLoading(true)
      return () => {
        isCancelled = true
      }
    }

    if (!user?.id) {
      setProfile(null)
      setBeliefSystem(null)
      setLoading(false)
      return () => {
        isCancelled = true
      }
    }

    const loadAllData = async () => {
      try {
        await Promise.allSettled([
          loadUserProfile(),
          loadBeliefSystem()
        ])
      } finally {
        if (!isCancelled) {
          setLoading(false)
        }
      }
    }

    setLoading(true)
    loadAllData()

    return () => {
      isCancelled = true
    }
  }, [user?.id, authLoading])

  const loadUserProfile = async () => {
    try {
      if (!user?.id) return
      // Use unified auth profile endpoint to ensure email and displayName are populated
      const res = await api.get(`/auth/profile/${user.id}`)
      if (res.success && (res.data as any)?.profile) {
        const p = (res.data as any).profile
        const normalized: UserProfile = {
          userId: p.id || user.id,
          displayName: p.displayName || user.username || '',
          email: p.email || user.email || '',
          location: p.location || '',
          interests: Array.isArray(p.interests) ? p.interests : (typeof p.interests === 'string' ? p.interests.split(',').map((i: string) => i.trim()).filter(Boolean) : []),
          contributions: Array.isArray(p.contributions) ? p.contributions : (typeof p.contributions === 'string' ? p.contributions.split(',').map((c: string) => c.trim()).filter(Boolean) : []),
          avatarUrl: p.avatarUrl || '',
          coverImageUrl: p.coverImageUrl || '',
          bio: p.bio || '',
          joinedDate: p.joinedDate || new Date().toISOString(),
          lastActive: p.lastActive || new Date().toISOString(),
          resonanceLevel: p.resonanceLevel || 0,
          totalContributions: p.totalContributions || 0,
          profileCompletion: p.profileCompletion || 0,
        }
        setProfile(normalized)
        setDisplayName(normalized.displayName)
        setEmail(normalized.email)
        setBio(normalized.bio || '')
        setLocation(normalized.location || '')
        setInterests(normalized.interests || [])
      } else if (res.success && (res.data as any)?.data?.profile) {
        const p = (res.data as any).data.profile
        const normalized: UserProfile = {
          userId: p.id || user.id,
          displayName: p.displayName || user.username || '',
          email: p.email || user.email || '',
          location: p.location || '',
          interests: Array.isArray(p.interests) ? p.interests : (typeof p.interests === 'string' ? p.interests.split(',').map((i: string) => i.trim()).filter(Boolean) : []),
          contributions: Array.isArray(p.contributions) ? p.contributions : (typeof p.contributions === 'string' ? p.contributions.split(',').map((c: string) => c.trim()).filter(Boolean) : []),
          avatarUrl: p.avatarUrl || '',
          coverImageUrl: p.coverImageUrl || '',
          bio: p.bio || '',
          joinedDate: p.joinedDate || new Date().toISOString(),
          lastActive: p.lastActive || new Date().toISOString(),
          resonanceLevel: p.resonanceLevel || 0,
          totalContributions: p.totalContributions || 0,
          profileCompletion: p.profileCompletion || 0,
        }
        setProfile(normalized)
        setDisplayName(normalized.displayName)
        setEmail(normalized.email)
        setBio(normalized.bio || '')
        setLocation(normalized.location || '')
        setInterests(normalized.interests || [])

      } else {
        // Fallback to stored user
        const fallbackProfile: UserProfile = {
          userId: user?.id || '',
          displayName: user?.displayName || user?.username || '',
          email: user?.email || '',
          location: '',
          interests: [],
          contributions: [],
          bio: '',
          joinedDate: new Date().toISOString(),
          lastActive: new Date().toISOString(),
          resonanceLevel: 0,
          totalContributions: 0,
          profileCompletion: 0,
        }
        setProfile(fallbackProfile)
        setDisplayName(fallbackProfile.displayName)
        setEmail(fallbackProfile.email)
        setBio(fallbackProfile.bio || '')
        setLocation(fallbackProfile.location || '')
        setInterests(fallbackProfile.interests || [])
      }
    } catch (error) {
      console.error('Error loading user profile:', error)
    }
  }

  const getSectionCompletion = (sectionId: string, profile: UserProfile | null, beliefSystem: BeliefSystem | null): boolean => {
    switch (sectionId) {
      case 'basic': return !!(profile?.displayName && profile?.email && profile?.bio && profile.bio.length > 10)
      case 'avatar': return !!(profile?.avatarUrl)
      case 'interests': return !!(profile?.interests && profile.interests.length > 0)
      case 'beliefs': return !!(beliefSystem?.framework && beliefSystem?.principles && beliefSystem.principles.length > 0)
      case 'location': return !!(profile?.location)
      default: return false
    }
  }

  const loadBeliefSystem = async () => {
    try {
      if (!user?.id) return
      const response = await api.get(`/userconcept/belief-system/${user.id}`)
      if (response.success && (response.data as any)) {
        const data: any = response.data
        if (data.success && data.beliefSystemId) {
          setBeliefSystem({
            userId: data.userId,
            framework: data.framework || '',
            principles: data.principles || [],
            values: data.values || [],
            language: data.language || 'en',
            culturalContext: data.culturalContext || '',
            spiritualTradition: data.spiritualTradition || '',
            scientificBackground: data.scientificBackground || '',
            resonanceThreshold: data.resonanceThreshold || 0.7,
            sacredFrequencies: data.sacredFrequencies || ['432Hz', '528Hz', '741Hz'],
            consciousnessLevel: data.consciousnessLevel || 'Awakening'
          })

          // Populate belief system form
          setFramework(data.framework || '')
          setPrinciples(data.principles || [])
          setValues(data.values || [])
          setLanguage(data.language || 'en')
          setCulturalContext(data.culturalContext || '')
          setSpiritualTradition(data.spiritualTradition || '')
          setScientificBackground(data.scientificBackground || '')
          setResonanceThreshold(data.resonanceThreshold || 0.7)
          setConsciousnessLevel(data.consciousnessLevel || 'Awakening')
        }
      }
    } catch (error) {
      console.error('Error loading belief system:', error)
    }
  }

  // const loadUserPoints = async () => {
  //   try {
  //     if (!user?.id) return
  //     const response = await api.get(`/points/${user.id}`)
  //     if (response.success && response.data) {
  //       setPointsData(response.data)
  //     }
  //   } catch (error) {
  //     console.error('Error loading user points:', error)
  //   }
  // }

  const saveProfile = async () => {
    setSaving(true)
    setMessage(null)
    
    try {
      if (!user?.id) return
      const profileData = {
        displayName,
        email,
        bio,
        location,
        interests,
        avatarUrl: profile?.avatarUrl || '',
        coverImageUrl: profile?.coverImageUrl || ''
      }

      const response = await api.put(`/auth/profile/${user.id}`, profileData)

      if (response.success) {
        setMessage({ type: 'success', text: 'Profile updated successfully!' })
        await loadUserProfile()
      } else {
        setMessage({ type: 'error', text: `Failed to update profile: ${response.error}` })
      }
    } catch (error) {
      setMessage({ type: 'error', text: `Error updating profile: ${error}` })
    } finally {
      setSaving(false)
    }
  }

  const saveBeliefSystem = async () => {
    setSaving(true)
    setMessage(null)
    
    try {
      if (!user?.id) return
      const beliefData = {
        userId: user.id,
        framework,
        principles,
        values,
        language,
        culturalContext,
        spiritualTradition,
        scientificBackground,
        coreValues: values.reduce((acc, val, idx) => ({ ...acc, [val]: idx + 1 }), {}),
        translationPreferences: { [language]: 1.0 },
        resonanceThreshold
      }

      const response = await api.post(`/userconcept/belief-system/register`, beliefData)

      if (response.success) {
        setMessage({ type: 'success', text: 'Belief system updated successfully!' })
        await loadBeliefSystem()
      } else {
        setMessage({ type: 'error', text: `Failed to update belief system: ${response.error}` })
      }
    } catch (error) {
      setMessage({ type: 'error', text: `Error updating belief system: ${error}` })
    } finally {
      setSaving(false)
    }
  }

  const addInterest = () => {
    if (newInterest.trim() && !interests.includes(newInterest.trim())) {
      setInterests([...interests, newInterest.trim()])
      setNewInterest('')
    }
  }

  const removeInterest = (interest: string) => {
    setInterests(interests.filter(i => i !== interest))
  }

  const addPrinciple = () => {
    const principle = prompt('Enter a principle:')
    if (principle && !principles.includes(principle)) {
      setPrinciples([...principles, principle])
    }
  }

  const addValue = () => {
    const value = prompt('Enter a value:')
    if (value && !values.includes(value)) {
      setValues([...values, value])
    }
  }

  useEffect(() => {
    setProfileSections(
      DEFAULT_PROFILE_SECTIONS.map((section) => ({
        ...section,
        completed: getSectionCompletion(section.id, profile, beliefSystem)
      }))
    )
  }, [profile, beliefSystem])

  if (loading) {
    return (
      <div className="min-h-screen bg-page text-foreground flex items-center justify-center">
        <div className="text-center">
          <div 
            className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"
            role="progressbar"
            aria-label="Loading profile"
          ></div>
          <p className="text-medium-contrast">Loading your resonance profile...</p>
        </div>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="min-h-screen bg-page text-foreground flex items-center justify-center">
        <div className="text-center">
          <p className="text-medium-contrast">Please log in to view your profile.</p>
        </div>
      </div>
    )
  }

  const rawCompletion = typeof profile?.profileCompletion === 'number' ? profile.profileCompletion : calculateProfileCompletion(profile, beliefSystem)
  const completion = Math.min(100, Math.max(0, Math.round(rawCompletion ?? 0)))
  const resonanceInfo = getResonanceLevel(profile?.resonanceLevel || 0)

  // Get badge color based on rarity
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

  // Render the active section form
  const renderActiveSection = () => {
    const activeSectionData = profileSections.find(s => s.id === activeSection)

    if (!activeSectionData) return null

    switch (activeSection) {
      case 'basic':
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <div className="text-4xl">{activeSectionData.icon}</div>
              <div>
                <h2 className="text-2xl font-bold text-high-contrast">{activeSectionData.title}</h2>
                <p className="text-medium-contrast">{activeSectionData.description}</p>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Display Name *
                </label>
                <input
                  type="text"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  className="input-standard w-full"
                  placeholder="Your display name"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Email *
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="input-standard w-full"
                  placeholder="your.email@example.com"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Bio
              </label>
              <textarea
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                rows={4}
                className="input-standard w-full resize-none"
                placeholder="Tell us about yourself, your interests, and what brings you to the Living Codex..."
              />
              <p className="text-xs text-medium-contrast mt-1">
                {bio.length}/500 characters
              </p>
            </div>

            <div className="flex justify-between items-center pt-6 border-t border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setActiveSection('overview')}
                className="px-4 py-2 text-medium-contrast hover:text-high-contrast transition-colors"
              >
                ← Back to Overview
              </button>
              <button
                onClick={saveProfile}
                disabled={saving}
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <span>💾</span>
                    Save Changes
                  </>
                )}
              </button>
            </div>
          </div>
        )

      case 'avatar':
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <div className="text-4xl">{activeSectionData.icon}</div>
              <div>
                <h2 className="text-2xl font-bold text-high-contrast">{activeSectionData.title}</h2>
                <p className="text-medium-contrast">{activeSectionData.description}</p>
              </div>
            </div>

            <div className="flex flex-col items-center space-y-6">
              <div className="relative">
                <div className="w-32 h-32 bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border-4 border-white dark:border-gray-800 overflow-hidden">
                  {profile?.avatarUrl ? (
                    <img
                      src={profile.avatarUrl}
                      alt={profile.displayName}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full bg-gradient-to-br from-blue-400 to-purple-600 flex items-center justify-center">
                      <span className="text-4xl text-white font-bold">
                        {profile?.displayName?.charAt(0)?.toUpperCase() || 'U'}
                      </span>
                    </div>
                  )}
                </div>
                <button className="absolute -bottom-2 -right-2 w-8 h-8 bg-blue-600 hover:bg-blue-700 text-white rounded-full flex items-center justify-center shadow-lg hover-lift">
                  📷
                </button>
              </div>

              <div className="text-center max-w-md">
                <p className="text-medium-contrast mb-4">
                  Upload a profile picture to personalize your presence in the Living Codex community.
                  Your avatar helps others recognize you and makes your contributions more memorable.
                </p>

                <div className="space-y-3">
                  <button className="bg-green-600 hover:bg-green-700 text-white px-6 py-2 rounded-lg flex items-center gap-2 mx-auto transition-all duration-200 hover-lift">
                    <span>📤</span>
                    Upload New Photo
                  </button>

                  <div className="grid grid-cols-3 gap-2">
                    <button className="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors text-center">
                      <div className="text-lg mb-1">😊</div>
                      <div className="text-xs text-medium-contrast">Friendly</div>
                    </button>
                    <button className="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors text-center">
                      <div className="text-lg mb-1">🌟</div>
                      <div className="text-xs text-medium-contrast">Mystical</div>
                    </button>
                    <button className="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors text-center">
                      <div className="text-lg mb-1">🎨</div>
                      <div className="text-xs text-medium-contrast">Creative</div>
                    </button>
                  </div>
                </div>

                <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-700 rounded-lg">
                  <h4 className="font-medium text-blue-800 dark:text-blue-200 mb-1">Avatar Tips</h4>
                  <ul className="text-xs text-blue-700 dark:text-blue-300 space-y-1">
                    <li>• Use a clear, high-quality image</li>
                    <li>• Show your face for better recognition</li>
                    <li>• Keep it appropriate for the community</li>
                    <li>• Square images work best</li>
                  </ul>
                </div>

                <p className="text-xs text-medium-contrast mt-3">
                  Supported formats: JPG, PNG, GIF • Max size: 5MB
                </p>
              </div>
            </div>

            <div className="flex justify-between items-center pt-6 border-t border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setActiveSection('overview')}
                className="px-4 py-2 text-medium-contrast hover:text-high-contrast transition-colors"
              >
                ← Back to Overview
              </button>
              <button
                onClick={saveProfile}
                disabled={saving}
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <span>💾</span>
                    Save Changes
                  </>
                )}
              </button>
            </div>
          </div>
        )

      case 'interests':
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <div className="text-4xl">{activeSectionData.icon}</div>
              <div>
                <h2 className="text-2xl font-bold text-high-contrast">{activeSectionData.title}</h2>
                <p className="text-medium-contrast">{activeSectionData.description}</p>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Add Interest
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={newInterest}
                  onChange={(e) => setNewInterest(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && addInterest()}
                  className="input-standard flex-1"
                  placeholder="e.g., machine learning, philosophy, music, consciousness, quantum physics..."
                />
                <button
                  onClick={addInterest}
                  className="bg-green-600 hover:bg-green-700 text-white px-4 py-2 rounded-lg flex items-center gap-2"
                >
                  <span>➕</span>
                  Add
                </button>
              </div>
            </div>

            <div>
              <h3 className="text-lg font-medium text-high-contrast mb-3">Your Interests</h3>
              <div className="flex flex-wrap gap-2">
                {interests.map((interest) => (
                  <span
                    key={interest}
                    className="bg-gradient-to-r from-blue-100 to-purple-100 dark:from-blue-900/30 dark:to-purple-900/30 text-blue-800 dark:text-blue-200 px-3 py-1 rounded-full text-sm flex items-center gap-2 border border-blue-200 dark:border-blue-700"
                  >
                    {interest}
                    <button
                      onClick={() => removeInterest(interest)}
                      className="text-blue-600 dark:text-blue-300 hover:text-red-500"
                    >
                      ×
                    </button>
                  </span>
                ))}
                {interests.length === 0 && (
                  <div className="w-full text-center py-8">
                    <div className="text-4xl mb-2">💭</div>
                    <p className="text-medium-contrast">No interests added yet. Share what resonates with you!</p>
                  </div>
                )}
              </div>
            </div>

            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-700 rounded-lg p-4">
              <h4 className="font-medium text-blue-800 dark:text-blue-200 mb-2">Why add interests?</h4>
              <ul className="text-sm text-blue-700 dark:text-blue-300 space-y-1">
                <li>• Discover like-minded people in your resonance field</li>
                <li>• Get personalized concept recommendations</li>
                <li>• Connect with communities that share your passions</li>
                <li>• Help others find you through shared interests</li>
              </ul>
            </div>

            <div className="flex justify-between items-center pt-6 border-t border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setActiveSection('overview')}
                className="px-4 py-2 text-medium-contrast hover:text-high-contrast transition-colors"
              >
                ← Back to Overview
              </button>
              <button
                onClick={saveProfile}
                disabled={saving}
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <span>💾</span>
                    Save Interests
                  </>
                )}
              </button>
            </div>
          </div>
        )

      case 'beliefs':
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <div className="text-4xl">{activeSectionData.icon}</div>
              <div>
                <h2 className="text-2xl font-bold text-high-contrast">{activeSectionData.title}</h2>
                <p className="text-medium-contrast">{activeSectionData.description}</p>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Belief Framework
                </label>
                <input
                  type="text"
                  value={framework}
                  onChange={(e) => setFramework(e.target.value)}
                  className="input-standard w-full"
                  placeholder="e.g., Scientific Materialism, Buddhist Philosophy, Stoicism, Quantum Consciousness..."
                />
                <p className="text-xs text-medium-contrast mt-1">
                  Your foundational worldview or philosophical framework
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Consciousness Level
                </label>
                <select
                  value={consciousnessLevel}
                  onChange={(e) => setConsciousnessLevel(e.target.value)}
                  className="input-standard w-full"
                >
                  <option value="">Select your consciousness level</option>
                  <option value="Awakening">🌸 Awakening - Beginning the journey</option>
                  <option value="Exploring">🔍 Exploring - Actively seeking truth</option>
                  <option value="Integrating">🔗 Integrating - Synthesizing insights</option>
                  <option value="Embodying">💫 Embodying - Living the wisdom</option>
                  <option value="Transcending">🌟 Transcending - Beyond concepts</option>
                </select>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Cultural Context
              </label>
              <input
                type="text"
                value={culturalContext}
                onChange={(e) => setCulturalContext(e.target.value)}
                className="input-standard w-full"
                placeholder="e.g., Western, Eastern, Indigenous, Mixed, Global Citizen..."
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Spiritual Tradition
                </label>
                <input
                  type="text"
                  value={spiritualTradition}
                  onChange={(e) => setSpiritualTradition(e.target.value)}
                  className="input-standard w-full"
                  placeholder="Optional - e.g., Buddhism, Christianity, Hinduism, Paganism..."
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-medium-contrast mb-2">
                  Scientific Background
                </label>
                <input
                  type="text"
                  value={scientificBackground}
                  onChange={(e) => setScientificBackground(e.target.value)}
                  className="input-standard w-full"
                  placeholder="Optional - e.g., Physics, Biology, Psychology, Computer Science..."
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Resonance Threshold: {resonanceThreshold}
              </label>
              <input
                type="range"
                min="0"
                max="1"
                step="0.1"
                value={resonanceThreshold}
                onChange={(e) => setResonanceThreshold(parseFloat(e.target.value))}
                className="w-full"
              />
              <p className="text-sm text-medium-contrast mt-1">
                How aligned concepts need to be with your beliefs to resonate with you (0.0 = very open, 1.0 = very selective)
              </p>
            </div>

            <div>
              <h3 className="text-lg font-medium text-high-contrast mb-3">Core Principles</h3>
              <div className="flex flex-wrap gap-2 mb-2">
                {principles.map((principle, idx) => (
                  <span
                    key={idx}
                    className="bg-gradient-to-r from-purple-100 to-indigo-100 dark:from-purple-900/30 dark:to-indigo-900/30 text-purple-800 dark:text-purple-200 px-3 py-1 rounded-full text-sm border border-purple-200 dark:border-purple-700"
                  >
                    {principle}
                  </span>
                ))}
              </div>
              <button
                onClick={addPrinciple}
                className="text-blue-600 dark:text-blue-400 hover:underline text-sm flex items-center gap-1"
              >
                <span>➕</span>
                Add Principle
              </button>
            </div>

            <div>
              <h3 className="text-lg font-medium text-high-contrast mb-3">Core Values</h3>
              <div className="flex flex-wrap gap-2 mb-2">
                {values.map((value, idx) => (
                  <span
                    key={idx}
                    className="bg-gradient-to-r from-green-100 to-emerald-100 dark:from-green-900/30 dark:to-emerald-900/30 text-green-800 dark:text-green-200 px-3 py-1 rounded-full text-sm border border-green-200 dark:border-green-700"
                  >
                    {value}
                  </span>
                ))}
              </div>
              <button
                onClick={addValue}
                className="text-blue-600 dark:text-blue-400 hover:underline text-sm flex items-center gap-1"
              >
                <span>➕</span>
                Add Value
              </button>
            </div>

            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-700 rounded-lg p-4">
              <h4 className="font-medium text-purple-800 dark:text-purple-200 mb-2">Your Sacred Frequencies</h4>
              <div className="grid grid-cols-3 gap-2 text-sm">
                <div className="text-center p-2 bg-white dark:bg-gray-800 rounded border">
                  <div className="font-semibold text-purple-600 dark:text-purple-400">432Hz</div>
                  <div className="text-purple-600 dark:text-purple-400">Heart Harmony</div>
                </div>
                <div className="text-center p-2 bg-white dark:bg-gray-800 rounded border">
                  <div className="font-semibold text-purple-600 dark:text-purple-400">528Hz</div>
                  <div className="text-purple-600 dark:text-purple-400">DNA Repair</div>
                </div>
                <div className="text-center p-2 bg-white dark:bg-gray-800 rounded border">
                  <div className="font-semibold text-purple-600 dark:text-purple-400">741Hz</div>
                  <div className="text-purple-600 dark:text-purple-400">Intuition</div>
                </div>
              </div>
              <p className="text-xs text-purple-700 dark:text-purple-300 mt-2">
                These frequencies resonate with your consciousness and help align you with universal harmony.
              </p>
            </div>

            <div className="flex justify-between items-center pt-6 border-t border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setActiveSection('overview')}
                className="px-4 py-2 text-medium-contrast hover:text-high-contrast transition-colors"
              >
                ← Back to Overview
              </button>
              <button
                onClick={saveBeliefSystem}
                disabled={saving}
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <span>💾</span>
                    Save Belief System
                  </>
                )}
              </button>
            </div>
          </div>
        )

      case 'location':
        return (
          <div className="space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <div className="text-4xl">{activeSectionData.icon}</div>
              <div>
                <h2 className="text-2xl font-bold text-high-contrast">{activeSectionData.title}</h2>
                <p className="text-medium-contrast">{activeSectionData.description}</p>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-medium-contrast mb-2">
                Your Location
              </label>
              <input
                type="text"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                className="input-standard w-full"
                placeholder="e.g., San Francisco, CA, USA"
              />
              <p className="text-sm text-medium-contrast mt-1">
                Used for local news, events, and connecting with nearby users in your resonance field.
              </p>
            </div>

            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 rounded-lg p-4">
              <h4 className="font-medium text-green-800 dark:text-green-200 mb-2">Benefits of sharing your location:</h4>
              <ul className="text-sm text-green-700 dark:text-green-300 space-y-1">
                <li>• Discover local concepts and discussions</li>
                <li>• Connect with nearby resonance community</li>
                <li>• Get location-specific news and insights</li>
                <li>• Find in-person events and gatherings</li>
                <li>• Help build local consciousness networks</li>
              </ul>
            </div>

            <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
              <h4 className="font-medium text-yellow-800 dark:text-yellow-200 mb-2">🔒 Privacy & Safety</h4>
              <p className="text-sm text-yellow-700 dark:text-yellow-300">
                Your location is used only for personalization and discovery within the Living Codex community.
                You can control who sees your location in your privacy settings, and it&apos;s never shared with external services.
              </p>
            </div>

            <div className="flex justify-between items-center pt-6 border-t border-gray-200 dark:border-gray-700">
              <button
                onClick={() => setActiveSection('overview')}
                className="px-4 py-2 text-medium-contrast hover:text-high-contrast transition-colors"
              >
                ← Back to Overview
              </button>
              <button
                onClick={saveProfile}
                disabled={saving}
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <span>💾</span>
                    Save Location
                  </>
                )}
              </button>
            </div>
          </div>
        )

      default:
        return (
          <div className="text-center py-8">
            <div className="text-4xl mb-4">🚧</div>
            <h3 className="text-lg font-medium text-high-contrast mb-2">Section Coming Soon</h3>
            <p className="text-medium-contrast">This section is under development.</p>
            <button
              onClick={() => setActiveSection('overview')}
              className="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg"
            >
              Back to Overview
            </button>
          </div>
        )
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50 dark:from-gray-900 dark:via-slate-800 dark:to-gray-900">
      <main className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">

        {/* Profile Header */}
        <div className="relative mb-12">
          {/* Cover Image */}
          <div className="h-48 md:h-64 bg-gradient-to-r from-purple-400 via-blue-500 to-cyan-400 dark:from-purple-600 dark:via-blue-700 dark:to-cyan-600 rounded-2xl overflow-hidden shadow-xl">
            <div className="absolute inset-0 bg-black/20"></div>
            {profile?.coverImageUrl ? (
              <img
                src={profile.coverImageUrl}
                alt="Cover"
                className="w-full h-full object-cover"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center">
                <div className="text-white/80 text-center">
                  <div className="text-6xl mb-2">🌟</div>
                  <p className="text-lg font-medium">Your Consciousness Journey</p>
                </div>
              </div>
            )}
          </div>

          {/* Profile Info Overlay */}
          <div className="absolute -bottom-16 left-8 right-8">
            <div className="flex items-end gap-6">
              {/* Avatar */}
              <div className="relative">
                <div className="w-32 h-32 bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border-4 border-white dark:border-gray-800 overflow-hidden">
                  {profile?.avatarUrl ? (
                    <img
                      src={profile.avatarUrl}
                      alt={profile.displayName}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full bg-gradient-to-br from-blue-400 to-purple-600 flex items-center justify-center">
                      <span className="text-4xl text-white font-bold">
                        {profile?.displayName?.charAt(0)?.toUpperCase() || 'U'}
                      </span>
                    </div>
                  )}
                </div>
                <div className={`absolute -bottom-2 -right-2 w-8 h-8 rounded-full border-4 border-white dark:border-gray-800 flex items-center justify-center text-sm ${resonanceInfo.color} bg-gradient-to-r`}>
                  ✨
                </div>
              </div>

              {/* Profile Info */}
              <div className="flex-1 pb-4">
                <div className="flex items-center gap-4 mb-2">
                  <h1 className="text-3xl font-bold text-high-contrast">{profile?.displayName}</h1>
                  <div className={`px-3 py-1 rounded-full text-sm font-medium bg-gradient-to-r ${resonanceInfo.color} text-white shadow-lg`}>
                    {resonanceInfo.title}
                  </div>
                </div>
                <p className="text-medium-contrast mb-2">{resonanceInfo.description}</p>
                <div className="flex items-center gap-6 text-sm text-medium-contrast">
                  <span>Joined {profile?.joinedDate ? new Date(profile.joinedDate).toLocaleDateString() : 'Recently'}</span>
                  <span>•</span>
                  <span>{profile?.totalContributions || 0} contributions</span>
                  <span>•</span>
                  <span>{profile?.interests?.length || 0} interests</span>
                </div>
              </div>

              {/* Quick Actions */}
              <div className="flex gap-3 pb-4">
                <button className="px-4 py-2 bg-white dark:bg-gray-800 text-high-contrast rounded-lg shadow-lg hover:shadow-xl transition-all duration-200 hover-lift">
                  Edit Profile
                </button>
                <button className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg shadow-lg hover:shadow-xl transition-all duration-200">
                  Share Profile
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Profile Stats Summary */}
        <div className="bg-card rounded-2xl p-6 shadow-xl border border-card mb-8">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-xl font-semibold text-high-contrast mb-1">Your Profile Stats</h2>
              <p className="text-medium-contrast text-sm">Track your profile completion and activity</p>
            </div>
            <div className="text-right">
              <div className="text-3xl font-bold text-blue-600 dark:text-blue-400">{completion}%</div>
              <div className="text-sm text-medium-contrast">Complete</div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Profile Completion */}
            <div className="bg-gradient-to-br from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20 rounded-xl p-4 border border-blue-200 dark:border-blue-700">
              <div className="flex items-center gap-3 mb-3">
                <div className="text-2xl">📊</div>
                <div>
                  <h3 className="font-semibold text-blue-800 dark:text-blue-200">Profile Complete</h3>
                  <p className="text-sm text-blue-600 dark:text-blue-300">{completion}% finished</p>
                </div>
              </div>
              <div className="w-full bg-blue-200 dark:bg-blue-700/30 rounded-full h-2">
                <div
                  className="bg-gradient-to-r from-blue-500 to-cyan-500 h-2 rounded-full transition-all duration-500"
                  style={{ width: `${completion}%` }}
                ></div>
              </div>
            </div>

            {/* Interests */}
            <div className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 rounded-xl p-4 border border-green-200 dark:border-green-700">
              <div className="flex items-center gap-3 mb-3">
                <div className="text-2xl">💫</div>
                <div>
                  <h3 className="font-semibold text-green-800 dark:text-green-200">Interests</h3>
                  <p className="text-sm text-green-600 dark:text-green-300">{profile?.interests?.length || 0} added</p>
                </div>
              </div>
              <div className="text-sm text-green-700 dark:text-green-300">
                {profile?.interests?.length === 0 ? 'Add interests to connect with like-minded people' : 'Great! You&apos;re building your resonance network'}
              </div>
            </div>

            {/* Contributions */}
            <div className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 rounded-xl p-4 border border-purple-200 dark:border-purple-700">
              <div className="flex items-center gap-3 mb-3">
                <div className="text-2xl">🌟</div>
                <div>
                  <h3 className="font-semibold text-purple-800 dark:text-purple-200">Contributions</h3>
                  <p className="text-sm text-purple-600 dark:text-purple-300">{profile?.totalContributions || 0} shared</p>
                </div>
              </div>
              <div className="text-sm text-purple-700 dark:text-purple-300">
                {profile?.totalContributions === 0 ? 'Start sharing concepts to build your influence' : 'You&apos;re actively contributing to the community'}
              </div>
            </div>
          </div>
        </div>

        {/* Onboarding Flow for New Users */}
        {completion < 50 && (
          <div className="bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500 rounded-2xl p-8 shadow-xl text-white mb-8 relative overflow-hidden">
            <div className="absolute inset-0 bg-black/10"></div>
            <div className="relative z-10">
              <div className="flex items-center gap-4 mb-6">
                <div className="text-4xl animate-pulse">🌟</div>
                <div>
                  <h2 className="text-2xl font-bold mb-2">Welcome to Your Consciousness Journey!</h2>
                  <p className="text-indigo-100">Complete your profile to unlock the full resonance experience</p>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                {profileSections.filter(section => !section.completed).slice(0, 3).map((section, idx) => (
                  <div key={section.id} className="bg-white/10 backdrop-blur-sm rounded-lg p-4 border border-white/20">
                    <div className="flex items-center gap-3 mb-2">
                      <div className="text-2xl">{section.icon}</div>
                      <div>
                        <div className="font-medium">{section.title}</div>
                        <div className="text-xs text-indigo-100">+{section.points} points</div>
                      </div>
                    </div>
                    <p className="text-xs text-indigo-100">{section.description}</p>
                  </div>
                ))}
              </div>

              <div className="flex items-center justify-between">
                <div className="text-sm text-indigo-100">
                  Complete {profileSections.filter(s => !s.completed).length} more sections to reach 50%
                </div>
                <button
                  onClick={() => {
                    const nextIncomplete = profileSections.find(s => !s.completed)
                    if (nextIncomplete) setActiveSection(nextIncomplete.id)
                  }}
                  className="bg-white text-indigo-600 px-6 py-2 rounded-lg font-medium hover:bg-indigo-50 transition-colors"
                >
                  Start Journey →
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Profile Completion Progress */}
        <div className="mt-20 mb-8">
          <div className="bg-card rounded-2xl p-6 shadow-xl border border-card">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-xl font-semibold text-high-contrast mb-1">Profile Completion</h2>
                <p className="text-medium-contrast text-sm">Complete your profile to unlock the full resonance experience</p>
              </div>
              <div className="text-right">
                <div className="text-3xl font-bold text-blue-600 dark:text-blue-400">{completion}%</div>
                <div className="text-sm text-medium-contrast">Complete</div>
              </div>
            </div>

            {/* Progress Bar */}
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3 mb-6">
              <div
                className="bg-gradient-to-r from-blue-500 to-purple-600 h-3 rounded-full transition-all duration-500 ease-out"
                style={{ width: `${completion}%` }}
              ></div>
            </div>

            {/* Completion Rewards */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {[
                { threshold: 25, reward: '🎯 Basic Discovery', unlocked: completion >= 25 },
                { threshold: 50, reward: '🔮 Enhanced Resonance', unlocked: completion >= 50 },
                { threshold: 75, reward: '🌟 Advanced Insights', unlocked: completion >= 75 },
                { threshold: 100, reward: '✨ Master Level', unlocked: completion >= 100 },
              ].map((reward, idx) => (
                <div
                  key={idx}
                  className={`text-center p-3 rounded-lg border-2 transition-all duration-200 ${
                    reward.unlocked
                      ? 'border-green-300 dark:border-green-600 bg-green-50 dark:bg-green-900/20'
                      : 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50'
                  }`}
                >
                  <div className={`text-2xl mb-1 ${reward.unlocked ? '' : 'grayscale opacity-50'}`}>
                    {reward.unlocked ? '✅' : '🔒'}
                  </div>
                  <div className="text-xs font-medium text-high-contrast">{reward.reward}</div>
                  <div className="text-xs text-medium-contrast">{reward.threshold}%</div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Social Proof & Community Benefits */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
          {/* Community Impact */}
          <div className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 rounded-2xl p-6 border border-green-200 dark:border-green-700">
            <div className="flex items-center gap-3 mb-4">
              <div className="text-3xl">🌱</div>
              <div>
                <h3 className="font-semibold text-green-800 dark:text-green-200">Community Impact</h3>
                <p className="text-sm text-green-600 dark:text-green-300">Your contribution matters</p>
              </div>
            </div>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm text-green-700 dark:text-green-300">Resonance Field</span>
                <span className="text-sm font-medium text-green-800 dark:text-green-200">{profile?.resonanceLevel || 0}%</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-green-700 dark:text-green-300">Community Reach</span>
                <span className="text-sm font-medium text-green-800 dark:text-green-200">{profile?.totalContributions || 0} connections</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-green-700 dark:text-green-300">Consciousness Level</span>
                <span className="text-sm font-medium text-green-800 dark:text-green-200">{consciousnessLevel || 'Awakening'}</span>
              </div>
            </div>
          </div>

          {/* Discovery Benefits */}
          <div className="bg-gradient-to-br from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20 rounded-2xl p-6 border border-blue-200 dark:border-blue-700">
            <div className="flex items-center gap-3 mb-4">
              <div className="text-3xl">🔍</div>
              <div>
                <h3 className="font-semibold text-blue-800 dark:text-blue-200">Enhanced Discovery</h3>
                <p className="text-sm text-blue-600 dark:text-blue-300">Find your tribe</p>
              </div>
            </div>
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                <span className="text-sm text-blue-700 dark:text-blue-300">Personalized concept recommendations</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                <span className="text-sm text-blue-700 dark:text-blue-300">Connect with like-minded souls</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                <span className="text-sm text-blue-700 dark:text-blue-300">Local resonance community</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                <span className="text-sm text-blue-700 dark:text-blue-300">Belief-aligned content</span>
              </div>
            </div>
          </div>

          {/* Badge Collection & Gamification */}
          <div className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 rounded-2xl p-6 border border-purple-200 dark:border-purple-700">
            <div className="flex items-center gap-3 mb-6">
              <div className="text-3xl">🏆</div>
              <div>
                <h3 className="font-semibold text-purple-800 dark:text-purple-200">Consciousness Badges</h3>
                <p className="text-sm text-purple-600 dark:text-purple-300">Your journey of self-discovery</p>
              </div>
              <div className="ml-auto">
                <div className="bg-purple-100 dark:bg-purple-800/30 text-purple-800 dark:text-purple-200 px-3 py-1 rounded-full text-sm font-medium">
                  {profileSections.filter(s => s.completed).length}/{profileSections.length} Earned
                </div>
              </div>
            </div>

            {/* Badge Grid */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {[
                {
                  id: 'newcomer',
                  name: 'Newcomer',
                  icon: '🌸',
                  description: 'Welcome to the Living Codex',
                  requirement: 'Join the community',
                  earned: true,
                  rarity: 'common',
                  points: 5
                },
                {
                  id: 'first_steps',
                  name: 'First Steps',
                  icon: '👣',
                  description: 'Complete basic profile',
                  requirement: '25% profile completion',
                  earned: completion >= 25,
                  rarity: 'common',
                  points: 10
                },
                {
                  id: 'resonance_seeker',
                  name: 'Resonance Seeker',
                  icon: '🔮',
                  description: 'Explore your interests',
                  requirement: 'Add interests & beliefs',
                  earned: completion >= 50,
                  rarity: 'uncommon',
                  points: 25
                },
                {
                  id: 'belief_weaver',
                  name: 'Belief Weaver',
                  icon: '🧵',
                  description: 'Define your worldview',
                  requirement: 'Complete belief framework',
                  earned: beliefSystem?.framework && beliefSystem?.principles?.length > 0,
                  rarity: 'rare',
                  points: 50
                },
                {
                  id: 'consciousness_explorer',
                  name: 'Consciousness Explorer',
                  icon: '🧠',
                  description: 'Deepen your awareness',
                  requirement: '75% profile completion',
                  earned: completion >= 75,
                  rarity: 'epic',
                  points: 75
                },
                {
                  id: 'master_resonator',
                  name: 'Master Resonator',
                  icon: '🌟',
                  description: 'Achieve full harmony',
                  requirement: '100% profile completion',
                  earned: completion >= 100,
                  rarity: 'legendary',
                  points: 100
                },
                {
                  id: 'community_connector',
                  name: 'Community Connector',
                  icon: '🤝',
                  description: 'Build resonance networks',
                  requirement: 'Connect with 5+ people',
                  earned: profile?.totalContributions ? profile.totalContributions >= 5 : false,
                  rarity: 'rare',
                  points: 30
                },
                {
                  id: 'wisdom_sharer',
                  name: 'Wisdom Sharer',
                  icon: '📚',
                  description: 'Contribute to collective knowledge',
                  requirement: 'Share 10+ concepts',
                  earned: profile?.totalContributions ? profile.totalContributions >= 10 : false,
                  rarity: 'epic',
                  points: 60
                },
              ].map((badge) => (
                <div
                  key={badge.id}
                  className={`relative p-3 rounded-xl border-2 transition-all duration-200 hover:scale-105 ${
                    badge.earned
                      ? `bg-gradient-to-br ${getBadgeColor(badge.rarity)} border-current shadow-lg`
                      : 'bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 opacity-60'
                  }`}
                  title={badge.description} // Tooltip for full description
                >
                  <div className="text-center">
                    <div className={`text-2xl mb-2 ${badge.earned ? '' : 'grayscale'}`}>
                      {badge.icon}
                    </div>
                    <div className={`font-semibold text-xs mb-1 ${badge.earned ? 'text-current' : 'text-gray-500 dark:text-gray-400'} truncate`}>
                      {badge.name}
                    </div>
                    <div className={`text-xs mb-2 ${badge.earned ? 'text-current opacity-80' : 'text-gray-400 dark:text-gray-500'} line-clamp-2 h-8 overflow-hidden`}>
                      {badge.description}
                    </div>
                    <div className="flex items-center justify-center gap-1">
                      <span className="text-xs text-yellow-600 dark:text-yellow-400">⭐</span>
                      <span className="text-xs font-medium text-current">{badge.points}</span>
                    </div>
                  </div>
                  {!badge.earned && (
                    <div className="absolute inset-0 bg-gray-900/10 dark:bg-gray-900/20 rounded-xl flex items-center justify-center">
                      <div className="text-xs text-gray-500 dark:text-gray-400 font-medium">Locked</div>
                    </div>
                  )}
                </div>
              ))}
            </div>

            {/* Progress to Next Badge */}
            {(() => {
              const nextBadge = [
                { threshold: 25, badge: 'First Steps' },
                { threshold: 50, badge: 'Resonance Seeker' },
                { threshold: 75, badge: 'Consciousness Explorer' },
                { threshold: 100, badge: 'Master Resonator' },
              ].find(b => completion < b.threshold)

              return nextBadge && (
                <div className="mt-6 p-4 bg-purple-100 dark:bg-purple-800/30 rounded-lg border border-purple-200 dark:border-purple-700">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium text-purple-800 dark:text-purple-200">
                      Next Badge: {nextBadge.badge}
                    </span>
                    <span className="text-xs text-purple-600 dark:text-purple-400">
                      {nextBadge.threshold - completion}% remaining
                    </span>
                  </div>
                  <div className="w-full bg-purple-200 dark:bg-purple-700/30 rounded-full h-2">
                    <div
                      className="bg-gradient-to-r from-purple-500 to-pink-500 h-2 rounded-full transition-all duration-500"
                      style={{ width: `${Math.min(100, (completion / nextBadge.threshold) * 100)}%` }}
                    ></div>
                  </div>
                </div>
              )
            })()}
          </div>
        </div>

        {/* Profile Sections */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          {profileSections.map((section) => (
            <div
              key={section.id}
              className={`bg-card rounded-xl p-6 shadow-lg border-2 transition-all duration-200 hover-lift cursor-pointer ${
                section.completed
                  ? 'border-green-300 dark:border-green-600 bg-green-50/50 dark:bg-green-900/10'
                  : 'border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600'
              }`}
              onClick={() => setActiveSection(section.id)}
            >
              <div className="flex items-start gap-4">
                <div className={`text-3xl ${section.completed ? 'grayscale-0' : 'grayscale opacity-60'}`}>
                  {section.icon}
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h3 className="text-lg font-semibold text-high-contrast">{section.title}</h3>
                    {section.completed && (
                      <div className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center">
                        <span className="text-white text-xs">✓</span>
                      </div>
                    )}
                    {section.required && (
                      <span className="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 rounded-full">
                        Required
                      </span>
                    )}
                  </div>
                  <p className="text-medium-contrast text-sm mb-3">{section.description}</p>
                  <div className="flex items-center justify-between">
                    <div className="text-xs text-medium-contrast">
                      {section.completed ? 'Completed' : 'Incomplete'}
                    </div>
                    <div className="text-xs font-medium text-blue-600 dark:text-blue-400">
                      +{section.points} pts
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Detailed Section Forms */}
        {activeSection !== 'overview' && (
          <div className="bg-card rounded-2xl p-8 shadow-xl border border-card mb-8">
            {renderActiveSection()}
          </div>
        )}

        {/* Success Message */}
        {message && (
          <div className={`mb-6 p-4 rounded-xl shadow-lg ${
            message.type === 'success'
              ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-200 border border-green-300 dark:border-green-600'
              : 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-200 border border-red-300 dark:border-red-600'
          }`}>
            <div className="flex items-center gap-3">
              <div className="text-2xl">
                {message.type === 'success' ? '✅' : '❌'}
              </div>
              <div>
                <div className="font-medium">{message.type === 'success' ? 'Success!' : 'Error'}</div>
                <div className="text-sm opacity-90">{message.text}</div>
              </div>
            </div>
          </div>
        )}

        {/* Sacred Frequencies Integration */}
        {beliefSystem?.sacredFrequencies && beliefSystem.sacredFrequencies.length > 0 && (
          <div className="bg-card rounded-2xl p-6 shadow-xl border border-card mb-8">
            <h2 className="text-xl font-semibold text-high-contrast mb-4 flex items-center gap-2">
              🎵 Sacred Frequencies
              <span className="text-sm font-normal text-medium-contrast">Your personal resonance signature</span>
            </h2>
            <div className="grid grid-cols-3 gap-4">
              {beliefSystem.sacredFrequencies.map((freq, idx) => (
                <div key={idx} className="text-center p-4 bg-gradient-to-br from-purple-100 to-blue-100 dark:from-purple-900/30 dark:to-blue-900/30 rounded-lg border border-purple-200 dark:border-purple-700">
                  <div className="text-2xl mb-2">🔊</div>
                  <div className="font-semibold text-high-contrast">{freq}</div>
                  <div className="text-sm text-medium-contrast">Resonance</div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Consciousness Level */}
        {consciousnessLevel && (
          <div className="bg-gradient-to-r from-indigo-500 via-purple-500 to-pink-500 rounded-2xl p-6 shadow-xl text-white mb-8">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold mb-2">Consciousness Level</h2>
                <p className="text-indigo-100">{consciousnessLevel}</p>
              </div>
              <div className="text-4xl">🧠</div>
            </div>
          </div>
        )}

        {/* Quick Actions Footer */}
        <div className="bg-card rounded-2xl p-6 shadow-xl border border-card">
          <h2 className="text-xl font-semibold text-high-contrast mb-4">Quick Actions</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <button className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-700 hover:bg-blue-100 dark:hover:bg-blue-900/30 transition-colors text-center">
              <div className="text-2xl mb-2">🔗</div>
              <div className="text-sm font-medium text-high-contrast">Connect Portal</div>
            </button>
            <button className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-700 hover:bg-green-100 dark:hover:bg-green-900/30 transition-colors text-center">
              <div className="text-2xl mb-2">📝</div>
              <div className="text-sm font-medium text-high-contrast">Create Concept</div>
            </button>
            <button className="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg border border-purple-200 dark:border-purple-700 hover:bg-purple-100 dark:hover:bg-purple-900/30 transition-colors text-center">
              <div className="text-2xl mb-2">🔮</div>
              <div className="text-sm font-medium text-high-contrast">Explore Resonance</div>
            </button>
            <button className="p-4 bg-orange-50 dark:bg-orange-900/20 rounded-lg border border-orange-200 dark:border-orange-700 hover:bg-orange-100 dark:hover:bg-orange-900/30 transition-colors text-center">
              <div className="text-2xl mb-2">📊</div>
              <div className="text-sm font-medium text-high-contrast">View Analytics</div>
            </button>
          </div>
        </div>
      </main>
    </div>
  )
}