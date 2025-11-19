import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './page/auth/auth.component';
import { AuthGuard } from './service/auth/auth.guard';
import { RoleGuard } from './service/auth/role.guard';
import { RegisterComponent } from './page/register/register.component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', loadChildren: () => import('./page/home/home.module').then(m => m.HomeModule), canActivate: [AuthGuard, RoleGuard], data: { roles: ['User', 'Admin'] } },
  { path: 'profile', loadChildren: () => import('./page/profile/profile.module').then(m => m.ProfileModule), canActivate: [AuthGuard, RoleGuard], data: { roles: ['User', 'Admin'] } },
  { path: 'auth', component: AuthComponent },
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
