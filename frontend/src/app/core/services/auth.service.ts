import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';

const STORAGE_KEY = 'interview-ai.session';

/** What we keep in sessionStorage after login. */
interface StoredSession {
  accessToken: string;
  expiresAt: string;
  user: User;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  /** The current session, or null when logged out. */
  private readonly session = signal<StoredSession | null>(this.loadSession());

  /** The logged-in user, or null. Components can display this. */
  readonly currentUser = computed(() => this.session()?.user ?? null);

  /** True when there is a session whose token hasn't expired. */
  isAuthenticated(): boolean {
    const session = this.session();
    return session !== null && new Date(session.expiresAt).getTime() > Date.now();
  }

  /** The token to send to the API, or null if not logged in / expired. */
  get accessToken(): string | null {
    return this.isAuthenticated() ? this.session()!.accessToken : null;
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/login', request)
      .pipe(tap((response) => this.saveSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', request)
      .pipe(tap((response) => this.saveSession(response)));
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  // ---------- private helpers ----------

  private saveSession(response: AuthResponse): void {
    const session: StoredSession = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      user: response.user,
    };

    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private clearSession(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }

  private loadSession(): StoredSession | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const session = JSON.parse(raw) as StoredSession;

      // Ignore a stored session whose token has already expired
      return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
    } catch {
      return null;
    }
  }
}