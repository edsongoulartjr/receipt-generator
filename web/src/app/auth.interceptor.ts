import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Endpoints de auth não recebem Bearer e não disparam refresh
  if (isAuthEndpoint(req)) {
    return next(req);
  }

  const token = authService.getToken();
  const authedReq = token ? withBearer(req, token) : req;

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }
      return handle401(req, next, authService, router);
    })
  );
};

function handle401(
  req: HttpRequest<unknown>,
  next: Parameters<HttpInterceptorFn>[1],
  authService: AuthService,
  router: Router
) {
  if (!authService.isRefreshing) {
    // Primeira requisição a receber 401 — inicia o refresh
    authService.isRefreshing = true;
    authService.refreshTokenSubject.next(null);

    return authService.doRefresh().pipe(
      switchMap(response => {
        authService.isRefreshing = false;
        authService.refreshTokenSubject.next(response.accessToken);
        return next(withBearer(req, response.accessToken));
      }),
      catchError(err => {
        authService.isRefreshing = false;
        // Refresh falhou — sessão encerrada
        authService.logout();
        return throwError(() => err);
      })
    );
  }

  // Outras requisições que receberam 401 enquanto o refresh estava em andamento
  // aguardam o novo token antes de repetir
  return authService.refreshTokenSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => next(withBearer(req, token!)))
  );
}

function isAuthEndpoint(req: HttpRequest<unknown>): boolean {
  return req.url.includes('/auth/login')
    || req.url.includes('/auth/refresh')
    || req.url.includes('/auth/logout');
}

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) });
}
