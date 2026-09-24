import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';
const STORAGE_KEY = 'interview-ai.session';
/** Renew this long before the token expires. */
const RENEW_BEFORE_MS = 5 * 60 * 1000;
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
private readonly session = signal<StoredSession | null>(this.loadSession());
private renewTimer?: ReturnType<typeof setTimeout>;
/** The logged-in user, or null. */
readonly currentUser = computed(() => this.session()?.user ?? null);
/** True when the session could not be renewed and will end soon. */
readonly sessionEnding = signal(false);
constructor() {
this.scheduleRenewal();
}
isAuthenticated(): boolean {
const session = this.session();
return session !== null && new Date(session.expiresAt).getTime() > Date.now();
}
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
/** Renew now, for example when the user clicks "Stay signed in". */
renewNow(): void {
this.renewSession();
}
// ---------- private helpers ----------
private renewSession(): void {
if (!this.isAuthenticated()) {
return;
}
this.http.post<AuthResponse>('/api/auth/refresh', {}).subscribe({
next: (response) => {
this.sessionEnding.set(false);
this.saveSession(response);
},
error: () => {
// The session cannot be extended (for example it hit its maximum length).
// Warn the user so they can finish and save their work.
this.sessionEnding.set(true);
},
});
}
private scheduleRenewal(): void {
clearTimeout(this.renewTimer);
const session = this.session();
if (!session) {
return;
}
const msUntilExpiry = new Date(session.expiresAt).getTime() - Date.now();
const renewIn = Math.max(msUntilExpiry - RENEW_BEFORE_MS, 10_000);
this.renewTimer = setTimeout(() => this.renewSession(), renewIn);
}
private saveSession(response: AuthResponse): void {
const session: StoredSession = {
accessToken: response.accessToken,
expiresAt: response.expiresAt,
user: response.user,
};
try {
sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
} catch {
// Storage can be unavailable; the session still works for this tab
}
this.session.set(session);
this.scheduleRenewal();
}
private clearSession(): void {
clearTimeout(this.renewTimer);
this.sessionEnding.set(false);
try {
sessionStorage.removeItem(STORAGE_KEY);
} catch {
// ignore
}
this.session.set(null);
}
private loadSession(): StoredSession | null {
try {
const raw = sessionStorage.getItem(STORAGE_KEY);
if (!raw) {
return null;
}
const session = JSON.parse(raw) as StoredSession;
return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
} catch {
return null;
}
}
}