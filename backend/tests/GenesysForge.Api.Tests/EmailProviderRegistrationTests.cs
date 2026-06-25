using GenesysForge.Application.Abstractions;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

/// <summary>DI выбирает реализацию IEmailSender по конфигу Email:Provider.</summary>
public class EmailProviderRegistrationTests
{
    private static IEmailSender Resolve(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider().GetRequiredService<IEmailSender>();
    }

    [Fact]
    public void Smtp_provider_selects_smtp_sender()
    {
        var sender = Resolve(new()
        {
            ["UseInMemoryDatabase"] = "true",
            ["Email:Provider"] = "Smtp",
            ["Email:Smtp:Host"] = "smtp.example.com",
        });
        Assert.IsType<SmtpEmailSender>(sender);
    }

    [Fact]
    public void Default_provider_selects_logging_stub()
    {
        var sender = Resolve(new() { ["UseInMemoryDatabase"] = "true" });
        Assert.IsType<LoggingEmailSender>(sender);
    }
}
