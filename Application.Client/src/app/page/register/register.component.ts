import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidatorFn } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { User } from '../../model/User';
import { AuthService } from '../../service/auth/auth.service';
import { NotificationService } from '../../service/notification/notification.service';
import { ThemeService } from '../../service/theme/theme.service';
import { HttpErrorResponse } from '@angular/common/http';
import { DEFAULT_USER_PERMISSIONS } from '../../model/permissions';
import { passwordValidators } from '../../shared/validators/password-rules';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  submitted = false;
  activeTheme = this.themeService.getActiveTheme();
  isRegistering = false;
  passwordVisibility = {
    password: false,
    confirmPassword: false,
  };

  private passwordMatchValidator: ValidatorFn = (group: AbstractControl) => {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    if (!password || !confirmPassword) {
      return null;
    }
    return password === confirmPassword ? null : { passwordsMismatch: true };
  };

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private notificationService: NotificationService,
    private translateService: TranslateService,
    private router: Router,
    private themeService: ThemeService,
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', passwordValidators()],
      confirmPassword: ['', Validators.required],
      updates: [true],
    }, { validators: this.passwordMatchValidator });
  }

  toggleTheme(): void {
    this.activeTheme = this.themeService.toggleTheme();
  }

  goToLogin(): void {
    this.router.navigate(['/auth']);
  }

  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    this.passwordVisibility[field] = !this.passwordVisibility[field];
  }

  getPasswordStrength(): number {
    const password = this.registerForm.get('password')?.value || '';
    let strength = 0;
    
    if (password.length >= 8) strength += 25;
    if (/[A-Z]/.test(password)) strength += 25;
    if (/[a-z]/.test(password)) strength += 25;
    if (/[0-9]/.test(password) || /[^A-Za-z0-9]/.test(password)) strength += 25;
    
    return strength;
  }

  getPasswordStrengthClass(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 25) return 'bg-red-500';
    if (strength <= 50) return 'bg-orange-500';
    if (strength <= 75) return 'bg-yellow-500';
    return 'bg-green-500';
  }

  getPasswordStrengthText(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 25) return 'auth.register.passwordStrength.weak';
    if (strength <= 50) return 'auth.register.passwordStrength.fair';
    if (strength <= 75) return 'auth.register.passwordStrength.good';
    return 'auth.register.passwordStrength.strong';
  }

  getPasswordStrengthTextClass(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 25) return 'text-red-500';
    if (strength <= 50) return 'text-orange-500';
    if (strength <= 75) return 'text-yellow-500';
    return 'text-green-500';
  }

  private buildErrorMessage(error: any): string {
    const generic = this.translateService.instant('auth.signupError');
    if (!error) {
      return generic;
    }

    // Quando vem um HttpErrorResponse, a carga útil costuma estar em error.error
    const apiError = (error instanceof HttpErrorResponse ? error.error : error) ?? error;

    if (typeof apiError === 'string') {
      return apiError;
    }

    if (apiError?.errors) {
      const flat = Object.values(apiError.errors).flat() as string[];
      if (flat.length) {
        return flat.join(' | ');
      }
    }

    const detail = apiError?.detail || apiError?.message || apiError?.title;
    return detail || generic;
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.registerForm.invalid) {
      return;
    }

    this.isRegistering = true;
    const { fullName, email, password } = this.registerForm.value;

    const newUser: User = {
      Account: {
        Email: email,
        Password: password,
        Permissions: [...DEFAULT_USER_PERMISSIONS],
        DateJoined: new Date(),
      },
      Profile: {
        Name: fullName,
        ProfilePictureUrl: ''
      },
    };

    this.authService.signup(newUser).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translateService.instant('auth.signupSuccess'));
        this.passwordVisibility = { password: false, confirmPassword: false };
        
        // Delayed navigation for smooth UX
        setTimeout(() => {
          this.router.navigate(['/auth'], { 
            queryParams: { email: email, registered: 'true' } 
          });
        }, 1500);
      },
      error: (err) => {
        this.isRegistering = false;
        const message = this.buildErrorMessage(err);
        this.notificationService.showError(message);
      },
    });
  }
}
