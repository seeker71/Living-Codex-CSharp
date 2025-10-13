'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Share2, Bookmark, ThumbsUp, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { useAuth } from '@/contexts/AuthContext';
import { useAttune, useAmplify } from '@/lib/hooks';
import { ConceptActivityFeed } from '@/components/collaboration/ConceptActivityFeed';
import { ConceptDiscussion } from '@/components/collaboration/ConceptDiscussion';
import { ConceptCollaborators } from '@/components/collaboration/ConceptCollaborators';
import { endpoints } from '@/lib/api';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

interface PageProps {
  params: Promise<{ conceptId: string }>;
}

export default function ReflectPage({ params }: PageProps) {
  const { conceptId } = use(params);
  const router = useRouter();
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'activity' | 'discussions' | 'collaborators'>('activity');

  const attuneMutation = useAttune();
  const amplifyMutation = useAmplify();

  // Fetch concept details
  const { data: conceptData, isLoading: conceptLoading } = useQuery({
    queryKey: ['concept', conceptId],
    queryFn: async () => {
      const response = await endpoints.getNode(conceptId);
      return response;
    },
  });

  const concept = conceptData?.data;

  const handleAttune = async () => {
    if (!user?.id) return;
    try {
      await attuneMutation.mutateAsync({ userId: user.id, conceptId });
    } catch (error) {
      console.error('Failed to attune:', error);
    }
  };

  const handleAmplify = async () => {
    if (!user?.id) return;
    const contribution = prompt('How are you amplifying this concept?');
    if (!contribution) return;

    try {
      await amplifyMutation.mutateAsync({
        userId: user.id,
        conceptId,
        contribution,
      });
    } catch (error) {
      console.error('Failed to amplify:', error);
    }
  };

  if (conceptLoading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-7xl">
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-1/3"></div>
          <div className="h-64 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 h-96 bg-gray-200 dark:bg-gray-700 rounded"></div>
            <div className="h-96 bg-gray-200 dark:bg-gray-700 rounded"></div>
          </div>
        </div>
      </div>
    );
  }

  if (!concept) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-7xl">
        <Card>
          <CardContent className="py-12 text-center">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              Concept Not Found
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-6">
              The concept you're looking for doesn't exist or has been removed.
            </p>
            <Button onClick={() => router.push('/discover')}>
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Discover
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-7xl">
      {/* Header */}
      <div className="mb-6">
        <button
          onClick={() => router.back()}
          className="flex items-center gap-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 mb-4"
        >
          <ArrowLeft className="w-4 h-4" />
          Back
        </button>
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <h1 className="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              {concept.title}
            </h1>
            <p className="text-lg text-gray-600 dark:text-gray-400">
              {concept.description}
            </p>
          </div>
          <div className="flex items-center gap-2 ml-4">
            <Button variant="outline" size="sm">
              <Share2 className="w-4 h-4" />
            </Button>
            <Button variant="outline" size="sm">
              <Bookmark className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Concept Overview Card */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-purple-500" />
            Concept Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Type</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">
                {concept.typeId}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">State</p>
              <p className="font-medium text-gray-900 dark:text-gray-100 capitalize">
                {concept.state}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Locale</p>
              <p className="font-medium text-gray-900 dark:text-gray-100">
                {concept.locale}
              </p>
            </div>
          </div>

          {user && (
            <div className="flex gap-3 mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
              <Button
                onClick={handleAttune}
                disabled={attuneMutation.isPending}
                className="flex-1"
              >
                <Sparkles className="w-4 h-4 mr-2" />
                {attuneMutation.isPending ? 'Attuning...' : 'Attune'}
              </Button>
              <Button
                onClick={handleAmplify}
                disabled={amplifyMutation.isPending}
                variant="outline"
                className="flex-1"
              >
                <ThumbsUp className="w-4 h-4 mr-2" />
                {amplifyMutation.isPending ? 'Amplifying...' : 'Amplify'}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Tabs */}
      <div className="flex gap-2 mb-6 border-b border-gray-200 dark:border-gray-700">
        <button
          onClick={() => setActiveTab('activity')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 ${
            activeTab === 'activity'
              ? 'border-purple-500 text-purple-600 dark:text-purple-400'
              : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
          }`}
        >
          Activity
        </button>
        <button
          onClick={() => setActiveTab('discussions')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 ${
            activeTab === 'discussions'
              ? 'border-purple-500 text-purple-600 dark:text-purple-400'
              : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
          }`}
        >
          Discussions
        </button>
        <button
          onClick={() => setActiveTab('collaborators')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 ${
            activeTab === 'collaborators'
              ? 'border-purple-500 text-purple-600 dark:text-purple-400'
              : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
          }`}
        >
          Collaborators
        </button>
      </div>

      {/* Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2">
          {activeTab === 'activity' && <ConceptActivityFeed conceptId={conceptId} />}
          {activeTab === 'discussions' && <ConceptDiscussion conceptId={conceptId} />}
          {activeTab === 'collaborators' && (
            <ConceptCollaborators conceptId={conceptId} className="lg:hidden" />
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Always show collaborators in sidebar on desktop */}
          <div className="hidden lg:block">
            <ConceptCollaborators conceptId={conceptId} />
          </div>

          {/* Quick Stats */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Quick Stats</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">ID</span>
                  <span className="font-mono text-xs text-gray-900 dark:text-gray-100">
                    {concept.id}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">Created</span>
                  <span className="text-gray-900 dark:text-gray-100">
                    {new Date(concept.meta?.createdAt || Date.now()).toLocaleDateString()}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

