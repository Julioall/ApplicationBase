import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from './service/auth/auth.service';
import { Subscription } from 'rxjs';
import { ThemeService } from './service/theme/theme.service';

type NavItem = {
  icon: string;
  label: string;
  badge?: string;
  active?: boolean;
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
  primaryNav: NavItem[] = [
    { icon: 'fa-solid fa-compass', label: 'home.primaryNav.overview', active: true },
    { icon: 'fa-solid fa-list-check', label: 'home.primaryNav.projects' },
    { icon: 'fa-solid fa-table-columns', label: 'home.primaryNav.boards' },
    { icon: 'fa-solid fa-users', label: 'home.primaryNav.teams' },
    { icon: 'fa-solid fa-chart-simple', label: 'home.primaryNav.reports' },
    { icon: 'fa-solid fa-robot', label: 'home.primaryNav.automation' },
  ];
  favoriteNav: NavItem[] = [
    { icon: 'fa-regular fa-star', label: 'home.favoriteNav.designSystem', badge: 'home.badges.ui' },
    { icon: 'fa-regular fa-star', label: 'home.favoriteNav.mobileApp', badge: 'home.badges.sprint' },
    { icon: 'fa-regular fa-star', label: 'home.favoriteNav.serviceDesk', badge: 'home.badges.support' },
  ];
  quickLinks: NavItem[] = [
    { icon: 'fa-regular fa-note-sticky', label: 'home.quickLinks.docs' },
    { icon: 'fa-solid fa-bolt', label: 'home.quickLinks.automation' },
    { icon: 'fa-solid fa-flag', label: 'home.quickLinks.roadmap' },
  ];
  private routerSubscription?: Subscription;

  @ViewChild('profileMenu') profileMenu?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly translateService: TranslateService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly themeService: ThemeService,
  ) {}

  ngOnInit(): void {
    const browserLang = this.translateService.getBrowserLang();
    this.translateService.setFallbackLang('en');
    this.translateService.use(browserLang ?? 'en');
    this.themeService.setTheme(this.themeService.getActiveTheme());
    this.updateShellVisibility(this.router.url);
    this.routerSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        const currentUrl = event.urlAfterRedirects ?? event.url ?? '';
        this.updateShellVisibility(currentUrl);
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
    this.router.navigate(['/auth']);
  }

  get userInitials(): string {
    return 'JA';
  }

  private updateShellVisibility(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);
    const isProfileRoute = normalizedUrl.startsWith('/profile');
    const isPublicRoute = normalizedUrl.startsWith('/auth') || normalizedUrl.startsWith('/register');
    const shouldShowShell = this.authService.isLoggedIn() && !isPublicRoute;
    this.shouldShowDashboardShell = shouldShowShell;
    if (!shouldShowShell) {
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
}
