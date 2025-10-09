/**
 * Create Page Tests
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';

// Mock next/navigation BEFORE importing the component
const mockPush = jest.fn();
const mockBack = jest.fn();
const mockRouter = {
  push: mockPush,
  back: mockBack,
  forward: jest.fn(),
  refresh: jest.fn(),
  replace: jest.fn(),
  prefetch: jest.fn(),
};

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
  usePathname: () => '/create',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock the concepts API
jest.mock('@/lib/concepts-api', () => ({
  createConcept: jest.fn(),
  CONCEPT_DOMAINS: [
    { value: 'consciousness', label: 'Consciousness & Awareness', frequency: 741 },
    { value: 'love', label: 'Love & Compassion', frequency: 528 },
    { value: 'science', label: 'Science & Knowledge', frequency: 256 }
  ],
  COMPLEXITY_LEVELS: [
    { value: 1, label: 'Fundamental', description: 'Basic building block concept' },
    { value: 5, label: 'Intermediate', description: 'Requires background knowledge' },
    { value: 10, label: 'Universal', description: 'Fundamental to existence' }
  ]
}));

// Mock auth context
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'user123', name: 'Test User' },
    isAuthenticated: true
  })
}));

// Mock hooks
jest.mock('@/lib/hooks', () => ({
  useTrackInteraction: () => jest.fn()
}));

// Import the component AFTER setting up mocks
import CreatePage from '../create/page';

import * as mockConceptsApi from '@/lib/concepts-api';

describe('Create Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders concept creation form', () => {
    render(<CreatePage />);
    
    expect(screen.getByText('✨ Concept Creation')).toBeInTheDocument();
    expect(screen.getByText('Create new concepts with AI assistance and generate visual representations')).toBeInTheDocument();
  });

  it('renders all three tabs', () => {
    render(<CreatePage />);
    
    expect(screen.getByText(/🧠.*Concept/)).toBeInTheDocument();
    expect(screen.getByText(/🤖.*AI Assistant/)).toBeInTheDocument();
    expect(screen.getByText(/🎨.*Visual Creation/)).toBeInTheDocument();
  });

  it('renders form fields in concept tab', () => {
    render(<CreatePage />);
    
    expect(screen.getByLabelText('Concept Name *')).toBeInTheDocument();
    expect(screen.getByLabelText('Description *')).toBeInTheDocument();
    expect(screen.getByLabelText('Domain')).toBeInTheDocument();
    expect(screen.getByLabelText('Complexity Level: 5')).toBeInTheDocument();
    expect(screen.getByLabelText('Tags')).toBeInTheDocument();
  });

  it('handles form input changes', () => {
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    
    fireEvent.change(nameInput, { target: { value: 'Test Concept' } });
    fireEvent.change(descriptionInput, { target: { value: 'A test concept' } });
    
    expect(nameInput).toHaveValue('Test Concept');
    expect(descriptionInput).toHaveValue('A test concept');
  });

  it('handles complexity slider', () => {
    render(<CreatePage />);
    
    const complexitySlider = screen.getByLabelText('Complexity Level: 5');
    fireEvent.change(complexitySlider, { target: { value: '8' } });
    
    expect(screen.getByLabelText('Complexity Level: 8')).toBeInTheDocument();
  });

  it('handles domain selection', () => {
    render(<CreatePage />);
    
    const domainSelect = screen.getByLabelText('Domain');
    fireEvent.change(domainSelect, { target: { value: 'love' } });
    
    expect(domainSelect).toHaveValue('love');
  });

  it('handles tag addition', () => {
    render(<CreatePage />);
    
    const tagInput = screen.getByPlaceholderText('Add a tag...');
    const addButton = screen.getByText('Add');
    
    fireEvent.change(tagInput, { target: { value: 'test-tag' } });
    fireEvent.click(addButton);
    
    expect(screen.getByText('test-tag')).toBeInTheDocument();
    expect(tagInput).toHaveValue('');
  });

  it('handles tag removal', () => {
    render(<CreatePage />);
    
    const tagInput = screen.getByPlaceholderText('Add a tag...');
    const addButton = screen.getByText('Add');
    
    fireEvent.change(tagInput, { target: { value: 'test-tag' } });
    fireEvent.click(addButton);
    
    expect(screen.getByText('test-tag')).toBeInTheDocument();
    
    const removeButton = screen.getByText('×');
    fireEvent.click(removeButton);
    
    expect(screen.queryByText('test-tag')).not.toBeInTheDocument();
  });

  it('prevents duplicate tags', () => {
    render(<CreatePage />);
    
    const tagInput = screen.getByPlaceholderText('Add a tag...');
    const addButton = screen.getByText('Add');
    
    fireEvent.change(tagInput, { target: { value: 'test-tag' } });
    fireEvent.click(addButton);
    
    fireEvent.change(tagInput, { target: { value: 'test-tag' } });
    fireEvent.click(addButton);
    
    const tags = screen.getAllByText('test-tag');
    expect(tags).toHaveLength(1);
  });

  it('handles tab switching', () => {
    render(<CreatePage />);
    
    const aiTab = screen.getByText(/🤖.*AI Assistant/);
    fireEvent.click(aiTab);
    
    expect(screen.getByText('🤖 AI Concept Assistant')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('I want to create a concept about...')).toBeInTheDocument();
  });

  it('shows validation error for empty form', async () => {
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    const createButton = screen.getByText('✨ Create Concept');
    
    // Initially the button should be disabled due to empty fields
    expect(createButton).toBeDisabled();
    
    // Fill in minimal required fields to enable the button
    fireEvent.change(nameInput, { target: { value: 'Test' } });
    fireEvent.change(descriptionInput, { target: { value: 'Test' } });
    
    // The button should now be enabled
    expect(createButton).not.toBeDisabled();
    
    // Clear the fields to disable the button again
    fireEvent.change(nameInput, { target: { value: '' } });
    fireEvent.change(descriptionInput, { target: { value: '' } });
    
    // The button should be disabled again
    expect(createButton).toBeDisabled();
  });

  it('creates concept successfully', async () => {
    mockConceptsApi.createConcept.mockResolvedValue({
      success: true,
      conceptId: 'codex.concept.Test.123',
      message: 'Concept created successfully'
    });
    
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    const createButton = screen.getByText('✨ Create Concept');
    
    fireEvent.change(nameInput, { target: { value: 'Test Concept' } });
    fireEvent.change(descriptionInput, { target: { value: 'A test concept' } });
    fireEvent.click(createButton);
    
    await waitFor(() => {
      expect(mockConceptsApi.createConcept).toHaveBeenCalledWith({
        name: 'Test Concept',
        description: 'A test concept',
        domain: 'consciousness',
        complexity: 5,
        tags: []
      });
    });
    
    await waitFor(() => {
      expect(screen.getByText('Concept created successfully')).toBeInTheDocument();
    });
  });

  it('handles API errors', async () => {
    mockConceptsApi.createConcept.mockRejectedValue(new Error('API Error'));
    
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    const createButton = screen.getByText('✨ Create Concept');
    
    fireEvent.change(nameInput, { target: { value: 'Test Concept' } });
    fireEvent.change(descriptionInput, { target: { value: 'A test concept' } });
    fireEvent.click(createButton);
    
    await waitFor(() => {
      expect(screen.getByText('API Error')).toBeInTheDocument();
    });
  });

  it('renders quick start templates', () => {
    render(<CreatePage />);
    
    expect(screen.getByText('🚀 Quick Start Templates')).toBeInTheDocument();
    expect(screen.getByText('Consciousness Bridge')).toBeInTheDocument();
    expect(screen.getByText('Unity Fractal')).toBeInTheDocument();
    expect(screen.getByText('Abundance Flow')).toBeInTheDocument();
  });

  it('handles template selection', () => {
    render(<CreatePage />);
    
    // Get the first template button (Consciousness Bridge)
    const templateButtons = screen.getAllByText('Use Template');
    const consciousnessTemplateButton = templateButtons[0];
    fireEvent.click(consciousnessTemplateButton);
    
    expect(screen.getByDisplayValue('Consciousness Bridge')).toBeInTheDocument();
    expect(screen.getByDisplayValue('A concept connecting different states of awareness')).toBeInTheDocument();
  });

  it('disables create button when form is invalid', () => {
    render(<CreatePage />);
    
    const createButton = screen.getByText('✨ Create Concept');
    expect(createButton).toBeDisabled();
  });

  it('enables create button when form is valid', () => {
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    
    fireEvent.change(nameInput, { target: { value: 'Test Concept' } });
    fireEvent.change(descriptionInput, { target: { value: 'A test concept' } });
    
    const createButton = screen.getByText('✨ Create Concept');
    expect(createButton).not.toBeDisabled();
  });

  it('shows loading state during creation', async () => {
    mockConceptsApi.createConcept.mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve({
        success: true,
        conceptId: 'codex.concept.Test.123',
        message: 'Concept created successfully'
      }), 100))
    );
    
    render(<CreatePage />);
    
    const nameInput = screen.getByLabelText('Concept Name *');
    const descriptionInput = screen.getByLabelText('Description *');
    const createButton = screen.getByText('✨ Create Concept');
    
    fireEvent.change(nameInput, { target: { value: 'Test Concept' } });
    fireEvent.change(descriptionInput, { target: { value: 'A test concept' } });
    fireEvent.click(createButton);
    
    expect(screen.getByText('✨ Creating Concept...')).toBeInTheDocument();
  });
});
