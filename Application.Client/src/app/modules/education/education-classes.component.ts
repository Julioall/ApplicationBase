import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription, debounceTime } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { EducationService } from '../../service/education/education.service';
import { CourseUnit } from '../../model/course-unit';
import { PagedResult } from '../../model/paged-result';
import { NotificationService } from '../../service/notification/notification.service';

@Component({
  selector: 'app-education-classes',
  templateUrl: './education-classes.component.html'
})
export class EducationClassesComponent implements OnInit, OnDestroy {
  ucs: CourseUnit[] = [];
  groupedUcs: { category: string; items: CourseUnit[] }[] = [];
  loadingUcs = false;
  searchControl = new FormControl('');
  private searchSub?: Subscription;

  constructor(
    private readonly educationService: EducationService,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadUcs();
    this.searchSub = this.searchControl.valueChanges
      .pipe(debounceTime(300))
      .subscribe(() => this.loadUcs());
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  openClassGroup(group: { category: string; items: CourseUnit[] }): void {
    const category = group.category;
    const classId = this.slugify(category);
    if (!classId) {
      return;
    }

    const sample = group.items[0];
    const queryParams = {
      className: category,
      programName: sample?.CourseName || undefined,
      schoolName: sample?.SchoolName || sample?.EventName || undefined
    };

    this.router.navigate(['/education', 'classes', classId], { queryParams });
  }

  trackByUc(_: number, uc: CourseUnit): string | number {
    return uc.Id || uc.MoodleId;
  }

  getGroupMeta(group: { category: string; items: CourseUnit[] }): string {
    if (!group.items.length) {
      return this.translate.instant('classes.countLabel', { count: 0 });
    }

    const sample = group.items[0];
    const program = sample.CourseName || '-';
    const school = sample.SchoolName || sample.EventName || '-';
    const count = this.translate.instant('classes.countLabel', { count: group.items.length });
    return `${program} • ${school} • ${count}`;
  }

  private loadUcs(): void {
    this.loadingUcs = true;
    const search = (this.searchControl.value ?? '').toString().trim();

    this.educationService.searchCourseUnits({ PageNumber: 1, PageSize: 50, Search: search || undefined })
      .pipe(finalize(() => this.loadingUcs = false))
      .subscribe({
        next: (result: PagedResult<CourseUnit>) => {
          this.ucs = result.Items || [];
          this.groupedUcs = this.buildGroups(this.ucs);
        },
        error: (err) => this.handleError(err, 'classes.errors.loadUnits')
      });
  }

  private handleError(err: any, translationKey: string): void {
    const detail = err?.detail || err?.title || err?.message || this.translate.instant(translationKey);
    this.notificationService.showError(detail, this.translate.instant('education.labels.error'));
  }

  private buildGroups(items: CourseUnit[]): { category: string; items: CourseUnit[] }[] {
    const groups = new Map<string, CourseUnit[]>();
    items.forEach(item => {
      const category = (item.CourseCategory || '').trim() || this.translate.instant('classes.uncategorized');
      const list = groups.get(category) ?? [];
      list.push(item);
      groups.set(category, list);
    });

    return Array.from(groups.entries())
      .map(([category, list]) => ({
        category,
        items: list.sort((a, b) => a.Fullname.localeCompare(b.Fullname))
      }))
      .sort((a, b) => a.category.localeCompare(b.category));
  }

  private slugify(value: string): string {
    const normalized = value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    return normalized || 'item';
  }
}
