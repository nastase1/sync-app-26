using SyncApp26.Domain.IRepositories;
using SyncApp26.Domain.Entities;
using SyncApp26.Infrastructure.Context;

namespace SyncApp26.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<User?> GetUserByIdAsync(Guid id)
        {
            var result = _context.Users.FindAsync(id);
            return result.AsTask();
        }

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var result = _context.Users.AsEnumerable();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<User>> GetUsersByDepartmentIdAsync(Guid departmentId)
        {
            var result = _context.Users.Where(u => u.DepartmentId == departmentId).AsEnumerable();
            return Task.FromResult(result);
        }

        public Task<IEnumerable<User>> GetUsersAssignedToAsync(Guid assignedToId)
        {
            var result = _context.Users.Where(u => u.AssignedToId == assignedToId).AsEnumerable();
            return Task.FromResult(result);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}