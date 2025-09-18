import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AtomFetcher, APIAdapter, UIPage, UILens, UIAction, UIControls } from './atoms';
import { endpoints } from './api';
import { useCallback } from 'react';
import { useAuth } from '../contexts/AuthContext';

const atomFetcher = new AtomFetcher();
const apiAdapter = new APIAdapter();

// Hook for fetching pages
export function usePages() {
  return useQuery({
    queryKey: ['pages'],
    queryFn: () => atomFetcher.fetchAtoms<UIPage>('codex.ui.page'),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// Hook for fetching lenses
export function useLenses() {
  return useQuery({
    queryKey: ['lenses'],
    queryFn: () => atomFetcher.fetchAtoms<UILens>('codex.ui.lens'),
    staleTime: 5 * 60 * 1000,
  });
}

// Hook for fetching actions
export function useActions() {
  return useQuery({
    queryKey: ['actions'],
    queryFn: () => atomFetcher.fetchAtoms<UIAction>('codex.ui.action'),
    staleTime: 5 * 60 * 1000,
  });
}

// Hook for fetching controls
export function useControls() {
  return useQuery({
    queryKey: ['controls'],
    queryFn: () => atomFetcher.fetchAtoms<UIControls>('codex.ui.controls'),
    staleTime: 5 * 60 * 1000,
  });
}

// Hook for concept discovery
export function useConceptDiscovery(params: Record<string, unknown> = {}) {
  return useQuery({
    queryKey: ['concepts', 'discover', params],
    queryFn: () => apiAdapter.call(
      { method: 'POST', path: '/concept/discover' },
      params
    ),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

// Hook for user discovery
export function useUserDiscovery(params: Record<string, unknown> = {}) {
  return useQuery({
    queryKey: ['users', 'discover', params],
    queryFn: () => apiAdapter.call(
      { method: 'POST', path: '/users/discover' },
      params
    ),
    staleTime: 2 * 60 * 1000,
  });
}

// Hook for resonance comparison
export function useResonanceCompare(concept1: string, concept2: string) {
  return useQuery({
    queryKey: ['resonance', 'compare', concept1, concept2],
    queryFn: () => apiAdapter.call(
      { method: 'POST', path: '/concepts/resonance/compare' },
      { concept1, concept2 }
    ),
    enabled: !!concept1 && !!concept2,
    staleTime: 5 * 60 * 1000,
  });
}

// Hook for attuning to a concept
export function useAttune() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ userId, conceptId }: { userId: string; conceptId: string }) => {
      const response = await endpoints.attuneToConcept(userId, conceptId);
      if (!response.success) {
        throw new Error(response.error || 'Failed to attune to concept');
      }
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['concepts'] });
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['energy'] });
    },
  });
}

// Hook for amplifying a concept
export function useAmplify() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ userId, conceptId, contribution }: { 
      userId: string; 
      conceptId: string; 
      contribution: string;
    }) => {
      const response = await endpoints.recordContribution({
        userId,
        entityId: conceptId,
        entityType: 'concept',
        contributionType: 'Rating', // Use valid enum value
        description: contribution,
        value: 1,
        metadata: { conceptId, action: 'amplify' }
      });
      if (!response.success) {
        throw new Error(response.error || 'Failed to amplify concept');
      }
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['concepts'] });
      queryClient.invalidateQueries({ queryKey: ['contributions'] });
      queryClient.invalidateQueries({ queryKey: ['energy'] });
      queryClient.invalidateQueries({ queryKey: ['news', 'feed'] });
    },
  });
}

// Hook for resonance controls state
export function useResonanceControls() {
  return useQuery({
    queryKey: ['resonance', 'controls'],
    queryFn: () => atomFetcher.fetchAtoms<UIControls>('codex.ui.controls'),
    select: (data) => data.find(control => control.id === 'controls.resonance'),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

// New hooks using improved API service with timeout handling

export function useHealthStatus() {
  return useQuery({
    queryKey: ['health'],
    queryFn: () => endpoints.health(),
    refetchInterval: 30000, // Check health every 30 seconds
    retry: 1, // Don't retry health checks aggressively
  });
}

export function useStorageStats() {
  return useQuery({
    queryKey: ['storage', 'stats'],
    queryFn: () => endpoints.storageStats(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useConcepts() {
  return useQuery({
    queryKey: ['concepts'],
    queryFn: () => endpoints.getConcepts(),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

export function useCollectiveEnergy() {
  return useQuery({
    queryKey: ['energy', 'collective'],
    queryFn: () => endpoints.getCollectiveEnergy(),
    refetchInterval: 60000, // Update every minute
  });
}

export function useContributorEnergy(userId: string) {
  return useQuery({
    queryKey: ['energy', 'contributor', userId],
    queryFn: () => endpoints.getContributorEnergy(userId),
    enabled: !!userId,
    staleTime: 2 * 60 * 1000,
  });
}

export function useTrendingTopics(limit = 10, hoursBack = 24) {
  return useQuery({
    queryKey: ['news', 'trending', limit, hoursBack],
    queryFn: () => endpoints.getTrendingTopics(limit, hoursBack),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useNewsFeed(userId: string, limit = 20, hoursBack = 24) {
  return useQuery({
    queryKey: ['news', 'feed', userId, limit, hoursBack],
    queryFn: () => endpoints.getNewsFeed(userId, limit, hoursBack),
    enabled: !!userId,
    staleTime: 2 * 60 * 1000,
  });
}

export function useNodes(typeId?: string, limit?: number) {
  return useQuery({
    queryKey: ['nodes', typeId, limit],
    queryFn: () => endpoints.getNodes(typeId, limit),
    staleTime: 5 * 60 * 1000,
  });
}

export function useAdvancedNodeSearch(searchParams: {
  typeIds?: string[];
  searchTerm?: string;
  states?: string[];
  take?: number;
  skip?: number;
  sortBy?: string;
  sortDescending?: boolean;
}) {
  return useQuery({
    queryKey: ['nodes', 'search', searchParams],
    queryFn: () => endpoints.searchNodesAdvanced(searchParams),
    staleTime: 30 * 1000, // 30 seconds for search results
    enabled: !!(searchParams.typeIds?.length || searchParams.searchTerm || searchParams.states?.length),
  });
}

export function useNodeTypes() {
  return useQuery({
    queryKey: ['storage', 'types'],
    queryFn: () => endpoints.getNodeTypes(),
    staleTime: 10 * 60 * 1000, // 10 minutes - node types don't change often
  });
}

export function useUserConcepts(userId: string) {
  return useQuery({
    queryKey: ['user', 'concepts', userId],
    queryFn: () => endpoints.getUserConcepts(userId),
    enabled: !!userId,
    staleTime: 2 * 60 * 1000,
  });
}

export function usePersonalNewsStream(userId: string, limit = 20) {
  return useQuery({
    queryKey: ['news', 'stream', userId, limit],
    queryFn: () => endpoints.getPersonalNewsStream(userId, limit),
    enabled: !!userId,
    refetchInterval: 30000, // Refresh every 30 seconds for real-time updates
    staleTime: 1 * 60 * 1000, // 1 minute
  });
}

export function usePersonalContributionsFeed(userId: string, limit = 20) {
  return useQuery({
    queryKey: ['contributions', 'feed', userId, limit],
    queryFn: () => endpoints.getPersonalContributionsFeed(userId, limit),
    enabled: !!userId,
    refetchInterval: 15000, // Refresh every 15 seconds for contribution updates
    staleTime: 30 * 1000, // 30 seconds
  });
}

// Contribution tracking hooks
export function useRecordContribution() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: endpoints.recordContribution,
    onSuccess: () => {
      // Invalidate related queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['contributions'] });
      queryClient.invalidateQueries({ queryKey: ['news'] });
      queryClient.invalidateQueries({ queryKey: ['energy'] });
    },
  });
}

export function useUserContributions(userId: string, query?: Record<string, any>) {
  return useQuery({
    queryKey: ['contributions', 'user', userId, query],
    queryFn: () => endpoints.getUserContributions(userId, query),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
  });
}

// Convenience hooks for common interactions
export function useTrackInteraction() {
  const recordContribution = useRecordContribution();
  const { user } = useAuth();
  
  return useCallback((entityId: string, interactionType: string, metadata?: Record<string, any>) => {
    if (!user?.id) {
      console.warn('Cannot track interaction: user not authenticated');
      return;
    }
    
    // Map interaction types to valid ContributionType enum values
    const contributionTypeMap: Record<string, string> = {
      'page-visit': 'View',
      'button-click': 'Share',
      'form-submit': 'Create',
      'content-edit': 'Update',
      'item-delete': 'Delete',
      'add-comment': 'Comment',
      'rate-item': 'Rating',
      'amplify': 'Rating',
      'attune': 'Share'
    };
    
    const contributionType = contributionTypeMap[interactionType] || 'View';
    
    recordContribution.mutate({
      userId: user.id,
      entityId,
      entityType: 'ui-interaction',
      contributionType,
      description: `User ${interactionType} interaction with ${entityId}`,
      value: 1,
      metadata: {
        ...metadata,
        originalInteractionType: interactionType,
        timestamp: new Date().toISOString(),
        page: window.location.pathname,
      }
    });
  }, [recordContribution, user?.id]);
}
