import { ChangeDetectionStrategy, Component, HostListener } from '@angular/core';
import { Observable } from 'rxjs';
import { ModalInstance, ModalService } from './modal.service';

@Component({
  selector: 'app-modal-container',
  templateUrl: './modal-container.component.html',
  styleUrls: ['./modal-container.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModalContainerComponent {
  modals$: Observable<ModalInstance[]> = this.modalService.modals$;

  constructor(private readonly modalService: ModalService) {}

  trackById(_: number, modal: ModalInstance): string {
    return modal.id;
  }

  onBackdropClick(modal: ModalInstance): void {
    if (modal.dismissible === false) {
      return;
    }
    this.modalService.close(modal.id, false);
  }

  onCancel(modal: ModalInstance): void {
    this.modalService.close(modal.id, false);
  }

  onConfirm(modal: ModalInstance): void {
    this.modalService.close(modal.id, true);
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: KeyboardEvent): void {
    const modals = this.modalService.snapshot;
    const active = modals[modals.length - 1];
    if (active && active.dismissible !== false) {
      event.preventDefault();
      this.modalService.close(active.id, false);
    }
  }
}
