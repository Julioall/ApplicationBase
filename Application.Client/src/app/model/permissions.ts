export const DEFAULT_USER_PERMISSIONS = ['view:home', 'view:profile'];
export const ADMIN_PERMISSION = 'manage:users';
export const MANAGE_SERVICES_PERMISSION = 'manage:services';
export const MANAGE_EMAIL_PERMISSION = 'manage:email';
export const MANAGE_WHATSAPP_PERMISSION = 'manage:whatsapp';
export const MANAGE_WHATSAPP_SELF_PERMISSION = 'manage:whatsapp-self';
export const VIEW_TODO_PERMISSION = 'view:todo';
export const MANAGE_TODO_PERMISSION = 'manage:todo';
export const ALL_PERMISSIONS = [
  ...DEFAULT_USER_PERMISSIONS,
  ADMIN_PERMISSION,
  MANAGE_SERVICES_PERMISSION,
  MANAGE_EMAIL_PERMISSION,
  MANAGE_WHATSAPP_PERMISSION,
  MANAGE_WHATSAPP_SELF_PERMISSION,
  VIEW_TODO_PERMISSION,
  MANAGE_TODO_PERMISSION,
];
