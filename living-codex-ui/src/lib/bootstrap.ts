// Bootstrap script to seed the server with initial UI atoms and test data
import { AtomFetcher, defaultAtoms } from './atoms';

const atomFetcher = new AtomFetcher();

export async function bootstrapUI() {
  console.log('🌱 Bootstrapping UI atoms...');

  try {
    // Create UI module
    await atomFetcher.createAtom(defaultAtoms.module);
    console.log('✅ Created UI module');

    // Create pages
    for (const page of defaultAtoms.pages) {
      await atomFetcher.createAtom(page);
    }
    console.log('✅ Created UI pages');

    // Create lenses
    for (const lens of defaultAtoms.lenses) {
      await atomFetcher.createAtom(lens);
    }
    console.log('✅ Created UI lenses');

    // Create actions
    for (const action of defaultAtoms.actions) {
      await atomFetcher.createAtom(action);
    }
    console.log('✅ Created UI actions');

    // Create controls
    await atomFetcher.createAtom(defaultAtoms.controls);
    console.log('✅ Created UI controls');

    // Create some test concepts
    const testConcepts = [
      {
        id: 'concept-quantum-resonance',
        name: 'Quantum Resonance',
        description: 'The fundamental principle that all matter vibrates at specific frequencies, creating resonance fields that can be amplified and harmonized.',
        axes: ['resonance', 'science', 'consciousness'],
        resonance: 0.95,
        type: 'concept',
        meta: {
          category: 'physics',
          complexity: 'high',
          created: new Date().toISOString()
        }
      },
      {
        id: 'concept-fractal-consciousness',
        name: 'Fractal Consciousness',
        description: 'The idea that consciousness exhibits fractal patterns at every scale, from individual thoughts to collective awareness.',
        axes: ['consciousness', 'unity', 'innovation'],
        resonance: 0.88,
        type: 'concept',
        meta: {
          category: 'philosophy',
          complexity: 'medium',
          created: new Date().toISOString()
        }
      },
      {
        id: 'concept-abundance-mindset',
        name: 'Abundance Mindset',
        description: 'A way of thinking that focuses on limitless possibilities and collaborative growth rather than scarcity and competition.',
        axes: ['abundance', 'unity', 'impact'],
        resonance: 0.92,
        type: 'concept',
        meta: {
          category: 'psychology',
          complexity: 'low',
          created: new Date().toISOString()
        }
      }
    ];

    for (const concept of testConcepts) {
      await atomFetcher.createAtom({
        type: 'codex.concept',
        id: concept.id,
        name: concept.name,
        description: concept.description,
        axes: concept.axes,
        resonance: concept.resonance,
        meta: concept.meta
      });
    }
    console.log('✅ Created test concepts');

    // Create some test users
    const testUsers = [
      {
        id: 'user-alex-resonance',
        name: 'Alex Resonance',
        username: 'alex_resonance',
        bio: 'Exploring the intersection of quantum physics and consciousness',
        interests: ['resonance', 'science', 'consciousness'],
        location: 'San Francisco, CA',
        meta: {
          joined: new Date().toISOString(),
          contributions: 42
        }
      },
      {
        id: 'user-maya-fractal',
        name: 'Maya Fractal',
        username: 'maya_fractal',
        bio: 'Artist and researcher studying fractal patterns in nature and mind',
        interests: ['consciousness', 'unity', 'innovation'],
        location: 'Barcelona, Spain',
        meta: {
          joined: new Date().toISOString(),
          contributions: 28
        }
      }
    ];

    for (const user of testUsers) {
      await atomFetcher.createAtom({
        type: 'codex.user',
        id: user.id,
        name: user.name,
        username: user.username,
        bio: user.bio,
        interests: user.interests,
        location: user.location,
        meta: user.meta
      });
    }
    console.log('✅ Created test users');

    console.log('🎉 Bootstrap complete! UI is ready.');
    return true;

  } catch (error) {
    console.error('❌ Bootstrap failed:', error);
    return false;
  }
}

// Auto-bootstrap when this module is imported
if (typeof window !== 'undefined') {
  bootstrapUI();
}
