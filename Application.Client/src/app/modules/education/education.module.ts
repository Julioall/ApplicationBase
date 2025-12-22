import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { EducationRoutingModule } from './education-routing.module';
import { EducationExplorerComponent } from './education-explorer.component';

@NgModule({
  declarations: [EducationExplorerComponent],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    EducationRoutingModule
  ]
})
export class EducationModule {}
