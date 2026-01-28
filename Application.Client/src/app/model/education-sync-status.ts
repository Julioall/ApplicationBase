export interface EducationSyncStatus {
  Id?: string;
  LastSyncAt?: number | null;
  ExpiresAt?: number | null;
  Status?: string | null;
  Message?: string | null;
  TriggeredByUserId?: string | null;
  TriggeredByName?: string | null;
  TriggeredAt?: number | null;
}
