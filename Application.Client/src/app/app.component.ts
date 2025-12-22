import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from './service/auth/auth.service';
import { Subscription, interval } from 'rxjs';
import { ThemeService } from './service/theme/theme.service';
import { UserService } from './service/user/user.service';
import { User } from './model/User';
import { ADMIN_PERMISSION, MANAGE_STUDENTS_PERMISSION, MANAGE_EDUCATION_PERMISSION, VIEW_STUDENTS_PERMISSION, VIEW_EDUCATION_PERMISSION } from './model/permissions';
import { Notification as AppNotification } from './model/notification';
import { NotificationApiService } from './service/notification/notification-api.service';

type NavItem = {
  icon: string;
  label: string;
  badge?: string;
  path?: string;
};

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Application Base';
  isNavOpen = false;
  isProfileMenuOpen = false;
  shouldShowDashboardShell = false;
  userAvatarUrl: string | null = null;
  private userInitialsValue = 'AB';
  sectionState: Record<'admin', boolean> = { admin: false };
  primaryNav: NavItem[] = [
    { icon: 'fa-solid fa-compass', label: 'home.primaryNav.panel', path: '/home' },
    { icon: 'fa-solid fa-people-group', label: 'home.primaryNav.classes' },
    { icon: 'fa-solid fa-signal', label: 'home.primaryNav.metrics' },
    { icon: 'fa-solid fa-clipboard-check', label: 'home.primaryNav.todo' },
  ];
  favoriteNav: NavItem[] = [
    { icon: 'fa-regular fa-file-lines', label: 'home.favoriteNav.reports' },
  ];
  adminShortcuts: NavItem[] = [
    { icon: 'fa-solid fa-user-shield', label: 'home.adminNav.users', path: '/admin/users' },
    { icon: 'fa-solid fa-gears', label: 'home.adminNav.services', path: '/admin/services' },
    { icon: 'fa-solid fa-user-graduate', label: 'navbar.students', path: '/students' },
    { icon: 'fa-solid fa-school', label: 'navbar.education', path: '/education' },
    { icon: 'fa-solid fa-life-ring', label: 'home.adminNav.support', path: '/support' },
  ];
  notifications: AppNotification[] = [];
  unreadNotifications = 0;
  isNotificationsOpen = false;
  isLoadingNotifications = false;
  isMarkingNotifications = false;
  private notificationPolling?: Subscription;
  private routerSubscription?: Subscription;
  private hasLoadedUser = false;
  private isFetchingUser = false;
  private readonly defaultInitials = 'AB';
  hasAdminAccess = false;

  @ViewChild('profileMenu') profileMenu?: ElementRef<HTMLDivElement>;
  @ViewChild('notificationsMenu') notificationsMenu?: ElementRef<HTMLDivElement>;
  @ViewChild('notificationsTrigger') notificationsTrigger?: ElementRef<HTMLButtonElement>;

  constructor(
    private readonly translateService: TranslateService,
  private readonly authService: AuthService,
  private readonly router: Router,
  private readonly themeService: ThemeService,
  private readonly userService: UserService,
  private readonly notificationApiService: NotificationApiService,
  ) {}

  ngOnInit(): void {
    const browserLang = this.translateService.getBrowserLang();
    this.translateService.setFallbackLang('en');
    this.translateService.use(browserLang ?? 'en');
    this.themeService.setTheme(this.themeService.getActiveTheme());
    this.refreshAdminAccess();
    this.updateShellVisibility(this.router.url);
    if (this.authService.isLoggedIn()) {
      this.ensureUserContext();
    } else {
      this.resetUserMetadata();
    }
    this.routerSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        const currentUrl = event.urlAfterRedirects ?? event.url ?? '';
        this.updateShellVisibility(currentUrl);
      }
    });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
    this.notificationPolling?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement | null;
    if (this.isProfileMenuOpen && this.profileMenu && target && !this.profileMenu.nativeElement.contains(target)) {
      this.isProfileMenuOpen = false;
    }

    const clickedNotificationArea =
      (this.notificationsMenu && target && this.notificationsMenu.nativeElement.contains(target)) ||
      (this.notificationsTrigger && target && this.notificationsTrigger.nativeElement.contains(target));
    if (this.isNotificationsOpen && !clickedNotificationArea) {
      this.isNotificationsOpen = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isProfileMenuOpen = false;
    this.isNotificationsOpen = false;
  }

  toggleNav(): void {
    this.isNavOpen = !this.isNavOpen;
  }

  closeNav(): void {
    this.isNavOpen = false;
  }

  onNavClick(link: NavItem, event: Event): void {
    event.preventDefault();
    if (link.path) {
      this.router.navigate([link.path]);
    }
    this.closeNav();
  }

  toggleSection(section: 'admin'): void {
    const willOpen = !this.sectionState[section];
    this.sectionState.admin = willOpen;
  }

  toggleProfileMenu(event: Event): void {
    event.stopPropagation();
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  toggleNotifications(event: Event): void {
    event.stopPropagation();
    if (!this.authService.isLoggedIn()) {
      return;
    }
    this.isNotificationsOpen = !this.isNotificationsOpen;
    if (this.isNotificationsOpen) {
      this.refreshNotifications();
    }
  }

  closeProfileMenu(): void {
    this.isProfileMenuOpen = false;
  }

  navigateToProfile(): void {
    this.closeProfileMenu();
    this.router.navigate(['/profile']);
  }

  logout(): void {
    this.authService.logout();
    this.closeProfileMenu();
    this.refreshAdminAccess();
    this.resetUserMetadata();
    this.stopNotificationPolling();
    this.router.navigate(['/auth']);
  }

  get userInitials(): string {
    return this.userInitialsValue;
  }

  get canAccessStudents(): boolean {
    return this.authService.hasAnyPermission([VIEW_STUDENTS_PERMISSION, MANAGE_STUDENTS_PERMISSION]);
  }

  get canAccessEducation(): boolean {
    return this.authService.hasAnyPermission([VIEW_EDUCATION_PERMISSION, MANAGE_EDUCATION_PERMISSION]);
  }

  isRouteActive(path?: string): boolean {
    if (!path) {
      return false;
    }
    return this.router.url.startsWith(path);
  }

  private updateShellVisibility(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);
    const isProfileRoute = normalizedUrl.startsWith('/profile');
    const isPublicRoute = normalizedUrl.startsWith('/auth') || normalizedUrl.startsWith('/register');
    const shouldShowShell = this.authService.isLoggedIn() && !isPublicRoute;
    this.refreshAdminAccess();
    this.shouldShowDashboardShell = shouldShowShell;
    if (shouldShowShell) {
      this.ensureUserContext();
      this.startNotificationPolling();
    } else {
      if (isProfileRoute) {
        this.hasLoadedUser = false;
      }
      this.closeNav();
      this.closeProfileMenu();
      this.stopNotificationPolling();
    }
  }

  private normalizeUrl(url: string): string {
    if (!url) {
      return '';
    }
    const [pathname] = url.split('?');
    return pathname || '';
  }

  private ensureUserContext(): void {
    if (!this.authService.isLoggedIn() || this.isFetchingUser) {
      return;
    }
    if (this.hasLoadedUser) {
      return;
    }
    this.isFetchingUser = true;
    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.applyUserMetadata(user);
        this.hasLoadedUser = true;
        this.isFetchingUser = false;
      },
      error: () => {
        this.resetUserMetadata();
        this.isFetchingUser = false;
      },
    });
  }

  private applyUserMetadata(user: User): void {
    const name = user.Profile?.Name ?? '';
    const email = user.Account?.Email ?? '';
    const avatarUrl = user.Profile?.ProfilePictureUrl?.trim() || null;
    this.userAvatarUrl = avatarUrl;
    this.userInitialsValue = this.resolveInitials(name, email);
  }

  private resolveInitials(name: string, email: string): string {
    const fromName = this.extractInitials(name);
    if (fromName) {
      return fromName;
    }
    const emailLocal = email?.split('@')[0] ?? '';
    const fromEmail = this.extractInitials(emailLocal);
    return fromEmail || this.defaultInitials;
  }

  private extractInitials(source: string): string {
    if (!source) {
      return '';
    }
    const trimmed = source.trim();
    if (!trimmed) {
      return '';
    }
    const parts = trimmed.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }
    const sanitized = trimmed.replace(/[^A-Za-z0-9]/g, '');
    if (sanitized.length >= 2) {
      return sanitized.slice(0, 2).toUpperCase();
    }
    if (sanitized.length === 1) {
      return `${sanitized}${sanitized}`.toUpperCase();
    }
    return '';
  }

  private resetUserMetadata(): void {
    this.userAvatarUrl = null;
    this.userInitialsValue = this.defaultInitials;
    this.hasLoadedUser = false;
    this.isFetchingUser = false;
    this.notifications = [];
    this.unreadNotifications = 0;
    this.isNotificationsOpen = false;
  }

  private refreshAdminAccess(): void {
    this.hasAdminAccess = this.authService.hasPermission(ADMIN_PERMISSION);
  }

  isAdminShortcutVisible(link: NavItem): boolean {
    if (link.path === '/students') {
      return this.canAccessStudents;
    }
    if (link.path === '/education') {
      return this.canAccessEducation;
    }
    return true;
  }

  private startNotificationPolling(): void {
    if (!this.authService.isLoggedIn()) {
      return;
    }
    this.stopNotificationPolling();
    this.refreshNotifications(false);
    this.notificationPolling = interval(20000).subscribe(() => this.refreshNotifications(false));
  }

  private stopNotificationPolling(): void {
    this.notificationPolling?.unsubscribe();
    this.notificationPolling = undefined;
  }

  private refreshNotifications(startLoading: boolean = true): void {
    if (!this.authService.isLoggedIn()) {
      return;
    }
    this.isLoadingNotifications = startLoading;
    this.notificationApiService.getLatest(12).subscribe({
      next: (result) => {
        this.notifications = result?.Items ?? [];
        this.unreadNotifications = result?.UnreadCount ?? 0;
        this.isLoadingNotifications = false;
      },
      error: () => {
        this.isLoadingNotifications = false;
      },
    });
  }

  markNotificationAsRead(notification: AppNotification, event?: Event): void {
    event?.stopPropagation();
    if (!notification || notification.IsRead || !notification.Id) {
      return;
    }

    this.notificationApiService.markAsRead(notification.Id).subscribe({
      next: () => {
        notification.IsRead = true;
        this.unreadNotifications = Math.max(0, this.unreadNotifications - 1);
      },
      error: () => {},
    });
  }

  markAllNotificationsAsRead(): void {
    const unreadIds = this.notifications.filter(n => !n.IsRead && !!n.Id).map(n => n.Id);
    if (unreadIds.length === 0) {
      return;
    }
    this.isMarkingNotifications = true;
    this.notificationApiService.markManyAsRead(unreadIds).subscribe({
      next: () => {
        this.notifications = this.notifications.map(n => ({ ...n, IsRead: true }));
        this.unreadNotifications = 0;
        this.isMarkingNotifications = false;
      },
      error: () => {
        this.isMarkingNotifications = false;
      },
    });
  }

  handleNotificationClick(notification: AppNotification): void {
    if (!notification) {
      return;
    }
    this.markNotificationAsRead(notification);
    if (notification.Link) {
      this.navigateToLink(notification.Link);
    }
  }

  deleteNotification(notification: AppNotification, event?: Event): void {
    event?.stopPropagation();
    if (!notification?.Id) {
      return;
    }

    const id = notification.Id;
    this.notifications = this.notifications.filter(n => n.Id !== id);
    if (!notification.IsRead && this.unreadNotifications > 0) {
      this.unreadNotifications -= 1;
    }

    this.notificationApiService.delete(id).subscribe({
      error: () => {
        // best-effort delete; if it fails, just refresh list next poll
      },
    });
  }

  trackByNotification(index: number, notification: AppNotification): string {
    return notification?.Id ?? index.toString();
  }

  private navigateToLink(link: string): void {
    if (!link) {
      return;
    }
    if (/^https?:\/\//i.test(link)) {
      window.open(link, '_blank', 'noreferrer');
    } else {
      this.router.navigateByUrl(link);
    }
    this.isNotificationsOpen = false;
  }
}
