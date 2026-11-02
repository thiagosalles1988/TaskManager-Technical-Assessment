import { FormControl, FormGroup, Validators } from '@angular/forms';
import { TaskInput, TaskStatus } from '../core/models';
export function createTaskForm() {
  return new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(120),
        (c) => (c.value?.trim() ? null : { required: true }),
      ],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
    status: new FormControl<TaskStatus>('Todo', {
      nonNullable: true,
      validators: [Validators.pattern(/^(Todo|InProgress|Completed)$/)],
    }),
    dueDate: new FormControl('', {
      nonNullable: true,
      validators: [
        (c) => (!c.value || Number.isFinite(new Date(c.value).getTime()) ? null : { date: true }),
      ],
    }),
  });
}
export function taskInput(form: ReturnType<typeof createTaskForm>): TaskInput {
  if (!form.valid) throw new Error('Review the highlighted fields.');
  const value = form.getRawValue();
  return {
    title: value.title.trim(),
    description: value.description,
    status: value.status,
    dueDateUtc: value.dueDate ? new Date(value.dueDate).toISOString() : null,
  };
}

export function localDateInput(utc: string | null): string {
  if (!utc) return '';
  const date = new Date(utc);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}
