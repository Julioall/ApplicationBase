import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { TranslateModule } from '@ngx-translate/core';
import { CKEditorModule } from '@ckeditor/ckeditor5-angular';
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
    CKEditorModule,
    TodoRoutingModule,
  ],
})
export class TodoModule {}
