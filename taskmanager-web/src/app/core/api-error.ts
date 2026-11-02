import { HttpErrorResponse } from '@angular/common/http';
export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Something went wrong. Please try again.';
  if (error.status === 0) return 'Cannot reach the server. Check your connection and try again.';
  if (error.status === 401) return 'Email or password is incorrect, or your session has expired.';
  if (error.status === 409) return 'An account with this email already exists.';
  if (error.status === 404) return 'This task is no longer available.';
  if (error.status === 429) return 'Too many attempts. Please wait a minute and try again.';
  const errors = error.error?.errors as Record<string, string[]> | undefined;
  if (errors && typeof errors === 'object') return Object.values(errors).flat().join(' ');
  return 'The server could not complete your request. Please try again.';
}
