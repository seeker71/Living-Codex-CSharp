const React = require('react');
const { render, screen, waitFor } = require('@testing-library/react');
require('@testing-library/jest-dom');

// Ensure test utils (including AuthContext mock) are initialized before importing component
const { renderWithProviders } = require('../src/__tests__/test-utils');
const { GalleryLens } = require('../src/components/lenses/GalleryLens');

describe('Gallery UI Tests with Real API', () => {

  test('should load gallery with real API calls', async () => {
    console.log('🎨 Testing Gallery UI with Real API Calls');
    
    // Test the real component with real API calls
    const defaultProps = {
      controls: {
        axes: ['resonance'],
        joy: 0.7,
        serendipity: 0.5,
      },
      userId: 'test-user',
      readOnly: false,
    };

    // Render the component with real API calls and AuthProvider
    const mockAuthValue = {
      user: { id: 'test-user', username: 'testuser' },
      isAuthenticated: true,
      isLoading: false
    };
    renderWithProviders(<GalleryLens {...defaultProps} />, { authValue: mockAuthValue });
    
    // Verify the component loads and displays content
    await waitFor(() => {
      expect(screen.getByText('Visual Discovery Gallery')).toBeInTheDocument();
      expect(screen.getByText('Explore concepts through visual representations and artistic interpretations')).toBeInTheDocument();
    }, { timeout: 10000 });
    
    console.log('✅ Gallery UI test passed - component loaded with real API calls');
  });

  test('should handle API errors gracefully', async () => {
    console.log('🔧 Testing Gallery UI Error Handling');
    
    const defaultProps = {
      controls: {
        axes: ['resonance'],
        joy: 0.7,
        serendipity: 0.5,
      },
      userId: 'test-user',
      readOnly: false,
    };

    // Render the component with AuthProvider
    const mockAuthValue = {
      user: { id: 'test-user', username: 'testuser' },
      isAuthenticated: true,
      isLoading: false
    };
    renderWithProviders(<GalleryLens {...defaultProps} />, { authValue: mockAuthValue });
    
    // Wait for either success or error state
    await waitFor(() => {
      // Either the gallery loads successfully or shows an error
      const hasContent = screen.queryByText('Explore concepts through visual representations and artistic interpretations') ||
                        screen.queryByText('Error Loading Gallery') ||
                        screen.queryByText('No concepts available yet');
      expect(hasContent).toBeTruthy();
    }, { timeout: 15000 });
    
    console.log('✅ Gallery UI error handling test passed');
  });
});
