import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TodoStatus, TodoTask } from '../../model/todo';
import { TranslateService } from '@ngx-translate/core';

type AgendaEntry = {
  task: TodoTask;
  start: Date | null;
  end: Date | null;
  isAllDay: boolean;
};

@Component({
  selector: 'app-todo-agenda',
  templateUrl: './todo-agenda.component.html',
})
export class TodoAgendaComponent {
  @Input() tasks: TodoTask[] = [];
  @Input() selectedDate: Date = new Date();
  @Input() canManage = false;
  @Output() taskSelected = new EventEmitter<TodoTask>();
  @Output() dateChange = new EventEmitter<Date>();

  TodoStatus = TodoStatus;

  constructor(private readonly translate: TranslateService) {}

  get agendaEntries(): AgendaEntry[] {
    const day = this.selectedDate;
    const entries: AgendaEntry[] = [];

    for (const task of this.tasks || []) {
      const entry = this.createEntryForDay(task, day);
      if (entry) {
        entries.push(entry);
      }
    }

    return entries.sort((a, b) => {
      if (a.isAllDay && !b.isAllDay) {
        return -1;
      }
      if (!a.isAllDay && b.isAllDay) {
        return 1;
      }
      const startA = a.start ? a.start.getTime() : 0;
      const startB = b.start ? b.start.getTime() : 0;
      return startA - startB;
    });
  }

  changeDay(delta: number): void {
    const newDate = new Date(this.selectedDate);
    newDate.setDate(newDate.getDate() + delta);
    this.dateChange.emit(newDate);
  }

  goToday(): void {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    this.dateChange.emit(today);
  }

  private createEntryForDay(task: TodoTask, day: Date): AgendaEntry | null {
    if (!this.occursOnDate(task, day)) {
      return null;
    }

    const baseStart = task.StartDate ? new Date(task.StartDate) : null;
    const baseEnd = task.DueDate ? new Date(task.DueDate) : null;
    const start = baseStart ? this.combineDateAndTime(day, baseStart) : baseEnd ? this.combineDateAndTime(day, baseEnd) : null;
    const end = baseEnd ? this.combineDateAndTime(day, baseEnd) : start;

    return {
      task,
      start,
      end,
      isAllDay: !!task.IsAllDay || (!start && !end),
    };
  }

  private occursOnDate(task: TodoTask, target: Date): boolean {
    const targetDay = this.startOfDay(target);
    const start = task.StartDate ? this.startOfDay(new Date(task.StartDate)) : null;
    const end = task.DueDate ? this.startOfDay(new Date(task.DueDate)) : null;

    const anchor = start ?? end;
    if (!anchor) {
      return false;
    }

    const endDate = end ?? start ?? anchor;
    const rangeStart = start ?? endDate;
    return targetDay.getTime() >= rangeStart.getTime() && targetDay.getTime() <= endDate.getTime();
  }

  getPriorityLabel(priority?: number | null): string {
    const value = priority === null || priority === undefined ? null : Number(priority);
    if (value === 1) {
      const text = this.translate.instant('todo.priority.low');
      return text !== 'todo.priority.low' ? text : 'Baixa';
    }
    if (value === 2) {
      const text = this.translate.instant('todo.priority.medium');
      return text !== 'todo.priority.medium' ? text : 'Media';
    }
    if (value === 3) {
      const text = this.translate.instant('todo.priority.high');
      return text !== 'todo.priority.high' ? text : 'Alta';
    }
    return '';
  }

  getPriorityClass(priority?: number | null): string {
    const value = priority === null || priority === undefined ? null : Number(priority);
    if (value === 1) return 'low';
    if (value === 2) return 'medium';
    if (value === 3) return 'high';
    return '';
  }

  private combineDateAndTime(day: Date, timeSource: Date): Date {
    const combined = new Date(day);
    combined.setHours(timeSource.getHours(), timeSource.getMinutes(), 0, 0);
    return combined;
  }

  private startOfDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }
}
