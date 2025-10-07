/**
 * Integration Test Harness
 * 
 * Manages backend server lifecycle for integration tests.
 * Ensures tests run against real API with proper setup/teardown.
 */

import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';

export class IntegrationTestHarness {
  private serverProcess: ChildProcess | null = null;
  private readonly backendUrl = 'http://localhost:5002';
  private readonly startupTimeout = 60000; // 60 seconds
  private readonly healthCheckInterval = 1000; // 1 second
  
  /**
   * Start the backend server if not already running
   */
  async startServer(): Promise<void> {
    // Check if server is already running
    if (await this.isServerHealthy()) {
      console.log('✅ Server already running');
      return;
    }
    
    console.log('🚀 Starting backend server...');
    
    const rootDir = path.resolve(__dirname, '../..');
    const startScript = path.join(rootDir, 'start-server.sh');
    
    this.serverProcess = spawn(startScript, [], {
      cwd: rootDir,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Testing',
      },
      stdio: 'pipe',
    });
    
    // Log output for debugging
    this.serverProcess.stdout?.on('data', (data) => {
      console.log(`[SERVER] ${data.toString().trim()}`);
    });
    
    this.serverProcess.stderr?.on('data', (data) => {
      console.error(`[SERVER ERROR] ${data.toString().trim()}`);
    });
    
    this.serverProcess.on('error', (error) => {
      console.error('❌ Failed to start server:', error);
    });
    
    this.serverProcess.on('exit', (code) => {
      if (code !== 0 && code !== null) {
        console.error(`❌ Server exited with code ${code}`);
      }
    });
    
    // Wait for server to be healthy
    await this.waitForHealth();
    console.log('✅ Server started successfully');
  }
  
  /**
   * Wait for server to respond to health checks
   */
  private async waitForHealth(): Promise<void> {
    const startTime = Date.now();
    
    while (Date.now() - startTime < this.startupTimeout) {
      if (await this.isServerHealthy()) {
        return;
      }
      
      await new Promise(resolve => setTimeout(resolve, this.healthCheckInterval));
    }
    
    throw new Error(`Server failed to start within ${this.startupTimeout}ms`);
  }
  
  /**
   * Check if server is healthy
   */
  private async isServerHealthy(): Promise<boolean> {
    try {
      const response = await fetch(`${this.backendUrl}/health`, {
        signal: AbortSignal.timeout(2000),
      });
      
      if (!response.ok) {
        return false;
      }
      
      const data = await response.json();
      return data.status === 'healthy' || data.status === 'Healthy';
    } catch (error) {
      return false;
    }
  }
  
  /**
   * Clean test data from database
   * Removes all entities created during testing
   */
  async cleanDatabase(): Promise<void> {
    try {
      // Delete test users (those with test prefix)
      await this.deleteTestUsers();
      
      // Delete test concepts
      await this.deleteTestConcepts();
      
      // Delete test edges
      await this.deleteTestEdges();
      
      console.log('🧹 Database cleaned');
    } catch (error) {
      console.warn('⚠️ Database cleanup failed:', error);
      // Don't throw - cleanup is best-effort
    }
  }
  
  /**
   * Delete all test users (username starts with 'testuser' or 'test_')
   */
  private async deleteTestUsers(): Promise<void> {
    try {
      const response = await fetch(`${this.backendUrl}/test/cleanup/users`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: '^(testuser|test_)' }),
      });
      
      if (!response.ok && response.status !== 404) {
        console.warn(`⚠️ User cleanup returned ${response.status}`);
      }
    } catch (error) {
      console.warn('⚠️ User cleanup failed:', error);
    }
  }
  
  /**
   * Delete all test concepts (id contains 'test')
   */
  private async deleteTestConcepts(): Promise<void> {
    try {
      const response = await fetch(`${this.backendUrl}/test/cleanup/concepts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: 'test' }),
      });
      
      if (!response.ok && response.status !== 404) {
        console.warn(`⚠️ Concept cleanup returned ${response.status}`);
      }
    } catch (error) {
      console.warn('⚠️ Concept cleanup failed:', error);
    }
  }
  
  /**
   * Delete all test edges
   */
  private async deleteTestEdges(): Promise<void> {
    try {
      const response = await fetch(`${this.backendUrl}/test/cleanup/edges`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ pattern: 'test' }),
      });
      
      if (!response.ok && response.status !== 404) {
        console.warn(`⚠️ Edge cleanup returned ${response.status}`);
      }
    } catch (error) {
      console.warn('⚠️ Edge cleanup failed:', error);
    }
  }
  
  /**
   * Stop the backend server
   */
  async stopServer(): Promise<void> {
    if (this.serverProcess) {
      console.log('🛑 Stopping backend server...');
      
      // Send SIGTERM for graceful shutdown
      this.serverProcess.kill('SIGTERM');
      
      // Wait up to 5 seconds for graceful shutdown
      await new Promise<void>((resolve) => {
        const timeout = setTimeout(() => {
          if (this.serverProcess) {
            console.log('⚠️ Force killing server...');
            this.serverProcess.kill('SIGKILL');
          }
          resolve();
        }, 5000);
        
        this.serverProcess?.on('exit', () => {
          clearTimeout(timeout);
          resolve();
        });
      });
      
      this.serverProcess = null;
      console.log('✅ Server stopped');
    }
  }
  
  /**
   * Get backend URL
   */
  getBackendUrl(): string {
    return this.backendUrl;
  }
}

// Singleton instance
let harnessInstance: IntegrationTestHarness | null = null;

/**
 * Get or create singleton harness instance
 */
export function getIntegrationHarness(): IntegrationTestHarness {
  if (!harnessInstance) {
    harnessInstance = new IntegrationTestHarness();
  }
  return harnessInstance;
}

/**
 * Setup function for Jest globalSetup
 */
export async function globalSetup() {
  const harness = getIntegrationHarness();
  await harness.startServer();
}

/**
 * Teardown function for Jest globalTeardown
 */
export async function globalTeardown() {
  if (harnessInstance) {
    await harnessInstance.stopServer();
  }
}


