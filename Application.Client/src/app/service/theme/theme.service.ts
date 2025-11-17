import { Injectable } from '@angular/core';

type SupportedTheme = 'light' | 'dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private activeTheme: SupportedTheme = 'light';

  constructor() {
    this.applyTheme();
  }

  toggleTheme(): SupportedTheme {
    this.activeTheme = this.activeTheme === 'light' ? 'dark' : 'light';
    this.applyTheme();
    return this.activeTheme;
  }

  getActiveTheme(): SupportedTheme {
    return this.activeTheme;
  }

  private applyTheme(): void {
    document.body.classList.toggle('dark-theme', this.activeTheme === 'dark');
    document.body.classList.toggle('light-theme', this.activeTheme === 'light');
    document.body.setAttribute('data-theme', this.activeTheme);
  }
}
