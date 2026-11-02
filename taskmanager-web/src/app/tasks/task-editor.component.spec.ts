import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { TaskEditorComponent } from './task-editor.component';
import { TaskItem } from '../core/models';

describe('task editor timestamp round trip', () => {
  let http: HttpTestingController;
  const original: TaskItem = {
    id: 'task-1',
    title: 'Original',
    description: '',
    status: 'Todo',
    dueDateUtc: '2026-11-04T15:00:45.1234567Z',
    createdAtUtc: '2026-11-01T00:00:00Z',
    updatedAtUtc: '2026-11-01T00:00:00Z',
  };
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TaskEditorComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: original.id }) } },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });
  afterEach(() => http.verify());

  it('preserves the exact original due instant when editing only the title', () => {
    const editor = TestBed.createComponent(TaskEditorComponent).componentInstance;
    http.expectOne('/api/tasks/task-1').flush(original);
    editor.form.controls.title.setValue('Updated title');
    editor.save();
    const update = http.expectOne('/api/tasks/task-1');
    expect(update.request.body.dueDateUtc).toBe('2026-11-04T15:00:45.1234567Z');
    update.flush(null);
  });

  it('converts an explicitly changed local due date to UTC', () => {
    const editor = TestBed.createComponent(TaskEditorComponent).componentInstance;
    http.expectOne('/api/tasks/task-1').flush(original);
    editor.form.controls.dueDate.setValue('2026-11-05T12:30');
    editor.save();
    const update = http.expectOne('/api/tasks/task-1');
    expect(update.request.body.dueDateUtc).toBe(new Date(2026, 10, 5, 12, 30).toISOString());
    update.flush(null);
  });

  it('allows clearing an existing optional due date', () => {
    const editor = TestBed.createComponent(TaskEditorComponent).componentInstance;
    http.expectOne('/api/tasks/task-1').flush(original);
    editor.form.controls.dueDate.setValue('');
    editor.save();
    const update = http.expectOne('/api/tasks/task-1');
    expect(update.request.body.dueDateUtc).toBeNull();
    update.flush(null);
  });
});
