// Identity Types
export interface LoginInputModel {
  email: string;
  password: string;
}

export interface RegisterInputModel {
  email: string;
  password: string;
  displayName: string;
}

export interface AuthResponse {
  success: boolean;
  token?: string;
  error?: string;
}

// Category Types
export interface CategoryInputModel {
  name: string;
  color?: string;
  sortOrder?: number;
}

export interface CategoryResponseModel {
  id: string;
  userId: string;
  name: string;
  color?: string;
  sortOrder?: number;
  createdAt: string;
}

export interface UpdateCategoryInputModel {
  id: string;
  name?: string;
  color?: string;
  sortOrder?: number;
}

// Task Types
export enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High'
}

export enum TaskStatus {
  Todo = 'Todo',
  InProgress = 'InProgress',
  Done = 'Done'
}

export interface CreateTaskInputModel {
  description: string;
  deadline?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
  estimatedMinutes?: number;
}

export interface TaskResponseModel {
  id: string;
  userId: string;
  categoryId: string;
  description: string;
  deadline?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
  estimatedMinutes?: number;
}

export interface UpdateTaskInputModel {
  id: string;
  categoryId: string;
  description: string;
  deadline?: string;
  priority?: TaskPriority;
  status?: TaskStatus;
  estimatedMinutes?: number;
}
