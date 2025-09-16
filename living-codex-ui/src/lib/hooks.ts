import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AtomFetcher, APIAdapter, UIPage, UILens, UIAction, UIControls } from './atoms';

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
export function useConceptDiscovery(params: Record<string, any> = {}) {
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
export function useUserDiscovery(params: Record<string, any> = {}) {
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
      return apiAdapter.call(
        { method: 'POST', path: '/concept/user/link' },
        { userId, conceptId, relation: 'attuned' }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['concepts'] });
      queryClient.invalidateQueries({ queryKey: ['users'] });
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
      return apiAdapter.call(
        { method: 'POST', path: '/contributions/record' },
        { userId, entityId: conceptId, contribution, type: 'amplification' }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['concepts'] });
      queryClient.invalidateQueries({ queryKey: ['contributions'] });
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
