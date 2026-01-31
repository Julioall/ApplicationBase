import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Componente Button Base Reutilizável
 * Usa Tailwind CSS puro para estilos
 * 
 * Uso:
 * <app-button [variant]="'primary'" (buttonClick)="handleClick()">
 *   Click me
 * </app-button>
 */
@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [ngClass]="getButtonClasses()"
      [disabled]="disabled || loading"
      (click)="buttonClick.emit()"
      type="button"
      [attr.aria-label]="ariaLabel"
    >
      <span *ngIf="loading" class="mr-2 inline-block animate-spin">⟳</span>
      <ng-content></ng-content>
    </button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'danger' | 'ghost' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() ariaLabel: string | null = null;

  @Output() buttonClick = new EventEmitter<void>();

  getButtonClasses(): string {
    const baseClasses = 'font-medium transition-all duration-150 ease-in-out cursor-pointer focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 inline-flex items-center justify-center whitespace-nowrap';

    const variants: Record<string, string> = {
      primary: 'bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800 focus:ring-primary-500',
      secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 active:bg-gray-400',
      danger: 'bg-danger-500 text-white hover:bg-danger-600 active:bg-danger-700 focus:ring-danger-500',
      ghost: 'bg-transparent text-primary-600 border border-primary-600 hover:bg-primary-50 active:bg-primary-100',
    };

    const sizes: Record<string, string> = {
      sm: 'px-3 py-2 text-xs',
      md: 'px-4 py-2 text-sm',
      lg: 'px-6 py-3 text-base',
    };

    return `${baseClasses} ${variants[this.variant]} ${sizes[this.size]}`;
  }
}

