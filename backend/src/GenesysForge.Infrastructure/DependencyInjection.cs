using GenesysForge.Application.Abstractions;
using GenesysForge.Infrastructure.Auth;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (config.GetValue<bool>("UseInMemoryDatabase"))
                options.UseInMemoryDatabase(config["InMemoryDatabaseName"] ?? "genesysforge-tests");
            else
                options.UseNpgsql(config.GetConnectionString("Default")
                    ?? "Host=localhost;Port=5432;Database=genesysforge;Username=genesys;Password=genesys_dev");
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        return services;
    }

    /// <summary>Создание схемы и сид встроенного контента при старте.</summary>
    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational()) db.Database.EnsureCreated();
        SeedData.Apply(db);
    }
}
