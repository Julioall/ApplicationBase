import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../service/notification/notification.service';
import { StudentsService } from '../../service/students/students.service';
import { Student } from '../../model/student';

@Component({
  selector: 'app-student-form',
  templateUrl: './student-form.component.html',
  styleUrls: ['./student-form.component.scss']
})
export class StudentFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  loading = false;
  submitted = false;
  studentId?: string;
  languages = ['pt', 'en', 'es'];
  timeZones = ['UTC', 'America/Sao_Paulo', 'America/New_York', 'Europe/London'];

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly studentsService: StudentsService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
  ) {
    this.form = this.fb.group({
      FirstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      LastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      Email: ['', [Validators.email]],
      IdNumber: ['', [Validators.maxLength(20)]],
      Phone: [''],
      DateOfBirth: [''],
      Address: this.fb.group({
        Street: [''],
        Number: [''],
        District: [''],
        City: [''],
        State: [''],
        PostalCode: [''],
        Country: [''],
      }),
      Institution: [''],
      Lang: ['', [Validators.pattern(/^[a-z]{2}(?:_[A-Z]{2})?$/)]],
      TimeZone: [''],
      IsActive: [true],
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEdit = true;
        this.studentId = id;
        this.loadStudent(id);
      }
    });
  }

  get addressGroup(): FormGroup {
    return this.form.get('Address') as FormGroup;
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.buildPayload();
    this.loading = true;

    const request$ = this.isEdit && this.studentId
      ? this.studentsService.updateStudent(this.studentId, payload)
      : this.studentsService.createStudent(payload);

    request$.subscribe({
      next: () => {
        const messageKey = this.isEdit ? 'students.form.updateSuccess' : 'students.form.createSuccess';
        this.loading = false;
        this.notificationService.showSuccess(this.translate.instant(messageKey));
        this.router.navigate(['/students']);
      },
      error: (err) => {
        this.loading = false;
        const detail = err?.detail || err?.title || err?.message || this.translate.instant('students.form.saveError');
        this.notificationService.showError(detail, this.translate.instant('students.common.error'));
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/students']);
  }

  private loadStudent(id: string): void {
    this.loading = true;
    this.studentsService.getStudent(id).subscribe({
      next: (student) => {
        this.patchForm(student);
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        const detail = err?.detail || err?.title || err?.message || this.translate.instant('students.form.loadError');
        this.notificationService.showError(detail, this.translate.instant('students.common.error'));
      }
    });
  }

  private patchForm(student: Student): void {
    this.form.patchValue({
      FirstName: student.FirstName,
      LastName: student.LastName,
      Email: student.Email,
      IdNumber: student.IdNumber,
      Phone: student.Phone,
      DateOfBirth: student.DateOfBirth ? student.DateOfBirth.substring(0, 10) : '',
      Institution: student.Institution,
      Lang: student.Lang,
      TimeZone: student.TimeZone,
      IsActive: student.IsActive
    });

    if (student.Address) {
      this.addressGroup.patchValue(student.Address);
    }
  }

  private buildPayload(): Partial<Student> {
    const raw = this.form.value;
    const address = this.addressGroup.value;
    return {
      FirstName: (raw.FirstName as string).trim(),
      LastName: (raw.LastName as string).trim(),
      Email: raw.Email ? (raw.Email as string).trim() : undefined,
      IdNumber: raw.IdNumber ? (raw.IdNumber as string).trim() : undefined,
      Phone: raw.Phone ? (raw.Phone as string).trim() : undefined,
      DateOfBirth: raw.DateOfBirth ? new Date(raw.DateOfBirth).toISOString() : null,
      Address: address,
      Institution: raw.Institution ? (raw.Institution as string).trim() : undefined,
      Lang: raw.Lang ? (raw.Lang as string).trim() : undefined,
      TimeZone: raw.TimeZone ? (raw.TimeZone as string).trim() : undefined,
      IsActive: raw.IsActive ?? true,
    };
  }

  showError(controlName: string, error: string): boolean {
    const control = this.form.get(controlName);
    return !!control && (control.touched || this.submitted) && control.hasError(error);
  }
}
