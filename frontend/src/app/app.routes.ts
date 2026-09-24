import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  // ---------- Public pages (only when logged OUT) ----------
  {
    path: 'login',
    title: 'Log in · Interview AI',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account · Interview AI',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register').then((m) => m.Register),
  },

  // ---------- Logged-in area ----------
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        title: 'My interview plans · Interview AI',
        loadComponent: () =>
          import('./features/reports/home/home').then((m) => m.Home),
      },
      {
        path: 'reports/:id',
        title: 'Interview plan · Interview AI',
        loadComponent: () =>
          import('./features/reports/report-detail/report-detail').then(
            (m) => m.ReportDetail,
          ),
      },
      {
        path: 'reports/:id/practice',
        title: 'Mock interview · Interview AI',
        loadComponent: () =>
          import('./features/reports/practice/practice').then(
            (m) => m.Practice,
          ),
      },
    ],
  },

  // ---------- Anything else ----------
  {
    path: '**',
    redirectTo: '',
  },
];
