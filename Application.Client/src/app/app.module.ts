import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { AuthComponent } from './page/auth/auth.component';
import { RegisterComponent } from './page/register/register.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { LoadingInterceptor } from './service/loading/loading.interceptor';
import { ProblemInterceptorProvider } from './service/http/problem.interceptor';
import { AuthInterceptorProvider } from './service/auth/auth.interceptor';
import { NgxSpinnerModule } from "ngx-spinner";
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ToastContainerComponent } from './shared/notification/toast-container.component';
import { ForgotPasswordComponent } from './page/auth/forgot-password.component';
import { ResetPasswordComponent } from './page/auth/reset-password.component';
import { ModalContainerComponent } from './shared/modal/modal-container.component';

export class AppTranslateLoader implements TranslateLoader {
  constructor(private http: HttpClient) {}
  public getTranslation(lang: string): Observable<Record<string, string>> {
    return this.http.get<Record<string, string>>(`i18n/${lang}.json`);
  }
}

@NgModule({
  declarations: [
    AppComponent,
    AuthComponent,
    RegisterComponent,
    ToastContainerComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    ModalContainerComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    NgxSpinnerModule,
    TranslateModule.forRoot({
      defaultLanguage: 'en',
      loader: {
        provide: TranslateLoader,
        useClass: AppTranslateLoader,
        deps: [HttpClient]
      }
    })
  ],
  providers: [
    AuthInterceptorProvider,
    ProblemInterceptorProvider,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoadingInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
