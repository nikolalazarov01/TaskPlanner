import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskPriority, TaskStatus } from '../../types';

// IMPORTANT: hoisted so it's available to hoisted vi.mock factories
const mocks = vi.hoisted(() => ({
  taskApi: {
    getAll: vi.fn(),
    delete: vi.fn(),
    updateStatus: vi.fn(),
    create: vi.fn(),
  },
  categoryApi: {
    getOne: vi.fn(),
  },
  auth: {
    logout: vi.fn(),
  },
  navigate: vi.fn(),
}));

vi.mock('../../services/api', () => ({
  taskApi: mocks.taskApi,
  categoryApi: mocks.categoryApi,
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    logout: mocks.auth.logout,
  }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mocks.navigate,
    useParams: () => ({ categoryId: '1' }),
  };
});

vi.mock('../AddTaskDialog', () => ({
  AddTaskDialog: (props: any) => {
    if (!props.isOpen) return null;
    return (
      <div data-testid="add-task-dialog">
        AddTaskDialog open
        <button onClick={props.onClose}>Close</button>
      </div>
    );
  },
}));

vi.mock('../TaskInfoDialog', () => ({
  TaskInfoDialog: (props: any) => {
    if (!props.isOpen) return null;
    return (
      <div data-testid="task-info-dialog">
        TaskInfoDialog open: {props.task?.description}
        <button onClick={props.onClose}>Close</button>
      </div>
    );
  },
}));

// Import AFTER mocks
import { Tasks } from '../Tasks';
import { createDataTransfer } from '../../test/helpers/dns';

beforeEach(() => {
  vi.clearAllMocks();
});

function getColumn(label: string) {
  const heading = screen.getByRole('heading', { name: label });
  const col = heading.closest('.kanban-column') as HTMLElement | null;
  if (!col) throw new Error(`Could not find .kanban-column for label: ${label}`);
  return col;
}

function getTaskCardInColumn(columnLabel: string, description: string) {
  const col = getColumn(columnLabel);
  const text = within(col).getByText(description);
  return text.closest('div[draggable="true"]') as HTMLElement;
}

describe('Tasks page (Kanban)', () => {
  const mockCategory = { id: '1', name: 'Work', color: '#ff0000' };

  describe('Loading and Initial Render', () => {
    it('shows loading state initially', () => {
      mocks.categoryApi.getOne.mockImplementation(() => new Promise(() => {}));
      mocks.taskApi.getAll.mockImplementation(() => new Promise(() => {}));

      render(<Tasks />);

      expect(screen.getByText('Loading tasks...')).toBeInTheDocument();
    });

    it('loads category and tasks and renders grouped columns', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        { id: 't2', description: 'Task B', status: TaskStatus.InProgress, priority: TaskPriority.High },
        { id: 't3', description: 'Task C', status: TaskStatus.Done, priority: TaskPriority.Low },
      ]);

      render(<Tasks />);

      expect(await screen.findByRole('heading', { name: 'Work' })).toBeInTheDocument();

      expect(screen.getByRole('heading', { name: 'To Do' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'In Progress' })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'Done' })).toBeInTheDocument();

      expect(within(getColumn('To Do')).getByText('Task A')).toBeInTheDocument();
      expect(within(getColumn('In Progress')).getByText('Task B')).toBeInTheDocument();
      expect(within(getColumn('Done')).getByText('Task C')).toBeInTheDocument();

      expect(mocks.categoryApi.getOne).toHaveBeenCalledWith('1');
      expect(mocks.taskApi.getAll).toHaveBeenCalledWith('1');
    });

    it('shows empty state when no tasks exist', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([]);

      render(<Tasks />);

      await screen.findByRole('heading', { name: 'Work' });

      expect(screen.getByText('No tasks in this category')).toBeInTheDocument();
      expect(screen.getByText('Create your first task')).toBeInTheDocument();
    });

    it('displays error message when loading fails', async () => {
      // If tasks fail, the component still renders (category load succeeds)
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockRejectedValue(new Error('Network error'));

      render(<Tasks />);

      await screen.findByRole('heading', { name: 'Work' });
      expect(await screen.findByText('Failed to load tasks')).toBeInTheDocument();
    });

    it('displays tasks with correct priority colors', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'High Priority', status: TaskStatus.Todo, priority: TaskPriority.High },
        { id: 't2', description: 'Medium Priority', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        { id: 't3', description: 'Low Priority', status: TaskStatus.Todo, priority: TaskPriority.Low },
      ]);

      render(<Tasks />);

      await screen.findByText('High Priority');

      const highCard = getTaskCardInColumn('To Do', 'High Priority');
      const mediumCard = getTaskCardInColumn('To Do', 'Medium Priority');
      const lowCard = getTaskCardInColumn('To Do', 'Low Priority');

      expect(within(highCard).getByText(/Priority:\s*High/i)).toHaveClass('text-red-400');
      expect(within(mediumCard).getByText(/Priority:\s*Medium/i)).toHaveClass('text-yellow-400');
      expect(within(lowCard).getByText(/Priority:\s*Low/i)).toHaveClass('text-green-400');
    });
  });

  describe('Drag and Drop', () => {
    beforeEach(() => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
    });

    it('optimistically moves a task on drag & drop and calls updateStatus', async () => {
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
      ]);
      mocks.taskApi.updateStatus.mockResolvedValue({});

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const todoCol = getColumn('To Do');
      const doneCol = getColumn('Done');
      const card = getTaskCardInColumn('To Do', 'Task A');

      const dt = createDataTransfer();
      fireEvent.dragStart(card, { dataTransfer: dt });
      fireEvent.dragOver(doneCol, { dataTransfer: dt });
      fireEvent.drop(doneCol, { dataTransfer: dt });

      expect(within(doneCol).getByText('Task A')).toBeInTheDocument();
      expect(within(todoCol).queryByText('Task A')).not.toBeInTheDocument();

      expect(mocks.taskApi.updateStatus).toHaveBeenCalledWith('t1', TaskStatus.Done);
    });

    it('does not call API when dropping in same column', async () => {
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const todoCol = getColumn('To Do');
      const card = getTaskCardInColumn('To Do', 'Task A');

      const dt = createDataTransfer();
      fireEvent.dragStart(card, { dataTransfer: dt });
      fireEvent.dragOver(todoCol, { dataTransfer: dt });
      fireEvent.drop(todoCol, { dataTransfer: dt });

      expect(mocks.taskApi.updateStatus).not.toHaveBeenCalled();
    });

    it('reverts optimistic update on API error (reloads tasks)', async () => {
      mocks.taskApi.getAll
        .mockResolvedValueOnce([
          { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        ])
        .mockResolvedValueOnce([
          { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        ]);

      mocks.taskApi.updateStatus.mockRejectedValue(new Error('Update failed'));
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      // Use current DOM nodes for the drag start
      const initialDoneCol = getColumn('Done');
      const card = getTaskCardInColumn('To Do', 'Task A');

      const dt = createDataTransfer();
      fireEvent.dragStart(card, { dataTransfer: dt });
      fireEvent.dragOver(initialDoneCol, { dataTransfer: dt });
      fireEvent.drop(initialDoneCol, { dataTransfer: dt });

      // Optimistic move: appears in Done immediately
      expect(within(getColumn('Done')).getByText('Task A')).toBeInTheDocument();

      // After failed API + refresh, re-query the column from the live DOM
      await waitFor(() => {
        const todoColNow = getColumn('To Do');
        expect(within(todoColNow).getByText('Task A')).toBeInTheDocument();
      });

      expect(mocks.taskApi.updateStatus).toHaveBeenCalledWith('t1', TaskStatus.Done);
      expect(mocks.taskApi.getAll).toHaveBeenCalledTimes(2);
    });


    it('moves task between all three columns correctly', async () => {
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
      ]);
      mocks.taskApi.updateStatus.mockResolvedValue({});

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const inProgressCol = getColumn('In Progress');
      const doneCol = getColumn('Done');

      // Todo -> In Progress
      let card = getTaskCardInColumn('To Do', 'Task A');
      let dt = createDataTransfer();
      fireEvent.dragStart(card, { dataTransfer: dt });
      fireEvent.dragOver(inProgressCol, { dataTransfer: dt });
      fireEvent.drop(inProgressCol, { dataTransfer: dt });

      expect(within(inProgressCol).getByText('Task A')).toBeInTheDocument();
      expect(mocks.taskApi.updateStatus).toHaveBeenCalledWith('t1', TaskStatus.InProgress);

      // In Progress -> Done
      card = getTaskCardInColumn('In Progress', 'Task A');
      dt = createDataTransfer();
      fireEvent.dragStart(card, { dataTransfer: dt });
      fireEvent.dragOver(doneCol, { dataTransfer: dt });
      fireEvent.drop(doneCol, { dataTransfer: dt });

      expect(within(doneCol).getByText('Task A')).toBeInTheDocument();
      expect(mocks.taskApi.updateStatus).toHaveBeenCalledWith('t1', TaskStatus.Done);
    });
  });

  describe('Task Actions', () => {
    beforeEach(() => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
      ]);
    });

    it('opens add task dialog when clicking Add Task button', async () => {
      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      await userEvent.click(screen.getByRole('button', { name: 'Add Task' }));
      expect(screen.getByTestId('add-task-dialog')).toBeInTheDocument();
    });

    it('opens add task dialog when clicking column + button', async () => {
      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const doneCol = getColumn('Done');
      await userEvent.click(within(doneCol).getByRole('button', { name: '+' }));

      expect(screen.getByTestId('add-task-dialog')).toBeInTheDocument();
    });

    it('deletes task when clicking delete button', async () => {
      mocks.taskApi.delete.mockResolvedValue({});
      // initial + reload after delete
      mocks.taskApi.getAll
        .mockResolvedValueOnce([
          { id: 't1', description: 'Task A', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        ])
        .mockResolvedValueOnce([]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const card = getTaskCardInColumn('To Do', 'Task A');
      await userEvent.click(within(card).getByRole('button', { name: '×' }));

      expect(mocks.taskApi.delete).toHaveBeenCalledWith('t1');
      expect(mocks.taskApi.getAll).toHaveBeenCalledTimes(2);
    });

    it('shows error when delete fails', async () => {
      mocks.taskApi.delete.mockRejectedValue(new Error('Delete failed'));

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const card = getTaskCardInColumn('To Do', 'Task A');
      await userEvent.click(within(card).getByRole('button', { name: '×' }));

      expect(await screen.findByText('Failed to delete task')).toBeInTheDocument();
    });

    it('opens info dialog when clicking info button', async () => {
      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      const card = getTaskCardInColumn('To Do', 'Task A');
      await userEvent.click(within(card).getByRole('button', { name: /info/i }));

      expect(screen.getByTestId('task-info-dialog')).toBeInTheDocument();
      expect(screen.getByText('TaskInfoDialog open: Task A')).toBeInTheDocument();
    });
  });

  describe('Navigation', () => {
    it('navigates back to categories on back button click', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      await userEvent.click(screen.getByRole('button', { name: /back to categories/i }));
      expect(mocks.navigate).toHaveBeenCalledWith('/categories');
    });

    it('calls logout when clicking logout button', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      await userEvent.click(screen.getByRole('button', { name: 'Logout' }));
      expect(mocks.auth.logout).toHaveBeenCalled();
    });
  });

  describe('Task Display Formatting', () => {
    it('formats deadline label', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        {
          id: 't1',
          description: 'Task A',
          status: TaskStatus.Todo,
          priority: TaskPriority.Medium,
          deadline: '2024-12-31T23:59:00',
        },
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      expect(screen.getByText(/Deadline:/)).toBeInTheDocument();
    });

    it('formats estimated minutes correctly', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        {
          id: 't1',
          description: 'Task A',
          status: TaskStatus.Todo,
          priority: TaskPriority.Medium,
          estimatedMinutes: 90,
        },
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      expect(screen.getByText(/Estimation:\s*1h 30m/)).toBeInTheDocument();
    });

    it('shows N/A for missing estimated minutes', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        {
          id: 't1',
          description: 'Task A',
          status: TaskStatus.Todo,
          priority: TaskPriority.Medium,
        },
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      expect(screen.getByText(/Estimation:\s*N\/A/)).toBeInTheDocument();
    });
  });

  describe('Multiple Tasks', () => {
    it('displays multiple tasks in each column', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'Todo 1', status: TaskStatus.Todo, priority: TaskPriority.Medium },
        { id: 't2', description: 'Todo 2', status: TaskStatus.Todo, priority: TaskPriority.High },
        { id: 't3', description: 'Progress 1', status: TaskStatus.InProgress, priority: TaskPriority.Low },
        { id: 't4', description: 'Done 1', status: TaskStatus.Done, priority: TaskPriority.Medium },
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      expect(within(getColumn('To Do')).getByText('Todo 1')).toBeInTheDocument();
      expect(within(getColumn('To Do')).getByText('Todo 2')).toBeInTheDocument();
      expect(within(getColumn('In Progress')).getByText('Progress 1')).toBeInTheDocument();
      expect(within(getColumn('Done')).getByText('Done 1')).toBeInTheDocument();
    });

    it('handles tasks with undefined status as Todo', async () => {
      mocks.categoryApi.getOne.mockResolvedValue(mockCategory);
      mocks.taskApi.getAll.mockResolvedValue([
        { id: 't1', description: 'No Status', priority: TaskPriority.Medium } as any,
      ]);

      render(<Tasks />);
      await screen.findByRole('heading', { name: 'Work' });

      expect(within(getColumn('To Do')).getByText('No Status')).toBeInTheDocument();
    });
  });
});
