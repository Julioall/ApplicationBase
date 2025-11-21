import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { User } from '../../model/User';
import { UserAccount } from '../../model/UserAccount';
import { UserProfile } from '../../model/UserProfile';
import { ChangePasswordPayload, UpdateProfilePayload, UserService } from '../../service/user/user.service';
import { NotificationService } from '../../service/notification/notification.service';
import { ThemeService } from '../../service/theme/theme.service';

type HydratedUser = User & { Account: UserAccount; Profile: UserProfile };

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
})
export class ProfileComponent implements OnInit {
  profileForm: FormGroup;
  passwordForm: FormGroup;
  user: HydratedUser | null = null;
  isLoading = false;
  isSavingProfile = false;
  isSavingPassword = false;
  isSavingAvatar = false;
  isEditingProfile = false;
  isEditingPassword = false;
  avatarPreview: string | null = null;
  private selectedAvatarFile: File | null = null;
  avatarDraftPreview: string | null = null;
  private avatarDraftFile: File | null = null;
  isAvatarModalOpen = false;
  avatarZoom = 1;
  draftZoom = 1;
  avatarOffsetX = 0;
  avatarOffsetY = 0;
  draftOffsetX = 0;
  draftOffsetY = 0;
  @ViewChild('cropArea') private cropArea?: ElementRef<HTMLDivElement>;
  @ViewChild('avatarInput') private avatarInput?: ElementRef<HTMLInputElement>;
  isDraggingAvatar = false;
  avatarMenuOpen = false;
  languages = [
    { value: 'en', label: 'profile.account.languages.en' },
    { value: 'pt', label: 'profile.account.languages.pt' },
  ];
  themes: Array<{ value: 'light' | 'dark'; label: string }> = [
    { value: 'light', label: 'profile.account.themes.light' },
    { value: 'dark', label: 'profile.account.themes.dark' },
  ];
  selectedLanguage = 'en';
  selectedTheme: 'light' | 'dark' = 'light';
  private dragStartX = 0;
  private dragStartY = 0;
  private initialOffsetX = 0;
  private initialOffsetY = 0;
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
    private readonly router: Router,
    private readonly themeService: ThemeService,
  ) {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: [{ value: '', disabled: true }],
      dateOfBirth: [''],
      jobTitle: [''],
      department: [''],
      organization: [''],
      location: [''],
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: [this.passwordsMatchValidator] }
    );

    this.setProfileControlsState(false);
  }

  ngOnInit(): void {
    this.initializePreferences();
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
      this.avatarDraftPreview = reader.result as string;
      this.avatarDraftFile = file;
      this.draftOffsetX = 0;
      this.draftOffsetY = 0;
      this.draftZoom = 1;
      this.isAvatarModalOpen = true;
      this.avatarMenuOpen = false;
    };
    reader.readAsDataURL(file);
    fileInput.value = '';
  }

  removeAvatar(): void {
    if (!this.user) {
      this.notificationService.showError(this.translate.instant('profile.messages.missingUser'));
      return;
    }

    this.avatarPreview = null;
    this.selectedAvatarFile = null;
    this.avatarDraftPreview = null;
    this.avatarDraftFile = null;
    this.avatarOffsetX = 0;
    this.avatarOffsetY = 0;
    this.avatarZoom = 1;
    this.avatarMenuOpen = false;
    this.isAvatarModalOpen = false;
    this.submitAvatarUpdate(true);
  }

  get avatarObjectPosition(): string {
    const x = 50 + this.clampOffset(this.avatarOffsetX);
    const y = 50 + this.clampOffset(this.avatarOffsetY);
    return `${x}% ${y}%`;
  }

  get avatarDraftObjectPosition(): string {
    const x = 50 + this.clampOffset(this.draftOffsetX);
    const y = 50 + this.clampOffset(this.draftOffsetY);
    return `${x}% ${y}%`;
  }

  startAvatarDrag(event: PointerEvent): void {
    if (!this.isAvatarModalOpen || !this.avatarDraftPreview || !this.cropArea) {
      return;
    }

    event.preventDefault();
    this.isDraggingAvatar = true;
    this.dragStartX = event.clientX;
    this.dragStartY = event.clientY;
    this.initialOffsetX = this.draftOffsetX;
    this.initialOffsetY = this.draftOffsetY;
  }

  onAvatarDrag(event: PointerEvent): void {
    if (!this.isDraggingAvatar || !this.cropArea || !this.isAvatarModalOpen) {
      return;
    }

    event.preventDefault();
    const rect = this.cropArea.nativeElement.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) {
      return;
    }

    const deltaX = ((event.clientX - this.dragStartX) / rect.width) * 100;
    const deltaY = ((event.clientY - this.dragStartY) / rect.height) * 100;

    this.draftOffsetX = this.clampOffset(this.initialOffsetX + deltaX);
    this.draftOffsetY = this.clampOffset(this.initialOffsetY - deltaY);
  }

  endAvatarDrag(event?: PointerEvent): void {
    if (this.isDraggingAvatar) {
      this.isDraggingAvatar = false;
    }
  }

  onProfileSubmit(): void {
    if (!this.isEditingProfile) {
      return;
    }
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
        this.isEditingProfile = false;
        this.setProfileControlsState(false);
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
    if (!this.isEditingPassword) {
      return;
    }
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
        this.isEditingPassword = false;
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

  startProfileEdit(): void {
    if (!this.user) {
      return;
    }
    this.isEditingProfile = true;
    this.setProfileControlsState(true);
  }

  cancelProfileEdit(): void {
    this.isEditingProfile = false;
    this.patchProfileForm();
    this.setProfileControlsState(false);
  }

  startPasswordEdit(): void {
    this.isEditingPassword = true;
    this.passwordForm.reset();
  }

  cancelPasswordEdit(): void {
    this.isEditingPassword = false;
    this.passwordForm.reset();
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
        this.setProfileControlsState(false);
        this.isEditingProfile = false;
        this.isEditingPassword = false;
        this.isLoading = false;
      },
      error: (error) => {
        if (!this.user) {
          this.user = null;
        }
        this.handleApiError(error, 'profile.messages.profileLoadError');
        this.isEditingProfile = false;
        this.isEditingPassword = false;
        this.setProfileControlsState(false);
        this.isLoading = false;
      },
    });
  }

  private ensureUserShape(user: User): HydratedUser {
    const account: UserAccount = { ...(user.Account ?? {}) };
    const profile: UserProfile = { ...(user.Profile ?? {}) };
    return {
      ...user,
      Account: account,
      Profile: profile,
    } as HydratedUser;
  }

  private patchProfileForm(): void {
    if (!this.user) {
      return;
    }

    this.profileForm.patchValue(
      {
        name: this.user.Profile?.Name ?? '',
        email: this.user.Account?.Email ?? '',
        dateOfBirth: this.safeDate(this.user.Profile?.DateOfBirth),
        jobTitle: this.user.Profile?.JobTitle ?? '',
        department: this.user.Profile?.Department ?? '',
        organization: this.user.Profile?.Organization ?? '',
        location: this.user.Profile?.Location ?? '',
      },
      { emitEvent: false }
    );
    this.profileForm.get('email')?.disable({ emitEvent: false });

    this.avatarPreview = this.user.Profile?.ProfilePictureUrl ?? null;
    this.selectedAvatarFile = null;
    this.avatarZoom = this.user.Profile?.ProfilePictureScale ?? 1;
    this.applyServerOrCachedOffsets();
    this.profileForm.markAsPristine();
    this.profileForm.markAsUntouched();
    this.setProfileControlsState(this.isEditingProfile);
  }

  safeDate(value: string | Date | null | undefined): string {
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
    const jobTitle = (this.profileForm.get('jobTitle')?.value || '').trim();
    const department = (this.profileForm.get('department')?.value || '').trim();
    const organization = (this.profileForm.get('organization')?.value || '').trim();
    const location = (this.profileForm.get('location')?.value || '').trim();
    return {
      Name: trimmedName || this.user.Profile?.Name || '',
      DateOfBirth: dateValue ? new Date(dateValue).toISOString() : null,
      JobTitle: jobTitle || null,
      Department: department || null,
      Organization: organization || null,
      Location: location || null,
    };
  }

  onLanguageChange(value: string): void {
    this.selectedLanguage = value;
    this.translate.use(value);
    this.setLocalStorageItem('preferredLanguage', value);
  }

  onThemeChange(value: string): void {
    const theme = value === 'dark' ? 'dark' : 'light';
    this.selectedTheme = theme;
    this.themeService.setTheme(theme);
  }

  private initializePreferences(): void {
    const storedLanguage = this.getLocalStorageItem('preferredLanguage');
    if (storedLanguage) {
      this.selectedLanguage = storedLanguage;
      this.translate.use(storedLanguage);
    } else if (this.translate.currentLang) {
      this.selectedLanguage = this.translate.currentLang;
    }

    this.selectedTheme = this.themeService.getActiveTheme();
    this.themeService.setTheme(this.selectedTheme);
  }

  private getLocalStorageItem(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private setLocalStorageItem(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // ignore storage issues
    }
  }

  private handleApiError(error: any, fallbackKey: string = 'profile.messages.genericError'): void {
    const fallback = this.translate.instant(fallbackKey);
    const detail = error?.error?.detail || error?.error?.title || error?.message || fallback;
    this.notificationService.showError(detail);
  }

  private clampOffset(value: number): number {
    return Math.max(-50, Math.min(50, value));
  }

  private applyServerOrCachedOffsets(): void {
    const serverX = Number(this.user?.Profile?.ProfilePictureOffsetX ?? 0);
    const serverY = Number(this.user?.Profile?.ProfilePictureOffsetY ?? 0);
    this.avatarOffsetX = serverX;
    this.avatarOffsetY = serverY;

    if (this.hasNonZeroOffsets(serverX, serverY, this.avatarZoom)) {
      this.saveOffsetsToCache();
      return;
    }

    this.restoreOffsetsFromCache();
  }

  private hasNonZeroOffsets(x: number, y: number, scale: number = 1): boolean {
    const tolerance = 0.001;
    return Math.abs(x) > tolerance || Math.abs(y) > tolerance || Math.abs(scale - 1) > tolerance;
  }

  private saveOffsetsToCache(): void {
    const key = this.getOffsetCacheKey();
    if (!key) {
      return;
    }

    try {
      localStorage.setItem(key, JSON.stringify({ x: this.avatarOffsetX, y: this.avatarOffsetY, z: this.avatarZoom }));
    } catch {
      // ignore storage failures
    }
  }

  private submitAvatarUpdate(isRemoval: boolean): void {
    if (!this.user) {
      this.notificationService.showError(this.translate.instant('profile.messages.missingUser'));
      return;
    }

    const payload = this.buildProfilePayload();
    if (!payload) {
      return;
    }

    if (isRemoval) {
      payload.RemoveProfilePicture = true;
    } else {
      if (this.selectedAvatarFile) {
        payload.ProfilePicture = this.selectedAvatarFile;
      }
      payload.ProfilePictureOffsetX = this.avatarOffsetX;
      payload.ProfilePictureOffsetY = this.avatarOffsetY;
      payload.ProfilePictureScale = this.avatarZoom;
    }

    this.isSavingAvatar = true;
    this.userService.updateProfile(payload).subscribe({
      next: () => {
        if (isRemoval) {
          this.clearOffsetCache();
        } else {
          this.saveOffsetsToCache();
        }
        this.selectedAvatarFile = null;
        this.notificationService.showSuccess(this.translate.instant('profile.messages.profileUpdated'));
      },
      error: (error) => {
        this.handleApiError(error);
        this.isSavingAvatar = false;
      },
      complete: () => {
        this.isSavingAvatar = false;
      },
    });
  }

  private restoreOffsetsFromCache(): void {
    const key = this.getOffsetCacheKey();
    if (!key) {
      return;
    }

    try {
      const cached = localStorage.getItem(key);
      if (!cached) {
        return;
      }

      const parsed = JSON.parse(cached);
      if (typeof parsed?.x === 'number') {
        this.avatarOffsetX = parsed.x;
      }

      if (typeof parsed?.y === 'number') {
        this.avatarOffsetY = parsed.y;
      }

      if (typeof parsed?.z === 'number') {
        this.avatarZoom = parsed.z;
      }
    } catch {
      // ignore invalid cache
    }
  }

  private clearOffsetCache(): void {
    const key = this.getOffsetCacheKey();
    if (!key) {
      return;
    }

    try {
      localStorage.removeItem(key);
    } catch {
      // ignore
    }
  }

  private getOffsetCacheKey(): string | null {
    return this.user?.Id ? `profile-avatar-offset:${this.user.Id}` : null;
  }

  private setProfileControlsState(enabled: boolean): void {
    const method = enabled ? 'enable' : 'disable';
    ['name', 'dateOfBirth', 'jobTitle', 'department', 'organization', 'location'].forEach((controlName) =>
    {
      this.profileForm.get(controlName)?.[method]({ emitEvent: false });
    });
    this.profileForm.get('email')?.disable({ emitEvent: false });
  }

  triggerAvatarSelection(): void {
    this.avatarInput?.nativeElement.click();
  }

  toggleAvatarMenu(event: Event): void {
    event.stopPropagation();
    this.avatarMenuOpen = !this.avatarMenuOpen;
  }

  @HostListener('document:click')
  handleDocumentClick(): void {
    this.avatarMenuOpen = false;
  }

  openAvatarModal(): void {
    this.avatarMenuOpen = false;
    this.isAvatarModalOpen = true;
    if (this.avatarPreview) {
      this.avatarDraftPreview = this.avatarPreview;
      this.draftOffsetX = this.avatarOffsetX;
      this.draftOffsetY = this.avatarOffsetY;
      this.draftZoom = this.avatarZoom;
      this.avatarDraftFile = this.selectedAvatarFile;
    } else {
      this.avatarDraftPreview = null;
      this.avatarDraftFile = null;
      this.draftOffsetX = 0;
      this.draftOffsetY = 0;
      this.draftZoom = 1;
    }
  }

  closeAvatarModal(): void {
    this.isAvatarModalOpen = false;
    this.avatarDraftPreview = null;
    this.avatarDraftFile = null;
  }

  clearAvatarDraft(event?: Event): void {
    event?.stopPropagation();
    this.avatarDraftPreview = null;
    this.avatarDraftFile = null;
    this.draftOffsetX = 0;
    this.draftOffsetY = 0;
    this.draftZoom = 1;
  }

  saveAvatarChanges(): void {
    if (this.avatarDraftPreview) {
      this.avatarPreview = this.avatarDraftPreview;
      if (this.avatarDraftFile) {
        this.selectedAvatarFile = this.avatarDraftFile;
      }
      this.avatarOffsetX = this.draftOffsetX;
      this.avatarOffsetY = this.draftOffsetY;
      this.avatarZoom = this.draftZoom;
    }

    this.closeAvatarModal();
    this.submitAvatarUpdate(false);
  }
}
