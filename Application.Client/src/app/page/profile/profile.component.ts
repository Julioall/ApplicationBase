import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { User } from '../../model/User';
import { UserAccount } from '../../model/UserAccount';
import { UserProfile } from '../../model/UserProfile';
import { ChangePasswordPayload, UpdateProfilePayload, UserService } from '../../service/user/user.service';
import { NotificationService } from '../../service/notification/notification.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
})
export class ProfileComponent implements OnInit {
  profileForm: FormGroup;
  passwordForm: FormGroup;
  user: User | null = null;
  isLoading = false;
  isSavingProfile = false;
  isSavingPassword = false;
  avatarPreview: string | null = null;
  private readonly maxAvatarSize = 2 * 1024 * 1024; // 2MB

  private readonly passwordsMatchValidator = (group: AbstractControl): ValidationErrors | null => {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    if (!newPassword || !confirmPassword) {
      return null;
    }
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  };

  constructor(
    private readonly fb: FormBuilder,
    private readonly userService: UserService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly router: Router
  ) {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: [{ value: '', disabled: true }],
      dateOfBirth: [''],
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: [this.passwordsMatchValidator] }
    );
  }

  ngOnInit(): void {
    this.loadUser();
  }

  get userInitials(): string {
    const name: string = this.profileForm.get('name')?.value;
    if (name) {
      const parts = name.trim().split(' ').filter(Boolean);
      if (parts.length >= 2) {
        return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
      }
      return parts[0][0]?.toUpperCase() ?? 'U';
    }

    const email: string = this.profileForm.get('email')?.value;
    return email ? email.substring(0, 2).toUpperCase() : 'U';
  }

  handleAvatarChange(event: Event): void {
    const fileInput = event.target as HTMLInputElement;
    const file = fileInput?.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.notificationService.showWarning(this.translate.instant('profile.messages.avatarInvalid'));
      fileInput.value = '';
      return;
    }

    if (file.size > this.maxAvatarSize) {
      this.notificationService.showWarning(this.translate.instant('profile.messages.avatarTooLarge'));
      fileInput.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.avatarPreview = reader.result as string;
      this.profileForm.markAsDirty();
    };
    reader.readAsDataURL(file);
  }

  removeAvatar(): void {
    this.avatarPreview = null;
    this.profileForm.markAsDirty();
  }

  onProfileSubmit(): void {
    if (!this.user) {
      this.notificationService.showError(this.translate.instant('profile.messages.missingUser'));
      return;
    }

    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.notificationService.showWarning(this.translate.instant('profile.messages.formInvalid'));
      return;
    }

    const payload = this.buildProfilePayload();
    if (!payload) {
      return;
    }

    this.isSavingProfile = true;
    this.userService.updateProfile(payload).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('profile.messages.profileUpdated'));
        this.loadUser(false);
      },
      error: (error) => {
        this.handleApiError(error);
        this.isSavingProfile = false;
      },
      complete: () => {
        this.isSavingProfile = false;
      },
    });
  }

  onPasswordSubmit(): void {
    if (!this.user) {
      this.notificationService.showError(this.translate.instant('profile.messages.missingUser'));
      return;
    }

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      const messageKey = this.passwordForm.hasError('passwordMismatch')
        ? 'profile.messages.passwordMismatch'
        : 'profile.messages.passwordFormInvalid';
      this.notificationService.showWarning(this.translate.instant(messageKey));
      return;
    }

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    const payload: ChangePasswordPayload = {
      CurrentPassword: currentPassword,
      NewPassword: newPassword,
    };

    this.isSavingPassword = true;
    this.userService.changePassword(payload).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('profile.messages.passwordUpdated'));
        this.passwordForm.reset();
      },
      error: (error) => {
        this.handleApiError(error);
        this.isSavingPassword = false;
      },
      complete: () => {
        this.isSavingPassword = false;
      },
    });
  }

  resetProfileForm(): void {
    this.patchProfileForm();
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }

  loadUser(showSpinner: boolean = true): void {
    if (showSpinner) {
      this.isLoading = true;
    }

    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.user = this.ensureUserShape(user);
        this.patchProfileForm();
        this.isLoading = false;
      },
      error: (error) => {
        if (!this.user) {
          this.user = null;
        }
        this.handleApiError(error, 'profile.messages.profileLoadError');
        this.isLoading = false;
      },
    });
  }

  private ensureUserShape(user: User): User {
    const account: UserAccount = { ...(user.Account ?? {}) };
    const profile: UserProfile = { ...(user.Profile ?? {}) };
    return {
      ...user,
      Account: account,
      Profile: profile,
    };
  }

  private patchProfileForm(): void {
    if (!this.user) {
      return;
    }

    this.profileForm.patchValue(
      {
        name: this.user.Profile?.Name ?? '',
        email: this.user.Account?.Email ?? '',
        dateOfBirth: this.formatDateInput(this.user.Profile?.DateOfBirth),
      },
      { emitEvent: false }
    );
    this.profileForm.get('email')?.disable({ emitEvent: false });

    this.avatarPreview = this.user.Profile?.ProfilePictureUrl ?? null;
    this.profileForm.markAsPristine();
    this.profileForm.markAsUntouched();
  }

  private formatDateInput(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }
    const date = new Date(value);
    return isNaN(date.getTime()) ? '' : date.toISOString().split('T')[0];
  }

  private buildProfilePayload(): UpdateProfilePayload | null {
    if (!this.user) {
      return null;
    }

    const dateValue = this.profileForm.get('dateOfBirth')?.value;
    const trimmedName = (this.profileForm.get('name')?.value || '').trim();
    return {
      Name: trimmedName || this.user.Profile?.Name || '',
      DateOfBirth: dateValue ? new Date(dateValue).toISOString() : null,
      ProfilePictureUrl: this.avatarPreview ?? null,
    };
  }

  private handleApiError(error: any, fallbackKey: string = 'profile.messages.genericError'): void {
    const fallback = this.translate.instant(fallbackKey);
    const detail = error?.error?.detail || error?.error?.title || error?.message || fallback;
    this.notificationService.showError(detail);
  }
}
