import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root',
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  loading$ = this.loadingSubject.asObservable();

  constructor(private spinner: NgxSpinnerService) {}

  startLoading() {
    this.loadingSubject.next(true);
    this.spinner.show(undefined, { type: 'ball-spin-fade', size: 'small' });
  }

  stopLoading() {
    this.loadingSubject.next(false);
    this.spinner.hide();
  }
}
