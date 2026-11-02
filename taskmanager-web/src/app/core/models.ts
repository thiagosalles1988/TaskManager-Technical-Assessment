export type TaskStatus = 'Todo' | 'InProgress' | 'Completed';
export const taskStatuses: readonly TaskStatus[] = ['Todo', 'InProgress', 'Completed'];
export const statusLabels: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Completed: 'Completed',
};
export interface TaskInput {
  title: string;
  description: string;
  status: TaskStatus;
  dueDateUtc: string | null;
}
export interface TaskItem extends TaskInput {
  id: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}
export interface User {
  id: string;
  email: string;
}
export interface AuthResult {
  accessToken: string;
  expiresAtUtc: string;
  user: User;
}
export interface Credentials {
  email: string;
  password: string;
}
