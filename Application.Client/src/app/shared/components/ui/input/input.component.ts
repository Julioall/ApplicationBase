/**
 * INPUT COMPONENT
 * Componente de input simplificado que implementa o design system
 */

import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

export type ComponentSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ],
  template: `
    <div class="form-group">
      <!-- Floating Label Input -->
      @if (floatingLabel) {
        <div class="relative">
          <input
            [id]="inputId"
            [type]="type"
            [placeholder]="' '"
            [disabled]="disabled"
            [readonly]="readonly"
            [required]="required"
            [class]="getInputClasses()"
            [value]="currentValue"
            (input)="onInput($event)"
            (blur)="onBlur($event)"
            (focus)="onFocus($event)"
            [attr.aria-label]="ariaLabel"
            [attr.autocomplete]="autocomplete"
          />
          <label 
            [for]="inputId"
            [class]="getLabelClasses()"
          >
            {{ label }}
            @if (required) {
              <span class="text-red-500 ml-1">*</span>
            }
          </label>
          
          <!-- Leading Icon -->
          @if (leadingIcon) {
            <div class="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 pointer-events-none">
              <ng-content select="[slot=leading-icon]"></ng-content>
            </div>
          }
          
          <!-- Password Toggle -->
          @if (showPasswordToggle && (type === 'password' || type === 'text')) {
            <button
              type="button"
              class="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              (click)="togglePasswordVisibility()"
            >
              <i [class]="passwordVisible ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
            </button>
          }
        </div>
      }
      
      <!-- Standard Input -->
      @if (!floatingLabel) {
        @if (label) {
          <label 
            [for]="inputId" 
            class="block text-sm font-medium text-gray-700 mb-2"
          >
            {{ label }}
            @if (required) {
              <span class="text-red-500 ml-1">*</span>
            }
          </label>
        }
        
        <div class="relative">
          <input
            [id]="inputId"
            [type]="type"
            [placeholder]="placeholder"
            [disabled]="disabled"
            [readonly]="readonly"
            [required]="required"
            [class]="getInputClasses()"
            [value]="currentValue"
            (input)="onInput($event)"
            (blur)="onBlur($event)"
            (focus)="onFocus($event)"
            [attr.aria-label]="ariaLabel"
            [attr.autocomplete]="autocomplete"
          />
          
          <!-- Leading Icon -->
          @if (leadingIcon) {
            <div class="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 pointer-events-none">
              <ng-content select="[slot=leading-icon]"></ng-content>
            </div>
          }
          
          <!-- Password Toggle -->
          @if (showPasswordToggle && (type === 'password' || type === 'text')) {
            <button
              type="button"
              class="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              (click)="togglePasswordVisibility()"
            >
              <i [class]="passwordVisible ? 'fas fa-eye-slash' : 'fas fa-eye'"></i>
            </button>
          }
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
export class InputComponent implements ControlValueAccessor {
  @Input() label?: string;
  @Input() placeholder = '';
  @Input() type: 'text' | 'email' | 'password' | 'number' = 'text';
  @Input() disabled = false;
  @Input() readonly = false;
  @Input() required = false;
  @Input() size: ComponentSize = 'md';
  @Input() floatingLabel = false;
  @Input() leadingIcon = false;
  @Input() showPasswordToggle = false;
  @Input() ariaLabel?: string;
  @Input() autocomplete?: string;
  
  @Output() inputChange = new EventEmitter<Event>();
  @Output() inputBlur = new EventEmitter<FocusEvent>();
  @Output() inputFocus = new EventEmitter<FocusEvent>();
  
  currentValue = '';
  passwordVisible = false;
  isFocused = false;
  inputId = `input-${Math.random().toString(36).substr(2, 9)}`;
  
  // ControlValueAccessor implementation
  private onChange = (value: any) => {};
  private onTouch = () => {};
  
  writeValue(value: any): void {
    this.currentValue = value || '';
  }
  
  registerOnChange(fn: any): void {
    this.onChange = fn;
  }
  
  registerOnTouched(fn: any): void {
    this.onTouch = fn;
  }
  
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
  
  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.currentValue = target.value;
    this.onChange(this.currentValue);
    this.inputChange.emit(event);
  }
  
  onBlur(event: FocusEvent): void {
    this.isFocused = false;
    this.onTouch();
    this.inputBlur.emit(event);
  }
  
  onFocus(event: FocusEvent): void {
    this.isFocused = true;
    this.inputFocus.emit(event);
  }
  
  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
    this.type = this.passwordVisible ? 'text' : 'password';
  }
  
  getInputClasses(): string {
    const baseClasses = 'w-full border rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent';
    
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
        classes += ' px-4 py-3 text-lg';
        break;
      case 'xl':
        classes += ' px-5 py-4 text-xl';
        break;
    }
    
    // Floating label specific styles
    if (this.floatingLabel) {
      classes += ' peer placeholder-transparent';
    }
    
    // Leading icon padding
    if (this.leadingIcon) {
      classes += ' pl-10';
    }
    
    // Password toggle padding
    if (this.showPasswordToggle) {
      classes += ' pr-10';
    }
    
    // State classes
    if (this.disabled) {
      classes += ' bg-gray-100 cursor-not-allowed';
    } else {
      classes += ' bg-white hover:border-gray-400';
    }
    
    classes += ' border-gray-300';
    
    return classes;
  }
  
  getLabelClasses(): string {
    const baseClasses = 'absolute left-4 text-gray-500 transition-all duration-200 pointer-events-none';
    
    let classes = baseClasses;
    
    if (this.floatingLabel) {
      if (this.currentValue || this.isFocused) {
        classes += ' -top-2 text-xs bg-white px-1 text-primary-600';
      } else {
        classes += ' top-1/2 transform -translate-y-1/2 text-base';
      }
    }
    
    return classes;
  }
}

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ],
  template: `
    <div class="form-group" [class]="containerClasses()">
      <!-- Standard Input -->
      @if (!floatingLabel) {
        @if (label) {
          <label 
            [for]="inputId" 
            class="form-label"
            [class]="labelClasses()"
          >
            {{ label }}
            @if (required) {
              <span class="text-error-500 ml-1">*</span>
            }
          </label>
        }
        
        <input
          #inputRef
          [id]="inputId"
          [type]="type"
          [placeholder]="placeholder"
          [disabled]="disabled"
          [readonly]="readonly"
          [required]="required"
          [class]="inputClasses()"
          [value]="value()"
          (input)="onInput($event)"
          (blur)="onBlur($event)"
          (focus)="onFocus($event)"
          [attr.aria-label]="ariaLabel"
          [attr.aria-describedby]="hasError() ? errorId : helpId"
          [attr.aria-invalid]="hasError()"
          [attr.autocomplete]="autocomplete"
        />
      }
      
      <!-- Floating Label Input -->
      @if (floatingLabel) {
        <div [class]="floatingContainerClasses()">
          <input
            #inputRef
            [id]="inputId"
            [type]="type"
            [placeholder]="' '"
            [disabled]="disabled"
            [readonly]="readonly"
            [required]="required"
            [class]="floatingInputClasses()"
            [value]="value()"
            (input)="onInput($event)"
            (blur)="onBlur($event)"
            (focus)="onFocus($event)"
            [attr.aria-label]="ariaLabel"
            [attr.aria-describedby]="hasError() ? errorId : helpId"
            [attr.aria-invalid]="hasError()"
            [attr.autocomplete]="autocomplete"
          />
          <label 
            [for]="inputId"
            [class]="floatingLabelClasses()"
          >
            {{ label }}
            @if (required) {
              <span class="text-error-500 ml-1">*</span>
            }
          </label>
        </div>
      }
      
      <!-- Leading Icon -->
      @if (leadingIcon) {
        <div class="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400">
          <ng-content select="[slot=leading-icon]"></ng-content>
        </div>
      }
      
      <!-- Trailing Icon/Action -->
      @if (trailingIcon || clearable || showPasswordToggle) {
        <div class="absolute right-3 top-1/2 transform -translate-y-1/2 flex items-center gap-2">
          @if (clearable && value() && !disabled) {
            <button
              type="button"
              class="text-gray-400 hover:text-gray-600 transition-colors"
              (click)="clear()"
              [attr.aria-label]="'Limpar ' + (label || 'campo')"
            >
              <!-- Clear Icon -->
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          }
          
          @if (showPasswordToggle && type === 'password') {
            <button
              type="button"
              class="text-gray-400 hover:text-gray-600 transition-colors"
              (click)="togglePassword()"
              [attr.aria-label]="showPassword ? 'Ocultar senha' : 'Mostrar senha'"
            >
              <!-- Eye Icon -->
              @if (!showPassword) {
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
              }
              @if (showPassword) {
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L12 12m3.878-3.878L21 21" />
                </svg>
              }
            </button>
          }
          
          @if (trailingIcon) {
            <ng-content select="[slot=trailing-icon]"></ng-content>
          }
        </div>
      }
      
      <!-- Help Text -->
      @if (helpText && !hasError()) {
        <p [id]="helpId" class="form-help">
          {{ helpText }}
        </p>
      }
      
      <!-- Error Message -->
      @if (hasError()) {
        <div [id]="errorId" class="form-error">
          <!-- Error Icon -->
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {{ errorMessage }}
        </div>
      }
      
      <!-- Success Message -->
      @if (hasSuccess()) {
        <div class="form-success">
          <!-- Success Icon -->
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          {{ successMessage }}
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    
    .form-group {
      position: relative;
    }
    
    .form-help {
      @apply mt-2 text-sm text-gray-600 dark:text-gray-400;
    }
  `]
})
export class InputComponent implements ControlValueAccessor {
  @ViewChild('inputRef') inputRef!: ElementRef<HTMLInputElement>;
  
  @Input() label?: string;
  @Input() placeholder?: string;
  @Input() type: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search' = 'text';
  @Input() size: ComponentSize = 'md';
  @Input() variant: ComponentVariant = 'default';
  @Input() disabled = false;
  @Input() readonly = false;
  @Input() required = false;
  @Input() errorMessage?: string;
  @Input() successMessage?: string;
  @Input() helpText?: string;
  @Input() leadingIcon = false;
  @Input() trailingIcon = false;
  @Input() clearable = false;
  @Input() showPasswordToggle = false;
  @Input() floatingLabel = true;
  @Input() ariaLabel?: string;
  @Input() autocomplete?: string;
  
  @Output() inputChange = new EventEmitter<string>();
  @Output() blur = new EventEmitter<FocusEvent>();
  @Output() focus = new EventEmitter<FocusEvent>();
  
  protected readonly designSystem = new DesignSystemService();
  
  private readonly value = signal('');
  private readonly isFocused = signal(false);
  private readonly showPassword = signal(false);
  
  protected readonly inputId = `input-${Math.random().toString(36).substr(2, 9)}`;
  protected readonly errorId = `${this.inputId}-error`;
  protected readonly helpId = `${this.inputId}-help`;
  
  private onChange = (value: string) => {};
  private onTouched = () => {};
  
  protected readonly hasError = computed(() => !!this.errorMessage);
  protected readonly hasSuccess = computed(() => !!this.successMessage && !this.hasError());
  protected readonly hasValue = computed(() => !!this.value());
  
  protected readonly containerClasses = computed(() => {
    let classes = '';
    
    if (this.leadingIcon) {
      classes += ' relative';
    }
    
    if (this.trailingIcon || this.clearable || this.showPasswordToggle) {
      classes += ' relative';
    }
    
    return classes;
  });
  
  protected readonly labelClasses = computed(() => {
    let classes = 'form-label';
    
    if (this.hasError()) {
      classes += ' text-error-600 dark:text-error-400';
    } else if (this.hasSuccess()) {
      classes += ' text-success-600 dark:text-success-400';
    }
    
    return classes;
  });
  
  protected readonly inputClasses = computed(() => {
    let variant: ComponentVariant = this.variant;
    
    if (this.hasError()) {
      variant = 'error';
    } else if (this.hasSuccess()) {
      variant = 'success';
    }
    
    let classes = this.designSystem.getInputClasses(variant, this.size);
    
    if (this.leadingIcon) {
      classes += ' pl-10';
    }
    
    if (this.trailingIcon || this.clearable || this.showPasswordToggle) {
      classes += ' pr-10';
    }
    
    return classes;
  });
  
  protected readonly floatingContainerClasses = computed(() => {
    let classes = 'floating-label relative';
    
    if (this.hasError()) {
      classes += ' error';
    } else if (this.hasSuccess()) {
      classes += ' success';
    }
    
    return classes;
  });
  
  protected readonly floatingInputClasses = computed(() => {
    let classes = this.inputClasses();
    
    if (this.leadingIcon) {
      classes += ' pl-10';
    }
    
    if (this.trailingIcon || this.clearable || this.showPasswordToggle) {
      classes += ' pr-10';
    }
    
    return classes;
  });
  
  protected readonly floatingLabelClasses = computed(() => {
    let classes = '';
    
    if (this.hasError()) {
      classes += ' text-error-500';
    } else if (this.hasSuccess()) {
      classes += ' text-success-500';
    } else if (this.isFocused()) {
      classes += ' text-primary-500';
    } else {
      classes += ' text-gray-500';
    }
    
    return classes;
  });
  
  constructor() {
    // Atualiza tipo do input quando mostrar/ocultar senha
    effect(() => {
      if (this.inputRef && this.type === 'password') {
        this.inputRef.nativeElement.type = this.showPassword() ? 'text' : 'password';
      }
    });
  }
  
  // ControlValueAccessor Implementation
  writeValue(value: string): void {
    this.value.set(value || '');
  }
  
  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }
  
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
  
  // Event Handlers
  protected onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    const value = target.value;
    
    this.value.set(value);
    this.onChange(value);
    this.inputChange.emit(value);
  }
  
  protected onBlur(event: FocusEvent): void {
    this.isFocused.set(false);
    this.onTouched();
    this.blur.emit(event);
  }
  
  protected onFocus(event: FocusEvent): void {
    this.isFocused.set(true);
    this.focus.emit(event);
  }
  
  protected clear(): void {
    this.value.set('');
    this.onChange('');
    this.inputChange.emit('');
    this.inputRef.nativeElement.focus();
  }
  
  protected togglePassword(): void {
    this.showPassword.set(!this.showPassword());
    this.inputRef.nativeElement.focus();
  }
  
  // Public Methods
  public focus(): void {
    this.inputRef?.nativeElement.focus();
  }
  
  public blur(): void {
    this.inputRef?.nativeElement.blur();
  }
}