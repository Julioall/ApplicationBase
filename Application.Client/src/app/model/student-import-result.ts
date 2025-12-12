export interface StudentImportError {
  Row: number;
  Message: string;
}

export interface StudentImportResult {
  Processed: number;
  Created: number;
  Updated: number;
  Skipped: number;
  Errors: StudentImportError[];
}
