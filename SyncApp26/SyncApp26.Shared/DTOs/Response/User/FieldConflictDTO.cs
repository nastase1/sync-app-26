namespace SyncApp26.Shared.DTOs.Response.User
{
    public class FieldConflictDTO
    {
        public string Field { get; set; }
        public object? DbValue { get; set; }
        public object? CsvValue { get; set; }
        public string? SelectedValue { get; set; }
        public bool Selected { get; set; }
    }
}
