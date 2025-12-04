import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { User } from '../../model/User';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../environment/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/User/add`;

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<any> {
    const url = `${environment.apiUrl}/Authentication/login`;
    const body = {
      Email: email,
      Password: password
    };
    return this.http.post<any>(url, body).pipe(
      map((response) => {
        if (response && response.token) {
          this.saveToken(response.token);
          if (response.refreshToken) {
            this.saveRefreshToken(response.refreshToken);
          }
        }
        return response;
      }),
      catchError(() => {
        return throwError(() => new Error('Login failed'));
      })
    );
  }

  signup(user: User): Observable<any> {
    return this.http.post<any>(this.apiUrl, user).pipe(
      catchError((err) => throwError(() => err))
    );
  }

  sendPasswordReset(email: string, resetUrl?: string): Observable<any> {
    const url = `${environment.apiUrl}/email/reset`;
    const body: any = { email };
    if (resetUrl) {
      body.resetUrl = resetUrl;
    }
    return this.http.post<any>(url, body);
  }

  logout(): void {
    this.removeToken();
  }

  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  saveRefreshToken(token: string): void {
    localStorage.setItem('refreshToken', token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  refreshToken(): Observable<any> {
    const refresh = this.getRefreshToken();
    if (!refresh) {
      return throwError(() => new Error('No refresh token available'));
    }
    const url = `${environment.apiUrl}/Authentication/refresh`;
    return this.http.post<any>(url, { refreshToken: refresh }).pipe(
      map(response => {
        if (response?.token) {
          this.saveToken(response.token);
        }
        if (response?.refreshToken) {
          this.saveRefreshToken(response.refreshToken);
        }
        return response;
      })
    );
  }

  removeToken(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }

  isTokenExpired(token: string): boolean {
    try {
      const decoded: any = jwtDecode(token);
      if (decoded.exp === undefined) return false;
      const date = new Date(0);
      date.setUTCSeconds(decoded.exp);
      return date.valueOf() < new Date().valueOf();
    } catch {
      return true;
    }
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    return token !== null && !this.isTokenExpired(token);
  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    if (token && !this.isTokenExpired(token)) {
      return new HttpHeaders().set('Authorization', `Bearer ${token}`);
    }
    return new HttpHeaders();
  }

  getPermissions(): string[] {
    const token = this.getToken();
    if (!token) return [];
    try {
      const decoded: any = jwtDecode(token);
      const raw = decoded['permissions'] ?? decoded['permission'];

      if (!raw) {
        return [];
      }

      if (Array.isArray(raw)) {
        return raw;
      }

      if (typeof raw === 'string') {
        return raw.split(',').map((value: string) => value.trim()).filter(Boolean);
      }

      return [];
    } catch {
      return [];
    }
  }

  hasPermission(permission: string): boolean {
    return this.getPermissions().includes(permission);
  }

  hasAnyPermission(permissions: string[]): boolean {
    const current = this.getPermissions();
    return permissions.some(p => current.includes(p));
  }

  getEmail(): string | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }
    try {
      const decoded: any = jwtDecode(token);
      return (
        decoded['email'] ||
        decoded['unique_name'] ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        null
      );
    } catch {
      return null;
    }
  }
}
