import { Injectable, effect, signal } from '@angular/core';

export type ThemeMode = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'interview-ai.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  /** The user's choice. 'system' follows the operating system. */
  readonly mode = signal<ThemeMode>(this.load());

  constructor() {
    // Whenever the mode changes, update <html> and remember the choice
    effect(() => this.apply(this.mode()));
  }

  toggle(): void {
    this.mode.set(this.resolved() === 'dark' ? 'light' : 'dark');
  }

  /** What is actually being shown right now. */
  resolved(): 'light' | 'dark' {
    const mode = this.mode();

    if (mode !== 'system') {
      return mode;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private apply(mode: ThemeMode): void {
    const root = document.documentElement;

    if (mode === 'system') {
      root.removeAttribute('data-theme');
    } else {
      root.setAttribute('data-theme', mode);
    }

    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // Storage can be unavailable (private mode); the theme still works for this visit
    }
  }

  private load(): ThemeMode {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'light' || stored === 'dark' ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}

