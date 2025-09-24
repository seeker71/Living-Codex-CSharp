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
}

export default function ProfilePage() {
  const { user } = useAuth()
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [beliefSystem, setBeliefSystem] = useState<BeliefSystem | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [activeTab, setActiveTab] = useState<'profile' | 'interests' | 'beliefs' | 'location'>('profile')
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null)

  // Form states
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
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

  useEffect(() => {
    if (user?.id) {
      const loadAllData = async () => {
        try {
          await Promise.allSettled([
            loadUserProfile(),
            loadBeliefSystem()
          ])
        } finally {
          setLoading(false)
        }
      }
      loadAllData()
    }
  }, [user?.id])

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
          location: '',
          interests: [],
          contributions: [],
        }
        setProfile(normalized)
        setDisplayName(normalized.displayName)
        setEmail(normalized.email)
      } else {
        // Fallback to stored user
        const fallbackProfile: UserProfile = {
          userId: user?.id || '',
          displayName: user?.username || '',
          email: user?.email || '',
          location: '',
          interests: [],
          contributions: []
        }
        setProfile(fallbackProfile)
        setDisplayName(fallbackProfile.displayName)
        setEmail(fallbackProfile.email)
      }
    } catch (error) {
      console.error('Error loading user profile:', error)
    }
  }

  const loadBeliefSystem = async () => {
    try {
      const response = await fetch(`http://localhost:5002/userconcept/belief-system/${user?.id}`)
      if (response.ok) {
        const data = await response.json()
        if (data.success && data.beliefSystemId) {
          setBeliefSystem({
            userId: data.userId,
            framework: data.framework || '',
            principles: data.principles || [],
            values: data.values || [],
            language: 'en',
            culturalContext: '',
            resonanceThreshold: 0.7
          })
          
          // Populate belief system form
          setFramework(data.framework || '')
          setPrinciples(data.principles || [])
          setValues(data.values || [])
        }
      }
    } catch (error) {
      console.error('Error loading belief system:', error)
    }
  }

  const saveProfile = async () => {
    setSaving(true)
    setMessage(null)
    
    try {
      const profileData = {
        displayName,
        email,
        metadata: {
          location,
          interests: interests.join(','),
          contributions: profile?.contributions?.join(',') || ''
        }
      }

      const response = await api.put(`/identity/${user?.id}`, profileData)

      if (response.success) {
        setMessage({ type: 'success', text: 'Profile updated successfully!' })
        await loadUserProfile() // Reload to get updated data
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
      const beliefData = {
        userId: user?.id,
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
        await loadBeliefSystem() // Reload to get updated data
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

  if (loading) {
    return (
      <div className="min-h-screen bg-page text-foreground flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <p className="text-medium-contrast">Loading profile...</p>
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

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="max-w-4xl mx-auto">
          <h1 className="text-3xl font-bold text-high-contrast mb-8">My Profile</h1>

          {message && (
            <div className={`mb-6 p-4 rounded-lg ${
              message.type === 'success' 
                ? 'bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100' 
                : 'bg-red-100 text-red-800 dark:bg-red-800 dark:text-red-100'
            }`}>
              {message.text}
            </div>
          )}

          {/* Tab Navigation */}
          <div className="border-b border-gray-200 dark:border-gray-700 mb-8">
            <nav className="-mb-px flex space-x-8">
              {[
                { id: 'profile', label: '👤 Profile', icon: '👤' },
                { id: 'interests', label: '💡 Interests', icon: '💡' },
                { id: 'beliefs', label: '🧠 Belief System', icon: '🧠' },
                { id: 'location', label: '📍 Location', icon: '📍' }
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`py-2 px-1 border-b-2 font-medium text-sm ${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                      : 'border-transparent text-medium-contrast hover:text-high-contrast hover:border-gray-300'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>

          {/* Profile Tab */}
          {activeTab === 'profile' && (
            <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h2 className="text-xl font-semibold text-high-contrast mb-6">Basic Information</h2>
              
              <div className="space-y-6">
                <div>
                  <label className="block text-sm font-medium text-medium-contrast mb-2">
                    Display Name
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
                    Email
                  </label>
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="input-standard w-full"
                    placeholder="your.email@example.com"
                  />
                </div>

                <div className="flex justify-end">
                  <button
                    onClick={saveProfile}
                    disabled={saving}
                    className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save Profile'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Interests Tab */}
          {activeTab === 'interests' && (
            <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h2 className="text-xl font-semibold text-high-contrast mb-6">My Interests</h2>
              
              <div className="space-y-6">
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
                      placeholder="e.g., machine learning, philosophy, music"
                    />
                    <button
                      onClick={addInterest}
                      className="bg-green-600 hover:bg-green-700 text-white px-4 py-2 rounded-lg"
                    >
                      Add
                    </button>
                  </div>
                </div>

                <div>
                  <h3 className="text-lg font-medium text-high-contrast mb-3">Current Interests</h3>
                  <div className="flex flex-wrap gap-2">
                    {interests.map((interest) => (
                      <span
                        key={interest}
                        className="bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-100 px-3 py-1 rounded-full text-sm flex items-center gap-2"
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
                      <p className="text-medium-contrast italic">No interests added yet.</p>
                    )}
                  </div>
                </div>

                <div className="flex justify-end">
                  <button
                    onClick={saveProfile}
                    disabled={saving}
                    className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save Interests'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Belief System Tab */}
          {activeTab === 'beliefs' && (
            <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h2 className="text-xl font-semibold text-high-contrast mb-6">Belief System</h2>
              
              <div className="space-y-6">
                <div>
                  <label className="block text-sm font-medium text-medium-contrast mb-2">
                    Framework
                  </label>
                  <input
                    type="text"
                    value={framework}
                    onChange={(e) => setFramework(e.target.value)}
                    className="input-standard w-full"
                    placeholder="e.g., Scientific Materialism, Buddhist Philosophy, Stoicism"
                  />
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
                    placeholder="e.g., Western, Eastern, Indigenous, Mixed"
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
                      placeholder="Optional"
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
                      placeholder="Optional"
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
                    How aligned concepts need to be with your beliefs to resonate with you.
                  </p>
                </div>

                <div>
                  <h3 className="text-lg font-medium text-high-contrast mb-3">Principles</h3>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {principles.map((principle, idx) => (
                      <span
                        key={idx}
                        className="bg-purple-100 text-purple-800 dark:bg-purple-800 dark:text-purple-100 px-3 py-1 rounded-full text-sm"
                      >
                        {principle}
                      </span>
                    ))}
                  </div>
                  <button
                    onClick={addPrinciple}
                    className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
                  >
                    + Add Principle
                  </button>
                </div>

                <div>
                  <h3 className="text-lg font-medium text-high-contrast mb-3">Values</h3>
                  <div className="flex flex-wrap gap-2 mb-2">
                    {values.map((value, idx) => (
                      <span
                        key={idx}
                        className="bg-green-100 text-green-800 dark:bg-green-800 dark:text-green-100 px-3 py-1 rounded-full text-sm"
                      >
                        {value}
                      </span>
                    ))}
                  </div>
                  <button
                    onClick={addValue}
                    className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
                  >
                    + Add Value
                  </button>
                </div>

                <div className="flex justify-end">
                  <button
                    onClick={saveBeliefSystem}
                    disabled={saving}
                    className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save Belief System'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Location Tab */}
          {activeTab === 'location' && (
            <div className="bg-card border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <h2 className="text-xl font-semibold text-high-contrast mb-6">Location</h2>
              
              <div className="space-y-6">
                <div>
                  <label className="block text-sm font-medium text-medium-contrast mb-2">
                    Location
                  </label>
                  <input
                    type="text"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    className="input-standard w-full"
                    placeholder="e.g., San Francisco, CA, USA"
                  />
                  <p className="text-sm text-medium-contrast mt-1">
                    Used for local news, events, and connecting with nearby users.
                  </p>
                </div>

                <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                  <h3 className="text-sm font-medium text-yellow-800 dark:text-yellow-200 mb-2">
                    🔒 Privacy Note
                  </h3>
                  <p className="text-sm text-yellow-700 dark:text-yellow-300">
                    Your location is used only for personalization and discovery. 
                    You can control who sees your location in your privacy settings.
                  </p>
                </div>

                <div className="flex justify-end">
                  <button
                    onClick={saveProfile}
                    disabled={saving}
                    className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save Location'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}