using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SyncApp26.Infrastructure.Context
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Configure SQLite with the connection string
            optionsBuilder.UseSqlite("Data Source=SyncApp26.db");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
