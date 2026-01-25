import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, Subject, combineLatest } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { UserSyncService } from '../../services/user-sync.service';
import { User, UserComparison, UserRole, Department } from '../../models/csv-sync.model';
import { PaginationComponent } from '../pagination/pagination.component';
import { ComparisonViewComponent } from '../comparison-view/comparison-view.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent, ComparisonViewComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  users$!: Observable<User[]>;
  paginatedUsers$!: Observable<User[]>;
  stats$!: Observable<any>;
  departments$!: Observable<Department[]>;
  currentComparison$!: Observable<UserComparison[] | null>;
  
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  
  searchQuery = '';
  selectedDepartment = 'all';
  selectedRole: UserRole | 'all' = 'all';
  isUploading = false;
  isSyncing = false;
  showComparison = false;
  
  currentComparisons: UserComparison[] = [];
  
  UserRole = UserRole;

  constructor(
    private userSyncService: UserSyncService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.users$ = this.userSyncService.users$;
    this.stats$ = this.userSyncService.getUserStats();
    this.departments$ = this.userSyncService.getDepartments();
    this.currentComparison$ = this.userSyncService.currentComparison$;
    
    // Subscribe to comparison changes
    this.currentComparison$
      .pipe(takeUntil(this.destroy$))
      .subscribe(comparisons => {
        this.showComparison = comparisons !== null && comparisons.length > 0;
        this.currentComparisons = comparisons || [];
      });
    
    this.paginatedUsers$ = combineLatest([
      this.users$,
      this.stats$
    ]).pipe(
      map(([users, stats]) => {
        // Filter users
        let filtered = users.filter(user => {
          const matchesSearch = !this.searchQuery || 
            user.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
            user.email?.toLowerCase().includes(this.searchQuery.toLowerCase());
          const matchesDepartment = this.selectedDepartment === 'all' || 
            user.department === this.selectedDepartment;
          const matchesRole = this.selectedRole === 'all' || 
            user.role === this.selectedRole;
          return matchesSearch && matchesDepartment && matchesRole;
        });
        
        this.totalItems = filtered.length;
        
        // Paginate
        const startIndex = (this.currentPage - 1) * this.pageSize;
        return filtered.slice(startIndex, startIndex + this.pageSize);
      })
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.uploadFile(file);
    }
  }

  uploadFile(file: File): void {
    this.isUploading = true;
    this.userSyncService.uploadAndCompare(file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (comparisons) => {
          console.log('CSV uploaded and compared:', comparisons);
          this.isUploading = false;
          this.showComparison = true;
        },
        error: (error) => {
          console.error('Upload failed:', error);
          this.isUploading = false;
        }
      });
  }

  syncSelectedUsers(): void {
    this.isSyncing = true;
    this.userSyncService.syncUsers(this.currentComparisons)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          console.log('Sync successful:', result);
          this.isSyncing = false;
          this.showComparison = false;
        },
        error: (error) => {
          console.error('Sync failed:', error);
          this.isSyncing = false;
        }
      });
  }

  cancelComparison(): void {
    this.userSyncService.clearComparison();
    this.showComparison = false;
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  onDepartmentFilterChange(): void {
    this.currentPage = 1;
  }

  onRoleFilterChange(): void {
    this.currentPage = 1;
  }

  getRoleBadgeColor(role: UserRole): string {
    return role === UserRole.LineManager
      ? 'bg-purple-500/10 text-purple-700 border-purple-500/20'
      : 'bg-blue-500/10 text-blue-700 border-blue-500/20';
  }

  getRoleIcon(role: UserRole): string {
    return role === UserRole.LineManager ? '👔' : '👤';
  }

  formatDate(date: Date | string): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }

  onComparisonSelectionChange(comparisons: UserComparison[]): void {
    this.currentComparisons = comparisons;
  }

  onFieldConflictResolved(event: { comparisonId: string, field: string, value: 'db' | 'csv' }): void {
    console.log('Conflict resolved:', event);
  }

  getSelectedSyncCount(): number {
    return this.currentComparisons.filter(c => c.selected).length;
  }

  navigateToDepartments(): void {
    this.router.navigate(['/departments']);
  }

  navigateToUsers(): void {
    this.router.navigate(['/users']);
  }

  navigateToEmployees(): void {
    this.router.navigate(['/employees']);
  }
}
