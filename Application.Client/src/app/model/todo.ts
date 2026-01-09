export enum TodoStatus {
  NotStarted = 0,
  InProgress = 1,
  Done = 2,
}

export enum TodoRecurrenceType {
  None = 0,
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
  Yearly = 4,
  Weekdays = 5,
}

export interface TodoRecurrence {
  Type: TodoRecurrenceType;
  Interval?: number | null;
  EndsOn?: string | null;
  DaysOfWeek?: number[] | null;
}

export interface TodoStep {
  Id: string;
  Title: string;
  IsCompleted: boolean;
  Order: number;
  CompletedAt?: string | null;
}

export interface TodoTask {
  Id: string;
  Title: string;
  Description?: string | null;
  Status: TodoStatus;
  Category?: string | null;
  Categories?: string[] | null;
  Priority?: number | null;
  StartDate?: string | null;
  DueDate?: string | null;
  CompletedAt?: string | null;
  ContextType?: string | null;
  ContextId?: string | null;
  CreatedByUserId?: string | null;
  CreatedAt: string;
  AssignedToUserId?: string | null;
  IsArchived: boolean;
  IsAllDay?: boolean;
  RecurrenceGroupId?: string | null;
  Recurrence?: TodoRecurrence | null;
  Steps: TodoStep[];
}

export interface TodoFilter {
  ContextType?: string;
  ContextId?: string;
  AssignedToUserId?: string;
  IncludeArchived?: boolean;
}

export interface CreateTodoStep {
  Title: string;
}

export interface UpdateTodoStep {
  Title?: string;
  IsCompleted?: boolean;
}

export interface ReorderTodoSteps {
  StepIds: string[];
}

export interface CreateTodoTask {
  Title: string;
  Description?: string | null;
  Status?: TodoStatus;
  Category?: string | null;
  Categories?: string[] | null;
  Priority?: number | null;
  StartDate?: string | null;
  DueDate?: string | null;
  ContextType?: string | null;
  ContextId?: string | null;
  AssignedToUserId?: string | null;
  IsAllDay?: boolean;
  Recurrence?: TodoRecurrence | null;
  Steps?: CreateTodoStep[];
}

export interface UpdateTodoTask {
  Title?: string | null;
  Description?: string | null;
  Status?: TodoStatus;
  Category?: string | null;
  Categories?: string[] | null;
  Priority?: number | null;
  StartDate?: string | null;
  DueDate?: string | null;
  ContextType?: string | null;
  ContextId?: string | null;
  AssignedToUserId?: string | null;
  IsAllDay?: boolean | null;
  Recurrence?: TodoRecurrence | null;
  ApplyToSeries?: boolean;
}

export interface TodoImageUpload {
  url: string;
  imageId: string;
}
