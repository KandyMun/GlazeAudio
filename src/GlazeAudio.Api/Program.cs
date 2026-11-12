using GlazeAudio.Api.Data;
using GlazeAudio.Api.Endpoints;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Infrastructure.Auth;
using GlazeAudio.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (Azure SQL / MSSQL via EF Core) ----------
var connectionString = builder.Configuration.GetConnectionString("GlazeAudioDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'GlazeAudioDb' is not set. Run infra/azure-sql-setup.sh, or set it with: " +
        "dotnet user-secrets set \"ConnectionStrings:GlazeAudioDb\" \"<connection string>\" --project src/GlazeAudio.Api");
}

builder.Services.AddDbContext<GlazeAudioDbContext>(options => options
    .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 6))
    .UseSeeding((context, _) => SeedData.Seed(context))
    .UseAsyncSeeding((context, _, ct) => SeedData.SeedAsync(context, ct)));

// Salted PBKDF2 password hashing (from ASP.NET Core Identity, no extra package needed).
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// ---------- Authentication (JWT access tokens) & authorization (roles from the token) ----------
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be set to a secret of at least 32 characters. Locally: " +
        "dotnet user-secrets set \"Jwt:SigningKey\" \"<random secret>\" --project src/GlazeAudio.Api " +
        "(the setup script does this); in Azure: the App Service setting Jwt__SigningKey.");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, JwtBearerHandler>(JwtBearerHandler.SchemeName, null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthClaims.AdminPolicy, policy => policy.RequireRole(UserRoles.Admin));

// ---------- Error handling: problem+json for 400 / 404 / 422 / 500 ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();

// ---------- OpenAPI specification ----------
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "GlazeAudio API";
    document.Info.Version = "v1";
    document.Info.Description =
        "REST API for GlazeAudio – a platform for reviewing music. " +
        "Resources are hierarchical: albums → songs → reviews. Reviews are written by users.";
    return Task.CompletedTask;
})
    .AddBearerSecurity());

// ---------- CORS: the frontend (Azure Static Web Apps / Vite dev server) calls the API from another origin ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins);
    else policy.AllowAnyOrigin();   // no list configured: allow any (tokens are sent in a header, not cookies)
    policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location");
}));

var app = builder.Build();

// Create/upgrade the schema and seed data on startup (UseSeeding runs only when the DB is empty).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GlazeAudioDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();                                    // GET /openapi/v1.json
app.MapGet("/docs", () => Results.Content(SwaggerUi.Html, "text/html")).ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

// Hypermedia entry point: a client can start here and follow the links.
app.MapGet("/api", (HttpRequest request) => Results.Ok(new ApiRoot("GlazeAudio API", "v1") { Links = ApiLinks.Root(request) }))
    .WithName("GetApiRoot")
    .WithTags("Root")
    .WithSummary("API entry point with links to the main resources")
    .Produces<ApiRoot>();

app.MapAlbumEndpoints();
app.MapSongEndpoints();
app.MapReviewEndpoints();
app.MapUserEndpoints();
app.MapAuthEndpoints();

app.Run();

// Exposed for integration tests (WebApplicationFactory<Program>).
public partial class Program;

/// <summary>Body of GET /api.</summary>
public record ApiRoot(string Name, string Version) : Resource;
