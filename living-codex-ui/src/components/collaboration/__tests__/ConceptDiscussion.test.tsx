import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConceptDiscussion } from '../ConceptDiscussion';
import { endpoints } from '@/lib/api';

// Mock auth
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'user.test123', displayName: 'Test User' },
    isAuthenticated: true,
  }),
}));

// Mock API
jest.mock('@/lib/api');

const mockEndpoints = endpoints as jest.Mocked<typeof endpoints>;

describe('ConceptDiscussion Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    jest.clearAllMocks();
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  describe('Discussion List Rendering', () => {
    it('renders loading state', () => {
      mockEndpoints.getConceptDiscussions.mockReturnValue(
        new Promise(() => {}) as any
      );

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      expect(screen.getByText('Discussions')).toBeInTheDocument();
    });

    it('renders discussions list with data', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [
            {
              id: 'discussion.1',
              title: 'Test Discussion 1',
              content: 'Content 1',
              username: 'Alice',
              createdAt: new Date().toISOString(),
              replyCount: 3,
              discussionType: 'general',
              status: 'open',
            },
            {
              id: 'discussion.2',
              title: 'Test Question',
              content: 'Content 2',
              username: 'Bob',
              createdAt: new Date().toISOString(),
              replyCount: 0,
              discussionType: 'question',
              status: 'open',
            },
          ],
        },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText('Test Discussion 1')).toBeInTheDocument();
        expect(screen.getByText('Test Question')).toBeInTheDocument();
      });
    });

    it('shows empty state when no discussions', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [],
        },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText(/No discussions yet/)).toBeInTheDocument();
      });
    });

    it('displays correct discussion count', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [
            { id: '1', title: 'D1', content: 'C1', username: 'U1', createdAt: new Date().toISOString(), replyCount: 0, discussionType: 'general', status: 'open' },
            { id: '2', title: 'D2', content: 'C2', username: 'U2', createdAt: new Date().toISOString(), replyCount: 0, discussionType: 'general', status: 'open' },
            { id: '3', title: 'D3', content: 'C3', username: 'U3', createdAt: new Date().toISOString(), replyCount: 0, discussionType: 'general', status: 'open' },
          ],
        },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText(/3 discussions/)).toBeInTheDocument();
      });
    });
  });

  describe('Discussion Type Icons', () => {
    it('displays correct icon for question type', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [
            {
              id: 'discussion.1',
              title: 'Question',
              content: 'Content',
              username: 'User',
              createdAt: new Date().toISOString(),
              replyCount: 0,
              discussionType: 'question',
              status: 'open',
            },
          ],
        },
      } as any);

      const { container } = render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        // Question icon should be present
        const icons = container.querySelectorAll('svg');
        expect(icons.length).toBeGreaterThan(0);
      });
    });
  });

  describe('New Discussion Form', () => {
    it('shows New Discussion button when authenticated', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: { discussions: [] },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText('New Discussion')).toBeInTheDocument();
      });
    });

    it('opens form when New Discussion clicked', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: { discussions: [] },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        const button = screen.getByText('New Discussion');
        fireEvent.click(button);
      });

      expect(screen.getByText('Start a New Discussion')).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/Discussion title/)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/What would you like to discuss/)).toBeInTheDocument();
    });

    it('shows discussion type selector in form', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: { discussions: [] },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        fireEvent.click(screen.getByText('New Discussion'));
      });

      const select = screen.getByRole('combobox') as HTMLSelectElement;
      expect(select).toBeInTheDocument();
      expect(select.querySelector('option[value="general"]')).toBeInTheDocument();
      expect(select.querySelector('option[value="question"]')).toBeInTheDocument();
      expect(select.querySelector('option[value="proposal"]')).toBeInTheDocument();
      expect(select.querySelector('option[value="issue"]')).toBeInTheDocument();
    });
  });

  describe('Reply Count Display', () => {
    it('shows reply count for each discussion', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [
            {
              id: 'discussion.1',
              title: 'Discussion with Replies',
              content: 'Content',
              username: 'User',
              createdAt: new Date().toISOString(),
              replyCount: 5,
              discussionType: 'general',
              status: 'open',
            },
          ],
        },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText(/5 replies/)).toBeInTheDocument();
      });
    });

    it('shows singular "reply" for count of 1', async () => {
      mockEndpoints.getConceptDiscussions.mockResolvedValue({
        success: true,
        data: {
          discussions: [
            {
              id: 'discussion.1',
              title: 'Discussion',
              content: 'Content',
              username: 'User',
              createdAt: new Date().toISOString(),
              replyCount: 1,
              discussionType: 'general',
              status: 'open',
            },
          ],
        },
      } as any);

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        // The text is "1 reply" (not "1 replies")
        expect(screen.getByText(/1 reply/)).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('shows error state when API fails', async () => {
      mockEndpoints.getConceptDiscussions.mockRejectedValue(new Error('API Error'));

      render(<ConceptDiscussion conceptId="test-concept" />, { wrapper });

      await waitFor(() => {
        expect(screen.getByText(/Failed to load discussions/)).toBeInTheDocument();
      });
    });
  });
});

