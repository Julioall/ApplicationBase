import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoadingInterceptor } from './loading.interceptor';
import { LoadingService } from './loading.service';

class LoadingServiceStub {
  startLoading = jasmine.createSpy('startLoading');
  stopLoading = jasmine.createSpy('stopLoading');
}

describe('LoadingInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let loading: LoadingServiceStub;

  beforeEach(() => {
    loading = new LoadingServiceStub();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: LoadingService, useValue: loading },
        { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true },
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('should start and stop loading around requests', () => {
    http.get('/api/data').subscribe();
    const req = controller.expectOne('/api/data');
    expect(loading.startLoading).toHaveBeenCalled();
    req.flush({});
    expect(loading.stopLoading).toHaveBeenCalled();
  });
});
