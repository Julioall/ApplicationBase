import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserDetailComponent } from './admin-user-detail.component';
import { AdminServicesComponent } from './admin-services.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'users' },
  { path: 'users', component: AdminUsersComponent },
  { path: 'users/:id', component: AdminUserDetailComponent },
  { path: 'services', component: AdminServicesComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
