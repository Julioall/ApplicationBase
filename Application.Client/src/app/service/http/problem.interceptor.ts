import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HTTP_INTERCEPTORS,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NotificationService } from '../notification/notification.service';

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  instance?: string;
  traceId?: string;
}

@Injectable()
export class ProblemInterceptor implements HttpInterceptor {
  constructor(private readonly notificationService: NotificationService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error instanceof HttpErrorResponse) {
          const contentType = error.headers.get('content-type') || '';

          if (contentType.includes('application/problem+json') && error.error) {
            const problem = error.error as ApiProblem;
            const messages = problem.errors
              ? Object.values(problem.errors).flat()
              : undefined;

            const message =
              (messages && messages.length ? messages.join(' | ') : undefined) ||
              problem.detail ||
              problem.title ||
              'Ocorreu um erro inesperado.';

            this.notificationService.showError(message, 'Erro');
            return throwError(() => problem);
          }

          // Fallback para outros erros HTTP
          const fallbackMessage = error.message || 'Ocorreu um erro inesperado.';
          this.notificationService.showError(fallbackMessage, 'Erro');
        }

        return throwError(() => error);
      })
    );
  }
}

export const ProblemInterceptorProvider = {
  provide: HTTP_INTERCEPTORS,
  useClass: ProblemInterceptor,
  multi: true,
};
