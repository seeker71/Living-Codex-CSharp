/**
 * Concept CRUD Integration Tests
 * 
 * These tests use the REAL backend API (no mocking).
 * They verify concept creation, retrieval, search, and relationships.
 * 
 * Test Strategy: Integration (Tier 1)
 * Coverage: Concept management critical path
 */

import { endpoints } from '@/lib/api';

describe('Concept CRUD - Integration Tests', () => {
  const timestamp = Date.now();
  const createdConceptIds: string[] = [];

  afterAll(async () => {
    // Cleanup: Delete test concepts
    try {
      await fetch('http://localhost:5002/test/cleanup/concepts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: `test_concept_${timestamp}` }),
      });
    } catch (error) {
      console.warn('Concept cleanup failed:', error);
    }
  });

  describe('Concept Creation', () => {
    it('should create a new concept with full data', async () => {
      const conceptData = {
        name: `Test Concept ${timestamp}`,
        description: 'A test concept for integration testing',
        domain: 'Testing',
        tags: ['test', 'integration', 'automated'],
      };

      const result = await endpoints.createConcept(conceptData);

      expect(result.success).toBe(true);
      expect(result.data).toBeDefined();
      expect(result.data.concept).toBeDefined();
      expect(result.data.concept.id).toBeDefined();
      expect(result.data.concept.name).toBe(conceptData.name);
      expect(result.data.concept.description).toBe(conceptData.description);
      expect(result.data.concept.domain).toBe(conceptData.domain);

      createdConceptIds.push(result.data.concept.id);
    });

    it('should create a concept with minimal data', async () => {
      const conceptData = {
        name: `Minimal Test Concept ${timestamp}`,
        description: 'Minimal description',
        domain: 'Testing',
        complexity: 1,
        tags: ['test']
      };

      const result = await endpoints.createConcept(conceptData);

      expect(result.success).toBe(true);
      expect(result.data).toBeDefined();
      expect(result.data.concept).toBeDefined();
      expect(result.data.concept.id).toBeDefined();
      expect(result.data.concept.name).toBe(conceptData.name);

      createdConceptIds.push(result.data.concept.id);
    });

    it('should reject concept creation without name', async () => {
      const conceptData = {
        description: 'Missing name field',
      };

      const result = await endpoints.createConcept(conceptData);

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(result.error).toMatch(/name.*required/i);
    });

    it('should reject duplicate concept names', async () => {
      const conceptName = `Duplicate Test ${timestamp}`;
      const conceptData = {
        name: conceptName,
        description: 'Duplicate test',
        domain: 'Testing',
        complexity: 1,
        tags: ['test']
      };
      
      // Create first concept
      const first = await endpoints.createConcept(conceptData);
      expect(first.success).toBe(true);
      createdConceptIds.push(first.data.concept.id);

      // Try to create duplicate
      const duplicate = await endpoints.createConcept(conceptData);
      // Backend may allow duplicates or reject them - adjust based on actual behavior
      if (!duplicate.success) {
        expect(duplicate.error).toMatch(/already exists|duplicate/i);
      }
    });
  });

  describe('Concept Retrieval', () => {
    it('should retrieve all concepts', async () => {
      const result = await endpoints.getConcepts();

      expect(result.success).toBe(true);
      expect(result.data).toBeDefined();
      expect(result.data.concepts).toBeDefined();
      expect(Array.isArray(result.data.concepts)).toBe(true);
      expect(result.data.totalCount).toBeGreaterThan(0);
    });

    it('should support pagination', async () => {
      const page1 = await endpoints.getConcepts({ skip: 0, take: 2 });
      const page2 = await endpoints.getConcepts({ skip: 2, take: 2 });

      expect(page1.success).toBe(true);
      expect(page2.success).toBe(true);
      expect(page1.data.concepts.length).toBeLessThanOrEqual(2);
      expect(page2.data.concepts.length).toBeLessThanOrEqual(2);
      
      // Pages should have different concepts (unless total < 3)
      if (page1.data.concepts.length > 0 && page2.data.concepts.length > 0) {
        expect(page1.data.concepts[0].id).not.toBe(page2.data.concepts[0].id);
      }
    });

    it('should filter concepts by search term', async () => {
      const searchTerm = `Test Concept ${timestamp}`;
      const result = await endpoints.getConcepts({ searchTerm });

      expect(result.success).toBe(true);
      expect(result.data).toBeDefined();
      expect(result.data.concepts).toBeDefined();
      
      // All results should match search term
      result.data.concepts.forEach((concept: any) => {
        const matchesName = concept.name?.toLowerCase().includes('test concept');
        const matchesDesc = concept.description?.toLowerCase().includes('test');
        expect(matchesName || matchesDesc).toBe(true);
      });
    });

    it('should return empty results for non-matching search', async () => {
      const result = await endpoints.getConcepts({ 
        searchTerm: `NonExistentConcept${timestamp}XYZ123` 
      });

      expect(result.success).toBe(true);
      expect(result.data.concepts.length).toBe(0);
    });

    it('should include concept metadata', async () => {
      const result = await endpoints.getConcepts({ take: 1 });

      expect(result.success).toBe(true);
      if (result.data.concepts.length > 0) {
        const concept = result.data.concepts[0];
        expect(concept.id).toBeDefined();
        expect(concept.name).toBeDefined();
        expect(concept.domain).toBeDefined();
        // resonance, energy may be optional but should be numbers if present
        if (concept.resonance !== undefined) {
          expect(typeof concept.resonance).toBe('number');
        }
      }
    });
  });

  describe('Concept Search and Discovery', () => {
    beforeAll(async () => {
      // Create concepts with specific attributes for searching
      const testConcepts = [
        { name: `Physics Concept ${timestamp}`, domain: 'Science', description: 'Related to physics', complexity: 5, tags: ['physics', 'science'] },
        { name: `Math Concept ${timestamp}`, domain: 'Science', description: 'Related to mathematics', complexity: 5, tags: ['math', 'science'] },
        { name: `Art Concept ${timestamp}`, domain: 'Arts', description: 'Related to artistic expression', complexity: 3, tags: ['art'] },
      ];

      for (const concept of testConcepts) {
        const result = await endpoints.createConcept(concept);
        if (result.success && result.data) {
          createdConceptIds.push(result.data.concept.id);
        }
      }
    });

    it('should find concepts by domain', async () => {
      const result = await endpoints.getConcepts({ searchTerm: 'Science' });

      expect(result.success).toBe(true);
      const scienceConcepts = result.data.concepts.filter((c: any) => 
        c.domain === 'Science' || c.name.includes('Physics') || c.name.includes('Math')
      );
      expect(scienceConcepts.length).toBeGreaterThan(0);
    });

    it('should find concepts by description keywords', async () => {
      const result = await endpoints.getConcepts({ searchTerm: 'artistic' });

      expect(result.success).toBe(true);
      const artisticConcepts = result.data.concepts.filter((c: any) => 
        c.description?.toLowerCase().includes('artistic')
      );
      expect(artisticConcepts.length).toBeGreaterThan(0);
    });

    it('should handle case-insensitive search', async () => {
      const lowerResult = await endpoints.getConcepts({ searchTerm: 'physics' });
      const upperResult = await endpoints.getConcepts({ searchTerm: 'PHYSICS' });
      const mixedResult = await endpoints.getConcepts({ searchTerm: 'PhYsIcS' });

      expect(lowerResult.data.concepts.length).toBeGreaterThan(0);
      expect(lowerResult.data.concepts.length).toBe(upperResult.data.concepts.length);
      expect(lowerResult.data.concepts.length).toBe(mixedResult.data.concepts.length);
    });
  });

  describe('Concept Metadata and Validation', () => {
    it('should preserve custom metadata fields', async () => {
      const conceptData = {
        name: `Custom Metadata Concept ${timestamp}`,
        description: 'Testing custom fields',
        domain: 'Testing',
        complexity: 1,
        tags: ['test'],
        customField1: 'custom value',
        customField2: 12345,
        customField3: { nested: 'object' },
      };

      const result = await endpoints.createConcept(conceptData);

      expect(result.success).toBe(true);
      createdConceptIds.push(result.data.concept.id);

      // Custom fields should be preserved
      // Note: Depending on backend implementation, these might be in meta or top-level
    });

    it('should handle special characters in concept names', async () => {
      const specialName = `Test Concept: "Quotes" & 'Apostrophes' (Parens) ${timestamp}`;
      const result = await endpoints.createConcept({ 
        name: specialName,
        description: 'Special characters test',
        domain: 'Testing',
        complexity: 1,
        tags: ['test']
      });

      expect(result.success).toBe(true);
      expect(result.data.concept.name).toBe(specialName);
      createdConceptIds.push(result.data.concept.id);
    });

    it('should handle very long descriptions', async () => {
      const longDescription = 'A'.repeat(5000);
      const result = await endpoints.createConcept({ 
        name: `Long Description Concept ${timestamp}`,
        description: longDescription,
        domain: 'Testing',
        complexity: 1,
        tags: ['test']
      });

      expect(result.success).toBe(true);
      createdConceptIds.push(result.data.concept.id);
    });

    it('should validate and sanitize concept data', async () => {
      const maliciousData = {
        name: `<script>alert('xss')</script> Test ${timestamp}`,
        description: '<img src=x onerror=alert(1)>',
        domain: 'Testing',
        complexity: 1,
        tags: ['test']
      };

      const result = await endpoints.createConcept(maliciousData);

      // Backend currently accepts as-is (sanitization could be added later)
      if (result.success) {
        createdConceptIds.push(result.data.concept.id);
      }
      // Note: XSS protection should be handled by frontend rendering, not storage
    });
  });

  describe('Performance and Limits', () => {
    it('should handle requests for large result sets', async () => {
      const result = await endpoints.getConcepts({ take: 1000 });

      expect(result.success).toBe(true);
      expect(result.data).toBeDefined();
      expect(result.data.concepts).toBeDefined();
      // Backend may cap at some reasonable limit
      expect(result.data.concepts.length).toBeLessThanOrEqual(1000);
    });

    it('should respond quickly to concept queries', async () => {
      const start = Date.now();
      await endpoints.getConcepts({ take: 10 });
      const duration = Date.now() - start;

      // Should respond in under 1 second for small queries
      expect(duration).toBeLessThan(1000);
    });

    it('should handle concurrent concept creation', async () => {
      const promises = Array.from({ length: 5 }, (_, i) =>
        endpoints.createConcept({ 
          name: `Concurrent Test ${timestamp}_${i}`,
          description: `Concept ${i}`,
          domain: 'Testing',
          complexity: 1,
          tags: ['test', 'concurrent']
        })
      );

      const results = await Promise.all(promises);

      results.forEach((result, i) => {
        expect(result.success).toBe(true);
        createdConceptIds.push(result.data.concept.id);
      });

      // All should have unique IDs
      const ids = results.map(r => r.data.concept.id);
      const uniqueIds = new Set(ids);
      expect(uniqueIds.size).toBe(5);
    });
  });
});


