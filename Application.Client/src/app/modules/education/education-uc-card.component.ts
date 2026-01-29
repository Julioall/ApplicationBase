import { Component, Input } from '@angular/core';
import { CourseUnit } from '../../model/course-unit';

@Component({
  selector: 'app-education-uc-card',
  templateUrl: './education-uc-card.component.html',
  styleUrls: ['./education-uc-card.component.scss']
})
export class EducationUcCardComponent {
  @Input() uc!: CourseUnit;
}
