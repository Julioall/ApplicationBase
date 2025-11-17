import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterTestingModule } from '@angular/router/testing';

import { RegisterComponent } from './register.component';
import { AuthService } from '../../service/auth/auth.service';
import { NotificationService } from '../../service/notification/notification.service';
import { TranslateService } from '@ngx-translate/core';
import { ThemeService } from '../../service/theme/theme.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let notificationService: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['signup']);
    notificationService = jasmine.createSpyObj<NotificationService>('NotificationService', ['showSuccess', 'showError']);

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, RouterTestingModule],
      declarations: [RegisterComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: NotificationService, useValue: notificationService },
        {
          provide: TranslateService,
          useValue: {
            instant: (key: string) => key,
          },
        },
        {
          provide: ThemeService,
          useValue: {
            getActiveTheme: () => 'light',
            toggleTheme: () => 'light',
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('não deve chamar signup nem notificar quando o formulário é inválido', () => {
    component.onSubmit();
    expect(authService.signup).not.toHaveBeenCalled();
    expect(notificationService.showError).not.toHaveBeenCalled();
  });

  it('deve propagar mensagem de validação do backend ao notificar erro', () => {
    component.registerForm.setValue({
      fullName: 'Usuário Teste',
      email: 'teste@exemplo.com',
      password: '12345678',
      confirmPassword: '12345678',
      updates: true,
    });

    const backendError = new HttpErrorResponse({
      status: 400,
      error: {
        errors: {
          'Account.Email': ['Este e-mail já existe na nossa base de dados'],
        },
      },
    });

    authService.signup.and.returnValue(throwError(() => backendError));

    component.onSubmit();

    expect(authService.signup).toHaveBeenCalled();
    expect(notificationService.showError).toHaveBeenCalledWith('Este e-mail já existe na nossa base de dados');
  });

  it('deve seguir fluxo de sucesso quando o backend retorna OK', () => {
    component.registerForm.setValue({
      fullName: 'Usuário Teste',
      email: 'novo@exemplo.com',
      password: '12345678',
      confirmPassword: '12345678',
      updates: true,
    });

    authService.signup.and.returnValue(of({}));

    component.onSubmit();

    expect(authService.signup).toHaveBeenCalled();
    expect(notificationService.showSuccess).toHaveBeenCalled();
  });
});
