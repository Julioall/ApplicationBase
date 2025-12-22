import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { Subscription, debounceTime } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../service/notification/notification.service';
import { EducationService } from '../../service/education/education.service';
import { EducationSchool } from '../../model/education-school';
import { EducationProgram } from '../../model/education-program';
import { EducationClass } from '../../model/education-class';
import { EducationUc } from '../../model/education-uc';
import { CourseImportResult } from '../../model/course-import-result';
import { AuthService } from '../../service/auth/auth.service';
import { MANAGE_EDUCATION_PERMISSION } from '../../model/permissions';

@Component({
  selector: 'app-education-explorer',
  templateUrl: './education-explorer.component.html',
  styleUrls: ['./education-explorer.component.scss']
})
export class EducationExplorerComponent implements OnInit, OnDestroy {
  form: FormGroup;
  schools: EducationSchool[] = [];
  programs: EducationProgram[] = [];
  classes: EducationClass[] = [];
  ucs: EducationUc[] = [];
  total = 0;
  pageNumber = 1;
  pageSize = 12;
  loadingSchools = false;
  loadingPrograms = false;
  loadingClasses = false;
  loadingUcs = false;
  importing = false;
  private searchSub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly educationService: EducationService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly authService: AuthService,
  ) {
    this.form = this.fb.group({
      schoolId: [''],
      programId: [''],
      classId: [''],
      search: ['']
    });
  }

  ngOnInit(): void {
    this.loadSchools();
    this.searchSub = this.searchControl.valueChanges
      .pipe(debounceTime(300))
      .subscribe(() => {
        this.pageNumber = 1;
        this.loadUcs();
      });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSchoolChange(): void {
    const schoolId = this.schoolControl.value;
    this.form.patchValue({ programId: '', classId: '' }, { emitEvent: false });
    this.programs = [];
    this.classes = [];
    this.ucs = [];
    this.total = 0;
    if (schoolId) {
      this.loadPrograms(schoolId);
    }
  }

  onProgramChange(): void {
    const programId = this.programControl.value;
    this.form.patchValue({ classId: '' }, { emitEvent: false });
    this.classes = [];
    this.ucs = [];
    this.total = 0;
    if (programId) {
      this.loadClasses(programId);
    }
  }

  onClassChange(): void {
    this.pageNumber = 1;
    this.loadUcs();
  }

  loadSchools(): void {
    this.loadingSchools = true;
    this.educationService.getSchools()
      .pipe(finalize(() => this.loadingSchools = false))
      .subscribe({
        next: (schools) => {
          this.schools = schools;
          const selected = this.pickFirstAvailable(schools.map(s => s.Id), this.schoolControl.value);
          this.form.patchValue({ schoolId: selected, programId: '', classId: '' }, { emitEvent: false });
          if (selected) {
            this.loadPrograms(selected);
          }
        },
        error: (err) => this.handleError(err, 'education.errors.loadSchools')
      });
  }

  loadPrograms(schoolId: string): void {
    this.loadingPrograms = true;
    this.educationService.getPrograms(schoolId)
      .pipe(finalize(() => this.loadingPrograms = false))
      .subscribe({
        next: (programs) => {
          this.programs = programs;
          const selected = this.pickFirstAvailable(programs.map(p => p.Id), this.programControl.value);
          this.form.patchValue({ programId: selected, classId: '' }, { emitEvent: false });
          if (selected) {
            this.loadClasses(selected);
          } else {
            this.classes = [];
            this.ucs = [];
            this.total = 0;
          }
        },
        error: (err) => this.handleError(err, 'education.errors.loadPrograms')
      });
  }

  loadClasses(programId: string): void {
    this.loadingClasses = true;
    this.educationService.getClasses(programId)
      .pipe(finalize(() => this.loadingClasses = false))
      .subscribe({
        next: (classes) => {
          this.classes = classes;
          const selected = this.pickFirstAvailable(classes.map(c => c.Id), this.classControl.value);
          this.form.patchValue({ classId: selected }, { emitEvent: false });
          this.pageNumber = 1;
          this.loadUcs();
        },
        error: (err) => this.handleError(err, 'education.errors.loadClasses')
      });
  }

  loadUcs(): void {
    const programId = this.programControl.value || '';
    const classId = this.classControl.value || '';
    const search = (this.searchControl.value || '').toString().trim();

    if (!programId && !classId && !search) {
      this.ucs = [];
      this.total = 0;
      return;
    }

    this.loadingUcs = true;
    this.educationService.searchUcs(
      {
        PageNumber: this.pageNumber,
        PageSize: this.pageSize,
        Search: search || undefined
      },
      classId || undefined,
      programId || undefined
    ).pipe(finalize(() => this.loadingUcs = false))
      .subscribe({
        next: (paged) => {
          this.ucs = paged.Items || [];
          this.total = paged.Total || 0;
          this.pageNumber = paged.PageNumber || this.pageNumber;
          this.pageSize = paged.PageSize || this.pageSize;
        },
        error: (err) => this.handleError(err, 'education.errors.loadUcs')
      });
  }

  onImportCourses(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.importing = true;
    this.educationService.importCourses(file)
      .pipe(finalize(() => {
        this.importing = false;
        input.value = '';
      }))
      .subscribe({
        next: (result) => {
          this.handleImportQueued(result);
          this.loadSchools();
        },
        error: (err) => this.handleError(err, 'education.errors.import')
      });
  }

  changePage(delta: number): void {
    const totalPages = Math.max(1, Math.ceil(this.total / this.pageSize));
    this.pageNumber = Math.min(Math.max(1, this.pageNumber + delta), totalPages);
    this.loadUcs();
  }

  formatPeriod(start?: number | null, end?: number | null, fallback?: string | null): string {
    if (start && end) {
      const startDate = new Date(start * 1000);
      const endDate = new Date(end * 1000);
      return `${this.formatDate(startDate)} - ${this.formatDate(endDate)}`;
    }
    return fallback || this.translate.instant('education.labels.periodUnknown');
  }

  formatClassPeriod(): string {
    const classId = this.form.get('classId')?.value;
    const selected = this.classes.find(c => c.Id === classId);
    if (!selected) {
      return this.translate.instant('education.labels.noClassSelected');
    }
    return this.formatPeriod(selected.StartDate ?? undefined, selected.EndDate ?? undefined, undefined);
  }

  trackByUc(_: number, uc: EducationUc): string | number | undefined {
    return uc.Id || uc.EadId;
  }

  get canImport(): boolean {
    return this.authService.hasPermission(MANAGE_EDUCATION_PERMISSION);
  }

  get hasResults(): boolean {
    return (this.ucs?.length ?? 0) > 0;
  }

  get totalPages(): number {
    const pages = Math.ceil(this.total / this.pageSize);
    return Number.isFinite(pages) && pages > 0 ? pages : 1;
  }

  get schoolControl(): FormControl<string> {
    return this.form.get('schoolId') as FormControl<string>;
  }

  get programControl(): FormControl<string> {
    return this.form.get('programId') as FormControl<string>;
  }

  get classControl(): FormControl<string> {
    return this.form.get('classId') as FormControl<string>;
  }

  get searchControl(): FormControl<string> {
    return this.form.get('search') as FormControl<string>;
  }

  get selectedSchoolName(): string {
    const id = this.schoolControl.value;
    return this.schools.find(s => s.Id === id)?.Name || '-';
  }

  get selectedProgramName(): string {
    const id = this.programControl.value;
    return this.programs.find(p => p.Id === id)?.Name || '-';
  }

  get selectedClassName(): string {
    const id = this.classControl.value;
    return this.classes.find(c => c.Id === id)?.Name || '-';
  }

  private formatDate(date: Date): string {
    return date.toLocaleDateString(this.translate.currentLang || 'en', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });
  }

  private pickFirstAvailable<T extends string | undefined>(ids: (T | undefined)[], current?: T): T | '' {
    if (current && ids.includes(current)) {
      return current;
    }
    const first = ids.find(id => !!id);
    return (first as T) || '';
  }

  private handleError(err: any, translationKey: string): void {
    const detail = err?.detail || err?.title || err?.message || this.translate.instant(translationKey);
    this.notificationService.showError(detail, this.translate.instant('education.labels.error'));
  }

  private handleImportQueued(_: any): void {
    this.notificationService.showInfo(
      this.translate.instant('education.import.queuedMessage'),
      this.translate.instant('education.import.title')
    );
  }
}
