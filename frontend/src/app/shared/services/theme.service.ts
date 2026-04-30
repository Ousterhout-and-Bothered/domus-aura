import { Injectable, signal, effect } from '@angular/core';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'domus-aura-theme';
const DARK_CLASS = 'dark-mode'; // matches app.config.ts → providePrimeNG.darkModeSelector

/**
 * App-wide light/dark theme. Toggles the `.dark-mode` class on <html>, which
 * the PrimeNG Aura preset uses as its dark-mode selector. Choice is persisted
 * to localStorage; on first load, falls back to the user's OS preference.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(this.loadInitialTheme());

  constructor() {
    // Apply on every theme change and on first load.
    effect(() => {
      const t = this.theme();
      this.applyTheme(t);
      this.persist(t);
    });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  set(theme: Theme): void {
    this.theme.set(theme);
  }

  /* ─────────────── internals ─────────────── */

  private loadInitialTheme(): Theme {
    if (typeof localStorage !== 'undefined') {
      const saved = localStorage.getItem(STORAGE_KEY) as Theme | null;
      if (saved === 'light' || saved === 'dark') return saved;
    }
    if (typeof window !== 'undefined' && window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';
    }
    return 'light';
  }

  private applyTheme(theme: Theme): void {
    if (typeof document === 'undefined') return;
    const html = document.documentElement;
    if (theme === 'dark') html.classList.add(DARK_CLASS);
    else html.classList.remove(DARK_CLASS);
  }

  private persist(theme: Theme): void {
    if (typeof localStorage === 'undefined') return;
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // Ignore quota errors — theme will reset to OS preference next reload.
    }
  }
}
