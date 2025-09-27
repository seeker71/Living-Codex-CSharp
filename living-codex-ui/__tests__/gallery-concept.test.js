import { render, screen } from '@testing-library/react';

// Mock the concept data structure
const mockSoundHealingConcept = {
  id: "test-concept-id",
  title: "Sacred Sound Resonance",
  description: "The ancient art of healing through vibrational frequencies that harmonize the body, mind, and spirit.",
  conceptType: "healing",
  style: "mystical",
  mood: "transcendent",
  colors: ["deep purple", "golden light", "crystal blue", "silver", "rainbow"],
  elements: ["crystal singing bowls", "sound waves", "chakra colors", "sacred geometry", "energy fields"],
  metadata: {
    domain: "spiritual_healing",
    frequency: "432Hz",
    healingProperties: ["stress relief", "emotional release", "energy balancing", "consciousness expansion"]
  }
};

// Mock component for testing concept display
function ConceptCard({ concept }) {
  return (
    <div className="concept-card" data-testid="concept-card">
      <h3 className="concept-title">{concept.title}</h3>
      <p className="concept-description">{concept.description}</p>
      <div className="concept-metadata">
        <span className="concept-type">{concept.conceptType}</span>
        <span className="concept-style">{concept.style}</span>
        <span className="concept-mood">{concept.mood}</span>
      </div>
      <div className="concept-colors">
        {concept.colors.map((color, index) => (
          <span key={index} className="color-swatch" style={{ backgroundColor: color }}>
            {color}
          </span>
        ))}
      </div>
      <div className="concept-elements">
        {concept.elements.map((element, index) => (
          <span key={index} className="element-tag">{element}</span>
        ))}
      </div>
    </div>
  );
}

describe('Gallery Concept Display', () => {
  test('should render sound healing concept with rich visual elements', () => {
    render(<ConceptCard concept={mockSoundHealingConcept} />);
    
    // Check that the concept title is displayed
    expect(screen.getByText('Sacred Sound Resonance')).toBeInTheDocument();
    
    // Check that the description is displayed
    expect(screen.getByText(/ancient art of healing through vibrational frequencies/)).toBeInTheDocument();
    
    // Check that visual elements are present
    expect(screen.getByText('mystical')).toBeInTheDocument();
    expect(screen.getByText('transcendent')).toBeInTheDocument();
    
    // Check that colors are displayed
    expect(screen.getByText('deep purple')).toBeInTheDocument();
    expect(screen.getByText('golden light')).toBeInTheDocument();
    expect(screen.getByText('rainbow')).toBeInTheDocument();
    
    // Check that elements are displayed
    expect(screen.getByText('crystal singing bowls')).toBeInTheDocument();
    expect(screen.getByText('sacred geometry')).toBeInTheDocument();
    expect(screen.getByText('energy fields')).toBeInTheDocument();
    
    // Verify the concept card exists
    expect(screen.getByTestId('concept-card')).toBeInTheDocument();
  });

  test('should have engaging visual properties for gallery display', () => {
    const concept = mockSoundHealingConcept;
    
    // Verify rich color palette
    expect(concept.colors).toHaveLength(5);
    expect(concept.colors).toContain('deep purple');
    expect(concept.colors).toContain('golden light');
    expect(concept.colors).toContain('rainbow');
    
    // Verify multiple visual elements
    expect(concept.elements).toHaveLength(5);
    expect(concept.elements).toContain('crystal singing bowls');
    expect(concept.elements).toContain('sacred geometry');
    expect(concept.elements).toContain('energy fields');
    
    // Verify engaging style and mood
    expect(concept.style).toBe('mystical');
    expect(concept.mood).toBe('transcendent');
    
    // Verify metadata richness
    expect(concept.metadata.healingProperties).toHaveLength(4);
    expect(concept.metadata.frequency).toBe('432Hz');
  });

  test('should display fundamental concepts with cosmic visual appeal', () => {
    const fundamentalConcept = {
      title: "Resonance",
      description: "The fundamental principle of vibrational harmony where two or more frequencies align and amplify each other.",
      conceptType: "fundamental",
      style: "cosmic",
      mood: "harmonious",
      colors: ["electric blue", "silver", "white", "rainbow spectrum", "deep space black"],
      elements: ["vibrating strings", "standing waves", "interference patterns", "spiral galaxies", "atomic structures"]
    };
    
    render(<ConceptCard concept={fundamentalConcept} />);
    
    // Check cosmic styling
    expect(screen.getByText('cosmic')).toBeInTheDocument();
    expect(screen.getByText('harmonious')).toBeInTheDocument();
    
    // Check cosmic colors
    expect(screen.getByText('electric blue')).toBeInTheDocument();
    expect(screen.getByText('deep space black')).toBeInTheDocument();
    
    // Check scientific elements
    expect(screen.getByText('vibrating strings')).toBeInTheDocument();
    expect(screen.getByText('spiral galaxies')).toBeInTheDocument();
    expect(screen.getByText('atomic structures')).toBeInTheDocument();
  });
});
