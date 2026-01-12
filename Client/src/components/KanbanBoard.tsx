// KanbanBoard.tsx
import React, { memo, useMemo } from 'react';
import type { TaskResponseModel, CategoryResponseModel } from '../types';
import { TaskStatus } from '../types';
import { KanbanColumn } from './KanbanColumn';

export type StatusColumnDef = { status: TaskStatus; label: string };

type KanbanBoardProps = {
  tasks: TaskResponseModel[];
  category?: CategoryResponseModel | null;

  statusColumns: StatusColumnDef[];

  onAddTask: (status: TaskStatus) => void;
  onDeleteTask: (taskId: string) => void;
  onInfoClick: (task: TaskResponseModel) => void;

  onDragStart: (e: React.DragEvent, task: TaskResponseModel) => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: (e: React.DragEvent, targetStatus: TaskStatus) => void;

  formatDate: (dateString?: string) => string;
  formatEstimatedMinutes: (minutes?: number) => string;
};

function groupTasksByStatus(tasks: TaskResponseModel[]) {
  const grouped: Record<TaskStatus, TaskResponseModel[]> = {
    [TaskStatus.Todo]: [],
    [TaskStatus.InProgress]: [],
    [TaskStatus.Done]: [],
  };

  for (const t of tasks) {
    const s = (t.status || TaskStatus.Todo) as TaskStatus;
    (grouped[s] ||= []).push(t);
  }

  return grouped;
}

export const KanbanBoard = memo(function KanbanBoard({
  tasks,
  category,
  statusColumns,
  onAddTask,
  onDeleteTask,
  onInfoClick,
  onDragStart,
  onDragOver,
  onDrop,
  formatDate,
  formatEstimatedMinutes,
}: KanbanBoardProps) {
  const grouped = useMemo(() => groupTasksByStatus(tasks), [tasks]);

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
      {statusColumns.map(({ status, label }) => (
        <KanbanColumn
          key={status}
          status={status}
          label={label}
          tasks={grouped[status] ?? []}
          category={category}
          onAddTask={onAddTask}
          onDeleteTask={onDeleteTask}
          onInfoClick={onInfoClick}
          onDragStart={onDragStart}
          onDragOver={onDragOver}
          onDrop={onDrop}
          formatDate={formatDate}
          formatEstimatedMinutes={formatEstimatedMinutes}
        />
      ))}
    </div>
  );
});
