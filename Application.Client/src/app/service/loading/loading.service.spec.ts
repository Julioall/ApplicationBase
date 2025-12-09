import { NgxSpinnerService } from 'ngx-spinner';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let spinner: jasmine.SpyObj<NgxSpinnerService>;
  let service: LoadingService;

  beforeEach(() => {
    spinner = jasmine.createSpyObj<NgxSpinnerService>('NgxSpinnerService', ['show', 'hide']);
    service = new LoadingService(spinner);
  });

  it('keeps spinner visible while there are active requests', () => {
    const states: boolean[] = [];
    service.loading$.subscribe((value) => states.push(value));

    service.startLoading();
    service.startLoading();

    expect(spinner.show).toHaveBeenCalledTimes(1);
    expect(states).toEqual([false, true]);

    service.stopLoading();
    expect(spinner.hide).not.toHaveBeenCalled();
    expect(states).toEqual([false, true]);

    service.stopLoading();
    expect(spinner.hide).toHaveBeenCalledTimes(1);
    expect(states).toEqual([false, true, false]);
  });

  it('ignores extra stop calls when already idle', () => {
    service.stopLoading();
    service.startLoading();
    service.stopLoading();
    service.stopLoading();

    expect(spinner.hide).toHaveBeenCalledTimes(1);
  });
});
