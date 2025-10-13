import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConceptCollaborators } from '../ConceptCollaborators';
import { endpoints } from '@/lib/api';

jest.mock('@/lib/api');

const mockEndpoints = endpoints as jest.Mocked<typeof endpoints>;

describe('ConceptCollaborators', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    jest.clearAllMocks();
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it('renders loading state', () => {
    mockEndpoints.getConceptCollaborators.mockReturnValue(
      new Promise(() => {}) as any
    );

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    expect(screen.getByText('Collaborators')).toBeInTheDocument();
  });

  it('renders collaborators list', async () => {
    mockEndpoints.getConceptCollaborators.mockResolvedValue({
      success: true,
      data: {
        collaborators: [
          {
            userId: 'user.1',
            username: 'Alice',
            relationshipType: 'attuned',
            strength: 1.0,
            createdAt: new Date().toISOString(),
          },
          {
            userId: 'user.2',
            username: 'Bob',
            relationshipType: 'amplified',
            strength: 1.0,
            createdAt: new Date().toISOString(),
          },
        ],
        attuneCount: 1,
        amplifyCount: 1,
      },
    } as any);

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText('Alice')).toBeInTheDocument();
      expect(screen.getByText('Bob')).toBeInTheDocument();
    });
  });

  it('displays attune and amplify counts', async () => {
    mockEndpoints.getConceptCollaborators.mockResolvedValue({
      success: true,
      data: {
        collaborators: [],
        attuneCount: 5,
        amplifyCount: 3,
      },
    } as any);

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/5 attuned/)).toBeInTheDocument();
      expect(screen.getByText(/3 amplified/)).toBeInTheDocument();
    });
  });

  it('shows empty state when no collaborators', async () => {
    mockEndpoints.getConceptCollaborators.mockResolvedValue({
      success: true,
      data: {
        collaborators: [],
        attuneCount: 0,
        amplifyCount: 0,
      },
    } as any);

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/No collaborators yet/)).toBeInTheDocument();
    });
  });

  it('renders error state', async () => {
    mockEndpoints.getConceptCollaborators.mockRejectedValue(new Error('API Error'));

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/Failed to load collaborators/)).toBeInTheDocument();
    });
  });

  it('groups collaborators by user and shows multiple relationships', async () => {
    mockEndpoints.getConceptCollaborators.mockResolvedValue({
      success: true,
      data: {
        collaborators: [
          {
            userId: 'user.1',
            username: 'Alice',
            relationshipType: 'attuned',
            createdAt: new Date().toISOString(),
          },
          {
            userId: 'user.1',
            username: 'Alice',
            relationshipType: 'amplified',
            createdAt: new Date().toISOString(),
          },
        ],
        attuneCount: 1,
        amplifyCount: 1,
      },
    } as any);

    render(<ConceptCollaborators conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      const alices = screen.getAllByText('Alice');
      // Should only show user once, even with multiple relationships
      expect(alices.length).toBe(1);
    });
  });
});

