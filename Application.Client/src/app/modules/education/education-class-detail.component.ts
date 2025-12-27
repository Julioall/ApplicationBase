import { Location } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { EducationService } from '../../service/education/education.service';
import { EducationUc } from '../../model/education-uc';
import { Student } from '../../model/student';
import { NotificationService } from '../../service/notification/notification.service';
import { StudentsService } from '../../service/students/students.service';

@Component({
  selector: 'app-education-class-detail',
  templateUrl: './education-class-detail.component.html',
  styleUrls: ['./education-class-detail.component.scss']
})
export class EducationClassDetailComponent implements OnInit, OnDestroy {
  classId = '';
  ucId = '';
  className = '';
  programName = '';
  schoolName = '';
  classPeriodText = '';
  programId = '';
  schoolId = '';
  units: EducationUc[] = [];
  selectedUc?: EducationUc;
  students: Student[] = [];
  loadingUnits = false;
  loadingMetadata = false;
  loadingStudents = false;
  importingParticipants = false;
  private routeSub?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly educationService: EducationService,
    private readonly studentsService: StudentsService,
    private readonly translate: TranslateService,
    private readonly notificationService: NotificationService,
    private readonly location: Location
  ) {}

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => {
      const classId = params.get('id') || '';
      const ucId = params.get('ucId') || '';
      if (!classId) {
        this.handleError(null, 'education.errors.loadClasses');
        return;
      }

      this.classId = classId;
      this.ucId = ucId;
      this.selectedUc = undefined;
      this.resolveMetadata();
      this.loadUnits();
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  goBack(): void {
    if (this.isUcDetail) {
      this.router.navigate(['/education', 'classes', this.classId], { queryParams: this.buildQueryParams() });
      return;
    }
    if (window.history.length > 1) {
      this.location.back();
    } else {
      this.router.navigate(['/education']);
    }
  }

  formatPeriod(start?: number | null, end?: number | null, fallback?: string | null): string {
    if (start && end) {
      const startDate = new Date(start * 1000);
      const endDate = new Date(end * 1000);
      return `${this.formatDate(startDate)} - ${this.formatDate(endDate)}`;
    }
    return fallback || this.translate.instant('education.labels.periodUnknown');
  }

  openUc(uc: EducationUc): void {
    if (!uc || !uc.EadId) {
      return;
    }

    this.router.navigate(['/education', 'classes', this.classId, 'ucs', uc.EadId], {
      queryParams: this.buildQueryParams()
    });
  }

  onImportParticipants(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!this.selectedUc?.EadId) {
      this.notificationService.showError(
        this.translate.instant('education.ucDetail.importMissingCourse'),
        this.translate.instant('education.labels.error')
      );
      input.value = '';
      return;
    }

    const renamed = new File([file], `courseid_${this.selectedUc.EadId}_participants.json`, {
      type: file.type || 'application/json'
    });

    this.importingParticipants = true;
    this.studentsService.importStudents(renamed)
      .pipe(finalize(() => {
        this.importingParticipants = false;
        input.value = '';
      }))
      .subscribe({
        next: (result) => {
          if (result.Errors?.length) {
            this.notificationService.showWarning(
              this.translate.instant('education.ucDetail.importWarning', { count: result.Errors.length }),
              this.translate.instant('education.ucDetail.importTitle')
            );
          } else {
            this.notificationService.showSuccess(
              this.translate.instant('education.ucDetail.importSuccess', { created: result.Created, updated: result.Updated, skipped: result.Skipped }),
              this.translate.instant('education.ucDetail.importTitle')
            );
          }
          this.loadUcStudents();
        },
        error: (err) => this.handleError(err, 'education.ucDetail.importError')
      });
  }

  get isUcDetail(): boolean {
    return !!this.ucId;
  }

  get displaySchoolName(): string {
    return this.selectedUc?.SchoolNameDerived || this.schoolName;
  }

  get displayProgramName(): string {
    return this.selectedUc?.ProgramNameDerived || this.programName;
  }

  get displayPeriodText(): string {
    if (this.isUcDetail && this.selectedUc) {
      return this.formatPeriod(this.selectedUc.StartDate, this.selectedUc.EndDate, this.selectedUc.PeriodTextDerived || null);
    }
    return this.classPeriodText;
  }

  private resolveMetadata(): void {
    const query = this.route.snapshot.queryParamMap;
    this.className = query.get('className') || this.className;
    this.programName = query.get('programName') || this.programName;
    this.schoolName = query.get('schoolName') || this.schoolName;

    this.programId = query.get('programId') || this.programId;
    this.schoolId = query.get('schoolId') || this.schoolId;

    if (this.programId) {
      this.loadingMetadata = true;
      this.educationService.getClasses(this.programId)
        .pipe(finalize(() => this.loadingMetadata = false))
        .subscribe({
          next: (classes) => {
            const found = classes.find(c => c.Id === this.classId);
            if (found) {
              this.className = this.className || found.Name;
              this.classPeriodText = this.formatPeriod(found.StartDate ?? undefined, found.EndDate ?? undefined, null);
              if (!this.schoolName && found.SchoolId) {
                this.loadSchoolName(found.SchoolId);
              }
              if (!this.programName && this.schoolId) {
                this.loadProgramName(this.schoolId, found.ProgramId);
              }
            }
          },
          error: (err) => this.handleError(err, 'education.errors.loadClasses')
        });
    } else if (this.schoolId) {
      this.loadSchoolName(this.schoolId);
    }
  }

  private loadSchoolName(schoolId: string): void {
    if (!schoolId) {
      return;
    }
    this.educationService.getSchools()
      .subscribe({
        next: (schools) => {
          const school = schools.find(s => s.Id === schoolId);
          if (school) {
            this.schoolName = school.Name;
          }
        },
        error: () => {}
      });
  }

  private loadProgramName(schoolId: string, programId: string): void {
    if (!schoolId || !programId) {
      return;
    }
    this.educationService.getPrograms(schoolId)
      .subscribe({
        next: (programs) => {
          const program = programs.find(p => p.Id === programId);
          if (program) {
            this.programName = program.Name;
          }
        },
        error: () => {}
      });
  }

  private loadUnits(): void {
    this.loadingUnits = true;
    this.educationService.getClassUcs(this.classId)
      .pipe(finalize(() => this.loadingUnits = false))
      .subscribe({
        next: (ucs) => {
          this.units = ucs || [];
          if (this.ucId) {
            this.selectedUc = this.units.find(uc =>
              uc.EadId?.toString() === this.ucId || uc.Id === this.ucId || uc.Id?.endsWith(`/${this.ucId}`)
            );
            if (!this.selectedUc) {
              this.handleError(null, 'education.ucDetail.notFound');
              this.students = [];
              this.loadingStudents = false;
              return;
            }
            this.loadUcStudents();
          } else {
            this.students = [];
          }
        },
        error: (err) => this.handleError(err, 'education.errors.loadUcs')
      });
  }

  private formatDate(date: Date): string {
    return date.toLocaleDateString(this.translate.currentLang || 'en', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });
  }

  trackByUc(index: number, uc: EducationUc): string | number {
    return uc?.Id || uc?.EadId || index;
  }

  trackByStudent(index: number, student: Student): string | number {
    return student?.Id || student?.Email || index;
  }

  getStudentName(student: Student): string {
    const first = student?.FirstName?.trim() || '';
    const last = student?.LastName?.trim() || '';
    const full = `${first} ${last}`.trim();
    return full || this.translate.instant('education.ucDetail.studentFallback');
  }

  private buildQueryParams(): Record<string, string> {
    return {
      className: this.className || '',
      programId: this.programId || '',
      schoolId: this.schoolId || '',
      programName: this.programName || '',
      schoolName: this.schoolName || '',
      classId: this.classId
    };
  }

  private loadUcStudents(): void {
    if (!this.selectedUc?.EadId) {
      this.students = [];
      return;
    }

    this.loadingStudents = true;
    this.educationService.getUcStudents(this.selectedUc.EadId)
      .pipe(finalize(() => this.loadingStudents = false))
      .subscribe({
        next: (students) => {
          this.students = students || [];
        },
        error: (err) => this.handleError(err, 'education.ucDetail.loadStudentsError')
      });
  }

  private handleError(err: any, translationKey: string): void {
    const detail = err?.detail || err?.title || err?.message || this.translate.instant(translationKey);
    this.notificationService.showError(detail, this.translate.instant('education.labels.error'));
  }
}
