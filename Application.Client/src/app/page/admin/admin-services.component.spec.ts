import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { AdminServicesComponent } from './admin-services.component';
import { EmailSettingsService, EmailSettings } from '../../service/email/email-settings.service';
import { WhatsAppSettingsService, WhatsAppSettings } from '../../service/whatsapp/whatsapp-settings.service';
import { WhatsAppInstancesService, WhatsAppInstance } from '../../service/whatsapp/whatsapp-instances.service';
import { NotificationService } from '../../service/notification/notification.service';
import { AuthService } from '../../service/auth/auth.service';

class NotificationStub {
  showWarning = jasmine.createSpy('showWarning');
  showError = jasmine.createSpy('showError');
  showSuccess = jasmine.createSpy('showSuccess');
}

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

class EmailSettingsServiceStub {
  settings: EmailSettings = {
    fromName: 'Name',
    fromEmail: 'from@test.com',
    host: 'smtp.test',
    port: 25,
    secure: 'ssl',
    password: 'stored',
  };

  getSettings() {
    return of(this.settings);
  }
  updateSettings(_: EmailSettings) {
    return of(this.settings);
  }
  sendTestEmail() {
    return of({});
  }
}

class WhatsAppSettingsServiceStub {
  settings: WhatsAppSettings = {
    maxUserInstances: 1,
  };

  getSettings() {
    return of(this.settings);
  }
  updateSettings(_: WhatsAppSettings) {
    return of(this.settings);
  }
}

class WhatsAppInstancesServiceStub {
  instances: WhatsAppInstance[] = [
    {
      id: 'instances/1-A',
      displayName: 'Main',
      status: 'pending',
      isActive: true,
      createdAt: new Date().toISOString(),
      ownerUserId: 'users/1-A',
    },
  ];

  getAdminInstances(_: string | undefined = undefined) {
    return of(this.instances);
  }

  getUserInstances() {
    return of([]);
  }

  createUserInstance() {
    return of(this.instances[0]);
  }
}

describe('AdminServicesComponent', () => {
  let component: AdminServicesComponent;
  let fixture: ComponentFixture<AdminServicesComponent>;
  let emailService: EmailSettingsServiceStub;
  let whatsAppService: WhatsAppSettingsServiceStub;
  let whatsAppInstances: WhatsAppInstancesServiceStub;
  let notification: NotificationStub;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    emailService = new EmailSettingsServiceStub();
    whatsAppService = new WhatsAppSettingsServiceStub();
    whatsAppInstances = new WhatsAppInstancesServiceStub();
    notification = new NotificationStub();
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    authService.hasPermission.and.returnValue(true);

    await TestBed.configureTestingModule({
      declarations: [AdminServicesComponent],
      imports: [
        HttpClientTestingModule,
        FormsModule,
        ReactiveFormsModule,
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } }),
      ],
      providers: [
        { provide: EmailSettingsService, useValue: emailService },
        { provide: WhatsAppSettingsService, useValue: whatsAppService },
        { provide: WhatsAppInstancesService, useValue: whatsAppInstances },
        { provide: NotificationService, useValue: notification },
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminServicesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load settings on init', () => {
    expect(component.hasStoredPassword).toBeTrue();
    expect(component.emailConfig.fromEmail).toBe('from@test.com');
    expect(component.emailConfig.password).toBe('');
  });

  it('should warn when saving without host/fromEmail', () => {
    component.emailConfig.fromEmail = '';
    component.emailConfig.host = '';
    component.saveConfig();
    expect(notification.showWarning).toHaveBeenCalled();
  });

  it('should warn when testing without password', () => {
    component.emailConfig.password = '';
    component.emailConfig.testEmail = 'user@test.com';
    component.testConnection();
    expect(notification.showWarning).toHaveBeenCalled();
  });

  it('should show error when loadSettings fails', () => {
    spyOn(emailService, 'getSettings').and.returnValue(throwError(() => new Error('fail')));
    component['loadSettings']();
    expect(notification.showError).toHaveBeenCalled();
  });

  it('should load WhatsApp settings on init', () => {
    expect(component.whatsappSettingsForm.get('maxUserInstances')?.value).toBe(1);
  });

  it('should load admin instances on init', () => {
    expect(component.adminInstances.length).toBe(1);
    expect(component.adminInstances[0].displayName).toBe('Main');
  });

  it('should show error when loadWhatsAppSettings fails', () => {
    spyOn(whatsAppService, 'getSettings').and.returnValue(throwError(() => new Error('fail')));
    component['loadWhatsAppSettings']();
    expect(notification.showError).toHaveBeenCalled();
  });

  it('should show error when loadAdminInstances fails', () => {
    spyOn(whatsAppInstances, 'getAdminInstances').and.returnValue(throwError(() => new Error('fail')));
    component.loadAdminInstances();
    expect(notification.showError).toHaveBeenCalled();
  });
});
