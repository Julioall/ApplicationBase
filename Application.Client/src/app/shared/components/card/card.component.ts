import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Componente Card Base Reutilizável
 * Fornece um container com estilo consistente usando Tailwind
 * 
 * Uso:
 * <app-card [elevation]="2">
 *   <h3>Card Title</h3>
 *   <p>Card content goes here</p>
 * </app-card>
 */
@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [ngClass]="getCardClasses()">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardComponent {
  @Input() elevation: 1 | 2 | 3 = 2;
  @Input() flat = false;
  @Input() hoverable = false;

  getCardClasses(): string {
    const baseClasses = 'bg-white rounded-lg border border-gray-200 transition-shadow duration-150';

    let shadowClass = '';
    if (!this.flat) {
      const shadows: Record<number, string> = {
        1: 'shadow-xs',
        2: 'shadow-sm',
        3: 'shadow-md',
      };
      shadowClass = shadows[this.elevation] || 'shadow-sm';
    } else {
      return `${baseClasses.replace('bg-white', 'bg-gray-50')} border-0`;
    }

    const hoverClass = this.hoverable ? 'hover:shadow-md cursor-pointer' : '';

    return `${baseClasses} ${shadowClass} p-6 ${hoverClass}`;
  }
}
