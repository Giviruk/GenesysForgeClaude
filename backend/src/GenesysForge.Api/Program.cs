using System.Text.Json.Serialization;
using GenesysForge.Api.Contracts;
using GenesysForge.Api.Data;
using GenesysForge.Api.Endpoints;
using GenesysForge.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
        options.UseInMemoryDatabase(builder.Configuration["InMemoryDatabaseName"] ?? "genesysforge-tests");
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=genesysforge;Username=genesys;Password=genesys_dev");
});

builder.Services.AddScoped<CharacterService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = "GenesysForge",
            ValidAudience = "GenesysForge",
            IssuerSigningKey = TokenService.GetSigningKey(builder.Configuration),
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    options.SerializerOptions.DictionaryKeyPolicy = null; // ключи словарей (тиры талантов) — как есть
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational()) db.Database.EnsureCreated();
    SeedData.Apply(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Доменные ошибки правил → 400 с сообщением.
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
});

app.MapOpenApi();
app.MapAuth();
app.MapReference();
app.MapCustomContent();
app.MapCharacters();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program; // для WebApplicationFactory в интеграционных тестах
