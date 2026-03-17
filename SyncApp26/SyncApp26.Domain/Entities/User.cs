namespace SyncApp26.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? AssignedToId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PersonalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Department? Department { get; set; }
        public User? AssignedTo { get; set; }  // Line manager
        public ICollection<User> AssignedUsers { get; set; } = new List<User>();  // Direct reports
    }
}