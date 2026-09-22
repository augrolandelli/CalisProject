using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CalisApi.Auth;
using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Models;
using CalisApi.Services;
using CalisApi.Services.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging estructurado (Serilog)
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Base de datos (SQL Server via EF Core Code-First)
builder.Services.AddDbContext<CalisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticación JWT Bearer
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key no configurada o insegura (mínimo 32 caracteres). Usar user-secrets o variables de entorno.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Autorización por rol — el servidor siempre valida (spec §2)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin))
    .AddPolicy("CloverOrAdmin", policy => policy.RequireRole(Roles.Clover, Roles.Admin));

// CORS: restringido al origen del PWA (ver appsettings)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Validación y servicios de aplicación
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IRutineService, RutineService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IPushService, PushService>();
builder.Services.AddHostedService<SessionEndedNotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICommunityEventService, CommunityEventService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<LocalFileStorage>();
builder.Services.AddSingleton<R2ObjectStorage>();
// Proveedor según configuración: "Local" (disco, por defecto) o "R2" (Cloudflare).
builder.Services.AddSingleton<IObjectStorage>(sp =>
    sp.GetRequiredService<IOptions<StorageSettings>>().Value.Provider
        .Equals("R2", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<R2ObjectStorage>()
        : sp.GetRequiredService<LocalFileStorage>());
builder.Services.AddHostedService<MediaCleanupService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("media-upload", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Semilla de datos solo en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);

    app.MapOpenApi();
}

// Aplicar migraciones pendientes automáticamente en producción.
// Para la beta esto simplifica el deploy; a largo plazo conviene aplicarlas en un job controlado.
if (app.Environment.IsProduction())
{
    await app.ApplyMigrationsAsync();

    using var seedScope = app.Services.CreateScope();
    var db = seedScope.ServiceProvider.GetRequiredService<CalisDbContext>();
    var hasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var config = seedScope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await AdminSeeder.SeedAsync(db, hasher, config, logger);
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

// Detrás de un proxy reverso (Easy Panel / Nginx), respetar los headers X-Forwarded-*.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

// Para tests de integración (WebApplicationFactory)
public partial class Program;
