import { TestBed, waitForAsync, inject } from "@angular/core/testing";
import { HttpClientTestingModule } from "@angular/common/http/testing";
import { UserService } from "./user.service";

describe('Service: User', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UserService]
    });
  });

  it('should ...', inject([UserService], (service: UserService) => {
    expect(service).toBeTruthy();
  }));
});
