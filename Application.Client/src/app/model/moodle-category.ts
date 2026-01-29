/**
 * Represents a Moodle category in the hierarchy.
 * Depth determines the level:
 * - Depth 1: Institution (SENAI, SESI)
 * - Depth 2: School (Escola SENAI Vila Canaã)
 * - Depth 3: Course/Program (Operador de Computador)
 * - Depth 4: Event/Class (1003121 - 00003/2025)
 */
export interface MoodleCategory {
  Id?: string;
  MoodleId: number;
  Name: string;
  ParentId: number;
  Depth: number;
  Path?: string;
  LastSyncedAt?: number;
  // Optional fields used for Depth 4 (Class) items
  StartDate?: number;
  EndDate?: number;
  SchoolId?: string;
  ProgramId?: string;
  CourseCategoryRaw?: string;
  PeriodTextDerived?: string;
}

/** Helper type aliases for semantic clarity */
export type Institution = MoodleCategory; // Depth 1
export type School = MoodleCategory;      // Depth 2
export type Program = MoodleCategory;     // Depth 3
export type Class = MoodleCategory;       // Depth 4
