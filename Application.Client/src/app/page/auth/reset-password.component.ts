import { Component, OnDestroy, OnInit, QueryList, ViewChildren, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, interval, Subscription } from 'rxjs';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';
import { passwordValidators } from '../../shared/validators/password-rules';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss'],
})
export class ResetPasswordComponent implements OnInit, OnDestroy {
  private static readonly CODE_LENGTH = 6;
  resetForm: FormGroup;
  isSaving = false;
  isVerifyingCode = false;
  codeValidated = false;
  codeError: string | null = null;
  codeDigits: string[] = new Array(ResetPasswordComponent.CODE_LENGTH).fill('');
  passwordVisibility = {
    newPassword: false,
    confirmPassword: false,
  };
  resendCooldown = 0;
  private resendSub?: Subscription;
  @ViewChildren('codeInput') codeInputs!: QueryList<ElementRef<HTMLInputElement>>;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly userService: UserService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
  ) {
    this.resetForm = this.fb.group(
      {
        email: ['', [Validators.required, Validators.email]],
        code: [
          '',
          [Validators.required, Validators.pattern(`^\\d{${ResetPasswordComponent.CODE_LENGTH}}$`)],
        ],
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
    const email = this.route.snapshot.queryParamMap.get('email');
    const code = this.route.snapshot.queryParamMap.get('code');
    if (email) {
      this.resetForm.patchValue({ email });
    }
    if (code) {
      this.applyCodeValue(code);
    }
    this.resetForm.get('code')?.valueChanges.subscribe(() => {
      this.codeValidated = false;
      this.codeError = null;
    });
    this.resetForm.get('email')?.valueChanges.subscribe(() => {
      this.codeValidated = false;
      this.codeError = null;
    });
    if (email && code && code.length === ResetPasswordComponent.CODE_LENGTH) {
      this.verifyCodeAsync(true);
    }
  }

  ngOnDestroy(): void {
    this.resendSub?.unsubscribe();
  }

  private passwordsMatchValidator(group: FormGroup) {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  onCodeInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').slice(-1);
    this.codeDigits[index] = value;
    input.value = value;
    this.clearFollowing(index);
    this.refreshInputValues();
    this.syncCodeForm();
    this.codeValidated = false;

    if (value && index < this.codeDigits.length - 1) {
      this.focusCodeInput(index + 1);
    } else if (this.codeDigits.join('').length === this.codeDigits.length && !this.codeDigits.includes('')) {
      this.verifyCodeAsync(true);
    }
  }

  onCodePaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasted = event.clipboardData?.getData('text') ?? '';
    this.applyCodeValue(pasted);
    if (!this.codeDigits.includes('')) {
      this.verifyCodeAsync(true);
    }
  }

  private applyCodeValue(code: string): void {
    const digits = code.replace(/\D/g, '').slice(0, this.codeDigits.length).split('');
    this.codeDigits = new Array(this.codeDigits.length).fill('');
    digits.forEach((d, idx) => {
      this.codeDigits[idx] = d;
    });
    this.refreshInputValues();
    this.resetForm.patchValue({ code: this.codeDigits.join('') }, { emitEvent: true });
    setTimeout(() => this.focusFirstEmptyCodeInput(), 0);
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
    this.resetForm.patchValue({ code: this.codeDigits.join('') }, { emitEvent: true });
  }

  private clearFollowing(fromIndex: number): void {
    for (let i = fromIndex + 1; i < this.codeDigits.length; i++) {
      if (this.codeDigits[i]) {
        this.codeDigits[i] = '';
      }
    }
  }

  private refreshInputValues(): void {
    this.codeInputs?.forEach((ref, idx) => {
      const el = ref.nativeElement;
      el.value = this.codeDigits[idx] || '';
    });
  }

  async verifyCodeAsync(isInitial: boolean = false): Promise<boolean> {
    if (this.isVerifyingCode) {
      return false;
    }
    if (this.resetForm.get('email')?.invalid || this.resetForm.get('code')?.invalid) {
      this.resetForm.get('email')?.markAsTouched();
      this.resetForm.get('code')?.markAsTouched();
      if (!isInitial) {
        this.notificationService.showWarning(this.translate.instant('auth.reset.validateFirst'));
      }
      return false;
    }
    const { email, code } = this.resetForm.getRawValue();
    this.isVerifyingCode = true;
    try {
      await firstValueFrom(this.userService.verifyRecoveryCode({ email, code }));
      this.codeValidated = true;
      this.codeError = null;
      if (!isInitial) {
        this.notificationService.showSuccess(this.translate.instant('auth.reset.codeValidated'));
      }
      return true;
    } catch (error: any) {
      this.codeValidated = false;
      const detail = error?.error?.detail || error?.error?.title || this.translate.instant('auth.resetFailed');
      this.codeError = detail;
      this.notificationService.showError(detail);
      this.applyCodeValue('');
      return false;
    } finally {
      this.isVerifyingCode = false;
    }
  }

  togglePasswordVisibility(field: 'newPassword' | 'confirmPassword'): void {
    this.passwordVisibility[field] = !this.passwordVisibility[field];
  }

  async submit(): Promise<void> {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      this.notificationService.showWarning(this.translate.instant('auth.resetPasswordInvalid'));
      return;
    }
    if (!this.codeValidated && !(await this.verifyCodeAsync())) {
      return;
    }

    const { email, code, newPassword } = this.resetForm.getRawValue();
    this.isSaving = true;
    this.userService.changePasswordWithCode({ email, code, newPassword }).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('auth.resetPasswordSuccess'));
        this.passwordVisibility = { newPassword: false, confirmPassword: false };
        this.codeValidated = false;
        this.resetForm.reset();
        this.router.navigate(['/auth']);
      },
      error: (error) => {
        const detail = error?.error?.detail || error?.error?.title || this.translate.instant('auth.resetFailed');
        this.notificationService.showError(detail);
        this.isSaving = false;
      },
      complete: () => {
        this.isSaving = false;
      },
    });
  }

  goToLogin(): void {
    this.router.navigate(['/auth']);
  }

  resendCode(): void {
    if (this.isVerifyingCode || this.isSaving || this.resendCooldown > 0) {
      return;
    }
    if (this.resetForm.get('email')?.invalid) {
      this.resetForm.get('email')?.markAsTouched();
      this.notificationService.showWarning(this.translate.instant('auth.resetEmailRequired'));
      return;
    }
    const email = this.resetForm.get('email')?.value;
    this.userService.generateRecoveryCode(true, email).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('auth.resetCodeSent'));
        this.codeValidated = false;
        this.startResendCooldown();
      },
      error: (error) => {
        const detail = error?.error?.detail || this.translate.instant('auth.resetFailed');
        this.notificationService.showError(detail);
      },
    });
  }

  private startResendCooldown(seconds: number = 60): void {
    this.resendCooldown = seconds;
    this.resendSub?.unsubscribe();
    this.resendSub = interval(1000).subscribe(() => {
      this.resendCooldown = Math.max(0, this.resendCooldown - 1);
      if (this.resendCooldown === 0) {
        this.resendSub?.unsubscribe();
      }
    });
  }
}
