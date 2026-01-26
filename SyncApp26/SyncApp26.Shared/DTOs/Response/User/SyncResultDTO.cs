namespace SyncApp26.Shared.DTOs.Response.User
{
    public class SyncResultDTO
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsFailed { get; set; }
        public int RecordsSkipped { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
