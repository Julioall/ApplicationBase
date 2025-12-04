import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  title: string;
  message: string;
  duration: number;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly toastsSubject = new BehaviorSubject<Toast[]>([]);
  readonly toasts$ = this.toastsSubject.asObservable();

  private createId(): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
      return crypto.randomUUID();
    }
    return Math.random().toString(36).slice(2, 9);
  }

  private push(type: ToastType, message: string, title: string, duration: number) {
    const toast: Toast = {
      id: this.createId(),
      type,
      title,
      message,
      duration,
    };

    this.toastsSubject.next([...this.toastsSubject.value, toast]);

    setTimeout(() => this.dismiss(toast.id), duration);
    return toast.id;
  }

  showSuccess(message: string, title: string = 'Sucesso') {
    return this.push('success', message, title, 3400);
  }

  showError(message: string, title: string = 'Erro') {
    return this.push('error', message, title, 4000);
  }

  showWarning(message: string, title: string = 'Aviso') {
    return this.push('warning', message, title, 3600);
  }

  showInfo(message: string, title: string = 'Info') {
    return this.push('info', message, title, 3200);
  }

  dismiss(id: string) {
    this.toastsSubject.next(this.toastsSubject.value.filter((toast) => toast.id !== id));
  }

  clear() {
    this.toastsSubject.next([]);
  }
}
