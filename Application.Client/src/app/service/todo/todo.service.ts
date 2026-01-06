import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environment/environment';
import {
  CreateTodoStep,
  CreateTodoTask,
  ReorderTodoSteps,
  TodoFilter,
  TodoImageUpload,
  TodoTask,
  UpdateTodoStep,
  UpdateTodoTask,
} from '../../model/todo';
import { NotificationService } from '../notification/notification.service';

@Injectable({
  providedIn: 'root',
})
export class TodoService {
  private readonly apiUrl = `${environment.apiUrl}/todo`;

  constructor(
    private readonly http: HttpClient,
    private readonly notification: NotificationService,
    private readonly translate: TranslateService,
  ) {}

  getTasks(filter?: TodoFilter): Observable<TodoTask[]> {
    let params = new HttpParams();
    const safeFilter = filter || {};

    if (safeFilter.ContextType) {
      params = params.set('ContextType', safeFilter.ContextType);
    }
    if (safeFilter.ContextId) {
      params = params.set('ContextId', safeFilter.ContextId);
    }
    if (safeFilter.AssignedToUserId) {
      params = params.set('AssignedToUserId', safeFilter.AssignedToUserId);
    }
    if (safeFilter.IncludeArchived !== undefined) {
      params = params.set('IncludeArchived', safeFilter.IncludeArchived ? 'true' : 'false');
    }

    return this.http.get<TodoTask[]>(this.apiUrl, { headers: this.getAuthHeaders(), params });
  }

  getTask(id: string): Observable<TodoTask> {
    return this.http.get<TodoTask>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  createTask(dto: CreateTodoTask): Observable<TodoTask> {
    return this.http
      .post<TodoTask>(this.apiUrl, dto, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.created'))));
  }

  updateTask(id: string, dto: UpdateTodoTask): Observable<TodoTask> {
    return this.http
      .put<TodoTask>(`${this.apiUrl}/${id}`, dto, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.updated'))));
  }

  archiveTask(id: string): Observable<void> {
    return this.http
      .post<void>(`${this.apiUrl}/${id}/archive`, {}, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showInfo(this.translate.instant('todo.notifications.archived'))));
  }

  addStep(taskId: string, dto: CreateTodoStep): Observable<TodoTask> {
    return this.http
      .post<TodoTask>(`${this.apiUrl}/${taskId}/steps`, dto, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.stepAdded'))));
  }

  updateStep(taskId: string, stepId: string, dto: UpdateTodoStep): Observable<TodoTask> {
    return this.http
      .put<TodoTask>(`${this.apiUrl}/${taskId}/steps/${stepId}`, dto, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.stepUpdated'))));
  }

  reorderSteps(taskId: string, dto: ReorderTodoSteps): Observable<TodoTask> {
    return this.http
      .post<TodoTask>(`${this.apiUrl}/${taskId}/steps/reorder`, dto, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showInfo(this.translate.instant('todo.notifications.stepsReordered'))));
  }

  deleteStep(taskId: string, stepId: string): Observable<TodoTask> {
    return this.http
      .delete<TodoTask>(`${this.apiUrl}/${taskId}/steps/${stepId}`, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.stepDeleted'))));
  }

  deleteTask(taskId: string): Observable<void> {
    return this.http
      .delete<void>(`${this.apiUrl}/${taskId}`, { headers: this.getAuthHeaders() })
      .pipe(tap(() => this.notification.showSuccess(this.translate.instant('todo.notifications.deleted'))));
  }

  uploadImage(file: File): Observable<TodoImageUpload> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<TodoImageUpload>(`${this.apiUrl}/images`, formData, { headers: this.getAuthHeaders(false) });
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
}
