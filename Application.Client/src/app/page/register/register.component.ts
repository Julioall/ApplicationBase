import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidatorFn } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { User } from '../../model/User';
import { AuthService } from '../../service/auth/auth.service';
import { NotificationService } from '../../service/notification/notification.service';
import { ThemeService } from '../../service/theme/theme.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  submitted = false;
  activeTheme = this.themeService.getActiveTheme();

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
      password: ['', [Validators.required, Validators.minLength(8)]],
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

  private buildErrorMessage(error: HttpErrorResponse): string {
    if (!error) {
      return this.translateService.instant('auth.signupError');
    }

    const apiError = (error.error ?? error) as any;

    if (typeof apiError === 'string') {
      return apiError;
    }

    if (apiError?.errors) {
      const flat = Object.values(apiError.errors).flat() as string[];
      if (flat.length) {
        return flat.join(' | ');
      }
    }

    if (apiError?.message) {
      return apiError.message;
    }

    return this.translateService.instant('auth.signupError');
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.registerForm.invalid) {
      return;
    }

    const { fullName, email, password } = this.registerForm.value;

    const newUser: User = {
      Account: {
        Email: email,
        Password: password,
        Role: 'User',
        DateJoined: new Date(),
      },
      Profile: {
        Name: fullName,
        DateOfBirth: undefined,
        ProfilePictureUrl: '',
        AnimeList: [],
      },
    };

    this.authService.signup(newUser).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translateService.instant('auth.signupSuccess'));
        this.router.navigate(['/auth'], { queryParams: { email } });
      },
      error: (err) => {
        const message = this.buildErrorMessage(err);
        this.notificationService.showError(message);
      },
    });
  }
}
