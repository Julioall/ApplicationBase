import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Componente Alert Base Reutilizável
 * Exibe mensagens de diferentes tipos usando Tailwind
 * 
 * Uso:
 * <app-alert type="success" [dismissible]="true">
 *   Operação realizada com sucesso!
 * </app-alert>
 */
@Component({
  selector: 'app-alert',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [ngClass]="getAlertClasses()" role="alert">
      <span class="text-lg mr-3">{{ getIcon() }}</span>
      <div class="flex-1">
        <ng-content></ng-content>
      </div>
      <button
        *ngIf="dismissible"
        type="button"
        (click)="onDismiss()"
        class="ml-4 text-xl leading-none hover:opacity-70 flex-shrink-0"
        aria-label="Fechar alerta"
      >
        ×
      </button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlertComponent {
  @Input() type: 'info' | 'success' | 'warning' | 'error' = 'info';
  @Input() dismissible = false;

  protected isVisible = true;

  getAlertClasses(): string {
    const baseClasses = 'flex items-center gap-3 p-4 rounded-md border-l-4 transition-opacity duration-300';

    const variants: Record<string, string> = {
      info: 'bg-blue-50 border-blue-500 text-blue-800',
      success: 'bg-green-50 border-green-500 text-green-800',
      warning: 'bg-yellow-50 border-yellow-500 text-yellow-800',
      error: 'bg-red-50 border-red-500 text-red-800',
    };

    const opacityClass = this.isVisible ? 'opacity-100' : 'opacity-0';

    return `${baseClasses} ${variants[this.type]} ${opacityClass}`;
  }

  getIcon(): string {
    const icons: Record<string, string> = {
      info: 'ℹ',
      success: '✓',
      warning: '⚠',
      error: '✕'
    };
    return icons[this.type] || 'ℹ';
  }

  onDismiss(): void {
    this.isVisible = false;
  }
}
