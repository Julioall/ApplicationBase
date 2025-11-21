import { Injectable } from '@angular/core';

export type SupportedTheme = 'light' | 'dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private static readonly THEME_STORAGE_KEY = 'preferredTheme';
  private activeTheme: SupportedTheme = 'light';

  constructor() {
    const storedTheme = this.loadStoredTheme();
    if (storedTheme) {
      this.activeTheme = storedTheme;
    }
    this.applyTheme();
  }

  toggleTheme(): SupportedTheme {
    const nextTheme: SupportedTheme = this.activeTheme === 'light' ? 'dark' : 'light';
    this.setTheme(nextTheme);
    return this.activeTheme;
  }

  setTheme(theme: SupportedTheme): void {
    if (this.activeTheme !== theme) {
      this.activeTheme = theme;
      this.persistTheme(theme);
    }
    this.applyTheme();
  }

  getActiveTheme(): SupportedTheme {
    return this.activeTheme;
  }

  private loadStoredTheme(): SupportedTheme | null {
    try {
      const stored = localStorage.getItem(ThemeService.THEME_STORAGE_KEY);
      if (stored === 'dark' || stored === 'light') {
        return stored;
      }
    } catch {
      // ignore storage errors
    }
    return null;
  }

  private persistTheme(theme: SupportedTheme): void {
    try {
      localStorage.setItem(ThemeService.THEME_STORAGE_KEY, theme);
    } catch {
      // ignore storage errors
    }
  }

  private applyTheme(): void {
    if (typeof document === 'undefined') {
      return;
    }
    const classList = document.body.classList;
    classList.remove('light-theme', 'dark-theme');
    classList.add(this.activeTheme === 'dark' ? 'dark-theme' : 'light-theme');
    document.body.setAttribute('data-theme', this.activeTheme);
  }
}
