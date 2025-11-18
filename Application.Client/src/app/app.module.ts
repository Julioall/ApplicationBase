import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { AuthComponent } from './page/auth/auth.component';
import { RegisterComponent } from './page/register/register.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { LoadingInterceptor } from './service/loading/loading.interceptor';
import { ProblemInterceptorProvider } from './service/http/problem.interceptor';
import { NgxSpinnerModule } from "ngx-spinner";
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { Observable } from 'rxjs';

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
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
    NgxSpinnerModule,
    ToastrModule.forRoot(),
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
