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
import { ModalService } from '../../shared/modal/modal.service';
import { LocalStorageService } from '../../shared/storage/local-storage.service';

type StatusFilter = 'all' | 'active' | 'suspended' | 'not_currently';
type ColumnKey = 'name' | 'email' | 'idNumber' | 'phone' | 'lastAccess' | 'status';
type SortOption = 'lastAccessDesc' | 'lastAccessAsc' | 'nameAsc' | 'nameDesc';

interface ColumnConfig {
  id: ColumnKey;
  labelKey: string;
  width: string;
  visible: boolean;
}

@Component({
  selector: 'app-students-list',
  templateUrl: './students-list.component.html'
})
export class StudentsListComponent implements OnInit, OnDestroy {
  private readonly columnsStorageKey = 'students.columns.preferences';
  students: Student[] = [];
  total = 0;
  pageNumber = 1;
  pageSize = 10;
  loading = false;
  exporting = false;
  statusFilter: StatusFilter = 'all';
  searchControl = new FormControl('');
  showFilterDropdown = false;
  statusDropdownOpen = false;
  sortOption: SortOption = 'lastAccessDesc';
  selectedStudent: Student | null = null;
  statusFilters: { id: StatusFilter; labelKey: string; dotClass: string }[] = [
    { id: 'all', labelKey: 'students.list.statusAll', dotClass: 'bg-primary shadow-[0_0_0_4px_color-mix(in_srgb,_var(--primary)_18%,_transparent)]' },
    { id: 'active', labelKey: 'students.list.statusActive', dotClass: 'bg-success shadow-[0_0_0_4px_color-mix(in_srgb,_var(--success)_18%,_transparent)]' },
    { id: 'not_currently', labelKey: 'students.list.statusInactive', dotClass: 'bg-warning shadow-[0_0_0_4px_color-mix(in_srgb,_var(--warning)_18%,_transparent)]' },
    { id: 'suspended', labelKey: 'students.status.suspended', dotClass: 'bg-danger shadow-[0_0_0_4px_color-mix(in_srgb,_var(--danger)_16%,_transparent)]' },
  ];
  columns: ColumnConfig[] = [
    { id: 'name', labelKey: 'Nome', width: '1fr', visible: true },
    { id: 'email', labelKey: 'Email', width: '1fr', visible: false },
    { id: 'idNumber', labelKey: 'CPF', width: '1fr', visible: false },
    { id: 'phone', labelKey: 'Telefone', width: '1fr', visible: false },
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
        this.students = this.applySort(this.filterByStatus(items));
        this.total = (this.statusFilter === 'suspended' || this.statusFilter === 'not_currently')
          ? this.students.length
          : result.Total;
        if (this.selectedStudent) {
          this.selectedStudent = this.students.find(student => student.Id === this.selectedStudent?.Id) || null;
        }
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
        if (this.selectedStudent?.Id === student.Id) {
          this.closeSidePanel();
        }
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

  onExport(): void {
    this.exporting = true;
    this.studentsService.exportStudents()
      .pipe(finalize(() => this.exporting = false))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          const prefix = this.translate.instant('students.list.exportFilenamePrefix') || 'students';
          link.download = `${prefix}-${new Date().toISOString().replace(/[:.]/g, '-')}.xlsx`;
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

  getStatusFilterLabel(filter: StatusFilter): string {
    const key = this.statusFilters.find(f => f.id === filter)?.labelKey;
    return key ? this.translate.instant(key) : filter;
  }

  getStatusFilterDotClass(filter: StatusFilter): string {
    return this.statusFilters.find(f => f.id === filter)?.dotClass || 'bg-primary';
  }

  toggleFilterDropdown(event?: Event): void {
    event?.stopPropagation();
    this.showFilterDropdown = !this.showFilterDropdown;
    if (!this.showFilterDropdown) {
      this.statusDropdownOpen = false;
    }
  }

  toggleStatusDropdown(event: Event): void {
    event.stopPropagation();
    this.statusDropdownOpen = !this.statusDropdownOpen;
  }

  selectStatus(filter: StatusFilter): void {
    this.setStatusFilter(filter);
    this.statusDropdownOpen = false;
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

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter = filter;
    this.pageNumber = 1;
    this.loadStudents();
  }

  setSort(option: SortOption): void {
    this.sortOption = option;
    this.students = this.applySort([...this.students]);
  }

  openSidePanel(student: Student): void {
    this.selectedStudent = student;
  }

  closeSidePanel(): void {
    this.selectedStudent = null;
  }

  isColumnVisible(columnId: ColumnKey): boolean {
    if (columnId === 'lastAccess' || columnId === 'status') {
      return true;
    }
    return this.visibleColumns.some(column => column.id === columnId);
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
    this.showFilterDropdown = false;
    this.statusDropdownOpen = false;
  }

  private applySort(items: Student[]): Student[] {
    if (this.sortOption === 'nameAsc' || this.sortOption === 'nameDesc') {
      return [...items].sort((a, b) => {
        const nameA = `${a.FirstName || ''} ${a.LastName || ''}`.trim().toLocaleLowerCase();
        const nameB = `${b.FirstName || ''} ${b.LastName || ''}`.trim().toLocaleLowerCase();
        const result = nameA.localeCompare(nameB);
        return this.sortOption === 'nameAsc' ? result : -result;
      });
    }

    // default: order by last access desc/asc, fallback to name asc
    return [...items].sort((a, b) => {
      const aDate = a.LastAccessAt ? new Date(a.LastAccessAt).getTime() : 0;
      const bDate = b.LastAccessAt ? new Date(b.LastAccessAt).getTime() : 0;
      if (aDate !== bDate) {
        return this.sortOption === 'lastAccessAsc' ? aDate - bDate : bDate - aDate;
      }
      const nameA = `${a.FirstName || ''} ${a.LastName || ''}`.trim().toLocaleLowerCase();
      const nameB = `${b.FirstName || ''} ${b.LastName || ''}`.trim().toLocaleLowerCase();
      return nameA.localeCompare(nameB);
    });
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
