import { Injectable, signal, effect } from '@angular/core';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'domus-aura-theme';
const DARK_CLASS = 'dark-mode'; // matches app.config.ts → providePrimeNG.darkModeSelector

/**
 * Manages the application-wide light and dark theme.
 *
 * Toggles the `.dark-mode` class on the `<html>` element, which is used by PrimeNG
 * as its dark-mode selector. The user's choice is persisted to `localStorage`.
 * On initial load, it falls back to the operating system's color scheme preference.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  /**
   * Signal representing the current theme ('light' or 'dark').
   */
  readonly theme = signal<Theme>(this.loadInitialTheme());

  constructor() {
    // Apply on every theme change and on first load.
    effect(() => {
      const t = this.theme();
      this.applyTheme(t);
      this.persist(t);
    });
  }

  /**
   * Toggles the current theme between light and dark.
   */
  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  /**
   * Explicitly sets the application theme.
   *
   * @param theme - The theme to apply ('light' or 'dark').
   */
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
