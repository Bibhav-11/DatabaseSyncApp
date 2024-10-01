using Microsoft.EntityFrameworkCore;

namespace SyncTest;

public class TargetContext: DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Data Source=BIBHAV--LAMICHH\\SQLEXPRESS;Initial Catalog=TestTarget;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True;");
    }

    public DbSet<Test> Test { get; set; }
}