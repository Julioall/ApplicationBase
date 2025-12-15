import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LocalStorageService {
  private get isAvailable(): boolean {
    try {
      return typeof window !== 'undefined' && !!window.localStorage;
    } catch {
      return false;
    }
  }

  get<T>(key: string, fallback?: T): T | undefined {
    if (!this.isAvailable) {
      return fallback;
    }

    const raw = localStorage.getItem(key);
    if (raw === null) {
      return fallback;
    }

    try {
      return JSON.parse(raw) as T;
    } catch {
      return fallback;
    }
  }

  set<T>(key: string, value: T): void {
    if (!this.isAvailable) {
      return;
    }

    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch {
      // ignore quota or serialization errors
    }
  }

  remove(key: string): void {
    if (!this.isAvailable) {
      return;
    }

    try {
      localStorage.removeItem(key);
    } catch {
      // ignore errors
    }
  }
}
