export interface EducationClass {
  Id?: string;
  SchoolId: string;
  ProgramId: string;
  Name: string;
  CourseCategoryRaw: string;
  StartDate?: number | null;
  EndDate?: number | null;
}
