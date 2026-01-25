import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { UserSyncService } from '../../services/user-sync.service';
import { User, UserRole, Department } from '../../models/csv-sync.model';
import { PaginationComponent } from '../pagination/pagination.component';

@Component({
  selector: 'app-employees-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './employees-detail.component.html',
  styleUrls: ['./employees-detail.component.css']
})
export class EmployeesDetailComponent implements OnInit {
  users$!: Observable<User[]>;
  paginatedUsers$!: Observable<User[]>;
  departments$!: Observable<Department[]>;
  selectedUser: User | null = null;
  
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  
  searchQuery = '';
  selectedDepartment = 'all';
  
  UserRole = UserRole;

  constructor(
    private userSyncService: UserSyncService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.users$ = this.userSyncService.users$;
    this.departments$ = this.userSyncService.getDepartments();
    
    // Check if specific user ID in route params
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.users$.subscribe(users => {
          this.selectedUser = users.find(u => u.id === params['id']) || null;
        });
      }
    });
    
    this.paginatedUsers$ = this.users$.pipe(
      map(users => {
        // Filter users
        let filtered = users.filter(user => {
          const matchesSearch = !this.searchQuery || 
            user.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
            user.email?.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
            user.department.toLowerCase().includes(this.searchQuery.toLowerCase());
          const matchesDepartment = this.selectedDepartment === 'all' || 
            user.department === this.selectedDepartment;
          return matchesSearch && matchesDepartment;
        });
        
        this.totalItems = filtered.length;
        
        // Paginate
        const startIndex = (this.currentPage - 1) * this.pageSize;
        return filtered.slice(startIndex, startIndex + this.pageSize);
      })
    );
  }

  selectUser(user: User): void {
    this.selectedUser = user;
  }

  closeDetails(): void {
    this.selectedUser = null;
    this.router.navigate(['/employees']);
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

  formatDate(date: Date | string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('ro-RO');
  }

  getRoleBadgeColor(role: UserRole): string {
    return role === UserRole.LineManager
      ? 'bg-purple-500/10 text-purple-700 border-purple-500/20'
      : 'bg-blue-500/10 text-blue-700 border-blue-500/20';
  }

  navigateToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  navigateToUsers(): void {
    this.router.navigate(['/users']);
  }

  navigateToDepartments(): void {
    this.router.navigate(['/departments']);
  }
}
