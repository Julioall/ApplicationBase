import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { delay, finalize} from 'rxjs/operators';
import { LoadingService } from './loading.service'; // Atualize o caminho conforme necessário

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  constructor(private loadingService: LoadingService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    this.loadingService.startLoading();
    return next.handle(req).pipe(
      delay(1000),
      finalize(() => {
        this.loadingService.stopLoading();
      })
    );
  }
  
}
