import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

const ANON_PATHS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];

const isApi = (url: string) => url.startsWith(environment.apiBase);
const isAnon = (url: string) => ANON_PATHS.some((p) => url.includes(p));

function withToken(req: HttpRequest<unknown>, auth: AuthService): HttpRequest<unknown> {
  const token = auth.getAccessToken();
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

/** Attaches the access token to API requests and transparently refreshes once on a 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!isApi(req.url) || isAnon(req.url)) return next(req);

  return next(withToken(req, auth)).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || !auth.getRefreshToken()) return throwError(() => err);
      return refreshAndRetry(req, next, auth, router);
    }),
  );
};

function refreshAndRetry(req: HttpRequest<unknown>, next: HttpHandlerFn, auth: AuthService, router: Router): Observable<HttpEvent<unknown>> {
  return auth.refresh().pipe(
    switchMap(() => next(withToken(req, auth))),
    catchError((err) => {
      auth.clear();
      void router.navigate(['/login']);
      return throwError(() => err);
    }),
  );
}
