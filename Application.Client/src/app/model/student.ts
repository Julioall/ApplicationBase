import { Address } from './address';

export interface Student {
  Id?: string;
  FirstName: string;
  LastName: string;
  Email?: string;
  IdNumber?: string;
  Phone?: string;
  DateOfBirth?: string | null;
  Address?: Address;
  Institution?: string;
  Lang?: string;
  TimeZone?: string;
  IsActive: boolean;
  CreatedAt?: string;
  UpdatedAt?: string | null;
}
