export enum ActivityType {
  RequiresCorrection = 0,
  AutoGraded = 1,
  Participation = 2,
  Unknown = 99
}

export interface StudentActivity {
  Name: string;
  FinalGrade?: number | null;
  SubmittedAt?: string | null;
  CorrectedAt?: string | null;
  SubmissionStatus?: string;
  Restriction?: string;
  StartAt?: string | null;
  EndAt?: string | null;
  Type: number; // ActivityType
  CorrectionStatus: string;
  IsPendingCorrection: boolean;
  HasRestriction: boolean;
  IsLate: boolean;
  Hidden: boolean;
}

export interface StudentUcDto {
  Id: string;
  FirstName: string;
  LastName: string;
  Email?: string;
  IdNumber?: string;
  Phone?: string;
  Institution?: string;
  IsActive: boolean;
  Status?: string;
  LastAccessAt?: string | null;
  CreatedAt?: string;
  UpdatedAt?: string | null;
  
  // Performance na UC
  FinalGrade?: number | null;
  Activities: StudentActivity[];
}
