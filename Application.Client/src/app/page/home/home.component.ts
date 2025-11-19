import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../service/auth/auth.service';

type NavItem = {
  icon: string;
  label: string;
  badge?: string;
  active?: boolean;
};

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  token: string | null | undefined;
  isLoggedIn = false;
  isNavOpen = false;
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

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.token = this.authService.getToken();
    if (!this.isLoggedIn) {
      this.router.navigate(['/auth']);
    }
  }

  toggleNav(): void {
    this.isNavOpen = !this.isNavOpen;
  }

  closeNav(): void {
    this.isNavOpen = false;
  }
}
