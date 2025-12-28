import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environment/environment';

export interface WhatsAppInstance {
  id: string;
  displayName: string;
  status: string;
  isActive: boolean;
  createdAt: string;
  ownerUserId?: string;
}

export interface CreateWhatsAppInstanceRequest {
  displayName: string;
  phoneNumber: string;
}

export interface UpdateWhatsAppInstanceRequest {
  displayName: string;
}

export interface WhatsAppQrResponse {
  qrCode: string;
}

export interface WhatsAppStatusResponse {
  status: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class WhatsAppInstancesService {
  private adminBaseUrl = `${environment.apiUrl}/whatsapp/admin/instances`;
  private userBaseUrl = `${environment.apiUrl}/whatsapp/me/instances`;

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

  getAdminInstances(search?: string): Observable<WhatsAppInstance[]> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    return this.http
      .get<WhatsAppInstance[] | any>(this.adminBaseUrl, { headers: this.getAuthHeaders(), params })
      .pipe(map((resp: any) => this.normalizeList(resp)));
  }

  createAdminInstance(request: CreateWhatsAppInstanceRequest): Observable<WhatsAppInstance> {
    return this.http
      .post<WhatsAppInstance | any>(this.adminBaseUrl, request, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  renameAdminInstance(id: string, request: UpdateWhatsAppInstanceRequest): Observable<WhatsAppInstance> {
    return this.http
      .put<WhatsAppInstance | any>(`${this.adminBaseUrl}/${id}`, request, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  deactivateAdminInstance(id: string): Observable<WhatsAppInstance> {
    return this.http
      .post<WhatsAppInstance | any>(`${this.adminBaseUrl}/${id}/deactivate`, {}, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  getAdminQr(id: string): Observable<WhatsAppQrResponse> {
    return this.http
      .post<WhatsAppQrResponse | any>(`${this.adminBaseUrl}/${id}/qr`, {}, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeQr(resp)));
  }

  getAdminStatus(id: string): Observable<WhatsAppStatusResponse> {
    return this.http
      .get<WhatsAppStatusResponse | any>(`${this.adminBaseUrl}/${id}/status`, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeStatus(resp)));
  }

  getUserInstances(): Observable<WhatsAppInstance[]> {
    return this.http
      .get<WhatsAppInstance[] | any>(this.userBaseUrl, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeList(resp)));
  }

  createUserInstance(request: CreateWhatsAppInstanceRequest): Observable<WhatsAppInstance> {
    return this.http
      .post<WhatsAppInstance | any>(this.userBaseUrl, request, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  renameUserInstance(id: string, request: UpdateWhatsAppInstanceRequest): Observable<WhatsAppInstance> {
    return this.http
      .put<WhatsAppInstance | any>(`${this.userBaseUrl}/${id}`, request, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  deactivateUserInstance(id: string): Observable<WhatsAppInstance> {
    return this.http
      .post<WhatsAppInstance | any>(`${this.userBaseUrl}/${id}/deactivate`, {}, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeInstance(resp)));
  }

  getUserQr(id: string): Observable<WhatsAppQrResponse> {
    return this.http
      .post<WhatsAppQrResponse | any>(`${this.userBaseUrl}/${id}/qr`, {}, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeQr(resp)));
  }

  getUserStatus(id: string): Observable<WhatsAppStatusResponse> {
    return this.http
      .get<WhatsAppStatusResponse | any>(`${this.userBaseUrl}/${id}/status`, { headers: this.getAuthHeaders() })
      .pipe(map((resp: any) => this.normalizeStatus(resp)));
  }

  private normalizeList(resp: any): WhatsAppInstance[] {
    if (!Array.isArray(resp)) {
      return [];
    }
    return resp.map((item) => this.normalizeInstance(item));
  }

  private normalizeInstance(resp: any): WhatsAppInstance {
    return {
      id: resp.id ?? resp.Id ?? '',
      displayName: resp.displayName ?? resp.DisplayName ?? '',
      status: resp.status ?? resp.Status ?? 'pending',
      isActive: resp.isActive ?? resp.IsActive ?? true,
      createdAt: resp.createdAt ?? resp.CreatedAt ?? new Date().toISOString(),
      ownerUserId: resp.ownerUserId ?? resp.OwnerUserId,
    };
  }

  private normalizeQr(resp: any): WhatsAppQrResponse {
    return {
      qrCode: resp.qrCode ?? resp.QrCode ?? '',
    };
  }

  private normalizeStatus(resp: any): WhatsAppStatusResponse {
    return {
      status: resp.status ?? resp.Status ?? 'pending',
      isActive: resp.isActive ?? resp.IsActive ?? true,
    };
  }
}
