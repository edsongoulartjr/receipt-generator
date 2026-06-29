import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, delay, of, switchMap, tap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { API_BASE_URL } from './api.config';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${API_BASE_URL}/auth`;
  private readonly accessTokenKey = 'jwt_token';
  private readonly refreshTokenKey = 'refresh_token';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  private roleSubject = new BehaviorSubject<string | null>(this.getRoleFromToken());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  role$ = this.roleSubject.asObservable();

  // Estado compartilhado com o interceptor para evitar múltiplos refreshes simultâneos
  isRefreshing = false;
  refreshTokenSubject = new BehaviorSubject<string | null>(null);

  constructor(private http: HttpClient, private router: Router) { }

  login(credentials: { username: string; password: string }): Observable<AuthResponse> {
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

  // Chamado pelo interceptor — retorna o novo access token via AuthResponse
  doRefresh(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  logout(): void {
    // Revoga o token no servidor (fire-and-forget)
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({ error: () => {} });

    this.clearTokens();
    this.isAuthenticatedSubject.next(false);
    this.roleSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  isSystemAdmin(): boolean {
    return this.getRoleFromToken() === 'SystemAdmin';
  }

  isCoopAdmin(): boolean {
    return this.getRoleFromToken() === 'CoopAdmin';
  }

  isAdminOrAbove(): boolean {
    const role = this.getRoleFromToken();
    return role === 'SystemAdmin' || role === 'CoopAdmin';
  }

  isDriver(): boolean {
    return this.getRoleFromToken() === 'Driver';
  }

  getFullName(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(this.toBase64Url(token.split('.')[1])));
      return payload['fullName'] || null;
    } catch {
      return null;
    }
  }

  getUsername(): string | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(this.toBase64Url(token.split('.')[1])));
      return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
        ?? payload['name']
        ?? null;
    } catch {
      return null;
    }
  }

  private sendLogin(credentials: { username: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        this.storeTokens(response);
        this.isAuthenticatedSubject.next(true);
        this.roleSubject.next(this.getRoleFromToken());
      })
    );
  }

  private storeTokens(response: AuthResponse): void {
    localStorage.setItem(this.accessTokenKey, response.accessToken);
    localStorage.setItem(this.refreshTokenKey, response.refreshToken);
  }

  private clearTokens(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
  }

  private hasToken(): boolean {
    return !!this.getToken();
  }

  private wakeApi(): Observable<unknown> {
    const healthUrl = API_BASE_URL.replace(/\/api\/?$/, '/health');
    return this.http.get(healthUrl, { responseType: 'text' }).pipe(
      catchError(() => of(null))
    );
  }

  private getRoleFromToken(): string | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(this.toBase64Url(token.split('.')[1])));
      return payload.role
        ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? null;
    } catch {
      return null;
    }
  }

  private toBase64Url(value: string): string {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padding = normalized.length % 4;
    return padding ? normalized + '='.repeat(4 - padding) : normalized;
  }
}
