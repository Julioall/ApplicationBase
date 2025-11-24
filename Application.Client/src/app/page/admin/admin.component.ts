import { Component, OnInit } from '@angular/core';
import { User } from '../../model/User';
import { ALL_PERMISSIONS, DEFAULT_USER_PERMISSIONS } from '../../model/permissions';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss']
})
export class AdminComponent implements OnInit {
  users: User[] = [];
  availablePermissions: string[] = [];
  selectedPermission: Record<string, string> = {};
  loading = false;

  constructor(
    private userService: UserService,
    private notificationService: NotificationService,
  ) {}

  ngOnInit(): void {
    this.loadAvailablePermissions();
    this.loadUsers();
  }

  trackByUserId(_: number, user: User): string | undefined {
    return user.Id || user.Account?.Email;
  }

  getUserPermissions(user: User): string[] {
    return user.Account?.Permissions ?? [...DEFAULT_USER_PERMISSIONS];
  }

  addPermission(user: User): void {
    const userId = user.Id;
    const permission = this.selectedPermission[userId || ''];
    if (!userId || !permission) {
      return;
    }

    const currentPermissions = this.getUserPermissions(user);
    if (currentPermissions.includes(permission)) {
      this.notificationService.showWarning('Esta permissão já está atribuída.');
      return;
    }

    const updatedPermissions = [...currentPermissions, permission];
    this.persistPermissions(user, updatedPermissions, `Permissão ${permission} adicionada.`);
    this.selectedPermission[userId] = '';
  }

  removePermission(user: User, permission: string): void {
    if (!user.Id) {
      return;
    }

    const updatedPermissions = this.getUserPermissions(user).filter(p => p !== permission);
    this.persistPermissions(user, updatedPermissions, `Permissão ${permission} removida.`);
  }

  resetToDefault(user: User): void {
    this.persistPermissions(user, [...DEFAULT_USER_PERMISSIONS], 'Permissões padrões restauradas.');
  }

  private persistPermissions(user: User, permissions: string[], successMessage: string): void {
    if (!user.Id) {
      return;
    }

    this.loading = true;
    this.userService.updatePermissions(user.Id, permissions).subscribe({
      next: (response) => {
        user.Account = user.Account || {};
        user.Account.Permissions = permissions;
        this.notificationService.showSuccess(successMessage);
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        const message = err?.error?.detail || 'Não foi possível atualizar as permissões.';
        this.notificationService.showError(message);
        this.loading = false;
      },
    });
  }

  private loadUsers(): void {
    this.loading = true;
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        this.notificationService.showError('Não foi possível carregar os usuários.');
      },
    });
  }

  private loadAvailablePermissions(): void {
    this.userService.getAvailablePermissions().subscribe({
      next: (permissions) => {
        this.availablePermissions = permissions;
      },
      error: () => {
        this.availablePermissions = [...ALL_PERMISSIONS];
      },
    });
  }
}
