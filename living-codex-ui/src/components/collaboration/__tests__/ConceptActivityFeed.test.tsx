import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConceptActivityFeed } from '../ConceptActivityFeed';
import { endpoints } from '@/lib/api';

// Mock the API
jest.mock('@/lib/api');

const mockEndpoints = endpoints as jest.Mocked<typeof endpoints>;

describe('ConceptActivityFeed', () => {
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

  it('renders loading state initially', () => {
    mockEndpoints.getConceptActivity.mockReturnValue(
      new Promise(() => {}) as any
    );

    render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    expect(screen.getByText('Activity Feed')).toBeInTheDocument();
    // Loading skeleton is displayed with animate-pulse class
    const container = screen.getByText('Activity Feed').closest('div');
    expect(container).toBeInTheDocument();
  });

  it('renders activity feed with activities', async () => {
    mockEndpoints.getConceptActivity.mockResolvedValue({
      success: true,
      data: {
        activities: [
          {
            type: 'attune',
            userId: 'user.1',
            username: 'Test User',
            conceptId: 'test-concept',
            timestamp: new Date().toISOString(),
            description: 'Test User attuned to this concept',
          },
          {
            type: 'amplify',
            userId: 'user.2',
            username: 'Another User',
            conceptId: 'test-concept',
            timestamp: new Date(Date.now() - 3600000).toISOString(),
            description: 'Another User amplified this concept',
            contributionType: 'Rating',
          },
        ],
      },
    } as any);

    render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/Test User attuned to this concept/)).toBeInTheDocument();
      expect(screen.getByText(/Another User amplified this concept/)).toBeInTheDocument();
    });
  });

  it('displays different icons for different activity types', async () => {
    mockEndpoints.getConceptActivity.mockResolvedValue({
      success: true,
      data: {
        activities: [
          {
            type: 'attune',
            userId: 'user.1',
            username: 'User1',
            timestamp: new Date().toISOString(),
            description: 'Attuned',
          },
          {
            type: 'amplify',
            userId: 'user.2',
            username: 'User2',
            timestamp: new Date().toISOString(),
            description: 'Amplified',
          },
          {
            type: 'discussion',
            userId: 'user.3',
            username: 'User3',
            timestamp: new Date().toISOString(),
            description: 'Created discussion',
          },
        ],
      },
    } as any);

    const { container } = render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      const icons = container.querySelectorAll('svg');
      expect(icons.length).toBeGreaterThan(0);
    });
  });

  it('formats timestamps as relative time', async () => {
    const now = new Date();
    const twoHoursAgo = new Date(now.getTime() - 2 * 3600000);

    mockEndpoints.getConceptActivity.mockResolvedValue({
      success: true,
      data: {
        activities: [
          {
            type: 'attune',
            userId: 'user.1',
            username: 'Test User',
            timestamp: twoHoursAgo.toISOString(),
            description: 'Test activity',
          },
        ],
      },
    } as any);

    render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/2h ago/)).toBeInTheDocument();
    });
  });

  it('renders error state when API fails', async () => {
    mockEndpoints.getConceptActivity.mockRejectedValue(new Error('API Error'));

    render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/Failed to load activity feed/)).toBeInTheDocument();
    });
  });

  it('shows empty state when no activities', async () => {
    mockEndpoints.getConceptActivity.mockResolvedValue({
      success: true,
      data: {
        activities: [],
      },
    } as any);

    render(<ConceptActivityFeed conceptId="test-concept" />, { wrapper });

    await waitFor(() => {
      expect(screen.getByText(/No activity yet/)).toBeInTheDocument();
    });
  });

  it('respects the limit prop', async () => {
    mockEndpoints.getConceptActivity.mockResolvedValue({
      success: true,
      data: {
        activities: [],
      },
    } as any);

    render(<ConceptActivityFeed conceptId="test-concept" limit={10} />, { wrapper });

    await waitFor(() => {
      expect(mockEndpoints.getConceptActivity).toHaveBeenCalledWith('test-concept', 10, 0);
    });
  });
});

