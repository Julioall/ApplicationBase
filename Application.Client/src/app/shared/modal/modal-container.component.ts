import { ChangeDetectionStrategy, Component, HostListener } from '@angular/core';
import { Observable } from 'rxjs';
import { ModalInstance, ModalService } from './modal.service';

@Component({
  selector: 'app-modal-container',
  template: `
    <ng-container *ngIf="modals$ | async as modals">
      <div class="fixed inset-0 pointer-events-none" *ngIf="modals.length > 0">
        <ng-container *ngFor="let modal of modals; trackBy: trackById">
          <div class="absolute inset-0 pointer-events-none z-[1]">
            <div 
              class="absolute inset-0 pointer-events-auto z-[1]" 
              [style.background]="'radial-gradient(circle at 20% 20%, rgba(255, 255, 255, 0.05), transparent 45%), var(--overlay-strong)'"
              style="backdrop-filter: blur(3px)"
              (click)="onBackdropClick(modal)"
            ></div>

            <div
              [ngClass]="getModalDialogClasses()"
              role="dialog"
              [attr.aria-modal]="true"
              [attr.aria-labelledby]="'modal-title-' + modal.id"
              [attr.aria-describedby]="modal.message ? 'modal-description-' + modal.id : null"
              (click)="$event.stopPropagation()"
            >
              <div class="flex gap-3 items-start justify-start">
                <div [ngClass]="getModalIconClasses(modal)" *ngIf="modal.icon || modal.destructive">
                  <i [class]="modal.icon || 'fa-regular fa-circle-question'" aria-hidden="true"></i>
                </div>
                <div>
                  <h2 class="m-0 mb-1 text-lg font-medium tracking-tight" [id]="'modal-title-' + modal.id">{{ modal.title }}</h2>
                  <p
                    class="m-0 text-text-subtle leading-relaxed"
                    *ngIf="modal.message"
                    [id]="'modal-description-' + modal.id"
                  >
                    {{ modal.message }}
                  </p>
                </div>
              </div>

              <div class="flex justify-end gap-2 flex-wrap">
                <button
                  *ngIf="modal.showCancel !== false"
                  type="button"
                  class="btn ghost min-w-[120px]"
                  (click)="onCancel(modal)"
                >
                  {{ modal.cancelText || ('modal.cancel' | translate) }}
                </button>
                <button
                  type="button"
                  class="btn min-w-[120px]"
                  [ngClass]="modal.destructive ? 'danger' : 'primary'"
                  (click)="onConfirm(modal)"
                >
                  {{ modal.confirmText || ('modal.confirm' | translate) }}
                </button>
              </div>
            </div>
          </div>
        </ng-container>
      </div>
    </ng-container>
  `,
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

  getModalDialogClasses(): string {
    return 'absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 scale-98 bg-surface text-text-primary rounded-xl border border-border-soft p-6 w-full max-w-[520px] sm:max-w-[calc(100%-24px)] pointer-events-auto flex flex-col gap-4 z-[2] animate-modal-in shadow-[0_18px_60px_rgba(var(--shadow-rgb),0.18),0_0_0_1px_rgba(var(--shadow-rgb),0.04)] sm:p-4';
  }

  getModalIconClasses(modal: ModalInstance): string {
    const baseClasses = 'w-11 h-11 rounded-full flex-shrink-0 inline-flex items-center justify-center';
    
    if (modal.destructive) {
      return `${baseClasses} bg-danger/15 text-danger`;
    }
    
    return `${baseClasses} bg-accent-soft text-accent-strong`;
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
