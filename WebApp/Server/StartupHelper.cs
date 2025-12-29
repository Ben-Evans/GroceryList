using Microsoft.EntityFrameworkCore;

namespace WebApp.Server;

public static class StartupHelper
{
    public static void SetupDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options
                .UseSqlServer(connectionString)
#if DEBUG
                .EnableSensitiveDataLogging()
#endif
            );

        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
    }

    public static async Task ApplyMigrationsAndSeedDatabase(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
            await dbContext.Database.MigrateAsync();
    }
}
