import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminUsersComponent } from './user.component';
import { AdminUserDetailComponent } from './user-detail.component';
import { AdminServicesComponent } from './services.component';
import { PermissionGuard } from '../../service/auth/permission.guard';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'services' },
  { path: 'users', component: AdminUsersComponent, canActivate: [PermissionGuard], data: { permissions: ['manage:users'] } },
  { path: 'users/:id', component: AdminUserDetailComponent, canActivate: [PermissionGuard], data: { permissions: ['manage:users'] } },
  { path: 'services', component: AdminServicesComponent, canActivate: [PermissionGuard], data: { permissions: ['manage:services'] } },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
