namespace SyncApp26.Shared.DTOs.Response.User
{
    public class UserComparisonDTO
    {
        public string Id { get; set; }
        public string Status { get; set; } // "new", "modified", "unchanged", "deleted"
        public CsvUserDTO? DbUser { get; set; }
        public CsvUserDTO? CsvUser { get; set; }
        public List<FieldConflictDTO> Conflicts { get; set; } = new();
        public bool Selected { get; set; }
    }
}
