import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface Department {
  id: string;
  name: string;
}

export interface DepartmentRequest {
  name: string;
}

export interface DepartmentResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private apiUrl = environment.apiUrl + environment.endpoints.departments;
  private departmentsSubject = new BehaviorSubject<Department[]>([]);
  
  departments$ = this.departmentsSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadDepartments();
  }

  /**
   * Load all departments from API
   */
  private loadDepartments(): void {
    this.getAllDepartments().subscribe({
      next: (departments) => {
        this.departmentsSubject.next(departments);
      },
      error: (error) => {
        console.error('Error loading departments:', error);
        this.departmentsSubject.next([]);
      }
    });
  }

  /**
   * Get all departments
   */
  getAllDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl).pipe(
      catchError(error => {
        console.error('Error fetching departments:', error);
        return of([]);
      })
    );
  }

  /**
   * Get department by ID
   */
  getDepartmentById(id: string): Observable<Department | null> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`).pipe(
      catchError(error => {
        console.error('Error fetching department:', error);
        return of(null);
      })
    );
  }

  /**
   * Create a new department
   */
  createDepartment(department: DepartmentRequest): Observable<DepartmentResponse> {
    return this.http.post<DepartmentResponse>(this.apiUrl, department).pipe(
      tap(() => {
        this.loadDepartments(); // Reload departments after creation
      }),
      catchError(error => {
        console.error('Error creating department:', error);
        return of({
          success: false,
          message: error.error?.message || 'Failed to create department'
        });
      })
    );
  }

  /**
   * Update an existing department
   */
  updateDepartment(id: string, department: DepartmentRequest): Observable<DepartmentResponse> {
    return this.http.put<DepartmentResponse>(`${this.apiUrl}/${id}`, department).pipe(
      tap(() => {
        this.loadDepartments(); // Reload departments after update
      }),
      catchError(error => {
        console.error('Error updating department:', error);
        return of({
          success: false,
          message: error.error?.message || 'Failed to update department'
        });
      })
    );
  }

  /**
   * Delete a department
   */
  deleteDepartment(id: string): Observable<DepartmentResponse> {
    return this.http.delete<DepartmentResponse>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this.loadDepartments(); // Reload departments after deletion
      }),
      catchError(error => {
        console.error('Error deleting department:', error);
        return of({
          success: false,
          message: error.error?.message || 'Failed to delete department'
        });
      })
    );
  }

  /**
   * Refresh departments list
   */
  refreshDepartments(): void {
    this.loadDepartments();
  }
}
