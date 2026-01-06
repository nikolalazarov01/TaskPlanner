import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { taskApi, categoryApi } from '../services/api';
import type { TaskResponseModel, CategoryResponseModel } from '../types';
import { TaskPriority } from '../types';

type SortOption = 'deadline' | 'estimatedMinutes' | 'none';
type GroupByPriority = boolean;

export const Tasks: React.FC = () => {
  const { categoryId } = useParams<{ categoryId: string }>();
  const [tasks, setTasks] = useState<TaskResponseModel[]>([]);
  const [category, setCategory] = useState<CategoryResponseModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [sortBy, setSortBy] = useState<SortOption>('none');
  const [groupByPriority, setGroupByPriority] = useState<GroupByPriority>(false);
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

  const getSortedTasks = (): TaskResponseModel[] => {
    let sorted = [...tasks];

    if (sortBy === 'deadline') {
      sorted.sort((a, b) => {
        if (!a.deadline && !b.deadline) return 0;
        if (!a.deadline) return 1;
        if (!b.deadline) return -1;
        return new Date(a.deadline).getTime() - new Date(b.deadline).getTime();
      });
    } else if (sortBy === 'estimatedMinutes') {
      sorted.sort((a, b) => {
        const aEst = a.estimatedMinutes ?? 0;
        const bEst = b.estimatedMinutes ?? 0;
        return aEst - bEst;
      });
    }

    return sorted;
  };

  const getGroupedTasks = (): Record<string, TaskResponseModel[]> => {
    if (!groupByPriority) {
      return { 'All Tasks': getSortedTasks() };
    }

    const grouped: Record<string, TaskResponseModel[]> = {
      High: [],
      Medium: [],
      Low: [],
    };

    getSortedTasks().forEach((task) => {
      const priority = task.priority || TaskPriority.Medium;
      grouped[priority].push(task);
    });

    return grouped;
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

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <div className="text-white text-xl">Loading tasks...</div>
      </div>
    );
  }

  const groupedTasks = getGroupedTasks();

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
        <div className="mb-6 flex flex-wrap gap-4">
          <button
            onClick={() => setGroupByPriority(!groupByPriority)}
            className={`px-4 py-2 rounded-lg font-semibold transition ${
              groupByPriority
                ? 'bg-blue-600 hover:bg-blue-700 text-white'
                : 'bg-gray-700 hover:bg-gray-600 text-gray-300'
            }`}
          >
            {groupByPriority ? 'Ungroup by Priority' : 'Group by Priority'}
          </button>

          <button
            onClick={() => setSortBy(sortBy === 'deadline' ? 'none' : 'deadline')}
            className={`px-4 py-2 rounded-lg font-semibold transition ${
              sortBy === 'deadline'
                ? 'bg-blue-600 hover:bg-blue-700 text-white'
                : 'bg-gray-700 hover:bg-gray-600 text-gray-300'
            }`}
          >
            {sortBy === 'deadline' ? '✓ Sort by Deadline' : 'Sort by Deadline'}
          </button>

          <button
            onClick={() => setSortBy(sortBy === 'estimatedMinutes' ? 'none' : 'estimatedMinutes')}
            className={`px-4 py-2 rounded-lg font-semibold transition ${
              sortBy === 'estimatedMinutes'
                ? 'bg-blue-600 hover:bg-blue-700 text-white'
                : 'bg-gray-700 hover:bg-gray-600 text-gray-300'
            }`}
          >
            {sortBy === 'estimatedMinutes' ? '✓ Sort by Estimation' : 'Sort by Estimation'}
          </button>
        </div>

        {/* Tasks Table */}
        {tasks.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-gray-400 text-lg">No tasks in this category</p>
          </div>
        ) : (
          <div className="space-y-8">
            {Object.entries(groupedTasks).map(([groupName, groupTasks]) => {
              if (groupTasks.length === 0) return null;

              return (
                <div key={groupName}>
                  {groupByPriority && (
                    <h2 className="text-xl font-bold text-white mb-4">{groupName} Priority</h2>
                  )}
                  <div className="bg-gray-800 rounded-lg overflow-hidden">
                    <table className="w-full">
                      <thead className="bg-gray-700">
                        <tr>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
                            Description
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
                            Deadline
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
                            Estimated
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-300 uppercase tracking-wider">
                            Priority
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-700">
                        {groupTasks.map((task) => (
                          <tr
                            key={task.id}
                            className="hover:bg-gray-750"
                            style={getCardStyle(category?.color)}
                          >
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="text-sm font-medium text-white">{task.description}</div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="text-sm text-gray-300">{formatDate(task.deadline)}</div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="text-sm text-gray-300">{formatEstimatedMinutes(task.estimatedMinutes)}</div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className={`text-sm font-semibold ${getPriorityColor(task.priority)}`}>
                                {task.priority || 'Medium'}
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </main>
    </div>
  );
};
