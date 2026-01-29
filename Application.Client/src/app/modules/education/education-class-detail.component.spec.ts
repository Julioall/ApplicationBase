import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { EducationClassDetailComponent } from './education-class-detail.component';
import { EducationService } from '../../service/education/education.service';
import { NotificationService } from '../../service/notification/notification.service';
import { Location } from '@angular/common';
import { Student } from '../../model/student';
import { CourseUnit } from '../../model/course-unit';

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

class EducationServiceStub {
    getClassCourseUnits = jasmine.createSpy('getClassCourseUnits').and.returnValue(of([
    {
      Id: 'ucs/27535',
      MoodleId: 27535,
      Fullname: 'Logica de Programacao',
      StartDate: 1706745600,
      EndDate: 1709251200
    } as CourseUnit
  ]));
  getCourseUnitStudents = jasmine.createSpy('getCourseUnitStudents').and.returnValue(of([
    { Id: 'students-1', FirstName: 'Ana', LastName: 'Silva', Email: 'ana@test.com', Phone: '123', IsActive: true } as Student
  ]));
  getClasses = jasmine.createSpy('getClasses').and.returnValue(of([
    { Id: 'class-1', Name: 'Turma 1', ProgramId: 'program-1', SchoolId: 'school-1', StartDate: 1, EndDate: 2 }
  ]));
  getSchools = jasmine.createSpy('getSchools').and.returnValue(of([]));
  getPrograms = jasmine.createSpy('getPrograms').and.returnValue(of([]));
}

class NotificationStub {
  showSuccess = jasmine.createSpy('showSuccess');
  showError = jasmine.createSpy('showError');
  showWarning = jasmine.createSpy('showWarning');
}

describe('EducationClassDetailComponent', () => {
  let fixture: ComponentFixture<EducationClassDetailComponent>;
  let component: EducationClassDetailComponent;
  let educationService: EducationServiceStub;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  const queryParamMap = convertToParamMap({
    className: 'Turma 1',
    programName: 'Programa',
    schoolName: 'Escola',
    programId: 'program-1',
    schoolId: 'school-1'
  });

  const createComponent = () => {
    fixture = TestBed.createComponent(EducationClassDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  beforeEach(async () => {
    paramMapSubject = new BehaviorSubject(convertToParamMap({ id: 'class-1' }));
    educationService = new EducationServiceStub();

    await TestBed.configureTestingModule({
      declarations: [EducationClassDetailComponent],
      imports: [
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })
      ],
      providers: [
        { provide: EducationService, useValue: educationService },
        { provide: NotificationService, useClass: NotificationStub },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
        { provide: Location, useValue: { back: jasmine.createSpy('back') } },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: paramMapSubject.asObservable(),
            snapshot: { queryParamMap }
          }
        }
      ]
    }).compileComponents();
  });

  it('loads linked students when ucId is present', () => {
    paramMapSubject.next(convertToParamMap({ id: 'class-1', ucId: '27535' }));
    createComponent();

    expect(educationService.getCourseUnitStudents).toHaveBeenCalledWith(27535);
    expect(component.students.length).toBe(1);
  });

  it('does not load linked students when ucId is missing', () => {
    paramMapSubject.next(convertToParamMap({ id: 'class-1' }));
    createComponent();

    expect(educationService.getCourseUnitStudents).not.toHaveBeenCalled();
    expect(component.students.length).toBe(0);
  });

  it('aggregates unique activities across students', () => {
    paramMapSubject.next(convertToParamMap({ id: 'class-1', ucId: '27535' }));
    createComponent();

    component.students = [
      { Id: 's1', Activities: [{ Name: 'A', Hidden: false }, { Name: 'B', Hidden: true }] } as any,
      { Id: 's2', Activities: [{ Name: 'A', Hidden: true }, { Name: 'C', Hidden: false }] } as any
    ];

    const activities = component.getAllActivities();

    expect(activities.map((a) => a.Name)).toEqual(['A', 'B', 'C']);
  });
});
