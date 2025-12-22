import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import { NotificationListResult } from '../../model/notification';

@Injectable({
  providedIn: 'root',
})
export class NotificationApiService {
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  constructor(private readonly http: HttpClient) {}

  getLatest(take = 15): Observable<NotificationListResult> {
    return this.http.get<NotificationListResult>(`${this.baseUrl}?take=${take}`);
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/read`, {});
  }

  markManyAsRead(ids: string[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/read-all`, { ids });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
