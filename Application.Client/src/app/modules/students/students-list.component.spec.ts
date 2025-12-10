import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { StudentsListComponent } from './students-list.component';
import { StudentsService } from '../../service/students/students.service';
import { NotificationService } from '../../service/notification/notification.service';
import { Student } from '../../model/student';

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

class StudentsServiceStub {
  students: Student[] = [{ Id: 'students-1', FirstName: 'Ana', LastName: 'Silva', IsActive: true }];
  getStudents = jasmine.createSpy('getStudents').and.returnValue(of({ Items: this.students, Total: 1, PageNumber: 1, PageSize: 10 }));
  deleteStudent = jasmine.createSpy('deleteStudent').and.returnValue(of(void 0));
}

class NotificationStub {
  showError = jasmine.createSpy('showError');
  showSuccess = jasmine.createSpy('showSuccess');
}

describe('StudentsListComponent', () => {
  let component: StudentsListComponent;
  let fixture: ComponentFixture<StudentsListComponent>;
  let studentsService: StudentsServiceStub;
  let routerNavigate: jasmine.Spy;

  beforeEach(async () => {
    studentsService = new StudentsServiceStub();
    routerNavigate = jasmine.createSpy('navigate');

    await TestBed.configureTestingModule({
      declarations: [StudentsListComponent],
      imports: [
        FormsModule,
        ReactiveFormsModule,
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })
      ],
      providers: [
        { provide: StudentsService, useValue: studentsService },
        { provide: NotificationService, useClass: NotificationStub },
        { provide: Router, useValue: { navigate: routerNavigate } },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load students on init', () => {
    expect(component.students.length).toBe(1);
    expect(component.total).toBe(1);
    expect(studentsService.getStudents).toHaveBeenCalled();
  });

  it('should navigate to edit when clicking edit', () => {
    component.onEdit(component.students[0]);
    expect(routerNavigate).toHaveBeenCalled();
  });

  it('should delete student when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    component.onDelete(component.students[0]);
    expect(studentsService.deleteStudent).toHaveBeenCalledWith('students-1');
  });
});
