import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../service/auth/auth.service';
import { TodoService } from '../../service/todo/todo.service';
import { MANAGE_TODO_PERMISSION, VIEW_TODO_PERMISSION } from '../../model/permissions';
import { CreateTodoTask, TodoRecurrence, TodoRecurrenceType, TodoStatus, TodoTask, UpdateTodoTask } from '../../model/todo';
import { QuillModules } from 'ngx-quill';
import { NotificationService } from '../../service/notification/notification.service';
import { TranslateService } from '@ngx-translate/core';

type TodoColumn = {
  status: TodoStatus;
  label: string;
  id: string;
  items: TodoTask[];
};

@Component({
  selector: 'app-todo-board',
  templateUrl: './todo-board.component.html',
  styleUrls: ['./todo-board.component.scss'],
})
export class TodoBoardComponent implements OnInit {
  TodoStatus = TodoStatus;
  TodoRecurrenceType = TodoRecurrenceType;
  columns: TodoColumn[] = [];
  tasks: TodoTask[] = [];
  selectedTask: TodoTask | null = null;
  createForm: FormGroup;
  scheduleErrors: string[] = [];
  isCreateModalOpen = false;
  isScheduleDialogOpen = false;
  isLoading = false;
  viewMode: 'board' | 'agenda' = 'board';
  agendaDate: Date = new Date();
  connectedDropListIds: string[] = [];
  categoryOptions: string[] = [];
  selectedCategories: string[] = [];
  isUploadingImage = false;
  readonly priorityLevels = [1, 2, 3];
  readonly recurrenceOptions = [
    { value: TodoRecurrenceType.None, label: 'todo.recurrence.none' },
    { value: TodoRecurrenceType.Daily, label: 'todo.recurrence.daily' },
    { value: TodoRecurrenceType.Weekdays, label: 'todo.recurrence.weekdays' },
    { value: TodoRecurrenceType.Weekly, label: 'todo.recurrence.weekly' },
    { value: TodoRecurrenceType.Monthly, label: 'todo.recurrence.monthly' },
    { value: TodoRecurrenceType.Yearly, label: 'todo.recurrence.yearly' },
  ];
  readonly weekDays = [
    { value: 0, label: 'todo.weekdays.sun', short: 'S' },
    { value: 1, label: 'todo.weekdays.mon', short: 'M' },
    { value: 2, label: 'todo.weekdays.tue', short: 'T' },
    { value: 3, label: 'todo.weekdays.wed', short: 'W' },
    { value: 4, label: 'todo.weekdays.thu', short: 'T' },
    { value: 5, label: 'todo.weekdays.fri', short: 'F' },
    { value: 6, label: 'todo.weekdays.sat', short: 'S' },
  ];
  private quillEditor: any;
  readonly descriptionModules: QuillModules = {
    toolbar: {
      container: [
        [{ header: [1, 2, 3, false] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ color: [] }, { background: [] }],
        [{ list: 'ordered' }, { list: 'bullet' }],
        [{ indent: '-1' }, { indent: '+1' }],
        [{ align: [] }],
        [{ size: [] }],
        ['link', 'image'],
        ['clean'],
      ],
      handlers: {
        image: () => this.handleImageUpload(),
      },
    },
  };
  readonly statusIcons: Record<TodoStatus, string> = {
    [TodoStatus.NotStarted]: 'fa-regular fa-circle',
    [TodoStatus.InProgress]: 'fa-solid fa-spinner',
    [TodoStatus.Done]: 'fa-regular fa-circle-check',
  };

  constructor(
    private readonly todoService: TodoService,
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly notification: NotificationService,
    private readonly translate: TranslateService
  ) {
    this.createForm = this.fb.group({
      Title: ['', Validators.required],
      Description: [''],
      Category: [''],
      Priority: [1],
      StartDate: [null],
      DueDate: [null],
      IsAllDay: [false],
      RecurrenceType: [TodoRecurrenceType.None],
      RecurrenceInterval: [1],
      RecurrenceEndsOn: [null],
      RecurrenceDaysOfWeek: [[]],
    });
  }

  setPriority(priority: number | null): void {
    this.createForm.patchValue({ Priority: priority });
  }

  isPriorityActive(current?: number | null, level?: number): boolean {
    if (!current || !level) {
      return false;
    }
    return current >= level;
  }

  onEditorCreated(quill: any): void {
    this.quillEditor = quill;
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  get canManage(): boolean {
    return this.authService.hasPermission(MANAGE_TODO_PERMISSION);
  }

  get canView(): boolean {
    return this.authService.hasAnyPermission([VIEW_TODO_PERMISSION, MANAGE_TODO_PERMISSION]);
  }

  openCreate(): void {
    if (!this.canManage) {
      return;
    }
    this.isCreateModalOpen = true;
    this.isScheduleDialogOpen = false;
    this.createForm.reset();
    this.selectedCategories = [];
    this.scheduleErrors = [];
    this.createForm.patchValue({
      Priority: 1,
      IsAllDay: false,
      RecurrenceType: TodoRecurrenceType.None,
      RecurrenceInterval: 1,
      RecurrenceEndsOn: null,
      RecurrenceDaysOfWeek: [],
      StartDate: null,
      DueDate: null,
    });
  }

  cancelCreate(): void {
    this.isCreateModalOpen = false;
    this.isScheduleDialogOpen = false;
  }

  submitCreate(): void {
    if (!this.createForm.valid || !this.canManage) {
      this.createForm.markAllAsTouched();
      return;
    }

    const formValue = this.createForm.value;
    this.scheduleErrors = this.getScheduleValidationErrors(formValue);
    if (this.scheduleErrors.length > 0) {
      this.notification.showError(this.scheduleErrors[0]);
      return;
    }
    const startDate = this.normalizeDateValue(formValue.StartDate);
    const dueDate = this.normalizeDateValue(formValue.DueDate);
    const recurrence = this.buildRecurrenceFromForm(formValue, startDate);
    const payload: CreateTodoTask = {
      Title: formValue.Title,
      Description: formValue.Description,
      Category: this.selectedCategories[0] ?? formValue.Category ?? null,
      Categories: this.selectedCategories.length > 0 ? this.selectedCategories : null,
      Priority: formValue.Priority ? Number(formValue.Priority) : null,
      StartDate: startDate,
      DueDate: dueDate,
      IsAllDay: !!formValue.IsAllDay,
      Recurrence: recurrence,
    };

    this.todoService.createTask(payload).subscribe({
      next: (task) => {
        this.tasks = [task, ...this.tasks];
        this.refreshColumns();
        this.refreshCategories();
        this.isCreateModalOpen = false;
        this.selectedTask = task;
      },
      error: (err) => {
        const message = this.getCreateErrorMessage(err);
        this.notification.showError(message);
      },
    });
  }

  selectTask(task: TodoTask): void {
    this.selectedTask = task;
  }

  closeDetail(): void {
    this.selectedTask = null;
  }

  setViewMode(mode: 'board' | 'agenda'): void {
    this.viewMode = mode;
  }

  onAgendaDateChange(date: Date): void {
    this.agendaDate = date;
  }

  handleImageUpload(): void {
    if (!this.canManage) {
      return;
    }

    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.style.display = 'none';
    document.body.appendChild(input);

    input.onchange = () => {
      const file = input.files?.[0];
      document.body.removeChild(input);

      if (!file) {
        return;
      }

      this.isUploadingImage = true;
      this.todoService
        .uploadImage(file)
        .pipe(finalize(() => (this.isUploadingImage = false)))
        .subscribe({
          next: ({ url }) => {
            const editor = this.quillEditor;
            if (!editor) {
              return;
            }
            const range = editor.getSelection(true) || { index: editor.getLength(), length: 0 };
            editor.insertEmbed(range.index, 'image', url, 'user');
            editor.setSelection(range.index + 1);
          },
          error: () => {
            const message = this.translate.instant('todo.notifications.imageUploadFailed');
            this.notification.showError(message !== 'todo.notifications.imageUploadFailed' ? message : 'Não foi possível enviar a imagem.');
          },
        });
    };

    input.click();
  }

  onCategoryEnter(event: Event, input: HTMLInputElement): void {
    event.preventDefault();
    this.addCategory(input.value);
    input.value = '';
  }

  addCategory(value: string): void {
    const trimmed = value?.trim();
    if (!trimmed) {
      return;
    }

    const exists = this.selectedCategories.some((c) => c.localeCompare(trimmed, undefined, { sensitivity: 'accent' }) === 0);
    if (exists) {
      return;
    }

    this.selectedCategories = [...this.selectedCategories, trimmed];
  }

  removeCategory(category: string): void {
    this.selectedCategories = this.selectedCategories.filter((c) => c !== category);
  }

  getTaskCategories(task: TodoTask): string[] {
    if (task.Categories && task.Categories.length > 0) {
      return task.Categories;
    }
    return task.Category ? [task.Category] : [];
  }

  handleTaskUpdated(task: TodoTask): void {
    this.replaceTask(task);
    this.refreshColumns();
    this.refreshCategories();
    this.selectedTask = this.tasks.find(t => t.Id === task.Id) ?? null;
  }

  handleTaskArchived(taskId: string): void {
    this.tasks = this.tasks.filter(t => t.Id !== taskId);
    this.refreshColumns();
    this.refreshCategories();
    this.selectedTask = null;
  }

  onDrop(event: CdkDragDrop<TodoTask[]>, newStatus: TodoStatus): void {
    if (!this.canManage) {
      return;
    }

    if (event.previousContainer === event.container && event.previousIndex === event.currentIndex) {
      return;
    }

    const task = event.previousContainer.data[event.previousIndex];
    if (!task || task.Status === newStatus) {
      return;
    }

    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    task.Status = newStatus;
    const update: UpdateTodoTask = { Status: newStatus };

    this.todoService.updateTask(task.Id, update).subscribe({
      next: (updated) => {
        this.replaceTask(updated);
        this.refreshColumns();
        if (this.selectedTask?.Id === updated.Id) {
          this.selectedTask = updated;
        }
      },
      error: () => {
        this.loadTasks();
      },
    });
  }

  getCompletedSteps(task: TodoTask): number {
    return task.Steps?.filter(step => step.IsCompleted).length ?? 0;
  }

  getDueBadge(task: TodoTask): { key: string; params?: Record<string, any>; variant: 'success' | 'warning' | 'danger' } | null {
    if (!task.DueDate) {
      return null;
    }

    const now = new Date();
    const due = new Date(task.DueDate);
    const startOfToday = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
    const startOfDue = new Date(Date.UTC(due.getUTCFullYear(), due.getUTCMonth(), due.getUTCDate()));
    const diffMs = startOfDue.getTime() - startOfToday.getTime();
    const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
      return { key: 'todo.badge.dueToday', variant: 'warning' };
    }

    if (diffDays > 0) {
      return {
        key: 'todo.badge.dueIn',
        params: { count: diffDays },
        variant: 'success',
      };
    }

    return {
      key: 'todo.badge.dueAgo',
      params: { count: Math.abs(diffDays) },
      variant: 'danger',
    };
  }

  openScheduleDialog(): void {
    this.scheduleErrors = [];
    this.isScheduleDialogOpen = true;
  }

  closeScheduleDialog(): void {
    this.isScheduleDialogOpen = false;
  }

  getScheduleSummary(form: FormGroup): string {
    const value = form.value;
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

  private getScheduleValidationErrors(formValue: any): string[] {
    const errors: string[] = [];
    const start = formValue.StartDate ? new Date(formValue.StartDate) : null;
    const due = formValue.DueDate ? new Date(formValue.DueDate) : null;
    const recurrenceEnd = formValue.RecurrenceEndsOn ? new Date(formValue.RecurrenceEndsOn) : null;
    const recurrenceType = formValue.RecurrenceType;

    if (start && due && start.getTime() > due.getTime()) {
      const msg = this.translate.instant('todo.errors.scheduleRange');
      errors.push(msg !== 'todo.errors.scheduleRange' ? msg : 'O término deve ser após o início.');
    }

    const hasRecurrence = recurrenceType !== undefined && recurrenceType !== null && recurrenceType !== TodoRecurrenceType.None;
    const anchor = start ?? due;

    if (hasRecurrence && !anchor) {
      const msg = this.translate.instant('todo.errors.recurrenceAnchor');
      errors.push(msg !== 'todo.errors.recurrenceAnchor' ? msg : 'Defina uma data de início ou término para recorrência.');
    }

    if (hasRecurrence && recurrenceEnd && anchor && recurrenceEnd.getTime() < anchor.getTime()) {
      const msg = this.translate.instant('todo.errors.recurrenceEndsOn');
      errors.push(msg !== 'todo.errors.recurrenceEndsOn' ? msg : 'A data de término da recorrência deve ser depois do início.');
    }

    const weekly = recurrenceType === TodoRecurrenceType.Weekly;
    const weeklyDays: number[] = Array.isArray(formValue.RecurrenceDaysOfWeek) ? formValue.RecurrenceDaysOfWeek : [];
    if (weekly && weeklyDays.length === 0 && !start) {
      const msg = this.translate.instant('todo.errors.recurrenceWeeklyDays');
      errors.push(msg !== 'todo.errors.recurrenceWeeklyDays' ? msg : 'Selecione ao menos um dia para repetição semanal.');
    }

    return errors;
  }

  private getCreateErrorMessage(error: any): string {
    const apiTitle = error?.error?.title || error?.error?.Title;
    const apiDetail = error?.error?.detail || error?.error?.Detail;
    if (apiTitle && apiDetail) {
      return `${apiTitle}: ${apiDetail}`;
    }
    if (apiDetail) {
      return apiDetail;
    }
    const fallback = this.translate.instant('todo.errors.createFailed');
    return fallback !== 'todo.errors.createFailed'
      ? fallback
      : 'Não foi possível criar a tarefa. Verifique datas e recorrência.';
  }

  private normalizeDateValue(value: any): string | null {
    if (value === undefined || value === null || value === '') {
      return null;
    }
    return value;
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

  loadTasks(): void {
    if (!this.canView) {
      return;
    }
    this.isLoading = true;
    this.todoService.getTasks().subscribe({
      next: (tasks) => {
            this.tasks = tasks ?? [];
            this.refreshColumns();
            this.syncSelectedTask();
            this.refreshCategories();
            this.isLoading = false;
          },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  private syncSelectedTask(): void {
    if (!this.selectedTask) {
      return;
    }

    const updated = this.tasks.find(t => t.Id === this.selectedTask?.Id);
    if (updated) {
      this.selectedTask = updated;
    } else {
      this.selectedTask = null;
    }
  }

  private replaceTask(updated: TodoTask): void {
    const index = this.tasks.findIndex(t => t.Id === updated.Id);
    if (index >= 0) {
      this.tasks[index] = updated;
    } else {
      this.tasks = [updated, ...this.tasks];
    }
  }

  private refreshColumns(): void {
    this.columns = [
      {
        status: TodoStatus.NotStarted,
        label: 'todo.status.notStarted',
        id: 'todo-not-started',
        items: this.tasks.filter(t => t.Status === TodoStatus.NotStarted && !t.IsArchived),
      },
      {
        status: TodoStatus.InProgress,
        label: 'todo.status.inProgress',
        id: 'todo-in-progress',
        items: this.tasks.filter(t => t.Status === TodoStatus.InProgress && !t.IsArchived),
      },
      {
        status: TodoStatus.Done,
        label: 'todo.status.done',
        id: 'todo-done',
        items: this.tasks.filter(t => t.Status === TodoStatus.Done && !t.IsArchived),
      },
    ];
    this.connectedDropListIds = this.columns.map(c => c.id);
  }

  private refreshCategories(): void {
    const categories = new Set<string>();
    this.tasks.forEach(task => {
      const taskCategories = task.Categories && task.Categories.length > 0 ? task.Categories : (task.Category ? [task.Category] : []);
      taskCategories.forEach((cat: string | null | undefined) => {
        if (cat) {
          categories.add(cat);
        }
      });
    });
    this.categoryOptions = Array.from(categories).sort((a, b) => a.localeCompare(b));
  }

  getStatusClass(status: TodoStatus): string {
    if (status === TodoStatus.NotStarted) {
      return 'NotStarted';
    }
    if (status === TodoStatus.InProgress) {
      return 'InProgress';
    }
    return 'Done';
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

  getCompletionPercent(task: TodoTask): number {
    if (!task.Steps || task.Steps.length === 0) {
      return 0;
    }
    const total = task.Steps.length;
    const done = this.getCompletedSteps(task);
    return Math.round((done / total) * 100);
  }
}
