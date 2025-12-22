import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environment/environment';
import { EducationSchool } from '../../model/education-school';
import { EducationProgram } from '../../model/education-program';
import { EducationClass } from '../../model/education-class';
import { EducationUc } from '../../model/education-uc';
import { PagedResult } from '../../model/paged-result';
import { UcSearchQuery } from '../../model/uc-search-query';
import { CourseImportResult } from '../../model/course-import-result';

@Injectable({
  providedIn: 'root'
})
export class EducationService {
  private readonly apiUrl = `${environment.apiUrl}/education`;

  constructor(private readonly http: HttpClient) {}

  getSchools(): Observable<EducationSchool[]> {
    return this.http.get<EducationSchool[]>(`${this.apiUrl}/schools`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getPrograms(schoolId: string): Observable<EducationProgram[]> {
    const params = new HttpParams().set('schoolId', schoolId);
    return this.http.get<EducationProgram[]>(`${this.apiUrl}/programs`, { headers: this.getAuthHeaders(), params })
      .pipe(catchError(this.handleError));
  }

  getClasses(programId: string): Observable<EducationClass[]> {
    const params = new HttpParams().set('programId', programId);
    return this.http.get<EducationClass[]>(`${this.apiUrl}/classes`, { headers: this.getAuthHeaders(), params })
      .pipe(catchError(this.handleError));
  }

  searchUcs(query: UcSearchQuery, classId?: string, programId?: string): Observable<PagedResult<EducationUc>> {
    let params = new HttpParams();

    if (query.PageNumber) {
      params = params.set('PageNumber', query.PageNumber);
    }
    if (query.PageSize) {
      params = params.set('PageSize', query.PageSize);
    }
    if (query.Search) {
      params = params.set('Search', query.Search);
    }
    if (classId) {
      params = params.set('classId', classId);
    }
    if (programId) {
      params = params.set('programId', programId);
    }

    return this.http.get<PagedResult<EducationUc>>(`${this.apiUrl}/ucs/search`, { headers: this.getAuthHeaders(), params })
      .pipe(catchError(this.handleError));
  }

  importCourses(file: File): Observable<CourseImportResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<CourseImportResult>(`${this.apiUrl}/import`, formData, {
      headers: this.getAuthHeaders(false)
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
    console.error('EducationService error:', error);
    return throwError(() => error);
  }
}
