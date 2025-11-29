import { TestBed } from "@angular/core/testing";
import { HttpClientTestingModule, HttpTestingController } from "@angular/common/http/testing";
import { UserService, UpdateProfilePayload } from "./user.service";
import { environment } from "../../environment/environment";

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UserService]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should attach bearer token on getCurrentUser', () => {
    localStorage.setItem('token', 'abc');

    service.getCurrentUser().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/user/me`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush({});
  });

  it('should call getUsersByPermission with permission path', () => {
    service.getUsersByPermission('view:home').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/user/permission/view:home`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should load available permissions', () => {
    service.getAvailablePermissions().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/user/permissions`);
    expect(req.request.method).toBe('GET');
    req.flush(['view:home']);
  });

  it('should send FormData for updateProfile without password', () => {
    localStorage.setItem('token', 'abc');
    const dob = '2000-01-01T00:00:00.000Z';
    const payload: UpdateProfilePayload = {
      Name: 'Tester',
      DateOfBirth: dob,
      JobTitle: 'Engineer',
      Department: 'R&D',
      Organization: 'Org',
      Location: 'City',
      ProfilePictureOffsetX: 10,
      ProfilePictureOffsetY: -5,
      ProfilePictureScale: 1.2
    };

    service.updateProfile(payload).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/user/profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    expect(req.request.body instanceof FormData).toBeTrue();
    const body = req.request.body as FormData;
    expect(body.get('Name')).toBe('Tester');
    expect(body.get('DateOfBirth')).toBe(dob);
    expect(body.get('JobTitle')).toBe('Engineer');
    expect(body.get('Department')).toBe('R&D');
    expect(body.get('Organization')).toBe('Org');
    expect(body.get('Location')).toBe('City');
    expect(body.get('ProfilePictureOffsetX')).toBe('10');
    expect(body.get('ProfilePictureOffsetY')).toBe('-5');
    expect(body.get('ProfilePictureScale')).toBe('1.2');
    req.flush({});
  });
});
