export interface EducationUc {
  Id?: string;
  EadId: number;
  Fullname: string;
  StartDate: number;
  EndDate: number;
  ViewUrl?: string;
  CourseImage?: string;
  CourseCategory?: string;
  SchoolNameDerived?: string;
  ProgramNameDerived?: string;
  PeriodTextDerived?: string;
  Progress?: number;
  Completed?: boolean;
  IsFavourite?: boolean;
  Hidden?: boolean;
  Summary?: string;
  LastAccess?: number;
  IdNumber?: string;
  Lang?: string;
}
