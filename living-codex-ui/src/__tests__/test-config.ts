// Test configuration for switching between mock and real API data
export const testConfig = {
  // Set to true to use real backend API, false to use mock data
  useRealApi: process.env.USE_REAL_API === 'true',
  
  // Backend API configuration for tests
  backend: {
    baseUrl: process.env.TEST_BACKEND_URL || 'http://localhost:5002',
    timeout: 10000,
  },
  
  // Test data configuration
  testData: {
    // If using real API, these will be ignored
    mockGalleryItems: [
      {
        id: 'concept-1',
        name: 'Quantum Computing',
        title: 'Quantum Computing',
        description: 'Computing based on quantum mechanical phenomena',
        domain: 'Technology',
        complexity: 8,
        resonance: 0.85,
        tags: ['quantum', 'computing', 'technology'],
        createdAt: '2024-01-15T10:00:00Z',
        energy: 0.7,
        imageUrl: 'https://via.placeholder.com/300x200/4F46E5/FFFFFF?text=Quantum+Computing',
        thumbnailUrl: 'https://via.placeholder.com/150x100/4F46E5/FFFFFF?text=QC',
        author: { name: 'Test Author' },
        likes: 42,
        comments: 8,
        axes: ['Technology', 'Science'],
        mediaType: 'image',
        aiGenerated: false
      },
      {
        id: 'concept-2',
        name: 'Consciousness',
        title: 'Consciousness',
        description: 'The state of being aware and able to think',
        domain: 'Philosophy',
        complexity: 9,
        resonance: 0.92,
        tags: ['consciousness', 'philosophy', 'awareness'],
        createdAt: '2024-01-14T15:30:00Z',
        energy: 0.8,
        imageUrl: 'https://via.placeholder.com/300x200/7C3AED/FFFFFF?text=Consciousness',
        thumbnailUrl: 'https://via.placeholder.com/150x100/7C3AED/FFFFFF?text=CON',
        author: { name: 'Test Author' },
        likes: 67,
        comments: 12,
        axes: ['Philosophy', 'Psychology'],
        mediaType: 'image',
        aiGenerated: false
      },
      {
        id: 'concept-3',
        name: 'Sustainable Energy',
        title: 'Sustainable Energy',
        description: 'Renewable energy sources for environmental sustainability',
        domain: 'Environment',
        complexity: 6,
        resonance: 0.78,
        tags: ['sustainability', 'energy', 'environment'],
        createdAt: '2024-01-13T09:15:00Z',
        energy: 0.6,
        imageUrl: 'https://via.placeholder.com/300x200/10B981/FFFFFF?text=Sustainable+Energy',
        thumbnailUrl: 'https://via.placeholder.com/150x100/10B981/FFFFFF?text=SE',
        author: { name: 'Test Author' },
        likes: 35,
        comments: 6,
        axes: ['Environment', 'Technology'],
        mediaType: 'image',
        aiGenerated: false
      }
    ]
  }
};

// Helper function to get fetch mock based on configuration
export function getFetchMock() {
  if (testConfig.useRealApi) {
    // Return undefined to use real fetch
    return undefined;
  }
  
  // Return mock implementation
  return jest.fn().mockImplementation((url: string) => {
    if (url.includes('/gallery/list')) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({
          success: true,
          items: testConfig.testData.mockGalleryItems
        }),
        text: () => Promise.resolve(JSON.stringify({
          success: true,
          items: testConfig.testData.mockGalleryItems
        }))
      });
    }
    return Promise.resolve({
      ok: false,
      status: 404,
      text: () => Promise.resolve('Not Found')
    });
  });
}

// Helper function to check if backend is available
export async function isBackendAvailable(): Promise<boolean> {
  try {
    const response = await fetch(`${testConfig.backend.baseUrl}/health`);
    return response.ok;
  } catch {
    return false;
  }
}

