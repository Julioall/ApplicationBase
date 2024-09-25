import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { User } from '../../core/model/User';
import { NotificationService } from '../../core/service/notification/notification.service';
import { AuthService } from '../../core/service/auth/auth.service';
import {
  FormGroup,
  FormBuilder,
  Validators,
} from '@angular/forms'; // Importações necessárias

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.scss'],
})
export class AuthComponent implements OnInit {
  loginForm!: FormGroup;
  loginSubmitted: boolean = false;
  signupForm!: FormGroup;
  signupSubmitted: boolean = false;
  rightPanelActive: string = '';
  successMessage: string = '';
  errorMessage: string = '';
  loginFormVisible: boolean = true;
  signupFormVisible: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService,
  ) {}

  ngOnInit() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });

    this.signupForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  showSignUp(): void {
    this.rightPanelActive = 'right-panel-active';
    setTimeout(() => {
      this.signupFormVisible = true;
      this.loginFormVisible = false;
      this.loginSubmitted = false;
      this.loginForm.reset();
    }, 350);
  }

  showSignIn(): void {
    this.rightPanelActive = '';
    setTimeout(() => {
      this.loginFormVisible = true;
      this.signupFormVisible = false;
      this.signupSubmitted = false;
      this.signupForm.reset();
    }, 350);
  }

  onLogin(): void {
    this.loginSubmitted = true;
    if (this.loginForm.invalid) {
      this.notificationService.showError('Please fill out the form correctly.');
      return;
    }
    const { email, password } = this.loginForm?.value;
    this.authService.login(email, password).subscribe({
      next: (response: { token: any }) => {
        if (response && response.token) {
          this.authService.saveToken(response.token);
          this.notificationService.showSuccess('Login successfully!');
          this.loginForm?.reset();
          this.router.navigate(['/home']);
        }
      },
      error: () => {
        this.notificationService.showError(
          'Login error. Please check your credentials.'
        );
      },
    });
  }

  onSignup(): void {
    this.signupSubmitted = true;
    if (this.signupForm.valid) {
      const { fullName, email, password } = this.signupForm.value;

      const newUser: User = {
        Account: {
          Email: email,
          Password: password,
          Role: 'user',
          DateJoined: new Date(),
        },
        Profile: {
          Name: fullName,
          DateOfBirth: undefined,
          ProfilePictureUrl: '',
          AnimeList: [],
        },
      };

      this.authService.signup(newUser).subscribe({
        next: () => {
          this.successMessage = 'Registration successful!';
          this.errorMessage = '';
          
          this.notificationService.showSuccess('Registration successful!');
          
          setTimeout(() => {
            this.showSignIn();
            this.loginForm?.patchValue({ email, password });
          }, 2000);
        },
        error: () => {
          this.notificationService.showError('Error during registration. Please try again.');
        },
      });      
    } else {
      this.notificationService.showError('Please fill out the form correctly.');
    }
  }
}
