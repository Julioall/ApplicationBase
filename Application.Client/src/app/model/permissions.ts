export const DEFAULT_USER_PERMISSIONS = ['view:home', 'view:profile'];
export const ADMIN_PERMISSION = 'manage:users';
export const ALL_PERMISSIONS = [
  ...DEFAULT_USER_PERMISSIONS,
  ADMIN_PERMISSION,
];
