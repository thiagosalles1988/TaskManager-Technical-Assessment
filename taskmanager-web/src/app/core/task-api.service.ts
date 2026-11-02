import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TaskInput, TaskItem } from './models';

@Injectable({ providedIn: 'root' })
export class TaskApi {
  private readonly http = inject(HttpClient);
  list() {
    return this.http.get<TaskItem[]>('/api/tasks');
  }
  get(id: string) {
    return this.http.get<TaskItem>(`/api/tasks/${encodeURIComponent(id)}`);
  }
  create(input: TaskInput) {
    return this.http.post<TaskItem>('/api/tasks', input);
  }
  update(id: string, input: TaskInput) {
    return this.http.put<void>(`/api/tasks/${encodeURIComponent(id)}`, input);
  }
  delete(id: string) {
    return this.http.delete<void>(`/api/tasks/${encodeURIComponent(id)}`);
  }
}
