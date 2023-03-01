using Microsoft.EntityFrameworkCore;
using DataAccess.Entities;

namespace DataAccess.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

    public DbSet<CoffeeShop> CoffeeShops { get; set; }
}