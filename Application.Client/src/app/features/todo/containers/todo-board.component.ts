import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CardComponent, BadgeComponent, ButtonComponent } from '@shared/components';
import { BaseSmartComponent } from '@shared/base-components';
import { TodoFacadeService, TodoModel } from '../services/todo-facade.service';

/**
 * Componente Smart: TodoBoardComponent
 * Responsável por:
 * - Buscar dados de tarefas
 * - Gerenciar estado
 * - Orquestrar sub-componentes
 * 
 * Usa Tailwind CSS puro para estilos
 */
@Component({
  selector: 'app-todo-board',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, CardComponent, BadgeComponent, ButtonComponent],
  template: `
    <div class="min-h-screen bg-gray-50 py-8 px-4">
      <div class="mx-auto max-w-7xl">
        <h1 class="text-4xl font-bold text-gray-900 mb-8">Minhas Tarefas</h1>

        <!-- Loading State -->
        <div *ngIf="loading$ | async" class="text-center py-12">
          <p class="text-gray-500 text-lg">Carregando tarefas...</p>
        </div>

        <!-- Error State -->
        <div *ngIf="error$ | async as error" class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          Erro ao carregar tarefas: {{ error }}
        </div>

        <!-- Tarefas Board - 3 Colunas -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <!-- Column: A Fazer -->
          <div class="space-y-4">
            <h2 class="text-xl font-semibold text-gray-900 px-2">A Fazer</h2>
            <div class="space-y-3">
              <div
                *ngFor="let todo of (todos$ | async) as todos"
                [hidden]="todo.completed"
                (click)="onSelectTodo(todo)"
              >
                <app-card [hoverable]="true">
                  <div class="space-y-2">
                    <div class="flex items-start justify-between gap-2">
                      <h3 class="font-semibold text-gray-900">{{ todo.title }}</h3>
                      <app-badge [variant]="getPriorityVariant(todo.priority)">
                        {{ todo.priority }}
                      </app-badge>
                    </div>
                    <p *ngIf="todo.description" class="text-sm text-gray-600">{{ todo.description }}</p>
                  </div>
                </app-card>
              </div>
            </div>
          </div>

          <!-- Column: Em Progresso -->
          <div class="space-y-4">
            <h2 class="text-xl font-semibold text-gray-900 px-2">Em Progresso</h2>
            <p class="text-sm text-gray-500 px-2 py-4">— Sem tarefas —</p>
          </div>

          <!-- Column: Concluído -->
          <div class="space-y-4">
            <h2 class="text-xl font-semibold text-gray-900 px-2">Concluído</h2>
            <div class="space-y-3">
              <div
                *ngFor="let todo of (todos$ | async) as todos"
                [hidden]="!todo.completed"
              >
                <app-card [flat]="true">
                  <h3 class="font-semibold text-gray-700 line-through opacity-60">{{ todo.title }}</h3>
                </app-card>
              </div>
            </div>
          </div>
        </div>

        <!-- Detail Panel - Lado Direito (Móvel: Modal) -->
        <div *ngIf="selectedTodo$ | async as todo" class="fixed right-0 top-0 h-full w-full sm:w-96 bg-white shadow-2xl z-50 overflow-y-auto">
          <div class="p-6">
            <button 
              (click)="onClearSelection()" 
              class="mb-4 text-gray-500 hover:text-gray-700 text-2xl leading-none font-light"
              aria-label="Fechar"
            >
              ×
            </button>
            <app-todo-task-detail 
              [todo]="todo"
              (edit)="onEditTodo($event)"
              (delete)="onDeleteTodo($event)"
              (toggleComplete)="onToggleComplete($event)"
              (stepToggle)="onStepToggle($event)">
            </app-todo-task-detail>
          </div>
        </div>

        <!-- Overlay -->
        <div 
          *ngIf="selectedTodo$ | async" 
          (click)="onClearSelection()"
          class="fixed inset-0 bg-black bg-opacity-50 z-40 sm:hidden"
        ></div>
      </div>
    </div>
  `
})
export class TodoBoardComponent extends BaseSmartComponent implements OnInit {
  todos$ = this.todoFacade.todos;
  loading$ = this.todoFacade.loading;
  error$ = this.todoFacade.error;
  selectedTodo$ = this.todoFacade.selectedTodo;

  constructor(private todoFacade: TodoFacadeService) {
    super();
  }

  ngOnInit(): void {
    this.todoFacade.loadTodos();
  }

  onSelectTodo(todo: TodoModel): void {
    this.todoFacade.selectTodo(todo);
  }

  onClearSelection(): void {
    this.todoFacade.clearSelection();
  }

  onEditTodo(todo: TodoModel): void {
    console.log('Edit todo:', todo);
  }

  onDeleteTodo(id: string): void {
    if (confirm('Tem certeza que deseja deletar esta tarefa?')) {
      this.todoFacade.deleteTodo(id);
    }
  }

  onToggleComplete(todo: TodoModel): void {
    this.todoFacade.updateTodo(todo.id, { completed: !todo.completed });
  }

  onStepToggle(step: any): void {
    console.log('Toggle step:', step);
  }

  getPriorityVariant(priority: string): any {
    const map: Record<string, any> = {
      high: 'error',
      medium: 'warning',
      low: 'success'
    };
    return map[priority] || 'default';
  }
}

