import { TodoRecurrenceType, TodoTask } from '../../model/todo';

export function occursOnDate(task: TodoTask, target: Date): boolean {
  if (!task) return false;

  const anchor = pickAnchorDate(task);
  if (!anchor) return false;

  const targetDay = startOfDay(target);
  const start = task.StartDate ? startOfDay(new Date(task.StartDate)) : null;
  const end = task.DueDate ? startOfDay(new Date(task.DueDate)) : start;

  // Non-recurring: must be within start/end range
  if (!task.Recurrence || task.Recurrence.Type === undefined || task.Recurrence.Type === TodoRecurrenceType.None) {
    if (!start) return false;
    const endTime = end ?? start;
    return targetDay.getTime() >= start.getTime() && targetDay.getTime() <= endTime.getTime();
  }

  const recurrence = task.Recurrence;
  const interval = recurrence.Interval && recurrence.Interval > 0 ? recurrence.Interval : 1;
  const recurrenceEnd = recurrence.EndsOn ? startOfDay(new Date(recurrence.EndsOn)) : null;
  if (recurrenceEnd && targetDay.getTime() > recurrenceEnd.getTime()) {
    return false;
  }

  const diffDays = Math.floor((targetDay.getTime() - anchor.getTime()) / (1000 * 60 * 60 * 24));
  if (diffDays < 0) {
    return false;
  }

  switch (recurrence.Type) {
    case TodoRecurrenceType.Daily:
      return diffDays % interval === 0;
    case TodoRecurrenceType.Weekdays: {
      const dow = targetDay.getDay();
      return dow >= 1 && dow <= 5 && diffDays % interval === 0;
    }
    case TodoRecurrenceType.Weekly: {
      const weeksDiff = Math.floor(diffDays / 7);
      if (weeksDiff % interval !== 0) {
        return false;
      }
      const allowedDays =
        recurrence.DaysOfWeek && recurrence.DaysOfWeek.length > 0
          ? recurrence.DaysOfWeek
          : [anchor.getDay()];
      return allowedDays.includes(targetDay.getDay());
    }
    case TodoRecurrenceType.Monthly: {
      const monthsDiff = (targetDay.getFullYear() - anchor.getFullYear()) * 12 + (targetDay.getMonth() - anchor.getMonth());
      return monthsDiff >= 0 && monthsDiff % interval === 0 && targetDay.getDate() === anchor.getDate();
    }
    case TodoRecurrenceType.Yearly: {
      const yearsDiff = targetDay.getFullYear() - anchor.getFullYear();
      return yearsDiff >= 0 && yearsDiff % interval === 0 && targetDay.getMonth() === anchor.getMonth() && targetDay.getDate() === anchor.getDate();
    }
    default:
      return false;
  }
}

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function pickAnchorDate(task: TodoTask): Date | null {
  if (task.StartDate) {
    return startOfDay(new Date(task.StartDate));
  }
  if (task.DueDate) {
    return startOfDay(new Date(task.DueDate));
  }
  return null;
}
