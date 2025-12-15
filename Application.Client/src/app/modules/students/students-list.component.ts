import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription, debounceTime } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { Student } from '../../model/student';
import { StudentsService } from '../../service/students/students.service';
import { NotificationService } from '../../service/notification/notification.service';
import { AuthService } from '../../service/auth/auth.service';
import { MANAGE_STUDENTS_PERMISSION } from '../../model/permissions';
import { StudentImportResult } from '../../model/student-import-result';
import { ModalService } from '../../shared/modal/modal.service';
import { LocalStorageService } from '../../shared/storage/local-storage.service';

type StatusFilter = 'all' | 'active' | 'suspended' | 'not_currently';
type ColumnKey = 'name' | 'email' | 'idNumber' | 'phone' | 'lastAccess' | 'status';

interface ColumnConfig {
  id: ColumnKey;
  labelKey: string;
  width: string;
  visible: boolean;
}

@Component({
  selector: 'app-students-list',
  templateUrl: './students-list.component.html',
  styleUrls: ['./students-list.component.scss']
})
export class StudentsListComponent implements OnInit, OnDestroy {
  private readonly columnsStorageKey = 'students.columns.preferences';
  students: Student[] = [];
  total = 0;
  pageNumber = 1;
  pageSize = 10;
  loading = false;
  importing = false;
  exporting = false;
  statusFilter: StatusFilter = 'all';
  searchControl = new FormControl('');
  showColumnMenu = false;
  columns: ColumnConfig[] = [
    { id: 'name', labelKey: 'students.list.columns.name', width: '1fr', visible: true },
    { id: 'email', labelKey: 'students.list.columns.email', width: '1fr', visible: true },
    { id: 'idNumber', labelKey: 'students.list.columns.idNumber', width: '1fr', visible: true },
    { id: 'phone', labelKey: 'students.list.columns.phone', width: '1fr', visible: true },
    { id: 'lastAccess', labelKey: 'students.list.columns.lastAccess', width: '1fr', visible: true },
    { id: 'status', labelKey: 'students.list.columns.status', width: '1fr', visible: true },
  ];
  private searchSub?: Subscription;

  constructor(
    private readonly studentsService: StudentsService,
    private readonly notificationService: NotificationService,
    public readonly translate: TranslateService,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly modalService: ModalService,
    private readonly localStorage: LocalStorageService,
  ) {}

  ngOnInit(): void {
    this.loadColumnPreferences();
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

  async onDelete(student: Student): Promise<void> {
    if (!student.Id) {
      this.notificationService.showError(this.translate.instant('students.list.missingId'), this.translate.instant('students.common.error'));
      return;
    }

    const confirmMessage = this.translate.instant('students.list.confirmDelete', { name: `${student.FirstName} ${student.LastName}`.trim() });
    const confirmed = await this.modalService.confirm({
      title: this.translate.instant('students.list.delete'),
      message: confirmMessage,
      confirmText: this.translate.instant('students.list.delete'),
      cancelText: this.translate.instant('modal.cancel'),
      icon: 'fa-solid fa-triangle-exclamation',
      destructive: true
    });

    if (!confirmed) {
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

  onImport(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.importing = true;
    this.studentsService.importStudents(file)
      .pipe(finalize(() => {
        this.importing = false;
        input.value = '';
      }))
      .subscribe({
        next: (result) => {
          this.handleImportResult(result);
          this.loadStudents();
        },
        error: (err) => {
          const detail = this.resolveErrorDetail(err, 'students.list.importError');
          this.notificationService.showError(detail, this.translate.instant('students.common.error'));
        }
      });
  }

  onExport(): void {
    this.exporting = true;
    this.studentsService.exportStudents()
      .pipe(finalize(() => this.exporting = false))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `alunos-${new Date().toISOString().replace(/[:.]/g, '-')}.xlsx`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          const detail = this.resolveErrorDetail(err, 'students.list.exportError');
          this.notificationService.showError(detail, this.translate.instant('students.common.error'));
        }
      });
  }

  trackByStudent(_: number, student: Student): string | undefined {
    return student.Id;
  }

  get visibleColumns(): ColumnConfig[] {
    return this.columns.filter(column => column.visible);
  }

  get gridTemplateColumns(): string {
    const count = this.visibleColumns.length || 1;
    return `repeat(${count}, 1fr) auto`;
  }

  toggleColumnMenu(event: Event): void {
    event.stopPropagation();
    this.showColumnMenu = !this.showColumnMenu;
  }

  toggleColumn(columnId: ColumnKey, event?: Event): void {
    event?.stopPropagation();
    const column = this.columns.find(c => c.id === columnId);
    if (!column) {
      return;
    }

    const visibleCount = this.visibleColumns.length;
    if (column.visible && visibleCount <= 1) {
      return;
    }

    column.visible = !column.visible;
    this.saveColumnPreferences();
  }

  get totalPages(): number {
    const pages = Math.ceil(this.total / this.pageSize);
    return Number.isFinite(pages) && pages > 0 ? pages : 1;
  }

  get canManageStudents(): boolean {
    return this.authService.hasPermission(MANAGE_STUDENTS_PERMISSION);
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

  private handleImportResult(result: StudentImportResult): void {
    const created = result?.Created ?? 0;
    const updated = result?.Updated ?? 0;
    this.notificationService.showSuccess(
      this.translate.instant('students.list.importSuccess', { created, updated }),
      this.translate.instant('students.list.importTitle')
    );

    const errors = result?.Errors || [];
    if (errors.length > 0) {
      const preview = errors.slice(0, 3)
        .map(error => this.translate.instant('students.list.importErrorRow', { row: error.Row, message: error.Message }))
        .join(' | ');

      this.notificationService.showWarning(
        preview,
        this.translate.instant('students.list.importWarning', { count: errors.length })
      );
    }
  }

  private resolveErrorDetail(err: any, fallbackKey: string): string {
    return err?.detail || err?.title || err?.message || this.translate.instant(fallbackKey);
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

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showColumnMenu = false;
  }

  private loadColumnPreferences(): void {
    const saved = this.localStorage.get<Partial<Record<ColumnKey, boolean>>>(this.columnsStorageKey);
    if (!saved) {
      return;
    }

    this.columns = this.columns.map(column => ({
      ...column,
      visible: saved[column.id] !== undefined ? !!saved[column.id] : column.visible
    }));
  }

  private saveColumnPreferences(): void {
    const prefs: Record<ColumnKey, boolean> = this.columns.reduce((acc, column) => {
      acc[column.id] = column.visible;
      return acc;
    }, {} as Record<ColumnKey, boolean>);

    this.localStorage.set(this.columnsStorageKey, prefs);
  }
}
