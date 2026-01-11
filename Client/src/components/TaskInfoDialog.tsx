import React from 'react';
import type { TaskResponseModel } from '../types';
import { TaskPriority } from '../types';

interface TaskInfoDialogProps {
  isOpen: boolean;
  onClose: () => void;
  task: TaskResponseModel | null;
}

export const TaskInfoDialog: React.FC<TaskInfoDialogProps> = ({ isOpen, onClose, task }) => {
  if (!isOpen || !task) return null;

  const formatDate = (dateString?: string): string => {
    if (!dateString) return 'Not set';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const formatEstimatedMinutes = (minutes?: number): string => {
    if (!minutes) return 'Not set';
    if (minutes < 60) return `${minutes} minutes`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours} hours ${mins} minutes` : `${hours} hours`;
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

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={onClose}>
      <div 
        className="bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-md max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-bold text-white">Task Details</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-white text-2xl font-bold"
          >
            ×
          </button>
        </div>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Description</label>
            <div className="text-white text-lg">{task.description}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Status</label>
            <div className="text-white">{task.status || 'Todo'}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Priority</label>
            <div className={`font-semibold ${getPriorityColor(task.priority)}`}>
              {task.priority || 'Medium'}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Deadline</label>
            <div className="text-white">{formatDate(task.deadline)}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Estimated Time</label>
            <div className="text-white">{formatEstimatedMinutes(task.estimatedMinutes)}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Task ID</label>
            <div className="text-white text-sm font-mono">{task.id}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Category ID</label>
            <div className="text-white text-sm font-mono">{task.categoryId}</div>
          </div>
        </div>

        <div className="mt-6 flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

