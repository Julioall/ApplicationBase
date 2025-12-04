import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environment/environment';

export interface EmailSettings {
  id?: string;
  fromName: string;
  fromEmail: string;
  host: string;
  port: number;
  secure: string;
  password: string;
}

export interface SendResetRequest {
  email: string;
}

@Injectable({
  providedIn: 'root',
})
export class EmailSettingsService {
  private apiUrl = `${environment.apiUrl}/email`;

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

  getSettings(): Observable<EmailSettings> {
    return this.http
      .get<EmailSettings | any>(`${this.apiUrl}/settings`, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalize(resp)));
  }

  updateSettings(settings: EmailSettings): Observable<EmailSettings> {
    return this.http
      .put<EmailSettings | any>(`${this.apiUrl}/settings`, settings, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalize(resp)));
  }

  sendTestEmail(body: SendResetRequest, settings: EmailSettings): Observable<any> {
    return this.http.post(`${this.apiUrl}/test`, { ...body, settings }, { headers: this.getAuthHeaders() });
  }

  private normalize(resp: any): EmailSettings {
    if (!resp) {
      return {
        fromName: '',
        fromEmail: '',
        host: '',
        port: 0,
        secure: '',
        password: '',
      };
    }

    return {
      id: resp.id ?? resp.Id,
      fromName: resp.fromName ?? resp.FromName ?? '',
      fromEmail: resp.fromEmail ?? resp.FromEmail ?? '',
      host: resp.host ?? resp.Host ?? '',
      port: resp.port ?? resp.Port ?? 0,
      secure: resp.secure ?? resp.Secure ?? '',
      password: resp.password ?? resp.Password ?? '',
    };
  }
}
