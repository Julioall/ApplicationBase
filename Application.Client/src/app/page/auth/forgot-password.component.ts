import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss'],
})
export class ForgotPasswordComponent implements OnInit {
  emailForm: FormGroup;
  codeForm: FormGroup;
  isSendingCode = false;
  isCodeStep = false;
  hasSentCode = false;
  codeDigits: string[] = new Array(6).fill('');
  @ViewChildren('codeInput') codeInputs!: QueryList<ElementRef<HTMLInputElement>>;

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly router: Router,
  ) {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });

    this.codeForm = this.fb.group({
      code: ['', [Validators.required, Validators.minLength(this.codeDigits.length)]],
    });
  }

  ngOnInit(): void {
    const savedEmail = localStorage.getItem('reset-email');
    if (savedEmail) {
      this.emailForm.patchValue({ email: savedEmail });
    }
    this.codeForm.get('code')?.valueChanges.subscribe(() => {
      // keep digits in sync when form value changes externally (safety)
      const code = this.codeForm.get('code')?.value || '';
      if (!code) {
        this.resetCodeInputs();
      }
    });
  }

  sendCode(): void {
    if (this.hasSentCode) {
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
        this.isCodeStep = true;
        this.hasSentCode = true;
        this.codeForm.reset();
        this.resetCodeInputs();
        this.notificationService.showSuccess(this.translate.instant('auth.resetCodeSent'));
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

  proceedToReset(): void {
    if (this.codeForm.invalid || this.emailForm.invalid) {
      this.codeForm.markAllAsTouched();
      this.emailForm.markAllAsTouched();
      return;
    }
    const email = this.emailForm.get('email')?.value;
    const code = this.codeForm.get('code')?.value;
    this.router.navigate(['/reset-password'], { queryParams: { email, code } });
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
      this.proceedToReset();
    }
  }

  onCodePaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasted = event.clipboardData?.getData('text') ?? '';
    this.applyCodeValue(pasted);
    if (this.isCodeComplete()) {
      this.proceedToReset();
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

  goToLogin(): void {
    this.router.navigate(['/auth']);
  }
}
