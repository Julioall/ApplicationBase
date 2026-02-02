/**
 * BUTTON COMPONENT
 * Componente de botão que implementa o design system
 */

import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type ComponentVariant = 'default' | 'primary' | 'secondary' | 'ghost' | 'danger' | 'error' | 'success';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [class]="getButtonClasses()"
      [disabled]="disabled || loading"
      [type]="type"
      (click)="handleClick($event)"
      [attr.aria-label]="ariaLabel"
      [attr.aria-disabled]="disabled"
    >
      <!-- Loading Spinner -->
      @if (loading) {
        <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2" aria-hidden="true"></div>
      }
      
      <!-- Button Content -->
      <ng-content></ng-content>
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `]
})
export class ButtonComponent {
  @Input() variant: ComponentVariant = 'primary';
  @Input() size: ComponentSize = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() ariaLabel?: string;
  @Input() fullWidth = false;
  
  @Output() click = new EventEmitter<MouseEvent>();
  
  handleClick(event: MouseEvent): void {
    if (!this.disabled && !this.loading) {
      this.click.emit(event);
    }
  }
  
  getButtonClasses(): string {
    const baseClasses = 'inline-flex items-center justify-center font-semibold rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2';
    
    let classes = baseClasses;
    
    // Size classes
    switch (this.size) {
      case 'xs':
        classes += ' px-2 py-1 text-xs';
        break;
      case 'sm':
        classes += ' px-3 py-2 text-sm';
        break;
      case 'md':
        classes += ' px-4 py-2 text-base';
        break;
      case 'lg':
        classes += ' px-6 py-3 text-lg';
        break;
      case 'xl':
        classes += ' px-8 py-4 text-xl';
        break;
    }
    
    // Variant classes
    switch (this.variant) {
      case 'primary':
        classes += ' bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500';
        break;
      case 'secondary':
        classes += ' bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-500';
        break;
      case 'ghost':
        classes += ' bg-transparent text-gray-700 hover:bg-gray-100 focus:ring-gray-500';
        break;
      default:
        classes += ' bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500';
    }
    
    if (this.fullWidth) {
      classes += ' w-full';
    }
    
    if (this.disabled || this.loading) {
      classes += ' opacity-50 cursor-not-allowed';
    }
    
    return classes;
  }
}
    
    if (this.isPressed()) {
      classes += ' scale-95';
    }
    
    return classes;
  });
  
  protected readonly contentClasses = computed(() => {
    let classes = '';
    
    if (this.loading) {
      classes += ' opacity-70';
    }
    
    return classes;
  });
  
  protected handleClick(event: MouseEvent): void {
    if (this.disabled || this.loading) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    
    // Efeito de press
    this.isPressed.set(true);
    setTimeout(() => this.isPressed.set(false), 150);
    
    this.click.emit(event);
  }
}