import { Routes } from '@angular/router';

import { authGuard } from './core/auth.guard';
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./auth/auth.component').then((m) => m.AuthComponent),
  },
  {
    path: 'register',
    data: { registration: true },
    loadComponent: () => import('./auth/auth.component').then((m) => m.AuthComponent),
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () => import('./tasks/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'tasks/new',
    canActivate: [authGuard],
    loadComponent: () => import('./tasks/task-editor.component').then((m) => m.TaskEditorComponent),
  },
  {
    path: 'tasks/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./tasks/task-editor.component').then((m) => m.TaskEditorComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: '**', redirectTo: 'tasks' },
];
