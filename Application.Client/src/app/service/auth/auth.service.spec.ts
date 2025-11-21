import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../environment/environment';
import { User } from '../../model/User';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should POST signup with Account/Profile payload', () => {
    const user: User = {
      Account: {
        Email: 'new@test.com',
        Password: 'Password123!',
        Role: 'User',
        DateJoined: new Date(),
      },
      Profile: {
        Name: 'Tester',
      },
    };

    service.signup(user).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/User/add`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.Account.Email).toBe('new@test.com');
    expect(req.request.body.Account.Password).toBe('Password123!');
    expect(req.request.body.Profile.Name).toBe('Tester');
    req.flush({});
  });

  it('should store token and refreshToken on login', () => {
    spyOn(localStorage, 'setItem').and.callThrough();

    service.login('user@test.com', 'pass').subscribe((res) => {
      expect(res.token).toBe('jwt-token');
      expect(res.refreshToken).toBe('refresh-token');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/Authentication/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.Email).toBe('user@test.com');
    expect(req.request.body.Password).toBe('pass');

    req.flush({ token: 'jwt-token', refreshToken: 'refresh-token' });

    expect(localStorage.setItem).toHaveBeenCalledWith('token', 'jwt-token');
    expect(localStorage.setItem).toHaveBeenCalledWith('refreshToken', 'refresh-token');
  });

  it('should propagate login error and not store token', () => {
    spyOn(localStorage, 'setItem').and.callThrough();

    service.login('user@test.com', 'wrong').subscribe({
      next: () => fail('expected error'),
      error: (err) => expect(err.message).toContain('Login failed'),
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/Authentication/login`);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.setItem).not.toHaveBeenCalledWith('token', jasmine.any(String));
  });
});
