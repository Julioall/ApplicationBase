import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { StudentsListComponent } from './students-list.component';
import { StudentFormComponent } from './student-form.component';
import { PermissionGuard } from '../../service/auth/permission.guard';

const routes: Routes = [
  { path: '', component: StudentsListComponent, canActivate: [PermissionGuard], data: { permissions: ['view:students'] } },
  { path: 'new', component: StudentFormComponent, canActivate: [PermissionGuard], data: { permissions: ['manage:students'] } },
  { path: ':id/edit', component: StudentFormComponent, canActivate: [PermissionGuard], data: { permissions: ['manage:students'] } },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class StudentsRoutingModule { }
