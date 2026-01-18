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
