// Alinha com o payload PascalCase retornado pela API (JsonSerializer sem naming policy)
export interface EducationReportImport {
  Id: string;
  Status: string;
  FileNames: string[];
  CreatedAt: string;
}
