import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './page/auth/auth.component';
import { AuthGuard } from './service/auth/auth.guard';
import { PermissionGuard } from './service/auth/permission.guard';
import { RegisterComponent } from './page/register/register.component';
import { ForgotPasswordComponent } from './page/auth/forgot-password.component';
import { ResetPasswordComponent } from './page/auth/reset-password.component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', loadChildren: () => import('./page/home/home.module').then(m => m.HomeModule), canActivate: [AuthGuard, PermissionGuard], data: { permissions: ['view:home'] } },
  { path: 'profile', loadChildren: () => import('./page/profile/profile.module').then(m => m.ProfileModule), canActivate: [AuthGuard, PermissionGuard], data: { permissions: ['view:profile'] } },
  { path: 'todo', loadChildren: () => import('./modules/todo/todo.module').then(m => m.TodoModule), canActivate: [AuthGuard, PermissionGuard], data: { permissions: ['view:todo'] } },
  { path: 'admin', loadChildren: () => import('./page/admin/admin.module').then(m => m.AdminModule), canActivate: [AuthGuard, PermissionGuard], data: { permissions: ['manage:services', 'manage:users'] } },
  { path: 'auth', component: AuthComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'register', component: RegisterComponent },
];


@NgModule({
  imports: [
    RouterModule.forRoot(routes),
  ],
  exports: [
    RouterModule
  ]
})
export class AppRoutingModule { }
