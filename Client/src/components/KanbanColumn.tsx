import React, { memo } from 'react';
import type { TaskResponseModel, CategoryResponseModel } from '../types';
import { TaskPriority, TaskStatus } from '../types';
import './KanbanColumn.less';

type KanbanColumnProps = {
  status: TaskStatus;
  label: string;
  tasks: TaskResponseModel[];
  category?: CategoryResponseModel | null;

  onAddTask: (status: TaskStatus) => void;
  onDeleteTask: (taskId: string) => void;
  onInfoClick: (task: TaskResponseModel) => void;

  onDragStart: (e: React.DragEvent, task: TaskResponseModel) => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: (e: React.DragEvent, targetStatus: TaskStatus) => void;

  formatDate: (dateString?: string) => string;
  formatEstimatedMinutes: (minutes?: number) => string;
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
  } as React.CSSProperties;
};

export const KanbanColumn = memo(function KanbanColumn({
  status,
  label,
  tasks,
  category,
  onAddTask,
  onDeleteTask,
  onInfoClick,
  onDragStart,
  onDragOver,
  onDrop,
  formatDate,
  formatEstimatedMinutes,
}: KanbanColumnProps) {
  return (
    <div
      className="bg-gray-800 rounded-lg p-4 min-h-[400px] kanban-column"
      onDragOver={onDragOver}
      onDrop={(e) => onDrop(e, status)}
    >
      <div className="flex justify-between items-center mb-4 shrink-0">
        <h2 className="text-xl font-bold text-white">{label}</h2>
        <button
          onClick={() => onAddTask(status)}
          className="text-white/80 hover:text-white bg-blue-600 hover:bg-blue-700 rounded-full w-7 h-7 flex items-center justify-center text-lg font-bold transition"
        >
          +
        </button>
      </div>

      <div className="kanban-column__list space-y-3">
        {tasks.map((task) => (
          <div
            key={task.id}
            draggable
            onDragStart={(e) => onDragStart(e, task)}
            className="bg-gray-700 rounded-lg p-4 cursor-move hover:bg-gray-600 transition relative kanban-card"
            style={getCardStyle(category?.color)}
          >
            <button
              onClick={(e) => {
                e.stopPropagation();
                onDeleteTask(task.id);
              }}
              className="absolute top-2 right-2 text-white/80 hover:text-white bg-black/20 rounded-full w-6 h-6 flex items-center justify-center"
            >
              ×
            </button>

            <div className="font-bold text-lg text-white pr-6">
              {task.description}
            </div>

            <div className="text-xs text-gray-300 space-y-1">
              <div className={`font-semibold ${getPriorityColor(task.priority)}`}>
                Priority: {task.priority || 'Medium'}
              </div>
              <div>Deadline: {formatDate(task.deadline)}</div>
              <div>Estimation: {formatEstimatedMinutes(task.estimatedMinutes)}</div>
            </div>

            <div className="flex justify-end">
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onInfoClick(task);
                }}
                className="text-white/80 hover:text-white bg-black/20 rounded px-3 py-1 text-xs font-semibold"
              >
                ℹ Info
              </button>
            </div>
          </div>
        ))}

        {tasks.length === 0 && (
          <div className="text-gray-500 text-sm text-center py-8">
            No tasks
          </div>
        )}
      </div>
    </div>
  );
});
