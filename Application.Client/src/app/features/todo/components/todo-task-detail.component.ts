import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonComponent, BadgeComponent } from '@shared/components';
import { BasePresentationalComponent } from '@shared/base-components';
import { TodoModel, TodoStep } from '../services/todo-facade.service';

/**
 * Componente Dumb (Presentational): TodoTaskDetailComponent
 * Responsável apenas por exibir dados recebidos
 * 
 * Usa Tailwind CSS puro para estilos
 */
@Component({
  selector: 'app-todo-task-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ButtonComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="space-y-2">
        <h2 class="text-2xl font-bold text-gray-900">{{ todo?.title }}</h2>
        <div class="flex items-center gap-3 flex-wrap">
          <app-badge [variant]="getPriorityVariant(todo?.priority)">
            {{ todo?.priority }}
          </app-badge>
          <span *ngIf="todo?.completed" class="inline-flex items-center px-3 py-1 rounded-full bg-green-100 text-green-800 text-sm font-medium">
            ✓ Concluído
          </span>
          <span *ngIf="!todo?.completed" class="inline-flex items-center px-3 py-1 rounded-full bg-blue-100 text-blue-800 text-sm font-medium">
            ◐ Em Progresso
          </span>
        </div>
      </div>

      <!-- Description -->
      <div class="space-y-2" *ngIf="todo?.description">
        <h3 class="text-sm font-semibold text-gray-700">Descrição</h3>
        <p class="text-gray-600 whitespace-pre-wrap">{{ todo?.description }}</p>
      </div>

      <!-- Due Date -->
      <div class="space-y-2" *ngIf="todo?.dueDate">
        <h3 class="text-sm font-semibold text-gray-700">Data de Vencimento</h3>
        <p class="text-gray-600">{{ todo?.dueDate | date: 'dd/MM/yyyy' }}</p>
      </div>

      <!-- Steps/Checklist -->
      <div class="space-y-3" *ngIf="todo?.steps && todo.steps.length > 0">
        <h3 class="text-sm font-semibold text-gray-700">Passos</h3>
        <div class="space-y-2">
          <div
            *ngFor="let step of todo.steps"
            class="flex items-center gap-3 p-3 rounded-lg bg-gray-50 hover:bg-gray-100 transition-colors cursor-pointer"
            (click)="onStepToggle(step)"
          >
            <input
              type="checkbox"
              [checked]="step.completed"
              class="w-5 h-5 rounded border-gray-300 text-primary-600 focus:ring-2 focus:ring-primary-500"
              [disabled]="!todo?.steps"
            />
            <span [class.line-through]="step.completed" [class.opacity-60]="step.completed" class="text-gray-700">
              {{ step.title }}
            </span>
          </div>
        </div>
      </div>

      <!-- Tags -->
      <div class="space-y-3" *ngIf="todo?.tags && todo.tags.length > 0">
        <h3 class="text-sm font-semibold text-gray-700">Tags</h3>
        <div class="flex flex-wrap gap-2">
          <app-badge *ngFor="let tag of todo.tags" variant="primary" [size]="'sm'">
            #{{ tag }}
          </app-badge>
        </div>
      </div>

      <!-- Actions -->
      <div class="space-y-2 border-t border-gray-200 pt-6">
        <app-button
          [variant]="!todo?.completed ? 'success' : 'secondary'"
          [size]="'md'"
          (buttonClick)="onToggleComplete()"
          class="w-full"
        >
          {{ !todo?.completed ? '✓ Marcar como Concluído' : '↻ Reabrir' }}
        </app-button>

        <div class="grid grid-cols-2 gap-2">
          <app-button
            [variant]="'secondary'"
            [size]="'md'"
            (buttonClick)="onEdit()"
            class="w-full"
          >
            Editar
          </app-button>
          <app-button
            [variant]="'danger'"
            [size]="'md'"
            (buttonClick)="onDelete()"
            class="w-full"
          >
            Deletar
          </app-button>
        </div>
      </div>
    </div>
  `
})
export class TodoTaskDetailComponent extends BasePresentationalComponent {
  @Input() todo: TodoModel | null = null;
  @Output() edit = new EventEmitter<TodoModel>();
  @Output() delete = new EventEmitter<string>();
  @Output() toggleComplete = new EventEmitter<TodoModel>();
  @Output() stepToggle = new EventEmitter<TodoStep>();

  onEdit(): void {
    if (this.todo) {
      this.edit.emit(this.todo);
    }
  }

  onDelete(): void {
    if (this.todo) {
      this.delete.emit(this.todo.id);
    }
  }

  onToggleComplete(): void {
    if (this.todo) {
      this.toggleComplete.emit(this.todo);
    }
  }

  onStepToggle(step: TodoStep): void {
    this.stepToggle.emit(step);
  }

  getPriorityVariant(priority?: string): any {
    const map: Record<string, any> = {
      high: 'error',
      medium: 'warning',
      low: 'success'
    };
    return map[priority || ''] || 'default';
  }
}
