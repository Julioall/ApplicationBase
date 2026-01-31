import { Directive, ChangeDetectionStrategy } from '@angular/core';

/**
 * Componente Base para Componentes Apresentacionais (Dumb)
 * Fornece funcionalidades comuns como:
 * - OnPush Change Detection (performance)
 * - Apenas @Input/@Output
 * - Sem lógica de negócio
 * 
 * Uso:
 * @Component({
 *   selector: 'app-todo-item',
 *   changeDetection: ChangeDetectionStrategy.OnPush
 * })
 * export class TodoItemComponent extends BasePresentationalComponent {
 *   @Input() todo!: TodoModel;
 *   @Output() onEdit = new EventEmitter<TodoModel>();
 * }
 */
@Directive({
  changeDetection: ChangeDetectionStrategy.OnPush
})
export abstract class BasePresentationalComponent {
  // Nenhuma lógica aqui, apenas apresentação
  // Subclasses usam @Input/@Output para comunicação
}
