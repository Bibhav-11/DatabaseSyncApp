using System.Configuration;
using DatabaseSyncApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSyncApp.Context;

public class SourceDbContext: DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Location> Locations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConfigurationManager.ConnectionStrings["SourceConnectionString"].ConnectionString);
    }
}