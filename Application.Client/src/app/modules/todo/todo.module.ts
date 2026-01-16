import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { TranslateModule } from '@ngx-translate/core';
import { QuillModule } from 'ngx-quill';
import { TodoRoutingModule } from './todo-routing.module';
import { TodoBoardComponent } from './todo-board.component';
import { TodoStepsChecklistComponent } from './todo-steps-checklist.component';
import { TodoTaskDetailComponent } from './todo-task-detail.component';
import { TodoAgendaComponent } from './todo-agenda.component';

@NgModule({
  declarations: [
    TodoBoardComponent,
    TodoStepsChecklistComponent,
    TodoTaskDetailComponent,
    TodoAgendaComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    DragDropModule,
    TranslateModule,
    QuillModule.forRoot(),
    TodoRoutingModule,
  ],
})
export class TodoModule {}
