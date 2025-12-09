import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root',
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  loading$ = this.loadingSubject.asObservable();
  private activeRequests = 0;

  constructor(private spinner: NgxSpinnerService) {}

  startLoading() {
    this.activeRequests += 1;
    if (this.activeRequests === 1) {
      this.loadingSubject.next(true);
      this.spinner.show(undefined, { type: 'ball-spin-fade', size: 'small' });
    }
  }

  stopLoading() {
    if (this.activeRequests === 0) {
      return;
    }

    this.activeRequests -= 1;
    if (this.activeRequests === 0) {
      this.loadingSubject.next(false);
      this.spinner.hide();
    }
  }
}
