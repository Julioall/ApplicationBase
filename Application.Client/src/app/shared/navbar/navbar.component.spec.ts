import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { NavbarComponent } from './navbar.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ThemeService } from '../../service/theme/theme.service';
import { AuthService } from '../../service/auth/auth.service';
import { UserService } from '../../service/user/user.service';
import { NotificationApiService } from '../../service/notification/notification-api.service';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { NotificationService } from '../../service/notification/notification.service';
import { EducationService } from '../../service/education/education.service';

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

const authStub: Partial<AuthService> = {
  isLoggedIn: () => false,
  logout: () => {},
  hasPermission: () => false,
  hasAnyPermission: () => false,
};

const userServiceStub: Partial<UserService> = {
  getCurrentUser: () => of({}),
};

const notificationApiStub: Partial<NotificationApiService> = {
  getLatest: () => of({ Items: [], UnreadCount: 0 }),
  markAsRead: (_id: string) => of(void 0),
  markManyAsRead: (_ids: string[]) => of(void 0),
  delete: (_id: string) => of(void 0),
};

const themeStub: Partial<ThemeService> = {
  setTheme: () => {},
  getActiveTheme: () => 'light' as any,
  toggleTheme: () => 'light' as any,
};

const educationServiceStub: Partial<EducationService> = {
  triggerSync: () => of({}) as any,
};

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ NavbarComponent ],
      imports: [
        RouterTestingModule,
        HttpClientTestingModule,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeLoader }
        })
      ],
      providers: [
        { provide: ThemeService, useValue: themeStub },
        { provide: AuthService, useValue: authStub },
        { provide: UserService, useValue: userServiceStub },
        { provide: NotificationApiService, useValue: notificationApiStub },
        NotificationService,
        { provide: EducationService, useValue: educationServiceStub },
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
