import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, MeResponse, UserDto } from '../models/models';

const ACCESS_KEY = 'cs-access';
const REFRESH_KEY = 'cs-refresh';

/** Holds the authenticated session: tokens + current user. Tokens persist so a refresh keeps the user in. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  private readonly _user = signal<UserDto | null>(null);
  private accessToken: string | null = this.read(ACCESS_KEY);
  private refreshToken: string | null = this.read(REFRESH_KEY);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  hasTokens(): boolean {
    return !!this.accessToken;
  }
  getAccessToken(): string | null {
    return this.accessToken;
  }
  getRefreshToken(): string | null {
    return this.refreshToken;
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/api/auth/login`, { email, password }).pipe(tap((r) => this.applyAuth(r)));
  }

  register(email: string, password: string, displayName: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/api/auth/register`, { email, password, displayName })
      .pipe(tap((r) => this.applyAuth(r)));
  }

  refresh(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.base}/api/auth/refresh`, { refreshToken: this.refreshToken })
      .pipe(tap((r) => this.applyAuth(r)));
  }

  loadMe(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.base}/api/auth/me`).pipe(tap((r) => this._user.set(r.user)));
  }

  logout(): void {
    if (this.refreshToken)
      this.http.post(`${this.base}/api/auth/logout`, { refreshToken: this.refreshToken }).subscribe({ error: () => {} });
    this.clear();
  }

  clear(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this._user.set(null);
    this.remove(ACCESS_KEY);
    this.remove(REFRESH_KEY);
  }

  private applyAuth(res: AuthResponse): void {
    this.accessToken = res.tokens.accessToken;
    this.refreshToken = res.tokens.refreshToken;
    this.write(ACCESS_KEY, res.tokens.accessToken);
    this.write(REFRESH_KEY, res.tokens.refreshToken);
    this._user.set(res.user);
  }

  private read(k: string): string | null {
    try { return localStorage.getItem(k); } catch { return null; }
  }
  private write(k: string, v: string): void {
    try { localStorage.setItem(k, v); } catch { /* ignore */ }
  }
  private remove(k: string): void {
    try { localStorage.removeItem(k); } catch { /* ignore */ }
  }
}
