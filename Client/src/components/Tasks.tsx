import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { taskApi, categoryApi } from '../services/api';
import type { TaskResponseModel, CategoryResponseModel } from '../types';
import { TaskPriority, TaskStatus } from '../types';
import { AddTaskDialog } from './AddTaskDialog';
import { TaskInfoDialog } from './TaskInfoDialog';

export const Tasks: React.FC = () => {
  const { categoryId } = useParams<{ categoryId: string }>();
  const [tasks, setTasks] = useState<TaskResponseModel[]>([]);
  const [category, setCategory] = useState<CategoryResponseModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<TaskResponseModel | null>(null);
  const [isInfoDialogOpen, setIsInfoDialogOpen] = useState(false);
  const [draggedTask, setDraggedTask] = useState<TaskResponseModel | null>(null);
  const { logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (categoryId) {
      loadCategory();
      loadTasks();
    }
  }, [categoryId]);

  const loadCategory = async () => {
    if (!categoryId) return;
    try {
      const data = await categoryApi.getOne(categoryId);
      setCategory(data);
    } catch (err) {
      setError('Failed to load category');
      console.error(err);
    }
  };

  const loadTasks = async () => {
    if (!categoryId) return;
    try {
      setLoading(true);
      const data = await taskApi.getAll(categoryId);
      setTasks(data);
      setError('');
    } catch (err) {
      setError('Failed to load tasks');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString?: string): string => {
    if (!dateString) return 'No deadline';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const formatEstimatedMinutes = (minutes?: number): string => {
    if (!minutes) return 'N/A';
    if (minutes < 60) return `${minutes}m`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  };

  const getPriorityColor = (priority?: TaskPriority): string => {
    switch (priority) {
      case TaskPriority.High:
        return 'text-red-400';
      case TaskPriority.Medium:
        return 'text-yellow-400';
      case TaskPriority.Low:
        return 'text-green-400';
      default:
        return 'text-gray-400';
    }
  };

  const getCardStyle = (color?: string) => {
    const bgColor = color || '#6B7280';
    return {
      backgroundColor: bgColor + '20',
      borderLeftColor: bgColor,
      borderLeftWidth: '4px',
    };
  };

  const getTasksByStatus = (status: TaskStatus): TaskResponseModel[] => {
    return tasks.filter(task => (task.status || TaskStatus.Todo) === status);
  };

  const handleDeleteTask = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await taskApi.delete(id);
      await loadTasks();
    } catch (err) {
      setError('Failed to delete task');
      console.error(err);
    }
  };

  const handleDragStart = (e: React.DragEvent, task: TaskResponseModel) => {
    setDraggedTask(task);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = async (e: React.DragEvent, targetStatus: TaskStatus) => {
    e.preventDefault();
    if (!draggedTask) return;

    const currentStatus = draggedTask.status || TaskStatus.Todo;
    if (currentStatus === targetStatus) {
      setDraggedTask(null);
      return;
    }

    try {
      await taskApi.updateStatus(draggedTask.id, targetStatus);
      await loadTasks();
    } catch (err) {
      setError('Failed to update task status');
      console.error(err);
    } finally {
      setDraggedTask(null);
    }
  };

  const handleInfoClick = (task: TaskResponseModel, e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedTask(task);
    setIsInfoDialogOpen(true);
  };

  const statusColumns: { status: TaskStatus; label: string }[] = [
    { status: TaskStatus.Todo, label: 'To Do' },
    { status: TaskStatus.InProgress, label: 'In Progress' },
    { status: TaskStatus.Done, label: 'Done' },
  ];

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <div className="text-white text-xl">Loading tasks...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900">
      {/* Header */}
      <header className="bg-gray-800 border-b border-gray-700">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <div className="flex items-center gap-4">
            <button
              onClick={() => navigate('/categories')}
              className="text-gray-400 hover:text-white"
            >
              ← Back to Categories
            </button>
            <h1 className="text-2xl font-bold text-white">
              {category?.name || 'Tasks'}
            </h1>
          </div>
          <button
            onClick={logout}
            className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
          >
            Logout
          </button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && (
          <div className="mb-4 p-3 bg-red-900/50 border border-red-700 rounded text-red-200">
            {error}
          </div>
        )}

        {/* Controls */}
        <div className="mb-6">
          <button
            onClick={() => setIsDialogOpen(true)}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
          >
            Add Task
          </button>
        </div>

        {/* Kanban Board */}
        {tasks.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-gray-400 text-lg mb-4">No tasks in this category</p>
            <button
              onClick={() => setIsDialogOpen(true)}
              className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
            >
              Create your first task
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {statusColumns.map(({ status, label }) => {
              const statusTasks = getTasksByStatus(status);
              return (
                <div
                  key={status}
                  className="bg-gray-800 rounded-lg p-4 min-h-[400px]"
                  onDragOver={handleDragOver}
                  onDrop={(e) => handleDrop(e, status)}
                >
                  <h2 className="text-xl font-bold text-white mb-4">{label}</h2>
                  <div className="space-y-3">
                    {statusTasks.map((task) => (
                      <div
                        key={task.id}
                        draggable
                        onDragStart={(e) => handleDragStart(e, task)}
                        className="bg-gray-700 rounded-lg p-4 cursor-move hover:bg-gray-600 transition relative"
                        style={getCardStyle(category?.color)}
                      >
                        {/* X button - top right */}
                        <button
                          aria-label="Delete task"
                          onClick={(e) => handleDeleteTask(task.id, e)}
                          className="absolute top-2 right-2 text-white/80 hover:text-white bg-black/20 hover:bg-black/30 rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold"
                        >
                          ×
                        </button>

                        {/* Task name - bold and big */}
                        <div className="font-bold text-lg text-white mb-2 pr-6">
                          {task.description}
                        </div>

                        {/* Priority, deadline, estimation - small font */}
                        <div className="text-xs text-gray-300 space-y-1 mb-4">
                          <div className={`font-semibold ${getPriorityColor(task.priority)}`}>
                            Priority: {task.priority || 'Medium'}
                          </div>
                          <div>Deadline: {formatDate(task.deadline)}</div>
                          <div>Estimation: {formatEstimatedMinutes(task.estimatedMinutes)}</div>
                        </div>

                        {/* Info button - bottom right */}
                        <div className="flex justify-end">
                          <button
                            onClick={(e) => handleInfoClick(task, e)}
                            className="text-white/80 hover:text-white bg-black/20 hover:bg-black/30 rounded px-3 py-1 text-xs font-semibold transition"
                            aria-label="View task details"
                          >
                            ℹ Info
                          </button>
                        </div>
                      </div>
                    ))}
                    {statusTasks.length === 0 && (
                      <div className="text-gray-500 text-sm text-center py-8">
                        No tasks
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </main>

      {/* Add Task Dialog */}
      {categoryId && (
        <AddTaskDialog
          isOpen={isDialogOpen}
          onClose={() => setIsDialogOpen(false)}
          categoryId={categoryId}
          onSuccess={loadTasks}
          onError={(errorMessage) => setError(errorMessage)}
        />
      )}

      {/* Task Info Dialog */}
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
