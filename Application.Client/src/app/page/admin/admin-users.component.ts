import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { User } from '../../model/user';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
})
export class AdminUsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  page = 1;
  pageSize = 10;

  constructor(
    private userService: UserService,
    private notificationService: NotificationService,
    private router: Router,
    private translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  trackByUserId(_: number, user: User): string | undefined {
    return user.Id || user.Account?.Email;
  }

  get paginatedUsers(): User[] {
    const start = (this.page - 1) * this.pageSize;
    return this.users.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.users.length / this.pageSize));
  }

  changePage(delta: number): void {
    this.page = Math.min(Math.max(1, this.page + delta), this.totalPages);
  }

  goToUser(user: User): void {
    if (!user.Id) {
      this.notificationService.showError(this.translate.instant('adminUsers.messages.missingId'));
      return;
    }
    this.router.navigate(['/admin/users', user.Id]);
  }

  private loadUsers(): void {
    this.loading = true;
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
        this.page = 1;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        const fallback = this.translate.instant('adminUsers.messages.loadError');
        const detail = err?.error?.detail || err?.error?.title || fallback;
        this.notificationService.showError(detail);
      },
    });
  }
}
