using System.Text.Json.Serialization;
using GenesysForge.Application;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Api.Endpoints;
using GenesysForge.Domain;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
    });
builder.Services.AddAuthorization();

// Источники CORS настраиваются через конфиг (Cors:Origins — список через ';'), по умолчанию — dev-фронтенд.
var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:5173")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
app.MapReference();
app.MapCustomContent();
app.MapCharacters();
app.MapNotes();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program; // для WebApplicationFactory в интеграционных тестах
