import axios from 'axios';
import type {
  LoginInputModel,
  RegisterInputModel,
  AuthResponse,
  CategoryInputModel,
  CategoryResponseModel,
  CreateTaskInputModel,
  TaskResponseModel,
  TaskStatus,
} from '../types';

const API_BASE_URL = 'https://localhost:44358/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor to handle 401 errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Identity API
export const identityApi = {
  login: async (data: LoginInputModel): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/identity/login', data);
    return response.data;
  },

  register: async (data: RegisterInputModel): Promise<void> => {
    await apiClient.post('/identity/register', data);
  },
};

// Category API
export const categoryApi = {
  getAll: async (): Promise<CategoryResponseModel[]> => {
    const response = await apiClient.get<CategoryResponseModel[]>('/category');
    return response.data;
  },

  getOne: async (id: string): Promise<CategoryResponseModel> => {
    const response = await apiClient.get<CategoryResponseModel>(`/category/${id}`);
    return response.data;
  },

  create: async (data: CategoryInputModel): Promise<CategoryResponseModel> => {
    const response = await apiClient.post<CategoryResponseModel>('/category', data);
    return response.data;
  },

  update: async (data: { id: string; name?: string; color?: string; sortOrder?: number }): Promise<CategoryResponseModel> => {
    const response = await apiClient.patch<CategoryResponseModel>('/category', data);
    return response.data;
  },

  delete: async (id: string): Promise<CategoryResponseModel> => {
    const response = await apiClient.delete<CategoryResponseModel>(`/category/${id}`);
    return response.data;
  },
};

// Task API
export const taskApi = {
  getAll: async (categoryId?: string): Promise<TaskResponseModel[]> => {
    const url = categoryId ? `/task?categoryId=${categoryId}` : '/task';
    const response = await apiClient.get<TaskResponseModel[]>(url);
    return response.data;
  },

  getOne: async (id: string, categoryId?: string): Promise<TaskResponseModel> => {
    const url = categoryId ? `/task/${id}?categoryId=${categoryId}` : `/task/${id}`;
    const response = await apiClient.get<TaskResponseModel>(url);
    return response.data;
  },

  create: async (data: CreateTaskInputModel, categoryId: string): Promise<TaskResponseModel> => {
    const response = await apiClient.post<TaskResponseModel>(`/task?categoryId=${categoryId}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<TaskResponseModel> => {
    const response = await apiClient.delete<TaskResponseModel>(`/task/${id}`);
    return response.data;
  },

  updateStatus: async (id: string, status: TaskStatus, previousStatus: TaskStatus): Promise<number> => {
    const response = await apiClient.patch<number>(`/task/update-status?taskId=${id}&status=${status}&previousStatus=${previousStatus}`);
    return response.data;
  },
};
