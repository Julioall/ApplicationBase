import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const expectedRoles = route.data['roles'] as string[] | undefined;
    if (!expectedRoles || expectedRoles.length === 0) {
      return true;
    }

    const userRole = this.authService.getRole();
    if (userRole && expectedRoles.includes(userRole)) {
      return true;
    }

    return this.router.parseUrl('/auth');
  }
}
