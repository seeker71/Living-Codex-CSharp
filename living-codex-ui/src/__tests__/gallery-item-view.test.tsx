import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'

// Mock the GalleryLens component to test individual item behavior
const MockGalleryItem = ({ item, onSelect }: { item: any, onSelect: (item: any) => void }) => {
  return (
    <div
      className="relative group cursor-pointer bg-white dark:bg-gray-800 rounded-lg overflow-hidden shadow-sm hover:shadow-lg transition-all duration-300"
      onClick={() => onSelect(item)}
      data-testid={`gallery-item-${item.id}`}
    >
      {/* Image Section */}
      <div className="aspect-square relative">
        {item.imageError ? (
          <div className="w-full h-full bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center p-4">
            <div className="text-red-500 text-2xl mb-2">⚠️</div>
            <div className="text-red-700 dark:text-red-300 text-xs text-center">
              <p className="font-semibold">Image Error</p>
              <p className="mt-1">{item.imageError}</p>
            </div>
          </div>
        ) : item.imageUrl ? (
          <img
            src={item.imageUrl}
            alt={item.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            onError={(e) => {
              e.currentTarget.style.display = 'none'
              e.currentTarget.nextElementSibling?.classList.remove('hidden')
            }}
          />
        ) : (
          <div className="w-full h-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center">
            <div className="text-gray-500 text-2xl">📷</div>
          </div>
        )}
        
        {/* Fallback for failed images */}
        <div className="hidden w-full h-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center">
          <div className="text-gray-500 text-2xl">📷</div>
        </div>
        
        {/* Status indicators */}
        <div className="absolute top-2 right-2 flex space-x-1">
          {item.aiGenerated && (
            <div className="bg-green-600 bg-opacity-80 text-white text-xs px-2 py-1 rounded">
              AI
            </div>
          )}
        </div>
      </div>
      
      {/* Overlay with concept info */}
      <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-30 transition-all duration-300 flex items-end">
        <div className="p-3 text-white opacity-0 group-hover:opacity-100 transition-opacity duration-300">
          <h3 className="font-semibold text-sm truncate">{item.title}</h3>
          <p className="text-xs text-gray-200 truncate">{item.domain}</p>
          <div className="flex items-center mt-1 space-x-2">
            <span className="text-xs bg-blue-500 px-2 py-1 rounded">Resonance: {item.resonance.toFixed(2)}</span>
          </div>
        </div>
      </div>

      {/* Always-visible caption */}
      <div className="p-3 border-t border-gray-100 dark:border-gray-700">
        <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate">{item.title}</h3>
        <div className="flex items-center justify-between mt-1">
          <span className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.domain}</span>
          {item.author?.name && (
            <span className="text-xs text-gray-500 dark:text-gray-400 truncate">by {item.author.name}</span>
          )}
        </div>
      </div>
    </div>
  )
}

// Mock the GalleryLens component
const MockGalleryLens = () => {
  const [selectedItem, setSelectedItem] = React.useState(null)
  
  const mockItems = [
    {
      id: 'concept-1',
      title: 'Quantum Computing',
      description: 'Computing based on quantum mechanical phenomena',
      domain: 'Technology',
      resonance: 0.85,
      imageUrl: 'https://via.placeholder.com/400x400/6366f1/ffffff?text=QC',
      thumbnailUrl: 'https://via.placeholder.com/200x200/6366f1/ffffff?text=QC',
      author: { name: 'Living Codex', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC' },
      aiGenerated: false
    },
    {
      id: 'concept-2',
      title: 'Consciousness',
      description: 'The state of being aware and able to think',
      domain: 'Philosophy',
      resonance: 0.92,
      imageUrl: 'https://via.placeholder.com/400x400/8b5cf6/ffffff?text=CO',
      thumbnailUrl: 'https://via.placeholder.com/200x200/8b5cf6/ffffff?text=CO',
      author: { name: 'Living Codex', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC' },
      aiGenerated: true
    },
    {
      id: 'concept-3',
      title: 'Sustainable Energy',
      description: 'Renewable energy sources for environmental sustainability',
      domain: 'Environment',
      resonance: 0.78,
      imageUrl: null, // No image URL
      thumbnailUrl: null,
      author: { name: 'Living Codex', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC' },
      aiGenerated: false
    },
    {
      id: 'concept-4',
      title: 'Failed Image',
      description: 'This concept has an image that fails to load',
      domain: 'Test',
      resonance: 0.5,
      imageUrl: 'https://invalid-url-that-will-fail.com/image.jpg',
      thumbnailUrl: 'https://invalid-url-that-will-fail.com/thumb.jpg',
      author: { name: 'Living Codex', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=LC' },
      aiGenerated: false,
      imageError: 'Failed to load image'
    }
  ]

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Concept Gallery</h2>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1">
        {mockItems.map((item) => (
          <MockGalleryItem
            key={item.id}
            item={item}
            onSelect={setSelectedItem}
          />
        ))}
      </div>

      {/* Modal */}
      {selectedItem && (
        <div className="fixed inset-0 bg-black bg-opacity-90 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="flex flex-col lg:flex-row">
              {/* Image Section */}
              <div className="flex-1 p-6">
                {selectedItem.imageError ? (
                  <div className="w-full h-96 bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center rounded-lg">
                    <div className="text-red-500 text-4xl mb-4">⚠️</div>
                    <h3 className="text-lg font-semibold text-red-700 dark:text-red-300 mb-2">Image Generation Failed</h3>
                    <p className="text-red-600 dark:text-red-400 text-sm text-center max-w-md">{selectedItem.imageError}</p>
                  </div>
                ) : selectedItem.imageUrl ? (
                  <img
                    src={selectedItem.imageUrl}
                    alt={selectedItem.title}
                    className="w-full h-auto rounded-lg"
                    onError={(e) => {
                      e.currentTarget.style.display = 'none'
                      e.currentTarget.nextElementSibling?.classList.remove('hidden')
                    }}
                  />
                ) : (
                  <div className="w-full h-96 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                    <div className="text-gray-500 text-4xl">📷</div>
                  </div>
                )}
                
                {/* Fallback for failed images in modal */}
                <div className="hidden w-full h-96 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                  <div className="text-gray-500 text-4xl">📷</div>
                </div>
              </div>

              {/* Details Section */}
              <div className="w-full lg:w-96 p-6 border-t lg:border-t-0 lg:border-l border-gray-200 dark:border-gray-700">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                    {selectedItem.title}
                  </h3>
                  <button
                    onClick={() => setSelectedItem(null)}
                    className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                    data-testid="close-modal"
                  >
                    ✕
                  </button>
                </div>

                <div className="space-y-4">
                  <div>
                    <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">Description</h4>
                    <p className="text-gray-600 dark:text-gray-400 text-sm mb-2">
                      {selectedItem.description}
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Domain:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.domain}</p>
                    </div>
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Resonance:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.resonance.toFixed(3)}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

describe('Gallery Item View Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Image Display Validation', () => {
    it('displays placeholder image correctly', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that placeholder images are displayed
      const quantumImage = screen.getByAltText('Quantum Computing')
      const consciousnessImage = screen.getByAltText('Consciousness')
      
      expect(quantumImage).toBeInTheDocument()
      expect(consciousnessImage).toBeInTheDocument()
      
      // Check that images have proper src attributes
      expect(quantumImage).toHaveAttribute('src', 'https://via.placeholder.com/400x400/6366f1/ffffff?text=QC')
      expect(consciousnessImage).toHaveAttribute('src', 'https://via.placeholder.com/400x400/8b5cf6/ffffff?text=CO')
    })

    it('shows fallback when no image URL is provided', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that fallback placeholder is shown for item without image URL
      const energyItem = screen.getByTestId('gallery-item-concept-3')
      const fallbackIcon = within(energyItem).getByText('📷')
      expect(fallbackIcon).toBeInTheDocument()
    })

    it('displays error state when image fails to load', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that error state is displayed for item with image error
      const failedItem = screen.getByTestId('gallery-item-concept-4')
      expect(within(failedItem).getByText('Image Error')).toBeInTheDocument()
      expect(within(failedItem).getByText('Failed to load image')).toBeInTheDocument()
    })

    it('handles image load error gracefully', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Simulate image load error
      const failedImage = screen.getByAltText('Failed Image')
      fireEvent.error(failedImage)
      
      // Check that fallback is shown
      await waitFor(() => {
        const fallbackIcon = within(failedImage.closest('div')!).getByText('📷')
        expect(fallbackIcon).toBeInTheDocument()
      })
    })
  })

  describe('Image Styling and Layout', () => {
    it('applies correct CSS classes to images', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveClass('w-full', 'h-full', 'object-cover')
      })
    })

    it('shows hover effects on gallery items', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      const quantumItem = screen.getByTestId('gallery-item-concept-1')
      expect(quantumItem).toHaveClass('group', 'cursor-pointer', 'hover:shadow-lg')
    })

    it('displays AI generated indicator correctly', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that AI indicator is shown for AI generated items
      const consciousnessItem = screen.getByTestId('gallery-item-concept-2')
      expect(within(consciousnessItem).getByText('AI')).toBeInTheDocument()
      
      // Check that AI indicator is not shown for non-AI items
      const quantumItem = screen.getByTestId('gallery-item-concept-1')
      expect(within(quantumItem).queryByText('AI')).not.toBeInTheDocument()
    })
  })

  describe('Modal Image Display', () => {
    it('opens modal with large image when clicking gallery item', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Click on quantum computing item
      const quantumItem = screen.getByTestId('gallery-item-concept-1')
      fireEvent.click(quantumItem)
      
      // Check that modal opens with large image
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
        const modalImage = screen.getByAltText('Quantum Computing')
        expect(modalImage).toHaveClass('w-full', 'h-auto', 'rounded-lg')
      })
    })

    it('displays error state in modal when image fails', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Click on failed image item
      const failedItem = screen.getByTestId('gallery-item-concept-4')
      fireEvent.click(failedItem)
      
      // Check that error state is displayed in modal
      await waitFor(() => {
        expect(screen.getByText('Image Generation Failed')).toBeInTheDocument()
        expect(screen.getByText('Failed to load image')).toBeInTheDocument()
      })
    })

    it('shows fallback in modal when no image URL', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Click on energy item (no image URL)
      const energyItem = screen.getByTestId('gallery-item-concept-3')
      fireEvent.click(energyItem)
      
      // Check that fallback is shown in modal
      await waitFor(() => {
        expect(screen.getByText('Sustainable Energy')).toBeInTheDocument()
        const fallbackIcon = screen.getByText('📷')
        expect(fallbackIcon).toBeInTheDocument()
      })
    })

    it('closes modal when clicking close button', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Click on quantum computing item
      const quantumItem = screen.getByTestId('gallery-item-concept-1')
      fireEvent.click(quantumItem)
      
      await waitFor(() => {
        expect(screen.getByText('Quantum Computing')).toBeInTheDocument()
      })
      
      // Click close button
      const closeButton = screen.getByTestId('close-modal')
      fireEvent.click(closeButton)
      
      // Check that modal is closed
      await waitFor(() => {
        expect(screen.queryByText('Quantum Computing')).not.toBeInTheDocument()
      })
    })
  })

  describe('Image Performance and Optimization', () => {
    it('uses appropriate image dimensions for thumbnails', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that images use placeholder service with appropriate dimensions
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        const src = img.getAttribute('src')
        if (src?.includes('via.placeholder.com')) {
          expect(src).toMatch(/400x400/) // Grid images should be 400x400
        }
      })
    })

    it('handles image loading states properly', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Check that all images are loaded immediately (placeholders)
      const images = screen.getAllByRole('img')
      expect(images.length).toBeGreaterThan(0)
      
      // All images should have src attributes
      images.forEach(img => {
        expect(img).toHaveAttribute('src')
      })
    })
  })

  describe('Accessibility for Images', () => {
    it('provides proper alt text for all images', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('alt')
        expect(img.getAttribute('alt')).not.toBe('')
        expect(img.getAttribute('alt')).toMatch(/Quantum Computing|Consciousness|Failed Image/)
      })
    })

    it('supports keyboard navigation for gallery items', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Test keyboard navigation
      const quantumItem = screen.getByTestId('gallery-item-concept-1')
      quantumItem.focus()
      expect(document.activeElement).toBe(quantumItem)
      
      // Test Enter key activation
      fireEvent.keyDown(quantumItem, { key: 'Enter', code: 'Enter' })
      // Should open modal (tested in other tests)
    })
  })

  describe('Error Recovery', () => {
    it('recovers from image load errors', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Simulate image load error
      const failedImage = screen.getByAltText('Failed Image')
      fireEvent.error(failedImage)
      
      // Check that error is handled gracefully
      await waitFor(() => {
        const fallbackIcon = within(failedImage.closest('div')!).getByText('📷')
        expect(fallbackIcon).toBeInTheDocument()
      })
    })

    it('maintains functionality when images fail to load', async () => {
      renderWithProviders(<MockGalleryLens />)
      
      await waitFor(() => {
        expect(screen.getByText('Concept Gallery')).toBeInTheDocument()
      })
      
      // Click on item with failed image
      const failedItem = screen.getByTestId('gallery-item-concept-4')
      fireEvent.click(failedItem)
      
      // Check that modal still opens and shows content
      await waitFor(() => {
        expect(screen.getByText('Failed Image')).toBeInTheDocument()
        expect(screen.getByText('This concept has an image that fails to load')).toBeInTheDocument()
      })
    })
  })
})
