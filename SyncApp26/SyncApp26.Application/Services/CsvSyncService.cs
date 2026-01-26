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

        public async Task<SyncResultDTO> SyncUsersAsync(List<UserComparisonDTO> usersToSync)
        {
            var result = new SyncResultDTO
            {
                Success = true,
                RecordsProcessed = 0,
                RecordsFailed = 0,
                RecordsSkipped = 0,
                Errors = new List<string>()
            };

            var departments = await _departmentService.GetAllDepartmentsAsync();

            foreach (var comparison in usersToSync)
            {
                try
                {
                    if (comparison.Status == "new" && comparison.CsvUser != null)
                    {
                        // Create new user
                        await CreateUserFromCsv(comparison.CsvUser, departments);
                        result.RecordsProcessed++;
                    }
                    else if (comparison.Status == "modified" && comparison.CsvUser != null)
                    {
                        // Update existing user with resolved conflicts
                        await UpdateUserFromCsv(comparison, departments);
                        result.RecordsProcessed++;
                    }
                    else if (comparison.Status == "deleted")
                    {
                        // Soft delete user
                        if (Guid.TryParse(comparison.Id, out var userId))
                        {
                            await _userService.DeleteUserAsync(userId);
                            result.RecordsProcessed++;
                        }
                        else
                        {
                            result.RecordsSkipped++;
                            result.Errors?.Add($"Invalid user ID for deletion: {comparison.Id}");
                        }
                    }
                    else if (comparison.Status == "unchanged")
                    {
                        result.RecordsSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    result.RecordsFailed++;
                    result.Errors?.Add($"Error processing user {comparison.Id}: {ex.Message}");
                    result.Success = false;
                }
            }

            result.Message = result.Success
                ? $"Sync completed successfully: {result.RecordsProcessed} processed, {result.RecordsSkipped} skipped"
                : $"Sync completed with errors: {result.RecordsProcessed} processed, {result.RecordsFailed} failed, {result.RecordsSkipped} skipped";

            return result;
        }

        private async Task CreateUserFromCsv(CsvUserDTO csvUser, IEnumerable<Domain.Entities.Department> departments)
        {
            // Parse name
            var nameParts = csvUser.Name.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : csvUser.Name;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Find department by name
            var department = departments.FirstOrDefault(d =>
                d.Name.Equals(csvUser.Department, StringComparison.OrdinalIgnoreCase));

            if (department == null)
            {
                throw new Exception($"Department '{csvUser.Department}' not found");
            }

            // Parse AssignedToId if provided
            Guid? assignedToId = null;
            if (!string.IsNullOrEmpty(csvUser.LineManagerId) && Guid.TryParse(csvUser.LineManagerId, out var parsedId))
            {
                assignedToId = parsedId;
            }

            var user = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = csvUser.Email,
                DepartmentId = department.Id,
                AssignedToId = assignedToId,
                CreatedAt = DateTime.UtcNow
            };

            await _userService.AddUserAsync(user);
        }

        private async Task UpdateUserFromCsv(UserComparisonDTO comparison, IEnumerable<Domain.Entities.Department> departments)
        {
            if (!Guid.TryParse(comparison.Id, out var userId))
            {
                throw new Exception($"Invalid user ID: {comparison.Id}");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new Exception($"User not found: {userId}");
            }

            // Apply changes based on resolved conflicts
            foreach (var conflict in comparison.Conflicts)
            {
                if (conflict.SelectedValue == "csv" && comparison.CsvUser != null)
                {
                    switch (conflict.Field.ToLower())
                    {
                        case "name":
                            var nameParts = comparison.CsvUser.Name.Split(' ', 2);
                            user.FirstName = nameParts.Length > 0 ? nameParts[0] : comparison.CsvUser.Name;
                            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                            break;

                        case "email":
                            user.Email = comparison.CsvUser.Email;
                            break;

                        case "department":
                            var department = departments.FirstOrDefault(d =>
                                d.Name.Equals(comparison.CsvUser.Department, StringComparison.OrdinalIgnoreCase));
                            if (department != null)
                            {
                                user.DepartmentId = department.Id;
                            }
                            break;
                    }
                }
                // If selectedValue is "db", keep existing value (do nothing)
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user);
        }
    }
}
