using Microsoft.EntityFrameworkCore;
using EasyVan.Models;

namespace EasyVan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Van> Vans { get; set; }
    }
}
