import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { EmailSettingsService, EmailSettings } from '../../service/email/email-settings.service';
import { NotificationService } from '../../service/notification/notification.service';

@Component({
  selector: 'app-admin-services',
  templateUrl: './admin-services.component.html',
  styleUrls: ['./admin-services.component.scss'],
})
export class AdminServicesComponent implements OnInit {
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
  readonly secureLabels: Record<string, string> = {
    none: 'adminEmail.secure.none',
    starttls: 'adminEmail.secure.starttls',
    ssl: 'adminEmail.secure.ssl',
  };

  constructor(
    private emailSettingsService: EmailSettingsService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadSettings();
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
}
