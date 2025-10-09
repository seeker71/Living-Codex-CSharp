/**
 * Concepts API Client Tests
 */

import { createConcept, CONCEPT_DOMAINS, COMPLEXITY_LEVELS } from '../concepts-api';

// Mock config
jest.mock('../config', () => ({
  config: {
    backend: {
      baseUrl: 'http://localhost:5002'
    }
  }
}));

// Mock fetch
global.fetch = jest.fn();

describe('Concepts API Client', () => {
  beforeEach(() => {
    (fetch as jest.Mock).mockClear();
  });

  describe('createConcept', () => {
    it('should create a concept successfully', async () => {
      const mockResponse = {
        success: true,
        conceptId: 'codex.concept.Test.123',
        message: 'Concept created successfully',
        timestamp: '2025-10-07T20:00:00Z'
      };
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse
      });

      const request = {
        name: 'Test Concept',
        description: 'A test concept',
        domain: 'consciousness',
        complexity: 5,
        tags: ['test', 'example']
      };

      const result = await createConcept(request);
      
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:5002/concept/create',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(request)
        }
      );
      expect(result).toEqual(mockResponse);
    });

    it('should throw error on failed request', async () => {
      const mockError = {
        message: 'Name is required',
        code: 'VALIDATION_ERROR'
      };
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        json: async () => mockError
      });

      const request = {
        name: '',
        description: 'Test',
        domain: 'consciousness',
        complexity: 5,
        tags: []
      };

      await expect(createConcept(request)).rejects.toThrow('Name is required');
    });

    it('should handle network errors', async () => {
      (fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const request = {
        name: 'Test',
        description: 'Test',
        domain: 'consciousness',
        complexity: 5,
        tags: []
      };

      await expect(createConcept(request)).rejects.toThrow('Network error');
    });
  });

  describe('CONCEPT_DOMAINS', () => {
    it('should have all required domains', () => {
      const expectedDomains = [
        'consciousness', 'love', 'healing', 'science', 'technology',
        'nature', 'society', 'art', 'spirituality', 'other'
      ];
      
      const domainValues = CONCEPT_DOMAINS.map(d => d.value);
      expectedDomains.forEach(domain => {
        expect(domainValues).toContain(domain);
      });
    });

    it('should have frequency values for each domain', () => {
      CONCEPT_DOMAINS.forEach(domain => {
        expect(domain.frequency).toBeGreaterThan(0);
        expect(typeof domain.frequency).toBe('number');
      });
    });

    it('should have proper labels', () => {
      CONCEPT_DOMAINS.forEach(domain => {
        expect(domain.label).toBeTruthy();
        expect(typeof domain.label).toBe('string');
      });
    });
  });

  describe('COMPLEXITY_LEVELS', () => {
    it('should have levels 1-10', () => {
      const values = COMPLEXITY_LEVELS.map(l => l.value);
      for (let i = 1; i <= 10; i++) {
        expect(values).toContain(i);
      }
    });

    it('should have descriptions for each level', () => {
      COMPLEXITY_LEVELS.forEach(level => {
        expect(level.description).toBeTruthy();
        expect(typeof level.description).toBe('string');
      });
    });

    it('should have proper labels', () => {
      COMPLEXITY_LEVELS.forEach(level => {
        expect(level.label).toBeTruthy();
        expect(typeof level.label).toBe('string');
      });
    });

    it('should be ordered by complexity', () => {
      const values = COMPLEXITY_LEVELS.map(l => l.value);
      const sortedValues = [...values].sort((a, b) => a - b);
      expect(values).toEqual(sortedValues);
    });
  });
});
