import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
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
    private readonly translate: TranslateService,
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
      this.notificationService.showWarning(this.translate.instant('adminUserDetail.messages.permissionAlready'));
      return;
    }
    const updatedPermissions = [...currentPermissions, this.selectedPermission];
    this.persistPermissions(
      updatedPermissions,
      'adminUserDetail.messages.permissionAdded',
      { permission: this.selectedPermission },
    );
    this.selectedPermission = '';
  }

  removePermission(permission: string): void {
    if (!this.user?.Id) {
      return;
    }
    const updatedPermissions = this.getUserPermissions().filter((p) => p !== permission);
    this.persistPermissions(
      updatedPermissions,
      'adminUserDetail.messages.permissionRemoved',
      { permission },
    );
  }

  resetToDefault(): void {
    this.persistPermissions([...DEFAULT_USER_PERMISSIONS], 'adminUserDetail.messages.resetDefault');
  }

  goBack(): void {
    this.router.navigate(['/admin/users']);
  }

  showResetPassword(): void {
    this.notificationService.showWarning(this.translate.instant('adminUserDetail.messages.resetPassword'));
  }

  private loadUser(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.notificationService.showError(this.translate.instant('adminUserDetail.messages.notFound'));
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
        const fallback = this.translate.instant('adminUserDetail.messages.loadError');
        const detail = err?.error?.detail || err?.error?.title || fallback;
        this.notificationService.showError(detail);
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

  private persistPermissions(
    permissions: string[],
    successKey: string,
    successParams?: Record<string, unknown>,
  ): void {
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
        this.notificationService.showSuccess(this.translate.instant(successKey, successParams));
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        const fallback = this.translate.instant('adminUserDetail.messages.updateError');
        const message = err?.error?.detail || err?.error?.title || fallback;
        this.notificationService.showError(message);
        this.loading = false;
      },
    });
  }
}
