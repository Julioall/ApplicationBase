/**
 * AUTH LAYOUT COMPONENT
 * Layout base para telas de autenticação usando o design system
 */

import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DesignSystemService } from '../../services/design-system.service';
import { CardComponent } from '../ui/card/card.component';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [CommonModule, CardComponent],
  template: `
    <!-- Background with Mesh Gradient -->
    <div class="page-container">
      <!-- Decorative Background Elements -->
      <div class="mesh-gradient-bg"></div>
      
      <!-- Floating Orbs -->
      @if (designSystem.animationsEnabled()) {
        <div class="floating-orb top-20 left-20 animate-float" style="animation-delay: 0s;"></div>
        <div class="morphing-blob top-1/4 right-20" style="animation-delay: 2s;"></div>
        <div class="floating-orb bottom-32 right-1/3 animate-float" style="animation-delay: 4s;"></div>
      }
      
      <!-- Main Content Container -->
      <div class="auth-container">
        <!-- Logo/Brand Area -->
        <div class="text-center mb-8 animate-fade-in">
          <div class="w-16 h-16 mx-auto mb-4 bg-gradient-primary rounded-2xl flex items-center justify-center shadow-glow-primary">
            <!-- Logo Icon -->
            <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <h1 class="text-2xl font-heading font-bold text-gradient">
            ApplicationBase
          </h1>
          <p class="text-gray-600 dark:text-gray-400 mt-2">
            Sistema moderno e elegante
          </p>
        </div>
        
        <!-- Auth Card -->
        <app-card 
          variant="glass" 
          padding="lg"
          [hoverable]="false"
          [decorative]="true"
          [class]="authCardClasses()"
        >
          <!-- Card Content -->
          <ng-content></ng-content>
        </app-card>
        
        <!-- Footer Links -->
        <div class="mt-6 text-center animate-delayed-fade-in">
          <ng-content select="[slot=footer-links]"></ng-content>
        </div>
      </div>
      
      <!-- Theme Toggle -->
      <button
        type="button"
        class="fixed top-6 right-6 p-3 rounded-full bg-white/20 backdrop-blur-md border border-white/30 hover:bg-white/30 transition-all duration-200 z-50"
        (click)="designSystem.toggleTheme()"
        [attr.aria-label]="themeToggleLabel()"
      >
        @if (designSystem.isDarkMode()) {
          <!-- Sun Icon -->
          <svg class="w-5 h-5 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
        } @else {
          <!-- Moon Icon -->
          <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                  d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
          </svg>
        }
      </button>
      
      <!-- Accessibility Skip Link -->
      <a 
        href="#main-content"
        class="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 bg-white px-4 py-2 rounded-lg shadow-lg z-50"
      >
        Pular para conteúdo principal
      </a>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }
    
    .page-container {
      position: relative;
      overflow: hidden;
    }
    
    /* Responsive adjustments */
    @media (max-width: 640px) {
      .auth-container {
        @apply p-4 max-w-sm;
      }
      
      .floating-orb,
      .morphing-blob {
        @apply opacity-30;
      }
    }
    
    @media (max-width: 480px) {
      .floating-orb,
      .morphing-blob {
        @apply hidden;
      }
    }
    
    /* Focus management */
    .page-container:focus-within .floating-orb,
    .page-container:focus-within .morphing-blob {
      animation-play-state: paused;
    }
    
    /* Print styles */
    @media print {
      .floating-orb,
      .morphing-blob,
      .mesh-gradient-bg,
      button {
        @apply hidden;
      }
      
      .page-container {
        @apply bg-white text-black min-h-0;
      }
    }
  `]
})
export class AuthLayoutComponent {
  protected readonly designSystem = new DesignSystemService();
  
  protected readonly authCardClasses = computed(() => {
    let classes = 'w-full max-w-md mx-auto';
    
    if (this.designSystem.animationsEnabled()) {
      classes += ' animate-fade-in-up';
    }
    
    return classes;
  });
  
  protected readonly themeToggleLabel = computed(() => {
    return this.designSystem.isDarkMode() 
      ? 'Alternar para tema claro' 
      : 'Alternar para tema escuro';
  });
}