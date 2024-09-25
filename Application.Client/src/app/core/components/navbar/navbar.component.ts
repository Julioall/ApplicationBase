import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../../core/service/auth/auth.service';
import { Router } from '@angular/router';
import { ThemeService } from '../../../core/service/theme/theme.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {

  constructor(private authService: AuthService, private router: Router, private themeService: ThemeService) { }

  ngOnInit() {
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  logOut() {
    this.authService.removeToken()
    this.router.navigate(['/auth']);
  }
}
