import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { TaskItem } from '../core/models';

const first: TaskItem = {
  id: 'first',
  title: 'First',
  description: '',
  status: 'Todo',
  dueDateUtc: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
};
const second: TaskItem = { ...first, id: 'second', title: 'Second' };

describe('dashboard request coordination', () => {
  let http: HttpTestingController;
  it('keeps the displayed status aligned with the server after an update fails', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    http.expectOne('/api/tasks').flush([first]);
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Completed';
    select.dispatchEvent(new Event('change'));
    http.expectOne('/api/tasks/first').flush({}, { status: 500, statusText: 'Server error' });
    fixture.detectChanges();
    expect(select.value).toBe('Todo');
  });
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('prevents a second status write while the first write is pending', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    http.expectOne('/api/tasks').flush([first, second]);
    fixture.componentInstance.changeStatus(first, 'Completed');
    fixture.componentInstance.changeStatus(second, 'Completed');
    const writes = http.match((request) => request.method === 'PUT');
    expect(writes.length).toBe(1);
    writes.forEach((write) => write.flush(null));
    http.match('/api/tasks').forEach((read) => read.flush([first, second]));
  });

  it('cancels a superseded list request so stale responses cannot replace current tasks', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const old = http.expectOne('/api/tasks');
    fixture.componentInstance.load();
    const latest = http.expectOne('/api/tasks');
    expect(old.cancelled).toBe(true);
    latest.flush([{ ...first, status: 'Completed' }]);
    expect(fixture.componentInstance.tasks()[0]?.status).toBe('Completed');
  });
});
