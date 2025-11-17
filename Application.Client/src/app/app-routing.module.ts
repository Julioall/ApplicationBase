import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './page/auth/auth.component';
import { AuthGuard } from './service/auth/auth.guard';
import { RoleGuard } from './service/auth/role.guard';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', loadChildren: () => import('./page/home/home.module').then(m => m.HomeModule), canActivate: [AuthGuard, RoleGuard], data: { roles: ['User', 'Admin'] } },
  { path: 'auth', component: AuthComponent },
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
