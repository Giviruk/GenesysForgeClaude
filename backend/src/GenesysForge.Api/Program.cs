using System.Text.Json.Serialization;
using GenesysForge.Application;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Api;
using GenesysForge.Api.Endpoints;
using GenesysForge.Api.Realtime;
using GenesysForge.Domain;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

ProductionConfiguration.Validate(builder.Configuration, builder.Environment);

// Структурное логирование на Serilog: compact JSON в Production (для агрегаторов логов),
// человекочитаемый текст в Development. Дополнительные настройки можно задать в "Serilog" секции конфига.
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .MinimumLevel.Information()
        // Серый шум фреймворка глушим: единый summary на запрос даёт UseSerilogRequestLogging.
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsProduction())
        loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
    else
        loggerConfiguration.WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthRateLimiting(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<ICampaignNotifier, SignalRCampaignNotifier>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = TokenService.Issuer,
            ValidAudience = TokenService.Issuer,
            IssuerSigningKey = TokenService.GetSigningKey(builder.Configuration),
            ValidateIssuerSigningKey = true,
        };
        // WebSocket'ы не передают заголовок Authorization — для хабов берём токен из query (?access_token=).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
    // Docker network addresses are dynamic; only reverse proxies can reach the API in production compose.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Источники CORS настраиваются через конфиг. ProductionConfiguration требует явные HTTPS origins.
var corsOrigins = ProductionConfiguration.ParseCorsOrigins(builder.Configuration);
if (corsOrigins.Length == 0) corsOrigins = ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    options.SerializerOptions.DictionaryKeyPolicy = null; // ключи словарей (тиры талантов) — как есть
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.Services.InitializeDatabase();

app.UseForwardedHeaders();
// Одна структурная запись на запрос (метод, путь, статус, длительность) + traceId/remoteIp.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});
if (builder.Configuration.GetValue("RateLimiting:Enabled", true))
    app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Исключения Application/Domain → HTTP-статусы с сообщением.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (DomainRuleException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message));
    }
    catch (ConflictException ex)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message));
    }
    catch (UnauthorizedException ex)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message));
    }
});

app.MapOpenApi();
app.MapAuth();
app.MapAccount();
app.MapReference();
app.MapSearch();
app.MapCustomContent();
app.MapHomebrewPacks();
app.MapCharacters();
app.MapNotes();
app.MapCampaigns();
app.MapSpells();
app.MapNpcs();
app.MapGameTable();
app.MapRolls();
app.MapEncounters();
app.MapContentPacks();
app.MapHub<CampaignHub>("/hubs/campaign");
app.MapGet("/api/health", async (
    GenesysForge.Infrastructure.Persistence.AppDbContext db,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var databaseOk = await db.Database.CanConnectAsync(ct);
        return databaseOk
            ? Results.Ok(new { status = "ok", database = "ok" })
            : Results.Json(new { status = "degraded", database = "unavailable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database health check failed");
        return Results.Json(new { status = "degraded", database = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

public partial class Program; // для WebApplicationFactory в интеграционных тестах
