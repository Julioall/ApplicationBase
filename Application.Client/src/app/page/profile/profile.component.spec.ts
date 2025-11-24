import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Pipe, PipeTransform } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { ProfileComponent } from './profile.component';
import { ChangePasswordPayload, UpdateProfilePayload, UserService } from '../../service/user/user.service';
import { NotificationService } from '../../service/notification/notification.service';
import { User } from '../../model/User';
import { UserAccount } from '../../model/UserAccount';
import { UserProfile } from '../../model/UserProfile';

type TestHydratedUser = User & { Account: UserAccount; Profile: UserProfile };

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
    use: jasmine.createSpy('use'),
    currentLang: 'en',
  } as unknown as TranslateService;

  const sampleUser: TestHydratedUser = {
    Id: '1',
    Account: {
      Email: 'tester@app.com',
      Permissions: ['view:home', 'view:profile'],
    },
    Profile: {
      Name: 'Tester',
      DateOfBirth: new Date('1990-01-01'),
      ProfilePictureUrl: 'https://cdn/avatar.png',
      JobTitle: 'Engineer',
      Department: 'Platform',
      Organization: 'ApplicationBase',
      Location: 'Goiânia',
    },
  };

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj<UserService>('UserService', ['getCurrentUser', 'updateProfile', 'changePassword']);
    notificationSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['showSuccess', 'showError', 'showWarning']);

    userServiceSpy.getCurrentUser.and.returnValue(of(sampleUser));
    userServiceSpy.updateProfile.and.returnValue(of({}));
    userServiceSpy.changePassword.and.returnValue(of({}));

    await TestBed.configureTestingModule({
      imports: [FormsModule, ReactiveFormsModule, RouterTestingModule],
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
    component.user = sampleUser;
    component.startProfileEdit();
    component.profileForm.get('name')?.setValue('  Updated Name  ');
    component.profileForm.get('dateOfBirth')?.setValue('1995-05-05');
    component.profileForm.get('jobTitle')?.setValue('Designer');
    component.profileForm.get('department')?.setValue('Product');
    component.profileForm.get('organization')?.setValue('Workspace Inc');
    component.profileForm.get('location')?.setValue('São Paulo');

    component.onProfileSubmit();

    const expectedPayload: UpdateProfilePayload = {
      Name: 'Updated Name',
      DateOfBirth: new Date('1995-05-05').toISOString(),
      JobTitle: 'Designer',
      Department: 'Product',
      Organization: 'Workspace Inc',
      Location: 'São Paulo',
    };

    expect(userServiceSpy.updateProfile).toHaveBeenCalledWith(expectedPayload);
    expect(notificationSpy.showSuccess).toHaveBeenCalledWith('profile.messages.profileUpdated');
  });

  it('should not call updateProfile when form invalid', () => {
    component.startProfileEdit();
    component.profileForm.get('name')?.setValue('');
    component.onProfileSubmit();
    expect(userServiceSpy.updateProfile).not.toHaveBeenCalled();
    expect(notificationSpy.showWarning).toHaveBeenCalledWith('profile.messages.formInvalid');
  });

  it('should call changePassword and reset form', () => {
    component.user = sampleUser;
    component.startPasswordEdit();
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
    component.startPasswordEdit();
    component.passwordForm.setValue({
      currentPassword: 'OldPass123!',
      newPassword: 'NewPass123!',
      confirmPassword: 'Mismatch',
    });

    component.onPasswordSubmit();

    expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
    expect(notificationSpy.showWarning).toHaveBeenCalledWith('profile.messages.passwordMismatch');
  });

  it('should remove avatar immediately and call updateProfile', () => {
    component.user = sampleUser;
    component.avatarPreview = sampleUser.Profile.ProfilePictureUrl ?? null;
    userServiceSpy.updateProfile.calls.reset();

    component.removeAvatar();

    expect(component.avatarPreview).toBeNull();
    expect(userServiceSpy.updateProfile).toHaveBeenCalledWith(
      jasmine.objectContaining({
        RemoveProfilePicture: true
      })
    );
  });

  it('should handle avatar uploads via modal and persist after saving', fakeAsync(() => {
    component.user = sampleUser;
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

    expect(component['avatarDraftPreview']).toBe('data:image/png;base64,test');
    expect(component.isAvatarModalOpen).toBeTrue();

    component.saveAvatarChanges();

    expect(component.avatarPreview).toBe('data:image/png;base64,test');
    expect(component.profileForm.dirty).toBeFalse();
    expect(userServiceSpy.updateProfile).toHaveBeenCalledWith(
      jasmine.objectContaining({
        ProfilePicture: mockFile,
        ProfilePictureOffsetX: component.avatarOffsetX,
        ProfilePictureOffsetY: component.avatarOffsetY,
        ProfilePictureScale: component.avatarZoom,
      })
    );
  }));

  it('should warn when selecting an invalid avatar type', () => {
    const invalidFile = new File(['text'], 'readme.txt', { type: 'text/plain' });
    const event = {
      target: {
        files: [invalidFile],
        value: '',
      },
    } as unknown as Event;

    component.handleAvatarChange(event);

    expect(notificationSpy.showWarning).toHaveBeenCalledWith('profile.messages.avatarInvalid');
    expect(component['avatarDraftPreview']).toBeNull();
  });

  it('should preload current avatar data when opening the modal', () => {
    const mockFile = new File(['avatar'], 'avatar.png', { type: 'image/png' });
    component.avatarPreview = 'data:image/png;base64,live';
    (component as any).selectedAvatarFile = mockFile;
    component.avatarOffsetX = 10;
    component.avatarOffsetY = -5;
    component.avatarZoom = 1.2;

    component.openAvatarModal();

    expect(component.isAvatarModalOpen).toBeTrue();
    expect(component['avatarDraftPreview']).toBe('data:image/png;base64,live');
    expect(component['avatarDraftFile']).toBe(mockFile);
    expect(component['draftOffsetX']).toBe(10);
    expect(component['draftOffsetY']).toBe(-5);
    expect(component['draftZoom']).toBe(1.2);
  });

  it('should clear avatar draft data and offsets', () => {
    component['avatarDraftPreview'] = 'data:image/png;base64,temp';
    component['avatarDraftFile'] = new File(['avatar'], 'avatar.png', { type: 'image/png' });
    component['draftOffsetX'] = 15;
    component['draftOffsetY'] = -12;
    component['draftZoom'] = 1.4;

    component.clearAvatarDraft();

    expect(component['avatarDraftPreview']).toBeNull();
    expect(component['avatarDraftFile']).toBeNull();
    expect(component['draftOffsetX']).toBe(0);
    expect(component['draftOffsetY']).toBe(0);
    expect(component['draftZoom']).toBe(1);
  });

  it('should show error message when loadUser fails', () => {
    userServiceSpy.getCurrentUser.and.returnValue(throwError(() => new Error('fail')));
    component.loadUser();

    expect(notificationSpy.showError).toHaveBeenCalledWith('fail');
    expect(component.isLoading).toBeFalse();
  });

  it('should navigate back to home', () => {
    const navigateSpy = spyOn(router, 'navigate');
    component.goBack();
    expect(navigateSpy).toHaveBeenCalledWith(['/home']);
  });
});
