using Microsoft.EntityFrameworkCore;

namespace SyncTest;

public class SourceContext: DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Data Source=BIBHAV--LAMICHH\\SQLEXPRESS;Initial Catalog=TestSource;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True;");
    }
    
    public DbSet<Test> Test { get; set; }
}