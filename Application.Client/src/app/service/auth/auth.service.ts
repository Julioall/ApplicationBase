import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { User } from '../../model/User';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../environment/environment';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/User/add`;

  constructor(private http: HttpClient, private readonly translate: TranslateService) {}

  login(email: string, password: string, remember: boolean = true): Observable<any> {
    const url = `${environment.apiUrl}/Authentication/login`;
    const body = {
      Email: email,
      Password: password
    };
    return this.http.post<any>(url, body).pipe(
      map((response) => {
        if (response && response.token) {
          this.saveToken(response.token, remember);
          if (response.refreshToken) {
            this.saveRefreshToken(response.refreshToken, remember);
          }
        }
        return response;
      }),
      catchError(() => {
        return throwError(() => new Error(this.translate.instant('auth.errors.loginFailed')));
      })
    );
  }

  loginMoodle(username: string, password: string, remember: boolean = true): Observable<any> {
    const url = `${environment.apiUrl}/Authentication/moodle/login`;
    const body = {
      Username: username,
      Password: password
    };

    return this.http.post<any>(url, body).pipe(
      map((response) => {
        if (response && response.token) {
          this.saveToken(response.token, remember);
          if (response.refreshToken) {
            this.saveRefreshToken(response.refreshToken, remember);
          }
        }
        return response;
      }),
      catchError(() => {
        return throwError(() => new Error(this.translate.instant('auth.errors.loginFailed')));
      })
    );
  }

  signup(user: User): Observable<any> {
    return this.http.post<any>(this.apiUrl, user).pipe(
      catchError((err) => throwError(() => err))
    );
  }

  sendPasswordReset(email: string): Observable<any> {
    const url = `${environment.apiUrl}/email/reset`;
    return this.http.post<any>(url, { email });
  }

  logout(): void {
    this.removeToken();
  }

  saveToken(token: string, remember: boolean = true): void {
    if (remember) {
      localStorage.setItem('token', token);
      sessionStorage.removeItem('token');
    } else {
      sessionStorage.setItem('token', token);
      localStorage.removeItem('token');
    }
  }

  saveRefreshToken(token: string, remember: boolean = true): void {
    if (remember) {
      localStorage.setItem('refreshToken', token);
      sessionStorage.removeItem('refreshToken');
    } else {
      sessionStorage.setItem('refreshToken', token);
      localStorage.removeItem('refreshToken');
    }
  }

  getToken(): string | null {
    return localStorage.getItem('token') || sessionStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken') || sessionStorage.getItem('refreshToken');
  }

  isRemembered(): boolean {
    return localStorage.getItem('token') !== null;
  }

  refreshToken(): Observable<any> {
    const refresh = this.getRefreshToken();
    if (!refresh) {
      return throwError(() => new Error(this.translate.instant('auth.errors.refreshTokenMissing')));
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
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
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

  isMoodleUser(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      const decoded: any = jwtDecode(token);
      const provider = decoded['auth_provider'];
      const sub = decoded['sub'] as string | undefined;
      return (typeof provider === 'string' && provider.toLowerCase() === 'moodle') || (sub?.toLowerCase().startsWith('moodle:') ?? false);
    } catch {
      return false;
    }
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
