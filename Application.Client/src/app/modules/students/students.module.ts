import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { StudentsRoutingModule } from './students-routing.module';
import { StudentsListComponent } from './students-list.component';
import { StudentFormComponent } from './student-form.component';

@NgModule({
  declarations: [StudentsListComponent, StudentFormComponent],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    StudentsRoutingModule
  ]
})
export class StudentsModule { }
