import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('Service: Authentication', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService]
    });
  });

  it('should ...', inject([AuthService], (service: AuthService) => {
    expect(service).toBeTruthy();
  }));
});
function inject(arg0: (typeof AuthService)[], arg1: (service: AuthService) => void): jasmine.ImplementationCallback | undefined {
  throw new Error('Function not implemented.');
}

