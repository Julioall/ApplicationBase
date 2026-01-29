import { Location } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { EducationService } from '../../service/education/education.service';
import { CourseUnit } from '../../model/course-unit';
import { StudentUcDto } from '../../model/student-uc-dto';
import { NotificationService } from '../../service/notification/notification.service';

@Component({
  selector: 'app-education-class-detail',
  templateUrl: './education-class-detail.component.html'
})
export class EducationClassDetailComponent implements OnInit, OnDestroy {
  classId = '';
  courseUnitId = '';
  className = '';
  programName = '';
  schoolName = '';
  classPeriodText = '';
  programId = '';
  schoolId = '';
  units: CourseUnit[] = [];
  selectedCourseUnit?: CourseUnit;
  students: StudentUcDto[] = [];
  loadingUnits = false;
  loadingMetadata = false;
  loadingStudents = false;
  private routeSub?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly educationService: EducationService,
    private readonly translate: TranslateService,
    private readonly notificationService: NotificationService,
    private readonly location: Location
  ) {}

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => {
      const classId = params.get('id') || '';
      const courseUnitId = params.get('courseUnitId') || '';
      if (!classId) {
        this.handleError(null, 'education.errors.loadClasses');
        return;
      }

      this.classId = classId;
      this.courseUnitId = courseUnitId;
      this.selectedCourseUnit = undefined;
      this.resolveMetadata();
      this.loadUnits();
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  goBack(): void {
    if (this.isCourseUnitDetail) {
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

  openUc(uc: CourseUnit): void {
    if (!uc || !uc.EadId) {
      return;
    }

    this.router.navigate(['/education', 'classes', this.classId, 'course-units', uc.EadId], {
      queryParams: this.buildQueryParams()
    });
  }

  get isCourseUnitDetail(): boolean {
    return !!this.courseUnitId;
  }

  get displaySchoolName(): string {
    return this.selectedCourseUnit?.SchoolName || this.selectedCourseUnit?.EventName || this.schoolName;
  }

  get displayProgramName(): string {
    return this.selectedCourseUnit?.CourseName || this.programName;
  }

  get displayPeriodText(): string {
    if (this.isCourseUnitDetail && this.selectedCourseUnit) {
      return this.formatPeriod(this.selectedCourseUnit.StartDate, this.selectedCourseUnit.EndDate, this.selectedCourseUnit.PeriodTextDerived || null);
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
              if (!this.programName && this.schoolId && found.ProgramId) {
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
    this.educationService.getClassCourseUnits(this.classId)
      .pipe(finalize(() => this.loadingUnits = false))
      .subscribe({
        next: (ucs) => {
          this.units = ucs || [];
          if (this.courseUnitId) {
            this.selectedCourseUnit = this.units.find(uc =>
              uc.EadId?.toString() === this.courseUnitId || uc.Id === this.courseUnitId || uc.Id?.endsWith(`/${this.courseUnitId}`)
            );
            if (!this.selectedCourseUnit) {
              this.handleError(null, 'education.ucDetail.notFound');
              this.students = [];
              this.loadingStudents = false;
              return;
            }
            this.loadCourseUnitStudents();
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

  trackByCourseUnit(index: number, uc: CourseUnit): string | number {
    return uc?.Id || uc?.EadId || index;
  }

  trackByStudent(index: number, student: StudentUcDto): string | number {
    return student?.Id || student?.Email || index;
  }

  trackByActivity(index: number, activity: any): number {
    return index;
  }

  hasActivities(): boolean {
    return this.students.some(s => s.Activities && s.Activities.length > 0);
  }

  getVisibleActivities(student: StudentUcDto): any[] {
    return (student.Activities || []).filter(a => !a.Hidden);
  }

  getAllActivities(): { Name: string; Hidden: boolean }[] {
    const map = new Map<string, boolean>();
    for (const student of this.students) {
      for (const act of student.Activities || []) {
        if (!map.has(act.Name)) {
          map.set(act.Name, !!act.Hidden);
        }
      }
    }
    return Array.from(map.entries()).map(([Name, Hidden]) => ({ Name, Hidden })).sort((a, b) => a.Name.localeCompare(b.Name));
  }

  getHiddenActivities(student: StudentUcDto): any[] {
    return (student.Activities || []).filter(a => a.Hidden);
  }

  hasHiddenActivities(student: StudentUcDto): boolean {
    return (student.Activities || []).some(a => a.Hidden);
  }

  getStudentName(student: StudentUcDto): string {
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

  private loadCourseUnitStudents(): void {
    if (!this.selectedCourseUnit?.EadId) {
      this.students = [];
      return;
    }

    this.loadingStudents = true;
    this.educationService.getCourseUnitStudents(this.selectedCourseUnit.EadId)
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
