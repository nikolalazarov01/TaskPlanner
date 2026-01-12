import React, { useState, useEffect } from 'react';
import { taskApi } from '../services/api';
import type { CreateTaskInputModel } from '../types';
import { TaskPriority, TaskStatus } from '../types';

interface AddTaskDialogProps {
  isOpen: boolean;
  onClose: () => void;
  categoryId: string;
  onSuccess?: () => void;
  onError?: (error: string) => void;
  initialStatus?: TaskStatus;
}

export const AddTaskDialog: React.FC<AddTaskDialogProps> = ({
  isOpen,
  onClose,
  categoryId,
  onSuccess,
  onError,
  initialStatus,
}) => {
  const [taskDescription, setTaskDescription] = useState('');
  const [taskDeadline, setTaskDeadline] = useState('');
  const [taskPriority, setTaskPriority] = useState<TaskPriority>(TaskPriority.Medium);
  const [taskStatus, setTaskStatus] = useState<TaskStatus>(initialStatus || TaskStatus.Todo);
  const [taskEstimatedMinutes, setTaskEstimatedMinutes] = useState<number | undefined>(undefined);

  const resetForm = () => {
    setTaskDescription('');
    setTaskDeadline('');
    setTaskPriority(TaskPriority.Medium);
    setTaskStatus(initialStatus || TaskStatus.Todo);
    setTaskEstimatedMinutes(undefined);
  };

  // Update status when initialStatus prop changes
  useEffect(() => {
    if (isOpen && initialStatus) {
      setTaskStatus(initialStatus);
    }
  }, [isOpen, initialStatus]);

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      // Convert datetime-local format to ISO string for API
      let deadlineISO: string | undefined = undefined;
      if (taskDeadline) {
        const date = new Date(taskDeadline);
        deadlineISO = date.toISOString();
      }

      const newTask: CreateTaskInputModel = {
        description: taskDescription,
        deadline: deadlineISO,
        priority: taskPriority,
        status: taskStatus,
        estimatedMinutes: taskEstimatedMinutes,
      };
      await taskApi.create(newTask, categoryId);
      resetForm();
      onClose();
      if (onSuccess) {
        onSuccess();
      }
    } catch (err) {
      const errorMessage = 'Failed to create task';
      if (onError) {
        onError(errorMessage);
      }
      console.error(err);
    }
  };

  const handleClose = () => {
    resetForm();
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
        <h2 className="text-2xl font-bold text-white mb-4">Add Task</h2>
        <form onSubmit={handleCreateTask}>
          <div className="space-y-4">
            <div>
              <label htmlFor="taskDescription" className="block text-sm font-medium text-gray-300 mb-1">
                Description *
              </label>
              <input
                id="taskDescription"
                type="text"
                value={taskDescription}
                onChange={(e) => setTaskDescription(e.target.value)}
                required
                className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Task description"
              />
            </div>

            <div>
              <label htmlFor="taskDeadline" className="block text-sm font-medium text-gray-300 mb-1">
                Deadline
              </label>
              <input
                id="taskDeadline"
                type="datetime-local"
                value={taskDeadline}
                onChange={(e) => setTaskDeadline(e.target.value)}
                className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label htmlFor="taskPriority" className="block text-sm font-medium text-gray-300 mb-1">
                Priority
              </label>
              <select
                id="taskPriority"
                value={taskPriority}
                onChange={(e) => setTaskPriority(e.target.value as TaskPriority)}
                className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={TaskPriority.Low}>Low</option>
                <option value={TaskPriority.Medium}>Medium</option>
                <option value={TaskPriority.High}>High</option>
              </select>
            </div>

            <div>
              <label htmlFor="taskStatus" className="block text-sm font-medium text-gray-300 mb-1">
                Status
              </label>
              <select
                id="taskStatus"
                value={taskStatus}
                onChange={(e) => setTaskStatus(e.target.value as TaskStatus)}
                className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={TaskStatus.Todo}>Todo</option>
                <option value={TaskStatus.InProgress}>In Progress</option>
                <option value={TaskStatus.Done}>Done</option>
              </select>
            </div>

            <div>
              <label htmlFor="taskEstimatedMinutes" className="block text-sm font-medium text-gray-300 mb-1">
                Estimated Minutes
              </label>
              <input
                id="taskEstimatedMinutes"
                type="number"
                min="0"
                value={taskEstimatedMinutes || ''}
                onChange={(e) => setTaskEstimatedMinutes(e.target.value ? parseInt(e.target.value) : undefined)}
                className="w-full px-4 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Estimated time in minutes"
              />
            </div>
          </div>

          <div className="mt-6 flex gap-4 justify-end">
            <button
              type="button"
              onClick={handleClose}
              className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white font-semibold rounded-lg transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition"
            >
              Create
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

