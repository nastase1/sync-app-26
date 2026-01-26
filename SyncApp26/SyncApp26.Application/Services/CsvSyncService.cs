using SyncApp26.Application.IServices;
using SyncApp26.Shared.DTOs.Response.User;

namespace SyncApp26.Application.Services
{
    public class CsvSyncService : ICsvSyncService
    {
        private readonly IUserService _userService;
        private readonly IDepartmentService _departmentService;

        public CsvSyncService(IUserService userService, IDepartmentService departmentService)
        {
            _userService = userService;
            _departmentService = departmentService;
        }

        public async Task<List<UserComparisonDTO>> CompareUsersAsync(List<CsvUserDTO> csvUsers)
        {
            var comparisons = new List<UserComparisonDTO>();
            var dbUsers = await _userService.GetAllUsersAsync();
            var departments = await _departmentService.GetAllDepartmentsAsync();

            // Process CSV users
            foreach (var csvUser in csvUsers)
            {
                // Find matching DB user by email or name
                var dbUser = dbUsers.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(csvUser.Email) && u.Email.Equals(csvUser.Email, StringComparison.OrdinalIgnoreCase)) ||
                    ($"{u.FirstName} {u.LastName}".Equals(csvUser.Name, StringComparison.OrdinalIgnoreCase)));

                if (dbUser == null)
                {
                    // New user
                    comparisons.Add(new UserComparisonDTO
                    {
                        Id = csvUser.Id,
                        Status = "new",
                        DbUser = null,
                        CsvUser = csvUser,
                        Conflicts = new List<FieldConflictDTO>(),
                        Selected = true
                    });
                }
                else
                {
                    // Existing user - check for modifications
                    var conflicts = DetectConflicts(dbUser, csvUser, departments);

                    comparisons.Add(new UserComparisonDTO
                    {
                        Id = dbUser.Id.ToString(),
                        Status = conflicts.Any() ? "modified" : "unchanged",
                        DbUser = MapUserToDto(dbUser, departments),
                        CsvUser = csvUser,
                        Conflicts = conflicts,
                        Selected = conflicts.Any()
                    });
                }
            }

            // Check for deleted users (in DB but not in CSV)
            foreach (var dbUser in dbUsers)
            {
                var existsInCsv = csvUsers.Any(cu =>
                    (!string.IsNullOrEmpty(cu.Email) && cu.Email.Equals(dbUser.Email, StringComparison.OrdinalIgnoreCase)) ||
                    cu.Name.Equals($"{dbUser.FirstName} {dbUser.LastName}", StringComparison.OrdinalIgnoreCase));

                if (!existsInCsv)
                {
                    comparisons.Add(new UserComparisonDTO
                    {
                        Id = dbUser.Id.ToString(),
                        Status = "deleted",
                        DbUser = MapUserToDto(dbUser, departments),
                        CsvUser = null,
                        Conflicts = new List<FieldConflictDTO>(),
                        Selected = false // Don't auto-select deletions
                    });
                }
            }

            return comparisons;
        }

        private List<FieldConflictDTO> DetectConflicts(Domain.Entities.User dbUser, CsvUserDTO csvUser, IEnumerable<Domain.Entities.Department> departments)
        {
            var conflicts = new List<FieldConflictDTO>();

            // Check name
            var dbName = $"{dbUser.FirstName} {dbUser.LastName}";
            if (!dbName.Equals(csvUser.Name, StringComparison.OrdinalIgnoreCase))
            {
                conflicts.Add(new FieldConflictDTO
                {
                    Field = "name",
                    DbValue = dbName,
                    CsvValue = csvUser.Name,
                    SelectedValue = "csv",
                    Selected = true
                });
            }

            // Check email
            if (!string.Equals(dbUser.Email, csvUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                conflicts.Add(new FieldConflictDTO
                {
                    Field = "email",
                    DbValue = dbUser.Email,
                    CsvValue = csvUser.Email,
                    SelectedValue = "csv",
                    Selected = true
                });
            }

            // Check department
            var dbDepartment = departments.FirstOrDefault(d => d.Id == dbUser.DepartmentId);
            if (dbDepartment != null && !dbDepartment.Name.Equals(csvUser.Department, StringComparison.OrdinalIgnoreCase))
            {
                conflicts.Add(new FieldConflictDTO
                {
                    Field = "department",
                    DbValue = dbDepartment.Name,
                    CsvValue = csvUser.Department,
                    SelectedValue = "csv",
                    Selected = true
                });
            }

            return conflicts;
        }

        private CsvUserDTO MapUserToDto(Domain.Entities.User user, IEnumerable<Domain.Entities.Department> departments)
        {
            var department = departments.FirstOrDefault(d => d.Id == user.DepartmentId);

            return new CsvUserDTO
            {
                Id = user.Id.ToString(),
                Name = $"{user.FirstName} {user.LastName}",
                DateOfBirth = DateTime.UtcNow, // Placeholder - add this field to User entity if needed
                Department = department?.Name ?? "Unknown",
                LineManagerId = user.AssignedToId?.ToString(),
                LineManagerName = null, // Would need to fetch this
                Role = "employee", // Add role field to User entity if needed
                Email = user.Email
            };
        }
    }
}
