import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { AdminServicesComponent } from './admin-services.component';
import { EmailSettingsService, EmailSettings } from '../../service/email/email-settings.service';
import { NotificationService } from '../../service/notification/notification.service';

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

describe('AdminServicesComponent', () => {
  let component: AdminServicesComponent;
  let fixture: ComponentFixture<AdminServicesComponent>;
  let emailService: EmailSettingsServiceStub;
  let notification: NotificationStub;

  beforeEach(async () => {
    emailService = new EmailSettingsServiceStub();
    notification = new NotificationStub();

    await TestBed.configureTestingModule({
      declarations: [AdminServicesComponent],
      imports: [HttpClientTestingModule, TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })],
      providers: [
        { provide: EmailSettingsService, useValue: emailService },
        { provide: NotificationService, useValue: notification },
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
});
