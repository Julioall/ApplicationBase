import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { TodoService } from '../../service/todo/todo.service';
import { TodoRecurrence, TodoRecurrenceType, TodoStatus, TodoStep, TodoTask, UpdateTodoTask } from '../../model/todo';

@Component({
  selector: 'app-todo-task-detail',
  templateUrl: './todo-task-detail.component.html',
  styleUrls: ['./todo-task-detail.component.scss'],
})
export class TodoTaskDetailComponent implements OnChanges {
  @Input() task: TodoTask | null = null;
  @Input() canManage = true;
  @Output() closed = new EventEmitter<void>();
  @Output() taskUpdated = new EventEmitter<TodoTask>();
  @Output() archived = new EventEmitter<string>();
  @Output() refreshRequested = new EventEmitter<void>();

  TodoStatus = TodoStatus;
  TodoRecurrenceType = TodoRecurrenceType;

  editForm: FormGroup;
  isEditing = false;
  isScheduleDialogOpen = false;
  readonly weekDays = [
    { value: 0, label: 'todo.weekdays.sun', short: 'S' },
    { value: 1, label: 'todo.weekdays.mon', short: 'M' },
    { value: 2, label: 'todo.weekdays.tue', short: 'T' },
    { value: 3, label: 'todo.weekdays.wed', short: 'W' },
    { value: 4, label: 'todo.weekdays.thu', short: 'T' },
    { value: 5, label: 'todo.weekdays.fri', short: 'F' },
    { value: 6, label: 'todo.weekdays.sat', short: 'S' },
  ];
  readonly recurrenceOptions = [
    { value: TodoRecurrenceType.None, label: 'todo.recurrence.none' },
    { value: TodoRecurrenceType.Daily, label: 'todo.recurrence.daily' },
    { value: TodoRecurrenceType.Weekdays, label: 'todo.recurrence.weekdays' },
    { value: TodoRecurrenceType.Weekly, label: 'todo.recurrence.weekly' },
    { value: TodoRecurrenceType.Monthly, label: 'todo.recurrence.monthly' },
    { value: TodoRecurrenceType.Yearly, label: 'todo.recurrence.yearly' },
  ];

  readonly statuses = [
    { value: TodoStatus.NotStarted, label: 'todo.status.notStarted' },
    { value: TodoStatus.InProgress, label: 'todo.status.inProgress' },
    { value: TodoStatus.Done, label: 'todo.status.done' },
  ];
  readonly priorityLevels = [1, 2, 3];

  constructor(
    private readonly todoService: TodoService,
    private readonly translate: TranslateService,
    private readonly fb: FormBuilder
  ) {
    this.editForm = this.fb.group({
      Title: ['', Validators.required],
      Description: [''],
      Priority: [null],
      Status: [TodoStatus.NotStarted, Validators.required],
      StartDate: [null],
      DueDate: [null],
      IsAllDay: [false],
      RecurrenceType: [TodoRecurrenceType.None],
      RecurrenceInterval: [1],
      RecurrenceEndsOn: [null],
      RecurrenceDaysOfWeek: [[]],
      ApplyToSeries: [false],
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['task'] && this.task) {
      this.patchFormFromTask(this.task);
      this.isEditing = false;
      this.isScheduleDialogOpen = false;
    }
  }

  close(): void {
    this.isEditing = false;
    this.isScheduleDialogOpen = false;
    this.closed.emit();
  }

  startEdit(): void {
    if (!this.task || !this.canManage) {
      return;
    }
    this.isEditing = true;
    this.isScheduleDialogOpen = false;
    this.patchFormFromTask(this.task);
  }

  cancelEdit(): void {
    this.isEditing = false;
    this.isScheduleDialogOpen = false;
    if (this.task) {
      this.patchFormFromTask(this.task);
    }
  }

  openScheduleDialog(): void {
    this.isScheduleDialogOpen = true;
  }

  closeScheduleDialog(): void {
    this.isScheduleDialogOpen = false;
  }

  getScheduleSummary(): string {
    const value = this.editForm.value;
    const allDay = !!value.IsAllDay;
    const start = this.formatScheduleDisplay(value.StartDate, allDay);
    const end = this.formatScheduleDisplay(value.DueDate, allDay);

    let summary = start;
    if (end) {
      summary = summary ? `${summary} -> ${end}` : end;
    }
    if (!summary) {
      summary = this.translate.instant('todo.schedule.unset');
    }

    const recurrence = this.buildRecurrencePreview(value);
    if (recurrence) {
      summary = `${summary} | ${this.getRecurrenceLabel(recurrence)}`;
    }
    return summary;
  }

  submitEdit(): void {
    if (!this.task || !this.canManage) {
      return;
    }

    if (!this.editForm.valid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const formValue = this.editForm.value;
    const startDate = this.normalizeDateValue(formValue.StartDate ?? this.task.StartDate ?? null);
    const dueDate = this.normalizeDateValue(formValue.DueDate ?? this.task.DueDate ?? null);
    const recurrence = this.buildRecurrenceFromForm(formValue, startDate ?? this.task.StartDate ?? null);
    const payload: UpdateTodoTask = {
      Title: formValue.Title,
      Description: formValue.Description ?? null,
      Priority: formValue.Priority ? Number(formValue.Priority) : null,
      Status: formValue.Status,
      StartDate: startDate,
      DueDate: dueDate,
      IsAllDay: !!formValue.IsAllDay,
      Recurrence: recurrence,
      ApplyToSeries: !!formValue.ApplyToSeries && !!this.task.RecurrenceGroupId,
    };

    this.todoService.updateTask(this.task.Id, payload).subscribe({
      next: (updated) => {
        const appliedSeries = payload.ApplyToSeries;
        this.isEditing = false;
        this.isScheduleDialogOpen = false;
        this.emitUpdated(updated);
        if (appliedSeries) {
          this.refreshRequested.emit();
        }
      },
      error: () => {},
    });
  }

  updateStatus(status: TodoStatus): void {
    if (!this.task || !this.canManage) {
      return;
    }

    this.todoService.updateTask(this.task.Id, { Status: status }).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  onToggleStep(event: { stepId: string; isCompleted: boolean }): void {
    if (!this.task || !this.canManage) {
      return;
    }

    this.todoService.updateStep(this.task.Id, event.stepId, { IsCompleted: event.isCompleted }).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  onTitleChange(event: { stepId: string; newTitle: string }): void {
    if (!this.task || !this.canManage) {
      return;
    }

    if (!event.newTitle || !event.newTitle.trim()) {
      return;
    }

    this.todoService.updateStep(this.task.Id, event.stepId, { Title: event.newTitle.trim() }).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  onReorderSteps(steps: TodoStep[]): void {
    if (!this.task || !this.canManage || !steps || steps.length === 0) {
      return;
    }

    const ids = steps.map((s) => s.Id);
    this.todoService.reorderSteps(this.task.Id, { StepIds: ids }).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  onAddStep(title: string): void {
    if (!this.task || !this.canManage) {
      return;
    }

    if (!title || !title.trim()) {
      return;
    }

    this.todoService.addStep(this.task.Id, { Title: title.trim() }).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  onDeleteStep(stepId: string): void {
    if (!this.task || !this.canManage) {
      return;
    }

    this.todoService.deleteStep(this.task.Id, stepId).subscribe({
      next: (updated) => this.emitUpdated(updated),
      error: () => {},
    });
  }

  archive(): void {
    if (!this.task || !this.canManage) {
      return;
    }

    const taskId = this.task.Id;
    this.todoService.archiveTask(taskId).subscribe({
      next: () => this.archived.emit(taskId),
      error: () => {},
    });
  }

  getCompletedSteps(): number {
    if (!this.task?.Steps) {
      return 0;
    }
    return this.task.Steps.filter((s) => s.IsCompleted).length;
  }

  getStatusLabel(status: TodoStatus): string {
    const translation = this.translate.instant(this.statuses.find((s) => s.value === status)?.label ?? '');
    return translation || TodoStatus[status];
  }

  getStatusClass(status: TodoStatus): string {
    if (status === TodoStatus.NotStarted) return 'NotStarted';
    if (status === TodoStatus.InProgress) return 'InProgress';
    return 'Done';
  }

  isPriorityActive(current?: number | null, level?: number): boolean {
    if (!current || !level) {
      return false;
    }
    return current >= level;
  }

  getPriorityLabel(priority?: number | null): string {
    if (priority === 1) {
      const text = this.translate.instant('todo.priority.low');
      return text !== 'todo.priority.low' ? text : 'Baixa';
    }
    if (priority === 2) {
      const text = this.translate.instant('todo.priority.medium');
      return text !== 'todo.priority.medium' ? text : 'Media';
    }
    if (priority === 3) {
      const text = this.translate.instant('todo.priority.high');
      return text !== 'todo.priority.high' ? text : 'Alta';
    }
    return '';
  }

  getPriorityClass(priority?: number | null): string {
    if (priority === 1) return 'low';
    if (priority === 2) return 'medium';
    if (priority === 3) return 'high';
    return '';
  }

  getTaskCategories(): string[] {
    if (this.task?.Categories && this.task.Categories.length > 0) {
      return this.task.Categories;
    }
    if (this.task?.Category) {
      return [this.task.Category];
    }
    return [];
  }

  getRecurrenceLabel(recurrence?: TodoRecurrence | null): string {
    if (!recurrence || recurrence.Type === undefined || recurrence.Type === TodoRecurrenceType.None) {
      return this.translate.instant('todo.recurrence.none');
    }

    switch (recurrence.Type) {
      case TodoRecurrenceType.Daily:
        return this.translate.instant('todo.recurrence.daily');
      case TodoRecurrenceType.Weekdays:
        return this.translate.instant('todo.recurrence.weekdays');
      case TodoRecurrenceType.Weekly: {
        const interval = recurrence.Interval && recurrence.Interval > 1 ? recurrence.Interval : 1;
        if (interval > 1) {
          return this.translate.instant('todo.recurrence.everyNWeeks', { count: interval });
        }
        return this.translate.instant('todo.recurrence.weekly');
      }
      case TodoRecurrenceType.Monthly:
        return this.translate.instant('todo.recurrence.monthly');
      case TodoRecurrenceType.Yearly:
        return this.translate.instant('todo.recurrence.yearly');
      default:
        return this.translate.instant('todo.recurrence.custom');
    }
  }

  private patchFormFromTask(task: TodoTask): void {
    const recurrence = task.Recurrence;
    this.editForm.patchValue(
      {
        Title: task.Title,
        Description: task.Description ?? null,
        Priority: task.Priority ?? null,
        Status: task.Status,
        StartDate: this.formatDateTimeInput(task.StartDate),
        DueDate: this.formatDateTimeInput(task.DueDate),
        IsAllDay: !!task.IsAllDay,
        RecurrenceType: recurrence?.Type ?? TodoRecurrenceType.None,
        RecurrenceInterval: recurrence?.Interval ?? 1,
        RecurrenceEndsOn: this.formatDateInput(recurrence?.EndsOn ?? null),
        RecurrenceDaysOfWeek: recurrence?.DaysOfWeek ?? [],
        ApplyToSeries: false,
      },
      { emitEvent: false }
    );
  }

  private normalizeDateValue(value: any): string | null {
    if (value === undefined || value === null || value === '') {
      return null;
    }
    return value;
  }

  private formatDateTimeInput(value?: string | null): string | null {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return null;
    }
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    const hours = `${date.getHours()}`.padStart(2, '0');
    const minutes = `${date.getMinutes()}`.padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private formatDateInput(value?: string | null): string | null {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return null;
    }
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private formatScheduleDisplay(value: string | null, allDay: boolean): string {
    if (!value) {
      return '';
    }
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    const options: Intl.DateTimeFormatOptions = allDay
      ? { day: '2-digit', month: '2-digit', year: 'numeric' }
      : { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' };
    return date.toLocaleString(undefined, options);
  }

  private buildRecurrencePreview(formValue: any): TodoRecurrence | null {
    if (!formValue) {
      return null;
    }
    const type = formValue.RecurrenceType;
    if (type === undefined || type === null || type === TodoRecurrenceType.None) {
      return null;
    }
    return {
      Type: type,
      Interval: formValue.RecurrenceInterval,
      EndsOn: formValue.RecurrenceEndsOn,
      DaysOfWeek: formValue.RecurrenceDaysOfWeek,
    };
  }

  private buildRecurrenceFromForm(formValue: any, startDate: string | null): TodoRecurrence | null {
    const type = formValue.RecurrenceType;
    if (type === undefined || type === null || type === TodoRecurrenceType.None) {
      return null;
    }

    const interval = formValue.RecurrenceInterval && Number(formValue.RecurrenceInterval) > 0 ? Number(formValue.RecurrenceInterval) : 1;
    let days: number[] | null = null;

    if (type === TodoRecurrenceType.Weekly) {
      const selected = (formValue.RecurrenceDaysOfWeek as number[] | null) ?? [];
      if (selected.length > 0) {
        days = selected;
      } else if (startDate) {
        days = [new Date(startDate).getDay()];
      }
    }

    if (type === TodoRecurrenceType.Weekdays) {
      days = [1, 2, 3, 4, 5];
    }

    return {
      Type: type,
      Interval: interval,
      EndsOn: this.normalizeDateValue(formValue.RecurrenceEndsOn),
      DaysOfWeek: days,
    };
  }

  private emitUpdated(task: TodoTask): void {
    this.task = task;
    this.patchFormFromTask(task);
    this.taskUpdated.emit(task);
  }

  deleteTask(): void {
    if (!this.task || !this.canManage) {
      return;
    }
    const taskId = this.task.Id;
    this.todoService.deleteTask(taskId).subscribe({
      next: () => this.archived.emit(taskId),
      error: () => {},
    });
  }
}
