export const DEFAULT_USER_PERMISSIONS = ['view:home', 'view:profile'];
export const ADMIN_PERMISSION = 'manage:users';
export const VIEW_STUDENTS_PERMISSION = 'view:students';
export const MANAGE_STUDENTS_PERMISSION = 'manage:students';
export const VIEW_EDUCATION_PERMISSION = 'view:education';
export const MANAGE_EDUCATION_PERMISSION = 'manage:education';
export const ALL_PERMISSIONS = [
  ...DEFAULT_USER_PERMISSIONS,
  ADMIN_PERMISSION,
  VIEW_STUDENTS_PERMISSION,
  MANAGE_STUDENTS_PERMISSION,
   VIEW_EDUCATION_PERMISSION,
   MANAGE_EDUCATION_PERMISSION,
];
