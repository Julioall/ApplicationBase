/**
 * DESIGN SYSTEM SERVICE
 * Serviço Angular para gerenciar temas, design tokens e utilitários de design
 */

import { Injectable, signal, computed, effect, Renderer2, RendererFactory2 } from '@angular/core';
import { inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { designTokens, DesignSystem, componentStyles } from './design-tokens';

export type Theme = 'light' | 'dark' | 'auto';
export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type ComponentVariant = 'default' | 'primary' | 'secondary' | 'ghost' | 'danger' | 'error' | 'success';

@Injectable({
  providedIn: 'root'
})
export class DesignSystemService {
  private readonly document = inject(DOCUMENT);
  private readonly rendererFactory = inject(RendererFactory2);
  private renderer: Renderer2;

  // ESTADO REATIVO DO TEMA
  private readonly themeSignal = signal<Theme>('auto');
  private readonly systemPrefersDark = signal<boolean>(false);
  
  // COMPUTED VALUES
  public readonly currentTheme = computed(() => this.themeSignal());
  public readonly isDarkMode = computed(() => {
    const theme = this.themeSignal();
    if (theme === 'dark') return true;
    if (theme === 'light') return false;
    return this.systemPrefersDark();
  });

  // CONFIGURAÇÕES DE ANIMAÇÃO
  private readonly reducedMotion = signal<boolean>(false);
  public readonly animationsEnabled = computed(() => !this.reducedMotion());

  // BREAKPOINTS ATIVOS
  private readonly activeBreakpoints = signal<Set<string>>(new Set());
  public readonly breakpoints = computed(() => this.activeBreakpoints());

  constructor() {
    this.renderer = this.rendererFactory.createRenderer(null, null);
    this.initializeThemeDetection();
    this.initializeBreakpointDetection();
    this.initializeAccessibilityPreferences();
    this.setupThemeEffect();
  }

  // ============================================
  // THEME MANAGEMENT
  // ============================================

  /**
   * Define o tema da aplicação
   */
  setTheme(theme: Theme): void {
    this.themeSignal.set(theme);
    this.saveThemeToStorage(theme);
  }

  /**
   * Alterna entre temas claro e escuro
   */
  toggleTheme(): void {
    const current = this.themeSignal();
    if (current === 'auto') {
      this.setTheme(this.systemPrefersDark() ? 'light' : 'dark');
    } else {
      this.setTheme(current === 'light' ? 'dark' : 'light');
    }
  }

  /**
   * Obtém classes CSS baseadas no tema atual
   */
  getThemeClasses(): string[] {
    const classes = ['transition-colors', 'duration-200'];
    if (this.isDarkMode()) {
      classes.push('dark');
    }
    return classes;
  }

  // ============================================
  // DESIGN TOKENS ACCESS
  // ============================================

  /**
   * Obtém cor do design system
   */
  getColor(colorName: keyof typeof designTokens.colors, shade: number = 500): string {
    return DesignSystem.getColor(colorName, shade.toString() as any);
  }

  /**
   * Obtém sombra do design system
   */
  getShadow(shadowName: keyof typeof designTokens.shadows): string {
    return DesignSystem.getShadow(shadowName);
  }

  /**
   * Obtém espaçamento do design system
   */
  getSpacing(spacingName: keyof typeof designTokens.spacing): string {
    return DesignSystem.getSpacing(spacingName);
  }

  /**
   * Cria gradiente personalizado
   */
  createGradient(startColor: string, endColor: string, direction: string = '135deg'): string {
    return DesignSystem.createGradient(startColor, endColor, direction);
  }

  // ============================================
  // COMPONENT STYLING
  // ============================================

  /**
   * Gera classes para botões
   */
  getButtonClasses(variant: ComponentVariant = 'primary', size: ComponentSize = 'md'): string {
    const base = componentStyles.button.base;
    const variantClass = componentStyles.button.variants[variant as keyof typeof componentStyles.button.variants] || componentStyles.button.variants.primary;
    const sizeClass = componentStyles.button.sizes[size];
    
    let classes = `${base} ${sizeClass} ${variantClass}`;
    
    // Adiciona efeitos se animações estiverem habilitadas
    if (this.animationsEnabled()) {
      classes += ' hover:scale-105 active:scale-95 transform';
    }
    
    return classes;
  }

  /**
   * Gera classes para inputs
   */
  getInputClasses(variant: ComponentVariant = 'default', size: ComponentSize = 'md'): string {
    const base = componentStyles.input.base;
    const variantClass = componentStyles.input.variants[variant as keyof typeof componentStyles.input.variants] || componentStyles.input.variants.default;
    const sizeClass = componentStyles.input.sizes[size];
    
    let classes = `${base} ${sizeClass} ${variantClass}`;
    
    // Tema escuro
    if (this.isDarkMode()) {
      classes += ' bg-gray-800 border-gray-700 text-white placeholder:text-gray-500';
    }
    
    return classes;
  }

  /**
   * Gera classes para cards
   */
  getCardClasses(variant: 'elevated' | 'flat' | 'glass' = 'elevated', padding: ComponentSize = 'md'): string {
    const base = componentStyles.card.base;
    const variantClass = componentStyles.card.variants[variant];
    const paddingClass = componentStyles.card.padding[padding];
    
    let classes = `${base} ${variantClass} ${paddingClass}`;
    
    // Tema escuro
    if (this.isDarkMode()) {
      if (variant === 'elevated') {
        classes = classes.replace('bg-white', 'bg-gray-800').replace('border-gray-100', 'border-gray-700');
      } else if (variant === 'flat') {
        classes = classes.replace('bg-white', 'bg-gray-800').replace('border-gray-200', 'border-gray-700');
      } else if (variant === 'glass') {
        classes += ' glass-container-dark';
      }
    }
    
    return classes;
  }

  // ============================================
  // RESPONSIVE UTILITIES
  // ============================================

  /**
   * Verifica se um breakpoint está ativo
   */
  isBreakpointActive(breakpoint: string): boolean {
    return this.activeBreakpoints().has(breakpoint);
  }

  /**
   * Obtém o breakpoint ativo mais largo
   */
  getCurrentBreakpoint(): string {
    const breakpoints = ['xs', 'sm', 'md', 'lg', 'xl', '2xl'];
    const active = this.activeBreakpoints();
    
    for (let i = breakpoints.length - 1; i >= 0; i--) {
      if (active.has(breakpoints[i])) {
        return breakpoints[i];
      }
    }
    
    return 'xs';
  }

  /**
   * Gera classes responsivas
   */
  getResponsiveClasses(classes: { [breakpoint: string]: string }): string {
    const current = this.getCurrentBreakpoint();
    const breakpointOrder = ['xs', 'sm', 'md', 'lg', 'xl', '2xl'];
    const currentIndex = breakpointOrder.indexOf(current);
    
    let applicableClasses = '';
    
    // Aplica classes do breakpoint atual e menores
    for (let i = 0; i <= currentIndex; i++) {
      const bp = breakpointOrder[i];
      if (classes[bp]) {
        applicableClasses += ` ${classes[bp]}`;
      }
    }
    
    return applicableClasses.trim();
  }

  // ============================================
  // ANIMATION UTILITIES
  // ============================================

  /**
   * Cria animação CSS personalizada
   */
  createAnimation(name: string, duration: string = '300ms', easing: string = 'ease-out'): string {
    if (!this.animationsEnabled()) {
      return '';
    }
    
    return `animation: ${name} ${duration} ${easing}`;
  }

  /**
   * Obtém classes de transição baseadas nas preferências de acessibilidade
   */
  getTransitionClasses(property: string = 'all', duration: string = '200'): string {
    if (!this.animationsEnabled()) {
      return '';
    }
    
    const easingFunction = designTokens.animations.easing['ease-out-quart'];
    return `transition-${property} duration-${duration} ${easingFunction}`;
  }

  // ============================================
  // ACCESSIBILITY UTILITIES
  // ============================================

  /**
   * Gera classes de foco acessíveis
   */
  getFocusClasses(color: keyof typeof designTokens.colors = 'primary'): string {
    const baseClasses = 'focus:outline-none focus:ring-4';
    const colorClass = `focus:ring-${color}-200`;
    
    if (this.isDarkMode()) {
      return `${baseClasses} focus:ring-${color}-800`;
    }
    
    return `${baseClasses} ${colorClass}`;
  }

  /**
   * Obtém classes de contraste para acessibilidade
   */
  getHighContrastClasses(): string {
    if (this.hasHighContrastPreference()) {
      return 'high-contrast border-2 border-current';
    }
    return '';
  }

  // ============================================
  // PRIVATE METHODS
  // ============================================

  private initializeThemeDetection(): void {
    // Detecta preferência do sistema
    if (typeof window !== 'undefined') {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      this.systemPrefersDark.set(mediaQuery.matches);
      
      mediaQuery.addEventListener('change', (e) => {
        this.systemPrefersDark.set(e.matches);
      });
      
      // Carrega tema salvo ou usa preferência do sistema
      const savedTheme = this.getThemeFromStorage();
      if (savedTheme) {
        this.themeSignal.set(savedTheme);
      }
    }
  }

  private initializeBreakpointDetection(): void {
    if (typeof window !== 'undefined') {
      const updateBreakpoints = () => {
        const active = new Set<string>();
        const width = window.innerWidth;
        
        if (width >= 475) active.add('xs');
        if (width >= 640) active.add('sm');
        if (width >= 768) active.add('md');
        if (width >= 1024) active.add('lg');
        if (width >= 1280) active.add('xl');
        if (width >= 1536) active.add('2xl');
        
        this.activeBreakpoints.set(active);
      };
      
      updateBreakpoints();
      window.addEventListener('resize', updateBreakpoints);
    }
  }

  private initializeAccessibilityPreferences(): void {
    if (typeof window !== 'undefined') {
      // Detecta preferência de movimento reduzido
      const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
      this.reducedMotion.set(motionQuery.matches);
      
      motionQuery.addEventListener('change', (e) => {
        this.reducedMotion.set(e.matches);
      });
    }
  }

  private setupThemeEffect(): void {
    effect(() => {
      const isDark = this.isDarkMode();
      
      if (typeof document !== 'undefined') {
        if (isDark) {
          this.renderer.addClass(document.documentElement, 'dark');
        } else {
          this.renderer.removeClass(document.documentElement, 'dark');
        }
      }
    });
  }

  private saveThemeToStorage(theme: Theme): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('theme', theme);
    }
  }

  private getThemeFromStorage(): Theme | null {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem('theme') as Theme;
    }
    return null;
  }

  private hasHighContrastPreference(): boolean {
    if (typeof window !== 'undefined') {
      return window.matchMedia('(prefers-contrast: high)').matches;
    }
    return false;
  }
}

// DECORADOR PARA INJEÇÃO DE DEPENDÊNCIA
export function InjectDesignSystem() {
  return inject(DesignSystemService);
}