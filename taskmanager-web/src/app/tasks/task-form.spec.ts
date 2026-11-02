import { createTaskForm, taskInput } from './task-form';

describe('task editor validation', () => {
  it('rejects blank or overlong titles and descriptions', () => {
    const form = createTaskForm();
    expect(form.valid).toBe(false);
    form.controls.title.setValue('   ');
    expect(form.valid).toBe(false);
    form.controls.title.setValue('x'.repeat(121));
    expect(form.valid).toBe(false);
    form.controls.title.setValue('Interview');
    form.controls.description.setValue('x'.repeat(2001));
    expect(form.valid).toBe(false);
  });
  it('accepts overdue dates and sends an explicit UTC instant', () => {
    const form = createTaskForm();
    form.patchValue({ title: ' Interview ', dueDate: '2025-01-01T12:30', status: 'InProgress' });
    expect(form.valid).toBe(true);
    const input = taskInput(form);
    expect(input.title).toBe('Interview');
    expect(input.status).toBe('InProgress');
    expect(input.dueDateUtc).toBe(new Date(2025, 0, 1, 12, 30).toISOString());
  });
  it('omits an optional due date and rejects an invalid date', () => {
    const form = createTaskForm();
    form.controls.title.setValue('Interview');
    expect(taskInput(form).dueDateUtc).toBeNull();
    form.controls.dueDate.setValue('invalid');
    expect(form.valid).toBe(false);
  });
});
