import { Address } from './address';

export interface Student {
  Id?: string;
  FirstName: string;
  LastName: string;
  Email?: string;
  IdNumber?: string;
  Phone?: string;
  Address?: Address;
  Institution?: string;
  Lang?: string;
  TimeZone?: string;
  Status?: 'active' | 'suspended' | 'not_currently';
  IsActive: boolean;
  CreatedAt?: string;
  UpdatedAt?: string | null;
  LastAccessAt?: string | null;
}
