import { Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  constructor(private toastr: ToastrService) {}

  showSuccess(message: string, title: string = 'Success') {
    this.toastr.success(message, title, {
      closeButton: true, // Exibir botão de fechar
      timeOut: 3000, // Tempo de exibição (em milissegundos)
      positionClass: 'toast-top-right' // Posição da notificação
    });
  }

  showError(message: string, title: string = 'Error') {
    this.toastr.error(message, title, {
      closeButton: true,
      timeOut: 2000,
      progressBar: true,
      positionClass: 'toast-top-right'
    });
  }

  showWarning(message: string, title: string = 'Warning') {
    this.toastr.warning(message, title, {
      closeButton: true,
      timeOut: 1000,
      progressBar: true,
      positionClass: 'toast-top-right'
    });
  }
}
