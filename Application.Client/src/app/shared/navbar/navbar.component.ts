import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../service/auth/auth.service';
import { ThemeService } from '../../service/theme/theme.service';
import { ADMIN_PERMISSION, MANAGE_STUDENTS_PERMISSION, VIEW_STUDENTS_PERMISSION } from '../../model/permissions';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {

  constructor(private authService: AuthService, private router: Router, private themeService: ThemeService) { }

  ngOnInit() {
  }

  get canManageUsers(): boolean {
    return this.authService.hasPermission(ADMIN_PERMISSION);
  }

  get canAccessStudents(): boolean {
    return this.authService.hasAnyPermission([VIEW_STUDENTS_PERMISSION, MANAGE_STUDENTS_PERMISSION]);
  }

  navigateTo(path: string) {
    this.router.navigate([path]);
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  logOut() {
    this.authService.removeToken()
    this.router.navigate(['/auth']);
  }
}
