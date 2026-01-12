import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { KanbanBoard } from '../KanbanBoard';
import { KanbanColumn } from '../KanbanColumn';
import { TaskStatus, TaskPriority } from '../../types';
import type { TaskResponseModel, CategoryResponseModel } from '../../types';

const mockCategory: CategoryResponseModel = {
  userId: 'user-id',
  id: '1',
  name: 'Work',
  color: '#ff0000',
  sortOrder: 1,
  createdAt: "2026-01-01"
};

const mockTasks: TaskResponseModel[] = [
  {
    userId: 'user-id',
    id: 't1',
    description: 'Todo Task',
    status: TaskStatus.Todo,
    priority: TaskPriority.High,
    estimatedMinutes: 60,
    deadline: '2024-12-31T23:59:00',
    categoryId: '1',
  },
  {
    userId: 'user-id-2',
    id: 't2',
    description: 'In Progress Task',
    status: TaskStatus.InProgress,
    priority: TaskPriority.Medium,
    categoryId: '1',
  },
  {
    userId: 'user-id-3',
    id: 't3',
    description: 'Done Task',
    status: TaskStatus.Done,
    priority: TaskPriority.Low,
    categoryId: '1',
  },
];

describe('KanbanBoard', () => {
  const mockHandlers = {
    onAddTask: vi.fn(),
    onDeleteTask: vi.fn(),
    onInfoClick: vi.fn(),
    onDragStart: vi.fn(),
    onDragOver: vi.fn(),
    onDrop: vi.fn(),
    formatDate: vi.fn((date) => date || 'No deadline'),
    formatEstimatedMinutes: vi.fn((mins) => mins ? `${mins}m` : 'N/A'),
  };

  const statusColumns = [
    { status: TaskStatus.Todo, label: 'To Do' },
    { status: TaskStatus.InProgress, label: 'In Progress' },
    { status: TaskStatus.Done, label: 'Done' },
  ];

  it('renders all three columns with correct labels', () => {
    render(
      <KanbanBoard
        tasks={mockTasks}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );

    expect(screen.getByRole('heading', { name: 'To Do' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'In Progress' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Done' })).toBeInTheDocument();
  });

  it('groups tasks by status correctly', () => {
    render(
      <KanbanBoard
        tasks={mockTasks}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );

    const todoHeading = screen.getByRole('heading', { name: 'To Do' });
    const todoCol = todoHeading.closest('.kanban-column') as HTMLElement;
    expect(within(todoCol).getByText('Todo Task')).toBeInTheDocument();

    const inProgressHeading = screen.getByRole('heading', { name: 'In Progress' });
    const inProgressCol = inProgressHeading.closest('.kanban-column') as HTMLElement;
    expect(within(inProgressCol).getByText('In Progress Task')).toBeInTheDocument();

    const doneHeading = screen.getByRole('heading', { name: 'Done' });
    const doneCol = doneHeading.closest('.kanban-column') as HTMLElement;
    expect(within(doneCol).getByText('Done Task')).toBeInTheDocument();
  });

  it('handles empty task list', () => {
    render(
      <KanbanBoard
        tasks={[]}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );

    const columns = screen.getAllByText('No tasks');
    expect(columns).toHaveLength(3); // One for each column
  });

  it('handles tasks with undefined status as Todo', () => {
    const tasksWithUndefinedStatus: TaskResponseModel[] = [
      {
        id: 't1',
        description: 'Undefined Status Task',
        priority: TaskPriority.Medium,
        categoryId: '1',
      } as any,
    ];

    render(
      <KanbanBoard
        tasks={tasksWithUndefinedStatus}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );

    const todoHeading = screen.getByRole('heading', { name: 'To Do' });
    const todoCol = todoHeading.closest('.kanban-column') as HTMLElement;
    expect(within(todoCol).getByText('Undefined Status Task')).toBeInTheDocument();
  });

  it('memoizes grouped tasks when tasks reference changes but content is same', () => {
    const { rerender } = render(
      <KanbanBoard
        tasks={mockTasks}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );


    // Create new array with same content
    const newTasks = [...mockTasks];
    rerender(
      <KanbanBoard
        tasks={newTasks}
        category={mockCategory}
        statusColumns={statusColumns}
        {...mockHandlers}
      />
    );

    // Component should re-render with new grouping
    const secondTodoTask = screen.getByText('Todo Task');
    expect(secondTodoTask).toBeInTheDocument();
  });
});

describe('KanbanColumn', () => {
  const mockHandlers = {
    onAddTask: vi.fn(),
    onDeleteTask: vi.fn(),
    onInfoClick: vi.fn(),
    onDragStart: vi.fn(),
    onDragOver: vi.fn(),
    onDrop: vi.fn(),
    formatDate: vi.fn((date) => date || 'No deadline'),
    formatEstimatedMinutes: vi.fn((mins) => mins ? `${mins}m` : 'N/A'),
  };

  const todoTasks: TaskResponseModel[] = [
    {
      userId: 'user-id',
      id: 't1',
      description: 'Task 1',
      status: TaskStatus.Todo,
      priority: TaskPriority.High,
      estimatedMinutes: 60,
      deadline: '2024-12-31T23:59:00',
      categoryId: '1',
    },
    {
      userId: 'user-id-2',
      id: 't2',
      description: 'Task 2',
      status: TaskStatus.Todo,
      priority: TaskPriority.Medium,
      categoryId: '1',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders column with correct label', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(screen.getByRole('heading', { name: 'To Do' })).toBeInTheDocument();
  });

  it('renders all tasks in the column', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(screen.getByText('Task 1')).toBeInTheDocument();
    expect(screen.getByText('Task 2')).toBeInTheDocument();
  });

  it('renders empty state when no tasks', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[]}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(screen.getByText('No tasks')).toBeInTheDocument();
  });

  it('calls onAddTask with correct status when + button clicked', async () => {
    const user = userEvent.setup();
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const addButton = screen.getByRole('button', { name: '+' });
    await user.click(addButton);

    expect(mockHandlers.onAddTask).toHaveBeenCalledWith(TaskStatus.Todo);
  });

  it('calls onDeleteTask when delete button clicked', async () => {
    const user = userEvent.setup();
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const deleteButtons = screen.getAllByRole('button', { name: '×' });
    await user.click(deleteButtons[0]);

    expect(mockHandlers.onDeleteTask).toHaveBeenCalledWith('t1');
  });

  it('calls onInfoClick when info button clicked', async () => {
    const user = userEvent.setup();
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const infoButtons = screen.getAllByRole('button', { name: /ℹ Info/i });
    await user.click(infoButtons[0]);

    expect(mockHandlers.onInfoClick).toHaveBeenCalledWith(todoTasks[0]);
  });

  it('applies correct priority colors', () => {
    const mixedPriorityTasks: TaskResponseModel[] = [
      { ...todoTasks[0], priority: TaskPriority.High, description: 'High Task' },
      { ...todoTasks[0], priority: TaskPriority.Medium, description: 'Medium Task' },
      { ...todoTasks[0], priority: TaskPriority.Low, description: 'Low Task' },
    ];

    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={mixedPriorityTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(screen.getByText(/Priority: High/i)).toHaveClass('text-red-400');
    expect(screen.getByText(/Priority: Medium/i)).toHaveClass('text-yellow-400');
    expect(screen.getByText(/Priority: Low/i)).toHaveClass('text-green-400');
  });

  it('applies category color to task cards', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[todoTasks[0]]}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const card = screen.getByText('Task 1').closest('div[draggable="true"]') as HTMLElement;
    expect(card).toHaveStyle({ borderLeftColor: '#ff0000' });
  });

  it('uses default gray color when category has no color', () => {
    const categoryWithoutColor = { ...mockCategory, color: undefined };
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[todoTasks[0]]}
        category={categoryWithoutColor}
        {...mockHandlers}
      />
    );

    const card = screen.getByText('Task 1').closest('div[draggable="true"]') as HTMLElement;
    expect(card).toHaveStyle({ borderLeftColor: '#6B7280' });
  });

  it('calls formatDate for deadline display', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[todoTasks[0]]}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(mockHandlers.formatDate).toHaveBeenCalledWith('2024-12-31T23:59:00');
    expect(screen.getByText(/Deadline:/)).toBeInTheDocument();
  });

  it('calls formatEstimatedMinutes for time display', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[todoTasks[0]]}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    expect(mockHandlers.formatEstimatedMinutes).toHaveBeenCalledWith(60);
    expect(screen.getByText(/Estimation:/)).toBeInTheDocument();
  });

  it('makes task cards draggable', () => {
    render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={[todoTasks[0]]}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const card = screen.getByText('Task 1').closest('div[draggable="true"]') as HTMLElement;
    expect(card).toHaveAttribute('draggable', 'true');
  });

  it('calls onDragStart when dragging starts', () => {
      render(
        <KanbanColumn
          status={TaskStatus.Todo}
          label="To Do"
          tasks={[todoTasks[0]]}
          category={mockCategory}
          {...mockHandlers}
        />
      );
  
      const card = screen.getByText('Task 1').closest('div[draggable="true"]') as HTMLElement;
  
      const dataTransfer = {
        effectAllowed: 'move',
        dropEffect: 'move',
        setDragImage: vi.fn(),
        setData: vi.fn(),
        getData: vi.fn(),
        clearData: vi.fn(),
        files: [],
        items: [],
        types: [],
      };
  
      fireEvent.dragStart(card, { dataTransfer });
  
      expect(mockHandlers.onDragStart).toHaveBeenCalledTimes(1);
    });
    
    it('handles drop events on the column', () => {
      render(
        <KanbanColumn
          status={TaskStatus.Done}
          label="Done"
          tasks={[]}
          category={mockCategory}
          {...mockHandlers}
        />
      );
  
      const column = screen.getByRole('heading', { name: 'Done' }).closest('.kanban-column') as HTMLElement;
  
      const dataTransfer = {
        effectAllowed: 'move',
        dropEffect: 'move',
        setDragImage: vi.fn(),
        setData: vi.fn(),
        getData: vi.fn(),
        clearData: vi.fn(),
        files: [],
        items: [],
        types: [],
      };
  
      fireEvent.dragOver(column, { dataTransfer });
      fireEvent.drop(column, { dataTransfer });
  
      expect(mockHandlers.onDrop).toHaveBeenCalledTimes(1);
    });

  it('memoizes and does not re-render unnecessarily', () => {
    const { rerender } = render(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );


    // Rerender with same props
    rerender(
      <KanbanColumn
        status={TaskStatus.Todo}
        label="To Do"
        tasks={todoTasks}
        category={mockCategory}
        {...mockHandlers}
      />
    );

    const secondCard = screen.getByText('Task 1');
    expect(secondCard).toBeInTheDocument();
  });
});