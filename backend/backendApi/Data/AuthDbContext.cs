// using Microsoft.AspNetCore.Identity;
// // using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore;

// namespace backendApi.Data;

// public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
// {
//     public AppDbContext(DbContextOptions<AppDbContext> options)
//         : base(options)
//     {
        
//     }
// }
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backendApi.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    { 
    }

    // later you can add your own tables, e.g.
    // public DbSet<GeneratedDocument> Documents { get; set; }
}