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

  getRole(): string | null {
    const token = this.getToken();
    if (!token) return null;
    const decoded: any = jwtDecode(token);
    return decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
  }
}
