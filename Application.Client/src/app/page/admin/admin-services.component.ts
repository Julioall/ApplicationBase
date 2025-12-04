import { Component, OnInit } from '@angular/core';
import { EmailSettingsService, EmailSettings } from '../../service/email/email-settings.service';

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
  statusMessage = '';
  statusType: 'success' | 'error' | '' = '';
  showPassword = false;
  isEditing = false;

  constructor(private emailSettingsService: EmailSettingsService) {}

  ngOnInit(): void {
    this.loadSettings();
  }

  private setStatus(message: string, type: 'success' | 'error' | ''): void {
    this.statusMessage = message;
    this.statusType = type;
  }

  private loadSettings(): void {
    this.emailSettingsService.getSettings().subscribe({
      next: (settings) => {
        this.emailConfig = { ...settings, testEmail: settings.fromEmail };
        this.setStatus('', '');
        this.isEditing = false;
        this.showPassword = false;
      },
      error: () => {
        this.setStatus('Não foi possível carregar as configurações de e-mail.', 'error');
      },
    });
  }

  saveConfig(): void {
    this.isSaving = true;
    this.setStatus('', '');
    const { testEmail, ...payload } = this.emailConfig;
    this.emailSettingsService.updateSettings(payload as EmailSettings).subscribe({
      next: (saved) => {
        this.emailConfig = { ...saved, testEmail: this.emailConfig.testEmail };
        this.isSaving = false;
        this.setStatus('Configurações salvas com sucesso.', 'success');
      },
      error: () => {
        this.isSaving = false;
        this.setStatus('Erro ao salvar as configurações.', 'error');
      },
    });
  }

  testConnection(): void {
    const to = this.emailConfig.testEmail || this.emailConfig.fromEmail;
    if (!to) {
      this.setStatus('Informe um e-mail para teste.', 'error');
      return;
    }
    this.isTesting = true;
    this.setStatus('', '');
    this.emailSettingsService.sendTestEmail({ email: to }, this.emailConfig).subscribe({
      next: () => {
        this.isTesting = false;
        this.setStatus('E-mail de teste enviado (verifique a caixa de entrada).', 'success');
      },
      error: () => {
        this.isTesting = false;
        this.setStatus('Falha ao enviar e-mail de teste.', 'error');
      },
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  startEdit(): void {
    this.isEditing = true;
    this.setStatus('', '');
  }

  cancelEdit(): void {
    this.loadSettings();
    this.isEditing = false;
    this.showPassword = false;
  }
}
