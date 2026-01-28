export interface MoodleCourse {
  Id?: string;
  EadId: number;
  Fullname: string;
  StartDate: number;
  EndDate: number;
  ViewUrl?: string;
  CourseImage?: string;
  CourseCategory?: string;
  CategoryNameDerived?: string;
  CourseCategoryNameDerived?: string;
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
