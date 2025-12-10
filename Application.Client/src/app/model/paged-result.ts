export interface PagedResult<T> {
  Items: T[];
  Total: number;
  PageNumber: number;
  PageSize: number;
}
