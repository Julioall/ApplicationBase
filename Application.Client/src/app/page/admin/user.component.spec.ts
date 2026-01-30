import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, Subject } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { AdminUsersComponent } from './user.component';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';
import { User } from '../../model/user';

class NotificationStub {
  showError = jasmine.createSpy('showError');
}

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

class UserServiceStub {
  users: User[] = [{ Id: '1', Account: { Email: 'a@test.com', Permissions: [] } as any } as User];
  getAllUsers() {
    return of(this.users);
  }
}

describe('AdminUsersComponent', () => {
  let component: AdminUsersComponent;
  let fixture: ComponentFixture<AdminUsersComponent>;
  let notification: NotificationStub;
  let userService: UserServiceStub;

  beforeEach(async () => {
    notification = new NotificationStub();
    userService = new UserServiceStub();

    await TestBed.configureTestingModule({
      declarations: [AdminUsersComponent],
      imports: [HttpClientTestingModule, TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })],
      providers: [
        { provide: UserService, useValue: userService },
        { provide: NotificationService, useValue: notification },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load users on init', () => {
    expect(component.users.length).toBe(1);
    expect(component.totalPages).toBe(1);
  });

  it('should handle load errors', () => {
    const subject = new Subject<User[]>();
    const consoleSpy = spyOn(console, 'error').and.stub();
    spyOn(userService, 'getAllUsers').and.returnValue(subject.asObservable());

    component['loadUsers']();
    subject.error({ error: { detail: 'fail' } });

    expect(notification.showError).toHaveBeenCalled();
    expect(consoleSpy).toHaveBeenCalled();
  });
});
