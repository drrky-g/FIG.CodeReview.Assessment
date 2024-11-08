using Microsoft.EntityFrameworkCore;

namespace FIG.Assessment.Models.Database;

// a dummy EFCore dbcontext - not concerned with actually setting up connection strings or configuring the context in this example
public class UserContext : DbContext
{
    public DbSet<UserDb> Users { get; set; }
}