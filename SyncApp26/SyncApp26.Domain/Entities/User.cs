namespace SyncApp26.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? AssignedToId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Department Department { get; set; }
    }
}