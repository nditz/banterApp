using System.Text;
using System.Threading.RateLimiting;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Features.Ai;
using BanterApp.Api.Features.Auth;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Leaderboards;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Features.Predictions;
using BanterApp.Api.Integrations;
using BanterApp.Api.Middleware;
using BanterApp.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddSingleton<ScoringService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddBanterIntegrations();

builder.Services.Configure<SupabaseOptions>(options =>
{
    options.Url = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL")
        ?? builder.Configuration["Supabase:Url"]
        ?? string.Empty;
    options.AnonKey = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_ANON_KEY")
        ?? builder.Configuration["Supabase:AnonKey"]
        ?? string.Empty;
    options.JwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
        ?? builder.Configuration["Supabase:JwtSecret"]
        ?? string.Empty;
});

builder.Services.AddHttpClient<SupabaseAuthService>();

var connectionString = ResolveConnectionString(builder.Configuration);
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("BanterApp"));
}

var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? builder.Configuration["Supabase:JwtSecret"];
var supabaseUrl = (Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL")
    ?? builder.Configuration["Supabase:Url"])?.TrimEnd('/');

var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    authBuilder.AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(supabaseUrl),
            ValidIssuer = string.IsNullOrWhiteSpace(supabaseUrl) ? null : $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
}
else
{
    authBuilder.AddJwtBearer();
}

builder.Services.AddAuthorization();

var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = permitLimit;
        limiter.Window = TimeSpan.FromSeconds(windowSeconds);
        limiter.QueueLimit = 0;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds)
            }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BanterApp API",
        Version = "v1",
        Description = "Phase 1 — World Cup prediction battle platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Supabase JWT. Format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<SupabaseJwtMiddleware>();
app.UseMiddleware<AnonymousUserMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapMatchEndpoints();
app.MapPredictionEndpoints();
app.MapLeagueEndpoints();
app.MapLeaderboardEndpoints();
app.MapFeedEndpoints();
app.MapAiEndpoints();
app.MapAuthEndpoints();

app.Run();

static string? ResolveConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        return databaseUrl;
    }

    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? configuration.GetConnectionString("DefaultConnection");

    return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
}
