import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { ProfileComponent } from './profile.component';
import { UserService, UpdateProfilePayload, ChangePasswordPayload } from '../../service/user/user.service';
import { NotificationService } from '../../service/notification/notification.service';
import { User } from '../../model/User';

@Pipe({ name: 'translate' })
class TranslatePipeMock implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let fixture: ComponentFixture<ProfileComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let router: Router;

  const translateStub = {
    instant: (key: string) => key,
  } as TranslateService;

  const sampleUser: User = {
    Id: '1',
    Account: {
      Email: 'tester@app.com',
      Password: 'OldPass123!',
      Role: 'User',
    },
    Profile: {
      Name: 'Tester',
      DateOfBirth: new Date('1990-01-01'),
      ProfilePictureUrl: 'https://cdn/avatar.png',
    },
  };

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj<UserService>('UserService', [
      'getCurrentUser',
      'updateProfile',
      'changePassword',
    ]);
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'showSuccess',
      'showError',
      'showWarning',
    ]);

    userServiceSpy.getCurrentUser.and.returnValue(of(sampleUser));
    userServiceSpy.updateProfile.and.returnValue(of({}));
    userServiceSpy.changePassword.and.returnValue(of({}));

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, RouterTestingModule],
      declarations: [ProfileComponent, TranslatePipeMock],
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: TranslateService, useValue: translateStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
    fixture.whenStable();
  });

  it('should load current user and populate the form', () => {
    expect(userServiceSpy.getCurrentUser).toHaveBeenCalled();
    expect(component.profileForm.get('name')?.value).toBe(sampleUser.Profile?.Name);
    expect(component.profileForm.get('email')?.value).toBe(sampleUser.Account?.Email);
    expect(component.avatarPreview).toBe(sampleUser.Profile?.ProfilePictureUrl ?? null);
    expect(component.isLoading).toBeFalse();
  });

  it('should call updateProfile with trimmed form data', () => {
    component.profileForm.get('name')?.setValue('  Updated Name  ');
    component.profileForm.get('dateOfBirth')?.setValue('1995-05-05');
    component.avatarPreview = 'data:image/png;base64,abc';
    component.user = sampleUser;

    component.onProfileSubmit();

    const expectedPayload: UpdateProfilePayload = {
      Name: 'Updated Name',
      DateOfBirth: new Date('1995-05-05').toISOString(),
      ProfilePictureUrl: 'data:image/png;base64,abc',
    };

    expect(userServiceSpy.updateProfile).toHaveBeenCalledWith(expectedPayload);
    expect(notificationSpy.showSuccess).toHaveBeenCalledWith('profile.messages.profileUpdated');
  });

  it('should not call updateProfile when form invalid', () => {
    component.profileForm.get('name')?.setValue('');
    component.onProfileSubmit();
    expect(userServiceSpy.updateProfile).not.toHaveBeenCalled();
    expect(notificationSpy.showWarning).toHaveBeenCalledWith('profile.messages.formInvalid');
  });

  it('should call changePassword and reset form', () => {
    component.user = sampleUser;
    component.passwordForm.setValue({
      currentPassword: 'OldPass123!',
      newPassword: 'NewPass123!',
      confirmPassword: 'NewPass123!',
    });

    component.onPasswordSubmit();

    const expectedPayload: ChangePasswordPayload = {
      CurrentPassword: 'OldPass123!',
      NewPassword: 'NewPass123!',
    };
    expect(userServiceSpy.changePassword).toHaveBeenCalledWith(expectedPayload);
    expect(notificationSpy.showSuccess).toHaveBeenCalledWith('profile.messages.passwordUpdated');
    expect(component.passwordForm.get('currentPassword')?.value).toBeNull();
  });

  it('should display error when passwords mismatch', () => {
    component.passwordForm.setValue({
      currentPassword: 'OldPass123!',
      newPassword: 'NewPass123!',
      confirmPassword: 'Mismatch',
    });

    component.onPasswordSubmit();

    expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
    expect(notificationSpy.showWarning).toHaveBeenCalledWith('profile.messages.passwordMismatch');
  });

  it('should handle avatar uploads and mark form dirty', fakeAsync(() => {
    component.profileForm.markAsPristine();

    class MockFileReader {
      public result: string | ArrayBuffer = 'data:image/png;base64,test';
      public onload: (() => void) | null = null;
      readAsDataURL(): void {
        if (this.onload) {
          this.onload();
        }
      }
    }
    spyOn(window as any, 'FileReader').and.returnValue(new MockFileReader());

    const mockFile = new File(['avatar'], 'avatar.png', { type: 'image/png' });
    const event = {
      target: {
        files: [mockFile],
        value: '',
      },
    } as unknown as Event;

    component.handleAvatarChange(event);
    tick();

    expect(component.avatarPreview).toBe('data:image/png;base64,test');
    expect(component.profileForm.dirty).toBeTrue();
  }));

  it('should show error message when loadUser fails', () => {
    userServiceSpy.getCurrentUser.and.returnValue(throwError(() => new Error('fail')));
    component.loadUser();

    expect(notificationSpy.showError).toHaveBeenCalledWith('fail');
    expect(component.isLoading).toBeFalse();
  });

  it('should go back to home when goBack is called', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.goBack();
    expect(navigateSpy).toHaveBeenCalledWith(['/home']);
  });
});
