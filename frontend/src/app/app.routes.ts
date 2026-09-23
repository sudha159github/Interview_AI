import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  // ---------- Public pages (only when logged OUT) ----------
  {
    path: 'login',
    title: 'Log in · Interview AI',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account · Interview AI',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },

  // ---------- Logged-in area (inside the shell with header) ----------
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        title: 'My interview plans · Interview AI',
        loadComponent: () => import('./features/reports/home/home').then((m) => m.Home),
      },
      {
        path: 'reports/:id',
        title: 'Interview plan · Interview AI',
        loadComponent: () =>
          import('./features/reports/report-detail/report-detail').then((m) => m.ReportDetail),
      },
    ],
  },

  // ---------- Anything else ----------
  { path: '**', redirectTo: '' },
];