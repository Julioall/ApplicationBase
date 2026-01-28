export interface MoodleCohort {
  Id?: string;
  CategoryId: string;
  CourseCategoryId: string;
  Name: string;
  CourseCategoryRaw: string;
  StartDate?: number | null;
  EndDate?: number | null;
}
