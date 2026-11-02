import {
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  computed,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription, finalize } from 'rxjs';
import { TaskApi } from '../core/task-api.service';
import { TaskItem, TaskStatus, statusLabels, taskStatuses } from '../core/models';
import { apiError } from '../core/api-error';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: ` <section class="page-heading">
      <div>
        <span class="eyebrow">Your personal workspace</span>
        <h1>A clear view of your day.</h1>
        <p class="muted">Small steps. Meaningful progress.</p>
      </div>
      <a class="button primary" routerLink="/tasks/new"
        ><span aria-hidden="true">＋</span> New task</a
      >
    </section>
    <div class="stats" aria-label="Task summary">
      <div class="stat">
        <span>Total tasks</span><strong>{{ tasks().length }}</strong>
      </div>
      @for (status of statuses; track status) {
        <div class="stat">
          <span>{{ labels[status] }}</span
          ><strong>{{ count(status) }}</strong>
        </div>
      }
    </div>
    <section class="task-section">
      <div class="section-toolbar">
        <h2>
          Your tasks <span class="count">{{ visible().length }}</span>
        </h2>
        <div class="filters" role="group" aria-label="Filter tasks by status">
          <button
            [class.active]="filter() === 'All'"
            [attr.aria-pressed]="filter() === 'All'"
            (click)="filter.set('All')"
          >
            All
          </button>
          @for (status of statuses; track status) {
            <button
              [class.active]="filter() === status"
              [attr.aria-pressed]="filter() === status"
              (click)="filter.set(status)"
            >
              {{ labels[status] }}
            </button>
          }
        </div>
      </div>
      @if (error()) {
        <div class="notice error" role="alert">
          {{ error() }} <button class="text-button" (click)="load()">Try again</button>
        </div>
      }
      @if (loading()) {
        <div class="empty-state" role="status">
          <span class="spinner" aria-hidden="true"></span>Loading your tasks…
        </div>
      } @else if (!error() && !visible().length) {
        <div class="empty-state panel">
          <span class="empty-icon" aria-hidden="true">✓</span>
          <h3>{{ tasks().length ? 'Nothing in this view.' : 'Start with one small step.' }}</h3>
          <p class="muted">
            {{
              tasks().length
                ? 'Choose another status to see your tasks.'
                : 'Add your first task and give your day a little direction.'
            }}
          </p>
          @if (!tasks().length) {
            <a class="button primary" routerLink="/tasks/new">Create your first task</a>
          }
        </div>
      } @else {
        <div class="task-grid">
          @for (task of visible(); track task.id) {
            <article class="panel task-card" [class.task-completed]="task.status === 'Completed'">
              <div class="card-top">
                <span
                  class="badge"
                  [class.in-progress]="task.status === 'InProgress'"
                  [class.completed]="task.status === 'Completed'"
                  >{{ labels[task.status] }}</span
                ><a
                  class="text-button"
                  [routerLink]="['/tasks', task.id, 'edit']"
                  [attr.aria-label]="'Edit ' + task.title"
                  >Edit</a
                >
              </div>
              <h3>{{ task.title }}</h3>
              <p class="task-description">{{ task.description || 'No description added.' }}</p>
              <p class="due-date" [class.overdue]="isOverdue(task)">
                <span aria-hidden="true">◷</span>
                {{ task.dueDateUtc ? (task.dueDateUtc | date: 'MMM d, y, HH:mm') : 'No due date' }}
                @if (isOverdue(task)) {
                  <span> · Overdue</span>
                }
              </p>
              <div class="card-actions">
                <label class="sr-only" [for]="'status-' + task.id"
                  >Status for {{ task.title }}</label
                ><select
                  [id]="'status-' + task.id"
                  [value]="task.status"
                  [disabled]="busy() !== null || deleting()"
                  (change)="
                    changeStatus(task, $any($event.target).value);
                    $any($event.target).value = task.status
                  "
                >
                  @for (status of statuses; track status) {
                    <option [value]="status" [selected]="status === task.status">
                      {{ labels[status] }}
                    </option>
                  }</select
                ><button
                  class="text-button danger"
                  (click)="askDelete(task)"
                  [disabled]="busy() !== null || deleting()"
                  [attr.aria-label]="'Delete ' + task.title"
                >
                  Delete
                </button>
              </div>
            </article>
          }
        </div>
      }
    </section>
    <dialog
      #confirmation
      (cancel)="target.set(null)"
      aria-labelledby="delete-title"
      aria-describedby="delete-description"
    >
      <div class="dialog-content">
        <span class="eyebrow">Delete task</span>
        <h2 id="delete-title">Remove this task?</h2>
        <p id="delete-description">“{{ target()?.title }}” will be permanently deleted.</p>
        <div class="form-actions">
          <button
            class="button secondary"
            (click)="closeDelete()"
            [disabled]="deleting()"
            autofocus
          >
            Keep task</button
          ><button class="button destructive" (click)="confirmDelete()" [disabled]="deleting()">
            {{ deleting() ? 'Deleting…' : 'Delete task' }}
          </button>
        </div>
      </div>
    </dialog>`,
})
export class DashboardComponent {
  private readonly api = inject(TaskApi);
  private readonly destroy = inject(DestroyRef);
  private listRequest?: Subscription;
  @ViewChild('confirmation') private confirmation!: ElementRef<HTMLDialogElement>;
  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly busy = signal<string | null>(null);
  readonly filter = signal<TaskStatus | 'All'>('All');
  readonly target = signal<TaskItem | null>(null);
  readonly deleting = signal(false);
  readonly statuses = taskStatuses;
  readonly labels = statusLabels;
  readonly visible = computed(() =>
    this.tasks().filter((t) => this.filter() === 'All' || t.status === this.filter()),
  );
  constructor() {
    this.load();
  }
  count(status: TaskStatus): number {
    return this.tasks().filter((t) => t.status === status).length;
  }
  isOverdue(task: TaskItem): boolean {
    return (
      task.status !== 'Completed' && !!task.dueDateUtc && Date.parse(task.dueDateUtc) < Date.now()
    );
  }
  load(): void {
    this.listRequest?.unsubscribe();
    this.loading.set(true);
    this.error.set('');
    this.listRequest = this.api
      .list()
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (tasks) => this.tasks.set(tasks),
        error: (error) => this.error.set(apiError(error)),
      });
  }
  changeStatus(task: TaskItem, status: TaskStatus): void {
    if (this.loading() || this.busy() !== null || this.deleting()) return;
    this.busy.set(task.id);
    this.error.set('');
    this.api
      .update(task.id, {
        title: task.title,
        description: task.description,
        dueDateUtc: task.dueDateUtc,
        status,
      })
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.busy.set(null)),
      )
      .subscribe({ next: () => this.load(), error: (error) => this.error.set(apiError(error)) });
  }
  askDelete(task: TaskItem): void {
    if (this.loading() || this.busy() !== null || this.deleting()) return;
    this.target.set(task);
    this.confirmation.nativeElement.showModal();
  }
  closeDelete(): void {
    this.confirmation.nativeElement.close();
    this.target.set(null);
  }
  confirmDelete(): void {
    const task = this.target();
    if (!task || this.loading() || this.busy() !== null || this.deleting()) return;
    this.deleting.set(true);
    this.api
      .delete(task.id)
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.deleting.set(false)),
      )
      .subscribe({
        next: () => {
          this.closeDelete();
          this.load();
        },
        error: (error) => {
          this.closeDelete();
          this.error.set(apiError(error));
        },
      });
  }
}
