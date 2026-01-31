import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Componente Badge Base Reutilizável
 * Para exibir etiquetas e status usando Tailwind
 * 
 * Uso:
 * <app-badge variant="success">Ativo</app-badge>
 */
@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [ngClass]="getBadgeClasses()">
      <ng-content></ng-content>
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BadgeComponent {
  @Input() variant: 'default' | 'primary' | 'success' | 'warning' | 'error' | 'secondary' = 'default';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() outline = false;

  getBadgeClasses(): string {
    const baseClasses = 'inline-flex items-center gap-1.5 font-medium rounded-full whitespace-nowrap transition-colors';

    const variants: Record<string, string> = {
      default: 'bg-gray-100 text-gray-700',
      primary: 'bg-blue-100 text-blue-700',
      success: 'bg-green-100 text-green-700',
      warning: 'bg-yellow-100 text-yellow-700',
      error: 'bg-red-100 text-red-700',
      secondary: 'bg-purple-100 text-purple-700',
    };

    const outlineVariants: Record<string, string> = {
      default: 'border border-gray-300 bg-transparent text-gray-700',
      primary: 'border border-blue-500 bg-transparent text-blue-600',
      success: 'border border-green-500 bg-transparent text-green-600',
      warning: 'border border-yellow-500 bg-transparent text-yellow-600',
      error: 'border border-red-500 bg-transparent text-red-600',
      secondary: 'border border-purple-600 bg-transparent text-purple-700',
    };

    const sizes: Record<string, string> = {
      sm: 'px-2 py-1 text-xs',
      md: 'px-3 py-1 text-sm',
      lg: 'px-4 py-2 text-base',
    };

    const variantClass = this.outline ? outlineVariants[this.variant] : variants[this.variant];

    return `${baseClasses} ${variantClass} ${sizes[this.size]}`;
  }
}
