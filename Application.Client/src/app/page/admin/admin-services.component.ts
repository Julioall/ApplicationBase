import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { EmailSettingsService, EmailSettings } from '../../service/email/email-settings.service';
import { WhatsAppSettingsService, WhatsAppSettings } from '../../service/whatsapp/whatsapp-settings.service';
import {
  WhatsAppInstancesService,
  WhatsAppInstance,
  CreateWhatsAppInstanceRequest,
} from '../../service/whatsapp/whatsapp-instances.service';
import { NotificationService } from '../../service/notification/notification.service';
import { AuthService } from '../../service/auth/auth.service';
import {
  MANAGE_EMAIL_PERMISSION,
  MANAGE_WHATSAPP_PERMISSION,
  MANAGE_WHATSAPP_SELF_PERMISSION,
} from '../../model/permissions';

@Component({
  selector: 'app-admin-services',
  templateUrl: './admin-services.component.html',
  styleUrls: ['./admin-services.component.scss'],
})
export class AdminServicesComponent implements OnInit, OnDestroy {
  emailConfig: EmailSettings & { testEmail?: string } = {
    fromName: 'ApplicationBase',
    fromEmail: 'no-reply@example.com',
    host: '',
    port: 587,
    secure: 'starttls',
    password: '',
    testEmail: '',
  };

  isSaving = false;
  isTesting = false;
  showPassword = false;
  isEditing = false;
  hasStoredPassword = false;
  canManageEmail = false;
  canManageSharedInstances = false;
  canManagePersonalInstances = false;
  maxUserInstances = 1;
  whatsappSettingsForm!: FormGroup;
  createWhatsAppForm!: FormGroup;
  createUserWhatsAppForm!: FormGroup;
  adminInstances: WhatsAppInstance[] = [];
  userInstances: WhatsAppInstance[] = [];
  isWhatsAppSettingsSaving = false;
  isWhatsAppInstancesLoading = false;
  isWhatsAppInstanceCreating = false;
  isWhatsAppInstanceRenaming = false;
  isWhatsAppInstanceDeactivating = false;
  isWhatsAppStatusRefreshing = false;
  isUserInstancesLoading = false;
  isUserInstanceCreating = false;
  isUserInstanceDeactivating = false;
  isUserStatusRefreshing = false;
  searchTerm = '';
  editingInstanceId: string | null = null;
  editingDisplayName = '';
  qrModalOpen = false;
  qrCode = '';
  qrInstanceLabel = '';
  private qrPollingTimer?: ReturnType<typeof setInterval>;
  private qrRefreshTimer?: ReturnType<typeof setInterval>;
  private qrInstanceId?: string;
  private qrIsAdminContext = false;
  readonly statusLabels: Record<string, string> = {
    pending: 'adminWhatsApp.status.pending',
    connected: 'adminWhatsApp.status.connected',
    disconnected: 'adminWhatsApp.status.disconnected',
    disabled: 'adminWhatsApp.status.disabled',
    error: 'adminWhatsApp.status.error',
  };
  readonly secureLabels: Record<string, string> = {
    none: 'adminEmail.secure.none',
    starttls: 'adminEmail.secure.starttls',
    ssl: 'adminEmail.secure.ssl',
  };

  constructor(
    private emailSettingsService: EmailSettingsService,
    private whatsAppSettingsService: WhatsAppSettingsService,
    private whatsAppInstancesService: WhatsAppInstancesService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.canManageEmail = this.authService.hasPermission(MANAGE_EMAIL_PERMISSION);
    this.canManageSharedInstances = this.authService.hasPermission(MANAGE_WHATSAPP_PERMISSION);
    this.canManagePersonalInstances = this.authService.hasPermission(MANAGE_WHATSAPP_SELF_PERMISSION);
    this.initWhatsAppForms();
    if (this.canManageEmail) {
      this.loadSettings();
    }
    this.loadWhatsAppSettings();
    if (this.canManagePersonalInstances) {
      this.loadUserInstances();
    }
    if (this.canManageSharedInstances) {
      this.loadAdminInstances();
    }
  }

  ngOnDestroy(): void {
    this.clearQrTimers();
  }

  private loadSettings(): void {
    this.emailSettingsService.getSettings().subscribe({
      next: (settings) => {
        this.hasStoredPassword = !!settings.password;
        this.emailConfig = { ...settings, password: '', testEmail: settings.fromEmail };
        this.isEditing = false;
        this.showPassword = false;
      },
      error: () => {
        this.notificationService.showError(this.t('adminEmail.messages.loadError'));
      },
    });
  }

  private initWhatsAppForms(): void {
    this.whatsappSettingsForm = this.fb.group({
      maxUserInstances: [1, [Validators.required, Validators.min(1)]],
    });

    this.createWhatsAppForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.maxLength(80)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
    });

    this.createUserWhatsAppForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.maxLength(80)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
    });
  }

  saveConfig(): void {
    this.isSaving = true;
    const payload = this.buildSettingsPayload();
    const validationMessage = this.validateCoreFields();
    if (validationMessage) {
      this.isSaving = false;
      this.notificationService.showWarning(validationMessage);
      return;
    }
    if (!this.hasStoredPassword && !payload.password) {
      this.isSaving = false;
      this.notificationService.showWarning(this.t('adminEmail.messages.smtpPasswordRequired'));
      return;
    }
    this.emailSettingsService.updateSettings(payload).subscribe({
      next: (saved) => {
        this.hasStoredPassword = this.hasStoredPassword || !!payload.password || !!saved.password;
        this.emailConfig = { ...saved, password: '', testEmail: this.emailConfig.testEmail };
        this.isSaving = false;
        this.notificationService.showSuccess(this.t('adminEmail.messages.saveSuccess'));
      },
      error: () => {
        this.isSaving = false;
        this.notificationService.showError(this.t('adminEmail.messages.saveError'));
      },
    });
  }

  private loadWhatsAppSettings(): void {
    this.whatsAppSettingsService.getSettings().subscribe({
      next: (settings) => {
        this.maxUserInstances = settings.maxUserInstances ?? 1;
        this.whatsappSettingsForm.patchValue(
          {
            maxUserInstances: this.maxUserInstances,
          },
          { emitEvent: false },
        );
      },
      error: () => {
        this.notificationService.showError(this.t('adminWhatsApp.messages.settingsLoadError'));
      },
    });
  }

  saveWhatsAppSettings(): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    if (this.whatsappSettingsForm.invalid) {
      this.whatsappSettingsForm.markAllAsTouched();
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.settingsInvalid'));
      return;
    }

    const payload: WhatsAppSettings = {
      maxUserInstances: Number(this.whatsappSettingsForm.get('maxUserInstances')?.value) || 1,
    };

    this.isWhatsAppSettingsSaving = true;
    this.whatsAppSettingsService.updateSettings(payload).subscribe({
      next: (saved) => {
        this.isWhatsAppSettingsSaving = false;
        this.maxUserInstances = saved.maxUserInstances ?? 1;
        this.whatsappSettingsForm.patchValue({ maxUserInstances: saved.maxUserInstances }, { emitEvent: false });
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.settingsSaved'));
      },
      error: () => {
        this.isWhatsAppSettingsSaving = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.settingsSaveError'));
      },
    });
  }

  loadAdminInstances(): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    this.isWhatsAppInstancesLoading = true;
    this.whatsAppInstancesService.getAdminInstances(this.searchTerm).subscribe({
      next: (items) => {
        this.adminInstances = items;
        this.isWhatsAppInstancesLoading = false;
      },
      error: () => {
        this.isWhatsAppInstancesLoading = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instancesLoadError'));
      },
    });
  }

  createAdminInstance(): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    if (this.createWhatsAppForm.invalid) {
      this.createWhatsAppForm.markAllAsTouched();
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }

    const payload: CreateWhatsAppInstanceRequest = {
      displayName: (this.createWhatsAppForm.get('displayName')?.value || '').trim(),
      phoneNumber: (this.createWhatsAppForm.get('phoneNumber')?.value || '').trim(),
    };

    if (!payload.displayName) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }
    if (!payload.phoneNumber) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.phoneRequired'));
      return;
    }

    this.isWhatsAppInstanceCreating = true;
    this.whatsAppInstancesService.createAdminInstance(payload).subscribe({
      next: (instance) => {
        this.isWhatsAppInstanceCreating = false;
        this.adminInstances = [instance, ...this.adminInstances];
        this.createWhatsAppForm.reset();
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceCreated'));
      },
      error: () => {
        this.isWhatsAppInstanceCreating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceCreateError'));
      },
    });
  }

  startRename(instance: WhatsAppInstance): void {
    this.editingInstanceId = instance.id;
    this.editingDisplayName = instance.displayName;
  }

  cancelRename(): void {
    this.editingInstanceId = null;
    this.editingDisplayName = '';
  }

  saveRename(instance: WhatsAppInstance): void {
    const name = (this.editingDisplayName || '').trim();
    if (!name) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }

    this.isWhatsAppInstanceRenaming = true;
    this.whatsAppInstancesService.renameAdminInstance(instance.id, { displayName: name }).subscribe({
      next: (updated) => {
        this.isWhatsAppInstanceRenaming = false;
        this.adminInstances = this.adminInstances.map((item) => (item.id === updated.id ? updated : item));
        this.cancelRename();
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceRenamed'));
      },
      error: () => {
        this.isWhatsAppInstanceRenaming = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceRenameError'));
      },
    });
  }

  refreshStatus(instance: WhatsAppInstance): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    this.isWhatsAppStatusRefreshing = true;
    this.whatsAppInstancesService.getAdminStatus(instance.id).subscribe({
      next: (status) => {
        this.isWhatsAppStatusRefreshing = false;
        this.adminInstances = this.adminInstances.map((item) =>
          item.id === instance.id ? { ...item, status: status.status } : item,
        );
      },
      error: () => {
        this.isWhatsAppStatusRefreshing = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.statusRefreshError'));
      },
    });
  }

  openQr(instance: WhatsAppInstance): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    this.whatsAppInstancesService.getAdminQr(instance.id).subscribe({
      next: (resp) => {
        this.qrCode = resp.qrCode;
        this.qrInstanceLabel = instance.displayName;
        this.qrModalOpen = true;
        this.qrInstanceId = instance.id;
        this.qrIsAdminContext = true;
        this.startQrPolling();
      },
      error: () => {
        this.notificationService.showError(this.t('adminWhatsApp.messages.qrLoadError'));
      },
    });
  }

  closeQr(): void {
    this.qrModalOpen = false;
    this.qrCode = '';
    this.qrInstanceLabel = '';
    this.qrInstanceId = undefined;
    this.clearQrTimers();
  }

  getQrCodeSrc(): string {
    if (!this.qrCode) {
      return '';
    }
    if (this.qrCode.startsWith('data:')) {
      return this.qrCode;
    }
    return `data:image/png;base64,${this.qrCode}`;
  }

  deactivateInstance(instance: WhatsAppInstance): void {
    if (!this.canManageSharedInstances) {
      return;
    }
    this.isWhatsAppInstanceDeactivating = true;
    this.whatsAppInstancesService.deactivateAdminInstance(instance.id).subscribe({
      next: (updated) => {
        this.isWhatsAppInstanceDeactivating = false;
        this.adminInstances = this.adminInstances.map((item) => (item.id === updated.id ? updated : item));
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceDeactivated'));
      },
      error: () => {
        this.isWhatsAppInstanceDeactivating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceDeactivateError'));
      },
    });
  }

  testConnection(): void {
    const to = this.emailConfig.testEmail || this.emailConfig.fromEmail;
    if (!to) {
      this.notificationService.showWarning(this.t('adminEmail.messages.testEmailRequired'));
      return;
    }
    if (!this.isValidEmail(to)) {
      this.notificationService.showWarning(this.t('adminEmail.messages.testEmailInvalid'));
      return;
    }
    const validationMessage = this.validateCoreFields();
    if (validationMessage) {
      this.notificationService.showWarning(validationMessage);
      return;
    }
    const password = (this.emailConfig.password || '').trim();
    if (!password) {
      this.notificationService.showWarning(this.t('adminEmail.messages.smtpPasswordRequiredTest'));
      return;
    }
    const payload = this.buildSettingsPayload();
    this.isTesting = true;
    this.emailSettingsService.sendTestEmail({ email: to }, payload).subscribe({
      next: () => {
        this.isTesting = false;
        this.notificationService.showSuccess(this.t('adminEmail.messages.testSuccess'));
      },
      error: () => {
        this.isTesting = false;
        this.notificationService.showError(this.t('adminEmail.messages.testError'));
      },
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  startEdit(): void {
    this.emailConfig.password = '';
    this.showPassword = false;
    this.isEditing = true;
  }

  cancelEdit(): void {
    this.loadSettings();
    this.isEditing = false;
    this.showPassword = false;
  }

  private buildSettingsPayload(): EmailSettings {
    const { testEmail, password, ...rest } = this.emailConfig;
    const payload: Partial<EmailSettings> = { ...rest };
    const trimmedPassword = (password || '').trim();
    if (trimmedPassword) {
      payload.password = trimmedPassword;
    }
    return payload as EmailSettings;
  }

  private validateCoreFields(): string | null {
    const email = (this.emailConfig.fromEmail || '').trim();
    const host = (this.emailConfig.host || '').trim();

    if (!email || !this.isValidEmail(email)) {
      return this.t('adminEmail.validation.fromEmail');
    }

    if (!host) {
      return this.t('adminEmail.validation.host');
    }

    return null;
  }

  private isValidEmail(value: string): boolean {
    return /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value);
  }

  private t(key: string): string {
    return this.translate.instant(key);
  }

  get canCreateUserInstance(): boolean {
    const activeCount = this.userInstances.filter((instance) => instance.isActive).length;
    return activeCount < this.maxUserInstances;
  }

  loadUserInstances(): void {
    if (!this.canManagePersonalInstances) {
      return;
    }
    this.isUserInstancesLoading = true;
    this.whatsAppInstancesService.getUserInstances().subscribe({
      next: (items) => {
        this.userInstances = items;
        this.isUserInstancesLoading = false;
      },
      error: () => {
        this.isUserInstancesLoading = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instancesLoadError'));
      },
    });
  }

  createUserInstance(): void {
    if (!this.canManagePersonalInstances) {
      return;
    }
    if (!this.canCreateUserInstance) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.quotaReached'));
      return;
    }
    if (this.createUserWhatsAppForm.invalid) {
      this.createUserWhatsAppForm.markAllAsTouched();
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }

    const payload: CreateWhatsAppInstanceRequest = {
      displayName: (this.createUserWhatsAppForm.get('displayName')?.value || '').trim(),
      phoneNumber: (this.createUserWhatsAppForm.get('phoneNumber')?.value || '').trim(),
    };

    if (!payload.displayName) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }
    if (!payload.phoneNumber) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.phoneRequired'));
      return;
    }

    this.isUserInstanceCreating = true;
    this.whatsAppInstancesService.createUserInstance(payload).subscribe({
      next: (instance) => {
        this.isUserInstanceCreating = false;
        this.userInstances = [instance, ...this.userInstances];
        this.createUserWhatsAppForm.reset();
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceCreated'));
      },
      error: () => {
        this.isUserInstanceCreating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceCreateError'));
      },
    });
  }

  refreshUserStatus(instance: WhatsAppInstance): void {
    if (!this.canManagePersonalInstances) {
      return;
    }
    this.isUserStatusRefreshing = true;
    this.whatsAppInstancesService.getUserStatus(instance.id).subscribe({
      next: (status) => {
        this.isUserStatusRefreshing = false;
        this.userInstances = this.userInstances.map((item) =>
          item.id === instance.id ? { ...item, status: status.status } : item,
        );
      },
      error: () => {
        this.isUserStatusRefreshing = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.statusRefreshError'));
      },
    });
  }

  openUserQr(instance: WhatsAppInstance): void {
    if (!this.canManagePersonalInstances) {
      return;
    }
    this.whatsAppInstancesService.getUserQr(instance.id).subscribe({
      next: (resp) => {
        this.qrCode = resp.qrCode;
        this.qrInstanceLabel = instance.displayName;
        this.qrModalOpen = true;
        this.qrInstanceId = instance.id;
        this.qrIsAdminContext = false;
        this.startQrPolling();
      },
      error: () => {
        this.notificationService.showError(this.t('adminWhatsApp.messages.qrLoadError'));
      },
    });
  }

  deactivateUserInstance(instance: WhatsAppInstance): void {
    if (!this.canManagePersonalInstances) {
      return;
    }
    this.isUserInstanceDeactivating = true;
    this.whatsAppInstancesService.deactivateUserInstance(instance.id).subscribe({
      next: (updated) => {
        this.isUserInstanceDeactivating = false;
        this.userInstances = this.userInstances.map((item) => (item.id === updated.id ? updated : item));
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceDeactivated'));
      },
      error: () => {
        this.isUserInstanceDeactivating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceDeactivateError'));
      },
    });
  }

  private startQrPolling(): void {
    this.clearQrTimers();

    const instanceId = this.qrInstanceId;
    if (!instanceId || !this.qrModalOpen) {
      return;
    }

    this.qrPollingTimer = setInterval(() => {
      if (!this.qrModalOpen || this.qrInstanceId !== instanceId) {
        this.clearQrTimers();
        return;
      }
      const request$ = this.qrIsAdminContext
        ? this.whatsAppInstancesService.getAdminStatus(instanceId)
        : this.whatsAppInstancesService.getUserStatus(instanceId);

      request$.subscribe({
        next: (status) => {
          const normalized = (status.status || '').toLowerCase();
          if (this.qrIsAdminContext) {
            this.adminInstances = this.adminInstances.map((item) =>
              item.id === instanceId ? { ...item, status: status.status } : item,
            );
          } else {
            this.userInstances = this.userInstances.map((item) =>
              item.id === instanceId ? { ...item, status: status.status } : item,
            );
          }

          if (normalized === 'connected') {
            this.closeQr();
          }
        },
      });
    }, 5000);

    this.qrRefreshTimer = setInterval(() => {
      if (!this.qrModalOpen || this.qrInstanceId !== instanceId) {
        this.clearQrTimers();
        return;
      }
      const refresh$ = this.qrIsAdminContext
        ? this.whatsAppInstancesService.getAdminQr(instanceId)
        : this.whatsAppInstancesService.getUserQr(instanceId);

      refresh$.subscribe({
        next: (resp) => {
          if (resp.qrCode && resp.qrCode !== this.qrCode) {
            this.qrCode = resp.qrCode;
          }
        },
      });
    }, 10000);
  }

  private clearQrTimers(): void {
    if (this.qrPollingTimer) {
      clearInterval(this.qrPollingTimer);
      this.qrPollingTimer = undefined;
    }
    if (this.qrRefreshTimer) {
      clearInterval(this.qrRefreshTimer);
      this.qrRefreshTimer = undefined;
    }
  }
}
