using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace MVCPrject.Data
{
    public class DBContext : IdentityDbContext

    {

        public DbSet<Recipe> Recipes { get; set; }
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

    }
}
