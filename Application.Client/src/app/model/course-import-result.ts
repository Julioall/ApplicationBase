export interface CourseImportResult {
  Processed: number;
  CreatedSchools: number;
  CreatedPrograms: number;
  CreatedClasses: number;
  CreatedUcs: number;
  UpdatedUcs: number;
  Linked: number;
  Errors: CourseImportError[];
}

export interface CourseImportError {
  CourseCategory?: string;
  UcName?: string;
  Message: string;
}
