import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable, throwError } from "rxjs";
import { catchError } from "rxjs/operators";
import { User } from "../../model/User";
import { environment } from "../../environment/environment";

export interface UpdateProfilePayload {
  Name?: string;
  DateOfBirth?: string | null;
  ProfilePictureUrl?: string | null;
}

export interface ChangePasswordPayload {
  CurrentPassword: string;
  NewPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = environment.apiUrl + '/user';

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

  addUser(user: User): Observable<any> {
    return this.http.post(`${this.apiUrl}/add`, user, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  deleteUser(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete/${id}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/all`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getUserById(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/get/${id}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getUsersByRole(role: string): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/role/${role}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getUserByUsername(username: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/username/${username}`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  updateUser(user: User): Observable<any> {
    return this.http.put(`${this.apiUrl}/update`, user, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  updateProfile(payload: UpdateProfilePayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/profile`, payload, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  changePassword(payload: ChangePasswordPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/change-password`, payload, { headers: this.getAuthHeaders() })
      .pipe(catchError(this.handleError));
  }

  uploadProfilePicture(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/profile-picture`, formData, { headers: this.getAuthHeaders(false) })
      .pipe(catchError(this.handleError));
  }

  deleteProfilePicture(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/profile-picture`, { headers: this.getAuthHeaders(false) })
      .pipe(catchError(this.handleError));
  }

  getProfilePicture(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/profile-picture`, { headers: this.getAuthHeaders(false), responseType: 'blob' as const })
      .pipe(catchError(this.handleError));
  }

  private handleError(error: unknown): Observable<never> {
    console.error('An error occurred:', error);
    return throwError(() => error);
  }
}
