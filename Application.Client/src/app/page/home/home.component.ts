import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/service/auth/auth.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  token: string | null | undefined;
  isLoggedIn = false;

  constructor(
    private authService: AuthService,
    private router: Router

  ) {}

  ngOnInit() {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.token = this.authService.getToken();
    if (!this.isLoggedIn) {
      this.router.navigate(['/auth']);
    }
  }
}
