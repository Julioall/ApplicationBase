import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { EducationService } from '../../service/education/education.service';
import { NotificationService } from '../../service/notification/notification.service';
import { NgxSpinnerService } from 'ngx-spinner';
import { EducationReportImportResult } from '../../model/education-report-import-result';

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
  importResult: EducationReportImportResult | null = null;

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

    this.educationService.importReport(this.selectedFiles).subscribe({
      next: (result) => {
        this.importResult = result;
        this.spinner.hide();
        this.isLoading = false;

        const message = `education.importReport.success`;
        this.notificationService.showSuccess(message);

        // Close dialog after 2 seconds
        setTimeout(() => {
          this.closeDialog();
        }, 2000);
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
      this.importResult = null;
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
