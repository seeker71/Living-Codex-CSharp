// Atom types based on LIVING_UI_SPEC.md
export interface UIAtom {
  type: string;
  id: string;
  name?: string;
  [key: string]: any;
}

export interface UIModule extends UIAtom {
  type: 'codex.ui.module';
  routes: string[];
}

export interface UIPage extends UIAtom {
  type: 'codex.ui.page';
  path: string;
  title: string;
  lenses: string[];
  controls: string[];
  status: string;
}

export interface UILens extends UIAtom {
  type: 'codex.ui.lens';
  name: string;
  projection: string;
  itemComponent: string;
  adapters: Record<string, { method: string; path: string }>;
  actions: string[];
  ranking?: string;
  status: string;
}

export interface UIAction extends UIAtom {
  type: 'codex.ui.action';
  label: string;
  effect: {
    method: string;
    path: string;
    bodyTemplate: Record<string, any>;
  };
  undo?: {
    method: string;
    path: string;
    bodyTemplate: Record<string, any>;
  };
}

export interface UIControls extends UIAtom {
  type: 'codex.ui.controls';
  fields: Array<{
    id: string;
    type: string;
    options?: string[];
    min?: number;
    max?: number;
  }>;
  urlBinding: string;
}

// API client for fetching atoms from the server
export class AtomFetcher {
  private baseUrl: string;

  constructor(baseUrl: string = 'http://localhost:5002') {
    this.baseUrl = baseUrl;
  }

  async fetchAtoms<T extends UIAtom>(type: string): Promise<T[]> {
    try {
      const response = await fetch(`${this.baseUrl}/storage-endpoints/nodes?type=${type}`);
      if (!response.ok) throw new Error(`Failed to fetch ${type} atoms`);
      const data = await response.json();
      return data.nodes || [];
    } catch (error) {
      console.error(`Error fetching ${type} atoms:`, error);
      return [];
    }
  }

  async fetchAtom<T extends UIAtom>(id: string): Promise<T | null> {
    try {
      const response = await fetch(`${this.baseUrl}/storage-endpoints/nodes/${id}`);
      if (!response.ok) return null;
      const data = await response.json();
      return data;
    } catch (error) {
      console.error(`Error fetching atom ${id}:`, error);
      return null;
    }
  }

  async createAtom<T extends UIAtom>(atom: T): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/storage-endpoints/nodes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(atom),
      });
      return response.ok;
    } catch (error) {
      console.error('Error creating atom:', error);
      return false;
    }
  }

  async updateAtom<T extends UIAtom>(id: string, atom: Partial<T>): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/storage-endpoints/nodes/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(atom),
      });
      return response.ok;
    } catch (error) {
      console.error('Error updating atom:', error);
      return false;
    }
  }
}

// Adapter for making API calls based on lens configurations
export class APIAdapter {
  private baseUrl: string;

  constructor(baseUrl: string = 'http://localhost:5002') {
    this.baseUrl = baseUrl;
  }

  async call(adapter: { method: string; path: string }, params: Record<string, any> = {}) {
    const url = `${this.baseUrl}${adapter.path}`;
    const options: RequestInit = {
      method: adapter.method,
      headers: { 'Content-Type': 'application/json' },
    };

    if (adapter.method !== 'GET' && Object.keys(params).length > 0) {
      options.body = JSON.stringify(params);
    }

    try {
      const response = await fetch(url, options);
      if (!response.ok) throw new Error(`API call failed: ${response.statusText}`);
      return await response.json();
    } catch (error) {
      console.error('API adapter error:', error);
      throw error;
    }
  }
}

// Default atoms for initial bootstrap
export const defaultAtoms = {
  module: {
    type: 'codex.ui.module',
    id: 'ui.module.core',
    name: 'Core UI Module',
    routes: ['/', '/discover', '/resonance', '/news', '/ontology', '/people', '/portals', '/create', '/about', '/graph', '/node/[id]', '/u/[id]']
  } as UIModule,

  pages: [
    {
      type: 'codex.ui.page',
      id: 'ui.page.home',
      path: '/',
      title: 'Home',
      lenses: ['lens.stream'],
      controls: ['controls.resonance'],
      status: 'Untested'
    },
    {
      type: 'codex.ui.page',
      id: 'ui.page.discover',
      path: '/discover',
      title: 'Discover',
      lenses: ['lens.stream', 'lens.threads', 'lens.gallery', 'lens.nearby', 'lens.swipe'],
      controls: ['controls.resonance'],
      status: 'Untested'
    }
  ] as UIPage[],

  lenses: [
    {
      type: 'codex.ui.lens',
      id: 'lens.stream',
      name: 'Stream Lens',
      projection: 'list',
      itemComponent: 'ConceptStreamCard',
      adapters: {
        list: { method: 'POST', path: '/concept/discover' },
        people: { method: 'POST', path: '/users/discover' }
      },
      actions: ['action.attune', 'action.amplify', 'action.reflect', 'action.weave'],
      ranking: 'resonance*joy*recency',
      status: 'Simple'
    },
    {
      type: 'codex.ui.lens',
      id: 'lens.gallery',
      name: 'Gallery Lens',
      projection: 'masonry',
      itemComponent: 'ConceptGalleryCard',
      adapters: {
        media: { method: 'GET', path: '/image/generations' },
        concepts: { method: 'GET', path: '/image/concepts' }
      },
      actions: ['action.attune', 'action.amplify'],
      status: 'Simple'
    }
  ] as UILens[],

  actions: [
    {
      type: 'codex.ui.action',
      id: 'action.attune',
      label: 'Attune',
      effect: {
        method: 'POST',
        path: '/concept/user/link',
        bodyTemplate: { userId: '${session.userId}', conceptId: '${item.id}', relation: 'attuned' }
      },
      undo: {
        method: 'POST',
        path: '/concept/user/unlink',
        bodyTemplate: { userId: '${session.userId}', conceptId: '${item.id}' }
      }
    }
  ] as UIAction[],

  controls: {
    type: 'codex.ui.controls',
    id: 'controls.resonance',
    fields: [
      { id: 'axes', type: 'multi', options: ['abundance', 'unity', 'resonance', 'innovation', 'science', 'consciousness', 'impact'] },
      { id: 'joy', type: 'range', min: 0, max: 1 },
      { id: 'serendipity', type: 'range', min: 0, max: 1 }
    ],
    urlBinding: 'querystring'
  } as UIControls
};
