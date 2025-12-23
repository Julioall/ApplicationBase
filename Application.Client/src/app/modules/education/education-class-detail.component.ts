import { Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { EducationService } from '../../service/education/education.service';
import { EducationUc } from '../../model/education-uc';
import { NotificationService } from '../../service/notification/notification.service';

@Component({
  selector: 'app-education-class-detail',
  templateUrl: './education-class-detail.component.html',
  styleUrls: ['./education-class-detail.component.scss']
})
export class EducationClassDetailComponent implements OnInit {
  classId = '';
  className = '';
  programName = '';
  schoolName = '';
  classPeriodText = '';
  units: EducationUc[] = [];
  loadingUnits = false;
  loadingMetadata = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly educationService: EducationService,
    private readonly translate: TranslateService,
    private readonly notificationService: NotificationService,
    private readonly location: Location
  ) {}

  ngOnInit(): void {
    this.classId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.classId) {
      this.handleError(null, 'education.errors.loadClasses');
      return;
    }

    this.resolveMetadata();
    this.loadUnits();
  }

  goBack(): void {
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

  private resolveMetadata(): void {
    const query = this.route.snapshot.queryParamMap;
    this.className = query.get('className') || this.className;
    this.programName = query.get('programName') || this.programName;
    this.schoolName = query.get('schoolName') || this.schoolName;

    const programId = query.get('programId');
    const schoolId = query.get('schoolId');

    if (programId) {
      this.loadingMetadata = true;
      this.educationService.getClasses(programId)
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
              if (!this.programName && schoolId) {
                this.loadProgramName(schoolId, found.ProgramId);
              }
            }
          },
          error: (err) => this.handleError(err, 'education.errors.loadClasses')
        });
    } else if (schoolId) {
      this.loadSchoolName(schoolId);
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

  private handleError(err: any, translationKey: string): void {
    const detail = err?.detail || err?.title || err?.message || this.translate.instant(translationKey);
    this.notificationService.showError(detail, this.translate.instant('education.labels.error'));
  }
}
