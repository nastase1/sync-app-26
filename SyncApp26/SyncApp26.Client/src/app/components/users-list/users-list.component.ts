import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Observable, combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';
import { UserSyncService } from '../../services/user-sync.service';
import { User, UserRole, Department } from '../../models/csv-sync.model';
import { PaginationComponent } from '../pagination/pagination.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.css']
})
export class UsersListComponent implements OnInit {
  users$!: Observable<User[]>;
  paginatedUsers$!: Observable<User[]>;
  departments$!: Observable<Department[]>;
  
  currentPage = 1;
  pageSize = 15;
  totalItems = 0;
  
  searchQuery = '';
  selectedDepartment = 'all';
  selectedRole: UserRole | 'all' = 'all';
  
  UserRole = UserRole;

  constructor(
    private userSyncService: UserSyncService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.users$ = this.userSyncService.users$;
    this.departments$ = this.userSyncService.getDepartments();
    
    // Check for department filter from query params
    this.route.queryParams.subscribe(params => {
      if (params['department']) {
        this.selectedDepartment = params['department'];
      }
    });
    
    this.paginatedUsers$ = this.users$.pipe(
      map(users => {
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

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  onSearchChange(): void {
    this.currentPage = 1;
  }

  onFilterChange(): void {
    this.currentPage = 1;
  }

  viewUserDetails(userId: string): void {
    this.router.navigate(['/employees', userId]);
  }

  getRoleBadgeColor(role: UserRole): string {
    return role === UserRole.LineManager
      ? 'bg-purple-500/10 text-purple-700 border-purple-500/20'
      : 'bg-blue-500/10 text-blue-700 border-blue-500/20';
  }

  navigateToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  navigateToDepartments(): void {
    this.router.navigate(['/departments']);
  }

  navigateToEmployees(): void {
    this.router.navigate(['/employees']);
  }
}
