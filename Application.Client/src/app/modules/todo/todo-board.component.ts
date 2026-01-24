import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../service/auth/auth.service';
import { TodoService } from '../../service/todo/todo.service';
import { UserService } from '../../service/user/user.service';
import { MANAGE_TODO_PERMISSION, VIEW_TODO_PERMISSION } from '../../model/permissions';
import { CreateTodoTask, TodoStatus, TodoTask, UpdateTodoTask, TodoAssignee } from '../../model/todo';
import { NotificationService } from '../../service/notification/notification.service';
import { TranslateService } from '@ngx-translate/core';
import ClassicEditor from '@ckeditor/ckeditor5-build-classic';

type TodoColumn = {
  status: TodoStatus;
  label: string;
  id: string;
  items: TodoTask[];
};

@Component({
  selector: 'app-todo-board',
  templateUrl: './todo-board.component.html',
})
export class TodoBoardComponent implements OnInit {
  TodoStatus = TodoStatus;
  columns: TodoColumn[] = [];
  tasks: TodoTask[] = [];
  selectedTask: TodoTask | null = null;
  createForm: FormGroup;
  isCreateModalOpen = false;
  isLoading = false;
  viewMode: 'board' | 'agenda' = 'board';
  agendaDate: Date = new Date();
  connectedDropListIds: string[] = [];
  categoryOptions: string[] = [];
  selectedCategories: string[] = [];
  selectedAssignees: TodoAssignee[] = [];
  assigneeOptions: TodoAssignee[] = [];
  assigneeSuggestions: TodoAssignee[] = [];
  assigneeQuery = '';
  isLoadingAssignees = false;
  private assigneeMap: Record<string, TodoAssignee> = {};
  readonly priorityLevels = [1, 2, 3];
  Editor = ClassicEditor;
  editorConfig = {
    toolbar: [
      'heading',
      '|',
      'bold',
      'italic',
      'underline',
      'link',
      'bulletedList',
      'numberedList',
      '|',
      'blockQuote',
      'code',
      'insertTable',
      '|',
      'undo',
      'redo',
    ],
    placeholder: '',
  };
  readonly statusIcons: Record<TodoStatus, string> = {
    [TodoStatus.NotStarted]: 'fa-regular fa-circle',
    [TodoStatus.InProgress]: 'fa-solid fa-spinner',
    [TodoStatus.Done]: 'fa-regular fa-circle-check',
  };
  selectedMobileStatus: TodoStatus = TodoStatus.NotStarted;

  constructor(
    private readonly todoService: TodoService,
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly notification: NotificationService,
    private readonly translate: TranslateService,
    private readonly userService: UserService
  ) {
    this.createForm = this.fb.group({
      Title: ['', Validators.required],
      Description: [''],
      Category: [''],
      Priority: [1],
      StartDate: [null],
      DueDate: [null],
      IsAllDay: [false],
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
    this.createForm.reset();
    this.selectedCategories = [];
    this.selectedAssignees = [];
    this.assigneeQuery = '';
    this.assigneeSuggestions = [];
    this.loadAssigneeOptions();
    this.createForm.patchValue({
      Priority: 1,
      IsAllDay: false,
      StartDate: null,
      DueDate: null,
    });
  }

  cancelCreate(): void {
    this.isCreateModalOpen = false;
  }

  submitCreate(): void {
    if (!this.createForm.valid || !this.canManage) {
      this.createForm.markAllAsTouched();
      return;
    }

    const formValue = this.createForm.value;
    const scheduleErrors = this.getScheduleValidationErrors(formValue);
    if (scheduleErrors.length > 0) {
      this.notification.showError(scheduleErrors[0]);
      return;
    }
    const startDate = this.normalizeDateValue(formValue.StartDate);
    const dueDate = this.normalizeDateValue(formValue.DueDate);
    const payload: CreateTodoTask = {
      Title: formValue.Title,
      Description: formValue.Description,
      Category: this.selectedCategories[0] ?? formValue.Category ?? null,
      Categories: this.selectedCategories.length > 0 ? this.selectedCategories : null,
      Priority: formValue.Priority ? Number(formValue.Priority) : null,
      StartDate: startDate,
      DueDate: dueDate,
      IsAllDay: !!formValue.IsAllDay,
      Assignees: this.selectedAssignees.length ? this.selectedAssignees : null,
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

  setMobileStatus(status: TodoStatus): void {
    this.selectedMobileStatus = status;
  }

  getMobileColumn(): TodoColumn | undefined {
    return this.columns.find(col => col.status === this.selectedMobileStatus);
  }

  onAgendaDateChange(date: Date): void {
    this.agendaDate = date;
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

  onAssigneeEnter(event: Event, input: HTMLInputElement): void {
    event.preventDefault();
    this.addAssignee(input.value);
    input.value = '';
  }

  addAssignee(value: string | TodoAssignee): void {
    const name = typeof value === 'string' ? value?.trim() : value?.Name?.trim();
    if (!name) {
      return;
    }
    const exists = this.selectedAssignees.some((a) => a.Name.localeCompare(name, undefined, { sensitivity: 'accent' }) === 0);
    if (exists) {
      return;
    }
    const avatarUrl = typeof value === 'string' ? null : value?.AvatarUrl ?? null;
    const id = typeof value === 'string' ? null : value?.Id ?? null;
    this.selectedAssignees = [...this.selectedAssignees, { Name: name, AvatarUrl: avatarUrl, Id: id }];
    this.assigneeQuery = '';
    this.assigneeSuggestions = [];
  }

  removeAssignee(name: string): void {
    this.selectedAssignees = this.selectedAssignees.filter((a) => a.Name !== name);
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

  private getScheduleValidationErrors(formValue: any): string[] {
    const errors: string[] = [];
    const start = formValue.StartDate ? new Date(formValue.StartDate) : null;
    const due = formValue.DueDate ? new Date(formValue.DueDate) : null;

    if (start && due && start.getTime() > due.getTime()) {
      const msg = this.translate.instant('todo.errors.scheduleRange');
      errors.push(msg !== 'todo.errors.scheduleRange' ? msg : 'O termino deve ser apos o inicio.');
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
      : 'Nao foi possivel criar a tarefa. Verifique as datas.';
  }

  private normalizeDateValue(value: any): string | null {
    if (value === undefined || value === null || value === '') {
      return null;
    }
    return value;
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
            this.updateAssigneeMapFromTasks(this.tasks);
            this.loadAssigneeOptions(true);
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

  getCompletionPercent(task: TodoTask): number {
    if (!task.Steps || task.Steps.length === 0) {
      return 0;
    }
    const total = task.Steps.length;
    const done = this.getCompletedSteps(task);
    return Math.round((done / total) * 100);
  }

  getAssignees(task: TodoTask): TodoAssignee[] {
    const direct = (task.Assignees || [])
      .map(a => this.enrichAssignee(a))
      .filter((a): a is TodoAssignee => !!a && !!a.Name);
    if (direct.length) {
      return direct;
    }
    const fallbackId = task.AssignedToUserId || task.CreatedByUserId;
    if (fallbackId && this.assigneeMap[fallbackId]) {
      return [this.assigneeMap[fallbackId]];
    }
    return [];
  }

  getAssigneeInitials(name: string | null | undefined): string {
    if (!name) {
      return '';
    }
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) {
      return parts[0].substring(0, 2).toUpperCase();
    }
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  onAssigneeInputChange(value: string): void {
    this.assigneeQuery = value;
    this.updateAssigneeSuggestions();
  }

  selectAssigneeSuggestion(option: TodoAssignee): void {
    this.addAssignee(option);
  }

  private updateAssigneeSuggestions(): void {
    const term = this.assigneeQuery.trim().toLowerCase();
    if (!term) {
      this.assigneeSuggestions = [];
      return;
    }
    const alreadySelected = new Set(this.selectedAssignees.map(a => a.Name.toLowerCase()));
    this.assigneeSuggestions = this.assigneeOptions
      .filter(opt => !alreadySelected.has(opt.Name.toLowerCase()))
      .filter(opt => opt.Name.toLowerCase().includes(term))
      .slice(0, 5);
  }

  private loadAssigneeOptions(force = false): void {
    if ((!force && this.assigneeOptions.length > 0) || this.isLoadingAssignees) {
      return;
    }
    this.isLoadingAssignees = true;
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.assigneeOptions = (users || []).map((u) => ({
          Id: u.Id ?? null,
          Name: u.Profile?.Name || u.Account?.Email || 'User',
          AvatarUrl: u.Profile?.ProfilePictureUrl ?? null,
        }));
        this.rebuildAssigneeMap();
        this.isLoadingAssignees = false;
        this.updateAssigneeSuggestions();
      },
      error: () => {
        this.isLoadingAssignees = false;
      },
    });
  }

  private rebuildAssigneeMap(): void {
    this.assigneeMap = {};
    this.assigneeOptions.forEach(opt => {
      if (opt.Id) {
        this.assigneeMap[opt.Id] = opt;
      }
    });
  }

  private updateAssigneeMapFromTasks(tasks: TodoTask[]): void {
    tasks.forEach(task => {
      (task.Assignees || []).forEach(a => {
        if (a?.Id) {
          this.assigneeMap[a.Id] = {
            Id: a.Id,
            Name: a.Name,
            AvatarUrl: a.AvatarUrl ?? this.assigneeMap[a.Id]?.AvatarUrl ?? null,
          };
        }
      });
    });
  }

  private enrichAssignee(assignee: TodoAssignee | null | undefined): TodoAssignee | null {
    if (!assignee) {
      return null;
    }
    const id = assignee.Id;
    if (assignee.Name) {
      return assignee;
    }
    if (id && this.assigneeMap[id]) {
      return this.assigneeMap[id];
    }
    return null;
  }
}

