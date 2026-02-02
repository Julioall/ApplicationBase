import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NotificationService, Toast, ToastType } from '../../service/notification/notification.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-toast-container',
  template: `
    <div [ngClass]="getContainerClasses()" *ngIf="toasts$ | async as toasts" aria-live="polite" role="status">
      <div class="flex flex-col space-y-3">
        <article 
          *ngFor="let toast of toasts; trackBy: trackById" 
          [ngClass]="getToastCardClasses(toast.type)"
          class="animate-[toast-in_160ms_ease]"
        >
          <div [ngClass]="getToastIconClasses(toast.type)">
            <i [class]="iconMap[toast.type]" aria-hidden="true"></i>
          </div>
          <div class="flex flex-col space-y-1">
            <div class="font-semibold text-sm">{{ toast.title }}</div>
            <div class="text-sm text-text-subtle">{{ toast.message }}</div>
          </div>
          <button 
            type="button" 
            class="border-none bg-transparent text-text-subtle cursor-pointer p-1 rounded-sm inline-flex items-center justify-center transition-all duration-150 hover:bg-surface-alt hover:text-text-primary" 
            aria-label="Fechar notificação" 
            (click)="dismiss(toast.id)"
          >
            <i class="fa-solid fa-xmark" aria-hidden="true"></i>
          </button>
        </article>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainerComponent {
  toasts$: Observable<Toast[]> = this.notificationService.toasts$;

  iconMap: Record<ToastType, string> = {
    success: 'fa-solid fa-check',
    error: 'fa-solid fa-xmark',
    warning: 'fa-solid fa-triangle-exclamation',
    info: 'fa-solid fa-circle-info',
  };

  constructor(private readonly notificationService: NotificationService) {}

  trackById(_: number, toast: Toast) {
    return toast.id;
  }

  dismiss(id: string) {
    this.notificationService.dismiss(id);
  }

  getContainerClasses(): string {
    return 'fixed bottom-5 right-5 w-full max-w-[420px] sm:max-w-[calc(100%-32px)] z-[1300] pointer-events-none sm:left-4 sm:right-4 sm:bottom-4 sm:w-auto';
  }

  getToastCardClasses(type: ToastType): string {
    return 'pointer-events-auto grid grid-cols-[auto_1fr_auto] items-center gap-3 p-3 px-4 bg-surface text-text-primary rounded-md border border-border-soft shadow-soft';
  }

  getToastIconClasses(type: ToastType): string {
    const baseClasses = 'w-8 h-8 rounded-sm inline-flex items-center justify-center text-text-inverse text-sm';
    
    const typeClasses: Record<ToastType, string> = {
      success: 'bg-success',
      error: 'bg-danger',
      warning: 'bg-warning text-text-primary',
      info: 'bg-info',
    };

    return `${baseClasses} ${typeClasses[type]}`;
  }
}
