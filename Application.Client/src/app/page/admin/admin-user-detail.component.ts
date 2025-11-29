import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { User } from '../../model/User';
import { ALL_PERMISSIONS, DEFAULT_USER_PERMISSIONS } from '../../model/permissions';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';

@Component({
  selector: 'app-admin-user-detail',
  templateUrl: './admin-user-detail.component.html',
  styleUrls: ['./admin-user-detail.component.scss'],
})
export class AdminUserDetailComponent implements OnInit {
  user?: User;
  availablePermissions: string[] = [];
  selectedPermission = '';
  loading = false;

  get avatarUrl(): string | null {
    return this.user?.Profile?.ProfilePictureUrl?.trim() || null;
  }

  get userInitials(): string {
    const name = this.user?.Profile?.Name?.trim();
    if (name) {
      const parts = name.split(' ').filter(Boolean);
      if (parts.length >= 2) {
        return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
      }
      return parts[0][0]?.toUpperCase() || 'U';
    }

    const email = this.user?.Account?.Email ?? '';
    return email ? email.substring(0, 2).toUpperCase() : 'U';
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly userService: UserService,
    private readonly notificationService: NotificationService,
  ) {}

  ngOnInit(): void {
    this.loadAvailablePermissions();
    this.loadUser();
  }

  getUserPermissions(): string[] {
    return this.user?.Account?.Permissions ?? [...DEFAULT_USER_PERMISSIONS];
  }

  addPermission(): void {
    if (!this.user?.Id || !this.selectedPermission) {
      return;
    }
    const currentPermissions = this.getUserPermissions();
    if (currentPermissions.includes(this.selectedPermission)) {
      this.notificationService.showWarning('Esta permissão já está atribuída.');
      return;
    }
    const updatedPermissions = [...currentPermissions, this.selectedPermission];
    this.persistPermissions(updatedPermissions, `Permissão ${this.selectedPermission} adicionada.`);
    this.selectedPermission = '';
  }

  removePermission(permission: string): void {
    if (!this.user?.Id) {
      return;
    }
    const updatedPermissions = this.getUserPermissions().filter((p) => p !== permission);
    this.persistPermissions(updatedPermissions, `Permissão ${permission} removida.`);
  }

  resetToDefault(): void {
    this.persistPermissions([...DEFAULT_USER_PERMISSIONS], 'Permissões padrão restauradas.');
  }

  goBack(): void {
    this.router.navigate(['/admin/permissions']);
  }

  showResetPassword(): void {
    this.notificationService.showWarning('Reset de senha deverá ser integrado ao backend.');
  }

  private loadUser(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.notificationService.showError('Usuário não encontrado.');
      return;
    }
    this.loading = true;
    this.userService.getUserById(id).subscribe({
      next: (user) => {
        this.user = user;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.notificationService.showError('Não foi possível carregar o usuário.');
        this.loading = false;
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

  private persistPermissions(permissions: string[], successMessage: string): void {
    if (!this.user?.Id) {
      return;
    }
    this.loading = true;
    this.userService.updatePermissions(this.user.Id, permissions).subscribe({
      next: () => {
        if (!this.user) {
          return;
        }
        this.user.Account = this.user.Account || {};
        this.user.Account.Permissions = permissions;
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
}
