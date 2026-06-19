using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
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
        // Режим наполнения справочного контента (private full / public safe). По умолчанию — PrivateFull.
        services.Configure<ContentOptions>(o =>
            o.Mode = Enum.TryParse<ContentMode>(config["Content:Mode"], ignoreCase: true, out var m)
                ? m : ContentMode.PrivateFull);

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
        services.AddSingleton<IExternalIdentityValidator, GoogleIdTokenValidator>();

        return services;
    }

    /// <summary>Применение миграций и сид встроенного контента при старте (в режиме из конфигурации).</summary>
    public static void InitializeDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mode = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ContentOptions>>().Value.Mode;
        if (db.Database.IsRelational()) db.Database.Migrate();
        SeedData.Apply(db, mode);
    }
}

/// <summary>Опции наполнения справочного контента, читаются из конфигурации (<c>Content:Mode</c>).</summary>
public sealed class ContentOptions
{
    public ContentMode Mode { get; set; } = ContentMode.PrivateFull;
}
