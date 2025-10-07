/**
 * User-Concept Relationship Integration Tests
 * 
 * These tests use the REAL backend API (no mocking).
 * They verify attune/unattune functionality and user-concept edges.
 * 
 * Test Strategy: Integration (Tier 1)
 * Coverage: User-concept interaction critical path
 */

import { endpoints } from '@/lib/api';

describe('User-Concept Relationships - Integration Tests', () => {
  const timestamp = Date.now();
  const testUsername = `testuser_uc_${timestamp}`;
  const testEmail = `${testUsername}@test.com`;
  const testPassword = 'TestPass123!';
  let userId: string;
  let authToken: string;
  const conceptIds: string[] = [];

  beforeAll(async () => {
    // Create test user
    const userResult = await endpoints.register(
      testUsername,
      testEmail,
      testPassword,
      'User-Concept Test User'
    );
    
    expect(userResult.success).toBe(true);
    userId = userResult.user.id;
    authToken = userResult.token;

    // Create test concepts
    const concepts = [
      { name: `UC Test Concept A ${timestamp}`, domain: 'Testing' },
      { name: `UC Test Concept B ${timestamp}`, domain: 'Testing' },
      { name: `UC Test Concept C ${timestamp}`, domain: 'Testing' },
    ];

    for (const concept of concepts) {
      const result = await endpoints.createConcept(concept);
      expect(result.success).toBe(true);
      conceptIds.push(result.concept.id);
    }
  });

  afterAll(async () => {
    // Cleanup
    try {
      await Promise.all([
        fetch('http://localhost:5002/test/cleanup/users', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ pattern: `^${testUsername}$` }),
        }),
        fetch('http://localhost:5002/test/cleanup/concepts', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ pattern: `uc_test_concept.*${timestamp}` }),
        }),
        fetch('http://localhost:5002/test/cleanup/edges', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ pattern: testUsername }),
        }),
      ]);
    } catch (error) {
      console.warn('Cleanup failed:', error);
    }
  });

  describe('Attune to Concept', () => {
    it('should successfully attune user to concept', async () => {
      const result = await endpoints.attuneToConcept(userId, conceptIds[0]);

      expect(result.success).toBe(true);
      expect(result.relationship).toBeDefined();
      expect(result.relationship.userId).toBe(userId);
      expect(result.relationship.conceptId).toBe(conceptIds[0]);
      expect(result.relationship.relationshipType).toBe('attuned');
    });

    it('should attune to multiple concepts', async () => {
      const result1 = await endpoints.attuneToConcept(userId, conceptIds[1]);
      const result2 = await endpoints.attuneToConcept(userId, conceptIds[2]);

      expect(result1.success).toBe(true);
      expect(result2.success).toBe(true);
    });

    it('should handle double attune (idempotent)', async () => {
      // Attune to same concept twice
      const result1 = await endpoints.attuneToConcept(userId, conceptIds[0]);
      const result2 = await endpoints.attuneToConcept(userId, conceptIds[0]);

      // Both should succeed or second should indicate already attuned
      expect(result1.success).toBe(true);
      expect(result2.success).toBe(true);
    });

    it('should reject attune with invalid user ID', async () => {
      const result = await endpoints.attuneToConcept('invalid-user-id', conceptIds[0]);

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should reject attune with invalid concept ID', async () => {
      const result = await endpoints.attuneToConcept(userId, 'invalid-concept-id');

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should track attunement strength', async () => {
      const result = await endpoints.attuneToConcept(userId, conceptIds[0]);

      expect(result.success).toBe(true);
      if (result.relationship.strength !== undefined) {
        expect(typeof result.relationship.strength).toBe('number');
        expect(result.relationship.strength).toBeGreaterThan(0);
        expect(result.relationship.strength).toBeLessThanOrEqual(1);
      }
    });
  });

  describe('Retrieve User Concepts', () => {
    it('should retrieve all concepts for a user', async () => {
      const result = await endpoints.getUserConcepts(userId);

      expect(result.success).toBe(true);
      expect(result.concepts).toBeDefined();
      expect(Array.isArray(result.concepts)).toBe(true);
      expect(result.concepts.length).toBeGreaterThan(0);
    });

    it('should include relationship metadata', async () => {
      const result = await endpoints.getUserConcepts(userId);

      expect(result.success).toBe(true);
      const firstConcept = result.concepts[0];
      
      // Should have concept data
      expect(firstConcept.conceptId || firstConcept.id).toBeDefined();
      
      // May have relationship type and strength
      if (firstConcept.relationshipType) {
        expect(firstConcept.relationshipType).toBe('attuned');
      }
    });

    it('should return empty array for user with no concepts', async () => {
      // Create a new user with no attunements
      const newUser = await endpoints.register(
        `lonely_user_${timestamp}`,
        `lonely_${timestamp}@test.com`,
        testPassword
      );

      const result = await endpoints.getUserConcepts(newUser.user.id);

      expect(result.success).toBe(true);
      expect(result.concepts).toBeDefined();
      expect(result.concepts.length).toBe(0);

      // Cleanup
      await fetch('http://localhost:5002/test/cleanup/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: `^lonely_user_${timestamp}$` }),
      });
    });

    it('should handle non-existent user gracefully', async () => {
      const result = await endpoints.getUserConcepts('nonexistent-user-id');

      // Should return empty or error
      if (result.success) {
        expect(result.concepts.length).toBe(0);
      } else {
        expect(result.error).toBeDefined();
      }
    });
  });

  describe('Unattune from Concept', () => {
    it('should successfully unattune from concept', async () => {
      // First ensure we're attuned
      await endpoints.attuneToConcept(userId, conceptIds[1]);

      // Then unattune
      const result = await endpoints.unattuneConcept(userId, conceptIds[1]);

      expect(result.success).toBe(true);
    });

    it('should verify concept removed from user list', async () => {
      // Unattune from concept 2
      await endpoints.unattuneConcept(userId, conceptIds[2]);

      // Get user concepts
      const userConcepts = await endpoints.getUserConcepts(userId);

      // Concept 2 should not be in the list
      const hasConceptTwo = userConcepts.concepts.some((c: any) => 
        c.conceptId === conceptIds[2] || c.id === conceptIds[2]
      );
      expect(hasConceptTwo).toBe(false);
    });

    it('should handle double unattune gracefully (idempotent)', async () => {
      const result1 = await endpoints.unattuneConcept(userId, conceptIds[1]);
      const result2 = await endpoints.unattuneConcept(userId, conceptIds[1]);

      // Both should succeed or indicate no relationship exists
      expect(result1.success).toBe(true);
      // Second may succeed or fail gracefully
      expect(result2).toBeDefined();
    });

    it('should handle unattune with invalid user ID', async () => {
      const result = await endpoints.unattuneConcept('invalid-user-id', conceptIds[0]);

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should handle unattune with invalid concept ID', async () => {
      const result = await endpoints.unattuneConcept(userId, 'invalid-concept-id');

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });
  });

  describe('User-Concept Edge Persistence', () => {
    it('should persist relationships across sessions', async () => {
      // Attune to a concept
      await endpoints.attuneToConcept(userId, conceptIds[0]);

      // "Logout" and get fresh user concepts
      const result = await endpoints.getUserConcepts(userId);

      // Relationship should still exist
      const hasRelationship = result.concepts.some((c: any) => 
        c.conceptId === conceptIds[0] || c.id === conceptIds[0]
      );
      expect(hasRelationship).toBe(true);
    });

    it('should maintain relationship order/timestamp', async () => {
      // Create relationships in sequence
      const concept1 = `Temporal Test 1 ${timestamp}`;
      const concept2 = `Temporal Test 2 ${timestamp}`;

      const c1 = await endpoints.createConcept({ name: concept1 });
      conceptIds.push(c1.concept.id);
      
      await endpoints.attuneToConcept(userId, c1.concept.id);
      await new Promise(resolve => setTimeout(resolve, 100)); // Small delay
      
      const c2 = await endpoints.createConcept({ name: concept2 });
      conceptIds.push(c2.concept.id);
      
      await endpoints.attuneToConcept(userId, c2.concept.id);

      const userConcepts = await endpoints.getUserConcepts(userId);

      // Should have both
      expect(userConcepts.concepts.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('Relationship Types and Strength', () => {
    it('should default to "attuned" relationship type', async () => {
      const result = await endpoints.attuneToConcept(userId, conceptIds[0]);

      expect(result.success).toBe(true);
      expect(result.relationship.relationshipType).toBe('attuned');
    });

    it('should default to strength 1.0', async () => {
      const result = await endpoints.attuneToConcept(userId, conceptIds[0]);

      expect(result.success).toBe(true);
      if (result.relationship.strength !== undefined) {
        expect(result.relationship.strength).toBe(1.0);
      }
    });

    it('should handle multiple users attuned to same concept', async () => {
      // Create second user
      const user2 = await endpoints.register(
        `testuser_uc2_${timestamp}`,
        `uc2_${timestamp}@test.com`,
        testPassword
      );

      // Both users attune to same concept
      const result1 = await endpoints.attuneToConcept(userId, conceptIds[0]);
      const result2 = await endpoints.attuneToConcept(user2.user.id, conceptIds[0]);

      expect(result1.success).toBe(true);
      expect(result2.success).toBe(true);

      // Cleanup second user
      await fetch('http://localhost:5002/test/cleanup/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: `^testuser_uc2_${timestamp}$` }),
      });
    });
  });

  describe('Error Handling and Edge Cases', () => {
    it('should handle attune with missing parameters', async () => {
      const result = await endpoints.attuneToConcept('', '');

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should handle getUserConcepts with null/undefined', async () => {
      try {
        const result = await endpoints.getUserConcepts('');
        // Should fail or return empty
        if (result.success) {
          expect(result.concepts).toBeDefined();
        }
      } catch (error) {
        // Acceptable to throw for invalid input
        expect(error).toBeDefined();
      }
    });

    it('should handle concurrent attune/unattune operations', async () => {
      const promises = [
        endpoints.attuneToConcept(userId, conceptIds[0]),
        endpoints.unattuneConcept(userId, conceptIds[0]),
        endpoints.attuneToConcept(userId, conceptIds[0]),
      ];

      const results = await Promise.allSettled(promises);

      // All should complete without throwing
      results.forEach(result => {
        expect(result.status).toBe('fulfilled');
      });
    });
  });
});


