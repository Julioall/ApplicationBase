import { TestBed, waitForAsync, inject } from "@angular/core/testing";
import { NotificationService } from "./notification.service";
import { ToastrModule } from 'ngx-toastr';

describe('Service: Notification', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToastrModule.forRoot()],
      providers: [NotificationService]
    });
  });

  it('should ...', inject([NotificationService], (service: NotificationService) => {
    expect(service).toBeTruthy();
  }));
});
