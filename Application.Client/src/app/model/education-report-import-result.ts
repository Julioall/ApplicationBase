export interface EducationReportImportResult {
  filesProcessed: number;
  rowsRead: number;
  programsCreated: number;
  programsUpdated: number;
  classesCreated: number;
  classesUpdated: number;
  ucsCreated: number;
  ucsUpdated: number;
  studentsCreated: number;
  studentsUpdated: number;
  ucLinksCreated: number;
  ucLinksUpdated: number;
  performanceRecordsUpserted: number;
  skippedRows: string[];
  errors: ImportError[];
}

export interface ImportError {
  rowNumber?: number;
  message?: string;
}
