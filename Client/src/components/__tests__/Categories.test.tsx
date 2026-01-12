import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

import { Categories } from '../Categories';

// ----- Hoisted mocks (safe with vi.mock hoisting) -----
const mocks = vi.hoisted(() => ({
  categoryApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    delete: vi.fn(),
  },
  auth: {
    logout: vi.fn(),
  },
  navigate: vi.fn(),
}));

vi.mock('../../services/api', () => ({
  categoryApi: mocks.categoryApi,
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    logout: mocks.auth.logout,
  }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<any>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mocks.navigate,
  };
});

function renderPage() {
  return render(
    <MemoryRouter>
      <Categories />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Categories page', () => {
  it('loads categories and renders grid', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([
      { id: 'c1', name: 'Work', color: '#ff0000', sortOrder: 1 },
      { id: 'c2', name: 'Home', color: '#00ff00', sortOrder: 2 },
    ]);

    renderPage();

    expect(await screen.findByText('Work')).toBeInTheDocument();
    expect(screen.getByText('Home')).toBeInTheDocument();

    expect(mocks.categoryApi.getAll).toHaveBeenCalledTimes(1);
  });

  it('shows empty state when no categories', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('No categories yet')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create your first category/i })).toBeInTheDocument();
  });

  it('opens and closes "Add Category" dialog', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([{ id: 'c1', name: 'Work', color: '#ff0000' }]);

    renderPage();
    await screen.findByText('Work');

    await userEvent.click(screen.getByRole('button', { name: /add a category/i }));
    expect(screen.getByRole('heading', { name: /add category/i })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /cancel/i }));
    expect(screen.queryByRole('heading', { name: /add category/i })).not.toBeInTheDocument();
  });

  it('creates a category and refreshes list', async () => {
    // initial list -> after create -> refreshed list
    mocks.categoryApi.getAll
      .mockResolvedValueOnce([{ id: 'c1', name: 'Work', color: '#ff0000' }])
      .mockResolvedValueOnce([
        { id: 'c1', name: 'Work', color: '#ff0000' },
        { id: 'c2', name: 'New Cat', color: '#3B82F6' },
      ]);

    mocks.categoryApi.create.mockResolvedValue({});

    renderPage();
    await screen.findByText('Work');

    await userEvent.click(screen.getByRole('button', { name: /add a category/i }));
    expect(screen.getByRole('heading', { name: /add category/i })).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText(/name/i), 'New Cat');

    // default color is set in component state (#3B82F6)
    await userEvent.click(screen.getByRole('button', { name: /^create$/i }));

    expect(mocks.categoryApi.create).toHaveBeenCalledWith({
      name: 'New Cat',
      color: '#3B82F6',
    });

    // after refresh, new category appears
    expect(await screen.findByText('New Cat')).toBeInTheDocument();
    expect(mocks.categoryApi.getAll).toHaveBeenCalledTimes(2);
  });

  it('deletes a category and refreshes list', async () => {
    mocks.categoryApi.getAll
      .mockResolvedValueOnce([
        { id: 'c1', name: 'Work', color: '#ff0000' },
        { id: 'c2', name: 'Home', color: '#00ff00' },
      ])
      .mockResolvedValueOnce([{ id: 'c2', name: 'Home', color: '#00ff00' }]);

    mocks.categoryApi.delete.mockResolvedValue({});

    renderPage();
    await screen.findByText('Work');

    // Find the card by heading text and delete inside it (aria-label "Delete category")
    const workTitle = screen.getByText('Work');
    const workCard = workTitle.closest('div') as HTMLElement;
    const deleteBtn = within(workCard).getByRole('button', { name: /delete category/i });

    await userEvent.click(deleteBtn);

    expect(mocks.categoryApi.delete).toHaveBeenCalledWith('c1');
    expect(await screen.findByText('Home')).toBeInTheDocument();
    expect(screen.queryByText('Work')).not.toBeInTheDocument();
    expect(mocks.categoryApi.getAll).toHaveBeenCalledTimes(2);
  });

  it('navigates to tasks page when category card is clicked', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([{ id: 'c1', name: 'Work', color: '#ff0000' }]);

    renderPage();
    const work = await screen.findByText('Work');

    // click on the card container
    await userEvent.click(work);

    expect(mocks.navigate).toHaveBeenCalledWith('/categories/c1/tasks');
  });

  it('does not navigate when delete button is clicked (stopPropagation)', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([{ id: 'c1', name: 'Work', color: '#ff0000' }]);
    mocks.categoryApi.delete.mockResolvedValue({});
    mocks.categoryApi.getAll.mockResolvedValueOnce([{ id: 'c1', name: 'Work', color: '#ff0000' }]).mockResolvedValueOnce([]);

    renderPage();
    await screen.findByText('Work');

    const workTitle = screen.getByText('Work');
    const workCard = workTitle.closest('div') as HTMLElement;
    const deleteBtn = within(workCard).getByRole('button', { name: /delete category/i });

    await userEvent.click(deleteBtn);

    expect(mocks.navigate).not.toHaveBeenCalled();
  });

  it('paginates with "Show more" (12 per page)', async () => {
    const many = Array.from({ length: 13 }).map((_, i) => ({
      id: `c${i + 1}`,
      name: `Cat ${i + 1}`,
      color: '#123456',
    }));
    mocks.categoryApi.getAll.mockResolvedValue(many);

    renderPage();

    // first page renders 12
    expect(await screen.findByText('Cat 1')).toBeInTheDocument();
    expect(screen.queryByText('Cat 12')).toBeInTheDocument();
    expect(screen.queryByText('Cat 13')).not.toBeInTheDocument();

    // show more renders remaining
    await userEvent.click(screen.getByRole('button', { name: /show more/i }));
    expect(await screen.findByText('Cat 13')).toBeInTheDocument();
  });

  it('shows error when load fails', async () => {
    mocks.categoryApi.getAll.mockRejectedValue(new Error('boom'));

    renderPage();

    expect(await screen.findByText('Failed to load categories')).toBeInTheDocument();
  });

  it('shows error when create fails', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([]);
    mocks.categoryApi.create.mockRejectedValue(new Error('boom'));

    renderPage();
    await screen.findByText('No categories yet');

    await userEvent.click(screen.getByRole('button', { name: /create your first category/i }));
    await userEvent.type(screen.getByLabelText(/name/i), 'New Cat');

    await userEvent.click(screen.getByRole('button', { name: /^create$/i }));

    expect(await screen.findByText('Failed to create category')).toBeInTheDocument();
  });

  it('logout calls auth.logout', async () => {
    mocks.categoryApi.getAll.mockResolvedValue([]);

    renderPage();
    await screen.findByText('No categories yet');

    await userEvent.click(screen.getByRole('button', { name: /logout/i }));
    expect(mocks.auth.logout).toHaveBeenCalledTimes(1);
  });
});
