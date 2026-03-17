namespace SyncApp26.Shared.DTOs.CSV.History
{
    public class ImportConflictRequestDTO
    {
        public Guid ImportHistoryId { get; set; }
        public Guid UserId { get; set; }
        public required string FieldName { get; set; } //department, line manager
        public required string OldValue { get; set; }
        public required string NewValue { get; set; }
        public required string Status { get; set; } //accepted, rejected
    }
}