import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../service/notification/notification.service';
import { AuthService } from '../../service/auth/auth.service';
import { TranslateService } from '@ngx-translate/core';
import {
  FormGroup,
  FormBuilder,
  Validators,
} from '@angular/forms';
import { ThemeService } from '../../service/theme/theme.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
})
export class AuthComponent implements OnInit {
  loginForm!: FormGroup;
  loginSubmitted: boolean = false;
  activeTheme = this.themeService.getActiveTheme();
  showPassword = false;
  loginMode: 'local' | 'moodle' = 'local';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService,
    private translateService: TranslateService,
    private themeService: ThemeService,
  ) {}

  ngOnInit() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      username: [''],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [true],
    });

    this.setValidatorsForMode(this.loginMode);
  }

  setMode(mode: 'local' | 'moodle'): void {
    if (this.loginMode === mode) {
      return;
    }

    this.loginMode = mode;
    this.setValidatorsForMode(mode);
    this.loginForm.updateValueAndValidity();
    this.loginSubmitted = false;
    this.showPassword = false;
  }

  private setValidatorsForMode(mode: 'local' | 'moodle'): void {
    const emailControl = this.loginForm.get('email');
    const usernameControl = this.loginForm.get('username');

    if (!emailControl || !usernameControl) {
      return;
    }

    if (mode === 'local') {
      emailControl.setValidators([Validators.required, Validators.email]);
      usernameControl.clearValidators();
    } else {
      usernameControl.setValidators([Validators.required]);
      emailControl.clearValidators();
    }

    emailControl.updateValueAndValidity();
    usernameControl.updateValueAndValidity();
  }

  toggleTheme(): void {
    this.activeTheme = this.themeService.toggleTheme();
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    if (this.loginMode === 'moodle') {
      this.notificationService.showWarning(this.translateService.instant('auth.login.moodleRecoveryNotAvailable'));
      return;
    }
    this.router.navigate(['/forgot-password'], {
      queryParams: {
        email: this.loginForm.get('email')?.value || undefined,
      },
    });
  }

  onLogin(): void {
    this.loginSubmitted = true;
    this.setValidatorsForMode(this.loginMode);

    if (this.loginForm.invalid) {
      this.notificationService.showError(this.translateService.instant('auth.formError'));
      return;
    }
    const { email, username, password, rememberMe } = this.loginForm?.value;

    const login$ = this.loginMode === 'moodle'
      ? this.authService.loginMoodle(username, password, rememberMe)
      : this.authService.login(email, password, rememberMe);

    login$.subscribe({
      next: (response: { token: any }) => {
        if (response && response.token) {
          this.notificationService.showSuccess(this.translateService.instant('auth.loginSuccess'));
          this.loginForm?.reset({ rememberMe: true });
          this.showPassword = false;
          this.router.navigate(['/home']);
        }
      },
      error: () => {
        this.notificationService.showError(
          this.translateService.instant('auth.loginError')
        );
      },
    });
  }
}
