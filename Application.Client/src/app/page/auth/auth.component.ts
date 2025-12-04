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
  styleUrls: ['./auth.component.scss'],
})
export class AuthComponent implements OnInit {
  loginForm!: FormGroup;
  loginSubmitted: boolean = false;
  activeTheme = this.themeService.getActiveTheme();
  showPassword = false;

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
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
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
    this.router.navigate(['/forgot-password'], {
      queryParams: {
        email: this.loginForm.get('email')?.value || undefined,
      },
    });
  }

  onLogin(): void {
    this.loginSubmitted = true;
    if (this.loginForm.invalid) {
      this.notificationService.showError(this.translateService.instant('auth.formError'));
      return;
    }
    const { email, password } = this.loginForm?.value;
    this.authService.login(email, password).subscribe({
      next: (response: { token: any }) => {
        if (response && response.token) {
          this.authService.saveToken(response.token);
          this.notificationService.showSuccess(this.translateService.instant('auth.loginSuccess'));
          this.loginForm?.reset();
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
