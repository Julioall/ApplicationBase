import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../service/notification/notification.service';
import { StudentsService } from '../../service/students/students.service';
import { Student } from '../../model/student';
import { CepService, CepResult } from '../../service/cep/cep.service';

@Component({
  selector: 'app-student-form',
  templateUrl: './student-form.component.html'
})
export class StudentFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  loading = false;
  submitted = false;
  studentId?: string;
  languages = [
    { value: 'pt', labelKey: 'students.form.langOptions.pt' },
    { value: 'en', labelKey: 'students.form.langOptions.en' },
    { value: 'es', labelKey: 'students.form.langOptions.es' },
  ];
  timeZones = ['UTC', 'America/Sao_Paulo', 'America/New_York', 'Europe/London'];
  statusOptions = [
    { value: 'active', label: 'students.status.active' },
    { value: 'suspended', label: 'students.status.suspended' },
    { value: 'not_currently', label: 'students.status.notCurrently' },
  ];
  cepLoading = false;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly studentsService: StudentsService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly cepService: CepService,
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
        PostalCode: ['', [Validators.pattern(/^\d{5}-?\d{3}$/)]],
        Country: [''],
      }),
      Institution: [''],
      Lang: ['', [Validators.pattern(/^[a-z]{2}(?:_[A-Z]{2})?$/)]],
      TimeZone: [''],
      Status: ['active'],
      LastAccessAt: [''],
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

  private formatCep(value?: string | null): string {
    const digits = this.cepService.sanitize(value || '');
    return digits.length === 8 ? digits.replace(/(\d{5})(\d{3})/, '$1-$2') : digits;
  }

  private formatDateTimeForInput(value?: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (num: number) => num.toString().padStart(2, '0');
    const year = date.getFullYear();
    const month = pad(date.getMonth() + 1);
    const day = pad(date.getDate());
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private normalizeStatus(status?: string | null): 'active' | 'suspended' | 'not_currently' {
    const normalized = (status || '').trim().toLowerCase();
    if (normalized === 'suspended') {
      return 'suspended';
    }
    if (normalized === 'not_currently') {
      return 'not_currently';
    }
    return 'active';
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

  onCepBlur(): void {
    if (this.cepLoading) {
      return;
    }
    this.searchCep(true);
  }

  searchCep(triggeredByBlur = false): void {
    const cepControl = this.addressGroup.get('PostalCode');
    const cep = this.cepService.sanitize(cepControl?.value);

    if (!cep || cep.length !== 8) {
      if (!triggeredByBlur) {
        this.notificationService.showWarning(this.translate.instant('students.form.errors.cepInvalid'));
      }
      return;
    }

    this.cepLoading = true;
    this.cepService.lookup(cep).subscribe({
      next: (result) => {
        this.patchAddressFromCep(result);
        this.notificationService.showSuccess(this.translate.instant('students.form.address.cepSuccess'));
      },
      error: () => {
        this.notificationService.showError(this.translate.instant('students.form.errors.cepNotFound'));
      },
      complete: () => {
        this.cepLoading = false;
      },
    });
  }

  showAddressError(controlName: string, error: string): boolean {
    const control = this.addressGroup.get(controlName);
    return !!control && (control.touched || this.submitted) && control.hasError(error);
  }

  private patchAddressFromCep(result: CepResult): void {
    this.addressGroup.patchValue({
      Street: result.street || this.addressGroup.get('Street')?.value,
      District: result.district || this.addressGroup.get('District')?.value,
      City: result.city || this.addressGroup.get('City')?.value,
      State: result.state || this.addressGroup.get('State')?.value,
      PostalCode: this.formatCep(result.cep),
      Country: this.addressGroup.get('Country')?.value || this.translate.instant('students.form.address.countryDefault'),
    });
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
      this.addressGroup.patchValue({
        ...student.Address,
        PostalCode: this.formatCep(student.Address.PostalCode || ''),
      });
    }

    this.form.patchValue({
      LastAccessAt: this.formatDateTimeForInput(student.LastAccessAt)
    });

    const normalizedStatus = this.normalizeStatus(student.Status || (student.IsActive ? 'active' : 'not_currently'));
    this.form.patchValue({
      Status: normalizedStatus,
      IsActive: normalizedStatus === 'active'
    });
  }

  private buildPayload(): Partial<Student> {
    const raw = this.form.value;
    const address = this.addressGroup.value;
    const postalCode = address.PostalCode ? this.cepService.sanitize(address.PostalCode) : undefined;
    const lastAccess = raw.LastAccessAt ? new Date(raw.LastAccessAt as string) : null;
    const status = this.normalizeStatus(raw.Status as string);
    const isActive = status === 'active';

    return {
      FirstName: (raw.FirstName as string).trim(),
      LastName: (raw.LastName as string).trim(),
      Email: raw.Email ? (raw.Email as string).trim() : undefined,
      IdNumber: raw.IdNumber ? (raw.IdNumber as string).trim() : undefined,
      Phone: raw.Phone ? (raw.Phone as string).trim() : undefined,
      DateOfBirth: raw.DateOfBirth ? new Date(raw.DateOfBirth).toISOString() : null,
      Address: {
        ...address,
        PostalCode: postalCode || undefined,
      },
      Institution: raw.Institution ? (raw.Institution as string).trim() : undefined,
      Lang: raw.Lang ? (raw.Lang as string).trim() : undefined,
      TimeZone: raw.TimeZone ? (raw.TimeZone as string).trim() : undefined,
      Status: status,
      LastAccessAt: lastAccess && !Number.isNaN(lastAccess.getTime()) ? lastAccess.toISOString() : null,
      IsActive: isActive,
    };
  }

  showError(controlName: string, error: string): boolean {
    const control = this.form.get(controlName);
    return !!control && (control.touched || this.submitted) && control.hasError(error);
  }
}
