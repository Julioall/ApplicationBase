import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environment/environment';

export interface WhatsAppSettings {
  maxUserInstances: number;
}

@Injectable({
  providedIn: 'root',
})
export class WhatsAppSettingsService {
  private settingsUrl = `${environment.apiUrl}/settings/whatsapp`;

  constructor(private http: HttpClient) {}

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

  getSettings(): Observable<WhatsAppSettings> {
    return this.http
      .get<WhatsAppSettings | any>(this.settingsUrl, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalize(resp)));
  }

  updateSettings(settings: WhatsAppSettings): Observable<WhatsAppSettings> {
    return this.http
      .put<WhatsAppSettings | any>(this.settingsUrl, settings, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalize(resp)));
  }

  private normalize(resp: any): WhatsAppSettings {
    if (!resp) {
      return { maxUserInstances: 1 };
    }

    return {
      maxUserInstances: resp.maxUserInstances ?? resp.MaxUserInstances ?? 1,
    };
  }
}
