import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PermissionGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const expectedPermissions = route.data['permissions'] as string[] | undefined;
    if (!expectedPermissions || expectedPermissions.length === 0) {
      return true;
    }

    if (this.authService.hasAnyPermission(expectedPermissions)) {
      return true;
    }

    return this.router.parseUrl('/auth');
  }
}
