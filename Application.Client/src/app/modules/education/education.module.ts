import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { EducationRoutingModule } from './education-routing.module';
import { EducationExplorerComponent } from './education-explorer.component';
import { EducationClassDetailComponent } from './education-class-detail.component';

@NgModule({
  declarations: [EducationExplorerComponent, EducationClassDetailComponent],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    EducationRoutingModule
  ]
})
export class EducationModule {}
