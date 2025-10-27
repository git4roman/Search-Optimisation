using Microsoft.EntityFrameworkCore;
using SO.Core;

namespace SO.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
        
    }
    public DbSet<UserEntity> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}