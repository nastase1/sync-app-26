import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, Subject, of } from 'rxjs';
import { map, delay, tap, catchError } from 'rxjs/operators';
import { User, UserRole, UserComparison, FieldConflict, CsvImport, SyncResult, SyncProgress, SyncStatus, Department } from '../models/csv-sync.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserSyncService {
  private apiUrl = environment.apiUrl + environment.endpoints.users;
  private syncProgressSubject = new BehaviorSubject<SyncProgress | null>(null);
  private currentComparisonSubject = new BehaviorSubject<UserComparison[] | null>(null);
  private usersSubject = new BehaviorSubject<User[]>([]);

  syncProgress$ = this.syncProgressSubject.asObservable();
  currentComparison$ = this.currentComparisonSubject.asObservable();
  users$ = this.usersSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUsers();
  }

  /**
   * Load users from API
   */
  private loadUsers(): void {
    this.getUsers().subscribe({
      next: (users) => {
        this.usersSubject.next(users);
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.usersSubject.next([]);
      }
    });
  }

  /**
   * Get all users from database
   */
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl).pipe(
      catchError(error => {
        console.error('Error fetching users:', error);
        return of([]);
      })
    );
  }

  /**
   * Upload CSV file and compare with database
   */
  uploadAndCompare(file: File): Observable<UserComparison[]> {
    const formData = new FormData();
    formData.append('file', file);
    const compareUrl = environment.apiUrl + environment.endpoints.usersCompare;
    return this.http.post<UserComparison[]>(compareUrl, formData).pipe(
      tap(comparisons => {
        this.currentComparisonSubject.next(comparisons);
      })
    );
  }

  /**
   * Sync selected users with resolved conflicts
   */
  syncUsers(comparisons: UserComparison[]): Observable<SyncResult> {
    const selected = comparisons.filter(c => c.selected);
    const syncUrl = environment.apiUrl + environment.endpoints.usersSync;
    
    return this.http.post<SyncResult>(syncUrl, { comparisons: selected }).pipe(
      tap(() => {
        this.currentComparisonSubject.next(null);
        this.loadUsers(); // Reload users after sync
      })
    );
  }

  /**
   * Get departments summary
   */
  getDepartments(): Observable<Department[]> {
    return this.users$.pipe(
      map(users => {
        const deptMap = new Map<string, { lineManagers: Set<string>, employees: Set<string> }>();
        
        users.forEach(user => {
          if (!deptMap.has(user.department)) {
            deptMap.set(user.department, { lineManagers: new Set(), employees: new Set() });
          }
          const dept = deptMap.get(user.department)!;
          if (user.role === UserRole.LineManager) {
            dept.lineManagers.add(user.id);
          } else {
            dept.employees.add(user.id);
          }
        });

        return Array.from(deptMap.entries()).map(([name, data]) => ({
          id: name.toLowerCase().replace(/\s+/g, '-'),
          name,
          lineManagerCount: data.lineManagers.size,
          employeeCount: data.employees.size
        }));
      })
    );
  }

  /**
   * Get sync statistics
   */
  getUserStats(): Observable<any> {
    return this.users$.pipe(
      map(users => ({
        total: users.length,
        lineManagers: users.filter(u => u.role === UserRole.LineManager).length,
        employees: users.filter(u => u.role === UserRole.Employee).length,
        departments: new Set(users.map(u => u.department)).size
      }))
    );
  }

  /**
   * Clear current comparison
   */
  clearComparison(): void {
    this.currentComparisonSubject.next(null);
  }
}
