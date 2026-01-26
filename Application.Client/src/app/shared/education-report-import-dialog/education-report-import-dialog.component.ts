import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { EducationService } from '../../service/education/education.service';
import { NotificationService } from '../../service/notification/notification.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { EducationReportImport } from '../../model/education-report-import';

@Component({
  selector: 'app-education-report-import-dialog',
  templateUrl: './education-report-import-dialog.component.html',
  styleUrls: ['./education-report-import-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule],
})
export class EducationReportImportDialogComponent implements OnInit {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  selectedFiles: File[] = [];
  isLoading = false;
  queuedImport: EducationReportImport | null = null;

  constructor(
    private educationService: EducationService,
    private notificationService: NotificationService,
    private spinner: NgxSpinnerService,
  ) {}

  ngOnInit(): void {
    // Initialize component if needed
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    const files = target.files;
    if (files) {
      this.selectedFiles = Array.from(files);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const files = event.dataTransfer?.files;
    if (files) {
      this.selectedFiles = Array.from(files).filter((f) => f.name.endsWith('.xlsx'));
    }
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
  }

  clearFiles(): void {
    this.selectedFiles = [];
  }

  async importReport(): Promise<void> {
    if (this.selectedFiles.length === 0) {
      this.notificationService.showError('education.importReport.selectFiles');
      return;
    }

    this.isLoading = true;
    await this.spinner.show();
    this.queuedImport = null;

    this.educationService.importReport(this.selectedFiles).subscribe({
      next: (importInfo) => {
        this.spinner.hide();
        this.isLoading = false;
        this.queuedImport = importInfo;
        this.notificationService.showSuccess('education.importReport.queued');
        this.clearFiles();
      },
      error: (error) => {
        this.spinner.hide();
        this.isLoading = false;
        console.error('Import failed:', error);
        this.notificationService.showError('education.importReport.error');
      },
    });
  }

  closeDialog(): void {
    if (!this.isLoading) {
      this.selectedFiles = [];
      this.queuedImport = null;
      this.close.emit();
    }
  }

  get hasFiles(): boolean {
    return this.selectedFiles.length > 0;
  }

  get filesInfo(): string {
    return this.selectedFiles.map((f) => f.name).join(', ');
  }
}
