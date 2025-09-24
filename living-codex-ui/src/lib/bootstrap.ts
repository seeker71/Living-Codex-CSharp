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

    // Note: No hard-coded test concepts or users are created.

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
