/**
 * CARD COMPONENT
 * Componente de card que implementa o design system com glassmorphism
 */

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="getCardClasses()" [attr.aria-label]="ariaLabel">
      <!-- Header -->
      @if (hasHeader) {
        <div [class]="getHeaderClasses()">
          @if (title) {
            <h3 [class]="getTitleClasses()">{{ title }}</h3>
          }
          @if (subtitle) {
            <p [class]="getSubtitleClasses()">{{ subtitle }}</p>
          }
          <ng-content select="[slot=header]"></ng-content>
        </div>
      }
      
      <!-- Content -->
      <div [class]="getContentClasses()">
        <ng-content></ng-content>
      </div>
      
      <!-- Footer -->
      @if (hasFooter) {
        <div [class]="getFooterClasses()">
          <ng-content select="[slot=footer]"></ng-content>
        </div>
      }
      
      <!-- Decorative Elements -->
      @if (variant === 'glass' && decorative) {
        <div class="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
          <div class="absolute -top-4 -right-4 w-24 h-24 bg-primary-500 opacity-10 rounded-full blur-xl"></div>
          <div class="absolute -bottom-4 -left-4 w-32 h-32 bg-accent-500 opacity-10 rounded-full blur-xl"></div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class CardComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() variant: 'elevated' | 'flat' | 'glass' = 'elevated';
  @Input() padding: ComponentSize = 'md';
  @Input() hoverable = false;
  @Input() clickable = false;
  @Input() decorative = false;
  @Input() ariaLabel?: string;
  
  get hasHeader(): boolean {
    return !!(this.title || this.subtitle);
  }
  
  get hasFooter(): boolean {
    return true; // Simplified - assume footer exists if slot is used
  }
  
  getCardClasses(): string {
    let classes = 'rounded-xl transition-all duration-300';
    
    // Variant classes
    switch (this.variant) {
      case 'elevated':
        classes += ' bg-white shadow-lg border border-gray-200';
        break;
      case 'flat':
        classes += ' bg-white border border-gray-200';
        break;
      case 'glass':
        classes += ' bg-white/70 backdrop-blur-md border border-white/20 shadow-xl';
        break;
    }
    
    // Hover effects
    if (this.hoverable) {
      classes += ' hover:-translate-y-1 hover:shadow-2xl cursor-pointer';
    }
    
    if (this.clickable) {
      classes += ' cursor-pointer focus:outline-none focus:ring-4 focus:ring-primary-200';
    }
    
    if (this.variant === 'glass') {
      classes += ' relative overflow-hidden';
    }
    
    // Padding
    switch (this.padding) {
      case 'xs':
        classes += ' p-2';
        break;
      case 'sm':
        classes += ' p-4';
        break;
      case 'md':
        classes += ' p-6';
        break;
      case 'lg':
        classes += ' p-8';
        break;
      case 'xl':
        classes += ' p-10';
        break;
    }
    
    return classes;
  }
  
  getHeaderClasses(): string {
    let classes = 'border-b border-gray-200';
    
    switch (this.padding) {
      case 'xs':
        classes += ' pb-1 mb-2';
        break;
      case 'sm':
        classes += ' pb-2 mb-3';
        break;
      case 'md':
        classes += ' pb-3 mb-4';
        break;
      case 'lg':
        classes += ' pb-4 mb-6';
        break;
      case 'xl':
        classes += ' pb-6 mb-8';
        break;
    }
    
    return classes;
  }
  
  getTitleClasses(): string {
    return 'text-xl font-bold text-gray-900 mb-1';
  }
  
  getSubtitleClasses(): string {
    return 'text-sm text-gray-600';
  }
  
  getContentClasses(): string {
    return 'text-gray-700';
  }
  
  getFooterClasses(): string {
    let classes = 'border-t border-gray-200';
    
    switch (this.padding) {
      case 'xs':
        classes += ' pt-1 mt-2';
        break;
      case 'sm':
        classes += ' pt-2 mt-3';
        break;
      case 'md':
        classes += ' pt-3 mt-4';
        break;
      case 'lg':
        classes += ' pt-4 mt-6';
        break;
      case 'xl':
        classes += ' pt-6 mt-8';
        break;
    }
    
    return classes;
  }
}

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="cardClasses()" [attr.aria-label]="ariaLabel">
      <!-- Header -->
      @if (hasHeader()) {
        <div [class]="headerClasses()">
          @if (title) {
            <h3 [class]="titleClasses()">{{ title }}</h3>
          }
          @if (subtitle) {
            <p [class]="subtitleClasses()">{{ subtitle }}</p>
          }
          <ng-content select="[slot=header]"></ng-content>
        </div>
      }
      
      <!-- Content -->
      <div [class]="contentClasses()">
        <ng-content></ng-content>
      </div>
      
      <!-- Footer -->
      @if (hasFooter()) {
        <div [class]="footerClasses()">
          <ng-content select="[slot=footer]"></ng-content>
        </div>
      }
      
      <!-- Decorative Elements -->
      @if (variant === 'glass' && decorative) {
        <div class="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
          <div class="absolute -top-4 -right-4 w-24 h-24 bg-primary-500 opacity-10 rounded-full blur-xl"></div>
          <div class="absolute -bottom-4 -left-4 w-32 h-32 bg-accent-500 opacity-10 rounded-full blur-xl"></div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class CardComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() variant: 'elevated' | 'flat' | 'glass' = 'elevated';
  @Input() padding: ComponentSize = 'md';
  @Input() hoverable = false;
  @Input() clickable = false;
  @Input() decorative = false;
  @Input() ariaLabel?: string;
  
  protected readonly designSystem = new DesignSystemService();
  
  protected readonly hasHeader = computed(() => {
    return !!(this.title || this.subtitle);
  });
  
  protected readonly hasFooter = computed(() => {
    // Note: In a real implementation, you might check if there's content projected
    // For now, we'll assume footer exists if the slot is used
    return true; // This would be determined by checking slot content
  });
  
  protected readonly cardClasses = computed(() => {
    let classes = this.designSystem.getCardClasses(this.variant, 'none'); // We'll handle padding separately
    
    if (this.hoverable && this.designSystem.animationsEnabled()) {
      classes += ' hover-lift cursor-pointer';
    }
    
    if (this.clickable) {
      classes += ' cursor-pointer focus:outline-none focus:ring-4 focus:ring-primary-200';
      if (this.designSystem.isDarkMode()) {
        classes += ' focus:ring-primary-800';
      }
    }
    
    if (this.variant === 'glass') {
      classes += ' relative overflow-hidden backdrop-blur-md';
    }
    
    // Add interactive states
    if (this.hoverable || this.clickable) {
      if (this.designSystem.animationsEnabled()) {
        classes += ' transition-all duration-300 ease-out';
      }
      
      if (this.variant === 'elevated') {
        classes += ' hover:shadow-2xl hover:-translate-y-1';
      } else if (this.variant === 'flat') {
        classes += ' hover:border-primary-300 hover:shadow-md';
      } else if (this.variant === 'glass') {
        classes += ' hover:backdrop-blur-lg';
      }
    }
    
    return classes;
  });
  
  protected readonly headerClasses = computed(() => {
    const paddingMap = {
      xs: 'p-3 pb-2',
      sm: 'p-4 pb-2',
      md: 'p-6 pb-3',
      lg: 'p-8 pb-4',
      xl: 'p-10 pb-5',
    };
    
    let classes = paddingMap[this.padding] || paddingMap.md;
    classes += ' border-b border-gray-200 dark:border-gray-700';
    
    return classes;
  });
  
  protected readonly contentClasses = computed(() => {
    const paddingMap = {
      xs: 'p-3',
      sm: 'p-4', 
      md: 'p-6',
      lg: 'p-8',
      xl: 'p-10',
    };
    
    const headerPaddingMap = {
      xs: 'pt-2',
      sm: 'pt-3',
      md: 'pt-4',
      lg: 'pt-5',
      xl: 'pt-6',
    };
    
    let classes = paddingMap[this.padding] || paddingMap.md;
    
    if (this.hasHeader()) {
      classes = classes.replace(/p-\d+/, ''); // Remove padding
      classes += ` px-${paddingMap[this.padding]?.split('-')[1] || '6'} ${headerPaddingMap[this.padding]}`;
    }
    
    return classes;
  });
  
  protected readonly footerClasses = computed(() => {
    const paddingMap = {
      xs: 'p-3 pt-2',
      sm: 'p-4 pt-2',
      md: 'p-6 pt-3',
      lg: 'p-8 pt-4',
      xl: 'p-10 pt-5',
    };
    
    let classes = paddingMap[this.padding] || paddingMap.md;
    classes += ' border-t border-gray-200 dark:border-gray-700';
    
    return classes;
  });
  
  protected readonly titleClasses = computed(() => {
    return 'text-lg font-semibold text-gray-900 dark:text-white mb-1';
  });
  
  protected readonly subtitleClasses = computed(() => {
    return 'text-sm text-gray-600 dark:text-gray-400';
  });
}