import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../service/auth/auth.service';
import { TodoService } from '../../service/todo/todo.service';
import { MANAGE_TODO_PERMISSION, VIEW_TODO_PERMISSION } from '../../model/permissions';
import { CreateTodoTask, TodoStatus, TodoTask, UpdateTodoTask } from '../../model/todo';

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
  columns: TodoColumn[] = [];
  tasks: TodoTask[] = [];
  selectedTask: TodoTask | null = null;
  createForm: FormGroup;
  isCreateModalOpen = false;
  isLoading = false;
  connectedDropListIds: string[] = [];
  readonly statusIcons: Record<TodoStatus, string> = {
    [TodoStatus.NotStarted]: 'fa-regular fa-circle',
    [TodoStatus.InProgress]: 'fa-solid fa-spinner',
    [TodoStatus.Done]: 'fa-regular fa-circle-check',
  };

  constructor(
    private readonly todoService: TodoService,
    private readonly fb: FormBuilder,
    private readonly authService: AuthService
  ) {
    this.createForm = this.fb.group({
      Title: ['', Validators.required],
      Description: [''],
      Category: [''],
      Priority: [null],
      StartDate: [null],
      DueDate: [null],
    });
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
    const payload: CreateTodoTask = {
      Title: formValue.Title,
      Description: formValue.Description,
      Category: formValue.Category,
      Priority: formValue.Priority ? Number(formValue.Priority) : null,
      StartDate: formValue.StartDate || null,
      DueDate: formValue.DueDate || null,
    };

    this.todoService.createTask(payload).subscribe({
      next: (task) => {
        this.tasks = [task, ...this.tasks];
        this.refreshColumns();
        this.isCreateModalOpen = false;
        this.selectedTask = task;
      },
      error: () => {},
    });
  }

  selectTask(task: TodoTask): void {
    this.selectedTask = task;
  }

  closeDetail(): void {
    this.selectedTask = null;
  }

  handleTaskUpdated(task: TodoTask): void {
    this.replaceTask(task);
    this.refreshColumns();
    this.selectedTask = this.tasks.find(t => t.Id === task.Id) ?? null;
  }

  handleTaskArchived(taskId: string): void {
    this.tasks = this.tasks.filter(t => t.Id !== taskId);
    this.refreshColumns();
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

  private loadTasks(): void {
    if (!this.canView) {
      return;
    }
    this.isLoading = true;
    this.todoService.getTasks().subscribe({
      next: (tasks) => {
        this.tasks = tasks ?? [];
        this.refreshColumns();
        this.syncSelectedTask();
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

  getStatusClass(status: TodoStatus): string {
    if (status === TodoStatus.NotStarted) {
      return 'NotStarted';
    }
    if (status === TodoStatus.InProgress) {
      return 'InProgress';
    }
    return 'Done';
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
