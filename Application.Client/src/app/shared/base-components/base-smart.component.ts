import { Directive, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * Componente Base para Smart Components
 * Fornece funcionalidades comuns como:
 * - Gerenciamento de inscrições (RxJS)
 * - Loading/Error states
 * - OnDestroy automático
 * 
 * Uso:
 * @Component({ ... })
 * export class MySmartComponent extends BaseSmartComponent implements OnInit {
 *   constructor(private todoService: TodoService) {
 *     super();
 *   }
 * 
 *   ngOnInit(): void {
 *     this.todoService.getTodos()
 *       .pipe(takeUntil(this.destroy$))
 *       .subscribe(todos => this.todos = todos);
 *   }
 * }
 */
@Directive()
export abstract class BaseSmartComponent implements OnDestroy {
  // Observable para desinscrição automática
  protected destroy$ = new Subject<void>();

  // Estados comuns
  loading = false;
  error: string | null = null;

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Método auxiliar para iniciar loading e tratar erros
   */
  protected handleLoading<T>(promise: Promise<T> | any): Promise<T> {
    this.loading = true;
    this.error = null;
    return promise
      .then((result: T) => {
        this.loading = false;
        return result;
      })
      .catch((err: any) => {
        this.loading = false;
        this.error = err?.message || 'Erro desconhecido';
        throw err;
      });
  }

  /**
   * Limpar erro
   */
  protected clearError(): void {
    this.error = null;
  }
}
