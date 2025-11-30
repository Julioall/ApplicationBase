import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from './service/auth/auth.service';
import { Subscription } from 'rxjs';
import { ThemeService } from './service/theme/theme.service';
import { UserService } from './service/user/user.service';
import { User } from './model/User';
import { ADMIN_PERMISSION } from './model/permissions';

type NavItem = {
  icon: string;
  label: string;
  badge?: string;
  active?: boolean;
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
  breadcrumbLabelKey = 'home.dashboard';
  breadcrumbIconClass = 'fa-solid fa-house';
  userAvatarUrl: string | null = null;
  private userInitialsValue = 'AB';
  sectionState: Record<'admin', boolean> = { admin: false };
  primaryNav: NavItem[] = [
    { icon: 'fa-solid fa-compass', label: 'home.primaryNav.panel', active: true },
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
    { icon: 'fa-solid fa-life-ring', label: 'home.adminNav.support', path: '/support' },
  ];
  private readonly routeBreadcrumbMap: Record<string, { label: string; icon: string }> = {
    home: { label: 'home.dashboard', icon: 'fa-solid fa-house' },
    profile: { label: 'profile.pageTitle', icon: 'fa-regular fa-user' },
    admin: { label: 'admin.pageTitle', icon: 'fa-solid fa-user-shield' },
  };
  private routerSubscription?: Subscription;
  private hasLoadedUser = false;
  private isFetchingUser = false;
  private readonly defaultInitials = 'AB';
  hasAdminAccess = false;

  @ViewChild('profileMenu') profileMenu?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly translateService: TranslateService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly themeService: ThemeService,
    private readonly userService: UserService,
  ) {}

  ngOnInit(): void {
    const browserLang = this.translateService.getBrowserLang();
    this.translateService.setFallbackLang('en');
    this.translateService.use(browserLang ?? 'en');
    this.themeService.setTheme(this.themeService.getActiveTheme());
    this.refreshAdminAccess();
    this.updateShellVisibility(this.router.url);
    this.updateBreadcrumb(this.router.url);
    if (this.authService.isLoggedIn()) {
      this.ensureUserContext();
    } else {
      this.resetUserMetadata();
    }
    this.routerSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        const currentUrl = event.urlAfterRedirects ?? event.url ?? '';
        this.updateShellVisibility(currentUrl);
        this.updateBreadcrumb(currentUrl);
      }
    });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.isProfileMenuOpen) {
      return;
    }
    const target = event.target as HTMLElement | null;
    if (this.profileMenu && target && !this.profileMenu.nativeElement.contains(target)) {
      this.isProfileMenuOpen = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isProfileMenuOpen = false;
  }

  toggleNav(): void {
    this.isNavOpen = !this.isNavOpen;
  }

  closeNav(): void {
    this.isNavOpen = false;
  }

  toggleSection(section: 'admin'): void {
    const willOpen = !this.sectionState[section];
    this.sectionState.admin = willOpen;
  }

  toggleProfileMenu(event: Event): void {
    event.stopPropagation();
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
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
    this.router.navigate(['/auth']);
  }

  get userInitials(): string {
    return this.userInitialsValue;
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
    } else {
      if (isProfileRoute) {
        this.hasLoadedUser = false;
      }
      this.closeNav();
      this.closeProfileMenu();
    }
  }

  private normalizeUrl(url: string): string {
    if (!url) {
      return '';
    }
    const [pathname] = url.split('?');
    return pathname || '';
  }

  private updateBreadcrumb(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);
    const [firstSegment = 'home', secondSegment] = normalizedUrl.split('/').filter(Boolean);

    if (firstSegment === 'admin') {
      this.setPrimaryActive(normalizedUrl, true);
      this.breadcrumbIconClass = 'fa-solid fa-user-shield';
      if (secondSegment === 'users') {
        this.breadcrumbLabelKey = 'home.breadcrumbs.adminUsers';
        return;
      }
      this.breadcrumbLabelKey = 'admin.pageTitle';
      return;
    }

    this.setPrimaryActive(normalizedUrl, false);
    const routeMeta = this.routeBreadcrumbMap[firstSegment];
    if (routeMeta) {
      this.breadcrumbLabelKey = routeMeta.label;
      this.breadcrumbIconClass = routeMeta.icon;
      return;
    }

    this.breadcrumbLabelKey = 'home.dashboard';
    this.breadcrumbIconClass = 'fa-solid fa-house';
  }

  private setPrimaryActive(normalizedUrl: string, isAdminRoute: boolean): void {
    this.primaryNav.forEach((item) => (item.active = false));
    if (isAdminRoute) {
      return;
    }
    if (!normalizedUrl || normalizedUrl === '/' || normalizedUrl.startsWith('/home')) {
      const first = this.primaryNav[0];
      if (first) {
        first.active = true;
      }
    }
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
  }

  private refreshAdminAccess(): void {
    this.hasAdminAccess = this.authService.hasPermission(ADMIN_PERMISSION);
  }
}
