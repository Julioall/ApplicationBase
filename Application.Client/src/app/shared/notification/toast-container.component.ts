import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NotificationService, Toast, ToastType } from '../../service/notification/notification.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-toast-container',
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.scss'],
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
}
