export enum TodoStatus {
  NotStarted = 0,
  InProgress = 1,
  Done = 2,
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
  Priority?: number | null;
  StartDate?: string | null;
  DueDate?: string | null;
  ContextType?: string | null;
  ContextId?: string | null;
  AssignedToUserId?: string | null;
  Steps?: CreateTodoStep[];
}

export interface UpdateTodoTask {
  Title?: string | null;
  Description?: string | null;
  Status?: TodoStatus;
  Category?: string | null;
  Priority?: number | null;
  StartDate?: string | null;
  DueDate?: string | null;
  ContextType?: string | null;
  ContextId?: string | null;
  AssignedToUserId?: string | null;
}
