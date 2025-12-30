import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TodoService } from '../../service/todo/todo.service';
import { TodoStatus, TodoStep, TodoTask } from '../../model/todo';

@Component({
  selector: 'app-todo-task-detail',
  templateUrl: './todo-task-detail.component.html',
  styleUrls: ['./todo-task-detail.component.scss'],
})
export class TodoTaskDetailComponent {
  @Input() task: TodoTask | null = null;
  @Input() canManage = true;
  @Output() closed = new EventEmitter<void>();
  @Output() taskUpdated = new EventEmitter<TodoTask>();
  @Output() archived = new EventEmitter<string>();

  TodoStatus = TodoStatus;

  readonly statuses = [
    { value: TodoStatus.NotStarted, label: 'todo.status.notStarted' },
    { value: TodoStatus.InProgress, label: 'todo.status.inProgress' },
    { value: TodoStatus.Done, label: 'todo.status.done' },
  ];

  constructor(private readonly todoService: TodoService) {}

  close(): void {
    this.closed.emit();
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

  private emitUpdated(task: TodoTask): void {
    this.task = task;
    this.taskUpdated.emit(task);
  }
}
