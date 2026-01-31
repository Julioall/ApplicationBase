import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

/**
 * Modelo TODO
 */
export interface TodoModel {
  id: string;
  title: string;
  description?: string;
  completed: boolean;
  priority: 'low' | 'medium' | 'high';
  dueDate?: Date;
  steps?: TodoStep[];
}

export interface TodoStep {
  id: string;
  title: string;
  completed: boolean;
}

/**
 * Serviço Façade para TODO
 * Orquestra múltiplos serviços e gerencia estado
 * 
 * Responsabilidades:
 * - Buscar todos as tarefas
 * - Criar/atualizar/deletar tarefas
 * - Gerenciar estado compartilhado
 */
@Injectable({
  providedIn: 'root'
})
export class TodoFacadeService {
  // BehaviorSubjects para estado reativo
  private todos$ = new BehaviorSubject<TodoModel[]>([]);
  private loading$ = new BehaviorSubject<boolean>(false);
  private error$ = new BehaviorSubject<string | null>(null);
  private selectedTodo$ = new BehaviorSubject<TodoModel | null>(null);

  // Observables públicos
  public todos = this.todos$.asObservable();
  public loading = this.loading$.asObservable();
  public error = this.error$.asObservable();
  public selectedTodo = this.selectedTodo$.asObservable();

  constructor() {
    this.loadTodos();
  }

  /**
   * Carregar todas as tarefas
   */
  loadTodos(): void {
    this.loading$.next(true);
    this.error$.next(null);

    // TODO: Chamar API via HttpClient
    // Mockado por enquanto
    setTimeout(() => {
      const mockTodos: TodoModel[] = [
        {
          id: '1',
          title: 'Configurar Design System',
          description: 'Criar tokens e componentes base',
          completed: true,
          priority: 'high',
          steps: [
            { id: '1a', title: 'Criar cores', completed: true },
            { id: '1b', title: 'Criar tipografia', completed: true },
            { id: '1c', title: 'Criar espaçamento', completed: false }
          ]
        },
        {
          id: '2',
          title: 'Refatorar componentes',
          description: 'Separar smart/dumb components',
          completed: false,
          priority: 'high'
        }
      ];
      this.todos$.next(mockTodos);
      this.loading$.next(false);
    }, 500);
  }

  /**
   * Criar nova tarefa
   */
  createTodo(todo: Omit<TodoModel, 'id'>): void {
    this.loading$.next(true);
    this.error$.next(null);

    setTimeout(() => {
      const newTodo: TodoModel = {
        ...todo,
        id: Date.now().toString()
      };
      const current = this.todos$.value;
      this.todos$.next([...current, newTodo]);
      this.loading$.next(false);
    }, 300);
  }

  /**
   * Atualizar tarefa
   */
  updateTodo(id: string, updates: Partial<TodoModel>): void {
    this.loading$.next(true);
    this.error$.next(null);

    setTimeout(() => {
      const updated = this.todos$.value.map(t =>
        t.id === id ? { ...t, ...updates } : t
      );
      this.todos$.next(updated);
      
      if (this.selectedTodo$.value?.id === id) {
        this.selectedTodo$.next(updated.find(t => t.id === id) || null);
      }
      this.loading$.next(false);
    }, 300);
  }

  /**
   * Deletar tarefa
   */
  deleteTodo(id: string): void {
    this.loading$.next(true);
    this.error$.next(null);

    setTimeout(() => {
      const filtered = this.todos$.value.filter(t => t.id !== id);
      this.todos$.next(filtered);
      
      if (this.selectedTodo$.value?.id === id) {
        this.selectedTodo$.next(null);
      }
      this.loading$.next(false);
    }, 300);
  }

  /**
   * Selecionar uma tarefa
   */
  selectTodo(todo: TodoModel): void {
    this.selectedTodo$.next(todo);
  }

  /**
   * Limpar seleção
   */
  clearSelection(): void {
    this.selectedTodo$.next(null);
  }

  /**
   * Limpar erro
   */
  clearError(): void {
    this.error$.next(null);
  }
}
