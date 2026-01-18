import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { TodoService } from '../../service/todo/todo.service';
import { TodoStatus, TodoStep, TodoTask, UpdateTodoTask } from '../../model/todo';
import ClassicEditor from '@ckeditor/ckeditor5-build-classic';

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

  editForm: FormGroup;
  isEditing = false;
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

  readonly statuses = [
    { value: TodoStatus.NotStarted, label: 'todo.status.notStarted' },
    { value: TodoStatus.InProgress, label: 'todo.status.inProgress' },
    { value: TodoStatus.Done, label: 'todo.status.done' },
  ];
  private readonly statusLabelKeys: Record<TodoStatus, string> = {
    [TodoStatus.NotStarted]: 'todo.status.notStarted',
    [TodoStatus.InProgress]: 'todo.status.inProgress',
    [TodoStatus.Done]: 'todo.status.done',
  };
  private readonly statusValues = new Set<TodoStatus>([
    TodoStatus.NotStarted,
    TodoStatus.InProgress,
    TodoStatus.Done,
  ]);
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
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['task'] && this.task) {
      this.patchFormFromTask(this.task);
      this.isEditing = false;
    }
  }

  close(): void {
    this.isEditing = false;
    this.closed.emit();
  }

  startEdit(): void {
    if (!this.task || !this.canManage) {
      return;
    }
    this.isEditing = true;
    this.patchFormFromTask(this.task);
    this.editorConfig = {
      ...this.editorConfig,
      placeholder: this.translate.instant('todo.fields.descriptionPlaceholder'),
    };
  }

  cancelEdit(): void {
    this.isEditing = false;
    if (this.task) {
      this.patchFormFromTask(this.task);
    }
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
    const payload: UpdateTodoTask = {
      Title: formValue.Title,
      Description: formValue.Description ?? null,
      Priority: formValue.Priority ? Number(formValue.Priority) : null,
      Status: formValue.Status,
      StartDate: startDate,
      DueDate: dueDate,
      IsAllDay: !!formValue.IsAllDay,
    };

    this.todoService.updateTask(this.task.Id, payload).subscribe({
      next: (updated) => {
        this.isEditing = false;
        this.emitUpdated(updated);
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

  getStatusLabel(status: TodoStatus | string | null | undefined): string {
    const normalized = this.normalizeStatus(status);
    if (normalized !== null) {
      const key = this.statusLabelKeys[normalized];
      const translation = this.translate.instant(key);
      if (translation && translation !== key) {
        return translation;
      }
      const fallback = TodoStatus[normalized];
      if (fallback) {
        return fallback.replace(/([A-Z])/g, ' $1').trim();
      }
    }
    return typeof status === 'string' ? status : '';
  }

  getStatusClass(status: TodoStatus | string | null | undefined): string {
    const normalized = this.normalizeStatus(status);
    if (normalized === TodoStatus.NotStarted) return 'NotStarted';
    if (normalized === TodoStatus.InProgress) return 'InProgress';
    if (normalized === TodoStatus.Done) return 'Done';
    return '';
  }

  isPriorityActive(current?: number | null, level?: number): boolean {
    if (!current || !level) {
      return false;
    }
    return current >= level;
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

  getTaskCategories(): string[] {
    if (this.task?.Categories && this.task.Categories.length > 0) {
      return this.task.Categories;
    }
    if (this.task?.Category) {
      return [this.task.Category];
    }
    return [];
  }

  private normalizeStatus(status: TodoStatus | string | null | undefined): TodoStatus | null {
    if (status === null || status === undefined) {
      return null;
    }

    if (typeof status === 'number' && this.statusValues.has(status)) {
      return status;
    }

    const numeric = Number(status);
    if (!Number.isNaN(numeric) && this.statusValues.has(numeric as TodoStatus)) {
      return numeric as TodoStatus;
    }

    const key = status.toString().toLowerCase().replace(/[\s_-]/g, '');
    if (key === 'notstarted') {
      return TodoStatus.NotStarted;
    }
    if (key === 'inprogress') {
      return TodoStatus.InProgress;
    }
    if (key === 'done' || key === 'completed' || key === 'concluida') {
      return TodoStatus.Done;
    }
    return null;
  }

  private patchFormFromTask(task: TodoTask): void {
    this.editForm.patchValue(
      {
        Title: task.Title,
        Description: task.Description ?? null,
        Priority: task.Priority ?? null,
        Status: task.Status,
        StartDate: this.formatDateTimeInput(task.StartDate),
        DueDate: this.formatDateTimeInput(task.DueDate),
        IsAllDay: !!task.IsAllDay,
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
