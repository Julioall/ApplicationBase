import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminEmailSettingsComponent } from './admin-email-settings.component';
import { AdminServicesComponent } from './admin-services.component';
import { AdminUserDetailComponent } from './admin-user-detail.component';

@NgModule({
  declarations: [AdminUsersComponent, AdminEmailSettingsComponent, AdminServicesComponent, AdminUserDetailComponent],
  imports: [CommonModule, FormsModule, AdminRoutingModule]
})
export class AdminModule { }
