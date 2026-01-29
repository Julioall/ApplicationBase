/**
 * Represents a CourseUnit from Moodle.
 * Corresponds to an individual course within a class/event.
 */
export interface CourseUnit {
  Id?: string;
  EadId: number;
  Fullname: string;
  StartDate: number;
  EndDate: number;
  ViewUrl?: string;
  CourseImage?: string;
  CourseCategory?: string;

  // Moodle category hierarchy
  // Path: /InstitutionId/SchoolId/CourseId/EventId
  InstitutionName?: string;
  InstitutionMoodleId?: number;
  SchoolName?: string;
  SchoolMoodleId?: number;
  CourseName?: string;
  CourseMoodleId?: number;
  EventName?: string;
  EventMoodleId?: number;

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
