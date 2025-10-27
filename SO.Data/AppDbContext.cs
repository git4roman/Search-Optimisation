using Microsoft.EntityFrameworkCore;

namespace SO.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
        
    }
}