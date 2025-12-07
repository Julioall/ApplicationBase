import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { AdminUserDetailComponent } from './admin-user-detail.component';
import { NotificationService } from '../../service/notification/notification.service';
import { UserService } from '../../service/user/user.service';
import { User } from '../../model/User';

class NotificationStub {
  showWarning = jasmine.createSpy('showWarning');
  showError = jasmine.createSpy('showError');
  showSuccess = jasmine.createSpy('showSuccess');
}

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

class UserServiceStub {
  user: User = {
    Id: '123',
    Account: { Email: 'user@test.com', Permissions: ['view:home'] } as any,
    Profile: { Name: 'Tester' } as any,
  } as User;
  getUserById() {
    return of(this.user);
  }
  getAvailablePermissions() {
    return of(['view:home', 'manage:users']);
  }
  updatePermissions = jasmine.createSpy('updatePermissions').and.returnValue(of({}));
}

describe('AdminUserDetailComponent', () => {
  let component: AdminUserDetailComponent;
  let fixture: ComponentFixture<AdminUserDetailComponent>;
  let notification: NotificationStub;
  let userService: UserServiceStub;

  beforeEach(async () => {
    notification = new NotificationStub();
    userService = new UserServiceStub();

    await TestBed.configureTestingModule({
      declarations: [AdminUserDetailComponent],
      imports: [HttpClientTestingModule, FormsModule, TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } })],
      providers: [
        { provide: UserService, useValue: userService },
        { provide: NotificationService, useValue: notification },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => '123' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUserDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load user and permissions on init', () => {
    expect(component.user?.Id).toBe('123');
    expect(component.availablePermissions.length).toBeGreaterThan(0);
  });

  it('should warn when adding duplicate permission', () => {
    component.selectedPermission = 'view:home';
    component.addPermission();
    expect(notification.showWarning).toHaveBeenCalled();
  });

  it('should call updatePermissions when removing permission', () => {
    component.removePermission('view:home');
    expect(userService.updatePermissions).toHaveBeenCalledWith('123', []);
  });
});
