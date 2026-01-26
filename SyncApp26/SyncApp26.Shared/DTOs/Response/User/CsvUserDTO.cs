namespace SyncApp26.Shared.DTOs.Response.User
{
    public class CsvUserDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Department { get; set; }
        public string? LineManagerId { get; set; }
        public string? LineManagerName { get; set; }
        public string Role { get; set; }
        public string? Email { get; set; }
    }
}
