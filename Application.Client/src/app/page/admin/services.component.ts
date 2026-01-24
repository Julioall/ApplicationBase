import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
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
  selector: 'app-services',
  templateUrl: './services.component.html',
  styleUrls: ['./services.component.scss'],
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
  adminInstances: WhatsAppInstance[] = [];
  userInstances: WhatsAppInstance[] = [];
  isWhatsAppSettingsSaving = false;
  isWhatsAppInstancesLoading = false;
  isWhatsAppInstanceCreating = false;
  isWhatsAppInstanceRenaming = false;
  isWhatsAppInstanceDeactivating = false;
  isWhatsAppEditing = false;
  isWhatsAppInstanceDisconnecting = false;
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
  private qrStop$?: Subject<void>;
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
  readonly sharedKey = 'adminWhatsApp.sharedLabel';

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
    this.resetCreateForm();
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
    this.stopQrSubscriptions();
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
      isShared: [false],
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
    if (!this.isWhatsAppEditing) {
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

  createInstance(): void {
    if (!this.canManagePersonalInstances && !this.canManageSharedInstances) {
      return;
    }
    if (!this.isWhatsAppEditing) {
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
    const isShared = this.resolveSharedSelection();

    if (!payload.displayName) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }
    if (!payload.phoneNumber) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.phoneRequired'));
      return;
    }

    if (isShared) {
      if (!this.canManageSharedInstances) {
        return;
      }
      this.isWhatsAppInstanceCreating = true;
      this.whatsAppInstancesService.createAdminInstance(payload).subscribe({
        next: (instance) => {
          this.isWhatsAppInstanceCreating = false;
          this.adminInstances = [instance, ...this.adminInstances];
          this.resetCreateForm(isShared);
          this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceCreated'));
        },
        error: () => {
          this.isWhatsAppInstanceCreating = false;
          this.notificationService.showError(this.t('adminWhatsApp.messages.instanceCreateError'));
        },
      });
      return;
    }

    if (!this.canCreateUserInstance) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.quotaReached'));
      return;
    }

    this.isUserInstanceCreating = true;
    this.whatsAppInstancesService.createUserInstance(payload).subscribe({
      next: (instance) => {
        this.isUserInstanceCreating = false;
        this.userInstances = [instance, ...this.userInstances];
        this.resetCreateForm(false);
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceCreated'));
      },
      error: () => {
        this.isUserInstanceCreating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceCreateError'));
      },
    });
  }

  startRename(instance: WhatsAppInstance & { isShared: boolean }): void {
    this.editingInstanceId = instance.id;
    this.editingDisplayName = instance.displayName;
  }

  cancelRename(): void {
    this.editingInstanceId = null;
    this.editingDisplayName = '';
  }

  saveRename(instance: WhatsAppInstance & { isShared: boolean }): void {
    const name = (this.editingDisplayName || '').trim();
    if (!name) {
      this.notificationService.showWarning(this.t('adminWhatsApp.messages.instanceNameRequired'));
      return;
    }

    this.isWhatsAppInstanceRenaming = true;
    const request$ = instance.isShared
      ? this.whatsAppInstancesService.renameAdminInstance(instance.id, { displayName: name })
      : this.whatsAppInstancesService.renameUserInstance(instance.id, { displayName: name });

    request$.subscribe({
      next: (updated) => {
        this.isWhatsAppInstanceRenaming = false;
        if (instance.isShared) {
          this.adminInstances = this.adminInstances.map((item) => (item.id === updated.id ? updated : item));
        } else {
          this.userInstances = this.userInstances.map((item) => (item.id === updated.id ? updated : item));
        }
        this.cancelRename();
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceRenamed'));
      },
      error: () => {
        this.isWhatsAppInstanceRenaming = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceRenameError'));
      },
    });
  }

  refreshStatus(instance: WhatsAppInstance & { isShared: boolean }): void {
    this.isWhatsAppStatusRefreshing = true;
    const request$ = instance.isShared
      ? this.whatsAppInstancesService.getAdminStatus(instance.id)
      : this.whatsAppInstancesService.getUserStatus(instance.id);
    request$.subscribe({
      next: (status) => {
        this.isWhatsAppStatusRefreshing = false;
        if (instance.isShared) {
          this.adminInstances = this.adminInstances.map((item) =>
            item.id === instance.id ? { ...item, status: status.status } : item,
          );
        } else {
          this.userInstances = this.userInstances.map((item) =>
            item.id === instance.id ? { ...item, status: status.status } : item,
          );
        }
      },
      error: () => {
        this.isWhatsAppStatusRefreshing = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.statusRefreshError'));
      },
    });
  }

  openQr(instance: WhatsAppInstance & { isShared: boolean }): void {
    this.qrCode = '';
    this.qrInstanceLabel = instance.displayName;
    this.qrModalOpen = true;
    this.qrInstanceId = instance.id;
    this.qrIsAdminContext = instance.isShared;
    this.fetchQrCode(instance.id, instance.isShared);
    this.startQrPolling();
  }

  closeQr(): void {
    this.qrModalOpen = false;
    this.qrCode = '';
    this.qrInstanceLabel = '';
    this.qrInstanceId = undefined;
    this.stopQrSubscriptions();
    this.clearQrTimers();
  }

  stopQrRequest(): void {
    this.closeQr();
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

  disconnectInstance(instance: WhatsAppInstance & { isShared: boolean }): void {
    if ((instance.status || '').toLowerCase() !== 'connected') {
      return;
    }

    this.isWhatsAppInstanceDisconnecting = true;
    const request$ = instance.isShared
      ? this.whatsAppInstancesService.disconnectAdminInstance(instance.id)
      : this.whatsAppInstancesService.disconnectUserInstance(instance.id);

    request$.subscribe({
      next: (updated) => {
        this.isWhatsAppInstanceDisconnecting = false;
        if (instance.isShared) {
          this.adminInstances = this.adminInstances.map((item) => (item.id === updated.id ? updated : item));
        } else {
          this.userInstances = this.userInstances.map((item) => (item.id === updated.id ? updated : item));
        }
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceDisconnected'));
      },
      error: () => {
        this.isWhatsAppInstanceDisconnecting = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceDisconnectError'));
      },
    });
  }

  deleteInstance(instance: WhatsAppInstance & { isShared: boolean }): void {
    const status = (instance.status || '').toLowerCase();
    if (status !== 'disconnected' && status !== 'pending') {
      return;
    }

    if (instance.isShared) {
      this.isWhatsAppInstanceDeactivating = true;
      this.whatsAppInstancesService.deactivateAdminInstance(instance.id).subscribe({
        next: (updated) => {
          this.isWhatsAppInstanceDeactivating = false;
          this.adminInstances = this.adminInstances.filter((item) => item.id !== updated.id);
          this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceDeleted'));
        },
        error: () => {
          this.isWhatsAppInstanceDeactivating = false;
          this.notificationService.showError(this.t('adminWhatsApp.messages.instanceDeleteError'));
        },
      });
      return;
    }

    this.isUserInstanceDeactivating = true;
    this.whatsAppInstancesService.deactivateUserInstance(instance.id).subscribe({
      next: (updated) => {
        this.isUserInstanceDeactivating = false;
        this.userInstances = this.userInstances.filter((item) => item.id !== updated.id);
        this.notificationService.showSuccess(this.t('adminWhatsApp.messages.instanceDeleted'));
      },
      error: () => {
        this.isUserInstanceDeactivating = false;
        this.notificationService.showError(this.t('adminWhatsApp.messages.instanceDeleteError'));
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

  startWhatsAppEdit(): void {
    if (!this.canManagePersonalInstances && !this.canManageSharedInstances) {
      return;
    }
    this.isWhatsAppEditing = true;
  }

  cancelWhatsAppEdit(): void {
    this.isWhatsAppEditing = false;
    if (this.canManageSharedInstances) {
      this.loadWhatsAppSettings();
    }
    this.resetCreateForm();
  }

  finishWhatsAppEdit(): void {
    this.isWhatsAppEditing = false;
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

  get canCreateInstance(): boolean {
    if (this.resolveSharedSelection()) {
      return this.canManageSharedInstances;
    }
    return this.canManagePersonalInstances && this.canCreateUserInstance;
  }

  get isSharedSelection(): boolean {
    return this.resolveSharedSelection();
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

  get combinedInstances(): Array<WhatsAppInstance & { isShared: boolean }> {
    const personal = this.userInstances.map((instance) => ({ ...instance, isShared: false }));
    const shared = this.adminInstances.map((instance) => ({ ...instance, isShared: true }));
    return [...personal, ...shared].sort(
      (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
    );
  }

  private resolveSharedSelection(): boolean {
    if (!this.canManageSharedInstances) {
      return false;
    }
    if (!this.canManagePersonalInstances) {
      return true;
    }
    return !!this.createWhatsAppForm.get('isShared')?.value;
  }

  private resetCreateForm(isShared?: boolean): void {
    const sharedValue =
      typeof isShared === 'boolean'
        ? isShared
        : this.canManageSharedInstances && !this.canManagePersonalInstances;
    this.createWhatsAppForm.reset({
      displayName: '',
      phoneNumber: '',
      isShared: sharedValue,
    });
  }

  private startQrPolling(): void {
    this.clearQrTimers();
    this.stopQrSubscriptions();
    this.qrStop$ = new Subject<void>();

    const instanceId = this.qrInstanceId;
    if (!instanceId || !this.qrModalOpen) {
      return;
    }

    this.fetchQrCode(instanceId, this.qrIsAdminContext);

    this.qrPollingTimer = setInterval(() => {
      if (!this.qrModalOpen || this.qrInstanceId !== instanceId) {
        this.clearQrTimers();
        return;
      }
      if (this.shouldStopQrRequests(instanceId, this.qrIsAdminContext)) {
        return;
      }
      const request$ = this.qrIsAdminContext
        ? this.whatsAppInstancesService.getAdminStatus(instanceId)
        : this.whatsAppInstancesService.getUserStatus(instanceId);

      request$.pipe(takeUntil(this.qrStop$!)).subscribe({
        next: (status) => {
          if (this.shouldStopQrRequests(instanceId, this.qrIsAdminContext)) {
            return;
          }
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
      if (this.shouldStopQrRequests(instanceId, this.qrIsAdminContext)) {
        return;
      }
      this.fetchQrCode(instanceId, this.qrIsAdminContext);
    }, 10000);
  }

  private fetchQrCode(instanceId: string, isAdminContext: boolean): void {
    if (!this.qrStop$) {
      this.qrStop$ = new Subject<void>();
    }
    if (this.shouldStopQrRequests(instanceId, isAdminContext)) {
      return;
    }
    const refresh$ = isAdminContext
      ? this.whatsAppInstancesService.getAdminQr(instanceId)
      : this.whatsAppInstancesService.getUserQr(instanceId);

    refresh$.pipe(takeUntil(this.qrStop$)).subscribe({
      next: (resp) => {
        if (this.shouldStopQrRequests(instanceId, isAdminContext)) {
          return;
        }
        if (resp.qrCode && resp.qrCode !== this.qrCode) {
          this.qrCode = resp.qrCode;
        }
      },
      error: () => {
        this.notificationService.showError(this.t('adminWhatsApp.messages.qrLoadError'));
      },
    });
  }

  private shouldStopQrRequests(instanceId: string, isAdminContext: boolean): boolean {
    return !this.qrModalOpen || this.qrInstanceId !== instanceId || this.qrIsAdminContext !== isAdminContext;
  }

  private stopQrSubscriptions(): void {
    if (this.qrStop$) {
      this.qrStop$.next();
      this.qrStop$.complete();
      this.qrStop$ = undefined;
    }
  }

  private getQrInstance(): WhatsAppInstance | undefined {
    if (!this.qrInstanceId) {
      return undefined;
    }
    const list = this.qrIsAdminContext ? this.adminInstances : this.userInstances;
    return list.find((item) => item.id === this.qrInstanceId);
  }

  get isQrInstanceConnected(): boolean {
    const instance = this.getQrInstance();
    return (instance?.status || '').toLowerCase() === 'connected';
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
