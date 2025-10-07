/**
 * Authentication Flow Integration Tests
 * 
 * These tests use the REAL backend API (no mocking).
 * They verify complete auth flows end-to-end.
 * 
 * Test Strategy: Integration (Tier 1)
 * Coverage: Authentication critical path
 */

import { endpoints } from '@/lib/api';

// Test harness will be set up globally
// For now, we'll rely on server being available at localhost:5002

describe('Authentication Flow - Integration Tests', () => {
  // Unique test user for this test run
  const timestamp = Date.now();
  const testUsername = `testuser_${timestamp}`;
  const testEmail = `${testUsername}@test.com`;
  const testPassword = 'TestPass123!';
  let userId: string;
  let authToken: string;

  afterAll(async () => {
    // Cleanup: Delete test user after all tests complete
    try {
      await fetch('http://localhost:5002/test/cleanup/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: `^${testUsername}$` }),
      });
    } catch (error) {
      console.warn('Cleanup failed:', error);
    }
  });

  describe('User Registration', () => {
    it('should successfully register a new user', async () => {
      const result = await endpoints.register(
        testUsername,
        testEmail,
        testPassword,
        'Test User'
      );

      expect(result.success).toBe(true);
      expect(result.user).toBeDefined();
      expect(result.user.username).toBe(testUsername);
      expect(result.user.email).toBe(testEmail);
      expect(result.user.id).toBeDefined();
      expect(result.token).toBeDefined();

      // Save for subsequent tests
      userId = result.user.id;
      authToken = result.token;
    });

    it('should reject duplicate username registration', async () => {
      const result = await endpoints.register(
        testUsername,
        'different@email.com',
        testPassword
      );

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(result.error).toMatch(/already exists|duplicate/i);
    });

    it('should reject duplicate email registration', async () => {
      const result = await endpoints.register(
        'differentuser',
        testEmail,
        testPassword
      );

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should reject weak passwords', async () => {
      const result = await endpoints.register(
        `testuser_${timestamp + 1}`,
        `test_${timestamp + 1}@test.com`,
        'weak'
      );

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });
  });

  describe('User Login', () => {
    it('should successfully login with username', async () => {
      const result = await endpoints.login(testUsername, testPassword, false);

      expect(result.success).toBe(true);
      expect(result.user).toBeDefined();
      expect(result.user.username).toBe(testUsername);
      expect(result.token).toBeDefined();
    });

    it('should successfully login with email', async () => {
      const result = await endpoints.login(testEmail, testPassword, false);

      expect(result.success).toBe(true);
      expect(result.user).toBeDefined();
      expect(result.user.username).toBe(testUsername);
      expect(result.token).toBeDefined();
    });

    it('should reject invalid password', async () => {
      const result = await endpoints.login(testUsername, 'WrongPassword123!', false);

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(result.error).toMatch(/invalid|incorrect|password/i);
    });

    it('should reject non-existent user', async () => {
      const result = await endpoints.login('nonexistentuser', testPassword, false);

      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should support remember me flag', async () => {
      const result = await endpoints.login(testUsername, testPassword, true);

      expect(result.success).toBe(true);
      expect(result.token).toBeDefined();
      // Remember me tokens should have longer expiry (not directly testable here)
    });
  });

  describe('Token Validation', () => {
    it('should validate a valid token', async () => {
      const result = await endpoints.validateToken(authToken);

      expect(result.success).toBe(true);
      expect(result.valid).toBe(true);
      expect(result.user).toBeDefined();
      expect(result.user.id).toBe(userId);
    });

    it('should reject an invalid token', async () => {
      const result = await endpoints.validateToken('invalid-token-12345');

      expect(result.success).toBe(false);
      expect(result.valid).toBe(false);
    });

    it('should reject an expired token', async () => {
      // Note: This test would require a token that's actually expired
      // For now, we'll skip or use a malformed token
      const expiredToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjB9.invalid';
      const result = await endpoints.validateToken(expiredToken);

      expect(result.valid).toBe(false);
    });
  });

  describe('User Profile', () => {
    it('should retrieve user profile', async () => {
      const result = await endpoints.getUserProfile(userId);

      expect(result.success).toBe(true);
      expect(result.profile).toBeDefined();
      expect(result.profile.id).toBe(userId);
      expect(result.profile.username).toBe(testUsername);
      expect(result.profile.email).toBe(testEmail);
      expect(result.profile.displayName).toBeDefined();
    });

    it('should update user profile', async () => {
      const updates = {
        displayName: 'Updated Test User',
        bio: 'This is my test bio',
        location: 'Test City, TC',
      };

      const result = await endpoints.updateUserProfile(userId, updates);

      expect(result.success).toBe(true);

      // Verify updates persisted
      const profile = await endpoints.getUserProfile(userId);
      expect(profile.profile.displayName).toBe(updates.displayName);
      expect(profile.profile.bio).toBe(updates.bio);
      expect(profile.profile.location).toBe(updates.location);
    });

    it('should handle profile updates with partial data', async () => {
      const updates = {
        bio: 'Updated bio only',
      };

      const result = await endpoints.updateUserProfile(userId, updates);

      expect(result.success).toBe(true);

      // Verify only bio changed, displayName unchanged
      const profile = await endpoints.getUserProfile(userId);
      expect(profile.profile.bio).toBe(updates.bio);
      expect(profile.profile.displayName).toBe('Updated Test User'); // From previous test
    });

    it('should reject profile update for non-existent user', async () => {
      const result = await endpoints.updateUserProfile('nonexistent-id', {
        displayName: 'Should Fail',
      });

      expect(result.success).toBe(false);
    });
  });

  describe('Logout', () => {
    it('should successfully logout', async () => {
      const result = await endpoints.logout(authToken);

      expect(result.success).toBe(true);
    });

    it('should invalidate token after logout', async () => {
      // Token should now be invalid
      const validation = await endpoints.validateToken(authToken);

      // Depending on implementation, this might return false or throw
      expect(validation.valid).toBe(false);
    });

    it('should handle logout with invalid token gracefully', async () => {
      const result = await endpoints.logout('invalid-token');

      // Should succeed or fail gracefully, not throw
      expect(result).toBeDefined();
    });
  });

  describe('Password Change', () => {
    const newPassword = 'NewTestPass456!';

    // Re-login to get a fresh token
    beforeAll(async () => {
      const loginResult = await endpoints.login(testUsername, testPassword, false);
      authToken = loginResult.token;
    });

    it('should successfully change password', async () => {
      const result = await endpoints.changePassword(userId, testPassword, newPassword);

      expect(result.success).toBe(true);
    });

    it('should allow login with new password', async () => {
      const result = await endpoints.login(testUsername, newPassword, false);

      expect(result.success).toBe(true);
      expect(result.token).toBeDefined();
    });

    it('should reject login with old password', async () => {
      const result = await endpoints.login(testUsername, testPassword, false);

      expect(result.success).toBe(false);
    });

    it('should reject password change with wrong current password', async () => {
      const result = await endpoints.changePassword(userId, 'WrongPassword!', 'AnotherNew123!');

      expect(result.success).toBe(false);
      expect(result.error).toMatch(/current password|incorrect/i);
    });
  });
});


