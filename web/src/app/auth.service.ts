import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, BehaviorSubject, catchError, delay, of, switchMap, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { API_BASE_URL } from './api.config';

interface LoginResponse {
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${API_BASE_URL}/auth`;
  private tokenKey = 'jwt_token';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  private roleSubject = new BehaviorSubject<string | null>(this.getRoleFromToken());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  role$ = this.roleSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) { }

  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.sendLogin(credentials).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status !== 0) {
          return throwError(() => error);
        }

        return this.wakeApi().pipe(
          delay(1000),
          switchMap(() => this.sendLogin(credentials))
        );
      })
    );
  }

  logout(): void {
    this.removeToken();
    this.isAuthenticatedSubject.next(false);
    this.roleSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  private setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  private removeToken(): void {
    localStorage.removeItem(this.tokenKey);
  }

  private hasToken(): boolean {
    return !!this.getToken();
  }

  isSuperAdmin(): boolean {
    return this.getRoleFromToken() === 'SuperAdmin';
  }

  private sendLogin(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        if (response?.token) {
          this.setToken(response.token);
          this.isAuthenticatedSubject.next(true);
          this.roleSubject.next(this.getRoleFromToken());
        }
      })
    );
  }

  private wakeApi(): Observable<unknown> {
    const healthUrl = API_BASE_URL.replace(/\/api\/?$/, '/health');
    return this.http.get(healthUrl, { responseType: 'text' }).pipe(
      catchError(() => of(null))
    );
  }

  private getRoleFromToken(): string | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    try {
      const payload = JSON.parse(atob(this.toBase64(token.split('.')[1])));
      return payload.role
        ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? null;
    } catch {
      return null;
    }
  }

  private toBase64(value: string): string {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padding = normalized.length % 4;
    return padding ? normalized + '='.repeat(4 - padding) : normalized;
  }
}
