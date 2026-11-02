import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResult, Credentials, User } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = 'taskmanager.session';
  private readonly session = signal<AuthResult | null>(this.restore());
  private expiryTimer?: ReturnType<typeof setTimeout>;
  readonly user = computed(() => this.session()?.user ?? null);

  constructor() {
    this.scheduleExpiry();
    inject(DestroyRef).onDestroy(() => clearTimeout(this.expiryTimer));
  }
  get token(): string | null {
    const session = this.session();
    if (!session) return null;
    if (Date.parse(session.expiresAtUtc) <= Date.now()) {
      this.logout(false);
      return null;
    }
    return session.accessToken;
  }
  login(credentials: Credentials): Observable<AuthResult> {
    return this.http.post<AuthResult>('/api/auth/login', credentials).pipe(
      tap((session) => {
        try {
          sessionStorage.setItem(this.storageKey, JSON.stringify(session));
        } catch {
          this.removeStoredSession(); /* Memory-only session when browser storage is unavailable. */
        }
        this.session.set(session);
        this.scheduleExpiry();
      }),
    );
  }
  register(credentials: Credentials): Observable<User> {
    return this.http.post<User>('/api/auth/register', credentials);
  }
  logout(navigate = true): void {
    clearTimeout(this.expiryTimer);
    this.session.set(null);
    this.removeStoredSession();
    if (navigate) void this.router.navigate(['/login']);
  }
  private restore(): AuthResult | null {
    try {
      const value: AuthResult | null = JSON.parse(
        sessionStorage.getItem(this.storageKey) ?? 'null',
      );
      if (
        value &&
        typeof value.accessToken === 'string' &&
        value.accessToken.length > 0 &&
        typeof value.user?.id === 'string' &&
        typeof value.user?.email === 'string' &&
        Date.parse(value.expiresAtUtc) > Date.now()
      )
        return value;
    } catch {
      /* Corrupt/blocked storage must not authenticate the browser. */
    }
    this.removeStoredSession();
    return null;
  }
  private removeStoredSession(): void {
    try {
      sessionStorage.removeItem(this.storageKey);
    } catch {
      /* Clearing in-memory authentication still succeeds when storage is blocked. */
    }
  }
  private scheduleExpiry(): void {
    clearTimeout(this.expiryTimer);
    const session = this.session();
    if (session)
      this.expiryTimer = setTimeout(
        () => this.logout(),
        Math.min(Date.parse(session.expiresAtUtc) - Date.now(), 2_147_483_647),
      );
  }
}
