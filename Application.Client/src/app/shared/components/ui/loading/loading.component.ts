/**
 * LOADING COMPONENT
 * Componente de loading com múltiplos tipos e estados elegantes
 */

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type LoadingType = 'spinner' | 'dots' | 'pulse' | 'skeleton' | 'ripple';
export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Overlay Mode -->
    @if (overlay) {
      <div class="fixed inset-0 bg-black bg-opacity-20 backdrop-blur-sm flex items-center justify-center z-50">
        <div [class]="getContainerClasses()" [attr.aria-label]="ariaLabel">
          <ng-container [ngTemplateOutlet]="loadingContent"></ng-container>
        </div>
      </div>
    } @else {
      <div [class]="getContainerClasses()" [attr.aria-label]="ariaLabel">
        <ng-container [ngTemplateOutlet]="loadingContent"></ng-container>
      </div>
    }
    
    <!-- Loading Content Template -->
    <ng-template #loadingContent>
      <!-- Spinner Loading -->
      @if (type === 'spinner') {
        <div [class]="getSpinnerClasses()"></div>
        @if (text) {
          <span [class]="getTextClasses()">{{ text }}</span>
        }
      }
      
      <!-- Dots Loading -->
      @if (type === 'dots') {
        <div class="flex space-x-1">
          <div class="w-2 h-2 bg-primary-600 rounded-full animate-bounce"></div>
          <div class="w-2 h-2 bg-primary-600 rounded-full animate-bounce" style="animation-delay: 0.1s"></div>
          <div class="w-2 h-2 bg-primary-600 rounded-full animate-bounce" style="animation-delay: 0.2s"></div>
        </div>
        @if (text) {
          <span [class]="getTextClasses()">{{ text }}</span>
        }
      }
      
      <!-- Pulse Loading -->
      @if (type === 'pulse') {
        <div [class]="getPulseClasses()"></div>
        @if (text) {
          <span [class]="getTextClasses()">{{ text }}</span>
        }
      }
      
      <!-- Skeleton Loading -->
      @if (type === 'skeleton') {
        @for (line of getSkeletonLines(); track $index) {
          <div [class]="getSkeletonLineClasses($index)"></div>
        }
      }
      
      <!-- Ripple Loading -->
      @if (type === 'ripple') {
        <div class="relative">
          <div [class]="getRippleClasses()"></div>
          <div [class]="getRippleClasses()" style="animation-delay: 0.5s"></div>
        </div>
        @if (text) {
          <span [class]="getTextClasses()">{{ text }}</span>
        }
      }
    </ng-template>
  `,
  styles: [`
    :host {
      display: block;
    }
    
    @keyframes pulse-ring {
      0% {
        transform: scale(0.8);
        opacity: 1;
      }
      100% {
        transform: scale(2.4);
        opacity: 0;
      }
    }
    
    .pulse-ring {
      animation: pulse-ring 1.5s ease-out infinite;
    }
  `]
})
export class LoadingComponent {
  @Input() type: LoadingType = 'spinner';
  @Input() size: ComponentSize = 'md';
  @Input() text?: string;
  @Input() overlay = false;
  @Input() ariaLabel = 'Loading...';
  @Input() skeletonLines = 3;
  
  getContainerClasses(): string {
    let classes = 'flex flex-col items-center justify-center space-y-2';
    
    if (this.overlay) {
      classes += ' bg-white rounded-lg p-6 shadow-xl';
    }
    
    return classes;
  }
  
  getSpinnerClasses(): string {
    let classes = 'border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin';
    
    switch (this.size) {
      case 'xs':
        classes += ' w-4 h-4';
        break;
      case 'sm':
        classes += ' w-6 h-6';
        break;
      case 'md':
        classes += ' w-8 h-8';
        break;
      case 'lg':
        classes += ' w-12 h-12';
        break;
      case 'xl':
        classes += ' w-16 h-16';
        break;
    }
    
    return classes;
  }
  
  getPulseClasses(): string {
    let classes = 'bg-primary-600 rounded-full animate-pulse';
    
    switch (this.size) {
      case 'xs':
        classes += ' w-4 h-4';
        break;
      case 'sm':
        classes += ' w-6 h-6';
        break;
      case 'md':
        classes += ' w-8 h-8';
        break;
      case 'lg':
        classes += ' w-12 h-12';
        break;
      case 'xl':
        classes += ' w-16 h-16';
        break;
    }
    
    return classes;
  }
  
  getRippleClasses(): string {
    let classes = 'absolute border-2 border-primary-600 rounded-full pulse-ring';
    
    switch (this.size) {
      case 'xs':
        classes += ' w-4 h-4';
        break;
      case 'sm':
        classes += ' w-6 h-6';
        break;
      case 'md':
        classes += ' w-8 h-8';
        break;
      case 'lg':
        classes += ' w-12 h-12';
        break;
      case 'xl':
        classes += ' w-16 h-16';
        break;
    }
    
    return classes;
  }
  
  getTextClasses(): string {
    let classes = 'text-gray-600 font-medium';
    
    switch (this.size) {
      case 'xs':
        classes += ' text-xs';
        break;
      case 'sm':
        classes += ' text-sm';
        break;
      case 'md':
        classes += ' text-base';
        break;
      case 'lg':
        classes += ' text-lg';
        break;
      case 'xl':
        classes += ' text-xl';
        break;
    }
    
    return classes;
  }
  
  getSkeletonLines(): number[] {
    return Array.from({ length: this.skeletonLines }, (_, i) => i);
  }
  
  getSkeletonLineClasses(index: number): string {
    let classes = 'bg-gray-200 rounded animate-pulse';
    
    // Height based on size
    switch (this.size) {
      case 'xs':
        classes += ' h-3';
        break;
      case 'sm':
        classes += ' h-4';
        break;
      case 'md':
        classes += ' h-5';
        break;
      case 'lg':
        classes += ' h-6';
        break;
      case 'xl':
        classes += ' h-8';
        break;
    }
    
    // Width variation for skeleton lines
    if (index === this.skeletonLines - 1) {
      classes += ' w-3/4';
    } else {
      classes += ' w-full';
    }
    
    // Margin between lines
    if (index > 0) {
      classes += ' mt-2';
    }
    
    return classes;
  }
}

export type LoadingType = 'spinner' | 'dots' | 'pulse' | 'skeleton' | 'ripple';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Spinner Loading -->
    @if (type === 'spinner') {
      <div [class]="containerClasses()" [attr.aria-label]="ariaLabel">
        <div [class]="spinnerClasses()"></div>
        @if (text) {
          <span [class]="textClasses()">{{ text }}</span>
        }
      </div>
    }
    
    <!-- Dots Loading -->
    @if (type === 'dots') {
      <div [class]="containerClasses()" [attr.aria-label]="ariaLabel">
        <div class="dots-loading">
          <div></div>
          <div></div>
          <div></div>
        </div>
        @if (text) {
          <span [class]="textClasses()">{{ text }}</span>
        }
      </div>
    }
    
    <!-- Pulse Loading -->
    @if (type === 'pulse') {
      <div [class]="containerClasses()" [attr.aria-label]="ariaLabel">
        <div [class]="pulseClasses()"></div>
        @if (text) {
          <span [class]="textClasses()">{{ text }}</span>
        }
      </div>
    }
    
    <!-- Skeleton Loading -->
    @if (type === 'skeleton') {
      <div [class]="containerClasses()" [attr.aria-label]="ariaLabel">
        @if (skeletonLines > 0) {
          @for (line of skeletonLinesArray; track $index) {
            <div [class]="skeletonLineClasses($index)"></div>
          }
        }
      </div>
    }
    
    <!-- Ripple Loading -->
    @if (type === 'ripple') {
      <div [class]="containerClasses()" [attr.aria-label]="ariaLabel">
        <div class="pulse-ring" [class]="rippleClasses()"></div>
        @if (text) {
          <span [class]="textClasses()">{{ text }}</span>
        }
      </div>
    }
    
    <!-- Overlay Mode -->
    @if (overlay) {
      <div class="loading-overlay animate-fade-in">
        <ng-container [ngTemplateOutlet]="mainContent"></ng-container>
      </div>
    }
    
    <!-- Main Content Template -->
    <ng-template #mainContent>
      <ng-content></ng-content>
    </ng-template>
  `,
  styles: [`
    :host {
      display: block;
    }
    
    .skeleton {
      background: linear-gradient(90deg, 
        rgba(255, 255, 255, 0) 0%,
        rgba(255, 255, 255, 0.2) 20%,
        rgba(255, 255, 255, 0.5) 60%,
        rgba(255, 255, 255, 0) 100%
      );
      background-size: 200px 100%;
      background-repeat: no-repeat;
      animation: shimmer 2s infinite linear;
    }
    
    @keyframes shimmer {
      0% { background-position: -200px 0; }
      100% { background-position: calc(200px + 100%) 0; }
    }
    
    .dark .skeleton {
      background: linear-gradient(90deg, 
        rgba(255, 255, 255, 0) 0%,
        rgba(255, 255, 255, 0.05) 20%,
        rgba(255, 255, 255, 0.1) 60%,
        rgba(255, 255, 255, 0) 100%
      );
    }
  `]
})
export class LoadingComponent {
  @Input() type: LoadingType = 'spinner';
  @Input() size: ComponentSize = 'md';
  @Input() text?: string;
  @Input() overlay = false;
  @Input() color: 'primary' | 'secondary' | 'white' | 'current' = 'primary';
  @Input() centered = false;
  @Input() skeletonLines = 3;
  @Input() ariaLabel = 'Carregando...';
  
  protected readonly designSystem = new DesignSystemService();
  
  protected readonly skeletonLinesArray = computed(() => 
    Array.from({ length: this.skeletonLines }, (_, i) => i)
  );
  
  protected readonly containerClasses = computed(() => {
    let classes = 'flex items-center';
    
    if (this.centered) {
      classes += ' justify-center';
    }
    
    if (this.type === 'skeleton') {
      classes = 'space-y-3 w-full';
    } else if (this.text) {
      classes += ' gap-3';
    }
    
    if (this.type === 'dots') {
      classes += ' flex-col gap-2';
    }
    
    return classes;
  });
  
  protected readonly spinnerClasses = computed(() => {
    const sizeMap = {
      xs: 'w-4 h-4 border-2',
      sm: 'w-5 h-5 border-2',
      md: 'w-6 h-6 border-2',
      lg: 'w-8 h-8 border-[3px]',
      xl: 'w-10 h-10 border-[3px]',
    };
    
    const colorMap = {
      primary: 'border-primary-200 border-t-primary-500',
      secondary: 'border-secondary-200 border-t-secondary-500',
      white: 'border-white/30 border-t-white',
      current: 'border-current/30 border-t-current',
    };
    
    let classes = 'rounded-full animate-spin';
    classes += ' ' + (sizeMap[this.size] || sizeMap.md);
    classes += ' ' + (colorMap[this.color] || colorMap.primary);
    
    if (this.designSystem.isDarkMode() && this.color === 'primary') {
      classes = classes.replace('border-primary-200', 'border-primary-800');
    }
    
    return classes;
  });
  
  protected readonly pulseClasses = computed(() => {
    const sizeMap = {
      xs: 'w-8 h-8',
      sm: 'w-10 h-10',
      md: 'w-12 h-12',
      lg: 'w-16 h-16',
      xl: 'w-20 h-20',
    };
    
    const colorMap = {
      primary: 'bg-primary-500',
      secondary: 'bg-secondary-500',
      white: 'bg-white',
      current: 'bg-current',
    };
    
    let classes = 'rounded-full animate-pulse-soft';
    classes += ' ' + (sizeMap[this.size] || sizeMap.md);
    classes += ' ' + (colorMap[this.color] || colorMap.primary);
    
    return classes;
  });
  
  protected readonly rippleClasses = computed(() => {
    const sizeMap = {
      xs: 'w-8 h-8',
      sm: 'w-10 h-10', 
      md: 'w-12 h-12',
      lg: 'w-16 h-16',
      xl: 'w-20 h-20',
    };
    
    const colorMap = {
      primary: 'border-primary-500',
      secondary: 'border-secondary-500',
      white: 'border-white',
      current: 'border-current',
    };
    
    let classes = 'rounded-full border-2';
    classes += ' ' + (sizeMap[this.size] || sizeMap.md);
    classes += ' ' + (colorMap[this.color] || colorMap.primary);
    
    return classes;
  });
  
  protected readonly textClasses = computed(() => {
    const sizeMap = {
      xs: 'text-xs',
      sm: 'text-sm',
      md: 'text-base',
      lg: 'text-lg', 
      xl: 'text-xl',
    };
    
    let classes = 'text-gray-600 dark:text-gray-400 font-medium';
    classes += ' ' + (sizeMap[this.size] || sizeMap.md);
    
    return classes;
  });
  
  protected skeletonLineClasses(index: number): string {
    const heightMap = {
      xs: 'h-3',
      sm: 'h-4',
      md: 'h-4',
      lg: 'h-5',
      xl: 'h-6',
    };
    
    let classes = 'skeleton rounded bg-gray-200 dark:bg-gray-700';
    classes += ' ' + (heightMap[this.size] || heightMap.md);
    
    // Vary widths for more realistic skeleton
    const widths = ['w-full', 'w-4/5', 'w-3/4', 'w-5/6', 'w-11/12'];
    const randomWidth = widths[index % widths.length];
    
    // Last line is usually shorter
    if (index === this.skeletonLines - 1) {
      classes += ' w-3/5';
    } else {
      classes += ' ' + randomWidth;
    }
    
    return classes;
  }
}