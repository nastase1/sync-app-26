using SyncApp26.Domain.Entities;
using SyncApp26.Domain.IRepositories;
using SyncApp26.Shared.DTOs;
using SyncApp26.Shared.DTOs.Response.User;

namespace SyncApp26.Application.Services;

public interface ICsvSyncService
{
    Task<List<UserComparisonDTO>> CompareWithDatabase(List<CsvUserDTO> csvUsers);
    Task<SyncResultDTO> SyncUsers(SyncRequestDTO syncRequest);
}

public class CsvSyncService : ICsvSyncService
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public CsvSyncService(IUserRepository userRepository, IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<List<UserComparisonDTO>> CompareWithDatabase(List<CsvUserDTO> csvUsers)
    {
        var comparisons = new List<UserComparisonDTO>();
        var dbUsers = (await _userRepository.GetAllUsersAsync()).ToList();
        var departments = (await _departmentRepository.GetAllDepartmentsAsync()).ToList();

        // Create a map of email to DB user for quick lookup
        var dbUserMap = dbUsers.ToDictionary(u => u.Email.ToLower(), u => u);

        // Process CSV users
        foreach (var csvUser in csvUsers)
        {
            var email = csvUser.Email.ToLower();
            var comparison = new UserComparisonDTO
            {
                Id = Guid.NewGuid().ToString(),
                Status = "unchanged",
                Selected = false
            };

            if (dbUserMap.TryGetValue(email, out var dbUser))
            {
                // User exists - compare fields
                comparison.DbUser = MapToUserGETResponseDTO(dbUser, dbUsers);
                comparison.CsvUser = new CsvUserDataDTO
                {
                    FirstName = csvUser.FirstName,
                    LastName = csvUser.LastName,
                    Email = csvUser.Email,
                    DepartmentName = csvUser.DepartmentName,
                    AssignedToName = csvUser.AssignedToEmail != null
                        ? dbUsers.FirstOrDefault(u => u.Email.ToLower() == csvUser.AssignedToEmail.ToLower())?.FirstName + " " +
                          dbUsers.FirstOrDefault(u => u.Email.ToLower() == csvUser.AssignedToEmail.ToLower())?.LastName
                        : null
                };

                // Detect conflicts
                var conflicts = new List<FieldConflictDTO>();

                if (dbUser.FirstName != csvUser.FirstName)
                {
                    conflicts.Add(new FieldConflictDTO
                    {
                        Field = "firstName",
                        DbValue = dbUser.FirstName,
                        CsvValue = csvUser.FirstName,
                        Selected = false
                    });
                }

                if (dbUser.LastName != csvUser.LastName)
                {
                    conflicts.Add(new FieldConflictDTO
                    {
                        Field = "lastName",
                        DbValue = dbUser.LastName,
                        CsvValue = csvUser.LastName,
                        Selected = false
                    });
                }

                if (dbUser.Department.Name != csvUser.DepartmentName)
                {
                    conflicts.Add(new FieldConflictDTO
                    {
                        Field = "departmentName",
                        DbValue = dbUser.Department.Name,
                        CsvValue = csvUser.DepartmentName,
                        Selected = false
                    });
                }

                // Check line manager
                var csvManagerEmail = csvUser.AssignedToEmail?.ToLower();
                var dbManagerId = dbUser.AssignedToId;
                var csvManager = csvManagerEmail != null ? dbUsers.FirstOrDefault(u => u.Email.ToLower() == csvManagerEmail) : null;

                if ((csvManager?.Id ?? null) != dbManagerId)
                {
                    conflicts.Add(new FieldConflictDTO
                    {
                        Field = "assignedToName",
                        DbValue = dbUser.AssignedTo != null ? $"{dbUser.AssignedTo.FirstName} {dbUser.AssignedTo.LastName}" : null,
                        CsvValue = csvManager != null ? $"{csvManager.FirstName} {csvManager.LastName}" : null,
                        Selected = false
                    });
                }

                comparison.Conflicts = conflicts;
                comparison.Status = conflicts.Count > 0 ? "modified" : "unchanged";
                comparison.Selected = conflicts.Count > 0; // Auto-select modified records
            }
            else
            {
                // New user from CSV
                comparison.Status = "new";
                comparison.CsvUser = new CsvUserDataDTO
                {
                    FirstName = csvUser.FirstName,
                    LastName = csvUser.LastName,
                    Email = csvUser.Email,
                    DepartmentName = csvUser.DepartmentName,
                    AssignedToName = csvUser.AssignedToEmail != null
                        ? dbUsers.FirstOrDefault(u => u.Email.ToLower() == csvUser.AssignedToEmail.ToLower())?.FirstName + " " +
                          dbUsers.FirstOrDefault(u => u.Email.ToLower() == csvUser.AssignedToEmail.ToLower())?.LastName
                        : null
                };
                comparison.Selected = true; // Auto-select new records
            }

            comparisons.Add(comparison);
        }

        // Find deleted users (in DB but not in CSV)
        var csvEmails = csvUsers.Select(u => u.Email.ToLower()).ToHashSet();
        foreach (var dbUser in dbUsers)
        {
            if (!csvEmails.Contains(dbUser.Email.ToLower()))
            {
                comparisons.Add(new UserComparisonDTO
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = "deleted",
                    DbUser = MapToUserGETResponseDTO(dbUser, dbUsers),
                    Selected = false // Don't auto-select deletions
                });
            }
        }

        return comparisons;
    }

    public async Task<SyncResultDTO> SyncUsers(SyncRequestDTO syncRequest)
    {
        var result = new SyncResultDTO { Success = true };
        var dbUsers = (await _userRepository.GetAllUsersAsync()).ToList();
        var departments = (await _departmentRepository.GetAllDepartmentsAsync()).ToList();

        foreach (var item in syncRequest.Items)
        {
            try
            {
                if (item.Status == "new" && item.CsvData != null)
                {
                    // Create new user
                    var department = departments.FirstOrDefault(d => d.Name.Equals(item.CsvData.DepartmentName, StringComparison.OrdinalIgnoreCase));
                    if (department == null)
                    {
                        // Create department if it doesn't exist
                        department = new Department
                        {
                            Id = Guid.NewGuid(),
                            Name = item.CsvData.DepartmentName,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _departmentRepository.AddDepartmentAsync(department);
                    }

                    var assignedToId = item.CsvData.AssignedToEmail != null
                        ? dbUsers.FirstOrDefault(u => u.Email.Equals(item.CsvData.AssignedToEmail, StringComparison.OrdinalIgnoreCase))?.Id
                        : null;

                    var newUser = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = item.CsvData.FirstName,
                        LastName = item.CsvData.LastName,
                        Email = item.CsvData.Email,
                        DepartmentId = department.Id,
                        AssignedToId = assignedToId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _userRepository.AddUserAsync(newUser);
                    result.RecordsProcessed++;
                }
                else if (item.Status == "modified" && item.CsvData != null)
                {
                    // Update existing user
                    var existingUser = dbUsers.FirstOrDefault(u => u.Email.Equals(item.CsvData.Email, StringComparison.OrdinalIgnoreCase));
                    if (existingUser != null)
                    {
                        // Apply selected conflict resolutions
                        foreach (var conflict in item.Conflicts.Where(c => c.Selected))
                        {
                            if (conflict.SelectedValue == "csv")
                            {
                                switch (conflict.Field.ToLower())
                                {
                                    case "firstname":
                                        existingUser.FirstName = item.CsvData.FirstName;
                                        break;
                                    case "lastname":
                                        existingUser.LastName = item.CsvData.LastName;
                                        break;
                                    case "departmentname":
                                        var department = departments.FirstOrDefault(d => d.Name.Equals(item.CsvData.DepartmentName, StringComparison.OrdinalIgnoreCase));
                                        if (department == null)
                                        {
                                            department = new Department
                                            {
                                                Id = Guid.NewGuid(),
                                                Name = item.CsvData.DepartmentName,
                                                CreatedAt = DateTime.UtcNow
                                            };
                                            await _departmentRepository.AddDepartmentAsync(department);
                                        }
                                        existingUser.DepartmentId = department.Id;
                                        break;
                                    case "assignedtoname":
                                        existingUser.AssignedToId = item.CsvData.AssignedToEmail != null
                                            ? dbUsers.FirstOrDefault(u => u.Email.Equals(item.CsvData.AssignedToEmail, StringComparison.OrdinalIgnoreCase))?.Id
                                            : null;
                                        break;
                                }
                            }
                        }

                        existingUser.UpdatedAt = DateTime.UtcNow;
                        await _userRepository.UpdateUserAsync(existingUser);
                        result.RecordsProcessed++;
                    }
                }
                else if (item.Status == "deleted")
                {
                    // Soft delete user (you could also use hard delete)
                    var userToDelete = dbUsers.FirstOrDefault(u => u.Id.ToString() == item.Id);
                    if (userToDelete != null)
                    {
                        userToDelete.DeletedAt = DateTime.UtcNow;
                        await _userRepository.UpdateUserAsync(userToDelete);
                        result.RecordsProcessed++;
                    }
                }
                else
                {
                    result.RecordsSkipped++;
                }
            }
            catch (Exception ex)
            {
                result.RecordsFailed++;
                result.Errors.Add($"Failed to process user {item.CsvData?.Email ?? item.Id}: {ex.Message}");
            }
        }

        result.Success = result.RecordsFailed == 0;
        result.Message = result.Success
            ? $"Successfully synced {result.RecordsProcessed} records"
            : $"Synced with errors: {result.RecordsFailed} failed";

        return result;
    }

    private UserGETResponseDTO MapToUserGETResponseDTO(User user, List<User> allUsers)
    {
        return new UserGETResponseDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department.Name,
            AssignedToId = user.AssignedToId,
            AssignedToName = user.AssignedTo != null ? $"{user.AssignedTo.FirstName} {user.AssignedTo.LastName}" : null,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
