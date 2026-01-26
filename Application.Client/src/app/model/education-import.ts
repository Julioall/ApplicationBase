export interface EducationImport {
  Id: string;
  FileName: string;
  Status: string;
  CreatedAt: string;
  StartedAt?: string | null;
  CompletedAt?: string | null;
  Processed: number;
  CreatedUcs: number;
  UpdatedUcs: number;
  Linked: number;
  ErrorMessage?: string | null;
}
