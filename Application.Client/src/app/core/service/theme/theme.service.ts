import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {

  private activeTheme: string = 'light';

  constructor() {}

  toggleTheme(): void {
    this.activeTheme = this.activeTheme === 'light' ? 'dark' : 'light';
    document.body.classList.toggle('dark-theme', this.activeTheme === 'dark');
  }

  getActiveTheme(): string {
    return this.activeTheme;
  }
}
