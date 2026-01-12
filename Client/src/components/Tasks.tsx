// Tasks.tsx (refactored)
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { taskApi, categoryApi } from '../services/api';
import type { TaskResponseModel, CategoryResponseModel } from '../types';
import { TaskStatus } from '../types';
import { AddTaskDialog } from './AddTaskDialog';
import { TaskInfoDialog } from './TaskInfoDialog';
import { KanbanBoard, type StatusColumnDef } from './KanbanBoard';

export const Tasks: React.FC = () => {
  const { categoryId } = useParams<{ categoryId: string }>();
  const [tasks, setTasks] = useState<TaskResponseModel[]>([]);
  const [category, setCategory] = useState<CategoryResponseModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [initialTaskStatus, setInitialTaskStatus] = useState<TaskStatus | undefined>(undefined);
  const [selectedTask, setSelectedTask] = useState<TaskResponseModel | null>(null);
  const [isInfoDialogOpen, setIsInfoDialogOpen] = useState(false);
  const [draggedTask, setDraggedTask] = useState<TaskResponseModel | null>(null);

  const { logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (categoryId) {
      void loadCategory(categoryId);
      void loadTasks(categoryId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryId]);

  const loadCategory = async (cid: string) => {
    try {
      const data = await categoryApi.getOne(cid);
      setCategory(data);
    } catch (err) {
      setError('Failed to load category');
      console.error(err);
    }
  };

  const loadTasks = async (cid: string) => {
    try {
      setLoading(true);
      const data = await taskApi.getAll(cid);
      setTasks(data);
      setError('');
    } catch (err) {
      setError('Failed to load tasks');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const refreshTasks = useCallback(async () => {
    if (!categoryId) return;
    await loadTasks(categoryId);
  }, [categoryId]);

  const formatDate = useCallback((dateString?: string): string => {
    if (!dateString) return 'No deadline';
    const date = new Date(dateString);
    return (
      date.toLocaleDateString() +
      ' ' +
      date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    );
  }, []);

  const formatEstimatedMinutes = useCallback((minutes?: number): string => {
    if (!minutes) return 'N/A';
    if (minutes < 60) return `${minutes}m`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }, []);

  const handleDeleteTask = useCallback(
    async (id: string) => {
      try {
        await taskApi.delete(id);
        await refreshTasks();
      } catch (err) {
        setError('Failed to delete task');
        console.error(err);
      }
    },
    [refreshTasks]
  );

  const handleDragStart = useCallback((e: React.DragEvent, task: TaskResponseModel) => {
    setDraggedTask(task);
    e.dataTransfer.effectAllowed = 'move';

    // Custom drag image
    const dragElement = e.currentTarget as HTMLElement;
    const dragImage = dragElement.cloneNode(true) as HTMLElement;
    dragImage.style.opacity = '0.95';
    dragImage.style.transform = 'rotate(2deg)';
    dragImage.style.position = 'absolute';
    dragImage.style.top = '-1000px';
    document.body.appendChild(dragImage);

    const rect = dragElement.getBoundingClientRect();
    const offsetX = e.clientX - rect.left;
    const offsetY = e.clientY - rect.top;

    e.dataTransfer.setDragImage(dragImage, offsetX, offsetY);

    setTimeout(() => {
      document.body.removeChild(dragImage);
    }, 0);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }, []);

  // IMPORTANT: optimistic update so only the KanbanBoard subtree re-renders (not loading the whole page)
  const handleDrop = useCallback(
    async (e: React.DragEvent, targetStatus: TaskStatus) => {
      e.preventDefault();
      if (!draggedTask) return;

      const currentStatus = draggedTask.status || TaskStatus.Todo;
      if (currentStatus === targetStatus) {
        setDraggedTask(null);
        return;
      }

      const draggedId = draggedTask.id;

      // Optimistic update: update local state immediately
      setTasks((prev) =>
        prev.map((t) => (t.id === draggedId ? { ...t, status: targetStatus } : t))
      );
      setDraggedTask(null);

      try {
        await taskApi.updateStatus(draggedId, targetStatus);
        // Optional: reconcile with server without a global loading spinner
        // await refreshTasks();
      } catch (err) {
        setError('Failed to update task status');
        console.error(err);
        // Revert (best-effort) by reloading from server
        await refreshTasks();
      }
    },
    [draggedTask, refreshTasks]
  );

  const handleInfoClick = useCallback((task: TaskResponseModel) => {
    setSelectedTask(task);
    setIsInfoDialogOpen(true);
  }, []);

  const handleAddTask = useCallback((status?: TaskStatus) => {
    setInitialTaskStatus(status);
    setIsDialogOpen(true);
  }, []);

  const statusColumns: StatusColumnDef[] = useMemo(
    () => [
      { status: TaskStatus.Todo, label: 'To Do' },
      { status: TaskStatus.InProgress, label: 'In Progress' },
      { status: TaskStatus.Done, label: 'Done' },
    ],
    []
  );

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <div className="text-white text-xl">Loading tasks...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900">
      <header className="bg-gray-800 border-b border-gray-700">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <div className="flex items-center gap-4">
            <button onClick={() => navigate('/categories')} className="text-gray-400 hover:text-white">
              ← Back to Categories
            </button>
            <h1 className="text-2xl font-bold text-white">{category?.name || 'Tasks'}</h1>
          </div>
          <button
            onClick={logout}
            className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
          >
            Logout
          </button>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && (
          <div className="mb-4 p-3 bg-red-900/50 border border-red-700 rounded text-red-200">
            {error}
          </div>
        )}

        <div className="mb-6">
          <button
            onClick={() => handleAddTask(undefined)}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
          >
            Add Task
          </button>
        </div>

        {tasks.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-gray-400 text-lg mb-4">No tasks in this category</p>
            <button
              onClick={() => handleAddTask(undefined)}
              className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
            >
              Create your first task
            </button>
          </div>
        ) : (
          <KanbanBoard
            tasks={tasks}
            category={category}
            statusColumns={statusColumns}
            onAddTask={(s) => handleAddTask(s)}
            onDeleteTask={handleDeleteTask}
            onInfoClick={handleInfoClick}
            onDragStart={handleDragStart}
            onDragOver={handleDragOver}
            onDrop={handleDrop}
            formatDate={formatDate}
            formatEstimatedMinutes={formatEstimatedMinutes}
          />
        )}
      </main>

      {categoryId && (
        <AddTaskDialog
          isOpen={isDialogOpen}
          onClose={() => {
            setIsDialogOpen(false);
            setInitialTaskStatus(undefined);
          }}
          categoryId={categoryId}
          onSuccess={refreshTasks}
          onError={(errorMessage) => setError(errorMessage)}
          initialStatus={initialTaskStatus}
        />
      )}

      <TaskInfoDialog
        isOpen={isInfoDialogOpen}
        onClose={() => {
          setIsInfoDialogOpen(false);
          setSelectedTask(null);
        }}
        task={selectedTask}
      />
    </div>
  );
};
