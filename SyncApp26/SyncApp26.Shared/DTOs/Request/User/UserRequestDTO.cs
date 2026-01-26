namespace SyncApp26.Shared.DTOs.Request.User
{
    public class UserRequestDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? AssignedToId { get; set; }
    }
}
