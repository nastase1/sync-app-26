export enum UserRole {
  Employee = 'employee',
  LineManager = 'line-manager'
}

export enum SyncStatus {
  Pending = 'pending',
  InProgress = 'in-progress',
  Synced = 'synced',
  Failed = 'failed',
  Conflict = 'conflict'
}

export interface User {
  id: string;
  name: string;
  dateOfBirth: Date;
  department: string;
  lineManagerId?: string;
  lineManagerName?: string;
  role: UserRole;
  email?: string;
}

export interface Department {
  id: string;
  name: string;
  lineManagerCount: number;
  employeeCount: number;
}

export interface UserComparison {
  id: string;
  status: 'new' | 'modified' | 'unchanged' | 'deleted';
  dbUser: User | null;
  csvUser: User | null;
  conflicts: FieldConflict[];
  selected: boolean;
}

export interface FieldConflict {
  field: keyof User;
  dbValue: any;
  csvValue: any;
  selectedValue?: 'db' | 'csv';
  selected: boolean; // Whether this field should be synced
}

export interface CsvImport {
  id: string;
  fileName: string;
  uploadedAt: Date;
  totalRecords: number;
  newRecords: number;
  modifiedRecords: number;
  unchangedRecords: number;
  conflicts: number;
  status: SyncStatus;
}

export interface SyncResult {
  success: boolean;
  recordsProcessed: number;
  recordsFailed: number;
  recordsSkipped: number;
  message?: string;
  errors?: string[];
}

export interface PaginationParams {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface SyncProgress {
  fileName: string;
  progress: number;
  currentRecord: number;
  totalRecords: number;
  status: SyncStatus;
}
