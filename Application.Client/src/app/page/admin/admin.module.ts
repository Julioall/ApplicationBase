import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminUsersComponent } from './admin-users.component';
import { AdminServicesComponent } from './admin-services.component';
import { AdminUserDetailComponent } from './admin-user-detail.component';
import { AdminEmailSettingsComponent } from './admin-email-settings.component';

@NgModule({
  declarations: [AdminUsersComponent, AdminServicesComponent, AdminUserDetailComponent, AdminEmailSettingsComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AdminRoutingModule, TranslateModule]
})
export class AdminModule { }
