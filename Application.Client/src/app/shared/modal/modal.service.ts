import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ModalConfig {
  title: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
  icon?: string;
  destructive?: boolean;
  dismissible?: boolean;
  showCancel?: boolean;
}

export interface ModalInstance extends ModalConfig {
  id: string;
  resolve: (result: boolean) => void;
}

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private readonly modalsSubject = new BehaviorSubject<ModalInstance[]>([]);
  readonly modals$ = this.modalsSubject.asObservable();

  get snapshot(): ModalInstance[] {
    return this.modalsSubject.value;
  }

  confirm(config: ModalConfig): Promise<boolean> {
    const id = this.generateId();

    return new Promise<boolean>((resolve) => {
      const modal: ModalInstance = {
        dismissible: true,
        showCancel: true,
        ...config,
        id,
        resolve
      };

      this.modalsSubject.next([...this.snapshot, modal]);
    });
  }

  close(id: string, result = false): void {
    const modal = this.snapshot.find((m) => m.id === id);
    if (!modal) {
      return;
    }

    modal.resolve(result);
    this.remove(id);
  }

  closeAll(): void {
    this.snapshot.forEach((modal) => modal.resolve(false));
    this.modalsSubject.next([]);
  }

  private remove(id: string): void {
    this.modalsSubject.next(this.snapshot.filter((modal) => modal.id !== id));
  }

  private generateId(): string {
    return `modal-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }
}
