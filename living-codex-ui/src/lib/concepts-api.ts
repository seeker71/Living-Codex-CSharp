/**
 * Concepts API Client
 * For creating and managing concepts in the Living Codex
 */

import config from './config';

export interface ConceptCreateRequest {
  name: string;
  description: string;
  domain: string;
  complexity: number;
  tags: string[];
}

export interface ConceptCreateResponse {
  success: boolean;
  conceptId?: string;
  message: string;
  timestamp: string;
}

/**
 * Create a new concept
 */
export async function createConcept(request: ConceptCreateRequest): Promise<ConceptCreateResponse> {
  const response = await fetch(
    `${config.NEXT_PUBLIC_BACKEND_URL}/concept/create`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || `Failed to create concept: ${response.statusText}`);
  }

  return response.json();
}

/**
 * Domains available for concepts
 */
export const CONCEPT_DOMAINS = [
  { value: 'consciousness', label: 'Consciousness & Awareness', frequency: 741 },
  { value: 'love', label: 'Love & Compassion', frequency: 528 },
  { value: 'healing', label: 'Healing & Harmony', frequency: 432 },
  { value: 'science', label: 'Science & Knowledge', frequency: 256 },
  { value: 'technology', label: 'Technology & Innovation', frequency: 320 },
  { value: 'nature', label: 'Nature & Earth', frequency: 174 },
  { value: 'society', label: 'Society & Community', frequency: 288 },
  { value: 'art', label: 'Art & Creativity', frequency: 396 },
  { value: 'spirituality', label: 'Spirituality & Wisdom', frequency: 639 },
  { value: 'other', label: 'Other', frequency: 256 },
];

/**
 * Complexity levels with descriptions
 */
export const COMPLEXITY_LEVELS = [
  { value: 1, label: 'Fundamental', description: 'Basic building block concept' },
  { value: 2, label: 'Simple', description: 'Easy to grasp, single idea' },
  { value: 3, label: 'Clear', description: 'Well-defined, few dependencies' },
  { value: 4, label: 'Moderate', description: 'Some complexity, multiple aspects' },
  { value: 5, label: 'Intermediate', description: 'Requires background knowledge' },
  { value: 6, label: 'Advanced', description: 'Deep understanding needed' },
  { value: 7, label: 'Complex', description: 'Many interconnections' },
  { value: 8, label: 'Profound', description: 'Deeply interconnected system' },
  { value: 9, label: 'Transcendent', description: 'Beyond simple explanation' },
  { value: 10, label: 'Universal', description: 'Fundamental to existence' },
];

