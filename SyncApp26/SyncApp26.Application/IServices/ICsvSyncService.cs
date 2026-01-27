using SyncApp26.Shared.DTOs;

namespace SyncApp26.Application.IServices
{
    public interface ICsvSyncService
    {
        Task<List<UserComparisonDTO>> CompareWithDatabase(List<CsvUserDTO> csvUsers);
        Task<SyncResultDTO> SyncUsers(SyncRequestDTO syncRequest);
    }
}