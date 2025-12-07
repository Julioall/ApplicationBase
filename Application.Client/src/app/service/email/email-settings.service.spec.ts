import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EmailSettingsService, EmailSettings } from './email-settings.service';
import { environment } from '../../environment/environment';

describe('EmailSettingsService', () => {
  let service: EmailSettingsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [EmailSettingsService],
    });
    service = TestBed.inject(EmailSettingsService);
    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => http.verify());

  it('should normalize PascalCase responses', () => {
    service.getSettings().subscribe((settings) => {
      expect(settings.fromEmail).toBe('from@test.com');
      expect(settings.port).toBe(25);
    });

    const req = http.expectOne(`${environment.apiUrl}/email/settings`);
    req.flush({
      Id: '1',
      FromEmail: 'from@test.com',
      Host: 'smtp',
      Port: 25,
      Secure: 'ssl',
      Password: 'p',
      FromName: 'Name',
    });
  });

  it('should include auth header when token present', () => {
    localStorage.setItem('token', 'abc');
    service.updateSettings({} as EmailSettings).subscribe();
    const req = http.expectOne(`${environment.apiUrl}/email/settings`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush({});
  });
});
