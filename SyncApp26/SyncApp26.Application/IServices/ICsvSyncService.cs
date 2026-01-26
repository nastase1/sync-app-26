using SyncApp26.Shared.DTOs.Response.User;

namespace SyncApp26.Application.IServices
{
    public interface ICsvSyncService
    {
        Task<List<UserComparisonDTO>> CompareUsersAsync(List<CsvUserDTO> csvUsers);
    }
}
