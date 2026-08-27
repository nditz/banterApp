using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Features.Ai;
using BanterApp.Api.Features.Auth;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Leaderboards;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Features.Predictions;
using BanterApp.Api.Features.TournamentBonuses;
using BanterApp.Api.Features.Football;
using BanterApp.Api.Features.UserPredictions;
using BanterApp.Api.Features.Studio;
using BanterApp.Api.Features.Errors;
using BanterApp.Api.Features.Health;
using BanterApp.Api.Features.Opinions;
using BanterApp.Api.Features.Sync;
using BanterApp.Api.Integrations;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Middleware;
using BanterApp.Api.Services;
using FluentValidation;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IErrorTrackingService, ErrorTrackingService>();
builder.Services.AddSingleton<IApplicationErrorLogger, ApplicationErrorLogger>();
builder.Services.AddSingleton<HangfireErrorLoggingFilter>();
builder.Services.AddScoped<LiveDataResetService>();
builder.Services.AddSingleton<ScoringService>();
builder.Services.AddSingleton<TournamentBonusScoringService>();
builder.Services.AddScoped<CompetitionCatalogService>();
builder.Services.AddScoped<PredictionRescoreService>();
builder.Services.AddScoped<MatchweekBonusService>();
builder.Services.AddSingleton<SessionTokenService>();
builder.Services.AddScoped<TurnstileService>();
builder.Services.AddScoped<OpinionQueryService>();
builder.Services.AddSingleton<IRateLimitMetrics, RateLimitMetrics>();
builder.Services.AddSingleton<IOutboundUrlValidator, OutboundUrlValidator>();
builder.Services.AddSingleton<ISafeHttpClient, SafeHttpClient>();
builder.Services.AddScoped<IProviderUsageGuard, ProviderUsageGuard>();
builder.Services.AddScoped<MatchScriptContextBuilder>();
builder.Services.AddScoped<IAuthAuditService, AuthAuditService>();
builder.Services.AddSingleton<ProductionStartupValidator>();
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<LegalOptions>(builder.Configuration.GetSection(LegalOptions.SectionName));
builder.Services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
builder.Services.AddScoped<AdminOverviewService>();
builder.Services.AddScoped<AdminHealthService>();
builder.Services.AddScoped<AdminReviewService>();
builder.Services.AddScoped<FootballDataAdminService>();
builder.Services.AddScoped<UserPredictionLockService>();
builder.Services.AddScoped<UserPredictionValidator>();
builder.Services.AddScoped<UserPredictionAggregateService>();
builder.Services.AddScoped<BanterContextEnricher>();
builder.Services.AddScoped<UsernameService>();
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddHttpClient();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new PredictionTypeJsonConverter());
    options.SerializerOptions.Converters.Add(new TournamentBonusCategoryJsonConverter());
});
builder.Services.AddBanterIntegrations(builder.Configuration);

builder.Services.AddHangfire((serviceProvider, config) => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage()
    .UseFilter(serviceProvider.GetRequiredService<HangfireErrorLoggingFilter>()));
// Two workers so live-score, news-ingest, and AI jobs don't block each other.
builder.Services.AddHangfireServer(options => options.WorkerCount = 2);

builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));

builder.Services.AddHttpClient<SupabaseAuthService>();

var connectionString = DatabaseConnection.Resolve(builder.Configuration);

if (!builder.Environment.IsDevelopment() &&
    DatabaseConnection.IsDirectSupabaseConnection(connectionString))
{
    throw new InvalidOperationException(
        "The database connection targets Supabase's DIRECT endpoint (db.<ref>.supabase.co), " +
        "which is IPv6-only and unreachable from IPv4-only hosts like Render. " +
        "Use the Supabase connection pooler instead: host 'aws-0-<region>.pooler.supabase.com', " +
        "username 'postgres.<ref>', port 6543 (transaction) or 5432 (session). " +
        "Copy it from Supabase Dashboard -> Connect -> Connection pooling, and update DATABASE_URL.");
}

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    var inMemoryName = builder.Configuration["Database:InMemoryName"];
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase(
            string.IsNullOrWhiteSpace(inMemoryName) ? "BanterApp" : inMemoryName));
}

var jwtSecret = builder.Configuration["Supabase:JwtSecret"];
var supabaseUrl = builder.Configuration["Supabase:Url"]?.TrimEnd('/');

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.Requirements.Add(new AdminRequirement()));
});

var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
var writePermitLimit = builder.Configuration.GetValue("RateLimiting:WritePermitLimit", 30);
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        var metrics = context.HttpContext.RequestServices.GetRequiredService<IRateLimitMetrics>();
        var policy = context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "global";
        var partition = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        metrics.RecordRejection(policy, partition);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
                ? (int?)ra.TotalSeconds
                : windowSeconds
        }, token);
    };

    void AddPolicy(string name, int limit, int windowSec) =>
        options.AddPolicy(name, httpContext =>
        {
            var key = ResolveRateLimitPartition(httpContext, name);
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromSeconds(windowSec),
                QueueLimit = 0
            });
        });

    options.AddFixedWindowLimiter(RateLimitPolicies.Api, limiter =>
    {
        limiter.PermitLimit = permitLimit;
        limiter.Window = TimeSpan.FromSeconds(windowSeconds);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter(RateLimitPolicies.Write, limiter =>
    {
        limiter.PermitLimit = writePermitLimit;
        limiter.Window = TimeSpan.FromSeconds(windowSeconds);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter(RateLimitPolicies.Auth, limiter =>
    {
        limiter.PermitLimit = authPermitLimit;
        limiter.Window = TimeSpan.FromSeconds(windowSeconds);
        limiter.QueueLimit = 0;
    });

    AddPolicy(RateLimitPolicies.AuthLogin, 5, 60);
    AddPolicy(RateLimitPolicies.AuthSignup, 3, 60);
    AddPolicy(RateLimitPolicies.AuthPasswordReset, 3, 900);
    AddPolicy(RateLimitPolicies.AuthSession, authPermitLimit, windowSeconds);
    AddPolicy(RateLimitPolicies.PublicFeed, 60, 60);
    AddPolicy(RateLimitPolicies.PublicSearch, 30, 60);
    AddPolicy(RateLimitPolicies.PublicArticle, 60, 60);
    AddPolicy(RateLimitPolicies.PublicPredictions, 60, 60);
    AddPolicy(RateLimitPolicies.PublicReactions, 20, 60);
    AddPolicy(RateLimitPolicies.OpenAiGenerate, 10, 60);
    AddPolicy(RateLimitPolicies.AdminJobsRun, 5, 60);
    AddPolicy(RateLimitPolicies.AdminJobsPauseResume, 10, 60);
    AddPolicy(RateLimitPolicies.AdminErrorsRetry, 5, 60);
    AddPolicy(RateLimitPolicies.AdminReviewUpdate, 20, 60);
    AddPolicy(RateLimitPolicies.RssSyncTrigger, 2, 3600);
    AddPolicy(RateLimitPolicies.YoutubeSyncTrigger, 2, 3600);
    AddPolicy(RateLimitPolicies.ClientErrorReport, 10, 60);

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveRateLimitPartition(context, "global"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds)
            }));
});

static string ResolveRateLimitPartition(HttpContext context, string policy)
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var sub = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(sub))
        {
            return $"{policy}:user:{sub}";
        }
    }

    if (context.Request.Headers.TryGetValue("X-Anonymous-Id", out var anonId) &&
        !string.IsNullOrWhiteSpace(anonId))
    {
        return $"{policy}:anon:{anonId}";
    }

    return $"{policy}:ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

var allowedOrigins = CorsConfiguration.ResolveAllowedOrigins(
    builder.Configuration,
    builder.Environment.IsDevelopment());

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
        Description = "Premier League prediction and banter platform"
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

if (app.Environment.IsProduction() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Production requires a PostgreSQL connection string.");
}

var startupValidator = app.Services.GetRequiredService<ProductionStartupValidator>();
await startupValidator.ValidateAsync();

await DatabaseSeeder.SeedAsync(app.Services);

app.UseForwardedHeaders();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AdminHangfireDashboardAuthorizationFilter()]
});

HangfireJobRegistration.RegisterRecurringJobs(app);

app.UseCors("Frontend");
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<BotProtectionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<SupabaseJwtMiddleware>();
app.UseMiddleware<AnonymousUserMiddleware>();
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapHealthEndpoints();
app.MapSyncEndpoints();
app.MapMatchEndpoints();
app.MapPredictionEndpoints();
app.MapTournamentBonusEndpoints();
app.MapFootballReferenceEndpoints();
app.MapUserPredictionEndpoints();
app.MapLeagueEndpoints();
app.MapLeaderboardEndpoints();
app.MapFeedEndpoints();
app.MapStudioEndpoints();
app.MapOpinionEndpoints();
app.MapAiEndpoints();
app.MapAuthEndpoints();
app.MapClientErrorEndpoints();
app.MapAdminEndpoints();

app.Run();
