import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environment/environment';
import { MoodleCategory } from '../../model/moodle-category';
import { MoodleCourseCategory } from '../../model/moodle-course-category';
import { MoodleCohort } from '../../model/moodle-cohort';
import { MoodleCourse } from '../../model/moodle-course';
import { MoodleSyncStatus } from '../../model/moodle-sync-status';
import { PagedResult } from '../../model/paged-result';
import { CourseSearchQuery } from '../../model/course-search-query';
import { StudentCourseDto } from '../../model/student-course-dto';

@Injectable({
  providedIn: 'root'
})
export class MoodleService {
  private readonly apiUrl = `${environment.apiUrl}/moodle`;

  constructor(private readonly http: HttpClient) {}

  getCategories(): Observable<MoodleCategory[]> {
    return this.http.get<MoodleCategory[]>(`${this.apiUrl}/categories`)
      .pipe(catchError(this.handleError));
  }

  getCourseCategories(categoryId: string): Observable<MoodleCourseCategory[]> {
    const params = new HttpParams().set('categoryId', categoryId);
    return this.http.get<MoodleCourseCategory[]>(`${this.apiUrl}/course-categories`, { params })
      .pipe(catchError(this.handleError));
  }

  getCohorts(courseCategoryId: string): Observable<MoodleCohort[]> {
    const params = new HttpParams().set('courseCategoryId', courseCategoryId);
    return this.http.get<MoodleCohort[]>(`${this.apiUrl}/cohorts`, { params })
      .pipe(catchError(this.handleError));
  }

  getSyncStatus(): Observable<MoodleSyncStatus> {
    return this.http.get<MoodleSyncStatus>(`${this.apiUrl}/sync-status`)
      .pipe(catchError(this.handleError));
  }

  triggerSync(): Observable<MoodleSyncStatus> {
    return this.http.post<MoodleSyncStatus>(`${this.apiUrl}/sync/trigger`, null)
      .pipe(catchError(this.handleError));
  }

  searchCourses(query: CourseSearchQuery): Observable<PagedResult<MoodleCourse>> {
    let params = new HttpParams();

    if (query.pageNumber) {
      params = params.set('PageNumber', query.pageNumber);
    }
    if (query.pageSize) {
      params = params.set('PageSize', query.pageSize);
    }
    if (query.search) {
      params = params.set('Search', query.search);
    }
    if (query.cohortId) {
      params = params.set('cohortId', query.cohortId);
    }
    if (query.courseCategoryId) {
      params = params.set('courseCategoryId', query.courseCategoryId);
    }

    return this.http.get<PagedResult<MoodleCourse>>(`${this.apiUrl}/courses/search`, { params })
      .pipe(catchError(this.handleError));
  }

  getCohortCourses(cohortId: string): Observable<MoodleCourse[]> {
    const params = new HttpParams().set('cohortId', cohortId);

    return this.http.get<MoodleCourse[]>(`${this.apiUrl}/courses`, { params }).pipe(catchError(this.handleError));
  }

  getCourseStudents(eadId: number): Observable<StudentCourseDto[]> {
    const params = new HttpParams().set('eadId', eadId);
    return this.http.get<StudentCourseDto[]>(`${this.apiUrl}/courses/students`, { params })
      .pipe(catchError(this.handleError));
  }

  toggleActivityHidden(studentId: string, courseId: string, activityName: string): Observable<void> {
    const params = new HttpParams()
      .set('studentId', studentId)
      .set('courseId', courseId)
      .set('activityName', activityName);
    return this.http.patch<void>(`${this.apiUrl}/students/activities/toggle-hidden`, null, { params })
      .pipe(catchError(this.handleError));
  }

  private handleError(error: unknown): Observable<never> {
    console.error('MoodleService error:', error);
    return throwError(() => error);
  }
}
