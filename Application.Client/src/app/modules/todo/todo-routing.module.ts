import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../service/auth/auth.guard';
import { PermissionGuard } from '../../service/auth/permission.guard';
import { TodoBoardComponent } from './todo-board.component';

const routes: Routes = [
  { path: '', component: TodoBoardComponent, canActivate: [AuthGuard, PermissionGuard], data: { permissions: ['view:todo'] } },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class TodoRoutingModule {}
