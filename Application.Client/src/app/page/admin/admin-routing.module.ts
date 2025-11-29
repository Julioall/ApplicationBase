import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminUsersComponent } from './admin-users.component';
import { AdminUserDetailComponent } from './admin-user-detail.component';
import { AdminEmailSettingsComponent } from './admin-email-settings.component';
import { AdminServicesComponent } from './admin-services.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'permissions' },
  { path: 'permissions', component: AdminUsersComponent },
  { path: 'permissions/:id', component: AdminUserDetailComponent },
  { path: 'email', component: AdminEmailSettingsComponent },
  { path: 'services', component: AdminServicesComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
