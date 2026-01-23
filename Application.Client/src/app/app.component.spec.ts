import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { NgxSpinnerModule } from 'ngx-spinner';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { AuthService } from './service/auth/auth.service';
import { ThemeService } from './service/theme/theme.service';

class FakeLoader implements TranslateLoader {
  getTranslation(): any {
    return of({});
  }
}

const authStub: Partial<AuthService> = {
  isLoggedIn: () => false,
};

const themeStub: Partial<ThemeService> = {
  setTheme: () => {},
  getActiveTheme: () => 'light' as any,
};

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      imports: [TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } }), NgxSpinnerModule, RouterTestingModule, HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: authStub },
        { provide: ThemeService, useValue: themeStub },
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
