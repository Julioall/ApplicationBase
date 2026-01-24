import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminUsersComponent } from './user.component';
import { AdminServicesComponent } from './services.component';
import { AdminUserDetailComponent } from './user-detail.component';
import { AdminEmailSettingsComponent } from './email.component';

@NgModule({
  declarations: [AdminUsersComponent, AdminServicesComponent, AdminUserDetailComponent, AdminEmailSettingsComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AdminRoutingModule, TranslateModule]
})
export class AdminModule { }
