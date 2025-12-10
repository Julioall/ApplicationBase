import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { StudentFormComponent } from './student-form.component';
import { StudentsService } from '../../service/students/students.service';
import { NotificationService } from '../../service/notification/notification.service';
import { Student } from '../../model/student';

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

describe('StudentFormComponent', () => {
  let component: StudentFormComponent;
  let fixture: ComponentFixture<StudentFormComponent>;
  let studentsService: jasmine.SpyObj<StudentsService>;
  let routerNavigate: jasmine.Spy;

  beforeEach(async () => {
    studentsService = jasmine.createSpyObj('StudentsService', ['createStudent', 'updateStudent', 'getStudent']);
    routerNavigate = jasmine.createSpy('navigate');

    await TestBed.configureTestingModule({
      declarations: [StudentFormComponent],
      imports: [
        ReactiveFormsModule,
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })
      ],
      providers: [
        FormBuilder,
        { provide: StudentsService, useValue: studentsService },
        { provide: NotificationService, useValue: { showError: jasmine.createSpy('showError'), showSuccess: jasmine.createSpy('showSuccess') } },
        { provide: Router, useValue: { navigate: routerNavigate } },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})) } },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create form with required controls', () => {
    expect(component.form.get('FirstName')).toBeTruthy();
    expect(component.form.get('LastName')).toBeTruthy();
  });

  it('should call createStudent on submit when valid', () => {
    studentsService.createStudent.and.returnValue(of({} as Student));
    component.form.patchValue({ FirstName: 'Ana', LastName: 'Silva', IsActive: true });

    component.onSubmit();

    expect(studentsService.createStudent).toHaveBeenCalled();
    expect(routerNavigate).toHaveBeenCalled();
  });

  it('should load student when route has id', () => {
    const student: Student = { Id: 'students-1', FirstName: 'Ana', LastName: 'Silva', IsActive: true };
    studentsService.getStudent.and.returnValue(of(student));
    const activatedRoute = TestBed.inject(ActivatedRoute) as any;
    activatedRoute.paramMap = of(convertToParamMap({ id: 'students-1' }));

    component.ngOnInit();

    expect(studentsService.getStudent).toHaveBeenCalledWith('students-1');
  });
});
