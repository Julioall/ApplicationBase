import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AuthService } from './service/auth/auth.service';
import { ThemeService } from './service/theme/theme.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  host: {
    'class': 'block bg-page text-text-primary'
  }
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Application Base';
  shouldShowDashboardShell = false;
  private routerSubscription?: Subscription;

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

  private updateShellVisibility(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);
    const isPublicRoute = normalizedUrl.startsWith('/auth') || normalizedUrl.startsWith('/register');
    this.shouldShowDashboardShell = this.authService.isLoggedIn() && !isPublicRoute;
  }

  private normalizeUrl(url: string): string {
    if (!url) {
      return '';
    }
    const [pathname] = url.split('?');
    return pathname || '';
  }
}
