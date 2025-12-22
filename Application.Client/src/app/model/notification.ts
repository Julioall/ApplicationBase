export interface Notification {
  Id: string;
  Title: string;
  Message: string;
  Type: 'success' | 'error' | 'warning' | 'info' | string;
  IsRead: boolean;
  CreatedAt: string;
  ReferenceId?: string | null;
  ReferenceType?: string | null;
  Link?: string | null;
}

export interface NotificationListResult {
  Items: Notification[];
  UnreadCount: number;
}
