/**
 * MODERN LOGIN FORM EXAMPLE
 * Exemplo de formulário de login implementando o design system completo
 */

import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthLayoutComponent } from '../layout/auth-layout.component';
import { ButtonComponent } from '../ui/button/button.component';
import { InputComponent } from '../ui/input/input.component';
import { LoadingComponent } from '../ui/loading/loading.component';
import { DesignSystemService } from '../../services/design-system.service';

@Component({
  selector: 'app-modern-login-example',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AuthLayoutComponent,
    ButtonComponent,
    InputComponent,
    LoadingComponent,
  ],
  template: `
    <app-auth-layout>
      <div id="main-content">
        <!-- Form Header -->
        <div class="text-center mb-8">
          <h2 class="form-title animate-fade-in">
            Bem-vindo de volta
          </h2>
          <p class="form-subtitle animate-delayed-fade-in">
            Entre em sua conta para continuar
          </p>
        </div>
        
        <!-- Login Form -->
        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="space-y-6" novalidate>
          <!-- Email Field -->
          <div class="animate-fade-in-up" style="animation-delay: 0.2s;">
            <app-input
              formControlName="email"
              type="email"
              label="Email"
              placeholder="seu@email.com"
              size="md"
              [errorMessage]="getEmailError()"
              [floatingLabel]="true"
              [leadingIcon]="true"
              autocomplete="email"
              ariaLabel="Digite seu endereço de email"
            >
              <svg slot="leading-icon" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207" />
              </svg>
            </app-input>
          </div>
          
          <!-- Password Field -->
          <div class="animate-fade-in-up" style="animation-delay: 0.3s;">
            <app-input
              formControlName="password"
              type="password"
              label="Senha"
              placeholder="Digite sua senha"
              size="md"
              [errorMessage]="getPasswordError()"
              [floatingLabel]="true"
              [leadingIcon]="true"
              [showPasswordToggle]="true"
              autocomplete="current-password"
              ariaLabel="Digite sua senha"
            >
              <svg slot="leading-icon" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
            </app-input>
          </div>
          
          <!-- Remember Me & Forgot Password -->
          <div class="flex items-center justify-between animate-fade-in-up" style="animation-delay: 0.4s;">
            <label class="flex items-center gap-2 cursor-pointer group">
              <input 
                type="checkbox" 
                formControlName="rememberMe"
                class="w-4 h-4 text-primary-600 bg-gray-100 border-gray-300 rounded focus:ring-primary-500 dark:focus:ring-primary-600 dark:ring-offset-gray-800 focus:ring-2 dark:bg-gray-700 dark:border-gray-600"
              />
              <span class="text-sm text-gray-600 dark:text-gray-400 group-hover:text-gray-800 dark:group-hover:text-gray-200 transition-colors">
                Lembrar-me
              </span>
            </label>
            
            <a 
              href="/forgot-password"
              class="nav-link text-sm font-medium"
            >
              Esqueceu a senha?
            </a>
          </div>
          
          <!-- Submit Button -->
          <div class="animate-fade-in-up" style="animation-delay: 0.5s;">
            <app-button
              type="submit"
              variant="primary"
              size="md"
              [fullWidth]="true"
              [loading]="isLoading()"
              [disabled]="loginForm.invalid || isLoading()"
              ariaLabel="Entrar na conta"
            >
              @if (isLoading()) {
                Entrando...
              } @else {
                Entrar
              }
            </app-button>
          </div>
          
          <!-- Divider -->
          <div class="relative my-8 animate-fade-in-up" style="animation-delay: 0.6s;">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-300 dark:border-gray-600"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-4 bg-transparent text-gray-500 dark:text-gray-400">
                ou continue com
              </span>
            </div>
          </div>
          
          <!-- Social Login Buttons -->
          <div class="grid grid-cols-2 gap-3 animate-fade-in-up" style="animation-delay: 0.7s;">
            <app-button
              type="button"
              variant="secondary"
              size="md"
              (click)="loginWithGoogle()"
              ariaLabel="Entrar com Google"
            >
              <svg slot="leading-icon" class="w-4 h-4" viewBox="0 0 24 24">
                <path fill="#4285f4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34a853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#fbbc05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path fill="#ea4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              Google
            </app-button>
            
            <app-button
              type="button"
              variant="secondary"
              size="md"
              (click)="loginWithMicrosoft()"
              ariaLabel="Entrar com Microsoft"
            >
              <svg slot="leading-icon" class="w-4 h-4" viewBox="0 0 24 24">
                <path fill="#f25022" d="M1 1h10v10H1z"/>
                <path fill="#00a4ef" d="M13 1h10v10H13z"/>
                <path fill="#7fba00" d="M1 13h10v10H1z"/>
                <path fill="#ffb900" d="M13 13h10v10H13z"/>
              </svg>
              Microsoft
            </app-button>
          </div>
        </form>
        
        <!-- Loading Overlay -->
        @if (isLoading()) {
          <app-loading
            type="spinner"
            size="lg"
            overlay="true"
            text="Autenticando..."
            color="white"
            [centered]="true"
          ></app-loading>
        }
      </div>
      
      <!-- Footer Links -->
      <div slot="footer-links" class="space-y-2">
        <p class="text-sm text-gray-600 dark:text-gray-400">
          Não tem uma conta?
          <a href="/register" class="nav-link font-medium ml-1">
            Criar conta
          </a>
        </p>
        
        <div class="flex items-center justify-center gap-4 text-xs text-gray-500">
          <a href="/privacy" class="hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
            Privacidade
          </a>
          <span>•</span>
          <a href="/terms" class="hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
            Termos
          </a>
          <span>•</span>
          <a href="/help" class="hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
            Ajuda
          </a>
        </div>
      </div>
    </app-auth-layout>
  `,
  styles: [`
    :host {
      display: block;
    }
    
    /* Custom animations for form elements */
    .animate-fade-in-up {
      opacity: 0;
      transform: translateY(20px);
      animation: fadeInUp 0.6s ease-out forwards;
    }
    
    /* Accessibility improvements */
    @media (prefers-reduced-motion: reduce) {
      .animate-fade-in-up {
        animation: none;
        opacity: 1;
        transform: none;
      }
    }
    
    /* Focus management */
    form:focus-within .floating-orb {
      opacity: 0.3;
      transition: opacity 0.3s ease;
    }
  `]
})
export class ModernLoginExampleComponent {
  protected readonly designSystem = new DesignSystemService();
  
  private readonly isLoading = signal(false);
  
  protected readonly loginForm: FormGroup;
  
  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }
  
  protected getEmailError(): string | undefined {
    const emailControl = this.loginForm.get('email');
    if (emailControl?.errors && emailControl.touched) {
      if (emailControl.errors['required']) {
        return 'Email é obrigatório';
      }
      if (emailControl.errors['email']) {
        return 'Digite um email válido';
      }
    }
    return undefined;
  }
  
  protected getPasswordError(): string | undefined {
    const passwordControl = this.loginForm.get('password');
    if (passwordControl?.errors && passwordControl.touched) {
      if (passwordControl.errors['required']) {
        return 'Senha é obrigatória';
      }
      if (passwordControl.errors['minlength']) {
        return 'Senha deve ter pelo menos 6 caracteres';
      }
    }
    return undefined;
  }
  
  protected onSubmit(): void {
    if (this.loginForm.valid) {
      this.isLoading.set(true);
      
      // Simula chamada de API
      setTimeout(() => {
        console.log('Login form submitted:', this.loginForm.value);
        this.isLoading.set(false);
        // Aqui você faria a chamada real para o backend
      }, 2000);
    } else {
      // Marca todos os campos como touched para mostrar erros
      Object.keys(this.loginForm.controls).forEach(key => {
        this.loginForm.get(key)?.markAsTouched();
      });
    }
  }
  
  protected loginWithGoogle(): void {
    console.log('Google login initiated');
    // Implementar login com Google
  }
  
  protected loginWithMicrosoft(): void {
    console.log('Microsoft login initiated');
    // Implementar login com Microsoft
  }
}