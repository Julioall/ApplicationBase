import { HTTP_INTERCEPTORS, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProblemInterceptor } from './problem.interceptor';
import { NotificationService } from '../notification/notification.service';

class NotificationStub {
  showError = jasmine.createSpy('showError');
}

describe('ProblemInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let notification: NotificationStub;

  beforeEach(() => {
    notification = new NotificationStub();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: NotificationService, useValue: notification },
        { provide: HTTP_INTERCEPTORS, useClass: ProblemInterceptor, multi: true },
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('should surface problem+json messages', (done) => {
    http.get('/api/problem').subscribe({
      error: (err) => {
        expect(notification.showError).toHaveBeenCalled();
        expect((err as any).title).toBe('ProblemTitle');
        done();
      },
    });

    const req = controller.expectOne('/api/problem');
    req.flush(
      { title: 'ProblemTitle', detail: 'Detail' },
      { status: 400, statusText: 'Bad Request', headers: { 'content-type': 'application/problem+json' } },
    );
  });

  it('should use fallback for generic errors', (done) => {
    http.get('/api/error').subscribe({
      error: () => {
        expect(notification.showError).toHaveBeenCalled();
        done();
      },
    });

    const req = controller.expectOne('/api/error');
    req.flush('fail', { status: 500, statusText: 'Server Error' });
  });
});
