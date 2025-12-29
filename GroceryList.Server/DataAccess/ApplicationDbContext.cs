using GroceryList.Server.DataAccess.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace GroceryList.Server.DataAccess;

public interface IApplicationDbContext
{
    public DbSet<GroceryItem> GroceryItems { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<GroceryItem> GroceryItems => Set<GroceryItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
