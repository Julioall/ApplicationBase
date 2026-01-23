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
import { AuthService } from '../../service/auth/auth.service';
import { MANAGE_EDUCATION_PERMISSION } from '../../model/permissions';
import { Router } from '@angular/router';

@Component({
  selector: 'app-education-explorer',
  templateUrl: './education-explorer.component.html'
})
export class EducationExplorerComponent implements OnInit, OnDestroy {
  form: FormGroup;
  schools: EducationSchool[] = [];
  programs: EducationProgram[] = [];
  classes: EducationClass[] = [];
  searchTerm = '';
  loadingSchools = false;
  loadingPrograms = false;
  loadingClasses = false;
  importing = false;
  private searchSub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly educationService: EducationService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {
    this.form = this.fb.group({
      schoolId: [''],
      programId: [''],
      search: ['']
    });
  }

  ngOnInit(): void {
    this.loadSchools();
    this.searchSub = this.searchControl.valueChanges
      .pipe(debounceTime(300))
      .subscribe((value) => {
        this.searchTerm = (value || '').toString().trim().toLowerCase();
      });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSchoolChange(): void {
    const schoolId = this.schoolControl.value;
    this.form.patchValue({ programId: '' }, { emitEvent: false });
    this.programs = [];
    this.classes = [];
    this.resetSearch();
    if (schoolId) {
      this.loadPrograms(schoolId);
    }
  }

  onProgramChange(): void {
    const programId = this.programControl.value;
    this.classes = [];
    this.resetSearch();
    if (programId) {
      this.loadClasses(programId);
    }
  }

  loadSchools(): void {
    this.loadingSchools = true;
    this.educationService.getSchools()
      .pipe(finalize(() => this.loadingSchools = false))
      .subscribe({
        next: (schools) => {
          this.schools = schools;
          const selected = this.pickFirstAvailable(schools.map(s => s.Id), this.schoolControl.value);
          this.form.patchValue({ schoolId: selected, programId: '' }, { emitEvent: false });
          this.programs = [];
          this.classes = [];
          this.resetSearch();
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
          this.form.patchValue({ programId: selected }, { emitEvent: false });
          this.classes = [];
          if (selected) {
            this.loadClasses(selected);
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
        },
        error: (err) => this.handleError(err, 'education.errors.loadClasses')
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

  formatPeriod(start?: number | null, end?: number | null, fallback?: string | null): string {
    if (start && end) {
      const startDate = new Date(start * 1000);
      const endDate = new Date(end * 1000);
      return `${this.formatDate(startDate)} - ${this.formatDate(endDate)}`;
    }
    return fallback || this.translate.instant('education.labels.periodUnknown');
  }

  formatClassPeriod(classItem: EducationClass): string {
    return this.formatPeriod(classItem.StartDate ?? undefined, classItem.EndDate ?? undefined, undefined);
  }

  get canImport(): boolean {
    return this.authService.hasPermission(MANAGE_EDUCATION_PERMISSION);
  }

  get hasResults(): boolean {
    return (this.classes?.length ?? 0) > 0;
  }

  get schoolControl(): FormControl<string> {
    return this.form.get('schoolId') as FormControl<string>;
  }

  get programControl(): FormControl<string> {
    return this.form.get('programId') as FormControl<string>;
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

  get filteredClasses(): EducationClass[] {
    if (!this.searchTerm) {
      return this.classes;
    }
    const term = this.searchTerm.toLowerCase();
    return this.classes.filter(classItem => {
      const className = (classItem.Name || '').toLowerCase();
      const courseName = (classItem.CourseCategoryRaw || '').toLowerCase();
      return className.includes(term) || courseName.includes(term);
    });
  }

  openClass(classItem: EducationClass): void {
    if (!classItem?.Id) {
      return;
    }
    const schoolName = this.selectedSchoolName === '-' ? '' : this.selectedSchoolName;
    const programName = this.selectedProgramName === '-' ? '' : this.selectedProgramName;
    this.router.navigate(['/education', 'classes', classItem.Id], {
      queryParams: {
        programId: classItem.ProgramId,
        schoolId: classItem.SchoolId,
        className: classItem.Name,
        programName,
        schoolName
      }
    });
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

  private resetSearch(): void {
    this.searchTerm = '';
    this.searchControl.setValue('', { emitEvent: false });
  }
}
