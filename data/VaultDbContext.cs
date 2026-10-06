using Microsoft.EntityFrameworkCore;
using VaultCrypt.Models;

namespace VaultCrypt.Data
{
    public class VaultDbContext : DbContext
    {
        public VaultDbContext(DbContextOptions<VaultDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Document> Documents { get; set; }
    }
}