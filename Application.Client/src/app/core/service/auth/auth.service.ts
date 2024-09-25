import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { User } from '../../model/User';
import { jwtDecode } from 'jwt-decode';  // Ajuste na importação
import { environment } from '../../../environment/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/User/add`;

  constructor(private http: HttpClient) {} // Injeta o HttpClient para fazer requisições HTTP.

  /**
   * Faz o login do usuário enviando o nome de usuário e senha para o backend.
   * @param email O nome de usuário ou email do usuário.
   * @param password A senha do usuário.
   * @returns Um Observable contendo a resposta da requisição.
   */
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
        }
        return response;
      }),
      catchError(error => {
        return throwError(() => new Error('Login failed'));
      })
    );
  }

  /**
   * Faz o cadastro de um novo usuário enviando seus dados para o backend.
   * @param user O objeto User contendo os dados do novo usuário.
   * @returns Um Observable contendo a resposta da requisição.
   */
  signup(user: User): Observable<any> {
    return this.http.post<any>(this.apiUrl, user).pipe(
      catchError(error => {
        return throwError(() => new Error('Sign up failed'));
      })
    );
  }

  /**
   * Faz o logout do usuário removendo o token do localStorage.
   */
  logout(): void {
    this.removeToken();
  }

  /**
   * Salva o token no localStorage.
   * @param token O token JWT a ser salvo.
   */
  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  /**
   * Recupera o token do localStorage.
   * @returns O token JWT ou null se não estiver presente.
   */
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  /**
   * Remove o token do localStorage.
   */
  removeToken(): void {
    localStorage.removeItem('token');
  }

  /**
   * Verifica se o token JWT está expirado.
   * @param token O token JWT a ser verificado.
   * @returns true se o token estiver expirado, caso contrário false.
   */
  isTokenExpired(token: string): boolean {
    try {
      const decoded: any = jwtDecode(token); // Ajuste na utilização da função decode
      if (decoded.exp === undefined) return false;
      const date = new Date(0);
      date.setUTCSeconds(decoded.exp);
      return date.valueOf() < new Date().valueOf();
    } catch (err) {
      return true; // Retorna true se houver um erro ao decodificar o token.
    }
  }

  /**
   * Verifica se o usuário está logado checando a validade do token.
   * @returns true se o usuário estiver logado e o token for válido, caso contrário false.
   */
  isLoggedIn(): boolean {
    const token = this.getToken();
    return token !== null && !this.isTokenExpired(token);
  }

  /**
   * Obtém os cabeçalhos de autenticação com o token JWT.
   * @returns Um HttpHeaders com o token de autenticação se estiver presente e válido.
   */
  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    if (token && !this.isTokenExpired(token)) {
      return new HttpHeaders().set('Authorization', `Bearer ${token}`);
    }
    return new HttpHeaders();
  }
}
