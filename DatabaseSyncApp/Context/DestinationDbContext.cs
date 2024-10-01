using System.Configuration;
using DatabaseSyncApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSyncApp.Context;

public class DestinationDbContext: DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Log> Logs { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(ConfigurationManager.ConnectionStrings["DestinationConnectionString"].ConnectionString);
    }
}