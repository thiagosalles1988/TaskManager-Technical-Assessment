import { Component, DestroyRef, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize } from 'rxjs';
import { TaskApi } from '../core/task-api.service';
import { statusLabels, taskStatuses } from '../core/models';
import { apiError } from '../core/api-error';
import { createTaskForm, localDateInput, taskInput } from './task-form';

@Component({
  selector: 'app-task-editor',
  imports: [ReactiveFormsModule, RouterLink],
  template: ` <div class="editor-wrap">
    <a class="back-link" routerLink="/tasks">← Back to your tasks</a>
    <section class="page-heading">
      <div>
        <span class="eyebrow">One step at a time</span>
        <h1>{{ id ? 'Edit task' : 'Create a task' }}</h1>
        <p class="muted">Give it a name. Make a little progress.</p>
      </div>
    </section>
    @if (error()) {
      <p class="notice error" role="alert">{{ error() }}</p>
    }
    @if (loading()) {
      <p role="status">Loading task…</p>
    } @else if (!loadFailed()) {
      <form class="panel editor-panel" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <label for="title">Title <span class="muted">(required)</span></label
        ><input
          id="title"
          formControlName="title"
          maxlength="120"
          placeholder="What would you like to get done?"
          [attr.aria-invalid]="form.controls.title.touched && form.controls.title.invalid"
          aria-describedby="title-help"
        />
        <div id="title-help" class="field-help">
          {{ form.controls.title.value.length }}/120
          @if (form.controls.title.touched && form.controls.title.invalid) {
            <span class="field-error">Enter a title of 1 to 120 characters.</span>
          }
        </div>
        <label for="description">Description <span class="muted">(optional)</span></label
        ><textarea
          id="description"
          rows="5"
          formControlName="description"
          maxlength="2000"
          placeholder="Add a little context…"
          aria-describedby="description-help"
        ></textarea>
        <div id="description-help" class="field-help">
          {{ form.controls.description.value.length }}/2000
        </div>
        <div class="form-columns">
          <div>
            <label for="status">Status</label
            ><select id="status" formControlName="status">
              @for (status of statuses; track status) {
                <option [value]="status">{{ labels[status] }}</option>
              }
            </select>
          </div>
          <div>
            <label for="dueDate">Due date <span class="muted">(optional)</span></label
            ><input
              id="dueDate"
              type="datetime-local"
              formControlName="dueDate"
              aria-describedby="date-help"
            />
            <p id="date-help" class="field-help">Your local time. Overdue dates are allowed.</p>
            @if (form.controls.dueDate.invalid) {
              <span class="field-error">Enter a valid date.</span>
            }
          </div>
        </div>
        <div class="form-actions">
          <a class="button secondary" routerLink="/tasks">Cancel</a
          ><button type="submit" class="button primary" [disabled]="saving()">
            {{ saving() ? 'Saving…' : id ? 'Save changes' : 'Create task' }}
          </button>
        </div>
      </form>
    }
  </div>`,
})
export class TaskEditorComponent {
  private readonly api = inject(TaskApi);
  private readonly router = inject(Router);
  private readonly destroy = inject(DestroyRef);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id');
  readonly form = createTaskForm();
  readonly statuses = taskStatuses;
  readonly labels = statusLabels;
  readonly loading = signal(!!this.id);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly loadFailed = signal(false);
  constructor() {
    if (this.id)
      this.api
        .get(this.id)
        .pipe(
          takeUntilDestroyed(this.destroy),
          finalize(() => this.loading.set(false)),
        )
        .subscribe({
          next: (task) =>
            this.form.patchValue({
              title: task.title,
              description: task.description,
              status: task.status,
              dueDate: localDateInput(task.dueDateUtc),
            }),
          error: (error) => {
            this.error.set(apiError(error));
            this.loadFailed.set(true);
          },
        });
  }
  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    const input = taskInput(this.form);
    const request: Observable<unknown> = this.id
      ? this.api.update(this.id, input)
      : this.api.create(input);
    request
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/tasks']);
        },
        error: (error) => this.error.set(apiError(error)),
      });
  }
}
