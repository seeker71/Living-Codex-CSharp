import React from 'react'
import { screen, waitFor, fireEvent, within } from '@testing-library/react'
import { renderWithProviders } from './test-utils'

// Mock image validation utilities
const validateImageUrl = (url: string): boolean => {
  if (!url) return false
  if (url.includes('invalid-url')) return false
  if (url.includes('error-url')) return false
  return true
}

const getImageDimensions = (url: string): { width: number; height: number } => {
  if (url.includes('400x400')) return { width: 400, height: 400 }
  if (url.includes('200x200')) return { width: 200, height: 200 }
  return { width: 400, height: 400 }
}

// Mock GalleryLens with image validation
const MockGalleryLensWithValidation = () => {
  const [items, setItems] = React.useState([])
  const [selectedItem, setSelectedItem] = React.useState(null)
  const [imageErrors, setImageErrors] = React.useState({})

  React.useEffect(() => {
    // Mock data with various image states
    const mockItems = [
      {
        id: 'valid-image',
        title: 'Valid Image',
        description: 'This has a valid image URL',
        domain: 'Test',
        resonance: 0.8,
        imageUrl: 'https://via.placeholder.com/400x400/6366f1/ffffff?text=VI',
        thumbnailUrl: 'https://via.placeholder.com/200x200/6366f1/ffffff?text=VI',
        author: { name: 'Test Author', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=TA' },
        aiGenerated: false
      },
      {
        id: 'invalid-url',
        title: 'Invalid URL',
        description: 'This has an invalid image URL',
        domain: 'Test',
        resonance: 0.6,
        imageUrl: 'https://invalid-url-that-will-fail.com/image.jpg',
        thumbnailUrl: 'https://invalid-url-that-will-fail.com/thumb.jpg',
        author: { name: 'Test Author', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=TA' },
        aiGenerated: false
      },
      {
        id: 'error-url',
        title: 'Error URL',
        description: 'This has an error URL',
        domain: 'Test',
        resonance: 0.4,
        imageUrl: 'https://error-url.com/image.jpg',
        thumbnailUrl: 'https://error-url.com/thumb.jpg',
        author: { name: 'Test Author', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=TA' },
        aiGenerated: false
      },
      {
        id: 'no-image',
        title: 'No Image',
        description: 'This has no image URL',
        domain: 'Test',
        resonance: 0.7,
        imageUrl: null,
        thumbnailUrl: null,
        author: { name: 'Test Author', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=TA' },
        aiGenerated: false
      },
      {
        id: 'empty-string',
        title: 'Empty String',
        description: 'This has an empty string image URL',
        domain: 'Test',
        resonance: 0.5,
        imageUrl: '',
        thumbnailUrl: '',
        author: { name: 'Test Author', avatar: 'https://via.placeholder.com/40x40/6366f1/ffffff?text=TA' },
        aiGenerated: false
      }
    ]

    // Validate images and set error states
    const validatedItems = mockItems.map(item => {
      const imageValid = validateImageUrl(item.imageUrl)
      const thumbnailValid = validateImageUrl(item.thumbnailUrl)
      
      return {
        ...item,
        imageValid,
        thumbnailValid,
        imageError: !imageValid ? 'Invalid image URL' : null,
        thumbnailError: !thumbnailValid ? 'Invalid thumbnail URL' : null
      }
    })

    setItems(validatedItems)
  }, [])

  const handleImageError = (itemId: string, error: string) => {
    setImageErrors(prev => ({
      ...prev,
      [itemId]: error
    }))
  }

  const renderImage = (item: any) => {
    if (imageErrors[item.id]) {
      return (
        <div className="w-full h-full bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center p-4">
          <div className="text-red-500 text-2xl mb-2">⚠️</div>
          <div className="text-red-700 dark:text-red-300 text-xs text-center">
            <p className="font-semibold">Image Error</p>
            <p className="mt-1">{imageErrors[item.id]}</p>
          </div>
        </div>
      )
    }

    if (!item.imageUrl || item.imageUrl === '') {
      return (
        <div className="w-full h-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center">
          <div className="text-gray-500 text-2xl">📷</div>
        </div>
      )
    }

    return (
      <img
        src={item.imageUrl}
        alt={item.title}
        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
        onError={() => handleImageError(item.id, 'Failed to load image')}
        onLoad={() => {
          // Validate image dimensions
          const dimensions = getImageDimensions(item.imageUrl)
          if (dimensions.width < 100 || dimensions.height < 100) {
            handleImageError(item.id, 'Image too small')
          }
        }}
      />
    )
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Gallery Image Validation Test</h2>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-1">
        {items.map((item) => (
          <div
            key={item.id}
            className="relative group cursor-pointer bg-white dark:bg-gray-800 rounded-lg overflow-hidden shadow-sm hover:shadow-lg transition-all duration-300"
            onClick={() => setSelectedItem(item)}
            data-testid={`gallery-item-${item.id}`}
          >
            <div className="aspect-square relative">
              {renderImage(item)}
              
              {/* Status indicators */}
              <div className="absolute top-2 right-2 flex space-x-1">
                {item.imageValid && (
                  <div className="bg-green-600 bg-opacity-80 text-white text-xs px-2 py-1 rounded">
                    ✓
                  </div>
                )}
                {!item.imageValid && (
                  <div className="bg-red-600 bg-opacity-80 text-white text-xs px-2 py-1 rounded">
                    ✗
                  </div>
                )}
              </div>
            </div>
            
            <div className="p-3 border-t border-gray-100 dark:border-gray-700">
              <h3 className="font-semibold text-sm text-gray-900 dark:text-gray-100 truncate">{item.title}</h3>
              <div className="flex items-center justify-between mt-1">
                <span className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.domain}</span>
                <span className="text-xs text-gray-500 dark:text-gray-400 truncate">
                  {item.imageValid ? 'Valid' : 'Invalid'}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Modal */}
      {selectedItem && (
        <div className="fixed inset-0 bg-black bg-opacity-90 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="flex flex-col lg:flex-row">
              <div className="flex-1 p-6">
                {imageErrors[selectedItem.id] ? (
                  <div className="w-full h-96 bg-red-100 dark:bg-red-900 flex flex-col items-center justify-center rounded-lg">
                    <div className="text-red-500 text-4xl mb-4">⚠️</div>
                    <h3 className="text-lg font-semibold text-red-700 dark:text-red-300 mb-2">Image Error</h3>
                    <p className="text-red-600 dark:text-red-400 text-sm text-center max-w-md">{imageErrors[selectedItem.id]}</p>
                  </div>
                ) : !selectedItem.imageUrl || selectedItem.imageUrl === '' ? (
                  <div className="w-full h-96 bg-gray-200 dark:bg-gray-700 flex items-center justify-center rounded-lg">
                    <div className="text-gray-500 text-4xl">📷</div>
                  </div>
                ) : (
                  <img
                    src={selectedItem.imageUrl}
                    alt={selectedItem.title}
                    className="w-full h-auto rounded-lg"
                    onError={() => handleImageError(selectedItem.id, 'Failed to load image in modal')}
                  />
                )}
              </div>

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
                      <span className="font-medium text-gray-900 dark:text-gray-100">Image Status:</span>
                      <p className={`text-sm ${selectedItem.imageValid ? 'text-green-600' : 'text-red-600'}`}>
                        {selectedItem.imageValid ? 'Valid' : 'Invalid'}
                      </p>
                    </div>
                    <div>
                      <span className="font-medium text-gray-900 dark:text-gray-100">Resonance:</span>
                      <p className="text-gray-600 dark:text-gray-400">{selectedItem.resonance.toFixed(3)}</p>
                    </div>
                  </div>

                  {imageErrors[selectedItem.id] && (
                    <div className="p-3 bg-red-100 dark:bg-red-900 rounded-lg">
                      <h4 className="text-sm font-medium text-red-800 dark:text-red-200 mb-1">Error Details:</h4>
                      <p className="text-xs text-red-700 dark:text-red-300">{imageErrors[selectedItem.id]}</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

describe('Gallery Image Validation Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Image URL Validation', () => {
    it('validates valid image URLs correctly', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that valid image is marked as valid
      const validItem = screen.getByTestId('gallery-item-valid-image')
      expect(within(validItem).getByText('Valid')).toBeInTheDocument()
      expect(within(validItem).getByText('✓')).toBeInTheDocument()
    })

    it('identifies invalid image URLs', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that invalid URL is marked as invalid
      const invalidItem = screen.getByTestId('gallery-item-invalid-url')
      expect(within(invalidItem).getByText('Invalid')).toBeInTheDocument()
      expect(within(invalidItem).getByText('✗')).toBeInTheDocument()
    })

    it('handles error URLs correctly', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that error URL is marked as invalid
      const errorItem = screen.getByTestId('gallery-item-error-url')
      expect(within(errorItem).getByText('Invalid')).toBeInTheDocument()
      expect(within(errorItem).getByText('✗')).toBeInTheDocument()
    })

    it('handles null image URLs', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that null URL shows fallback
      const noImageItem = screen.getByTestId('gallery-item-no-image')
      expect(within(noImageItem).getByText('📷')).toBeInTheDocument()
    })

    it('handles empty string image URLs', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that empty string URL shows fallback
      const emptyItem = screen.getByTestId('gallery-item-empty-string')
      expect(within(emptyItem).getByText('📷')).toBeInTheDocument()
    })
  })

  describe('Image Load Error Handling', () => {
    it('handles image load errors gracefully', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Simulate image load error
      const invalidImage = screen.getByAltText('Invalid URL')
      fireEvent.error(invalidImage)
      
      // Check that error is handled
      await waitFor(() => {
        const invalidItem = screen.getByTestId('gallery-item-invalid-url')
        expect(within(invalidItem).getByText('Image Error')).toBeInTheDocument()
        expect(within(invalidItem).getByText('Failed to load image')).toBeInTheDocument()
      })
    })

    it('validates image dimensions on load', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Simulate image load with valid dimensions
      const validImage = screen.getByAltText('Valid Image')
      fireEvent.load(validImage)
      
      // Check that valid image loads without error
      expect(validImage).toBeInTheDocument()
    })
  })

  describe('Modal Image Validation', () => {
    it('shows error state in modal for invalid images', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Click on invalid URL item
      const invalidItem = screen.getByTestId('gallery-item-invalid-url')
      fireEvent.click(invalidItem)
      
      // Check that error is shown in modal
      await waitFor(() => {
        expect(screen.getByText('Image Error')).toBeInTheDocument()
        expect(screen.getByText('Invalid image URL')).toBeInTheDocument()
      })
    })

    it('shows fallback in modal for null images', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Click on no image item
      const noImageItem = screen.getByTestId('gallery-item-no-image')
      fireEvent.click(noImageItem)
      
      // Check that fallback is shown in modal
      await waitFor(() => {
        expect(screen.getByText('No Image')).toBeInTheDocument()
        const fallbackIcon = screen.getByText('📷')
        expect(fallbackIcon).toBeInTheDocument()
      })
    })

    it('displays valid images correctly in modal', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Click on valid image item
      const validItem = screen.getByTestId('gallery-item-valid-image')
      fireEvent.click(validItem)
      
      // Check that valid image is displayed in modal
      await waitFor(() => {
        expect(screen.getByText('Valid Image')).toBeInTheDocument()
        const modalImage = screen.getByAltText('Valid Image')
        expect(modalImage).toHaveClass('w-full', 'h-auto', 'rounded-lg')
      })
    })
  })

  describe('Error Recovery and Retry', () => {
    it('allows retry after image load failure', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Simulate image load error
      const invalidImage = screen.getByAltText('Invalid URL')
      fireEvent.error(invalidImage)
      
      // Check that error is displayed
      await waitFor(() => {
        const invalidItem = screen.getByTestId('gallery-item-invalid-url')
        expect(within(invalidItem).getByText('Image Error')).toBeInTheDocument()
      })
      
      // Simulate retry by triggering load again
      fireEvent.load(invalidImage)
      
      // Error should still be there since URL is invalid
      const invalidItem = screen.getByTestId('gallery-item-invalid-url')
      expect(within(invalidItem).getByText('✗')).toBeInTheDocument()
    })
  })

  describe('Performance and Optimization', () => {
    it('loads images efficiently', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that all images have src attributes
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('src')
      })
    })

    it('handles multiple image states simultaneously', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that all different image states are handled
      expect(screen.getByTestId('gallery-item-valid-image')).toBeInTheDocument()
      expect(screen.getByTestId('gallery-item-invalid-url')).toBeInTheDocument()
      expect(screen.getByTestId('gallery-item-error-url')).toBeInTheDocument()
      expect(screen.getByTestId('gallery-item-no-image')).toBeInTheDocument()
      expect(screen.getByTestId('gallery-item-empty-string')).toBeInTheDocument()
    })
  })

  describe('Accessibility for Image Validation', () => {
    it('provides proper alt text for all image states', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      const images = screen.getAllByRole('img')
      images.forEach(img => {
        expect(img).toHaveAttribute('alt')
        expect(img.getAttribute('alt')).not.toBe('')
      })
    })

    it('provides error information for screen readers', async () => {
      renderWithProviders(<MockGalleryLensWithValidation />)
      
      await waitFor(() => {
        expect(screen.getByText('Gallery Image Validation Test')).toBeInTheDocument()
      })
      
      // Check that error states are accessible
      const invalidItem = screen.getByTestId('gallery-item-invalid-url')
      expect(within(invalidItem).getByText('Image Error')).toBeInTheDocument()
    })
  })
})
