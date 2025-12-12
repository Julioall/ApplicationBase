import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription, debounceTime } from 'rxjs';
import { Student } from '../../model/student';
import { StudentsService } from '../../service/students/students.service';
import { NotificationService } from '../../service/notification/notification.service';

type StatusFilter = 'all' | 'active' | 'suspended' | 'not_currently';

@Component({
  selector: 'app-students-list',
  templateUrl: './students-list.component.html',
  styleUrls: ['./students-list.component.scss']
})
export class StudentsListComponent implements OnInit, OnDestroy {
  students: Student[] = [];
  total = 0;
  pageNumber = 1;
  pageSize = 10;
  loading = false;
  statusFilter: StatusFilter = 'all';
  searchControl = new FormControl('');
  private searchSub?: Subscription;

  constructor(
    private readonly studentsService: StudentsService,
    private readonly notificationService: NotificationService,
    public readonly translate: TranslateService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadStudents();
    this.searchSub = this.searchControl.valueChanges
      .pipe(debounceTime(300))
      .subscribe(() => {
        this.pageNumber = 1;
        this.loadStudents();
      });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  loadStudents(): void {
    this.loading = true;
    const search = (this.searchControl.value ?? '').toString().trim();
    const isActive = this.statusFilter === 'all' ? undefined : this.statusFilter === 'active';

    this.studentsService.getStudents({
      PageNumber: this.pageNumber,
      PageSize: this.pageSize,
      Search: search || undefined,
      IsActive: isActive
    }).subscribe({
      next: (result) => {
        const items = result.Items || [];
        this.students = this.filterByStatus(items);
        this.total = (this.statusFilter === 'suspended' || this.statusFilter === 'not_currently')
          ? this.students.length
          : result.Total;
        this.pageNumber = result.PageNumber || this.pageNumber;
        this.pageSize = result.PageSize || this.pageSize;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        const detail = err?.detail || err?.title || err?.message || this.translate.instant('students.list.loadError');
        this.notificationService.showError(detail, this.translate.instant('students.common.error'));
      }
    });
  }

  onAdd(): void {
    this.router.navigate(['/students/new']);
  }

  onEdit(student: Student): void {
    if (!student.Id) {
      this.notificationService.showError(this.translate.instant('students.list.missingId'), this.translate.instant('students.common.error'));
      return;
    }
    this.router.navigate(['/students', student.Id, 'edit']);
  }

  onDelete(student: Student): void {
    if (!student.Id) {
      this.notificationService.showError(this.translate.instant('students.list.missingId'), this.translate.instant('students.common.error'));
      return;
    }

    const confirmMessage = this.translate.instant('students.list.confirmDelete', { name: `${student.FirstName} ${student.LastName}`.trim() });
    if (!confirm(confirmMessage)) {
      return;
    }

    this.loading = true;
    this.studentsService.deleteStudent(student.Id).subscribe({
      next: () => {
        this.notificationService.showSuccess(this.translate.instant('students.list.deleteSuccess'));
        this.loadStudents();
      },
      error: (err) => {
        this.loading = false;
        const detail = err?.detail || err?.title || err?.message || this.translate.instant('students.list.deleteError');
        this.notificationService.showError(detail, this.translate.instant('students.common.error'));
      }
    });
  }

  changePage(delta: number): void {
    const totalPages = Math.max(1, Math.ceil(this.total / this.pageSize));
    this.pageNumber = Math.min(Math.max(1, this.pageNumber + delta), totalPages);
    this.loadStudents();
  }

  trackByStudent(_: number, student: Student): string | undefined {
    return student.Id;
  }

  get totalPages(): number {
    const pages = Math.ceil(this.total / this.pageSize);
    return Number.isFinite(pages) && pages > 0 ? pages : 1;
  }

  getStatusLabel(student: Student): string {
    const status = (student.Status || (student.IsActive ? 'active' : 'not_currently')).toLowerCase();
    switch (status) {
      case 'suspended':
        return this.translate.instant('students.status.suspended');
      case 'not_currently':
        return this.translate.instant('students.status.notCurrently');
      default:
        return this.translate.instant('students.status.active');
    }
  }

  getStatusClass(student: Student): string {
    const status = (student.Status || (student.IsActive ? 'active' : 'not_currently')).toLowerCase();
    if (status === 'suspended') {
      return 'suspended';
    }
    if (status === 'not_currently') {
      return 'inactive';
    }
    return 'active';
  }

  private filterByStatus(items: Student[]): Student[] {
    const filter = this.statusFilter;
    if (filter === 'all') {
      return items;
    }

    return items.filter(student => {
      const status = (student.Status || (student.IsActive ? 'active' : 'not_currently')).toLowerCase();
      if (filter === 'active') {
        return status === 'active';
      }

      if (filter === 'suspended') {
        return status === 'suspended';
      }

      return status === 'not_currently';
    });
  }
}
