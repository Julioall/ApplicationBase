import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environment/environment';
import { Student } from '../../model/student';
import { StudentQuery } from '../../model/student-query';
import { PagedResult } from '../../model/paged-result';
import { StudentImportResult } from '../../model/student-import-result';

@Injectable({
  providedIn: 'root'
})
export class StudentsService {
  private readonly apiUrl = `${environment.apiUrl}/students`;

  constructor(private readonly http: HttpClient) {}

  getStudents(query?: StudentQuery): Observable<PagedResult<Student>> {
    let params = new HttpParams();
    const safeQuery = query || {};

    if (safeQuery.PageNumber && safeQuery.PageNumber > 0) {
      params = params.set('PageNumber', safeQuery.PageNumber);
    }
    if (safeQuery.PageSize && safeQuery.PageSize > 0) {
      params = params.set('PageSize', safeQuery.PageSize);
    }
    if (safeQuery.Search) {
      params = params.set('Search', safeQuery.Search);
    }
    if (safeQuery.IsActive !== undefined && safeQuery.IsActive !== null) {
      params = params.set('IsActive', safeQuery.IsActive ? 'true' : 'false');
    }

    return this.http.get<PagedResult<Student>>(this.apiUrl, { headers: this.getAuthHeaders(), params })
      .pipe(catchError(this.handleError));
  }

  getStudent(id: string): Observable<Student> {
    return this.http.get<Student>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  createStudent(student: Partial<Student>): Observable<Student> {
    return this.http.post<Student>(this.apiUrl, student, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  updateStudent(id: string, student: Partial<Student>): Observable<Student> {
    return this.http.put<Student>(`${this.apiUrl}/${id}`, student, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  deleteStudent(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  importStudents(file: File): Observable<StudentImportResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<StudentImportResult>(`${this.apiUrl}/import`, formData, {
      headers: this.getAuthHeaders(false)
    }).pipe(catchError(this.handleError));
  }

  exportStudents(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export`, {
      headers: this.getAuthHeaders(false),
      responseType: 'blob'
    }).pipe(catchError(this.handleError));
  }

  private getAuthHeaders(includeJson = true): HttpHeaders {
    const token = localStorage.getItem('token');
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    if (includeJson) {
      headers = headers.set('Content-Type', 'application/json');
    }
    return headers;
  }

  private handleError(error: unknown): Observable<never> {
    console.error('StudentsService error:', error);
    return throwError(() => error);
  }
}
