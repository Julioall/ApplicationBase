import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EducationExplorerComponent } from './education-explorer.component';
import { EducationClassDetailComponent } from './education-class-detail.component';
import { PermissionGuard } from '../../service/auth/permission.guard';
import { VIEW_EDUCATION_PERMISSION } from '../../model/permissions';

const routes: Routes = [
  {
    path: '',
    component: EducationExplorerComponent,
    canActivate: [PermissionGuard],
    data: { permissions: [VIEW_EDUCATION_PERMISSION] }
  },
  {
    path: 'classes/:id',
    component: EducationClassDetailComponent,
    canActivate: [PermissionGuard],
    data: { permissions: [VIEW_EDUCATION_PERMISSION] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EducationRoutingModule {}
