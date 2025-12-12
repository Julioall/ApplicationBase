import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { StudentsService } from './students.service';
import { environment } from '../../environment/environment';
import { Student } from '../../model/student';

describe('StudentsService', () => {
  let service: StudentsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [StudentsService]
    });

    service = TestBed.inject(StudentsService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should fetch students with query params and auth header', () => {
    localStorage.setItem('token', 'abc');
    service.getStudents({ PageNumber: 2, PageSize: 5, Search: 'john', IsActive: true }).subscribe();

    const req = httpMock.expectOne(r => r.url === `${environment.apiUrl}/students`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    expect(req.request.params.get('PageNumber')).toBe('2');
    expect(req.request.params.get('PageSize')).toBe('5');
    expect(req.request.params.get('Search')).toBe('john');
    expect(req.request.params.get('IsActive')).toBe('true');
    req.flush({ Items: [], Total: 0, PageNumber: 2, PageSize: 5 });
  });

  it('should create student', () => {
    const payload: Partial<Student> = { FirstName: 'Ana', LastName: 'Silva', IsActive: true };
    service.createStudent(payload).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/students`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({});
  });

  it('should update student by id', () => {
    const payload: Partial<Student> = { FirstName: 'Ana', LastName: 'Silva', IsActive: true };
    service.updateStudent('students-1', payload).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/students/students-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({});
  });

  it('should delete student', () => {
    service.deleteStudent('students-2').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/students/students-2`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  it('should import students with form data', () => {
    const file = new File(['content'], 'students.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    service.importStudents(file).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/students/import`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('Content-Type')).toBeNull();
    expect(req.request.body instanceof FormData).toBeTrue();
    req.flush({ Processed: 1, Created: 1, Updated: 0, Skipped: 0, Errors: [] });
  });

  it('should export students as blob', () => {
    service.exportStudents().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/students/export`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob());
  });
});
