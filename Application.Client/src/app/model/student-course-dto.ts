export interface StudentCourseDto {
  Id: string;
  FirstName: string;
  LastName: string;
  Email?: string;
  IdNumber?: string;
  Phone?: string;
  Institution?: string;
  IsActive: boolean;
  Status?: string;
  LastAccessAt?: Date;
  CreatedAt?: Date;
  UpdatedAt?: Date;
  FinalGrade?: number;
  Activities: StudentActivityDto[];
}

export interface StudentActivityDto {
  Name: string;
  FinalGrade?: number;
  SubmittedAt?: Date;
  CorrectedAt?: Date;
  SubmissionStatus?: string;
  Restriction?: string;
  StartAt?: Date;
  EndAt?: Date;
  Type: number;
  CorrectionStatus: string;
  IsPendingCorrection: boolean;
  HasRestriction: boolean;
  IsLate: boolean;
  Hidden: boolean;
}
