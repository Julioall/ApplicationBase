import { occursOnDate, startOfDay } from './recurrence.util';
import { TodoRecurrenceType, TodoStatus, TodoTask } from '../../model/todo';

function makeTask(overrides: Partial<TodoTask>): TodoTask {
  return {
    Id: 'task-1',
    Title: 'Test task',
    Status: TodoStatus.NotStarted,
    CreatedAt: new Date(2026, 0, 1, 9, 0, 0).toISOString(),
    IsArchived: false,
    Steps: [],
    ...overrides,
  };
}

describe('recurrence.util', () => {
  it('returns start of day with time reset', () => {
    const date = new Date(2026, 0, 5, 15, 30, 45);
    const normalized = startOfDay(date);
    expect(normalized.getHours()).toBe(0);
    expect(normalized.getMinutes()).toBe(0);
    expect(normalized.getSeconds()).toBe(0);
  });

  describe('occursOnDate', () => {
    it('returns false when task has no start date', () => {
      const task = makeTask({ StartDate: null, DueDate: null });
      expect(occursOnDate(task, new Date(2026, 0, 10))).toBeFalse();
    });

    it('evaluates non recurring task within range', () => {
      const task = makeTask({
        StartDate: new Date(2026, 0, 10, 9, 0, 0).toISOString(),
        DueDate: new Date(2026, 0, 12, 18, 0, 0).toISOString(),
      });

      expect(occursOnDate(task, new Date(2026, 0, 11, 12, 0, 0))).toBeTrue();
      expect(occursOnDate(task, new Date(2026, 0, 9, 12, 0, 0))).toBeFalse();
      expect(occursOnDate(task, new Date(2026, 0, 13, 12, 0, 0))).toBeFalse();
    });

    it('handles daily recurrence interval', () => {
      const task = makeTask({
        StartDate: new Date(2026, 0, 1, 9, 0, 0).toISOString(),
        Recurrence: { Type: TodoRecurrenceType.Daily, Interval: 2 },
      });

      expect(occursOnDate(task, new Date(2026, 0, 1, 8, 0, 0))).toBeTrue();
      expect(occursOnDate(task, new Date(2026, 0, 2, 8, 0, 0))).toBeFalse();
      expect(occursOnDate(task, new Date(2026, 0, 3, 8, 0, 0))).toBeTrue();
    });

    it('skips weekends for weekdays recurrence', () => {
      const task = makeTask({
        StartDate: new Date(2026, 0, 5, 9, 0, 0).toISOString(), // Monday
        Recurrence: { Type: TodoRecurrenceType.Weekdays },
      });

      expect(occursOnDate(task, new Date(2026, 0, 7, 10, 0, 0))).toBeTrue(); // Wednesday
      expect(occursOnDate(task, new Date(2026, 0, 10, 10, 0, 0))).toBeFalse(); // Saturday
    });

    it('respects weekly days and interval', () => {
      const task = makeTask({
        StartDate: new Date(2026, 0, 5, 9, 0, 0).toISOString(), // Monday
        Recurrence: { Type: TodoRecurrenceType.Weekly, Interval: 2, DaysOfWeek: [1, 3] },
      });

      expect(occursOnDate(task, new Date(2026, 0, 12, 9, 0, 0))).toBeFalse(); // one week later
      expect(occursOnDate(task, new Date(2026, 0, 19, 9, 0, 0))).toBeTrue(); // two weeks later
      expect(occursOnDate(task, new Date(2026, 0, 21, 9, 0, 0))).toBeTrue(); // same cycle, allowed day
    });

    it('stops when recurrence ends', () => {
      const task = makeTask({
        StartDate: new Date(2026, 0, 5, 9, 0, 0).toISOString(),
        Recurrence: {
          Type: TodoRecurrenceType.Daily,
          EndsOn: new Date(2026, 0, 7, 0, 0, 0).toISOString(),
        },
      });

      expect(occursOnDate(task, new Date(2026, 0, 7, 12, 0, 0))).toBeTrue();
      expect(occursOnDate(task, new Date(2026, 0, 8, 12, 0, 0))).toBeFalse();
    });
  });
});
