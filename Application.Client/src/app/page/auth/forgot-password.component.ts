import { Component, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, interval, Subscription } from 'rxjs';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';
import { passwordValidators } from '../../shared/validators/password-rules';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  private static readonly CODE_LENGTH = 6;
  emailForm: FormGroup;
  codeForm: FormGroup;
  passwordForm: FormGroup;
  step: 'email' | 'code' | 'password' = 'email';
  isSendingCode = false;
  isVerifyingCode = false;
  isSavingPassword = false;
  hasSentCode = false;
  codeValidated = false;
  codeError: string | null = null;
  codeDigits: string[] = new Array(ForgotPasswordComponent.CODE_LENGTH).fill('');
  cooldown = 0;
  private cooldownSub?: Subscription;
  passwordVisibility = {
    newPassword: false,
    confirmPassword: false,
  };
  @ViewChildren('codeInput') codeInputs!: QueryList<ElementRef<HTMLInputElement>>;

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });

    this.codeForm = this.fb.group({
      code: [
        '',
        [Validators.required, Validators.pattern(`^\\d{${ForgotPasswordComponent.CODE_LENGTH}}$`)],
      ],
    });

    this.passwordForm = this.fb.group(
      {
        newPassword: [
          '',
          passwordValidators(),
        ],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: [this.passwordsMatchValidator] }
    );
  }

  ngOnInit(): void {
    const savedEmail = localStorage.getItem('reset-email');
    const queryEmail = this.route.snapshot.queryParamMap.get('email');
    const queryCode = this.route.snapshot.queryParamMap.get('code');
    const emailToUse = queryEmail || savedEmail;

    if (emailToUse) {
      this.emailForm.patchValue({ email: emailToUse });
    }
    if (queryCode) {
      this.applyCodeValue(queryCode);
      this.step = 'code';
    }

    this.codeForm.get('code')?.valueChanges.subscribe(() => {
      const code = this.codeForm.get('code')?.value || '';
      if (!code) {
        this.resetCodeInputs();
      }
      this.codeValidated = false;
      this.codeError = null;
      if (this.step === 'password') {
        this.step = 'code';
      }
    });

    if (emailToUse && queryCode && queryCode.length === ForgotPasswordComponent.CODE_LENGTH) {
      this.verifyCode(true);
    }
  }

  ngOnDestroy(): void {
    this.cooldownSub?.unsubscribe();
  }

  sendCode(): void {
    if (this.cooldown > 0) {
      return;
    }
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      this.notificationService.showWarning(this.translate.instant('auth.resetEmailRequired'));
      return;
    }

    const email = this.emailForm.get('email')?.value;
    this.isSendingCode = true;
    this.userService.generateRecoveryCode(true, email).subscribe({
      next: (res) => {
        localStorage.setItem('reset-email', email);
        this.step = 'code';
        this.codeValidated = false;
        this.codeError = null;
        this.hasSentCode = true;
        this.codeForm.reset();
        this.resetCodeInputs();
        this.notificationService.showSuccess(this.translate.instant('auth.resetCodeSent'));
        this.startCooldown();
      },
      error: (error) => {
        const detail = error?.error?.detail || this.translate.instant('auth.resetFailed');
        this.notificationService.showError(detail);
        this.isSendingCode = false;
      },
      complete: () => {
        this.isSendingCode = false;
      },
    });
  }

  onCodeInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const digit = input.value.replace(/\D/g, '').slice(-1);
    this.codeDigits[index] = digit;
    input.value = digit;
    this.clearFollowing(index);
    
    this.syncCodeForm();

    if (digit && index < this.codeDigits.length - 1) {
      this.focusCodeInput(index + 1);
    } else if (this.isCodeComplete()) {
      this.verifyCode();
    }
  }

  onCodePaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasted = event.clipboardData?.getData('text') ?? '';
    this.applyCodeValue(pasted);
    if (this.isCodeComplete()) {
      this.verifyCode();
    }
  }

  private applyCodeValue(code: string): void {
    const digits = code.replace(/\D/g, '').slice(0, this.codeDigits.length).split('');
    this.codeDigits = new Array(this.codeDigits.length).fill('');
    digits.forEach((d, idx) => (this.codeDigits[idx] = d));
    this.codeForm.patchValue({ code: this.codeDigits.join('') });
    setTimeout(() => this.focusFirstEmptyCodeInput(), 0);
  }

  private resetCodeInputs(): void {
    this.codeDigits = new Array(this.codeDigits.length).fill('');
    this.focusFirstEmptyCodeInput()
  }

  private isCodeComplete(): boolean {
    return this.codeDigits.join('').length === this.codeDigits.length && !this.codeDigits.includes('');
  }

  private focusCodeInput(index: number): void {
    const el = this.codeInputs?.get(index)?.nativeElement;
    el?.focus();
    el?.select();
  }

  private focusFirstEmptyCodeInput(): void {
    const firstEmpty = this.codeDigits.findIndex((d) => d === '');
    this.focusCodeInput(firstEmpty >= 0 ? firstEmpty : 0);
  }

  private syncCodeForm(): void {
    this.codeForm.patchValue({ code: this.codeDigits.join('') });
  }

  private clearFollowing(fromIndex: number): void {
    for (let i = fromIndex + 1; i < this.codeDigits.length; i++) {
      if (this.codeDigits[i]) {
        this.codeDigits[i] = '';
      }
    }
  }

  private startCooldown(seconds: number = 60): void {
    this.cooldown = seconds;
    this.cooldownSub?.unsubscribe();
    this.cooldownSub = interval(1000).subscribe(() => {
      this.cooldown = Math.max(0, this.cooldown - 1);
      if (this.cooldown === 0) {
        this.cooldownSub?.unsubscribe();
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/auth']);
  }

  async verifyCode(isInitial: boolean = false): Promise<void> {
    if (this.isVerifyingCode) {
      return;
    }
    if (this.emailForm.invalid || this.codeForm.invalid) {
      this.emailForm.markAllAsTouched();
      this.codeForm.markAllAsTouched();
      if (!isInitial) {
        this.notificationService.showWarning(this.translate.instant('auth.reset.validateFirst'));
      }
      return;
    }
    const email = this.emailForm.get('email')?.value;
    const code = this.codeForm.get('code')?.value;
    this.isVerifyingCode = true;
    try {
      await firstValueFrom(this.userService.verifyRecoveryCode({ email, code }));
      this.codeValidated = true;
      this.codeError = null;
      this.step = 'password';
      this.notificationService.showSuccess(this.translate.instant('auth.reset.codeValidated'));
    } catch (error: any) {
      this.codeValidated = false;
      const detail = error?.error?.detail || error?.error?.title || this.translate.instant('auth.resetFailed');
      this.codeError = detail;
      this.notificationService.showError(detail);
      this.applyCodeValue('');
    } finally {
      this.isVerifyingCode = false;
    }
  }

  togglePasswordVisibility(field: 'newPassword' | 'confirmPassword'): void {
    this.passwordVisibility[field] = !this.passwordVisibility[field];
  }

  submitPassword(): void {
    if (this.emailForm.invalid || this.codeForm.invalid) {
      this.emailForm.markAllAsTouched();
      this.codeForm.markAllAsTouched();
      this.notificationService.showWarning(this.translate.instant('auth.resetPasswordInvalid'));
      return;
    }
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      const key = this.passwordForm.hasError('passwordMismatch')
        ? 'profile.messages.passwordMismatch'
        : 'profile.messages.passwordFormInvalid';
      this.notificationService.showWarning(this.translate.instant(key));
      return;
    }
    if (!this.codeValidated) {
      this.notificationService.showWarning(this.translate.instant('auth.reset.validateFirst'));
      return;
    }

    const email = this.emailForm.get('email')?.value;
    const code = this.codeForm.get('code')?.value;
    const { newPassword } = this.passwordForm.getRawValue();
    this.isSavingPassword = true;
    this.userService.changePasswordWithCode({ email, code, newPassword }).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('auth.resetPasswordSuccess'));
        this.passwordVisibility = { newPassword: false, confirmPassword: false };
        this.codeValidated = false;
        this.passwordForm.reset();
        this.codeForm.reset();
        this.resetCodeInputs();
        this.step = 'email';
        this.router.navigate(['/auth']);
      },
      error: (error) => {
        const detail = error?.error?.detail || error?.error?.title || this.translate.instant('auth.resetFailed');
        this.notificationService.showError(detail);
        this.isSavingPassword = false;
      },
      complete: () => {
        this.isSavingPassword = false;
      },
    });
  }

  private passwordsMatchValidator(group: FormGroup) {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }
}
