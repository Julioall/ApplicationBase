import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../service/auth/auth.service';
import { ThemeService } from '../../service/theme/theme.service';

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
