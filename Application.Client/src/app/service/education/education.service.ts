import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environment/environment';
import { MoodleCategory, School, Program, Class } from '../../model/moodle-category';
import { CourseUnit } from '../../model/course-unit';
import { EducationSyncStatus } from '../../model/education-sync-status';
import { PagedResult } from '../../model/paged-result';
import { UcSearchQuery } from '../../model/uc-search-query';
import { Student } from '../../model/student';
import { StudentUcDto } from '../../model/student-uc-dto';

@Injectable({
  providedIn: 'root'
})
export class EducationService {
  private readonly apiUrl = `${environment.apiUrl}/education`;

  constructor(private readonly http: HttpClient) {}

  getSchools(): Observable<School[]> {
    return this.http.get<School[]>(`${this.apiUrl}/schools`)
      .pipe(catchError(this.handleError));
  }

  getPrograms(schoolId: string): Observable<Program[]> {
    const params = new HttpParams().set('schoolId', schoolId);
    return this.http.get<Program[]>(`${this.apiUrl}/programs`, { params })
      .pipe(catchError(this.handleError));
  }

  getClasses(programId: string): Observable<Class[]> {
    const params = new HttpParams().set('programId', programId);
    return this.http.get<Class[]>(`${this.apiUrl}/classes`, { params })
      .pipe(catchError(this.handleError));
  }

  getSyncStatus(): Observable<EducationSyncStatus> {
    return this.http.get<EducationSyncStatus>(`${this.apiUrl}/sync-status`)
      .pipe(catchError(this.handleError));
  }

  triggerSync(): Observable<EducationSyncStatus> {
    return this.http.post<EducationSyncStatus>(`${this.apiUrl}/sync/trigger`, null)
      .pipe(catchError(this.handleError));
  }

  searchCourseUnits(query: UcSearchQuery, classId?: string, programId?: string): Observable<PagedResult<CourseUnit>> {
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

    return this.http.get<PagedResult<CourseUnit>>(`${this.apiUrl}/course-units/search`, { params })
      .pipe(catchError(this.handleError));
  }

  getClassCourseUnits(classId: string): Observable<CourseUnit[]> {
    const params = new HttpParams().set('classId', classId);

    return this.http.get<CourseUnit[]>(`${this.apiUrl}/course-units`, { params }).pipe(catchError(this.handleError));
  }

  getCourseUnitStudents(eadId: number): Observable<StudentUcDto[]> {
    const params = new HttpParams().set('eadId', eadId);
    return this.http.get<StudentUcDto[]>(`${this.apiUrl}/course-units/students`, { params })
      .pipe(catchError(this.handleError));
  }

  toggleActivityHidden(studentId: string, courseUnitId: string, activityName: string): Observable<void> {
    const params = new HttpParams()
      .set('studentId', studentId)
      .set('courseUnitId', courseUnitId)
      .set('activityName', activityName);
    return this.http.patch<void>(`${this.apiUrl}/students/activities/toggle-hidden`, null, { params })
      .pipe(catchError(this.handleError));
  }


  private handleError(error: unknown): Observable<never> {
    console.error('EducationService error:', error);
    return throwError(() => error);
  }
}
